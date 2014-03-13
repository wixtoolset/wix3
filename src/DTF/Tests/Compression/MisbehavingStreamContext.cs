//-------------------------------------------------------------------------------------------------
// <copyright file="MisbehavingStreamContext.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.IO;
using System.Collections.Generic;
using Microsoft.Deployment.Compression;

namespace Microsoft.Deployment.Test
{
    public class MisbehavingStreamContext : ArchiveFileStreamContext
    {
        public const string EXCEPTION = "Test exception.";

        private bool throwEx;
        private bool getName;
        private bool openArchive;
        private bool closeArchive;
        private bool openFile;
        private bool closeFile;
        private int closeFileCount;

        public MisbehavingStreamContext(
            string cabinetFile,
            string directory,
            IDictionary<string, string> files,
            bool throwEx,
            bool getName,
            bool openArchive,
            bool closeArchive,
            bool openFile,
            bool closeFile)
            : base(cabinetFile, directory, files)
        {
            this.throwEx = throwEx;
            this.getName = getName;
            this.openArchive = openArchive;
            this.closeArchive = closeArchive;
            this.openFile = openFile;
            this.closeFile = closeFile;
        }

        public override string GetArchiveName(int archiveNumber)
        {
            if (!this.getName)
            {
                if (throwEx)
                {
                    throw new Exception(EXCEPTION);
                }
                else
                {
                    return null;
                }
            }
            return base.GetArchiveName(archiveNumber);
        }

        public override Stream OpenArchiveWriteStream(
            int archiveNumber,
            string archiveName,
            bool truncate,
            CompressionEngine compressionEngine)
        {
            if (!this.openArchive)
            {
                if (throwEx)
                {
                    throw new Exception(EXCEPTION);
                }
                else
                {
                    return null;
                }
            }
            return base.OpenArchiveWriteStream(
                archiveNumber, archiveName, truncate, compressionEngine);
        }

        public override void CloseArchiveWriteStream(
            int archiveNumber,
            string archiveName,
            Stream stream)
        {
            if (!this.closeArchive)
            {
                if (throwEx)
                {
                    this.closeArchive = true;
                    throw new Exception(EXCEPTION);
                }
                return;
            }
            base.CloseArchiveWriteStream(archiveNumber, archiveName, stream);
        }

        public override Stream OpenFileReadStream(
            string path,
            out FileAttributes attributes,
            out DateTime lastWriteTime)
        {
            if (!this.openFile)
            {
                if (throwEx)
                {
                    throw new Exception(EXCEPTION);
                }
                else
                {
                    attributes = FileAttributes.Normal;
                    lastWriteTime = DateTime.MinValue;
                    return null;
                }
            }
            return base.OpenFileReadStream(path, out attributes, out lastWriteTime);
        }

        public override void CloseFileReadStream(string path, Stream stream)
        {
            if (!this.closeFile && ++closeFileCount == 2)
            {
                if (throwEx)
                {
                    throw new Exception(EXCEPTION);
                }
                return;
            }
            base.CloseFileReadStream(path, stream);
        }

        public override Stream OpenArchiveReadStream(
            int archiveNumber,
            string archiveName,
            CompressionEngine compressionEngine)
        {
            if (!this.openArchive)
            {
                if (throwEx)
                {
                    throw new Exception(EXCEPTION);
                }
                else
                {
                    return null;
                }
            }
            return base.OpenArchiveReadStream(archiveNumber, archiveName, compressionEngine);
        }

        public override void CloseArchiveReadStream(
            int archiveNumber,
            string archiveName,
            Stream stream)
        {
            if (!this.closeArchive)
            {
                if (throwEx)
                {
                    this.closeArchive = true;
                    throw new Exception(EXCEPTION);
                }
                return;
            }
            base.CloseArchiveReadStream(archiveNumber, archiveName, stream);
        }

        public override Stream OpenFileWriteStream(
            string path,
            long fileSize,
            DateTime lastWriteTime)
        {
            if (!this.openFile)
            {
                if (throwEx)
                {
                    throw new Exception(EXCEPTION);
                }
                else
                {
                    return null;
                }
            }
            return base.OpenFileWriteStream(path, fileSize, lastWriteTime);
        }

        public override void CloseFileWriteStream(
            string path,
            Stream stream,
            FileAttributes attributes,
            DateTime lastWriteTime)
        {
            if (!this.closeFile && ++closeFileCount == 2)
            {
                if (throwEx)
                {
                    throw new Exception(EXCEPTION);
                }
                return;
            }
            base.CloseFileWriteStream(path, stream, attributes, lastWriteTime);
        }
    }
}
