using System;
using System.IO;
using System.IO.Compression;
using System.Diagnostics;
using System.Reflection;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Win32;

internal static class Setup
{
    static readonly string Target=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HakoTools","HakoMark");
    static readonly string Desktop=Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
    const string RegistryPath=@"Software\Microsoft\Windows\CurrentVersion\Uninstall\HakoMark";
    [STAThread] static void Main(string[] args)
    {
        Application.EnableVisualStyles();
        if(args.Length>0 && args[0]=="--install") { try { Install(); Environment.ExitCode=0; } catch(Exception ex) { File.WriteAllText(Path.Combine(Path.GetTempPath(),"HakoMark-install-error.txt"),ex.ToString()); Environment.ExitCode=1; } return; }
        if(args.Length>0 && args[0]=="--uninstall") {
            if(MessageBox.Show("Désinstaller Hako Mark ? Tes préférences seront conservées.","Hako Mark",MessageBoxButtons.YesNo)!=DialogResult.Yes)return;
            string temp=Path.Combine(Path.GetTempPath(),"HakoMark-uninstall-"+Guid.NewGuid().ToString("N")+".exe");
            File.Copy(Application.ExecutablePath,temp);
            Process.Start(temp,"--remove"); return;
        }
        if(args.Length>0 && args[0]=="--remove") { try { System.Threading.Thread.Sleep(1200); StopApp(); RemoveShortcuts(); if(Directory.Exists(Target))Directory.Delete(Target,true); Registry.CurrentUser.DeleteSubKeyTree(RegistryPath,false); MessageBox.Show("Hako Mark est désinstallé.","Hako Mark"); } catch(Exception ex){ MessageBox.Show(ex.Message,"Désinstallation impossible"); } return; }
        var form=new Form { Text="Installer Hako Mark",ClientSize=new Size(510,300),StartPosition=FormStartPosition.CenterScreen,BackColor=Color.Black,ForeColor=Color.White,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false };
        var title=new Label { Text="HAKO MARK",Font=new Font("Segoe UI",26),ForeColor=Color.FromArgb(255,237,4),Location=new Point(28,24),Size=new Size(450,50) };
        var status=new Label { Text="Annote ton écran en quelques gestes.\n\nUn raccourci Hako Mark sera créé sur ton Bureau.\nAucun compte ni composant à installer séparément.",Location=new Point(30,90),Size=new Size(440,95),Font=new Font("Segoe UI",11) };
        var button=new Button { Text="INSTALLER HAKO MARK",Location=new Point(30,210),Size=new Size(440,48),BackColor=Color.FromArgb(255,237,4),ForeColor=Color.Black,FlatStyle=FlatStyle.Flat };
        bool installed=false;
        button.Click += async delegate {
            if(installed) { Process.Start(Path.Combine(Target,"HakoMark.exe")); form.Close(); return; }
            button.Enabled=false; status.Text="Installation en cours…";
            try {
                await Task.Run((Action)Install);
                installed=true; status.Text="Hako Mark est installé.\n\nSon icône est maintenant sur ton Bureau."; button.Text="OUVRIR HAKO MARK";
            } catch(Exception ex) { status.Text="Installation interrompue : "+ex.Message; button.Text="RÉESSAYER"; }
            button.Enabled=true;
        };
        form.Controls.AddRange(new Control[] { title,status,button }); Application.Run(form);
    }
    static void StopApp() {
        foreach(var p in Process.GetProcessesByName("HakoMark")) {
            try { if(string.Equals(p.MainModule.FileName,Path.Combine(Target,"HakoMark.exe"),StringComparison.OrdinalIgnoreCase)) { p.Kill(); p.WaitForExit(5000); } } finally { p.Dispose(); }
        }
    }
    static void Install() {
        StopApp(); Directory.CreateDirectory(Target);
        using(var stream=Assembly.GetExecutingAssembly().GetManifestResourceStream("payload.zip"))
        using(var zip=new ZipArchive(stream,ZipArchiveMode.Read)) {
            foreach(var entry in zip.Entries) {
                var dest=Path.GetFullPath(Path.Combine(Target,entry.FullName));
                if(!dest.StartsWith(Target+Path.DirectorySeparatorChar,StringComparison.OrdinalIgnoreCase))throw new IOException("Chemin du paquet invalide.");
                if(string.IsNullOrEmpty(entry.Name)) { Directory.CreateDirectory(dest); continue; }
                Directory.CreateDirectory(Path.GetDirectoryName(dest)); entry.ExtractToFile(dest,true);
            }
        }
        File.Copy(Application.ExecutablePath,Path.Combine(Target,"Desinstaller.exe"),true);
        dynamic shell=Activator.CreateInstance(Type.GetTypeFromProgID("WScript.Shell"));
        dynamic shortcut=shell.CreateShortcut(Path.Combine(Desktop,"Hako Mark.lnk"));
        shortcut.TargetPath=Path.Combine(Target,"HakoMark.exe"); shortcut.WorkingDirectory=Target; shortcut.IconLocation=Path.Combine(Target,"HakoMark.ico")+",0"; shortcut.Save();
        string old=Path.Combine(Desktop,"Desinstaller Hako Mark.lnk"); if(File.Exists(old))File.Delete(old);
        string menu=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),"Programs","HakoTools");
        Directory.CreateDirectory(menu);
        File.Copy(Path.Combine(Desktop,"Hako Mark.lnk"),Path.Combine(menu,"Hako Mark.lnk"),true);
        string oldMenu=Path.Combine(menu,"Desinstaller Hako Mark.lnk"); if(File.Exists(oldMenu))File.Delete(oldMenu);
        using(var key=Registry.CurrentUser.CreateSubKey(RegistryPath)) {
            key.SetValue("DisplayName","Hako Mark"); key.SetValue("Publisher","Adeline Courtois"); key.SetValue("DisplayVersion","0.3.3");
            key.SetValue("DisplayIcon",Path.Combine(Target,"HakoMark.ico"));
            key.SetValue("UninstallString","\""+Path.Combine(Target,"Desinstaller.exe")+"\" --uninstall");
            key.SetValue("NoModify",1); key.SetValue("NoRepair",1);
        }
    }
    static void RemoveShortcuts() {
        foreach(var file in new[] { Path.Combine(Desktop,"Hako Mark.lnk"),Path.Combine(Desktop,"Desinstaller Hako Mark.lnk"),Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.StartMenu),"Programs","HakoTools","Hako Mark.lnk") })
            if(File.Exists(file))File.Delete(file);
    }
}
