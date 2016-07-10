// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Deployment.Test
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading;
    using System.Collections.Generic;
    using System.Runtime.Serialization;
    using System.Runtime.Serialization.Formatters.Binary;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Deployment.Compression;
    using Microsoft.Deployment.Compression.Cab;

    [TestClass]
    public class CabTest
    {
        public CabTest()
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
        public void CabinetMultithread()
        {
            this.multithreadExceptions = new List<Exception>();

            const int threadCount = 10;
            IList<Thread> threads = new List<Thread>(threadCount);

            for (int i = 0; i < threadCount; i++)
            {
                Thread thread = new Thread(new ThreadStart(this.CabinetMultithreadWorker));
                thread.Name = "CabinetMultithreadWorker_" + i;
                threads.Add(thread);
            }

            foreach (Thread thread in threads)
            {
                thread.Start();
            }

            foreach (Thread thread in threads)
            {
                thread.Join();
            }

            foreach (Exception ex in this.multithreadExceptions)
            {
                Console.WriteLine();
                Console.WriteLine(ex);
            }
            Assert.AreEqual<int>(0, this.multithreadExceptions.Count);
        }

        private IList<Exception> multithreadExceptions;

        private void CabinetMultithreadWorker()
        {
            try
            {
                string threadName = Thread.CurrentThread.Name;
                int threadNumber = Int32.Parse(threadName.Substring(threadName.IndexOf('_') + 1));
                this.RunCabinetPackUnpack(100, 10240 + threadNumber, 0, 0, CompressionLevel.Normal);
            }
            catch (Exception ex)
            {
                this.multithreadExceptions.Add(ex);
            }
        }

        [TestMethod]
        public void CabinetFileCounts()
        {
            this.RunCabinetPackUnpack(0, 10, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 10, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(100, 10, 0, 0, CompressionLevel.Normal);
        }

        [TestMethod]
        public void CabinetExtremeFileCounts()
        {
            this.RunCabinetPackUnpack(66000, 10);
        }

        [TestMethod]
        public void CabinetFileSizes()
        {
            this.RunCabinetPackUnpack(1, 0, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 1, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 2, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 3, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 4, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 5, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 6, 0, 0, CompressionLevel.Normal);
            // Skip file sizes 7-9: see "buggy" file sizes test below.
            this.RunCabinetPackUnpack(1, 10, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 11, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 12, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 100 * 1024, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 10 * 1024 * 1024, 0, 0, CompressionLevel.Normal);
        }

        [TestMethod]
        public void CabinetBuggyFileSizes()
        {
            // Windows' cabinet.dll has a known bug (#55001 in Windows OS Bugs) 
            // LZX compression causes an AV with file sizes of 7, 8, or 9 bytes.
            try
            {
                this.RunCabinetPackUnpack(1, 7, 0, 0, CompressionLevel.Normal);
                this.RunCabinetPackUnpack(1, 8, 0, 0, CompressionLevel.Normal);
                this.RunCabinetPackUnpack(1, 9, 0, 0, CompressionLevel.Normal);
            }
            catch (AccessViolationException)
            {
                Assert.Fail("Known 7,8,9 file size bug detected in Windows' cabinet.dll.");
            }
        }

        [Timeout(36000000), TestMethod]
        public void CabinetExtremeFileSizes()
        {
            this.RunCabinetPackUnpack(10, 512L * 1024 * 1024); // 5GB
            //this.RunCabinetPackUnpack(1, 5L * 1024 * 1024 * 1024); // 5GB
        }

        [TestMethod]
        public void CabinetFolders()
        {
            this.RunCabinetPackUnpack(0, 10, 1, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(1, 10, 1, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(100, 10, 1, 0, CompressionLevel.Normal);

            IList<ArchiveFileInfo> fileInfo;
            fileInfo = this.RunCabinetPackUnpack(7, 100 * 1024, 250 * 1024, 0, CompressionLevel.None);
            Assert.AreEqual<int>(2, ((CabFileInfo) fileInfo[fileInfo.Count - 1]).CabinetFolderNumber,
                "Testing whether cabinet has the correct # of folders.");

            fileInfo = this.RunCabinetPackUnpack(10, 100 * 1024, 250 * 1024, 0, CompressionLevel.None);
            Assert.AreEqual<int>(3, ((CabFileInfo) fileInfo[fileInfo.Count - 1]).CabinetFolderNumber,
                "Testing whether cabinet has the correct # of folders.");

            fileInfo = this.RunCabinetPackUnpack(2, 100 * 1024, 40 * 1024, 0, CompressionLevel.None);
            Assert.AreEqual<int>(1, ((CabFileInfo) fileInfo[fileInfo.Count - 1]).CabinetFolderNumber,
                "Testing whether cabinet has the correct # of folders.");
        }

        [TestMethod]
        public void CabinetArchiveCounts()
        {
            IList<ArchiveFileInfo> fileInfo;
            fileInfo = this.RunCabinetPackUnpack(10, 100 * 1024, 0, 400 * 1024, CompressionLevel.None);
            Assert.AreEqual<int>(2, fileInfo[fileInfo.Count - 1].ArchiveNumber,
                "Testing whether archive spans the correct # of cab files.");

            fileInfo = this.RunCabinetPackUnpack(2, 90 * 1024, 0, 40 * 1024, CompressionLevel.None);
            Assert.AreEqual<int>(2, fileInfo[fileInfo.Count - 1].ArchiveNumber,
                "Testing whether archive spans the correct # of cab files.");
        }

        [TestMethod]
        public void CabinetProgress()
        {
            CompressionTestUtil.ExpectedProgress = new List<int[]>(new int[][] {
                //            StatusType,  CurFile,TotalFiles,CurFolder,CurCab,TotalCabs
                new int[] { (int)     ArchiveProgressType.StartFile,   0, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  0, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   1, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  1, 15, 0, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   2, 15, 1, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  2, 15, 1, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   3, 15, 1, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  3, 15, 1, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   4, 15, 2, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  4, 15, 2, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   5, 15, 2, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  5, 15, 2, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   6, 15, 3, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  6, 15, 3, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   7, 15, 3, 0, 1 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  7, 15, 3, 0, 1 },
                new int[] { (int) ArchiveProgressType.StartArchive,    7, 15, 3, 0, 1 },
                new int[] { (int) ArchiveProgressType.FinishArchive,   7, 15, 3, 0, 1 },
                new int[] { (int)     ArchiveProgressType.StartFile,   8, 15, 4, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  8, 15, 4, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,   9, 15, 4, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  9, 15, 4, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,  10, 15, 5, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 10, 15, 5, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,  11, 15, 5, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 11, 15, 5, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,  12, 15, 6, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 12, 15, 6, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,  13, 15, 6, 1, 2 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 13, 15, 6, 1, 2 },
                new int[] { (int) ArchiveProgressType.StartArchive,   13, 15, 6, 1, 2 },
                new int[] { (int) ArchiveProgressType.FinishArchive,  13, 15, 6, 1, 2 },
                new int[] { (int)     ArchiveProgressType.StartFile,  14, 15, 7, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 14, 15, 7, 2, 3 },
                new int[] { (int) ArchiveProgressType.StartArchive,   14, 15, 7, 2, 3 },
                new int[] { (int) ArchiveProgressType.FinishArchive,  14, 15, 7, 2, 3 },
                //            StatusType,  CurFile,TotalFiles,CurFolder,CurCab,TotalCabs
                new int[] { (int) ArchiveProgressType.StartArchive,    0, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   0, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  0, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   1, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  1, 15, 0, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   2, 15, 1, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  2, 15, 1, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   3, 15, 1, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  3, 15, 1, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   4, 15, 2, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  4, 15, 2, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   5, 15, 2, 0, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  5, 15, 2, 0, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   6, 15, 3, 0, 3 },
                new int[] { (int) ArchiveProgressType.FinishArchive,   6, 15, 3, 0, 3 },
                new int[] { (int) ArchiveProgressType.StartArchive,    6, 15, 3, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  6, 15, 3, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   7, 15, 3, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  7, 15, 3, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   8, 15, 4, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  8, 15, 4, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,   9, 15, 4, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile,  9, 15, 4, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  10, 15, 5, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 10, 15, 5, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  11, 15, 5, 1, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 11, 15, 5, 1, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  12, 15, 6, 1, 3 },
                new int[] { (int) ArchiveProgressType.FinishArchive,  12, 15, 6, 1, 3 },
                new int[] { (int) ArchiveProgressType.StartArchive,   12, 15, 6, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 12, 15, 6, 2, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  13, 15, 6, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 13, 15, 6, 2, 3 },
                new int[] { (int)     ArchiveProgressType.StartFile,  14, 15, 7, 2, 3 },
                new int[] { (int)     ArchiveProgressType.FinishFile, 14, 15, 7, 2, 3 },
                new int[] { (int) ArchiveProgressType.FinishArchive,  14, 15, 7, 2, 3 },
            });

            try
            {
                this.RunCabinetPackUnpack(15, 20 * 1024, 1 * 1024, 130 * 1024, CompressionLevel.None);
            }
            finally
            {
                CompressionTestUtil.ExpectedProgress = null;
            }
        }

        [TestMethod]
        public void CabArchiveSizeParam()
        {
            Console.WriteLine("Testing various values for the maxArchiveSize parameter.");
            this.RunCabinetPackUnpack(5,  1024, 0, Int64.MinValue);
            this.RunCabinetPackUnpack(5,  1024, 0, -1);
            this.RunCabinetPackUnpack(5,    10, 0, 2);
            this.RunCabinetPackUnpack(5,   100, 0, 256);
            this.RunCabinetPackUnpack(5, 24000, 0, 32768);
            this.RunCabinetPackUnpack(5,  1024, 0, Int64.MaxValue);
        }

        [TestMethod]
        public void CabFolderSizeParam()
        {
            Console.WriteLine("Testing various values for the maxFolderSize parameter.");
            this.RunCabinetPackUnpack(5, 10, Int64.MinValue, 0);
            this.RunCabinetPackUnpack(5, 10, -1, 0);
            this.RunCabinetPackUnpack(5, 10, 2, 0);
            this.RunCabinetPackUnpack(5, 10, 16, 0);
            this.RunCabinetPackUnpack(5, 10, 100, 0);
            this.RunCabinetPackUnpack(5, 10, Int64.MaxValue, 0);
        }

        [TestMethod]
        public void CabCompLevelParam()
        {
            Console.WriteLine("Testing various values for the compressionLevel parameter.");
            this.RunCabinetPackUnpack(5, 1024, 0, 0, CompressionLevel.None);
            this.RunCabinetPackUnpack(5, 1024, 0, 0, CompressionLevel.Min);
            this.RunCabinetPackUnpack(5, 1024, 0, 0, CompressionLevel.Normal);
            this.RunCabinetPackUnpack(5, 1024, 0, 0, CompressionLevel.Max);
            this.RunCabinetPackUnpack(5, 1024, 0, 0, (CompressionLevel) ((int) CompressionLevel.None - 1));
            this.RunCabinetPackUnpack(5, 1024, 0, 0, (CompressionLevel) ((int) CompressionLevel.Max + 1));
            this.RunCabinetPackUnpack(5, 1024, 0, 0, (CompressionLevel) Int32.MinValue);
            this.RunCabinetPackUnpack(5, 1024, 0, 0, (CompressionLevel) Int32.MaxValue);
        }

        [TestMethod]
        public void CabEngineNullParams()
        {
            string[] testFiles = new string[] { "test.txt" };
            ArchiveFileStreamContext streamContext = new ArchiveFileStreamContext("test.cab", null, null);

            using (CabEngine cabEngine = new CabEngine())
            {
                cabEngine.CompressionLevel = CompressionLevel.None;

                CompressionTestUtil.TestCompressionEngineNullParams(
                    cabEngine, streamContext, testFiles);
            }
        }

        [TestMethod]
        public void CabBadPackStreamContexts()
        {
            string[] testFiles = new string[] { "test.txt" };
            CompressionTestUtil.GenerateRandomFile(testFiles[0], 0, 20000);

            using (CabEngine cabEngine = new CabEngine())
            {
                cabEngine.CompressionLevel = CompressionLevel.None;

                CompressionTestUtil.TestBadPackStreamContexts(cabEngine, "test.cab", testFiles);
            }
        }

        [TestMethod]
        public void CabEngineNoTempFileTest()
        {
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("testnotemp.txt", 0, txtSize);

            ArchiveFileStreamContext streamContext = new ArchiveFileStreamContext("testnotemp.cab", null, null);

            using (CabEngine cabEngine = new CabEngine())
            {
                cabEngine.UseTempFiles = false;
                cabEngine.Pack(streamContext, new string[] { "testnotemp.txt" });
            }

            new CabInfo("testnotemp.cab").UnpackFile("testnotemp.txt", "testnotemp2.txt");
            Assert.AreEqual(txtSize, new FileInfo("testnotemp2.txt").Length);
        }

        [TestMethod]
        public void CabExtractorIsCabinet()
        {
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("test.txt", 0, txtSize);
            new CabInfo("test.cab").PackFiles(null, new string[] { "test.txt" }, null);
            using (CabEngine cabEngine = new CabEngine())
            {
                bool isCab;
                using (Stream fileStream = File.OpenRead("test.txt"))
                {
                    isCab = cabEngine.IsArchive(fileStream);
                }
                Assert.IsFalse(isCab);
                using (Stream cabStream = File.OpenRead("test.cab"))
                {
                    isCab = cabEngine.IsArchive(cabStream);
                }
                Assert.IsTrue(isCab);
                using (Stream cabStream = File.OpenRead("test.cab"))
                {
                    using (Stream fileStream = new FileStream("test.txt", FileMode.Open, FileAccess.ReadWrite))
                    {
                        fileStream.Seek(0, SeekOrigin.End);
                        byte[] buf = new byte[1024];
                        int count;
                        while ((count = cabStream.Read(buf, 0, buf.Length)) > 0)
                        {
                            fileStream.Write(buf, 0, count);
                        }
                        fileStream.Seek(0, SeekOrigin.Begin);
                        isCab = cabEngine.IsArchive(fileStream);
                    }
                }
                Assert.IsFalse(isCab);
                using (Stream fileStream = new FileStream("test.txt", FileMode.Open, FileAccess.ReadWrite))
                {
                    fileStream.Write(new byte[] { (byte) 'M', (byte) 'S', (byte) 'C', (byte) 'F' }, 0, 4);
                    fileStream.Seek(0, SeekOrigin.Begin);
                    isCab = cabEngine.IsArchive(fileStream);
                }
                Assert.IsFalse(isCab);
            }
        }

        [TestMethod]
        public void CabExtractorFindOffset()
        {
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("test.txt", 0, txtSize);
            new CabInfo("test.cab").PackFiles(null, new string[] { "test.txt" }, null);
            using (CabEngine cabEngine = new CabEngine())
            {
                long offset;
                using (Stream fileStream = File.OpenRead("test.txt"))
                {
                    offset = cabEngine.FindArchiveOffset(fileStream);
                }
                Assert.AreEqual<long>(-1, offset);
                using (Stream cabStream = File.OpenRead("test.cab"))
                {
                    using (Stream fileStream = new FileStream("test.txt", FileMode.Open, FileAccess.ReadWrite))
                    {
                        fileStream.Seek(0, SeekOrigin.End);
                        byte[] buf = new byte[1024];
                        int count;
                        while ((count = cabStream.Read(buf, 0, buf.Length)) > 0)
                        {
                            fileStream.Write(buf, 0, count);
                        }
                        fileStream.Seek(0, SeekOrigin.Begin);
                        offset = cabEngine.FindArchiveOffset(fileStream);
                    }
                }
                Assert.AreEqual<long>(txtSize, offset);
            }
        }

        [TestMethod]
        public void CabExtractorGetFiles()
        {
            IList<ArchiveFileInfo> fileInfo;
            CabInfo cabInfo = new CabInfo("testgetfiles.cab");
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("testgetfiles0.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("testgetfiles1.txt", 1, txtSize);
            cabInfo.PackFiles(null, new string[] { "testgetfiles0.txt", "testgetfiles1.txt" }, null);
            using (CabEngine cabEngine = new CabEngine())
            {
                IList<string> files;
                using (Stream cabStream = File.OpenRead("testgetfiles.cab"))
                {
                    files = cabEngine.GetFiles(cabStream);
                }
                Assert.IsNotNull(files);
                Assert.AreEqual<int>(2, files.Count);
                Assert.AreEqual<string>("testgetfiles0.txt", files[0]);
                Assert.AreEqual<string>("testgetfiles1.txt", files[1]);

                using (Stream cabStream = File.OpenRead("testgetfiles.cab"))
                {
                    files = cabEngine.GetFiles(new ArchiveFileStreamContext("testgetfiles.cab"), null);
                }
                Assert.IsNotNull(files);
                Assert.AreEqual<int>(2, files.Count);
                Assert.AreEqual<string>("testgetfiles0.txt", files[0]);
                Assert.AreEqual<string>("testgetfiles1.txt", files[1]);

                using (Stream cabStream = File.OpenRead("testgetfiles.cab"))
                {
                    fileInfo = cabEngine.GetFileInfo(cabStream);
                }
                Assert.IsNotNull(fileInfo);
                Assert.AreEqual<int>(2, fileInfo.Count);
                Assert.AreEqual<string>("testgetfiles0.txt", fileInfo[0].Name);
                Assert.AreEqual<string>("testgetfiles1.txt", fileInfo[1].Name);
                using (Stream cabStream = File.OpenRead("testgetfiles.cab"))
                {
                    fileInfo = cabEngine.GetFileInfo(new ArchiveFileStreamContext("testgetfiles.cab"),  null);
                }
                Assert.IsNotNull(fileInfo);
                Assert.AreEqual<int>(2, fileInfo.Count);
                Assert.AreEqual<string>("testgetfiles0.txt", fileInfo[0].Name);
                Assert.AreEqual<string>("testgetfiles1.txt", fileInfo[1].Name);
            }

            fileInfo = this.RunCabinetPackUnpack(15, 20 * 1024, 1 * 1024, 130 * 1024);
            Assert.IsNotNull(fileInfo);
            Assert.AreEqual<int>(15, fileInfo.Count);
            for (int i = 0; i < fileInfo.Count; i++)
            {
                Assert.IsNull(fileInfo[i].Archive);
                Assert.AreEqual<string>(TEST_FILENAME_PREFIX + i + ".txt", fileInfo[i].Name);
                Assert.IsTrue(DateTime.Now - fileInfo[i].LastWriteTime < new TimeSpan(0, 1, 0));
            }
        }

        [TestMethod]
        public void CabExtractorExtract()
        {
            int txtSize = 40960;
            CabInfo cabInfo = new CabInfo("test.cab");
            CompressionTestUtil.GenerateRandomFile("test0.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("test1.txt", 1, txtSize);
            cabInfo.PackFiles(null, new string[] { "test0.txt", "test1.txt" }, null);
            using (CabEngine cabEngine = new CabEngine())
            {
                using (Stream cabStream = File.OpenRead("test.cab"))
                {
                    using (Stream exStream = cabEngine.Unpack(cabStream, "test0.txt"))
                    {
                        string str = new StreamReader(exStream).ReadToEnd();
                        string expected = new StreamReader("test0.txt").ReadToEnd();
                        Assert.AreEqual<string>(expected, str);
                    }
                    cabStream.Seek(0, SeekOrigin.Begin);
                    using (Stream exStream = cabEngine.Unpack(cabStream, "test1.txt"))
                    {
                        string str = new StreamReader(exStream).ReadToEnd();
                        string expected = new StreamReader("test1.txt").ReadToEnd();
                        Assert.AreEqual<string>(expected, str);
                    }
                }
                using (Stream txtStream = File.OpenRead("test0.txt"))
                {
                    Exception caughtEx = null;
                    try
                    {
                        cabEngine.Unpack(txtStream, "test0.txt");
                    }
                    catch (Exception ex) { caughtEx = ex; }
                    Assert.IsInstanceOfType(caughtEx, typeof(CabException));
                    Assert.AreEqual<int>(2, ((CabException) caughtEx).Error);
                    Assert.AreEqual<int>(0, ((CabException) caughtEx).ErrorCode);
                    Assert.AreEqual<string>("Cabinet file does not have the correct format.", caughtEx.Message);
                }
            }
        }

        [TestMethod]
        public void CabBadUnpackStreamContexts()
        {
            int txtSize = 40960;
            CabInfo cabInfo = new CabInfo("test2.cab");
            CompressionTestUtil.GenerateRandomFile("cabtest-0.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("cabtest-1.txt", 1, txtSize);
            cabInfo.PackFiles(null, new string[] { "cabtest-0.txt", "cabtest-1.txt" }, null);

            using (CabEngine cabEngine = new CabEngine())
            {
                CompressionTestUtil.TestBadUnpackStreamContexts(cabEngine, "test2.cab");
            }
        }

        [TestMethod]
        public void CabinetExtractUpdate()
        {
            int fileCount = 5, fileSize = 2048;
            string dirA = String.Format("{0}-{1}-A", fileCount, fileSize);
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
            Directory.CreateDirectory(dirA);
            string dirB = String.Format("{0}-{1}-B", fileCount, fileSize);
            if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);

            string[] files = new string[fileCount];
            for (int iFile = 0; iFile < fileCount; iFile++)
            {
                files[iFile] = "â‚¬" + iFile + ".txt";
                CompressionTestUtil.GenerateRandomFile(Path.Combine(dirA, files[iFile]), iFile, fileSize);
            }

            CabInfo cabInfo = new CabInfo("testupdate.cab");
            cabInfo.Pack(dirA);
            cabInfo.Unpack(dirB);

            DateTime originalTime = File.GetLastWriteTime(Path.Combine(dirA, "â‚¬1.txt"));
            DateTime pastTime = originalTime - new TimeSpan(0, 5, 0);
            DateTime futureTime = originalTime + new TimeSpan(0, 5, 0);

            using (CabEngine cabEngine = new CabEngine())
            {
                string cabName = "testupdate.cab";
                ArchiveFileStreamContext streamContext = new ArchiveFileStreamContext(cabName, dirB, null);
                streamContext.ExtractOnlyNewerFiles = true;

                Assert.AreEqual<bool>(true, streamContext.ExtractOnlyNewerFiles);
                Assert.IsNotNull(streamContext.ArchiveFiles);
                Assert.AreEqual<int>(1, streamContext.ArchiveFiles.Count);
                Assert.AreEqual<string>(cabName, streamContext.ArchiveFiles[0]);
                Assert.AreEqual<string>(dirB, streamContext.Directory);

                File.SetLastWriteTime(Path.Combine(dirB, "â‚¬1.txt"), futureTime);
                cabEngine.Unpack(streamContext, null);
                Assert.IsTrue(File.GetLastWriteTime(Path.Combine(dirB, "â‚¬1.txt")) - originalTime > new TimeSpan(0, 4, 55));

                File.SetLastWriteTime(Path.Combine(dirB, "â‚¬1.txt"), pastTime);
                File.SetLastWriteTime(Path.Combine(dirB, "â‚¬2.txt"), pastTime);
                File.SetAttributes(Path.Combine(dirB, "â‚¬2.txt"), FileAttributes.ReadOnly);
                File.SetAttributes(Path.Combine(dirB, "â‚¬2.txt"), FileAttributes.Hidden);
                File.SetAttributes(Path.Combine(dirB, "â‚¬2.txt"), FileAttributes.System);

                cabEngine.Unpack(streamContext, null);
                Assert.IsTrue((File.GetLastWriteTime(Path.Combine(dirB, "â‚¬1.txt")) - originalTime).Duration() < new TimeSpan(0, 0, 5));

                // Just test the rest of the streamContext properties here.
                IDictionary<string, string> testMap = new Dictionary<string, string>();
                streamContext = new ArchiveFileStreamContext(cabName, dirB, testMap);
                Assert.AreSame(testMap, streamContext.Files);

                Assert.IsFalse(streamContext.EnableOffsetOpen);
                streamContext.EnableOffsetOpen = true;
                Assert.IsTrue(streamContext.EnableOffsetOpen);
                streamContext = new ArchiveFileStreamContext(cabName, ".", testMap);
                Assert.AreEqual<string>(".", streamContext.Directory);
                string[] testArchiveFiles = new string[] { cabName };
                streamContext = new ArchiveFileStreamContext(testArchiveFiles, ".", testMap);
                Assert.AreSame(testArchiveFiles, streamContext.ArchiveFiles);
            }
        }

        [TestMethod]
        public void CabinetOffset()
        {
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("test.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("base.txt", 1, 2 * txtSize + 4);

            ArchiveFileStreamContext streamContext = new ArchiveFileStreamContext("base.txt", null, null);
            streamContext.EnableOffsetOpen = true;

            using (CabEngine cabEngine = new CabEngine())
            {
                cabEngine.Pack(streamContext, new string[] { "test.txt" });
            }

            Assert.IsTrue(new FileInfo("base.txt").Length > 2 * txtSize + 4);

            string saveText;
            using (Stream txtStream = File.OpenRead("test.txt"))
            {
                saveText = new StreamReader(txtStream).ReadToEnd();
            }
            File.Delete("test.txt");

            using (CabEngine cex = new CabEngine())
            {
                cex.Unpack(streamContext, null);
            }
            string testText;
            using (Stream txtStream = File.OpenRead("test.txt"))
            {
                testText = new StreamReader(txtStream).ReadToEnd();
            }
            Assert.AreEqual<string>(saveText, testText);
        }

        [TestMethod]
        public void CabinetUtfPaths()
        {
            string[] files = new string[]
            {
                "어그리먼트送信ポート1ßà_Agreement.txt",
                "콘토소ßà_MyProfile.txt",
                "파트너1ßà_PartnerProfile.txt",
            };

            string dirA = "utf8-A";
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
            Directory.CreateDirectory(dirA);
            string dirB = "utf8-B";
            if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);

            int txtSize = 1024;
            CompressionTestUtil.GenerateRandomFile(Path.Combine(dirA, files[0]), 0, txtSize);
            CompressionTestUtil.GenerateRandomFile(Path.Combine(dirA, files[1]), 1, txtSize);
            CompressionTestUtil.GenerateRandomFile(Path.Combine(dirA, files[2]), 2, txtSize);

            ArchiveFileStreamContext streamContextA = new ArchiveFileStreamContext("utf8.cab", dirA, null);
            using (CabEngine cabEngine = new CabEngine())
            {
                cabEngine.Pack(streamContextA, files);
            }

            ArchiveFileStreamContext streamContextB = new ArchiveFileStreamContext("utf8.cab", dirB, null);
            using (CabEngine cex = new CabEngine())
            {
                cex.Unpack(streamContextB, null);
            }

            bool directoryMatch = CompressionTestUtil.CompareDirectories(dirA, dirB);
            Assert.IsTrue(directoryMatch,
                "Testing whether cabinet output directory matches input directory.");
        }

        [TestMethod]
        public void CabInfoProperties()
        {
            Exception caughtEx;
            CabInfo cabInfo = new CabInfo("test.cab");
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("test00.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("test01.txt", 1, txtSize);
            cabInfo.PackFiles(null, new string[] { "test00.txt", "test01.txt" }, null);

            Assert.AreEqual<string>(new FileInfo("test.cab").Directory.FullName, cabInfo.Directory.FullName, "CabInfo.FullName");
            Assert.AreEqual<string>(new FileInfo("test.cab").DirectoryName, cabInfo.DirectoryName, "CabInfo.DirectoryName");
            Assert.AreEqual<long>(new FileInfo("test.cab").Length, cabInfo.Length, "CabInfo.Length");
            Assert.AreEqual<string>("test.cab", cabInfo.Name, "CabInfo.Name");
            Assert.AreEqual<string>(new FileInfo("test.cab").FullName, cabInfo.ToString(), "CabInfo.ToString()");
            cabInfo.CopyTo("test3.cab");
            caughtEx = null;
            try
            {
                cabInfo.CopyTo("test3.cab");
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(IOException), "CabInfo.CopyTo() caught exception: " + caughtEx);
            cabInfo.CopyTo("test3.cab", true);
            cabInfo.MoveTo("test4.cab");
            Assert.AreEqual<string>("test4.cab", cabInfo.Name);
            Assert.IsTrue(cabInfo.Exists, "CabInfo.Exists()");
            Assert.IsTrue(cabInfo.IsValid(), "CabInfo.IsValid");
            cabInfo.Delete();
            Assert.IsFalse(cabInfo.Exists, "!CabInfo.Exists()");
        }

        [TestMethod]
        public void CabInfoNullParams()
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
                files[iFile] = "cabinfo-" + iFile + ".txt";
                CompressionTestUtil.GenerateRandomFile(Path.Combine(dirA, files[iFile]), iFile, fileSize);
            }

            CabInfo cabInfo = new CabInfo("testnull.cab");

            CompressionTestUtil.TestArchiveInfoNullParams(cabInfo, dirA, dirB, files);
        }

        [TestMethod]
        public void CabInfoGetFiles()
        {
            IList<CabFileInfo> fileInfo;
            CabInfo cabInfo = new CabInfo("test.cab");
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("testinfo0.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("testinfo1.txt", 1, txtSize);
            cabInfo.PackFiles(null, new string[] { "testinfo0.txt", "testinfo1.txt" }, null);

            fileInfo = cabInfo.GetFiles();
            Assert.IsNotNull(fileInfo);
            Assert.AreEqual<int>(2, fileInfo.Count);
            Assert.AreEqual<string>("testinfo0.txt", fileInfo[0].Name);
            Assert.AreEqual<string>("testinfo1.txt", fileInfo[1].Name);

            fileInfo = cabInfo.GetFiles("*.txt");
            Assert.IsNotNull(fileInfo);
            Assert.AreEqual<int>(2, fileInfo.Count);
            Assert.AreEqual<string>("testinfo0.txt", fileInfo[0].Name);
            Assert.AreEqual<string>("testinfo1.txt", fileInfo[1].Name);

            fileInfo = cabInfo.GetFiles("testinfo1.txt");
            Assert.IsNotNull(fileInfo);
            Assert.AreEqual<int>(1, fileInfo.Count);
            Assert.AreEqual<string>("testinfo1.txt", fileInfo[0].Name);
        }

        [TestMethod]
        public void CabInfoCompressExtract()
        {
            int fileCount = 10, fileSize = 1024;
            string dirA = String.Format("{0}-{1}-A", fileCount, fileSize);
            if (Directory.Exists(dirA)) Directory.Delete(dirA, true);
            Directory.CreateDirectory(dirA);
            Directory.CreateDirectory(Path.Combine(dirA, "sub"));
            string dirB = String.Format("{0}-{1}-B", fileCount, fileSize);
            if (Directory.Exists(dirB)) Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);

            string[] files = new string[fileCount];
            for (int iFile = 0; iFile < fileCount; iFile++)
            {
                files[iFile] = "â‚¬" + iFile + ".txt";
                CompressionTestUtil.GenerateRandomFile(Path.Combine(dirA, files[iFile]), iFile, fileSize);
            }
            CompressionTestUtil.GenerateRandomFile(Path.Combine(Path.Combine(dirA, "sub"), "â‚¬-.txt"), fileCount + 1, fileSize);

            CabInfo cabInfo = new CabInfo("test.cab");
            cabInfo.Pack(dirA);
            cabInfo.Unpack(dirB);
            bool directoryMatch = CompressionTestUtil.CompareDirectories(dirA, dirB);
            Assert.IsFalse(directoryMatch,
                "Testing whether cabinet output directory matches input directory.");
            Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);
            cabInfo.Pack(dirA, true, CompressionLevel.Normal, null);
            cabInfo.Unpack(dirB);
            directoryMatch = CompressionTestUtil.CompareDirectories(dirA, dirB);
            Assert.IsTrue(directoryMatch,
                "Testing whether cabinet output directory matches input directory.");
            Directory.Delete(dirB, true);
            Directory.Delete(Path.Combine(dirA, "sub"), true);
            Directory.CreateDirectory(dirB);
            cabInfo.Delete();

            cabInfo.PackFiles(dirA, files, null);
            cabInfo.UnpackFiles(files, dirB, null);
            directoryMatch = CompressionTestUtil.CompareDirectories(dirA, dirB);
            Assert.IsTrue(directoryMatch,
                "Testing whether cabinet output directory matches input directory.");
            Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);
            cabInfo.Delete();

            IDictionary<string, string> testMap = new Dictionary<string, string>(files.Length);
            for (int iFile = 0; iFile < fileCount; iFile++)
            {
                testMap[files[iFile] + ".key"] = files[iFile];
            }
            cabInfo.PackFileSet(dirA, testMap);
            cabInfo.UnpackFileSet(testMap, dirB);
            directoryMatch = CompressionTestUtil.CompareDirectories(dirA, dirB);
            Assert.IsTrue(directoryMatch,
                "Testing whether cabinet output directory matches input directory.");
            Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);

            testMap.Remove(files[1] + ".key");
            cabInfo.UnpackFileSet(testMap, dirB);
            directoryMatch = CompressionTestUtil.CompareDirectories(dirA, dirB);
            Assert.IsFalse(directoryMatch,
                "Testing whether cabinet output directory matches input directory.");
            Directory.Delete(dirB, true);
            Directory.CreateDirectory(dirB);
            cabInfo.Delete();

            cabInfo.PackFiles(dirA, files, null);
            cabInfo.UnpackFile("â‚¬2.txt", Path.Combine(dirB, "test.txt"));
            Assert.IsTrue(File.Exists(Path.Combine(dirB, "test.txt")));
            Assert.AreEqual<int>(1, Directory.GetFiles(dirB).Length);
        }

        [TestMethod]
        public void CabFileInfoProperties()
        {
            CabInfo cabInfo = new CabInfo("test.cab");
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("test00.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("test01.txt", 1, txtSize);
            File.SetAttributes("test01.txt", FileAttributes.ReadOnly | FileAttributes.Archive);
            DateTime testTime = File.GetLastWriteTime("test01.txt");
            cabInfo.PackFiles(null, new string[] { "test00.txt", "test01.txt" }, null);
            File.SetAttributes("test01.txt", FileAttributes.Archive);

            CabFileInfo cfi = new CabFileInfo(cabInfo, "test01.txt");
            Assert.AreEqual(cabInfo.FullName, cfi.CabinetName);
            Assert.AreEqual<int>(0, ((CabFileInfo) cfi).CabinetFolderNumber);
            Assert.AreEqual<string>(Path.Combine(cabInfo.FullName, "test01.txt"), cfi.FullName);
            cfi = new CabFileInfo(cabInfo, "test01.txt");
            Assert.IsTrue(cfi.Exists);
            cfi = new CabFileInfo(cabInfo, "test01.txt");
            Assert.AreEqual<long>(txtSize, cfi.Length);
            cfi = new CabFileInfo(cabInfo, "test00.txt");
            Assert.AreEqual<FileAttributes>(FileAttributes.Archive, cfi.Attributes);
            cfi = new CabFileInfo(cabInfo, "test01.txt");
            Assert.AreEqual<FileAttributes>(FileAttributes.ReadOnly | FileAttributes.Archive, cfi.Attributes);
            cfi = new CabFileInfo(cabInfo, "test01.txt");
            Assert.IsTrue((testTime - cfi.LastWriteTime).Duration() < new TimeSpan(0, 0, 5));
            Assert.AreEqual<string>(Path.Combine(cabInfo.FullName, "test01.txt"), cfi.ToString());
            cfi.CopyTo("testcopy.txt");
            Assert.IsTrue(File.Exists("testCopy.txt"));
            Assert.AreEqual<long>(cfi.Length, new FileInfo("testCopy.txt").Length);

            Exception caughtEx = null;
            try
            {
                cfi.CopyTo("testcopy.txt", false);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(IOException));
        }

        [TestMethod]
        public void CabFileInfoOpenText()
        {
            CabInfo cabInfo = new CabInfo("test.cab");
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("test00.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("test01.txt", 1, txtSize);

            string expectedText = File.ReadAllText("test01.txt");
            
            cabInfo.PackFiles(null, new string[] { "test00.txt", "test01.txt" }, null);

            CabFileInfo cfi = new CabFileInfo(cabInfo, "test01.txt");
            using (StreamReader cabFileReader = cfi.OpenText())
            {
                string text = cabFileReader.ReadToEnd();
                Assert.AreEqual(expectedText, text);

                // Check the assumption that the cab can't be deleted while a stream is open.
                Exception caughtEx = null;
                try
                {
                    File.Delete(cabInfo.FullName);
                }
                catch (Exception ex)
                {
                    caughtEx = ex;
                }

                Assert.IsInstanceOfType(caughtEx, typeof(IOException));
            }

            // Ensure all streams are closed after disposing of the StreamReader returned by OpenText.
            File.Delete(cabInfo.FullName);
        }

        [TestMethod]
        public void CabFileInfoNullParams()
        {
            Exception caughtEx;
            CabInfo cabInfo = new CabInfo("test.cab");
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("test00.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("test01.txt", 1, txtSize);
            cabInfo.PackFiles(null, new string[] { "test00.txt", "test01.txt" }, null);
            CabFileInfo cfi = new CabFileInfo(cabInfo, "test01.txt");

            caughtEx = null;
            try
            {
                new CabFileInfo(null, "test00.txt");
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException));
            caughtEx = null;
            try
            {
                new CabFileInfo(cabInfo, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException));
            caughtEx = null;
            try
            {
                cfi.CopyTo(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException));
        }

        [TestMethod]
        public void CabInfoSerialization()
        {
            CabInfo cabInfo = new CabInfo("testser.cab");
            int txtSize = 10240;
            CompressionTestUtil.GenerateRandomFile("testser00.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("testser01.txt", 1, txtSize);
            cabInfo.PackFiles(null, new string[] { "testser00.txt", "testser01.txt" }, null);
            ArchiveFileInfo cfi = cabInfo.GetFiles()[1];

            MemoryStream memStream = new MemoryStream();

            BinaryFormatter formatter = new BinaryFormatter();

            memStream.Seek(0, SeekOrigin.Begin);
            formatter.Serialize(memStream, cabInfo);
            memStream.Seek(0, SeekOrigin.Begin);
            CabInfo cabInfo2 = (CabInfo) formatter.Deserialize(memStream);
            Assert.AreEqual<string>(cabInfo.FullName, cabInfo2.FullName);

            memStream.Seek(0, SeekOrigin.Begin);
            formatter.Serialize(memStream, cfi);
            memStream.Seek(0, SeekOrigin.Begin);
            CabFileInfo cfi2 = (CabFileInfo) formatter.Deserialize(memStream);
            Assert.AreEqual<string>(cfi.FullName, cfi2.FullName);
            Assert.AreEqual<long>(cfi.Length, cfi2.Length);

            CabException cabEx = new CabException();
            memStream.Seek(0, SeekOrigin.Begin);
            formatter.Serialize(memStream, cabEx);
            memStream.Seek(0, SeekOrigin.Begin);
            formatter.Deserialize(memStream);

            cabEx = new CabException("Test exception.", null);
            Assert.AreEqual<string>("Test exception.", cabEx.Message);
        }

        [TestMethod]
        public void CabFileStreamContextNullParams()
        {
            ArchiveFileStreamContext streamContext = null;
            Exception caughtEx = null;
            try
            {
                streamContext = new ArchiveFileStreamContext(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Passing null to constructor.");
            caughtEx = null;
            try
            {
                streamContext = new ArchiveFileStreamContext(new string[] { }, "testDir", new Dictionary<string, string>());
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Passing 0-length array to constructor.");
            caughtEx = null;
            try
            {
                streamContext = new ArchiveFileStreamContext(new string[] { "test.cab" }, null, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx);
        }

        [TestMethod]
        public void CabinetTruncateOnCreate()
        {
            CabInfo cabInfo = new CabInfo("testtruncate.cab");
            int txtSize = 20240;
            CompressionTestUtil.GenerateRandomFile("testtruncate0.txt", 0, txtSize);
            CompressionTestUtil.GenerateRandomFile("testtruncate1.txt", 1, txtSize);
            cabInfo.PackFiles(null, new string[] { "testtruncate0.txt", "testtruncate1.txt" }, null);

            long size1 = cabInfo.Length;

            txtSize /= 5;
            CompressionTestUtil.GenerateRandomFile("testtruncate2.txt", 2, txtSize);
            CompressionTestUtil.GenerateRandomFile("testtruncate3.txt", 3, txtSize);
            cabInfo.PackFiles(null, new string[] { "testtruncate2.txt", "testtruncate3.txt" }, null);

            // The newly created cab file should be smaller than before.
            Assert.AreNotEqual<long>(size1, cabInfo.Length, "Checking that cabinet file got truncated when creating a smaller cab in-place.");
        }

        [TestMethod]
        public void CabTruncatedArchive()
        {
            CabInfo cabInfo = new CabInfo("test-t.cab");
            CompressionTestUtil.GenerateRandomFile("cabtest-0.txt", 0, 5);
            CompressionTestUtil.GenerateRandomFile("cabtest-1.txt", 1, 5);
            cabInfo.PackFiles(null, new string[] { "cabtest-0.txt", "cabtest-1.txt" }, null);

            CompressionTestUtil.TestTruncatedArchive(cabInfo, typeof(CabException));
        }
        private const string TEST_FILENAME_PREFIX = "\x20AC";

        private IList<ArchiveFileInfo> RunCabinetPackUnpack(int fileCount, long fileSize)
        {
            return RunCabinetPackUnpack(fileCount, fileSize, 0, 0);
        }
        private IList<ArchiveFileInfo> RunCabinetPackUnpack(int fileCount, long fileSize,
            long maxFolderSize, long maxArchiveSize)
        {
            return this.RunCabinetPackUnpack(fileCount, fileSize, maxFolderSize, maxArchiveSize, CompressionLevel.Normal);
        }
        private IList<ArchiveFileInfo> RunCabinetPackUnpack(int fileCount, long fileSize,
            long maxFolderSize, long maxArchiveSize, CompressionLevel compLevel)
        {
            Console.WriteLine("Creating cabinet with {0} files of size {1}",
                fileCount, fileSize);
            Console.WriteLine("MaxFolderSize={0}, MaxArchiveSize={1}, CompressionLevel={2}",
                maxFolderSize, maxArchiveSize, compLevel);

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

            string[] archiveNames = new string[100];
            for (int i = 0; i < archiveNames.Length; i++)
            {
                archiveNames[i] = String.Format("{0}-{1}{2}{3}.cab", fileCount, fileSize,
                    (i == 0 ? "" : "-"), (i == 0 ? "" : i.ToString()));
            }

            string progressTextFile = String.Format("progress_{0}-{1}.txt", fileCount, fileSize);
            CompressionTestUtil testUtil = new CompressionTestUtil(progressTextFile);

            IList<ArchiveFileInfo> fileInfo;
            using (CabEngine cabEngine = new CabEngine())
            {
                cabEngine.CompressionLevel = compLevel;

                File.AppendAllText(progressTextFile,
                    "\r\n\r\n====================================================\r\nCREATE\r\n\r\n");
                cabEngine.Progress += testUtil.PrintArchiveProgress;

                OptionStreamContext streamContext = new OptionStreamContext(archiveNames, dirA, null);
                if (maxFolderSize == 1)
                {
                    streamContext.OptionHandler =
                        delegate(string optionName, object[] parameters)
                        {
                            if (optionName == "nextFolder") return true;
                            return null;
                        };
                }
                else if (maxFolderSize > 1)
                {
                    streamContext.OptionHandler =
                        delegate(string optionName, object[] parameters)
                        {
                            if (optionName == "maxFolderSize") return maxFolderSize;
                            return null;
                        };
                }
                cabEngine.Pack(streamContext, files, maxArchiveSize);

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

                Console.WriteLine("Listing cabinet with {0} files of size {1}",
                    fileCount, fileSize);
                File.AppendAllText(progressTextFile, "\r\n\r\nLIST\r\n\r\n");
                fileInfo = cabEngine.GetFileInfo(
                    new ArchiveFileStreamContext(createdArchiveNames, null, null), null);

                Assert.AreEqual<int>(fileCount, fileInfo.Count);
                if (fileCount > 0)
                {
                    int folders = ((CabFileInfo) fileInfo[fileInfo.Count - 1]).CabinetFolderNumber + 1;
                    if (maxFolderSize == 1)
                    {
                        Assert.AreEqual<int>(fileCount, folders);
                    }
                }

                Console.WriteLine("Extracting cabinet with {0} files of size {1}",
                    fileCount, fileSize);
                File.AppendAllText(progressTextFile, "\r\n\r\nEXTRACT\r\n\r\n");
                cabEngine.Unpack(new ArchiveFileStreamContext(createdArchiveNames, dirB, null), null);
            }

            bool directoryMatch = CompressionTestUtil.CompareDirectories(dirA, dirB);
            Assert.IsTrue(directoryMatch,
                "Testing whether cabinet output directory matches input directory.");

            return fileInfo;
        }
    }
}
