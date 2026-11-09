using HakoMark.Core;

namespace HakoMark.Drawing;

public interface IDrawingTool
{
    ToolKind Kind { get; }
    Annotation Create(PointD start, PointD end, IReadOnlyList<PointD> points, HakoSettings settings, bool shiftHeld);
}

public sealed class DrawingToolFactory
{
    private readonly IReadOnlyDictionary<ToolKind, IDrawingTool> _tools = new Dictionary<ToolKind, IDrawingTool>
    {
        [ToolKind.Arrow] = new ArrowTool(), [ToolKind.Ellipse] = new EllipseTool(), [ToolKind.Rectangle] = new RectangleTool(),
        [ToolKind.Line] = new LineTool(), [ToolKind.Freehand] = new FreehandTool()
    };

    public IDrawingTool Get(ToolKind kind) => _tools.TryGetValue(kind, out var tool) ? tool : _tools[ToolKind.Arrow];
}

internal abstract class ShapeTool
{
    protected static (PointD Start, PointD End) Normalize(PointD start, PointD end) =>
        (new(Math.Min(start.X, end.X), Math.Min(start.Y, end.Y)), new(Math.Max(start.X, end.X), Math.Max(start.Y, end.Y)));

    protected static PointD SnapSquare(PointD start, PointD end)
    {
        var size = Math.Max(Math.Abs(end.X - start.X), Math.Abs(end.Y - start.Y));
        return new(start.X + (end.X < start.X ? -1 : 1) * size, start.Y + (end.Y < start.Y ? -1 : 1) * size);
    }
}

internal sealed class ArrowTool : IDrawingTool
{
    public ToolKind Kind => ToolKind.Arrow;
    public Annotation Create(PointD start, PointD end, IReadOnlyList<PointD> points, HakoSettings settings, bool shiftHeld) => new ArrowAnnotation
    {
        Type = Kind, Start = start, End = end, ColorHex = settings.LastColorHex, Stroke = settings.Stroke, Effect = settings.Effect, Duration = settings.Duration, Style = settings.ArrowStyle
    };
}

internal sealed class EllipseTool : ShapeTool, IDrawingTool
{
    public ToolKind Kind => ToolKind.Ellipse;
    public Annotation Create(PointD start, PointD end, IReadOnlyList<PointD> points, HakoSettings settings, bool shiftHeld)
    {
        if (shiftHeld) end = SnapSquare(start, end); var shape = Normalize(start, end);
        return new EllipseAnnotation { Type = Kind, Start = shape.Start, End = shape.End, ColorHex = settings.LastColorHex, Stroke = settings.Stroke, Effect = settings.Effect, Duration = settings.Duration };
    }
}

internal sealed class RectangleTool : ShapeTool, IDrawingTool
{
    public ToolKind Kind => ToolKind.Rectangle;
    public Annotation Create(PointD start, PointD end, IReadOnlyList<PointD> points, HakoSettings settings, bool shiftHeld)
    {
        if (shiftHeld) end = SnapSquare(start, end); var shape = Normalize(start, end);
        return new RectangleAnnotation { Type = Kind, Start = shape.Start, End = shape.End, ColorHex = settings.LastColorHex, Stroke = settings.Stroke, Effect = settings.Effect, Duration = settings.Duration };
    }
}

internal sealed class LineTool : IDrawingTool
{
    public ToolKind Kind => ToolKind.Line;
    public Annotation Create(PointD start, PointD end, IReadOnlyList<PointD> points, HakoSettings settings, bool shiftHeld)
    {
        if (shiftHeld)
        {
            var dx = end.X - start.X; var dy = end.Y - start.Y; var angle = Math.Atan2(dy, dx);
            var snapped = Math.Round(angle / (Math.PI / 4)) * (Math.PI / 4); var length = Math.Sqrt(dx * dx + dy * dy);
            end = new(start.X + Math.Cos(snapped) * length, start.Y + Math.Sin(snapped) * length);
        }
        return new LineAnnotation { Type = Kind, Start = start, End = end, ColorHex = settings.LastColorHex, Stroke = settings.Stroke, Effect = settings.Effect, Duration = settings.Duration };
    }
}

internal sealed class FreehandTool : IDrawingTool
{
    public ToolKind Kind => ToolKind.Freehand;
    public Annotation Create(PointD start, PointD end, IReadOnlyList<PointD> points, HakoSettings settings, bool shiftHeld) => new FreehandAnnotation
    {
        Type = Kind, Start = start, End = end, Points = points.ToList(), ColorHex = settings.LastColorHex, Stroke = settings.Stroke, Effect = settings.Effect, Duration = settings.Duration
    };
}

public sealed class AnnotationHistory
{
    private readonly List<Annotation> _items = [];
    private readonly List<Annotation[]> undo = [];
    private readonly Stack<Annotation[]> redo = [];
    public IReadOnlyList<Annotation> Items => _items;
    private void Snapshot() { undo.Add(_items.ToArray()); if(undo.Count>100) undo.RemoveAt(0); redo.Clear(); }
    public void Add(Annotation annotation) { Snapshot(); _items.Add(annotation); }
    public void Remove(Annotation annotation) { if(!_items.Contains(annotation)) return; Snapshot(); _items.Remove(annotation); }
    public void Clear() { if(_items.Count==0)return; Snapshot(); _items.Clear(); }
    public void Undo() { if(undo.Count==0)return; redo.Push(_items.ToArray()); var previous=undo[^1]; undo.RemoveAt(undo.Count-1); _items.Clear(); _items.AddRange(previous); }
    public void Redo() { if(redo.Count==0)return; undo.Add(_items.ToArray()); _items.Clear(); _items.AddRange(redo.Pop()); }
    public void Expire(Guid id) { _items.RemoveAll(item => item.Id == id); }
}
