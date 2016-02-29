using System;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace ConEmuVS
{
    [Guid("bd360fa9-0b43-443d-9b40-e25a0f0d9615")]
    public sealed class ConEmuToolWindow : ToolWindowPane
    {
        public ConEmuToolWindow() : base(null) {
            this.Caption = "ConEmu";
            this.Content = new ConEmuToolWindowControl();
        }
    }
}