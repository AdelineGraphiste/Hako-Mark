using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using HakoMark.Core;
using HakoMark.Drawing;

namespace HakoMark.Overlay;

public sealed class OverlayWindow : Window
{
    private const int GwlExstyle = -20, WsExTransparent = 0x20, WsExToolWindow = 0x80, WsExNoActivate = 0x08000000;
    private readonly AnnotationCanvas _canvas;
    private readonly HakoSettings _settings;
    private bool _annotationMode;
    private IntPtr _returnFocus;

    public OverlayWindow(HakoSettings settings, AnnotationHistory history)
    {
        Title="Hako Mark — Écran"; _settings = settings; WindowStyle = WindowStyle.None; ResizeMode = ResizeMode.NoResize; AllowsTransparency = true; Background = Brushes.Transparent; ShowInTaskbar = false; Topmost = true;
        Left = SystemParameters.VirtualScreenLeft; Top = SystemParameters.VirtualScreenTop; Width = SystemParameters.VirtualScreenWidth; Height = SystemParameters.VirtualScreenHeight;
        _canvas = new AnnotationCanvas(settings, history) { IsEnabled = false }; Content = _canvas; Loaded += (_, _) => SetClickThrough(true);
        _canvas.GestureFinished += () => { if(settings.ReturnToNavigationAfterDrawing) AnnotationMode=false; };
        PreviewMouseRightButtonDown += (_,e) => { if(AnnotationMode && settings.RightClickExits) { AnnotationMode=false; e.Handled=true; } };
    }

    public bool AnnotationMode
    {
        get => _annotationMode;
        set {
            if(value && !_annotationMode) _returnFocus=FindExternalWindow();
            _annotationMode = value; _canvas.CancelDrawing(); SetClickThrough(!value); _canvas.IsEnabled = value;
            Background = value ? new SolidColorBrush(Color.FromArgb(1, 0, 0, 0)) : Brushes.Transparent;
            if (value) { Activate(); _canvas.Focus(); Keyboard.Focus(_canvas); }
            else if(_returnFocus!=IntPtr.Zero && NativeMethods.IsWindow(_returnFocus)) {
                NativeMethods.GetWindowThreadProcessId(NativeMethods.GetForegroundWindow(),out var process);
                if(process==Environment.ProcessId) NativeMethods.SetForegroundWindow(_returnFocus);
                _returnFocus=IntPtr.Zero;
            }
            ModeChanged?.Invoke();
        }
    }
    private static IntPtr FindExternalWindow() {
        var window=NativeMethods.GetForegroundWindow();
        for(int i=0;window!=IntPtr.Zero && i<256;i++,window=NativeMethods.GetWindow(window,2)) {
            NativeMethods.GetWindowThreadProcessId(window,out var process);
            if(process!=Environment.ProcessId && NativeMethods.IsWindowVisible(window))return window;
        }
        return IntPtr.Zero;
    }
    public event Action? ModeChanged;

    public void SetTool(ToolKind tool) { _canvas.SelectedTool = tool; _canvas.Cursor=tool==ToolKind.Freehand ? Cursors.Pen : tool==ToolKind.Eraser ? Cursors.Hand : Cursors.Cross; }
    public void Undo() { _canvas.History.Undo(); _canvas.InvalidateVisual(); }
    public void Redo() { _canvas.History.Redo(); _canvas.InvalidateVisual(); }
    public void ClearAll() { _canvas.History.Clear(); _canvas.InvalidateVisual(); }

    private void SetClickThrough(bool clickThrough)
    {
        if (!IsLoaded) return; var hwnd = new WindowInteropHelper(this).Handle; var style = NativeMethods.GetWindowLong(hwnd, GwlExstyle);
        style |= WsExToolWindow; if (clickThrough) style |= WsExTransparent | WsExNoActivate; else style &= ~(WsExTransparent | WsExNoActivate); NativeMethods.SetWindowLong(hwnd, GwlExstyle, style);
    }
}

internal sealed class AnnotationCanvas : FrameworkElement
{
    private readonly HakoSettings _settings; private readonly DrawingToolFactory _factory = new(); private Point _start; private bool _drawing; private readonly List<PointD> _points = [];
    public AnnotationHistory History { get; } public ToolKind SelectedTool { get; set; } = ToolKind.Arrow;
    public event Action? GestureFinished;
    private bool _erasing;

    public AnnotationCanvas(HakoSettings settings, AnnotationHistory history)
    {
        _settings = settings; History = history; Focusable = true; IsHitTestVisible = false;
        IsEnabledChanged += (_, _) => IsHitTestVisible = IsEnabled; MouseLeftButtonDown += OnMouseDown; MouseMove += OnMouseMove; MouseLeftButtonUp += OnMouseUp;
    }

    public void CancelDrawing() { _drawing = false; _erasing=false; ReleaseMouseCapture(); InvalidateVisual(); }
    internal void CompleteGesture() {
        // Release capture before changing the native click-through style.
        // Never forward the finishing click to the application underneath.
        _drawing=false; _erasing=false; ReleaseMouseCapture(); InvalidateVisual();
        GestureFinished?.Invoke();
    }
    protected override void OnRender(DrawingContext dc) { base.OnRender(dc); dc.DrawRectangle(Brushes.Transparent, null, new Rect(RenderSize)); foreach (var annotation in History.Items) DrawAnnotation(dc, annotation); if (_drawing) DrawPreview(dc); }
    private void OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (!IsEnabled) return;
        if(SelectedTool == ToolKind.Eraser) {
            _erasing=true; CaptureMouse(); e.Handled=true;
            var p=e.GetPosition(this);
            var hit=History.Items.Reverse().FirstOrDefault(a => Hit(a,p));
            if(hit is not null) History.Remove(hit); InvalidateVisual(); return;
        }
        _drawing = true; _start = e.GetPosition(this); _points.Clear(); _points.Add(ToPointD(_start)); CaptureMouse(); InvalidateVisual();
    }
    private void OnMouseMove(object sender, MouseEventArgs e) {
        if (!_drawing) return;
        if (SelectedTool == ToolKind.Freehand) {
            var p=e.GetPosition(this); var s=_settings.Options(ToolKind.Freehand).Smoothing;
            var last=_points[^1]; _points.Add(new PointD(last.X*s+p.X*(1-s),last.Y*s+p.Y*(1-s)));
        }
        InvalidateVisual();
    }
    private void OnMouseUp(object sender, MouseButtonEventArgs e)
    {
        if(_erasing) { e.Handled=true; CompleteGesture(); return; }
        if (!_drawing) return; var end = e.GetPosition(this); var shift = Keyboard.Modifiers.HasFlag(ModifierKeys.Shift); var annotation = Configure(_factory.Get(SelectedTool).Create(ToPointD(_start), ToPointD(end), _points, _settings.ForTool(SelectedTool), shift));
        if (Math.Abs(end.X - _start.X) > 2 || Math.Abs(end.Y - _start.Y) > 2 || _points.Count>1) { History.Add(annotation); ScheduleExpiration(annotation); }
        e.Handled=true; CompleteGesture();
    }
    private Annotation Configure(Annotation a) {
        var o=_settings.Options(a.Type);
        a=a with { Opacity=o.Opacity };
        if(a is ArrowAnnotation arrow)return arrow with { HeadSize=o.HeadSize,Curvature=o.Curvature };
        if(a is EllipseAnnotation e) return e with { Dashed=o.Dashed,Filled=o.Filled };
        if(a is RectangleAnnotation r) return r with { Rounded=o.Rounded,Dashed=o.Dashed,Filled=o.Filled };
        if(a is LineAnnotation line)return line with { Dashed=o.Dashed };
        return a;
    }
    private void DrawPreview(DrawingContext dc) { var current = Mouse.GetPosition(this); var annotation = Configure(_factory.Get(SelectedTool).Create(ToPointD(_start), ToPointD(current), _points, _settings.ForTool(SelectedTool), Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))); DrawAnnotation(dc, annotation); }
    private static bool Hit(Annotation a,Point p) {
        var pen=new Pen(Brushes.Black,a.Stroke+14);
        if(a is ArrowAnnotation { Style:ArrowStyle.Curved } arrow) { var g=CurvedArrow.Create(arrow); return g.FillContains(p)||g.StrokeContains(pen,p); }
        var start=new Point(a.Start.X,a.Start.Y); var end=new Point(a.End.X,a.End.Y);
        if(a is FreehandAnnotation free) { for(var i=1;i<free.Points.Count;i++)if(new LineGeometry(new Point(free.Points[i-1].X,free.Points[i-1].Y),new Point(free.Points[i].X,free.Points[i].Y)).StrokeContains(pen,p))return true; return false; }
        var rect=new Rect(start,end);
        Geometry geometry=a switch { EllipseAnnotation=>new EllipseGeometry(rect),RectangleAnnotation=>new RectangleGeometry(rect),_=>new LineGeometry(start,end) };
        return geometry.StrokeContains(pen,p) || (a is RectangleAnnotation { Filled:true } or EllipseAnnotation { Filled:true } && geometry.FillContains(p));
    }
    private readonly Dictionary<Guid,double> fades = [];
    private void ScheduleExpiration(Annotation annotation)
    {
        if (annotation.Duration == AnnotationDuration.Permanent) return;
        var timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds((int)annotation.Duration) };
        timer.Tick += (_, _) => {
            timer.Stop(); var started=DateTime.UtcNow;
            var fade=new DispatcherTimer { Interval=TimeSpan.FromMilliseconds(16) };
            fade.Tick += (_,_) => { var amount=1-(DateTime.UtcNow-started).TotalMilliseconds/280; if(amount<=0) { fade.Stop(); fades.Remove(annotation.Id); History.Expire(annotation.Id); } else fades[annotation.Id]=amount; InvalidateVisual(); };
            fade.Start();
        };
        timer.Start();
    }

    private void DrawAnnotation(DrawingContext dc, Annotation annotation)
    {
        if(annotation.Effect!=HakoMark.Core.VisualEffect.None) {
            if(annotation.Effect==HakoMark.Core.VisualEffect.SoftShadow) {
                dc.PushTransform(new TranslateTransform(2,3));
                DrawAnnotation(dc,annotation with { Effect=HakoMark.Core.VisualEffect.None, ColorHex="#000000", Opacity=annotation.Opacity*0.3, Stroke=annotation.Stroke+3 }); dc.Pop();
            } else {
                int layers=annotation.Effect==HakoMark.Core.VisualEffect.StrongGlow ? 5 : 3;
                for(int i=layers;i>0;i--) {
                    if(annotation is ArrowAnnotation { Style:ArrowStyle.Curved } curved) {
                        var glow=new SolidColorBrush((Color)ColorConverter.ConvertFromString(annotation.ColorHex)) { Opacity=annotation.Opacity*0.07*fades.GetValueOrDefault(annotation.Id,1) };
                        dc.DrawGeometry(null,new Pen(glow,i*4),CurvedArrow.Create(curved));
                    } else DrawAnnotation(dc,annotation with { Effect=HakoMark.Core.VisualEffect.None, Stroke=annotation.Stroke+i*3, Opacity=annotation.Opacity*0.07 });
                }
            }
        }
        dc.PushOpacity(fades.GetValueOrDefault(annotation.Id,1));
        var brush = new SolidColorBrush((Color)ColorConverter.ConvertFromString(annotation.ColorHex)) { Opacity = annotation.Opacity }; brush.Freeze();
        var pen = new Pen(brush, annotation.Stroke * (annotation is ArrowAnnotation { Style: ArrowStyle.Wide } ? 2 : 1)) { LineJoin = PenLineJoin.Round, StartLineCap = PenLineCap.Round, EndLineCap = PenLineCap.Round };
        if(annotation is EllipseAnnotation { Dashed:true } or RectangleAnnotation { Dashed:true } or LineAnnotation { Dashed:true }) pen.DashStyle=DashStyles.Dash; pen.Freeze();
        var start = new Point(annotation.Start.X, annotation.Start.Y); var end = new Point(annotation.End.X, annotation.End.Y);
        switch (annotation)
        {
            case ArrowAnnotation arrow:
                if(arrow.Style==ArrowStyle.Curved) { dc.DrawGeometry(brush,null,CurvedArrow.Create(arrow)); }
                else if(arrow.Style==ArrowStyle.Outline) {
                    var v=end-start; if(v.Length<1)break; v.Normalize(); var n=new Vector(-v.Y,v.X); double head=Math.Min((end-start).Length*0.5,arrow.HeadSize*1.5);
                    var g=new StreamGeometry(); using(var c=g.Open()) { c.BeginFigure(start+n*annotation.Stroke,false,true); c.PolyLineTo(new[] { end-v*head+n*annotation.Stroke,end-v*head+n*head*0.6,end,end-v*head-n*head*0.6,end-v*head-n*annotation.Stroke,start-n*annotation.Stroke },true,true); } dc.DrawGeometry(null,new Pen(brush,Math.Max(1,annotation.Stroke/2)),g);
                } else { dc.DrawLine(pen,start,end); DrawArrowHead(dc,pen,start,end,arrow.HeadSize,arrow.Style); if(arrow.Style==ArrowStyle.Double)DrawArrowHead(dc,pen,end,start,arrow.HeadSize,arrow.Style); }
                break;
            case EllipseAnnotation ellipse: dc.DrawEllipse(ellipse.Filled ? brush : null, pen, new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2), Math.Abs(end.X - start.X) / 2, Math.Abs(end.Y - start.Y) / 2); break;
            case RectangleAnnotation rectangle: var rect = new Rect(start, end); if (rectangle.Rounded) dc.DrawRoundedRectangle(rectangle.Filled ? brush : null, pen, rect, 12, 12); else dc.DrawRectangle(rectangle.Filled ? brush : null, pen, rect); break;
            case LineAnnotation: dc.DrawLine(pen, start, end); break;
            case FreehandAnnotation freehand when freehand.Points.Count > 1:
                var geometry = new StreamGeometry(); using (var context = geometry.Open()) { context.BeginFigure(new Point(freehand.Points[0].X, freehand.Points[0].Y), false, false); context.PolyLineTo(freehand.Points.Skip(1).Select(p => new Point(p.X, p.Y)).ToList(), true, true); } geometry.Freeze(); dc.DrawGeometry(null, pen, geometry); break;
        }
        dc.Pop();
    }

    private static void DrawArrowHead(DrawingContext dc, Pen pen, Point start, Point end, double size, ArrowStyle style)
    {
        if (style == ArrowStyle.Wide) size *= 1.35; var angle = Math.Atan2(end.Y - start.Y, end.X - start.X);
        var left = new Point(end.X - size * Math.Cos(angle - Math.PI / 6), end.Y - size * Math.Sin(angle - Math.PI / 6)); var right = new Point(end.X - size * Math.Cos(angle + Math.PI / 6), end.Y - size * Math.Sin(angle + Math.PI / 6));
        dc.DrawLine(pen, end, left); dc.DrawLine(pen, end, right);
    }
    private static PointD ToPointD(Point point) => new(point.X, point.Y);
}

internal static class NativeMethods
{
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern IntPtr GetForegroundWindow();
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern IntPtr GetWindow(IntPtr hwnd,uint command);
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern uint GetWindowThreadProcessId(IntPtr hwnd,out uint process);
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool IsWindowVisible(IntPtr hwnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);
    [System.Runtime.InteropServices.DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)] public static extern int GetWindowLong(IntPtr hWnd, int nIndex);
    [System.Runtime.InteropServices.DllImport("user32.dll", SetLastError = true)] public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);
}
