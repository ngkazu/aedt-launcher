using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using aedt_launcher;
using ICSharpCode.SharpZipLib.Tar;
using Xunit;

namespace aedt_launcher.Tests
{
    public class FileClassTests : IDisposable
    {
        private readonly string tempDir = Path.Combine(Path.GetTempPath(), "aedt-launcher-tests-" + Guid.NewGuid().ToString("N"));

        public FileClassTests()
        {
            Directory.CreateDirectory(tempDir);
        }

        public void Dispose()
        {
            Directory.Delete(tempDir, recursive: true);
        }

        [Theory]
        [InlineData(new[] { "ICEPAK", "2019", "Icepak", "19.0" }, "19.0")]
        [InlineData(new[] { "a", "b", "19.1" }, "19.1")]
        [InlineData(new[] { "a", "b" }, null)]
        [InlineData(new[] { "a", "b", "Icepak" }, null)]
        public void ParseIcepakVersion_ReturnsExpectedVersion(string[] input, string? expected)
        {
            Assert.Equal(expected, FileClass.ParseIcepakVersion(input));
        }

        [Fact]
        public void CheckFile_SIwaveFile_ReadsDbVersion()
        {
            string path = Path.Combine(tempDir, "board.siw");
            File.WriteAllText(path, "DB_VERSION 2019.1\n");

            var file = new FileClass();
            file.CheckFile(path);

            Assert.Equal(FileKind.SIwave, file.FileType);
            Assert.Equal("2019.1", file.FileVersion);
        }

        [Fact]
        public void CheckFile_AedtFile_ReadsVersionAfterDesktopLine()
        {
            string path = Path.Combine(tempDir, "project.aedt");
            File.WriteAllText(path, "begin 'Desktop'\n\t2019,1\n");

            var file = new FileClass();
            file.CheckFile(path);

            Assert.Equal(FileKind.Aedt, file.FileType);
            Assert.Equal("2019.1", file.FileVersion);
        }

        [Fact]
        public void CheckFile_IcepakDirectory_ReadsVersionFromModelFile()
        {
            string dir = Path.Combine(tempDir, "packfolder");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "model"), "ICEPAK 2019 Icepak 19.0\n");

            var file = new FileClass();
            file.CheckFile(dir);

            Assert.Equal(FileKind.Icepak, file.FileType);
            Assert.Equal("19.0", file.FileVersion);
        }

        [Fact]
        public void CheckFile_UnsupportedExtension_LeavesTypeNone()
        {
            string path = Path.Combine(tempDir, "readme.txt");
            File.WriteAllText(path, "DB_VERSION 2019.1\n");

            var file = new FileClass();
            file.CheckFile(path);

            Assert.Equal(FileKind.None, file.FileType);
            Assert.Equal("None", file.FileVersion);
        }

        [Fact]
        public void CheckFile_NonGzipTgz_LeavesTypeNone()
        {
            string path = Path.Combine(tempDir, "broken.tgz");
            File.WriteAllText(path, "not a gzip file");

            var file = new FileClass();
            file.CheckFile(path);

            Assert.Equal(FileKind.None, file.FileType);
        }

        [Fact]
        public void ExtractSAversion_NonGzipTgz_SetsTypeToError()
        {
            string path = Path.Combine(tempDir, "broken.tgz");
            File.WriteAllText(path, "not a gzip file");

            var file = new FileClass();
            file.CheckFile(path);
            file.ExtractSAversion();

            Assert.Equal(FileKind.Error, file.FileType);
        }

        [Fact]
        public void CheckFile_MissingPath_LeavesTypeNone()
        {
            var file = new FileClass();
            file.CheckFile(Path.Combine(tempDir, "missing.siw"));

            Assert.Equal(FileKind.None, file.FileType);
        }

        [Fact]
        public void CheckFile_UppercaseExtension_IsRecognized()
        {
            string path = Path.Combine(tempDir, "BOARD.SIW");
            File.WriteAllText(path, "DB_VERSION 2019.1\n");

            var file = new FileClass();
            file.CheckFile(path);

            Assert.Equal(FileKind.SIwave, file.FileType);
        }

        [Fact]
        public void CheckFile_PackfileWithModelAndSuffixedModel_PrefersModel()
        {
            string path = Path.Combine(tempDir, "packfile.tzr");
            CreatePackfile(path, ("job/a.model", "ICEPAK 2019 Icepak 18.0\n"), ("job/model", "ICEPAK 2019 Icepak 19.0\n"));

            var file = new FileClass();
            file.CheckFile(path);

            Assert.Equal(FileKind.IcepakPackfile, file.FileType);
            Assert.Equal("19.0", file.FileVersion);
        }

        [Fact]
        public void CheckFile_PackfileWithOnlySuffixedModel_UsesSuffixedModel()
        {
            string path = Path.Combine(tempDir, "packfile.tzr");
            CreatePackfile(path, ("job/a.model", "ICEPAK 2019 Icepak 18.0\n"));

            var file = new FileClass();
            file.CheckFile(path);

            Assert.Equal(FileKind.IcepakPackfile, file.FileType);
            Assert.Equal("18.0", file.FileVersion);
        }

        [Fact]
        public void CheckFile_PackfileWithXmodelOnly_LeavesTypeNone()
        {
            string path = Path.Combine(tempDir, "packfile.tzr");
            CreatePackfile(path, ("job/xmodel", "ICEPAK 2019 Icepak 19.0\n"));

            var file = new FileClass();
            file.CheckFile(path);

            Assert.Equal(FileKind.None, file.FileType);
        }

        private static void CreatePackfile(string path, params (string Name, string Content)[] entries)
        {
            using var fileStream = File.Create(path);
            using var gzStream = new GZipStream(fileStream, CompressionLevel.Optimal);
            using var tarStream = new TarOutputStream(gzStream, Encoding.UTF8);
            foreach (var (name, content) in entries)
            {
                byte[] data = Encoding.UTF8.GetBytes(content);
                var entry = TarEntry.CreateTarEntry(name);
                entry.Size = data.Length;
                tarStream.PutNextEntry(entry);
                tarStream.Write(data, 0, data.Length);
                tarStream.CloseEntry();
            }
        }
    }
}
