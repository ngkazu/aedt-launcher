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
        private string filename = string.Empty;
        private string filepath = string.Empty;
        private string filetype = string.Empty;
        private string fileversion = string.Empty;
        private int flag;

        public string get_filename()
        {
            return this.filename;
        }

        public string get_filetye()
        {
            return this.filetype;
        }

        public string get_fileversion()
        {
            return this.fileversion;
        }

        public void extrctSAversion()
        {
            string fo = Path.GetDirectoryName(this.filename) ?? string.Empty;

            using (var tzrStream = File.OpenRead(this.filename))
            using (var gzStream = new GZipStream(tzrStream, CompressionMode.Decompress))
            using (var tarStream = TarArchive.CreateInputTarArchive(gzStream, Encoding.UTF8))
            {
                tarStream.ExtractContents(fo);
            }
            this.flag = 1;
        }

        private void checkSIversion(string fi)
        {
            foreach (var line in File.ReadLines(fi, Encoding.UTF8))
            {
                if (Regex.IsMatch(line, "DB_VERSION"))
                {
                    this.fileversion = Regex.Replace(line, @"[^0-9.]", "");
                    this.filetype = "SIwave";
                    this.filepath = fi;
                    return;
                }
            }
        }

        private void checkAEDTversion(string fi)
        {
            using var lines = File.ReadLines(fi, Encoding.UTF8).GetEnumerator();
            while (lines.MoveNext())
            {
                if (Regex.IsMatch(lines.Current, "begin 'Desktop'") && lines.MoveNext())
                {
                    var st = Regex.Replace(lines.Current, @"[^0-9,]", "");

                    this.fileversion = Regex.Replace(st, @",", ".");
                    this.filetype = "AEDT";
                    this.filepath = fi;
                    return;
                }
            }
        }

        internal static string? ParseIcepakVersion(string[] ar)
        {
            if (ar.Length < 3) return null;
            int index = ar[2] == "Icepak" ? 3 : 2;
            return ar.Length > index ? ar[index] : null;
        }

        private void checkSAdirectory(string fi)
        {
            string fe = Path.Combine(fi, "model");
            if (File.Exists(fe))
            {
                string? li = File.ReadLines(fe, Encoding.UTF8).FirstOrDefault();
                if (li == null) return;

                this.filetype = "ICEPAK";
                this.fileversion = ParseIcepakVersion(li.Split(' ')) ?? this.fileversion;
                this.filepath = fi;
            }
        }

        private void checkSAversion(string fi)
        {
            using (var tzrStream = File.OpenRead(fi))
            using (var gzStream = new GZipStream(tzrStream, CompressionMode.Decompress))
            using (var tarStream = new TarInputStream(gzStream, Encoding.UTF8))
            {
                TarEntry? te;
                while ((te = tarStream.GetNextEntry()) != null)
                {
                    if (Regex.IsMatch(te.Name, "model$"))
                    {
                        using (var ms = new MemoryStream())
                        {
                            tarStream.CopyEntryContents(ms);
                            ms.Position = 0;
                            string li = new StreamReader(ms).ReadToEnd();
                            string head = li.Substring(0, Math.Min(100, li.Length));

                            this.filetype = "ICEPAK Packfile";
                            this.fileversion = ParseIcepakVersion(head.Split(' ')) ?? this.fileversion;
                            this.filepath = Path.GetDirectoryName(fi) + "\\" + te.Name.Split('/')[0];
                        }
                        break;
                    }
                }
            }
        }

        public void checkFILE(string fi)
        {
            this.filename = fi;
            this.filetype = "None";
            this.filepath = "None";
            this.fileversion = "None";
            this.flag = 0;

            if (File.Exists(fi))
            {
                string ext = Path.GetExtension(fi);
                switch (ext)
                {
                    case ".tzr":
                    case ".tgz":
                        checkSAversion(fi);
                        break;
                    case ".aedt":
                        checkAEDTversion(fi);
                        break;
                    case ".siw":
                        checkSIversion(fi);
                        break;
                    default:
                        break;
                }
            }
            else if (Directory.Exists(fi))
            {
                checkSAdirectory(fi);
            }
        }

        public void launchApplication()
        {
            string myKey;
            string myPath;

            switch (this.filetype)
            {
                case "ICEPAK":
                    myKey = "ICEPAK:" + this.fileversion;
                    myPath = this.filepath;
                    break;
                case "ICEPAK Packfile":
                    if (flag == 0) extrctSAversion();
                    myKey = "ICEPAK:" + this.fileversion;
                    myPath = this.filepath;
                    break;
                case "AEDT":
                    myKey = "AEDT:" + this.fileversion;
                    myPath = this.filename;
                    break;
                case "SIwave":
                    myKey = "SIwave:" + this.fileversion;
                    myPath = this.filename;
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
                    Process p = new Process();
                    p.StartInfo.FileName = ConfigurationManager.AppSettings.Get(myKey) ?? string.Empty;
                    p.StartInfo.Arguments = myPath;
                    p.Start();
                }
                catch
                {

                }
            }
        }
    }
}
