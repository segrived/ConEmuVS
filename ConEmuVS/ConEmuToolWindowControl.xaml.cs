using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Windows;

namespace ConEmuVS
{
    public partial class ConEmuToolWindowControl
    {
        private Process _conEmuProcess;
        private GuiMacro _guiMarco;
        

        private string GetConEmuExe() {
            bool bExeLoaded = false;
            string lsConEmuExe = null;

            while (!bExeLoaded && (this._conEmuProcess != null) && !this._conEmuProcess.HasExited) {
                try {
                    lsConEmuExe = this._conEmuProcess.Modules[0].FileName;
                    bExeLoaded = true;
                } catch (Win32Exception) {
                    Thread.Sleep(50);
                }
            }
            return lsConEmuExe;
        }

        private string GetConEmuCD() {
            string conEmuDirectory = ConEmuVSPackage.GeneralOptions.ConEmuInstallPath;
            string conEmuCd = Environment.Is64BitProcess ? "ConEmuCD64.dll" : "ConEmuCD.dll";
            return Path.Combine(conEmuDirectory, "ConEmu", conEmuCd);
        }

        private void ExecuteGuiMacro(string asMacro) {

            string conEmuCd = this.GetConEmuCD();
            if (conEmuCd == null) {
                throw new GuiMacroException("ConEmuCD must not be null");
            }
            if (this._guiMarco != null && this._guiMarco.LibraryPath != conEmuCd) {
                this._guiMarco = null;
            }

            try {
                if (this._guiMarco == null) {
                    this._guiMarco = new GuiMacro(conEmuCd);
                }
                this._guiMarco.Execute(this._conEmuProcess.Id.ToString(), asMacro, (code, data) => {
                    Debugger.Log(0, "GuiMacroResult", $"code={code}; data={data}\n");
                });
            } catch (GuiMacroException e) {
                MessageBox.Show(e.Message, "GuiMacroException", MessageBoxButton.OK);
            }
        }

        public ConEmuToolWindowControl() {
            this.InitializeComponent();
            this.StartConEmu();
        }

        private void StartConEmu() {
            string conEmuDirectory = ConEmuVSPackage.GeneralOptions.ConEmuInstallPath;
            if (String.IsNullOrEmpty(conEmuDirectory)) {
                return;
            }

            string conEmuExecutableName = Environment.Is64BitOperatingSystem ? "ConEmu64.exe" : "ConEmu.exe";
            string conEmuPath = Path.Combine(conEmuDirectory, conEmuExecutableName);
            if (!File.Exists(conEmuPath)) {
                return;
            }

            string conEmuConfig = Path.Combine(ConEmuVSPackage.ExtensionConfigPath, "config.xml");

            string shell = ConEmuVSPackage.GeneralOptions.ConEmuCommandLine;
            if (String.IsNullOrEmpty(shell)) {
                shell = "cmd.exe"; //default value
            }

            string sRunArgs =
                @" -InsideWnd 0x" + this.ConEmuHost.Handle.ToString("X") +
                @" -LoadCfgFile " + conEmuConfig + " -cmd " + shell;
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
                this.StartConEmu();
            } catch(Exception ex) {
                MessageBox.Show(ex.Message);
            }
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e) {
            this.ExecuteGuiMacro("Settings()");
        }
    }
}