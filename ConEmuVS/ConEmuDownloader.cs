using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading.Tasks;
using ConEmuVS.Helpers;
using MadMilkman.Ini;

namespace ConEmuVS
{
    [Serializable]
    public class ConEmuInstallationException : Exception
    {
        public ConEmuInstallationException() {}
        public ConEmuInstallationException(string message) : base(message) {}
        public ConEmuInstallationException(string message, Exception inner) 
            : base(message, inner) {}

        protected ConEmuInstallationException(
            SerializationInfo info,
            StreamingContext context) : base(info, context) {}
    }

    public class ConEmuDownloader
    {
        public ConEmuDownloader() {
            this._parser = new IniFile();
        }

        private readonly IniFile _parser;

        private const string VersionInfoFile = "http://www.conemu.ru/version.ini";

        public async Task DownloadAndCopyConEmu() {
            var client = new WebClient();
            string versionFileInfoPath = Path.GetTempFileName();

            try {
                await client.DownloadFileTaskAsync(new Uri(VersionInfoFile), versionFileInfoPath);
            } catch (Exception ex) {
                throw new ConEmuInstallationException($"Version file cannot be downloaded: {ex.Message}");
            }

            this._parser.Load(versionFileInfoPath);
            string location = this._parser.Sections["ConEmu_Stable_2"].Keys["location_arc"].Value;
            string address = location.Split(',').Last();

            if (String.IsNullOrEmpty(address) || ExtensionHelpers.IsValidUrl(address)) {
                throw new ConEmuInstallationException("Invalid URL address, please install ConEmu manually");
            }

            string tempFile = Path.GetTempFileName();
            try {
                // TODO: check MD5 hash
                await client.DownloadFileTaskAsync(new Uri(address), tempFile);
            } catch {
                throw new ConEmuInstallationException("Cannot download ConEmu archive, please install ConEmu manually");
            }
            try {
                ExtensionHelpers.ExtractToDirectory(tempFile, ConEmuVSPackage.ExtensionConfigPath);
            } catch (Exception) {
                throw new ConEmuInstallationException("Cannot extract ConEmu archive, please install ConEmu manually");
            }
            ConEmuVSPackage.GeneralOptions.ConEmuInstallPath = ConEmuVSPackage.ExtensionConfigPath;
            ConEmuVSPackage.GeneralOptions.SaveSettingsToStorage();
        }
    }
}
