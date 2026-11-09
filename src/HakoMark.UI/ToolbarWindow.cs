using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using HakoMark.Core;
using HakoMark.Overlay;

namespace HakoMark.UI;

public static class Brand
{
    public static readonly FontFamily Font = new(new Uri("pack://application:,,,/HakoMark.UI;component/"), "./Fonts/#ABeeZee");
    public static readonly Brush Yellow = new SolidColorBrush(Color.FromRgb(255,237,4));
    public static TextBlock Text(string text, double size = 14) => new() { Text = text, FontSize = size, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0,0,0,12), Foreground = Brushes.White };
    public static Button Button(string title, Action action, bool primary = false)
    {
        var b = new Button { Content = title, Padding = new Thickness(14,10,14,10), Margin = new Thickness(4), Background = primary ? Yellow : Brushes.Black, Foreground = primary ? Brushes.Black : Brushes.White, BorderBrush = Brushes.White, BorderThickness = new Thickness(1), Cursor = Cursors.Hand };
        b.Click += (_,_) => action(); return b;
    }
    public static void Window(Window w, string title, double width = 560)
    {
        w.Title = title; w.Width = width; w.SizeToContent = SizeToContent.Height; w.MaxHeight = SystemParameters.WorkArea.Height - 60;
        w.Background = Brushes.Black; w.Foreground = Brushes.White; w.FontFamily = Font; w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
    }
}

public sealed class ToolbarWindow : Window
{
    private readonly OverlayWindow overlay;
    private readonly HakoSettings settings;
    private readonly SettingsStore store;
    private readonly Dictionary<ToolKind,Button> buttons = [];
    private readonly Button mode;
    private readonly Button eraser;
    public event Action? HomeRequested;
    public event Action? MinimizeRequested;
    public ToolbarWindow(OverlayWindow overlay, HakoSettings settings, SettingsStore store)
    {
        this.overlay=overlay; this.settings=settings; this.store=store;
        Title="Hako Mark — Barre d’annotation"; FontFamily=Brand.Font;
        WindowStyle=WindowStyle.None; ResizeMode=ResizeMode.CanResizeWithGrip;
        Background=Brushes.Black; Topmost=true; ShowInTaskbar=false;
        MinWidth=320; MinHeight=190; Width=settings.ToolbarWidth; Height=settings.ToolbarHeight;
        System.Windows.Shell.WindowChrome.SetWindowChrome(this,new System.Windows.Shell.WindowChrome { CaptionHeight=0,ResizeBorderThickness=new Thickness(7),CornerRadius=new CornerRadius(8),GlassFrameThickness=new Thickness(0) });
        var root=new DockPanel();
        var header=new DockPanel { Margin=new Thickness(8) }; DockPanel.SetDock(header,Dock.Top);
        var close=Brand.Button("×",() => { overlay.AnnotationMode=false; Hide(); }); close.ToolTip="Masquer la barre — retrouve-la depuis HOME ou Ctrl + Maj + A";
        DockPanel.SetDock(close,Dock.Right); header.Children.Add(close);
        var minimize=Brand.Button("—",() => { overlay.AnnotationMode=false; Hide(); MinimizeRequested?.Invoke(); });
        minimize.ToolTip="Réduire dans la barre des tâches"; DockPanel.SetDock(minimize,Dock.Right); header.Children.Add(minimize);
        var grip=new TextBlock { Text="⠿",FontSize=26,Foreground=Brushes.White,VerticalAlignment=VerticalAlignment.Center,Margin=new Thickness(8),Cursor=Cursors.SizeAll,ToolTip="Glisser pour déplacer · tirer les bords pour redimensionner" };
        grip.MouseLeftButtonDown += (_,_) => { DragMove(); SaveBounds(); }; DockPanel.SetDock(grip,Dock.Left); header.Children.Add(grip);
        var home=Brand.Button("HOME",() => { overlay.AnnotationMode=false; HomeRequested?.Invoke(); }); DockPanel.SetDock(home,Dock.Left); header.Children.Add(home);
        mode=Brand.Button("ANNOTER",() => overlay.AnnotationMode=!overlay.AnnotationMode,true);
        header.Children.Add(mode); root.Children.Add(header);
        var body=new StackPanel();
        var label=Brand.Text("DESSINER",11); label.Margin=new Thickness(14,4,4,2); body.Children.Add(label);
        var drawing=new WrapPanel { Margin=new Thickness(8,0,8,8) };
        foreach(var (kind,name,tip) in new[] { (ToolKind.Arrow,"↗ Flèche","Flèche — A"),(ToolKind.Ellipse,"○ Cercle","Cercle / ellipse — C · Maj : cercle parfait"),(ToolKind.Rectangle,"□ Cadre","Rectangle — R · Maj : carré"),(ToolKind.Line,"╱ Ligne","Ligne — L · Maj : angle régulier"),(ToolKind.Freehand,"✎ Stylo","Dessin et écriture à main levée — P") }) {
            var b=Brand.Button(name,() => Select(kind)); b.ToolTip=tip; b.Padding=new Thickness(10,8,10,8); buttons[kind]=b; drawing.Children.Add(b);
        }
        body.Children.Add(drawing);
        var actions=new StackPanel { Background=Brand.Yellow };
        var actionTitle=Brand.Text("CORRIGER ET RÉGLER",11); actionTitle.Foreground=Brushes.Black; actionTitle.Margin=new Thickness(14,8,4,2); actions.Children.Add(actionTitle);
        var commands=new WrapPanel { Margin=new Thickness(8,0,8,8) };
        Button Action(string text,Action run,string tip) { var b=Brand.Button(text,run,true); b.BorderBrush=Brushes.Black; b.Padding=new Thickness(10,8,10,8); b.ToolTip=tip; commands.Children.Add(b); return b; }
        eraser=Action("Gomme",() => Select(ToolKind.Eraser),"Clique sur une annotation pour la supprimer — G");
        Action("Couleurs",Colors,"Palette rapide et couleur personnalisée");
        Action("Annuler",overlay.Undo,"Annuler la dernière annotation — Ctrl + Z seulement en mode annotation");
        Action("Rétablir",overlay.Redo,"Rétablir — Ctrl + Y seulement en mode annotation");
        Action("Tout effacer",overlay.ClearAll,"Effacer toutes les annotations — annulable avec le bouton Annuler");
        Action("Réglages",() => { overlay.AnnotationMode=false; new SettingsWindow(settings,store).ShowDialog(); },"Personnaliser chaque outil et la sortie du mode annotation");
        Action("? Aide",() => { overlay.AnnotationMode=false; new HelpWindow(settings).ShowDialog(); },"Toutes les explications et raccourcis");
        actions.Children.Add(commands); body.Children.Add(actions);
        root.Children.Add(new ScrollViewer { Content=body,VerticalScrollBarVisibility=ScrollBarVisibility.Auto,HorizontalScrollBarVisibility=ScrollBarVisibility.Disabled });
        Content=new Border { Child=root,BorderBrush=Brand.Yellow,BorderThickness=new Thickness(1),CornerRadius=new CornerRadius(8) };
        SizeChanged += (_,_) => { settings.ToolbarWidth=Width; settings.ToolbarHeight=Height; };
        overlay.ModeChanged += UpdateMode; overlay.SetTool(settings.LastTool); UpdateMode();
        Activated += (_,_) => UpdateMode();
    }
    private void SaveBounds() { settings.ToolbarLeft=Left; settings.ToolbarTop=Top; store.Save(settings); }
    private void Select(ToolKind kind) { settings.LastTool=kind; overlay.SetTool(kind); overlay.AnnotationMode=true; UpdateMode(); }
    public void UpdateMode()
    {
        mode.Content=(overlay.AnnotationMode ? "TERMINER" : "ANNOTER")+" · "+settings.ActivationKey;
        mode.ToolTip=!overlay.AnnotationMode ? "Navigation libre : défile, clique ou change d’onglet. Clique sur un outil pour annoter à nouveau." : (settings.ReturnToNavigationAfterDrawing ? "Après ce tracé, retour automatique à la navigation. " : "Mode continu. ")+"Arrêter : Échap"+(settings.RightClickExits ? ", clic droit" : "")+", "+settings.ExitKey;
        foreach(var (kind,b) in buttons) { bool selected=kind==settings.LastTool; b.Background=selected ? Brand.Yellow : Brushes.Black; b.Foreground=selected ? Brushes.Black : Brushes.White; b.BorderThickness=new Thickness(selected ? 3 : 1); }
        eraser.Content=settings.LastTool==ToolKind.Eraser ? "✓ Gomme" : "Gomme";
        eraser.BorderThickness=new Thickness(settings.LastTool==ToolKind.Eraser ? 3 : 1);
    }
    private void Colors()
    {
        overlay.AnnotationMode=false;
        var menu=new ContextMenu();
        foreach(var color in new[] { "#FFED04","#FF0000","#0066FF","#00A83B","#FFFFFF","#000000" }.Concat(settings.RecentColors).Distinct()) {
            var item=new MenuItem { Header=color,Icon=new Border { Width=18,Height=18,Background=new SolidColorBrush((Color)ColorConverter.ConvertFromString(color)) } };
            item.Click += (_,_) => { settings.LastColorHex=color; store.Save(settings); }; menu.Items.Add(item);
        }
        var custom=new MenuItem { Header="+ Couleur personnalisée" }; custom.Click += (_,_) => new SettingsWindow(settings,store).ShowDialog(); menu.Items.Add(custom); menu.IsOpen=true;
    }
}

public sealed class HomeWindow : Window
{
    public HomeWindow(Action draw, Action settings,HakoSettings preferences)
    {
        Brand.Window(this,"Hako Mark",600);
        var p = new StackPanel { Margin = new Thickness(36) };
        var logo = Brand.Text("HAKO MARK",34); logo.Foreground = Brand.Yellow; p.Children.Add(logo);
        p.Children.Add(Brand.Text("Montre exactement ce que tu veux, directement sur ton écran.",22));
        p.Children.Add(Brand.Text("1. Choisis un outil, puis clique et glisse sur ton écran.\n2. Par défaut, relâche : tu peux aussitôt défiler ou changer d’onglet.\n3. Pour annoter à nouveau, clique sur un outil ou utilise Ctrl + Maj + A.\nLe mode continu reste disponible dans Réglages > Application."));
        p.Children.Add(Brand.Button("COMMENCER À ANNOTER  ↗", () => { WindowState=WindowState.Minimized; draw(); },true));
        p.Children.Add(Brand.Button("PERSONNALISER MES OUTILS",settings));
        p.Children.Add(Brand.Button("COMMENT UTILISER HAKO MARK",() => new HelpWindow(preferences).ShowDialog()));
        p.Children.Add(Brand.Button("À PROPOS",() => new AboutWindow().ShowDialog()));
        p.Children.Add(Brand.Button("QUITTER HAKO MARK",() => Application.Current.Shutdown()));
        var shortcutNote=Brand.Text("",12);
        void RefreshShortcut() => shortcutNote.Text=preferences.ActivationKey+" : reprendre le dernier outil ou arrêter · Ctrl + Maj + A : alternative · Échap : retour au bureau";
        RefreshShortcut(); Activated += (_,_) => RefreshShortcut(); p.Children.Add(shortcutNote);
        Content=new ScrollViewer { Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
        Closing += (_,e) => { e.Cancel=true; WindowState=WindowState.Minimized; };
    }
}

public sealed class AboutWindow : Window
{
    public AboutWindow()
    {
        Brand.Window(this,"À propos — Hako Mark");
        var p = new StackPanel { Margin=new Thickness(28) };
        p.Children.Add(Brand.Text("HAKO MARK · HakoTools",26));
        p.Children.Add(Brand.Text("Outil d’annotation d’écran pour présentations, démonstrations, visios et tutoriels."));
        p.Children.Add(Brand.Text("Créé par Adeline Courtois — Graphiste"));
        p.Children.Add(Brand.Button("adelinecourtois.fr",() => System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("https://adelinecourtois.fr") { UseShellExecute=true })));
        p.Children.Add(Brand.Text("Version 0.3.3")); p.Children.Add(Brand.Button("Fermer",Close)); Content=p;
    }
}

public sealed class HelpWindow : Window
{
    public HelpWindow(HakoSettings? settings=null)
    {
        Brand.Window(this,"Comment utiliser Hako Mark");
        var p = new StackPanel { Margin=new Thickness(28) };
        p.Children.Add(Brand.Text("COMMENT UTILISER HAKO MARK",24));
        p.Children.Add(Brand.Text("ACTIVER AU CLAVIER — "+(settings?.ActivationKey ?? "F8")+"\nAppuie sur cette touche depuis ton navigateur ou ton logiciel pour reprendre le dernier outil choisi. Appuie encore pour arrêter. Après un tracé, tu peux naviguer immédiatement et réactiver sans attendre que le dessin disparaisse. Personnalise la touche dans Réglages > Application. Une lettre comme X est réservée à Hako Mark tant qu’il tourne, même barre masquée ; préfère F8 pour ne pas gêner la saisie. L’activation est suspendue pendant les fenêtres de réglages et d’aide. Ctrl + Maj + A reste aussi disponible."));
        p.Children.Add(Brand.Text("1. Active Hako Mark avec un outil de la barre ou Ctrl + Maj + A.\n\n2. Choisis Flèche, Cercle, Cadre, Ligne ou Stylo.\n\n3. Clique, maintiens et déplace la souris sur ton écran.\n\n4. Relâche : par défaut, la navigation redevient immédiatement disponible.\n\n5. Pour annuler après le tracé, utilise Annuler dans la barre. Ctrl + Z / Ctrl + Y agissent sur Hako Mark uniquement lorsque le mode annotation est actif."));
        p.Children.Add(Brand.Text("ANNOTER PUIS NAVIGUER — ACTIVÉ PAR DÉFAUT\nUne annotation, puis retour automatique à la navigation : après chaque glissement (ou clic de gomme), tu peux défiler, changer d’onglet, cliquer et utiliser ton logiciel. Aucun Échap n’est nécessaire. Pour refaire une flèche, sélectionne Flèche dans la barre ou réactive avec Ctrl + Maj + A.\n\nLe retour à la navigation ne supprime pas le dessin : il reste jusqu’à la fin de sa durée, ou jusqu’à son effacement s’il est permanent. Les annotations sont fixées à l’écran : elles ne suivent pas les mots quand la page défile. Utilise une courte durée ou Tout effacer avant de changer de page."));
        p.Children.Add(Brand.Text("MODE CONTINU\nDans Réglages > Application, décoche « Une annotation, puis retour automatique à la navigation ». Tu peux alors dessiner plusieurs éléments à la suite ; termine avec clic droit, Échap, ta touche choisie ou TERMINER pour pouvoir naviguer. Ce choix est mémorisé."));
        p.Children.Add(Brand.Text("A : flèche · C : cercle · R : cadre · L : ligne · P : stylo · G : gomme\nMaj : cercle, carré ou angle régulier."));
        p.Children.Add(Brand.Text("LES DEUX ZONES DE LA BARRE\nDessiner (noir) : Flèche, Cercle, Cadre, Ligne et Stylo.\nCorriger et régler (jaune) : Gomme, Couleurs, Annuler, Rétablir, Tout effacer, Réglages et Aide."));
        p.Children.Add(Brand.Text("GOMME ET HISTORIQUE\nSélectionne Gomme puis clique sur le trait à supprimer. Elle ne dessine rien. Annuler restaure aussi une suppression ou un effacement complet. Rétablir refait l’action. Les boutons fonctionnent en navigation ; les raccourcis Ctrl + Z / Ctrl + Y concernent Hako Mark uniquement en mode annotation."));
        p.Children.Add(Brand.Text("COULEURS ET RÉGLAGES\nCouleurs ouvre la palette. Réglages contient un onglet pour chaque outil : épaisseur, opacité, durée et effet. Les formes proposent le contour pointillé et le remplissage ; le cadre peut avoir des coins arrondis. Le stylo propose un lissage."));
        p.Children.Add(Brand.Text("FLÈCHE COURBÉE PLEINE\nDans Réglages > Flèche, choisis Courbée pleine. La silhouette reproduit le modèle avec ses proportions conservées. Clique au départ de la queue, maintiens et déplace la souris : la pointe suit le curseur, et la flèche tourne et grandit avec le mouvement. Relâche pour la poser."));
        p.Children.Add(Brand.Text("TERMINER SANS ÉCHAP\n"+(settings?.RightClickExits!=false ? "Le clic droit termine l’annotation. " : "Le clic droit est désactivé dans tes réglages. ")+"Touche supplémentaire : "+(settings?.ExitKey ?? "X")+". Choisis-la dans Réglages > Application. Le bouton TERMINER fonctionne aussi. Échap reste toujours disponible."));
        p.Children.Add(Brand.Text("PERMANENT OU TEMPORAIRE\nChaque outil a sa propre durée : ∞, 2, 3, 5 ou 10 secondes. En temporaire, le tracé disparaît avec un fondu. En permanent, il reste jusqu’à une suppression. Les modifications concernent les prochains tracés."));
        p.Children.Add(Brand.Text("HOME, RÉDUCTION ET DIMENSIONS\nHOME ouvre l’accueil. Le bouton — réduit Hako Mark dans la barre des tâches ; clique sur son icône pour le restaurer et retrouver la barre. La croix masque la barre sans quitter l’application. Retrouve-la aussi avec Ctrl + Maj + A. Déplace-la par la poignée à points ; tire ses bords ou ses coins pour changer ses dimensions."));
        p.Children.Add(Brand.Text("BARRE DES TÂCHES ET DÉSINSTALLATION\nL’accueil reste disponible dans la barre des tâches. Clic droit sur son icône > Épingler à la barre des tâches.\nLe dossier Hako Mark Installer du Bureau contient Installer et Désinstaller. La désinstallation est aussi disponible dans les paramètres Windows. Tes préférences sont conservées."));
        p.Children.Add(Brand.Button("Fermer",Close)); Content=new ScrollViewer { Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto };
    }
}

public sealed class SettingsWindow : Window
{
    public static Func<string,bool>? ApplyActivationKey { get; set; }
    public SettingsWindow(HakoSettings settings,SettingsStore store)
    {
        Brand.Window(this,"Réglages — Hako Mark",680); Height=690; SizeToContent=SizeToContent.Manual;
        var draft=System.Text.Json.JsonSerializer.Deserialize<HakoSettings>(System.Text.Json.JsonSerializer.Serialize(settings,new System.Text.Json.JsonSerializerOptions { NumberHandling=System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals }),new System.Text.Json.JsonSerializerOptions { NumberHandling=System.Text.Json.Serialization.JsonNumberHandling.AllowNamedFloatingPointLiterals })!;
        var root=new DockPanel { Margin=new Thickness(20) };
        var title=Brand.Text("UN RÉGLAGE POUR CHAQUE OUTIL",23); DockPanel.SetDock(title,Dock.Top); root.Children.Add(title);
        var note=Brand.Text("Les réglages s’appliquent aux prochains tracés. La taille des formes se définit en glissant la souris.",12); DockPanel.SetDock(note,Dock.Top); root.Children.Add(note);
        var tabs=new TabControl { Background=Brushes.Black,BorderBrush=Brand.Yellow };
        var commits=new List<Action>();
        var save=Brand.Button("ENREGISTRER",() => {
            foreach(var commit in commits)commit();
            if(ApplyActivationKey is not null && !ApplyActivationKey(draft.ActivationKey)) { MessageBox.Show("Cette touche est déjà utilisée ou indisponible. Choisis une autre touche.","Raccourci indisponible"); return; }
            settings.ActivationKey=draft.ActivationKey;
            settings.Tools=draft.Tools; settings.ReturnToNavigationAfterDrawing=draft.ReturnToNavigationAfterDrawing; settings.RightClickExits=draft.RightClickExits; settings.ExitKey=draft.ExitKey; settings.LastColorHex=draft.LastColorHex; settings.RecentColors=draft.RecentColors; store.Save(settings); Close();
        },true);
        DockPanel.SetDock(save,Dock.Bottom); root.Children.Add(save);
        StackPanel Tab(string label) {
            var p=new StackPanel { Margin=new Thickness(16) };
            tabs.Items.Add(new TabItem { Header=label,Foreground=Brushes.Black,Content=new ScrollViewer { Content=p,VerticalScrollBarVisibility=ScrollBarVisibility.Auto } }); return p;
        }
        Slider Slider(StackPanel p,string label,double min,double max,double value,string format="0") {
            var t=Brand.Text(label+" : "+value.ToString(format),13); p.Children.Add(t);
            var s=new Slider { Minimum=min,Maximum=max,Value=value,Margin=new Thickness(2,0,2,16) }; s.ValueChanged += (_,_) => t.Text=label+" : "+s.Value.ToString(format); p.Children.Add(s); return s;
        }
        ComboBox Choice(StackPanel p,string label,string[] names,int selected) {
            p.Children.Add(Brand.Text(label,13)); var c=new ComboBox { ItemsSource=names,SelectedIndex=Math.Max(0,selected),Padding=new Thickness(8),Margin=new Thickness(0,0,0,16),Background=Brushes.White,Foreground=Brushes.Black }; p.Children.Add(c); return c;
        }
        CheckBox Check(StackPanel p,string label,bool value) {
            var c=new CheckBox { Content=label,IsChecked=value,Foreground=Brushes.White,Margin=new Thickness(0,6,0,10) }; p.Children.Add(c); return c;
        }
        foreach(var (tool,name) in new[] { (ToolKind.Arrow,"Flèche"),(ToolKind.Ellipse,"Cercle"),(ToolKind.Rectangle,"Cadre"),(ToolKind.Line,"Ligne"),(ToolKind.Freehand,"Stylo") }) {
            var p=Tab(name); var o=draft.Options(tool);
            var stroke=Slider(p,"Épaisseur",1,40,o.Stroke);
            var opacity=Slider(p,"Opacité",0.1,1,o.Opacity,"0%");
            var durations=new[] { 0,2,3,5,10 };
            var duration=Choice(p,"Durée",["∞ Permanent","2 secondes","3 secondes","5 secondes","10 secondes"],Array.IndexOf(durations,(int)o.Duration));
            var effect=Choice(p,"Effet",["Aucun","Ombre douce","Glow léger","Glow renforcé"],(int)o.Effect);
            commits.Add(() => { o.Stroke=stroke.Value; o.Opacity=opacity.Value; o.Duration=(AnnotationDuration)durations[duration.SelectedIndex]; o.Effect=(VisualEffect)effect.SelectedIndex; });
            if(tool==ToolKind.Arrow) {
                var style=Choice(p,"Style",["Classique","Arrondie","Large","Contour","Double","Courbée pleine"],(int)o.ArrowStyle);
                var head=Slider(p,"Taille de la pointe",8,100,o.HeadSize);
                p.Children.Add(Brand.Text("Courbée pleine : silhouette fidèle au modèle, avec ses proportions conservées. Clique à la queue puis glisse jusqu’au point à montrer. Le mouvement règle sa taille et son orientation. L’épaisseur et la pointe restent proportionnelles pour préserver cette forme.",12));
                var preview=new System.Windows.Shapes.Path { Fill=Brand.Yellow,Width=280,Height=150,Stretch=Stretch.Uniform,Margin=new Thickness(8),ToolTip="Aperçu de la flèche courbée" }; p.Children.Add(preview);
                void Refresh() { var curved=style.SelectedIndex==(int)ArrowStyle.Curved; head.IsEnabled=!curved; stroke.IsEnabled=!curved; preview.Data=HakoMark.Drawing.CurvedArrow.Create(new ArrowAnnotation { Type=ToolKind.Arrow,Start=new PointD(162,482),End=new PointD(489,264),Style=ArrowStyle.Curved }); }
                style.SelectionChanged += (_,_) => Refresh();
                head.ValueChanged += (_,_) => Refresh(); stroke.ValueChanged += (_,_) => Refresh(); Refresh();
                commits.Add(() => { o.ArrowStyle=(ArrowStyle)style.SelectedIndex; o.HeadSize=head.Value; });
            }
            if(tool is ToolKind.Rectangle or ToolKind.Ellipse or ToolKind.Line) {
                var dashed=Check(p,"Contour pointillé",o.Dashed); commits.Add(() => o.Dashed=dashed.IsChecked==true);
            }
            if(tool is ToolKind.Rectangle or ToolKind.Ellipse) {
                var fill=Check(p,"Forme remplie",o.Filled); commits.Add(() => o.Filled=fill.IsChecked==true);
                p.Children.Add(Brand.Text("Un petit glissement crée une petite forme ; un grand glissement crée une grande forme. Maintiens Maj pour un carré ou un cercle parfait.",12));
            }
            if(tool==ToolKind.Rectangle) { var round=Check(p,"Coins arrondis",o.Rounded); commits.Add(() => o.Rounded=round.IsChecked==true); }
            if(tool==ToolKind.Line)p.Children.Add(Brand.Text("Dessine dans n’importe quelle direction. Maj contraint à l’horizontale, à la verticale ou à 45°.",12));
            if(tool==ToolKind.Freehand) {
                var smoothing=Slider(p,"Lissage",0,0.85,o.Smoothing,"0%"); commits.Add(() => o.Smoothing=smoothing.Value);
                p.Children.Add(Brand.Text("Écris à main levée, dans l’orientation de ton choix. Augmente l’épaisseur pour un trait gras ; diminue-la pour écrire fin. Le stylo ne saisit pas de texte typographique.",12));
            }
        }
        var general=Tab("Application");
        general.Children.Add(Brand.Text("ACTIVER OU ARRÊTER AU CLAVIER",18));
        var activationKeys=new[] { "F8","X","F2","F3","F4","F6","F7","F9","F10","F11","Q","S","D","F","V","B","N","M" };
        var activation=Choice(general,"Touche globale : reprendre le dernier outil / terminer",activationKeys,Array.IndexOf(activationKeys,draft.ActivationKey));
        general.Children.Add(Brand.Text("F8 par défaut. Un appui active le dernier outil ; un autre arrête. Après un tracé, appuie à nouveau pour recommencer, sans attendre sa disparition. Le changement est immédiat après Enregistrer. Attention : X ou une autre lettre sera réservée à Hako Mark dans toutes les applications tant qu’il tourne, même barre masquée. Choisis F8 pour écrire normalement. Ctrl + Maj + A reste disponible.",12));
        commits.Add(() => draft.ActivationKey=activationKeys[activation.SelectedIndex]);
        general.Children.Add(Brand.Text("ANNOTER PUIS NAVIGUER",18));
        var single=Check(general,"Une annotation, puis retour automatique à la navigation",draft.ReturnToNavigationAfterDrawing);
        general.Children.Add(Brand.Text("Activé par défaut : relâche après ton tracé pour défiler ou changer d’onglet. Clique ensuite sur un outil pour annoter à nouveau. Décoche pour dessiner plusieurs éléments en continu. La durée des annotations ne change pas.",12));
        commits.Add(() => draft.ReturnToNavigationAfterDrawing=single.IsChecked==true);
        general.Children.Add(Brand.Text("SORTIR DU MODE ANNOTATION",18));
        var right=Check(general,"Clic droit pour terminer",draft.RightClickExits);
        var keys=new[] { "X","Q","S","D","F","V","B","N","M","Space","Enter","F2","F3","F4","F6","F7","F8","F9","F10","F11","F12" };
        var exit=Choice(general,"Touche supplémentaire pour terminer",keys,Array.IndexOf(keys,draft.ExitKey));
        general.Children.Add(Brand.Text("Échap reste toujours disponible. Tu peux aussi cliquer sur TERMINER. Ces raccourcis n’agissent que pendant l’annotation.",12));
        commits.Add(() => { draft.RightClickExits=right.IsChecked==true; draft.ExitKey=keys[exit.SelectedIndex]; });
        general.Children.Add(Brand.Text("COULEUR DES ANNOTATIONS",18));
        var hex=new TextBox { Text=draft.LastColorHex,Padding=new Thickness(10),Margin=new Thickness(0,0,0,8) }; general.Children.Add(hex);
        general.Children.Add(Brand.Button("Choisir une couleur",() => { using var dialog=new System.Windows.Forms.ColorDialog { FullOpen=true }; if(dialog.ShowDialog()==System.Windows.Forms.DialogResult.OK)hex.Text=$"#{dialog.Color.R:X2}{dialog.Color.G:X2}{dialog.Color.B:X2}"; }));
        hex.TextChanged += (_,_) => save.IsEnabled=System.Text.RegularExpressions.Regex.IsMatch(hex.Text,"^#[0-9a-fA-F]{6}$");
        commits.Add(() => { draft.LastColorHex=hex.Text; draft.RecentColors=draft.RecentColors.Prepend(hex.Text).Distinct().Take(8).ToList(); });
        general.Children.Add(Brand.Text("BARRE ET BARRE DES TÂCHES",18));
        general.Children.Add(Brand.Text("HOME ouvre l’accueil. La croix masque la barre et rend la main au bureau. Pour la retrouver : accueil, icône de notification ou Ctrl + Maj + A.\n\nPour épingler Hako Mark : clic droit sur son icône dans la barre des tâches, puis « Épingler à la barre des tâches ».\n\nTire un bord de la barre pour la rendre large, compacte ou verticale. Les boutons passent automatiquement à la ligne.",12));
        tabs.SelectedIndex=Math.Min((int)settings.LastTool,4);
        root.Children.Add(tabs); Content=root;
    }
}
