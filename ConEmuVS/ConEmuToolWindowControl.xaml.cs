using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows;
using ConEmuVS.Helpers;
using MessageBox = System.Windows.Forms.MessageBox;

namespace ConEmuVS
{
    public partial class ConEmuToolWindowControl
    {
        private Process _conEmuProcess;

        public ConEmuToolWindowControl() {
            this.InitializeComponent();

            var conEmuDirectory = ConEmuVSPackage.GeneralOptions.ConEmuInstallPath;
            if (String.IsNullOrEmpty(conEmuDirectory)) {
                return;
            }
            var x = Environment.CurrentDirectory;
            string conEmuExecutableName = Environment.Is64BitOperatingSystem ? "ConEmu64.exe" : "ConEmu.exe";
            string conEmuPath = Path.Combine(conEmuDirectory, conEmuExecutableName);
            if (! File.Exists(conEmuPath)) {
                return;
            }

            string conEmuConfig = Path.Combine(ConEmuVSPackage.ExtensionConfigPath, "config.xml");

            string sRunArgs =
                @" -InsideWnd 0x" + this.ConEmuHost.Handle.ToString("X") +
                @" -LoadCfgFile " + conEmuConfig + " -cmd {cmd}";

            try {
                this._conEmuProcess = Process.Start(conEmuPath, sRunArgs);
            } catch (Win32Exception) {
                MessageBox.Show("Can't initialize ConEmu, sorry");
            }
        }

        // temp version
        private async void DonwnloadAndInstallConEmu(object sender, RoutedEventArgs e) {
            var downloader = new ConEmuDownloader();
            try {
                await downloader.DownloadAndCopyConEmu();
                MessageBox.Show($"OK, ConEmu was downloaded and copied to ${ConEmuVSPackage.ExtensionConfigPath}");
            } catch(Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }
    }
}