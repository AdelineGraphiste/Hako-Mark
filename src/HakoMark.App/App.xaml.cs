using System.Windows;
using System.Windows.Input;
using WinForms = System.Windows.Forms;
using HakoMark.Core;
using HakoMark.Drawing;
using HakoMark.Overlay;
using HakoMark.UI;
using HakoMark.Windows;

namespace HakoMark.App;

public partial class App : Application
{
    private readonly SettingsStore store = new();
    private HakoSettings settings = new();
    private OverlayWindow overlay = null!;
    private ToolbarWindow toolbar = null!;
    private HomeWindow home = null!;
    private GlobalHotkeyService hotkeys = null!;
    private WinForms.NotifyIcon tray = null!;
    private System.Threading.Mutex? instance;
    protected override void OnStartup(StartupEventArgs e)
    {
        instance = new System.Threading.Mutex(true,"Local\\HakoTools.HakoMark",out var first);
        if(!first) { GlobalHotkeyService.OpenExisting(); Shutdown(); return; }
        ShutdownMode=ShutdownMode.OnExplicitShutdown;
        base.OnStartup(e);
        settings=store.Load();
        overlay=new OverlayWindow(settings,new AnnotationHistory()); overlay.Show();
        toolbar=new ToolbarWindow(overlay,settings,store) { Owner=overlay };
        toolbar.Left=double.IsFinite(settings.ToolbarLeft) ? settings.ToolbarLeft : SystemParameters.WorkArea.Left+24;
        toolbar.Top=double.IsFinite(settings.ToolbarTop) ? settings.ToolbarTop : SystemParameters.WorkArea.Top+24;
        toolbar.Show();
        overlay.PreviewKeyDown += HandleKey;
        toolbar.PreviewKeyDown += HandleKey;
        hotkeys=new GlobalHotkeyService(); hotkeys.Attach(toolbar);
        hotkeys.ActivationPressed += (_,_) => {
            if(Windows.Cast<Window>().Any(w => w.IsVisible && w != home && w != toolbar && w != overlay)) return;
            home.WindowState=WindowState.Minimized; toolbar.Show(); overlay.AnnotationMode=!overlay.AnnotationMode;
        };
        home=new HomeWindow(() => { toolbar.Show(); overlay.AnnotationMode=true; },() => { overlay.AnnotationMode=false; new SettingsWindow(settings,store).ShowDialog(); },settings);
        MainWindow=home;
        toolbar.HomeRequested += OpenHome;
        toolbar.MinimizeRequested += () => { home.Show(); home.WindowState=WindowState.Minimized; };
        home.StateChanged += (_,_) => { if(home.WindowState==WindowState.Normal)toolbar.Show(); };
        home.Show();
        SettingsWindow.ApplyActivationKey=hotkeys.ConfigureActivation;
        if(!hotkeys.ConfigureActivation(settings.ActivationKey)) MessageBox.Show("La touche "+settings.ActivationKey+" est indisponible. Choisis une autre touche dans Réglages > Application. Ctrl + Maj + A reste le raccourci alternatif.","Hako Mark");
        hotkeys.OpenRequested += OpenHome;
        tray=new WinForms.NotifyIcon { Text="Hako Mark", Visible=true, Icon=System.Drawing.Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? System.Drawing.SystemIcons.Application };
        var menu=new WinForms.ContextMenuStrip();
        menu.Items.Add("Ouvrir Hako Mark",null,(_,_) => OpenHome());
        menu.Items.Add("Afficher la barre",null,(_,_) => toolbar.Show());
        menu.Items.Add("Masquer la barre",null,(_,_) => { overlay.AnnotationMode=false; toolbar.Hide(); });
        menu.Items.Add("Paramètres",null,(_,_) => { overlay.AnnotationMode=false; new SettingsWindow(settings,store).ShowDialog(); });
        menu.Items.Add("Aide",null,(_,_) => { overlay.AnnotationMode=false; new HelpWindow(settings).ShowDialog(); });
        menu.Items.Add("À propos",null,(_,_) => new AboutWindow().ShowDialog());
        menu.Items.Add("Quitter",null,(_,_) => Shutdown());
        tray.ContextMenuStrip=menu; tray.DoubleClick += (_,_) => OpenHome();
    }
    private void OpenHome() { overlay.AnnotationMode=false; home.Show(); home.WindowState=WindowState.Normal; home.Activate(); toolbar.Show(); }
    private void HandleKey(object sender,KeyEventArgs e)
    {
        if(!overlay.AnnotationMode)return;
        if(e.Key==Key.Escape || (Keyboard.Modifiers==ModifierKeys.None && e.Key.ToString()==settings.ExitKey)) overlay.AnnotationMode=false;
        else if(Keyboard.Modifiers==ModifierKeys.Control && e.Key==Key.Z) overlay.Undo();
        else if(Keyboard.Modifiers==ModifierKeys.Control && e.Key==Key.Y) overlay.Redo();
        else if(e.Key==Key.Delete) overlay.ClearAll();
        else if(Keyboard.Modifiers==ModifierKeys.None) {
            var tool=e.Key switch { Key.A=>ToolKind.Arrow,Key.C=>ToolKind.Ellipse,Key.R=>ToolKind.Rectangle,Key.L=>ToolKind.Line,Key.P=>ToolKind.Freehand,Key.G=>ToolKind.Eraser,_=>(ToolKind?)null };
            if(tool is null)return; settings.LastTool=tool.Value; overlay.SetTool(tool.Value); toolbar.UpdateMode();
        } else return;
        e.Handled=true;
    }
    protected override void OnExit(ExitEventArgs e)
    {
        tray?.Dispose(); hotkeys?.Dispose(); instance?.Dispose();
        if(toolbar is not null) { settings.ToolbarLeft=toolbar.Left; settings.ToolbarTop=toolbar.Top; store.Save(settings); }
        base.OnExit(e);
    }
}
