using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace ConEmuVS
{
    internal sealed class ConEmuToolWindowCommand
    {
        public const int CommandId = 0x0100;

        public static readonly Guid CommandSet
            = new Guid("34b27755-410e-414d-8c3b-a62775d874cd");

        private readonly Package _package;

        private ConEmuToolWindowCommand(Package package) {
            if (package == null) {
                throw new ArgumentNullException(nameof(package));
            }

            this._package = package;
            var serviceObj = this.ServiceProvider.GetService(typeof(IMenuCommandService));
            var commandService = serviceObj as OleMenuCommandService;
            if (commandService == null) {
                return;
            }
            var menuCommandId = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(this.ShowToolWindow, menuCommandId);
            commandService.AddCommand(menuItem);
        }

        public static ConEmuToolWindowCommand Instance { get; private set; }

        private IServiceProvider ServiceProvider => this._package;

        public static void Initialize(Package package) {
            Instance = new ConEmuToolWindowCommand(package);
        }

        private void ShowToolWindow(object sender, EventArgs e) {
            var window = this._package.FindToolWindow(typeof(ConEmuToolWindow), 0, true);
            if (window?.Frame == null) {
                throw new NotSupportedException("Cannot create tool window");
            }
            var windowFrame = (IVsWindowFrame)window.Frame;
            Microsoft.VisualStudio.ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}