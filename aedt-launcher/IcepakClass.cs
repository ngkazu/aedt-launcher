using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
//
using System.Text.RegularExpressions;
using System.IO.Compression;
using ICSharpCode.SharpZipLib.Tar;
using System.Configuration;

namespace aedt_launcher
{
    class IcepakClass
    {
        private string filename;
        private string filepath;
        private string filetype;
        private string fileversion;
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
            string fo = System.IO.Path.GetDirectoryName(this.filename);

            using (var tzrStream = System.IO.File.OpenRead(this.filename))
            using (var gzStream = new GZipStream(tzrStream, CompressionMode.Decompress))
            using (var tarStream = TarArchive.CreateInputTarArchive(gzStream))
            {
                tarStream.ExtractContents(fo);
            }
            this.flag = 1;
        }

        private void checkSIversion(string fi)
        {
            using (var sr = new System.IO.StreamReader(fi, Encoding.GetEncoding("UTF-8")))
            {
                while (sr.Peek() > -1)
                {
                    string line = sr.ReadLine();
                    if (Regex.IsMatch(line, "DB_VERSION"))
                    {
                        this.fileversion = Regex.Replace(line, @"[^0-9.]", "");
                        this.filetype = "SIwave";
                        this.filepath = fi;
                        break;
                    }
                }
            }
        }

        private void checkAEDTversion(string fi)
        {
            using (var sr = new System.IO.StreamReader(fi, Encoding.GetEncoding("UTF-8")))
            {
                while (sr.Peek() > -1)
                {
                    if (Regex.IsMatch(sr.ReadLine(), "begin 'Desktop'"))
                    {
                        string line = sr.ReadLine();
                        var st = Regex.Replace(line, @"[^0-9,]", "");

                        this.fileversion = Regex.Replace(st, @",", ".");
                        this.filetype = "AEDT";
                        this.filepath = fi;
                        break;
                    }
                }
            }
        }

        private void checkSAdirectory(string fi)
        {
            string fe = fi + "\\model";
            if (System.IO.File.Exists(fe))
            {
                using (var sr = new System.IO.StreamReader(fe, Encoding.GetEncoding("UTF-8")))
                {
                    string li = sr.ReadLine();
                    string[] ar = li.Split(' ');
                    this.filetype = "ICEPAK";
                    if (ar[2] == "Icepak")
                    {
                        this.fileversion = ar[3];
                    }
                    else
                    {
                        this.fileversion = ar[2];
                    }
                    this.filepath = fi;
                }
            }
        }

        private void checkSAversion(string fi)
        {
            using (var tzrStream = System.IO.File.OpenRead(fi))
            using (var gzStream = new GZipStream(tzrStream, CompressionMode.Decompress))
            using (var tarStream = new TarInputStream(gzStream))
            {
                TarEntry te;
                while ((te = tarStream.GetNextEntry()) != null)
                {
                    if (Regex.IsMatch(te.Name, "model$"))
                    {
                        using (var ms = new System.IO.MemoryStream())
                        {
                            tarStream.CopyEntryContents(ms);
                            ms.Position = 0;
                            string li = new System.IO.StreamReader(ms).ReadToEnd();
                            string[] ar = li.Substring(0, 100).Split(' ');

                            this.filetype = "ICEPAK Packfile";
                            if (ar[2] == "Icepak")
                            {
                                this.fileversion = ar[3];
                            }
                            else
                            {
                                this.fileversion = ar[2];
                            }

                            ar = te.Name.Split('/');
                            this.filepath = System.IO.Path.GetDirectoryName(fi) + "\\" + ar[0];
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

            if (System.IO.File.Exists(fi))
            {
                string ext = System.IO.Path.GetExtension(fi);
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
            else if (System.IO.Directory.Exists(fi))
            {
                checkSAdirectory(fi);
            }
            else
            {

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
                    System.Diagnostics.Process p = new System.Diagnostics.Process();
                    p.StartInfo.FileName = ConfigurationManager.AppSettings.Get(myKey);
                    p.StartInfo.Arguments = myPath;
                    p.Start();
                }
                catch
                {
                    
                }
                finally
                {

                }
            }
        }
    }
}
