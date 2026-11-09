using HakoMark.Core;
using HakoMark.Drawing;
using System.IO;

Assert(System.Text.Json.JsonSerializer.Deserialize<HakoSettings>("{}")!.ActivationKey=="F8","Existing settings default to F8 activation");
Assert(System.Text.Json.JsonSerializer.Deserialize<HakoSettings>("{\"ActivationKey\":\"X\"}")!.ActivationKey=="X","Custom activation key is restored");

var settings = new HakoSettings(); var factory = new DrawingToolFactory(); var ellipse = factory.Get(ToolKind.Ellipse).Create(new(0, 0), new(100, 40), [], settings, true);
Assert(ellipse is EllipseAnnotation && ellipse.End.X - ellipse.Start.X == ellipse.End.Y - ellipse.Start.Y, "Shift constrains an ellipse to a circle");
var history = new AnnotationHistory(); for (var index = 0; index < 60; index++) history.Add(factory.Get(ToolKind.Arrow).Create(new(0, 0), new(index, index), [], settings, false));
Assert(history.Items.Count == 60, "Drawing more than 50 annotations must not delete visible marks");
history.Clear(); Assert(history.Items.Count==0,"Clear");
history.Undo(); Assert(history.Items.Count==60,"Undo restores all cleared marks");
history.Redo(); Assert(history.Items.Count==0,"Redo clears again");
var square=factory.Get(ToolKind.Rectangle).Create(new(0,0),new(40,0),[],settings,true);
Assert(square.End.X==square.End.Y,"Shift on horizontal drag creates a square");
Console.WriteLine("HakoMark.Tests: all checks passed.");

settings.Options(ToolKind.Arrow).Stroke=18;
settings.Options(ToolKind.Freehand).Stroke=2;
Assert(settings.ForTool(ToolKind.Arrow).Stroke==18 && settings.ForTool(ToolKind.Freehand).Stroke==2,"Per-tool widths remain independent");
settings.Options(ToolKind.Ellipse).Duration=AnnotationDuration.ThreeSeconds;
Assert(settings.ForTool(ToolKind.Line).Duration==AnnotationDuration.Permanent,"Duration stays independent by tool");
foreach(var end in new[] { new PointD(250,0),new PointD(-250,0),new PointD(0,250),new PointD(0,-250),new PointD(180,-140),new PointD(-180,140) }) {
    var a=new ArrowAnnotation { Type=ToolKind.Arrow,Start=new PointD(20,30),End=end,Style=ArrowStyle.Curved };
    var g=CurvedArrow.Create(a);
    var length=Math.Sqrt(Math.Pow(end.X-20,2)+Math.Pow(end.Y-30,2));
    var tip=g.Transform.Transform(new System.Windows.Point(length,0));
    Assert(Math.Abs(tip.X-end.X)<0.00001 && Math.Abs(tip.Y-end.Y)<0.00001,"Curved arrow tip follows the pointer in every direction");
    var inside=g.Transform.Transform(new System.Windows.Point(length-1,0));
    Assert(g.FillContains(inside),"Arrow tip has a filled triangular head");
}
Exception? renderError=null;
var render=new Thread(() => {
    try {
        var app=new System.Windows.Application();
        var shortcutWindow=new System.Windows.Window();
        var conflictWindow=new System.Windows.Window();
        using(var shortcuts=new HakoMark.Windows.GlobalHotkeyService())
        using(var conflict=new HakoMark.Windows.GlobalHotkeyService()) {
            shortcuts.Attach(shortcutWindow); conflict.Attach(conflictWindow);
            Assert(shortcuts.ConfigureActivation("F24"),"Register global activation");
            Assert(shortcuts.ConfigureActivation("F24"),"Same activation is idempotent");
            Assert(!conflict.ConfigureActivation("F24"),"Conflicting activation rejected");
            Assert(!shortcuts.ConfigureActivation("InvalidKey"),"Invalid activation rejected");
            Assert(!conflict.ConfigureActivation("F24"),"Previous key retained after failure");
            Assert(shortcuts.ConfigureActivation("F23"),"Change activation immediately");
            Assert(conflict.ConfigureActivation("F24"),"Previous activation released");
        }
        shortcutWindow.Close(); conflictWindow.Close();
        var xdoc=System.Xml.Linq.XDocument.Load("src/HakoMark.App/App.xaml");
        var ns=System.Xml.Linq.XNamespace.Get("http://schemas.microsoft.com/winfx/2006/xaml/presentation");
        var dict=new System.Xml.Linq.XElement(ns+"ResourceDictionary",new System.Xml.Linq.XAttribute(System.Xml.Linq.XNamespace.Xmlns+"x","http://schemas.microsoft.com/winfx/2006/xaml"),xdoc.Root!.Elements().First().Elements());
        app.Resources=(System.Windows.ResourceDictionary)System.Windows.Markup.XamlReader.Parse(dict.ToString());
        var visual=new System.Windows.Media.DrawingVisual();
        using(var dc=visual.RenderOpen()) {
            dc.DrawRectangle(System.Windows.Media.Brushes.White,null,new System.Windows.Rect(0,0,800,500));
            foreach(var (start,end,bend) in new[] { (new PointD(50,210),new PointD(350,100),-0.5),(new PointD(450,80),new PointD(720,210),0.5),(new PointD(350,300),new PointD(60,390),-0.5),(new PointD(680,450),new PointD(620,280),-0.5) })
                dc.DrawGeometry(System.Windows.Media.Brushes.Black,null,CurvedArrow.Create(new ArrowAnnotation { Type=ToolKind.Arrow,Start=start,End=end,Style=ArrowStyle.Curved,Stroke=8,HeadSize=55,Curvature=bend }));
        }
        var bitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(800,500,96,96,System.Windows.Media.PixelFormats.Pbgra32); bitmap.Render(visual);
        var png=new System.Windows.Media.Imaging.PngBitmapEncoder(); png.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(bitmap));
        Directory.CreateDirectory("release"); using(var stream=File.Create("release/curved-arrow-preview.png"))png.Save(stream);
        var referenceVisual=new System.Windows.Media.DrawingVisual();
        using(var dc=referenceVisual.RenderOpen()) {
            dc.DrawRectangle(System.Windows.Media.Brushes.White,null,new System.Windows.Rect(0,0,612,612));
            dc.DrawGeometry(new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromRgb(26,26,26)),null,CurvedArrow.Create(new ArrowAnnotation { Type=ToolKind.Arrow,Start=new PointD(162,482),End=new PointD(489,264),Style=ArrowStyle.Curved }));
        }
        var referenceBitmap=new System.Windows.Media.Imaging.RenderTargetBitmap(612,612,96,96,System.Windows.Media.PixelFormats.Pbgra32); referenceBitmap.Render(referenceVisual);
        var referencePng=new System.Windows.Media.Imaging.PngBitmapEncoder(); referencePng.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(referenceBitmap));
        using(var stream=File.Create("release/reference-arrow-preview.png"))referencePng.Save(stream);
        if(args.Length>0 && File.Exists(args[0])) {
            using var file=File.OpenRead(args[0]);
            var source=System.Windows.Media.Imaging.BitmapDecoder.Create(file,System.Windows.Media.Imaging.BitmapCreateOptions.PreservePixelFormat,System.Windows.Media.Imaging.BitmapCacheOption.OnLoad).Frames[0];
            var converted=new System.Windows.Media.Imaging.FormatConvertedBitmap(source,System.Windows.Media.PixelFormats.Bgra32,null,0);
            var actual=new byte[612*612*4]; var expected=new byte[actual.Length];
            referenceBitmap.CopyPixels(actual,612*4,0); converted.CopyPixels(expected,612*4,0);
            int intersection=0,union=0;
            for(int i=0;i<actual.Length;i+=4) { bool a=actual[i]<100; bool b=expected[i]<100; if(a||b)union++; if(a&&b)intersection++; }
            var match=(double)intersection/union;
            Console.WriteLine("Reference silhouette overlap: "+match.ToString("P2"));
            Assert(match>0.95,"Curved arrow must match the supplied silhouette");
        }
        var uiSettings=new HakoSettings();
        var overlay=new HakoMark.Overlay.OverlayWindow(uiSettings,new AnnotationHistory());
        var canvas=(HakoMark.Overlay.AnnotationCanvas)overlay.Content;
        var retained=factory.Get(ToolKind.Arrow).Create(new(0,0),new(100,100),[],settings,false);
        canvas.History.Add(retained);
        overlay.AnnotationMode=true;
        canvas.CompleteGesture();
        Assert(!overlay.AnnotationMode && !canvas.IsEnabled && !canvas.IsHitTestVisible,"A completed gesture releases annotation input");
        Assert(canvas.History.Items.Count==1,"Return to navigation retains the annotation");
        uiSettings.ReturnToNavigationAfterDrawing=false;
        overlay.AnnotationMode=true;
        canvas.CompleteGesture();
        Assert(overlay.AnnotationMode && canvas.IsEnabled,"Continuous mode stays active after a gesture");
        overlay.AnnotationMode=false;
        uiSettings.ReturnToNavigationAfterDrawing=true;
        Assert(System.Text.Json.JsonSerializer.Deserialize<HakoSettings>("{}")!.ReturnToNavigationAfterDrawing,"Existing settings without the new property default to automatic navigation");
        var toolbar=new HakoMark.UI.ToolbarWindow(overlay,uiSettings,new SettingsStore("release/test-settings.json"));
        var root=(System.Windows.FrameworkElement)toolbar.Content;
        foreach(var (width,height,name) in new[] { (770,260,"toolbar-wide"),(340,560,"toolbar-compact") }) {
            root.Measure(new System.Windows.Size(width,height)); root.Arrange(new System.Windows.Rect(0,0,width,height)); root.UpdateLayout();
            var shot=new System.Windows.Media.Imaging.RenderTargetBitmap(width,height,96,96,System.Windows.Media.PixelFormats.Pbgra32); shot.Render(root);
            var encoder=new System.Windows.Media.Imaging.PngBitmapEncoder(); encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(shot));
            using(var stream=File.Create("release/"+name+".png"))encoder.Save(stream);
        }
        toolbar.Close(); overlay.Close();
    } catch(Exception ex) { renderError=ex; }
});
render.SetApartmentState(ApartmentState.STA); render.Start(); render.Join();
if(renderError is not null)throw renderError;
Console.WriteLine("Per-tool preferences, curved-arrow directions and renderer: passed.");

static void Assert(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
