//-----------------------------------------------------------------------
// <copyright file="BuildTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest.MsbuildIntegrationTests
{
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography;
    using System.Threading;
    using Xunit;

    public class BuildTests : WixTestBase
    {
        [Fact]
        public void CanBuildAndBuildWithoutChanges()
        {
            this.Initialize(@"TestData\SimpleMsi\");
            this.DuplicateTestDataToTestFolder();

            MSBuild msb = new MSBuild();
            msb.Properties.Add("WixTargetsPath", Settings.WixTargetsPath);
            msb.ProjectFile = "SimpleMsi.wixproj";

            var result = msb.Run();
            Assert.Contains("Building target \"Compile\" completely.", result.StandardOutput);
            Assert.Contains("Building target \"Link\" completely.", result.StandardOutput);

            var firstPassFiles = Directory.GetFiles(this.TestFolder, "*", SearchOption.AllDirectories).Select(p => new { Path = p, Hash = this.GetHash(p), Modified = File.GetLastWriteTime(p) }).ToList();

            result = msb.Run();
            Assert.Contains("Skipping target \"Compile\" because all output files are up-to-date with respect to the input files.", result.StandardOutput);
            Assert.Contains("Skipping target \"Link\" because all output files are up-to-date with respect to the input files.", result.StandardOutput);

            var secondPassFiles = Directory.GetFiles(this.TestFolder, "*", SearchOption.AllDirectories).Select(p => new { Path = p, Hash = this.GetHash(p), Modified = File.GetLastWriteTime(p) }).ToList();
            foreach (var st in secondPassFiles)
            {
                var m = firstPassFiles.Where(f => f.Path == st.Path).SingleOrDefault();
                Assert.NotNull(m);
                Assert.Equal(m.Hash, st.Hash);
            }

            this.Completed();
        }

        [Fact]
        public void CanBuildAndClean()
        {
            this.Initialize(@"TestData\SimpleMsi\");
            this.DuplicateTestDataToTestFolder();

            string binPath = Path.Combine(this.TestFolder, "bin\\");
            string objPath = Path.Combine(this.TestFolder, "obj\\");

            // Build
            MSBuild msb = new MSBuild();
            msb.Properties.Add("WixTargetsPath", Settings.WixTargetsPath);
            msb.ProjectFile = "SimpleMsi.wixproj";
            var result = msb.Run();

            Assert.Contains("Building target \"Compile\" completely.", result.StandardOutput);
            Assert.Contains("Building target \"Link\" completely.", result.StandardOutput);

            var built = Directory.GetFiles(binPath, "*", SearchOption.AllDirectories);
            var intermediate = Directory.GetFiles(objPath, "*", SearchOption.AllDirectories);
            Assert.NotEmpty(built);

            // Clean
            msb.Targets.Add("Clean");
            result = msb.Run();

            var cleanBin = Directory.GetFiles(binPath, "*", SearchOption.AllDirectories);
            var cleanObj = Directory.GetFiles(objPath, "*", SearchOption.AllDirectories);
            Assert.Empty(cleanBin);
            Assert.Empty(cleanObj);

            this.Completed();
        }

        private byte[] GetHash(string path)
        {
            using (var f = File.OpenRead(path))
            using (var md5 = MD5.Create())
            {
                return md5.ComputeHash(f);
            }
        }
    }
}
