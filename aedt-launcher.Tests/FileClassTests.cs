using System;
using System.IO;
using aedt_launcher;
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
            file.checkFILE(path);

            Assert.Equal("SIwave", file.get_filetye());
            Assert.Equal("2019.1", file.get_fileversion());
        }

        [Fact]
        public void CheckFile_AedtFile_ReadsVersionAfterDesktopLine()
        {
            string path = Path.Combine(tempDir, "project.aedt");
            File.WriteAllText(path, "begin 'Desktop'\n\t2019,1\n");

            var file = new FileClass();
            file.checkFILE(path);

            Assert.Equal("AEDT", file.get_filetye());
            Assert.Equal("2019.1", file.get_fileversion());
        }

        [Fact]
        public void CheckFile_IcepakDirectory_ReadsVersionFromModelFile()
        {
            string dir = Path.Combine(tempDir, "packfolder");
            Directory.CreateDirectory(dir);
            File.WriteAllText(Path.Combine(dir, "model"), "ICEPAK 2019 Icepak 19.0\n");

            var file = new FileClass();
            file.checkFILE(dir);

            Assert.Equal("ICEPAK", file.get_filetye());
            Assert.Equal("19.0", file.get_fileversion());
        }

        [Fact]
        public void CheckFile_UnsupportedExtension_LeavesTypeNone()
        {
            string path = Path.Combine(tempDir, "readme.txt");
            File.WriteAllText(path, "DB_VERSION 2019.1\n");

            var file = new FileClass();
            file.checkFILE(path);

            Assert.Equal("None", file.get_filetye());
            Assert.Equal("None", file.get_fileversion());
        }

        [Fact]
        public void CheckFile_MissingPath_LeavesTypeNone()
        {
            var file = new FileClass();
            file.checkFILE(Path.Combine(tempDir, "missing.siw"));

            Assert.Equal("None", file.get_filetye());
        }
    }
}
