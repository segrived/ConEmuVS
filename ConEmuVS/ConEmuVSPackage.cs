using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ConEmuVS
{
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideToolWindow(typeof(ConEmuToolWindow))]
    [ProvideOptionPage(typeof(ConEmuVsDialogPage), "ConEmuVS", "General", 0, 0, true)]
    public sealed class ConEmuVSPackage : Package
    {
        public const string PackageGuidString = "e15b3f57-3aea-4bf6-956d-cc993d21a21c";

        public static ConEmuVsDialogPage GeneralOptions;

        public static string ExtensionConfigPath => Path.Combine(Environment
            .GetFolderPath(Environment.SpecialFolder.ApplicationData), "ConEmuVS");

        public ConEmuVSPackage() {
        }

        #region Package Members
        protected override void Initialize() {
            base.Initialize();
            ConEmuToolWindowCommand.Initialize(this);

            GeneralOptions = (ConEmuVsDialogPage)this.GetDialogPage(typeof(ConEmuVsDialogPage));
        }
        #endregion
    }
}
