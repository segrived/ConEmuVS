using System;
using System.ComponentModel;
using Microsoft.VisualStudio.Shell;

namespace ConEmuVS
{
    [DesignerCategory("")]
    public class ConEmuVsDialogPage : DialogPage
    {
        private const string CategoryTitle = "General";

        [Category(CategoryTitle)]
        [DisplayName("ConEmu installation path")]
        [Description("")]
        public string ConEmuInstallPath { get; set; } = String.Empty;
    }
}
