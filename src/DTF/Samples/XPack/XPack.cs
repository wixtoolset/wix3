
namespace Microsoft.Deployment.Samples.XPack
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Deployment.Compression;

    public class XPack
    {
        public static void Usage(TextWriter writer)
        {
            writer.WriteLine("Usage: XPack /P <archive.cab> <directory>");
            writer.WriteLine("Usage: XPack /P <archive.zip> <directory>");
            writer.WriteLine();
            writer.WriteLine("Packs all files in a directory tree into an archive,");
            writer.WriteLine("using either the cab or zip format. Any existing archive");
            writer.WriteLine("with the same name will be overwritten.");
            writer.WriteLine();
            writer.WriteLine("Usage: XPack /U <archive.cab> <directory>");
            writer.WriteLine("Usage: XPack /U <archive.zip> <directory>");
            writer.WriteLine();
            writer.WriteLine("Unpacks all files from a cab or zip archive to the");
            writer.WriteLine("specified directory. Any existing files with the same");
            writer.WriteLine("names will be overwritten.");
        }

        public static void Main(string[] args)
        {
            try
            {
                if (args.Length == 3 && args[0].ToUpperInvariant() == "/P")
                {
                    ArchiveInfo a = GetArchive(args[1]);
                    a.Pack(args[2], true, CompressionLevel.Max, ProgressHandler);
                }
                else if (args.Length == 3 && args[0].ToUpperInvariant() == "/U")
                {
                    ArchiveInfo a = GetArchive(args[1]);
                    a.Unpack(args[2], ProgressHandler);
                }
                else
                {
                    Usage(Console.Out);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        private static void ProgressHandler(object source, ArchiveProgressEventArgs e)
        {
            if (e.ProgressType == ArchiveProgressType.StartFile)
            {
                Console.WriteLine(e.CurrentFileName);
            }
        }

        private static ArchiveInfo GetArchive(string name)
        {
            string extension = Path.GetExtension(name).ToUpperInvariant();
            if (extension == ".CAB")
            {
                return new Microsoft.Deployment.Compression.Cab.CabInfo(name);
            }
            else if (extension == ".ZIP")
            {
                return new Microsoft.Deployment.Compression.Zip.ZipInfo(name);
            }
            else
            {
                throw new ArgumentException("Unknown archive file extension: " + extension);
            }
        }
    }
}
