using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ConEmuVS
{
    public class GuiMacroException : Exception
    {
        public GuiMacroException(string asMessage)
            : base(asMessage) {}
    }

    public class GuiMacro
    {
        public enum GuiMacroResult
        {
            GmrOk = 0,
            GmrPending = 1,
            GmrDllNotLoaded = 2,
            GmrException = 3,
            GmrInvalidInstance = 4,
            GmrExecError = 5,
        };

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr LoadLibrary(string libname);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern bool FreeLibrary(IntPtr hModule);

        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)]
        private static extern IntPtr GetProcAddress(IntPtr hModule, string lpProcName);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate int FConsoleMain3(int anWorkMode, string asCommandLine);

        public delegate void ExecuteResult(GuiMacroResult code, string data);

        [UnmanagedFunctionPointer(CallingConvention.StdCall, CharSet = CharSet.Unicode)]
        private delegate int FGuiMacro(string asWhere, string asMacro, out IntPtr bstrResult);

        private IntPtr _conEmuCd;
        private FConsoleMain3 _fnConsoleMain3;
        private FGuiMacro _fnGuiMacro;

        public string LibraryPath { get; }

        private string ExecuteLegacy(string asWhere, string asMacro) {
            if (this._conEmuCd == IntPtr.Zero) {
                throw new GuiMacroException("ConEmuCD was not loaded");
            }
            if (this._fnConsoleMain3 == null) {
                throw new GuiMacroException("ConsoleMain3 function was not found");
            }

            string cmdLine = " -GuiMacro";
            if (!String.IsNullOrEmpty(asWhere)) {
                cmdLine += ":" + asWhere;
            }
            cmdLine += " " + asMacro;

            Environment.SetEnvironmentVariable("ConEmuMacroResult", null);

            string result;

            int iRc = this._fnConsoleMain3.Invoke(3, cmdLine);

            switch (iRc) {
                case 200: // CERR_CMDLINEEMPTY
                case 201: // CERR_CMDLINE
                    throw new GuiMacroException("Bad command line was passed to ConEmuCD");
                case 0: // This is expected
                case 133: // CERR_GUIMACRO_SUCCEEDED: not expected, but...
                    result = Environment.GetEnvironmentVariable("ConEmuMacroResult");
                    if (result == null) {
                        throw new GuiMacroException("ConEmuMacroResult was not set");
                    }
                    break;
                case 134: // CERR_GUIMACRO_FAILED
                    throw new GuiMacroException("GuiMacro execution failed");
                default:
                    throw new GuiMacroException($"Internal ConEmuCD error: {iRc}");
            }

            return result;
        }

        private void ExecuteHelper(string asWhere, string asMacro, ExecuteResult aCallbackResult) {
            if (aCallbackResult == null) {
                throw new GuiMacroException("aCallbackResult was not specified");
            }

            string result;
            GuiMacroResult code;

            if (this._fnGuiMacro != null) {
                IntPtr fnCallback = Marshal.GetFunctionPointerForDelegate(aCallbackResult);
                if (fnCallback == IntPtr.Zero) {
                    throw new GuiMacroException("GetFunctionPointerForDelegate failed");
                }

                IntPtr bstrPtr;
                int iRc = this._fnGuiMacro.Invoke(asWhere, asMacro, out bstrPtr);

                switch (iRc) {
                    case 0: // This is not expected for `GuiMacro` exported funciton, but JIC
                    case 133: // CERR_GUIMACRO_SUCCEEDED: expected
                        code = GuiMacroResult.GmrOk;
                        break;
                    case 134: // CERR_GUIMACRO_FAILED
                        code = GuiMacroResult.GmrExecError;
                        break;
                    default:
                        throw new GuiMacroException($"Internal ConEmuCD error: {iRc}");
                }

                if (bstrPtr == IntPtr.Zero) {
                    // Not expected, `GuiMacro` must return some BSTR in any case
                    throw new GuiMacroException("Empty bstrPtr was returned");
                }

                result = Marshal.PtrToStringBSTR(bstrPtr);
                Marshal.FreeBSTR(bstrPtr);

                if (result == null) {
                    // Not expected, `GuiMacro` must return some BSTR in any case
                    throw new GuiMacroException("Marshal.PtrToStringBSTR failed");
                }
            } else {
                result = this.ExecuteLegacy(asWhere, asMacro);
                code = GuiMacroResult.GmrOk;
            }

            aCallbackResult(code, result);
        }

        public GuiMacroResult Execute(string asWhere, string asMacro, ExecuteResult aCallbackResult) {
            if (this._conEmuCd == IntPtr.Zero) {
                return GuiMacroResult.GmrDllNotLoaded;
            }

            new Thread(() => {
                Thread.CurrentThread.IsBackground = true;
                try {
                    this.ExecuteHelper(asWhere, asMacro, aCallbackResult);
                } catch (GuiMacroException e) {
                    aCallbackResult(GuiMacroResult.GmrException, e.Message);
                }
            }).Start();

            return GuiMacroResult.GmrPending;
        }

        public GuiMacro(string asLibrary) {
            this._conEmuCd = IntPtr.Zero;
            this._fnConsoleMain3 = null;
            this._fnGuiMacro = null;
            this.LibraryPath = asLibrary;
            this.LoadConEmuDll(asLibrary);
        }

        ~GuiMacro() {
            this.UnloadConEmuDll();
        }

        private void LoadConEmuDll(string asLibrary) {
            if (this._conEmuCd != IntPtr.Zero) {
                return;
            }

            this._conEmuCd = LoadLibrary(asLibrary);
            if (this._conEmuCd == IntPtr.Zero) {
                int errorCode = Marshal.GetLastWin32Error();
                throw new GuiMacroException($"Can't load library, ErrCode={errorCode}\n{asLibrary}");
            }

            // int __stdcall ConsoleMain3(int anWorkMode/*0-Server&ComSpec,1-AltServer,2-Reserved*/, LPCWSTR asCmdLine)
            const string fnNameOld = "ConsoleMain3";
            IntPtr ptrConsoleMain = GetProcAddress(this._conEmuCd, fnNameOld);
            const string fnNameNew = "GuiMacro";
            IntPtr ptrGuiMacro = GetProcAddress(this._conEmuCd, fnNameNew);

            if ((ptrConsoleMain == IntPtr.Zero) && (ptrGuiMacro == IntPtr.Zero)) {
                this.UnloadConEmuDll();
                throw new GuiMacroException(
                    $"Function {fnNameOld} not found in library\n{asLibrary}\nUpdate ConEmu modules");
            }

            if (ptrGuiMacro != IntPtr.Zero) {
                // To call: ExecGuiMacro.Invoke(asWhere, asCommand, callbackDelegate);
                this._fnGuiMacro = (FGuiMacro)Marshal.GetDelegateForFunctionPointer(ptrGuiMacro, typeof(FGuiMacro));
            }
            if (ptrConsoleMain != IntPtr.Zero) {
                // To call: ConsoleMain3.Invoke(0, cmdline);
                this._fnConsoleMain3 =
                    (FConsoleMain3)Marshal.GetDelegateForFunctionPointer(ptrConsoleMain, typeof(FConsoleMain3));
            }
        }

        private void UnloadConEmuDll() {
            if (this._conEmuCd != IntPtr.Zero) {
                FreeLibrary(this._conEmuCd);
                this._conEmuCd = IntPtr.Zero;
            }
        }
    }
}