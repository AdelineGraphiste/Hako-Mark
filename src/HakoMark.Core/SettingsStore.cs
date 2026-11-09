using System.Text.Json;
using System.Text.Json.Serialization;

namespace HakoMark.Core;

public sealed class SettingsStore
{
    private readonly string _path;
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true, NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals };

    public SettingsStore(string? path = null)
    {
        _path = path ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "HakoTools", "HakoMark.Preferences", "settings.json");
    }

    public HakoSettings Load()
    {
        try
        {
            if (!File.Exists(_path)) return new HakoSettings();
            var settings = JsonSerializer.Deserialize<HakoSettings>(File.ReadAllText(_path), JsonOptions);
            settings ??= new HakoSettings();
            if(!System.Text.RegularExpressions.Regex.IsMatch(settings.LastColorHex ?? "", "^#[0-9a-fA-F]{6}$"))settings.LastColorHex="#FFED04";
            settings.Stroke=double.IsFinite(settings.Stroke) ? Math.Clamp(settings.Stroke,1,30) : 5;
            settings.Opacity=double.IsFinite(settings.Opacity) ? Math.Clamp(settings.Opacity,0.1,1) : 1;
            settings.RecentColors=(settings.RecentColors ?? []).Where(c => c is not null && System.Text.RegularExpressions.Regex.IsMatch(c,"^#[0-9a-fA-F]{6}$")).Take(8).ToList();
            if(!Enum.IsDefined(settings.ArrowStyle))settings.ArrowStyle=ArrowStyle.Classic;
            if(!Enum.IsDefined(settings.Duration))settings.Duration=AnnotationDuration.Permanent;
            settings.ToolbarWidth=double.IsFinite(settings.ToolbarWidth) ? Math.Clamp(settings.ToolbarWidth,320,1800) : 770;
            settings.ToolbarHeight=double.IsFinite(settings.ToolbarHeight) ? Math.Clamp(settings.ToolbarHeight,190,1000) : 230;
            if(string.IsNullOrWhiteSpace(settings.ExitKey))settings.ExitKey="X";
            foreach(var tool in Enum.GetValues<ToolKind>()) {
                var o=settings.Options(tool);
                o.Stroke=double.IsFinite(o.Stroke) ? Math.Clamp(o.Stroke,1,40) : 5;
                o.Opacity=double.IsFinite(o.Opacity) ? Math.Clamp(o.Opacity,0.1,1) : 1;
                o.Smoothing=double.IsFinite(o.Smoothing) ? Math.Clamp(o.Smoothing,0,0.85) : 0.35;
                o.Curvature=double.IsFinite(o.Curvature) ? Math.Clamp(o.Curvature,-0.9,0.9) : -0.5;
                o.HeadSize=double.IsFinite(o.HeadSize) ? Math.Clamp(o.HeadSize,8,100) : 24;
                if(!Enum.IsDefined(o.ArrowStyle))o.ArrowStyle=ArrowStyle.Classic;
                if(!Enum.IsDefined(o.Effect))o.Effect=VisualEffect.None;
                if(!Enum.IsDefined(o.Duration))o.Duration=AnnotationDuration.Permanent;
            }
            return settings;
        }
        catch (IOException) { return new HakoSettings(); }
        catch (JsonException) { return new HakoSettings(); }
        catch (UnauthorizedAccessException) { return new HakoSettings(); }
    }

    public void Save(HakoSettings settings)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);
        var temporary=_path+".tmp";
        File.WriteAllText(temporary, JsonSerializer.Serialize(settings, JsonOptions));
        File.Move(temporary,_path,true);
    }
}
