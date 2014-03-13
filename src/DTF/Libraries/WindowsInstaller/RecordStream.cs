//---------------------------------------------------------------------
// <copyright file="RecordStream.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.WindowsInstaller
{
    using System;
    using System.IO;

    internal class RecordStream : Stream
    {
        private Record record;
        private int field;
        private long position;

        internal RecordStream(Record record, int field)
            : base()
        {
            this.record = record;
            this.field = field;
        }

        public override bool CanRead { get { return true; } }
        public override bool CanWrite { get { return false; } }
        public override bool CanSeek { get { return false; } }

        public override long Length
        {
            get
            {
                return this.record.GetDataSize(this.field);
            }
        }

        public override long Position
        {
            get
            {
                return this.position;
            }

            set
            {
                throw new NotSupportedException();
            }
        }

        public override long Seek(long offset, SeekOrigin origin)
        {
            throw new NotSupportedException();
        }

        public override void SetLength(long value)
        {
            throw new NotSupportedException();
        }

        public override void Flush()
        {
            throw new NotSupportedException();
        }

        public override int Read(byte[] buffer, int offset, int count)
        {
            if (count > 0)
            {
                byte[] readBuffer = (offset == 0 ? buffer : new byte[count]);
                uint ucount = (uint) count;
                uint ret = RemotableNativeMethods.MsiRecordReadStream((int) this.record.Handle, (uint) this.field, buffer, ref ucount);
                if (ret != 0)
                {
                    throw InstallerException.ExceptionFromReturnCode(ret);
                }
                count = (int) ucount;
                if (offset > 0)
                {
                    Array.Copy(readBuffer, 0, buffer, offset, count);
                }
                this.position += count;
            }
            return count;
        }

        public override void Write(byte[] array, int offset, int count)
        {
            throw new NotSupportedException();
        }

        public override string ToString()
        {
            return "[Binary data]";
        }
    }
}
