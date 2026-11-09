namespace HakoMark.Core;

public enum ToolKind { Arrow, Ellipse, Rectangle, Line, Freehand, Eraser }
public enum ArrowStyle { Classic, Rounded, Wide, Outline, Double, Curved }
public enum VisualEffect { None, SoftShadow, LightGlow, StrongGlow }
public enum AnnotationDuration { Permanent = 0, TwoSeconds = 2, ThreeSeconds = 3, FiveSeconds = 5, TenSeconds = 10 }

public readonly record struct PointD(double X, double Y);

public abstract record Annotation
{
    public Guid Id { get; init; } = Guid.NewGuid();
    public required ToolKind Type { get; init; }
    public required PointD Start { get; init; }
    public required PointD End { get; init; }
    public List<PointD> Points { get; init; } = [];
    public string ColorHex { get; init; } = "#FFED04";
    public double Stroke { get; init; } = 5;
    public double Opacity { get; init; } = 1;
    public VisualEffect Effect { get; init; } = VisualEffect.None;
    public AnnotationDuration Duration { get; init; } = AnnotationDuration.Permanent;
    public DateTime CreatedAtUtc { get; init; } = DateTime.UtcNow;
}

public sealed record ArrowAnnotation : Annotation
{
    public double Curvature { get; init; } = -0.5;
    public ArrowStyle Style { get; init; } = ArrowStyle.Classic;
    public double HeadSize { get; init; } = 18;
}

public sealed record EllipseAnnotation : Annotation { public bool Dashed { get; init; } public bool Filled { get; init; } }
public sealed record RectangleAnnotation : Annotation { public bool Rounded { get; init; } public bool Filled { get; init; } public bool Dashed { get; init; } }
public sealed record LineAnnotation : Annotation { public bool Dashed { get; init; } }
public sealed record FreehandAnnotation : Annotation;

public sealed class HakoSettings
{
    public Dictionary<ToolKind, ToolOptions> Tools { get; set; } = [];
    public bool RightClickExits { get; set; } = true;
    public bool ReturnToNavigationAfterDrawing { get; set; } = true;
    public string ExitKey { get; set; } = "X";
    public string ActivationKey { get; set; } = "F8";
    public double ToolbarWidth { get; set; } = 770;
    public double ToolbarHeight { get; set; } = 230;
    public ToolOptions Options(ToolKind tool)
    {
        Tools ??= [];
        if(!Tools.TryGetValue(tool,out var options) || options is null)
            Tools[tool]=options=new ToolOptions { Stroke=Stroke, Opacity=Opacity, Rounded=Rounded, Dashed=Dashed, ArrowStyle=ArrowStyle, Effect=Effect, Duration=Duration };
        return options;
    }
    public HakoSettings ForTool(ToolKind tool)
    {
        var o=Options(tool);
        return new HakoSettings { LastColorHex=LastColorHex, Stroke=o.Stroke, Opacity=o.Opacity, Rounded=o.Rounded, Dashed=o.Dashed, ArrowStyle=o.ArrowStyle, Effect=o.Effect, Duration=o.Duration };
    }
    public double Opacity { get; set; } = 1;
    public bool Rounded { get; set; }
    public bool Dashed { get; set; }
    public string LastColorHex { get; set; } = "#FFED04";
    public List<string> RecentColors { get; set; } = ["#FFED04", "#FF0000", "#0066FF", "#00A83B", "#FFFFFF", "#000000"];
    public ToolKind LastTool { get; set; } = ToolKind.Arrow;
    public double Stroke { get; set; } = 5;
    public ArrowStyle ArrowStyle { get; set; } = ArrowStyle.Classic;
    public VisualEffect Effect { get; set; } = VisualEffect.None;
    public AnnotationDuration Duration { get; set; } = AnnotationDuration.Permanent;
    public double ToolbarLeft { get; set; } = double.NaN;
    public double ToolbarTop { get; set; } = double.NaN;
    public bool StartWithWindows { get; set; }
    public bool ShowToolbarAtStartup { get; set; } = true;
    public bool ShowWelcomeOnNextStart { get; set; } = true;
}

public sealed class ToolOptions
{
    public double Stroke { get; set; } = 5;
    public double Opacity { get; set; } = 1;
    public bool Rounded { get; set; }
    public bool Dashed { get; set; }
    public bool Filled { get; set; }
    public double Smoothing { get; set; } = 0.35;
    public double HeadSize { get; set; } = 24;
    public double Curvature { get; set; } = -0.5;
    public ArrowStyle ArrowStyle { get; set; } = ArrowStyle.Classic;
    public VisualEffect Effect { get; set; }
    public AnnotationDuration Duration { get; set; }
}
