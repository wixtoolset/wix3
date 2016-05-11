// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Cab
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using Microsoft.Tools.WindowsInstallerXml.Cab.Interop;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Compression level to use when creating cabinet.
    /// </summary>
    public enum CompressionLevel
    {
        /// <summary>Use no compression.</summary>
        None,

        /// <summary>Use low compression.</summary>
        Low,

        /// <summary>Use medium compression.</summary>
        Medium,

        /// <summary>Use high compression.</summary>
        High,

        /// <summary>Use ms-zip compression.</summary>
        Mszip
    }

    /// <summary>
    /// Wrapper class around interop with wixcab.dll to compress files into a cabinet.
    /// </summary>
    public sealed class WixCreateCab : IDisposable
    {
        private static readonly string CompressionLevelVariable = "WIX_COMPRESSION_LEVEL";
        private IntPtr handle = IntPtr.Zero;
        private bool disposed;
        private int maxSize;

        /// <summary>
        /// Creates a cabinet.
        /// </summary>
        /// <param name="cabName">Name of cabinet to create.</param>
        /// <param name="cabDir">Directory to create cabinet in.</param>
        /// <param name="maxFiles">Maximum number of files that will be added to cabinet.</param>
        /// <param name="maxSize">Maximum size of cabinet.</param>
        /// <param name="maxThresh">Maximum threshold for each cabinet.</param>
        /// <param name="compressionLevel">Level of compression to apply.</param>
        public WixCreateCab(string cabName, string cabDir, int maxFiles, int maxSize, int maxThresh, CompressionLevel compressionLevel)
        {
            string compressionLevelVariable = Environment.GetEnvironmentVariable(CompressionLevelVariable);
            this.maxSize = maxSize;

            try
            {
                // Override authored compression level if environment variable is present.
                if (!String.IsNullOrEmpty(compressionLevelVariable))
                {
                    compressionLevel = WixCreateCab.CompressionLevelFromString(compressionLevelVariable);
                }
            }
            catch (WixException)
            {
                throw new WixException(WixErrors.IllegalEnvironmentVariable(CompressionLevelVariable, compressionLevelVariable));
            }
        
            if (String.IsNullOrEmpty(cabDir))
            {
                cabDir = Directory.GetCurrentDirectory();
            }

            try
            {
                NativeMethods.CreateCabBegin(cabName, cabDir, (uint)maxFiles, (uint)maxSize, (uint)maxThresh, (uint)compressionLevel, out this.handle);
            }
            catch (COMException ce)
            {
                // If we get a "the file exists" error, we must have a full temp directory - so report the issue
                if (0x80070050 == unchecked((uint)ce.ErrorCode))
                {
                    throw new WixException(WixErrors.FullTempDirectory("WSC", Path.GetTempPath()));
                }

                throw;
            }
        }

        /// <summary>
        /// Destructor for cabinet creation.
        /// </summary>
        ~WixCreateCab()
        {
            this.Dispose();
        }

        /// <summary>
        /// Converts a compression level from its string to its enum value.
        /// </summary>
        /// <param name="compressionLevel">Compression level as a string.</param>
        /// <returns>CompressionLevel enum value</returns>
        public static CompressionLevel CompressionLevelFromString(string compressionLevel)
        {
            switch (compressionLevel.ToLower(CultureInfo.InvariantCulture))
            {
                case "low":
                    return Cab.CompressionLevel.Low;
                case "medium":
                    return Cab.CompressionLevel.Medium;
                case "high":
                    return Cab.CompressionLevel.High;
                case "none":
                    return Cab.CompressionLevel.None;
                case "mszip":
                    return Cab.CompressionLevel.Mszip;
                default:
                    throw new WixException(WixErrors.IllegalCompressionLevel(compressionLevel));
            }
        }

        /// <summary>
        /// Adds a file to the cabinet.
        /// </summary>
        /// <param name="fileRow">The filerow of the file to add.</param>
        public void AddFile(FileRow fileRow)
        {
            MsiInterop.MSIFILEHASHINFO hashInterop = new MsiInterop.MSIFILEHASHINFO();

            if (null != fileRow.HashRow)
            {
                hashInterop.FileHashInfoSize = 20;
                hashInterop.Data0 = (int)fileRow.HashRow[2];
                hashInterop.Data1 = (int)fileRow.HashRow[3];
                hashInterop.Data2 = (int)fileRow.HashRow[4];
                hashInterop.Data3 = (int)fileRow.HashRow[5];

                this.AddFile(fileRow.Source, fileRow.File, hashInterop);
            }
            else
            {
                this.AddFile(fileRow.Source, fileRow.File);
            }
        }

        /// <summary>
        /// Adds a file to the cabinet.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="token">The token for the file.</param>
        public void AddFile(string file, string token)
        {
            this.AddFile(file, token, null);
        }

        /// <summary>
        /// Adds a file to the cabinet with an optional MSI file hash.
        /// </summary>
        /// <param name="file">The file to add.</param>
        /// <param name="token">The token for the file.</param>
        /// <param name="fileHash">The MSI file hash of the file.</param>
        private void AddFile(string file, string token, MsiInterop.MSIFILEHASHINFO fileHash)
        {
            try
            {
                NativeMethods.CreateCabAddFile(file, token, fileHash, this.handle);
            }
            catch (COMException ce)
            {
                if (0x80004005 == unchecked((uint)ce.ErrorCode)) // E_FAIL
                {
                    throw new WixException(WixErrors.CreateCabAddFileFailed());
                }
                else if (0x80070070 == unchecked((uint)ce.ErrorCode)) // ERROR_DISK_FULL
                {
                    throw new WixException(WixErrors.CreateCabInsufficientDiskSpace());
                }
                else
                {
                    throw;
                }
            }
            catch (DirectoryNotFoundException)
            {
                throw new WixFileNotFoundException(file);
            }
            catch (FileNotFoundException)
            {
                throw new WixFileNotFoundException(file);
            }
        }

        /// <summary>
        /// Complete/commit the cabinet - this must be called before Dispose so that errors will be 
        /// reported on the same thread.
        /// This Complete should be used with no Cabinet splitting as it has the split cabinet names callback address as Zero
        /// </summary>
        public void Complete()
        {
            this.Complete(IntPtr.Zero);
        }

        /// <summary>
        /// Complete/commit the cabinet - this must be called before Dispose so that errors will be 
        /// reported on the same thread.
        /// </summary>
        /// <param name="newCabNamesCallBackAddress">Address of Binder's callback function for Cabinet Splitting</param>
        public void Complete(IntPtr newCabNamesCallBackAddress)
        {
            if (IntPtr.Zero != this.handle)
            {
                try
                {
                    if (newCabNamesCallBackAddress != IntPtr.Zero && this.maxSize != 0)
                    {
                        NativeMethods.CreateCabFinish(this.handle, newCabNamesCallBackAddress);
                    }
                    else
                    {
                        NativeMethods.CreateCabFinish(this.handle, IntPtr.Zero);
                    }
                        
                    GC.SuppressFinalize(this);
                    this.disposed = true;
                }
                catch (COMException ce)
                {
                    if (0x80004005 == unchecked((uint)ce.ErrorCode)) // E_FAIL
                    {
                        // This error seems to happen, among other situations, when cabbing more than 0xFFFF files
                        throw new WixException(WixErrors.FinishCabFailed());
                    }
                    else if (0x80070070 == unchecked((uint)ce.ErrorCode)) // ERROR_DISK_FULL
                    {
                        throw new WixException(WixErrors.CreateCabInsufficientDiskSpace());
                    }
                    else
                    {
                        throw;
                    }
                }
                finally
                {
                    this.handle = IntPtr.Zero;
                }
            }
        }

        /// <summary>
        /// Cancels ("rolls back") the creation of the cabinet.
        /// Don't throw WiX errors from here, because we're in a different thread, and they won't be reported correctly.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                if (IntPtr.Zero != this.handle)
                {
                    NativeMethods.CreateCabCancel(this.handle);
                    this.handle = IntPtr.Zero;
                }

                GC.SuppressFinalize(this);
                this.disposed = true;
            }
        }
    }
}
