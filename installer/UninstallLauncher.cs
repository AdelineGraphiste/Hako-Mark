using System;
using System.IO;
using System.Diagnostics;
using System.Windows.Forms;

internal static class UninstallLauncher
{
    [STAThread] static void Main()
    {
        Application.EnableVisualStyles();
        var uninstaller=Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),"HakoTools","HakoMark","Desinstaller.exe");
        if(!File.Exists(uninstaller)) { MessageBox.Show("Hako Mark n’est pas installé sur cet ordinateur.","Hako Mark"); return; }
        Process.Start(new ProcessStartInfo(uninstaller,"--uninstall") { UseShellExecute=true });
    }
}
