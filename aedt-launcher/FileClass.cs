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
    class FileClass
    {
        private string _filename = string.Empty;
        private string _filepath = string.Empty;
        private string _filetype = string.Empty;
        private string _fileversion = string.Empty;
        private int _flag;

        public string FileName => this._filename;

        public string FileType => this._filetype;

        public string FileVersion => this._fileversion;

        public void ExtractSAversion()
        {
            string folder = Path.GetDirectoryName(this._filename) ?? string.Empty;

            using (var tzrStream = File.OpenRead(this._filename))
            using (var gzStream = new GZipStream(tzrStream, CompressionMode.Decompress))
            using (var tarStream = TarArchive.CreateInputTarArchive(gzStream, Encoding.UTF8))
            {
                tarStream.ExtractContents(folder);
            }
            this._flag = 1;
        }

        private void CheckSIversion(string path)
        {
            foreach (var line in File.ReadLines(path, Encoding.UTF8))
            {
                if (Regex.IsMatch(line, "DB_VERSION"))
                {
                    this._fileversion = Regex.Replace(line, @"[^0-9.]", "");
                    this._filetype = "SIwave";
                    this._filepath = path;
                    return;
                }
            }
        }

        private void CheckAEDTversion(string path)
        {
            using var lines = File.ReadLines(path, Encoding.UTF8).GetEnumerator();
            while (lines.MoveNext())
            {
                if (Regex.IsMatch(lines.Current, "begin 'Desktop'") && lines.MoveNext())
                {
                    var versionText = Regex.Replace(lines.Current, @"[^0-9,]", "");

                    this._fileversion = Regex.Replace(versionText, @",", ".");
                    this._filetype = "AEDT";
                    this._filepath = path;
                    return;
                }
            }
        }

        internal static string? ParseIcepakVersion(string[] parts)
        {
            if (parts.Length < 3) return null;
            int index = parts[2] == "Icepak" ? 3 : 2;
            return parts.Length > index ? parts[index] : null;
        }

        private void CheckSAdirectory(string path)
        {
            string modelPath = Path.Combine(path, "model");
            if (File.Exists(modelPath))
            {
                string? line = File.ReadLines(modelPath, Encoding.UTF8).FirstOrDefault();
                if (line == null) return;

                this._filetype = "ICEPAK";
                this._fileversion = ParseIcepakVersion(line.Split(' ')) ?? this._fileversion;
                this._filepath = path;
            }
        }

        private void CheckSAversion(string path)
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

                            this._filetype = "ICEPAK Packfile";
                            this._fileversion = ParseIcepakVersion(head.Split(' ')) ?? this._fileversion;
                            this._filepath = Path.GetDirectoryName(path) + "\\" + entry.Name.Split('/')[0];
                        }
                        break;
                    }
                }
            }
        }

        public void CheckFile(string path)
        {
            this._filename = path;
            this._filetype = "None";
            this._filepath = "None";
            this._fileversion = "None";
            this._flag = 0;

            if (File.Exists(path))
            {
                string extension = Path.GetExtension(path);
                switch (extension)
                {
                    case ".tzr":
                    case ".tgz":
                        CheckSAversion(path);
                        break;
                    case ".aedt":
                        CheckAEDTversion(path);
                        break;
                    case ".siw":
                        CheckSIversion(path);
                        break;
                    default:
                        break;
                }
            }
            else if (Directory.Exists(path))
            {
                CheckSAdirectory(path);
            }
        }

        public void launchApplication()
        {
            string myKey;
            string myPath;

            switch (this._filetype)
            {
                case "ICEPAK":
                    myKey = "ICEPAK:" + this._fileversion;
                    myPath = this._filepath;
                    break;
                case "ICEPAK Packfile":
                    if (_flag == 0) ExtractSAversion();
                    myKey = "ICEPAK:" + this._fileversion;
                    myPath = this._filepath;
                    break;
                case "AEDT":
                    myKey = "AEDT:" + this._fileversion;
                    myPath = this._filename;
                    break;
                case "SIwave":
                    myKey = "SIwave:" + this._fileversion;
                    myPath = this._filename;
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
