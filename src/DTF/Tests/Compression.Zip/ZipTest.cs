// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Deployment.Test
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Deployment.Compression;
    using Microsoft.Deployment.Compression.Zip;

    [TestClass]
    public class ZipTest
    {
        public ZipTest()
        {
        }

        [TestInitialize]
        public void Initialize()
        {
        }

        [TestCleanup]
        public void Cleanup()
        {
        }

        [TestMethod]
        public void ZipFileCounts()
        {
            this.RunZipPackUnpack(0, 10, 0);
            this.RunZipPackUnpack(0, 100000, 0);
            this.RunZipPackUnpack(1, 10, 0);
            this.RunZipPackUnpack(100, 10, 0);
        }

        [TestMethod]
        public void ZipExtremeFileCounts()
        {
            this.RunZipPackUnpack(66000, 10, 0);
        }

        [TestMethod]
        public void ZipFileSizes()
        {
            this.RunZipPackUnpack(1, 0, 0);
            for (int n = 1; n <= 33; n++)
            {
                this.RunZipPackUnpack(1, n, 0);
            }
            this.RunZipPackUnpack(1, 100 * 1024, 0);
            this.RunZipPackUnpack(1, 10 * 1024 * 1024, 0);
        }

        [Timeout(36000000), TestMethod]
        public void ZipExtremeFileSizes()
        {
            //this.RunZipPackUnpack(10, 512L * 1024 * 1024, 0); // 5GB
            this.RunZipPackUnpack(1, 5L * 1024 * 1024 * 1024, 0, CompressionLevel.None); // 5GB
        }

        [TestMethod]
        public void ZipArchiveCounts()
        {
            IList<ArchiveFileInfo> fileInfo;
            fileInfo = this.RunZipPackUnpack(10, 100 * 1024, 400 * 1024, CompressionLevel.None);
            Assert.AreEqual<int>(2, fileInfo[fileInfo.Count - 1].ArchiveNumber,
                "Testing whether archive spans the correct # of zip files.");

            fileInfo = this.RunZipPackUnpack(2, 90 * 1024, 40 * 1024, CompressionLevel.None);
            Assert.AreEqual<int>(2, fileInfo[fileInfo.Count - 1].ArchiveNumber,
                "Testing whether archive spans the correct # of zip files.");
        }

        [TestMethod]
        public void ZipProgress()
        {
            CompressionTestUtil.ExpectedProgress = new List<int[]>(new int[][] {
                //    StatusType,  CurFile,TotalFiles,CurFolder,CurArchive,TotalArchives
                new int[] { (int) ArchiveProgressType.StartArchive,    0, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   0, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  0, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   1, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  1, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   2, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  2, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   3, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  3, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   4, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  4, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   5, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  5, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   6, 15, 0, 0, 1 },
                new int[] { (int) ArchiveProgressType.FinishArchive,   6, 15, 0, 0, 1 },
                new int[] { (int) ArchiveProgressType.StartArchive,    6, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  6, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,   7, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  7, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,   8, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  8, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,   9, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  9, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,  10, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 10, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,  11, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 11, 15, 0, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,  12, 15, 0, 1, 2 },
                new int[] { (int) ArchiveProgressType.FinishArchive,  12, 15, 0, 1, 2 },
                new int[] { (int) ArchiveProgressType.StartArchive,   12, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 12, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  13, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 13, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  14, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 14, 15, 0, 2, 3 },
                new int[] { (int) ArchiveProgressType.FinishArchive,  14, 15, 0, 2, 3 },
                //    StatusType,  CurFile,TotalFiles,CurFolder,CurArchive,TotalArchives
                new int[] { (int) ArchiveProgressType.StartArchive,    0, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   0, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  0, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   1, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  1, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   2, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  2, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   3, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  3, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   4, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  4, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   5, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  5, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   6, 15, 0, 0, 3 },
                new int[] { (int) ArchiveProgressType.FinishArchive,   6, 15, 0, 0, 3 },
                new int[] { (int) ArchiveProgressType.StartArchive,    6, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  6, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   7, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  7, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   8, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  8, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   9, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  9, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  10, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 10, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  11, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 11, 15, 0, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  12, 15, 0, 1, 3 },
                new int[] { (int) ArchiveProgressType.FinishArchive,  12, 15, 0, 1, 3 },
                new int[] { (int) ArchiveProgressType.StartArchive,   12, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 12, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  13, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 13, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  14, 15, 0, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 14, 15, 0, 2, 3 },
                new int[] { (int) ArchiveProgressType.FinishArchive,  14, 15, 0, 2, 3 },
            });
            CompressionTestUtil.ExpectedProgress = null;

            try
            {
                this.RunZipPackUnpack(15, 20 * 1024, 130 * 1024, CompressionLevel.None);
            }
            finally
            {
                CompressionTestUtil.ExpectedProgress = null;
            }
        }

        [TestMethod]
        public void ZipArchiveSizes()
        {
            Console.WriteLine("Testing various values for the maxArchiveSize parameter.");
            this.RunZipPackUnpack(5, 1024, Int64.MinValue);
            this.RunZipPackUnpack(5, 1024, -1);
            this.RunZipPackUnpack(2, 10, 0);

            this.RunZipPackUnpack(1, 10, 1);
            this.RunZipPackUnpack(2, 10, 2);
            this.RunZipPackUnpack(2, 10, 3);
            this.RunZipPackUnpack(2, 10, 4);
            this.RunZipPackUnpack(2, 10, 5);
            this.RunZipPackUnpack(2, 10, 6);
            this.RunZipPackUnpack(2, 10, 7);
            this.RunZipPackUnpack(5, 10, 8);
            this.RunZipPackUnpack(5, 10, 9);
            this.RunZipPackUnpack(5, 10, 10);
            this.RunZipPackUnpack(5, 10, 11);
            this.RunZipPackUnpack(5, 10, 12);

            this.RunZipPackUnpack(5, 101, 255);
            this.RunZipPackUnpack(5, 102, 256);
            this.RunZipPackUnpack(5, 103, 257);
            this.RunZipPackUnpack(5, 24000, 32768);
            this.RunZipPackUnpack(5, 1024, Int64.MaxValue);
        }

        [TestMethod]
        public void ZipCompLevelParam()
        {
            Console.WriteLine("Testing various values for the compressionLevel parameter.");
            this.RunZipPackUnpack(5, 1024, 0, CompressionLevel.None);
            this.RunZipPackUnpack(5, 1024, 0, CompressionLevel.Min);
            this.RunZipPackUnpack(5, 1024, 0, CompressionLevel.Normal);
            this.RunZipPackUnpack(5, 1024, 0, CompressionLevel.Max);
            this.RunZipPackUnpack(5, 1024, 0, (CompressionLevel) ((int) CompressionLevel.None - 1));
            this.RunZipPackUnpack(5, 1024, 0, (CompressionLevel) ((int) CompressionLevel.Max + 1));
            this.RunZipPackUnpack(5, 1024, 0, (CompressionLevel) Int32.MinValue);
            this.RunZipPackUnpack(5, 1024, 0, (CompressionLevel) Int32.MaxValue);
        }

        [TestMethod]
        public void ZipInfoGetFiles()
        {
            IList<ZipFileInfo> fileInfos;
            ZipInfo zipInfo = new ZipInfo("testgetfiles.zip");

            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("testinfo0.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("testinfo1.txt", 1, txtSize);
            CompressionTestUtil.GenerateRandomFile("testinfo2.ini", 2, txtSize);
            zipInfo.PackFiles(null, new string[] { "testinfo0.txt", "testinfo1.txt", "testinfo2.ini" }, null);

            fileInfos = zipInfo.GetFiles();
            Assert.IsNotNull(fileInfos);
            Assert.AreEqual<int>(3, fileInfos.Count);
            Assert.AreEqual<string>("testinfo0.txt", fileInfos[0].Name);
            Assert.AreEqual<string>("testinfo1.txt", fileInfos[1].Name);
            Assert.AreEqual<string>("testinfo2.ini", fileInfos[2].Name);

            fileInfos = zipInfo.GetFiles("*.txt");
            Assert.IsNotNull(fileInfos);
            Assert.AreEqual<int>(2, fileInfos.Count);
            Assert.AreEqual<string>("testinfo0.txt", fileInfos[0].Name);
            Assert.AreEqual<string>("testinfo1.txt", fileInfos[1].Name);

            fileInfos = zipInfo.GetFiles("testinfo1.txt");
            Assert.IsNotNull(fileInfos);
            Assert.AreEqual<int>(1, fileInfos.Count);
            Assert.AreEqual<string>("testinfo1.txt", fileInfos[0].Name);
            Assert.IsTrue(DateTime.Now - fileInfos[0].LastWriteTime < TimeSpan.FromMinutes(1),
                "Checking ZipFileInfo.LastWriteTime is current.");
        }

        [TestMethod]
        public void ZipInfoNullParams()
        {
            int fileCount = 10, fileSize = 1024;
            string dirA = String.Format("{0}-{1}-A", fileCount, fileSize);
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
            Directory.CreateDirectory(dirA);
            string dirB = String.Format("{0}-{1}-B", fileCount, fileSize);
            if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);

            string[] files = new string[fileCount];
            for (int iFile = 0; iFile < fileCount; iFile++)
            {
                files[iFile] = "zipinfo-" + iFile + ".txt";
                CompressionTestUtil.GenerateRandomFile(Path.Combine(dirA, files[iFile]), iFile, fileSize);
            }

            ZipInfo zipInfo = new ZipInfo("testnull.zip");

            CompressionTestUtil.TestArchiveInfoNullParams(zipInfo, dirA, dirB, files);
        }

        [TestMethod]
        public void ZipFileInfoNullParams()
        {
            Exception caughtEx;
            ZipInfo zipInfo = new ZipInfo("test.zip");
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("test00.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("test01.txt", 1, txtSize);
            zipInfo.PackFiles(null, new string[] { "test00.txt", "test01.txt" }, null);
            ZipFileInfo zfi = new ZipFileInfo(zipInfo, "test01.txt");

            caughtEx = null;
            try
            {
                new ZipFileInfo(null, "test00.txt");
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException));
            caughtEx = null;
            try
            {
                new ZipFileInfo(zipInfo, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException));
            caughtEx = null;
            try
            {
                zfi.CopyTo(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void ZipEngineNullParams()
        {
            string[] testFiles = new string[] { "test.txt" };
            ArchiveFileStreamContext streamContext = new ArchiveFileStreamContext("test.zip", null, null);

            using (ZipEngine zipEngine = new ZipEngine())
            {
                zipEngine.CompressionLevel = CompressionLevel.None;

                CompressionTestUtil.TestCompressionEngineNullParams(zipEngine, streamContext, testFiles);
            }
        }

        [TestMethod]
        public void ZipBadPackStreamContexts()
        {
            string[] testFiles = new string[] { "test.txt" };
            CompressionTestUtil.GenerateRandomFile(testFiles[0], 0, 20000);

            using (ZipEngine zipEngine = new ZipEngine())
            {
                zipEngine.CompressionLevel = CompressionLevel.None;

                CompressionTestUtil.TestBadPackStreamContexts(zipEngine, "test.zip", testFiles);
            }
        }

        [TestMethod]
        public void ZipBadUnpackStreamContexts()
        {
            int txtSize = 40960;
            ZipInfo zipInfo = new ZipInfo("test2.zip");
            CompressionTestUtil.GenerateRandomFile("ziptest-0.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("ziptest-1.txt", 1, txtSize);
            zipInfo.PackFiles(null, new string[] { "ziptest-0.txt", "ziptest-1.txt" }, null);

            using (ZipEngine zipEngine = new ZipEngine())
            {
                CompressionTestUtil.TestBadUnpackStreamContexts(zipEngine, "test2.zip");
            }
        }

        [TestMethod]
        public void ZipTruncatedArchive()
        {
            ZipInfo zipInfo = new ZipInfo("test-t.zip");
            CompressionTestUtil.GenerateRandomFile("ziptest-0.txt", 0, 5);
            CompressionTestUtil.GenerateRandomFile("ziptest-1.txt", 1, 5);
            zipInfo.PackFiles(null, new string[] { "ziptest-0.txt", "ziptest-1.txt" }, null);

            CompressionTestUtil.TestTruncatedArchive(zipInfo, typeof(ZipException));
        }

        /*
        [TestMethod]
        public void ZipUnpack()
        {
            IList<ZipFileInfo> fileInfos;
            foreach (FileInfo zipFile in new DirectoryInfo("D:\\temp").GetFiles("*.zip"))
            {
                Console.WriteLine("=====================================================");
                Console.WriteLine(zipFile.FullName);
                Console.WriteLine("=====================================================");
                ZipInfo zipTest = new ZipInfo(zipFile.FullName);
                fileInfos = zipTest.GetFiles();
                Assert.AreNotEqual<int>(0, fileInfos.Count);
                foreach (ArchiveFileInfo file in fileInfos)
                {
                    Console.WriteLine("{0}\t{1}\t{2}", Path.Combine(file.Path, file.Name), file.Length, file.LastWriteTime);
                }

                Directory.CreateDirectory(Path.GetFileNameWithoutExtension(zipFile.Name));
                zipTest.Unpack(Path.GetFileNameWithoutExtension(zipFile.Name));
            }
        }
        */

        /*
        [TestMethod]
        public void ZipUnpackSelfExtractor()
        {
            ZipInfo zipTest = new ZipInfo(@"C:\temp\testzip.exe");
            IList<ZipFileInfo> fileInfos = zipTest.GetFiles();
            Assert.AreNotEqual<int>(0, fileInfos.Count);
            foreach (ArchiveFileInfo file in fileInfos)
            {
                Console.WriteLine("{0}\t{1}\t{2}", Path.Combine(file.Path, file.Name), file.Length, file.LastWriteTime);
            }

            string extractDir = Path.GetFileNameWithoutExtension(zipTest.Name);
            Directory.CreateDirectory(extractDir);
            zipTest.Unpack(extractDir);
        }
        */

        private const string TEST_FILENAME_PREFIX = "\x20AC";

        private IList<ArchiveFileInfo> RunZipPackUnpack(int fileCount, long fileSize,
            long maxArchiveSize)
        {
            return this.RunZipPackUnpack(fileCount, fileSize, maxArchiveSize, CompressionLevel.Normal);
        }

        private IList<ArchiveFileInfo> RunZipPackUnpack(int fileCount, long fileSize,
            long maxArchiveSize, CompressionLevel compLevel)
        {
            Console.WriteLine("Creating zip archive with {0} files of size {1}",
                fileCount, fileSize);
            Console.WriteLine("MaxArchiveSize={0}, CompressionLevel={1}", maxArchiveSize, compLevel);

            string dirA = String.Format("{0}-{1}-A", fileCount, fileSize);
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
            Directory.CreateDirectory(dirA);
            string dirB = String.Format("{0}-{1}-B", fileCount, fileSize);
            if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);

            string[] files = new string[fileCount];
            for (int iFile = 0; iFile < fileCount; iFile++)
            {
                files[iFile] = TEST_FILENAME_PREFIX + iFile + ".txt";
                CompressionTestUtil.GenerateRandomFile(Path.Combine(dirA, files[iFile]), iFile, fileSize);
            }

            string[] archiveNames = new string[1000];
            for (int i = 0; i < archiveNames.Length; i++)
            {
                if (i < 100)
                {
                    archiveNames[i] = String.Format(
                        (i == 0 ? "{0}-{1}.zip" : "{0}-{1}.z{2:d02}"),
                        fileCount, fileSize, i);
                }
                else
                {
                    archiveNames[i] = String.Format(
                         "{0}-{1}.{2:d03}", fileCount, fileSize, i);
                }
            }

            string progressTextFile = String.Format("progress_{0}-{1}.txt", fileCount, fileSize);
            CompressionTestUtil testUtil = new CompressionTestUtil(progressTextFile);

            IList<ArchiveFileInfo> fileInfo;
            using (ZipEngine zipEngine = new ZipEngine())
            {
                zipEngine.CompressionLevel = compLevel;

                File.AppendAllText(progressTextFile,
                    "\r\n\r\n====================================================\r\nCREATE\r\n\r\n");
                zipEngine.Progress += testUtil.PrintArchiveProgress;

                OptionStreamContext streamContext = new OptionStreamContext(archiveNames, dirA, null);
                streamContext.OptionHandler =
                    delegate(string optionName, object[] parameters)
                    {
                        // For testing purposes, force zip64 for only moderately large files.
                        switch (optionName)
                        {
                            case  "forceZip64":
                                return fileSize > UInt16.MaxValue;
                            default:
                                return null;
                        }
                    };

                zipEngine.Pack(streamContext, files, maxArchiveSize);

                string checkArchiveName = archiveNames[0];
                if (File.Exists(archiveNames[1])) checkArchiveName = archiveNames[1];
                using (Stream archiveStream = File.OpenRead(checkArchiveName))
                {
                    bool isArchive = zipEngine.IsArchive(archiveStream);
                    Assert.IsTrue(isArchive, "Checking that created archive appears valid.");
                }

                IList<string> createdArchiveNames = new List<string>(archiveNames.Length);
                for (int i = 0; i < archiveNames.Length; i++)
                {
                    if (File.Exists(archiveNames[i]))
                    {
                        createdArchiveNames.Add(archiveNames[i]);
                    }
                    else
                    {
                        break;
                    }
                }

                Assert.AreNotEqual<int>(0, createdArchiveNames.Count);

                Console.WriteLine("Listing zip archive with {0} files of size {1}",
                    fileCount, fileSize);
                File.AppendAllText(progressTextFile, "\r\n\r\nLIST\r\n\r\n");
                fileInfo = zipEngine.GetFileInfo(
                    new ArchiveFileStreamContext(createdArchiveNames, null, null), null);

                Assert.AreEqual<int>(fileCount, fileInfo.Count);

                Console.WriteLine("Extracting zip archive with {0} files of size {1}",
                    fileCount, fileSize);
                File.AppendAllText(progressTextFile, "\r\n\r\nEXTRACT\r\n\r\n");
                zipEngine.Unpack(new ArchiveFileStreamContext(createdArchiveNames, dirB, null), null);
            }

            bool directoryMatch = CompressionTestUtil.CompareDirectories(dirA, dirB);
            Assert.IsTrue(directoryMatch,
                "Testing whether zip output directory matches input directory.");

            return fileInfo;
        }
    }
}
