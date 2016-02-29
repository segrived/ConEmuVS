using System;
using System.Diagnostics;
using System.IO;

namespace ConEmuVS.Helpers
{
    public static class ExtensionHelpers
    {
        public static string GetExtenstionPath() {
            var instanceType = ConEmuToolWindowCommand.Instance.GetType();
            return Path.GetDirectoryName(instanceType.Assembly.Location);
        }

        public static void ExtractToDirectory(string archive, string outPath) {
            string pathTo7Zip = Path.Combine(GetExtenstionPath(), "Tools", "7za.exe");
            Directory.CreateDirectory(Path.GetDirectoryName(outPath));
            var proc = new ProcessStartInfo {
                FileName = pathTo7Zip,
                Arguments = $"x {archive} -y -o\"{outPath}\"",
                CreateNoWindow = true,
            };
            Process.Start(proc);
        }

        public static bool IsValidUrl(string url) {
            Uri uriResult;
            bool result = Uri.TryCreate(url, UriKind.Absolute, out uriResult)
                && uriResult.Scheme == Uri.UriSchemeHttp;
            return result;
        }
    }
}
