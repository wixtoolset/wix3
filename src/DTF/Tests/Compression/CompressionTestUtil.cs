//-------------------------------------------------------------------------------------------------
// <copyright file="CompressionTestUtil.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Security.Cryptography;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Deployment.Compression;

namespace Microsoft.Deployment.Test
{
    public class CompressionTestUtil
    {
        private static MD5 md5 = new MD5CryptoServiceProvider();

        private string progressTextFile;

        public CompressionTestUtil(string progressTextFile)
        {
            this.progressTextFile = progressTextFile;
        }

        public static IList<int[]> ExpectedProgress
        {
            get { return CompressionTestUtil.expectedProgress; }
            set { CompressionTestUtil.expectedProgress = value; }
        }
        private static IList<int[]> expectedProgress;

        public void PrintArchiveProgress(object source, ArchiveProgressEventArgs e)
        {
            switch (e.ProgressType)
            {
                case ArchiveProgressType.StartFile:
                    {
                        Console.WriteLine("StartFile: {0}", e.CurrentFileName);
                    } break;
                case ArchiveProgressType.FinishFile:
                    {
                        Console.WriteLine("FinishFile: {0}", e.CurrentFileName);
                    } break;
                case ArchiveProgressType.StartArchive:
                    {
                        Console.WriteLine("StartArchive: {0} : {1}", e.CurrentArchiveNumber, e.CurrentArchiveName);
                    } break;
                case ArchiveProgressType.FinishArchive:
                    {
                        Console.WriteLine("FinishArchive: {0} : {1}", e.CurrentArchiveNumber, e.CurrentArchiveName);
                    } break;
            }

            File.AppendAllText(this.progressTextFile, e.ToString().Replace("\n", Environment.NewLine));

            if (CompressionTestUtil.expectedProgress != null &&
                e.ProgressType != ArchiveProgressType.PartialFile &&
                e.ProgressType != ArchiveProgressType.PartialArchive)
            {
                Assert.AreNotEqual<int>(0, CompressionTestUtil.expectedProgress.Count);
                int[] expected = CompressionTestUtil.expectedProgress[0];
                CompressionTestUtil.expectedProgress.RemoveAt(0);
                Assert.AreEqual<ArchiveProgressType>((ArchiveProgressType) expected[0], e.ProgressType, "Checking ProgressType.");
                Assert.AreEqual<int>(expected[1], e.CurrentFileNumber, "Checking CurrentFileNumber.");
                Assert.AreEqual<int>(expected[2], e.TotalFiles, "Checking TotalFiles.");
                Assert.AreEqual<int>(expected[4], e.CurrentArchiveNumber, "Checking CurrentArchiveNumber.");
                Assert.AreEqual<int>(expected[5], e.TotalArchives, "Checking TotalArchives.");
            }
        }

        public static bool CompareDirectories(string dirA, string dirB)
        {
            bool difference = false;
            Console.WriteLine("Comparing directories {0}, {1}", dirA, dirB);

            string[] filesA = Directory.GetFiles(dirA);
            string[] filesB = Directory.GetFiles(dirB);
            for (int iA = 0; iA < filesA.Length; iA++)
            {
                filesA[iA] = Path.GetFileName(filesA[iA]);
            }
            for (int iB = 0; iB < filesB.Length; iB++)
            {
                filesB[iB] = Path.GetFileName(filesB[iB]);
            }
            Array.Sort(filesA);
            Array.Sort(filesB);

            for (int iA = 0, iB = 0; iA < filesA.Length || iB < filesB.Length; )
            {
                int comp;
                if (iA == filesA.Length)
                {
                    comp = 1;
                }
                else if (iB == filesB.Length)
                {
                    comp = -1;
                }
                else
                {
                    comp = String.Compare(filesA[iA], filesB[iB]);
                }
                if (comp < 0)
                {
                    Console.WriteLine("< " + filesA[iA]);
                    difference = true;
                    iA++;
                }
                else if (comp > 0)
                {
                    Console.WriteLine("> " + filesB[iB]);
                    difference = true;
                    iB++;
                }
                else
                {
                    string fileA = Path.Combine(dirA, filesA[iA]);
                    string fileB = Path.Combine(dirB, filesB[iB]);

                    byte[] hashA;
                    byte[] hashB;

                    lock (CompressionTestUtil.md5)
                    {
                        using (Stream fileAStream = File.OpenRead(fileA))
                        {
                            hashA = CompressionTestUtil.md5.ComputeHash(fileAStream);
                        }
                        using (Stream fileBStream = File.OpenRead(fileB))
                        {
                            hashB = CompressionTestUtil.md5.ComputeHash(fileBStream);
                        }
                    }

                    for (int i = 0; i < hashA.Length; i++)
                    {
                        if (hashA[i] != hashB[i])
                        {
                            Console.WriteLine("~  " + filesA[iA]);
                            difference = true;
                            break;
                        }
                    }

                    iA++;
                    iB++;
                }
            }

            string[] dirsA = Directory.GetDirectories(dirA);
            string[] dirsB = Directory.GetDirectories(dirB);
            for (int iA = 0; iA < dirsA.Length; iA++)
            {
                dirsA[iA] = Path.GetFileName(dirsA[iA]);
            }
            for (int iB = 0; iB < dirsB.Length; iB++)
            {
                dirsB[iB] = Path.GetFileName(dirsB[iB]);
            }
            Array.Sort(dirsA);
            Array.Sort(dirsB);

            for (int iA = 0, iB = 0; iA < dirsA.Length || iB < dirsB.Length; )
            {
                int comp;
                if (iA == dirsA.Length)
                {
                    comp = 1;
                }
                else if (iB == dirsB.Length)
                {
                    comp = -1;
                }
                else
                {
                    comp = String.Compare(dirsA[iA], dirsB[iB]);
                }
                if (comp < 0)
                {
                    Console.WriteLine("< {0}\\", dirsA[iA]);
                    difference = true;
                    iA++;
                }
                else if (comp > 0)
                {
                    Console.WriteLine("> {1}\\", dirsB[iB]);
                    difference = true;
                    iB++;
                }
                else
                {
                    string subDirA = Path.Combine(dirA, dirsA[iA]);
                    string subDirB = Path.Combine(dirB, dirsB[iB]);
                    if (!CompressionTestUtil.CompareDirectories(subDirA, subDirB))
                    {
                        difference = true;
                    }
                    iA++;
                    iB++;
                }
            }

            return !difference;
        }


        public static void GenerateRandomFile(string path, int seed, long size)
        {
            Console.WriteLine("Generating random file {0} (seed={1}, size={2})",
                path, seed, size);
            Random random = new Random(seed);
            bool easy = random.Next(2) == 1;
            int chunk = 1024 * random.Next(1, 100);
            using (TextWriter tw = new StreamWriter(
                File.Create(path, 4096), Encoding.ASCII))
            {
                for (long count = 0; count < size; count++)
                {
                    char c = (char) (easy ? random.Next('a', 'b' + 1)
                        : random.Next(32, 127));
                    tw.Write(c);
                    if (--chunk == 0)
                    {
                        chunk = 1024 * random.Next(1, 101);
                        easy = random.Next(2) == 1;
                    }
                }
            }
        }

        public static void TestArchiveInfoNullParams(
            ArchiveInfo archiveInfo,
            string dirA,
            string dirB,
            string[] files)
        {
            Exception caughtEx = null;
            try
            {
                archiveInfo.PackFiles(null, null, files);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.PackFiles(null, files, new string[] { });
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentOutOfRangeException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.PackFileSet(dirA, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.PackFiles(null, files, files);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(FileNotFoundException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.PackFiles(dirA, null, files);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.PackFiles(dirA, files, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Caught exception: " + caughtEx);

            caughtEx = null;
            try
            {
                archiveInfo.CopyTo(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.CopyTo(null, true);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.MoveTo(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.GetFiles(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.UnpackFile(null, "test.txt");
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.UnpackFile("test.txt", null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.UnpackFiles(null, dirB, files);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.UnpackFiles(files, null, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.UnpackFiles(files, null, files);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.UnpackFiles(files, dirB, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.UnpackFiles(files, dirB, new string[] { });
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentOutOfRangeException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                archiveInfo.UnpackFileSet(null, dirB);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
        }

        public static void TestCompressionEngineNullParams(
            CompressionEngine engine,
            ArchiveFileStreamContext streamContext,
            string[] testFiles)
        {
            Exception caughtEx;

            Console.WriteLine("Testing null streamContext.");
            caughtEx = null;
            try
            {
                engine.Pack(null, testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                engine.Pack(null, testFiles, 0);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);

            Console.WriteLine("Testing null files.");
            caughtEx = null;
            try
            {
                engine.Pack(streamContext, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);

            Console.WriteLine("Testing null files.");
            caughtEx = null;
            try
            {
                engine.Pack(streamContext, null, 0);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);


            Console.WriteLine("Testing null stream.");
            caughtEx = null;
            try
            {
                engine.IsArchive(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                engine.FindArchiveOffset(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                engine.GetFiles(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                engine.GetFileInfo(null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                engine.Unpack(null, "testUnpack.txt");
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            Console.WriteLine("Testing null streamContext.");
            caughtEx = null;
            try
            {
                engine.GetFiles(null, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                engine.GetFileInfo(null, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
            caughtEx = null;
            try
            {
                engine.Unpack((IUnpackStreamContext) null, null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(ArgumentNullException), "Caught exception: " + caughtEx);
        }

        public static void TestBadPackStreamContexts(
            CompressionEngine engine, string archiveName, string[] testFiles)
        {
            Exception caughtEx;

            Console.WriteLine("Testing streamContext that returns null from GetName.");
            caughtEx = null;
            try
            {
                engine.Pack(
                    new MisbehavingStreamContext(archiveName, null, null, false, false, true, true, true, true),
                    testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx is FileNotFoundException, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that returns null from OpenArchive.");
            caughtEx = null;
            try
            {
                engine.Pack(
                    new MisbehavingStreamContext(archiveName, null, null, false, true, false, true, true, true),
                    testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx is FileNotFoundException, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that returns null from OpenFile.");
            caughtEx = null;
            try
            {
                engine.Pack(
                    new MisbehavingStreamContext(archiveName, null, null, false, true, true, true, false, true),
                    testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on GetName.");
            caughtEx = null;
            try
            {
                engine.Pack(
                    new MisbehavingStreamContext(archiveName, null, null, true, false, true, true, true, true),
                    testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on OpenArchive.");
            caughtEx = null;
            try
            {
                engine.Pack(
                    new MisbehavingStreamContext(archiveName, null, null, true, true, false, true, true, true),
                    testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on CloseArchive.");
            caughtEx = null;
            try
            {
                engine.Pack(
                    new MisbehavingStreamContext(archiveName, null, null, true, true, true, false, true, true),
                    testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on OpenFile.");
            caughtEx = null;
            try
            {
                engine.Pack(
                    new MisbehavingStreamContext(archiveName, null, null, true, true, true, true, false, true),
                    testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on CloseFile.");
            caughtEx = null;
            try
            {
                engine.Pack(
                    new MisbehavingStreamContext(archiveName, null, null, true, true, true, true, true, false),
                    testFiles);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
        }

        public static void TestBadUnpackStreamContexts(
            CompressionEngine engine, string archiveName)
        {
            Exception caughtEx;

            Console.WriteLine("Testing streamContext that returns null from OpenArchive.");
            caughtEx = null;
            try
            {
                engine.Unpack(new MisbehavingStreamContext(archiveName, null, null, false, true, false, true, true, true), null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsInstanceOfType(caughtEx, typeof(FileNotFoundException), "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that returns null from OpenFile.");
            caughtEx = null;
            try
            {
                engine.Unpack(new MisbehavingStreamContext(archiveName, null, null, false, true, true, true, false, true), null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsNull(caughtEx, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on OpenArchive.");
            caughtEx = null;
            try
            {
                engine.Unpack(new MisbehavingStreamContext(archiveName, null, null, true, true, false, true, true, true), null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on CloseArchive.");
            caughtEx = null;
            try
            {
                engine.Unpack(new MisbehavingStreamContext(archiveName, null, null, true, true, true, false, true, true), null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on OpenFile.");
            caughtEx = null;
            try
            {
                engine.Unpack(new MisbehavingStreamContext(archiveName, null, null, true, true, true, true, false, true), null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
            Console.WriteLine("Testing streamContext that throws on CloseFile.");
            caughtEx = null;
            try
            {
                engine.Unpack(new MisbehavingStreamContext(archiveName, null, null, true, true, true, true, true, false), null);
            }
            catch (Exception ex) { caughtEx = ex; }
            Assert.IsTrue(caughtEx != null && caughtEx.Message == MisbehavingStreamContext.EXCEPTION, "Caught exception: " + caughtEx);
        }

        public static void TestTruncatedArchive(
            ArchiveInfo archiveInfo, Type expectedExceptionType)
        {
            for (long len = archiveInfo.Length - 1; len >= 0; len--)
            {
                string testArchive = String.Format("{0}.{1:d06}",
                    archiveInfo.FullName, len);
                if (File.Exists(testArchive))
                {
                    File.Delete(testArchive);
                }

                archiveInfo.CopyTo(testArchive);
                using (FileStream truncateStream =
                    File.Open(testArchive, FileMode.Open, FileAccess.ReadWrite))
                {
                    truncateStream.SetLength(len);
                }

                ArchiveInfo testArchiveInfo = (ArchiveInfo) archiveInfo.GetType()
                    .GetConstructor(new Type[] { typeof(string) }).Invoke(new object[] { testArchive });

                Exception caughtEx = null;
                try
                {
                    testArchiveInfo.GetFiles();
                }
                catch (Exception ex) { caughtEx = ex; }
                File.Delete(testArchive);

                if (caughtEx != null)
                {
                    Assert.IsInstanceOfType(caughtEx, expectedExceptionType,
                        String.Format("Caught exception listing archive truncated to {0}/{1} bytes",
                        len, archiveInfo.Length));
                }
            }
        }
    }
}
