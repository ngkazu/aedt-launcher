using System;
using System.Configuration;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using ICSharpCode.SharpZipLib.Tar;

namespace aedt_launcher
{
    internal sealed record DetectedFile(string FileName, string FileType, string FileVersion, string FilePath, bool IsExtracted)
    {
        public static DetectedFile None(string fileName) => new(fileName, "None", "None", "None", false);
    }

    class FileClass
    {
        private DetectedFile _detected = DetectedFile.None(string.Empty);

        public string FileName => _detected.FileName;

        public string FileType => _detected.FileType;

        public string FileVersion => _detected.FileVersion;

        public void ExtractSAversion()
        {
            string folder = Path.GetDirectoryName(_detected.FileName) ?? string.Empty;

            using (var tzrStream = File.OpenRead(_detected.FileName))
            using (var gzStream = new GZipStream(tzrStream, CompressionMode.Decompress))
            using (var tarStream = TarArchive.CreateInputTarArchive(gzStream, Encoding.UTF8))
            {
                tarStream.ExtractContents(folder);
            }
            _detected = _detected with { IsExtracted = true };
        }

        private static DetectedFile? CheckSIversion(string path)
        {
            foreach (var line in File.ReadLines(path, Encoding.UTF8))
            {
                if (Regex.IsMatch(line, "DB_VERSION"))
                {
                    return new DetectedFile(path, "SIwave", Regex.Replace(line, @"[^0-9.]", ""), path, false);
                }
            }
            return null;
        }

        private static DetectedFile? CheckAEDTversion(string path)
        {
            using var lines = File.ReadLines(path, Encoding.UTF8).GetEnumerator();
            while (lines.MoveNext())
            {
                if (Regex.IsMatch(lines.Current, "begin 'Desktop'") && lines.MoveNext())
                {
                    var versionText = Regex.Replace(lines.Current, @"[^0-9,]", "");
                    var version = Regex.Replace(versionText, @",", ".");
                    return new DetectedFile(path, "AEDT", version, path, false);
                }
            }
            return null;
        }

        internal static string? ParseIcepakVersion(string[] parts)
        {
            if (parts.Length < 3) return null;
            int index = parts[2] == "Icepak" ? 3 : 2;
            return parts.Length > index ? parts[index] : null;
        }

        private static DetectedFile? CheckSAdirectory(string path)
        {
            string modelPath = Path.Combine(path, "model");
            if (File.Exists(modelPath))
            {
                string? line = File.ReadLines(modelPath, Encoding.UTF8).FirstOrDefault();
                if (line == null) return null;

                return new DetectedFile(path, "ICEPAK", ParseIcepakVersion(line.Split(' ')) ?? "None", path, false);
            }
            return null;
        }

        private static DetectedFile? CheckSAversion(string path)
        {
            using (var tzrStream = File.OpenRead(path))
            using (var gzStream = new GZipStream(tzrStream, CompressionMode.Decompress))
            using (var tarStream = new TarInputStream(gzStream, Encoding.UTF8))
            {
                TarEntry? entry;
                while ((entry = tarStream.GetNextEntry()) != null)
                {
                    if (Regex.IsMatch(entry.Name, "model$"))
                    {
                        using (var memoryStream = new MemoryStream())
                        {
                            tarStream.CopyEntryContents(memoryStream);
                            memoryStream.Position = 0;
                            string content = new StreamReader(memoryStream).ReadToEnd();
                            string head = content.Substring(0, Math.Min(100, content.Length));

                            string folder = Path.Combine(Path.GetDirectoryName(path) ?? string.Empty, entry.Name.Split('/')[0]);
                            return new DetectedFile(path, "ICEPAK Packfile", ParseIcepakVersion(head.Split(' ')) ?? "None", folder, false);
                        }
                    }
                }
            }
            return null;
        }

        public void CheckFile(string path)
        {
            DetectedFile? detected = null;

            if (File.Exists(path))
            {
                string extension = Path.GetExtension(path);
                switch (extension)
                {
                    case ".tzr":
                    case ".tgz":
                        detected = CheckSAversion(path);
                        break;
                    case ".aedt":
                        detected = CheckAEDTversion(path);
                        break;
                    case ".siw":
                        detected = CheckSIversion(path);
                        break;
                    default:
                        break;
                }
            }
            else if (Directory.Exists(path))
            {
                detected = CheckSAdirectory(path);
            }

            _detected = detected ?? DetectedFile.None(path);
        }

        public void launchApplication()
        {
            string myKey;
            string myPath;

            switch (_detected.FileType)
            {
                case "ICEPAK":
                    myKey = "ICEPAK:" + _detected.FileVersion;
                    myPath = _detected.FilePath;
                    break;
                case "ICEPAK Packfile":
                    if (!_detected.IsExtracted) ExtractSAversion();
                    myKey = "ICEPAK:" + _detected.FileVersion;
                    myPath = _detected.FilePath;
                    break;
                case "AEDT":
                    myKey = "AEDT:" + _detected.FileVersion;
                    myPath = _detected.FileName;
                    break;
                case "SIwave":
                    myKey = "SIwave:" + _detected.FileVersion;
                    myPath = _detected.FileName;
                    break;
                default:
                    myKey = "ERROR";
                    myPath = "";
                    break;
            }

            if (myKey != "ERROR")
            {
                try
                {
                    using var process = new Process();
                    process.StartInfo.FileName = ConfigurationManager.AppSettings.Get(myKey) ?? string.Empty;
                    process.StartInfo.Arguments = myPath;
                    process.Start();
                }
                catch
                {

                }
            }
        }
    }
}
