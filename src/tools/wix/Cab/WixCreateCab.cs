//-------------------------------------------------------------------------------------------------
// <copyright file="WixCreateCab.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Wrapper class around interop with wixcab.dll to compress files into a cabinet.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Cab
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;

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
                bool success = RetryCabAction(file, () => { NativeMethods.CreateCabAddFile(file, token, fileHash, this.handle); return true; });

                if (!success)
                {
                    throw new IOException();
                }
            }
            catch (COMException ce)
            {
                const uint E_FAIL = 0x80004005;

                // from winerror.h
                const uint ERROR_ACCESS_DENIED = 5;
                const uint ERROR_SHARING_VIOLATION = 32;
                const uint ERROR_LOCK_VIOLATION = 33;
                const uint ERROR_OPEN_FAILED = 110;
                const uint ERROR_PATH_BUSY = 148;
                const uint ERROR_FILE_CHECKED_OUT = 220;
                const uint ERROR_HANDLE_DISK_FULL = 39;
                const uint ERROR_DISK_FULL = 112;

                if (E_FAIL == unchecked((uint)ce.ErrorCode))
                {
                    throw new WixException(WixErrors.CreateCabAddFileFailed());
                }
                else
                {
                    switch (unchecked((uint)ce.ErrorCode) & 0xffff)
                    {
                        case ERROR_ACCESS_DENIED:
                        case ERROR_SHARING_VIOLATION:
                        case ERROR_LOCK_VIOLATION:
                        case ERROR_OPEN_FAILED:
                        case ERROR_PATH_BUSY:
                        case ERROR_FILE_CHECKED_OUT:
                            throw new WixException(WixErrors.FileInUse(null, file));

                        case ERROR_HANDLE_DISK_FULL:
                        case ERROR_DISK_FULL:
                            throw new WixException(WixErrors.CreateCabInsufficientDiskSpace());

                        default:
                            throw;
                    }
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
            catch (IOException)
            {
                // get a file path with the exception message.
                throw new WixSharingViolationException(file);
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

        /// <summary>
        /// Private method to retry a <c>CAB</c> action that may block because
        /// another process is working the file.  Retries on
        /// an <see cref="COMException" /> or an <see cref="IOException"/>.
        /// </summary>
        /// <param name="path">File source to watch.</param>
        /// <typeparam name="T">Return type of the file action.</typeparam>
        /// <param name="func">File <c>I/O</c> Delegate to retry.</param>
        /// <returns>
        /// Returns the result of the delegate on success, or <c>default(T)</c> on failure.
        /// </returns>
        private T RetryCabAction<T>(string file, Func<T> func)
        {
            // initial state unsignaled
            AutoResetEvent are = new AutoResetEvent(false);
            int i = 0;
            FileInfo fi = new FileInfo(file);

            // from winerror.h
            const uint ERROR_ACCESS_DENIED = 5;
            const uint ERROR_SHARING_VIOLATION = 32;
            const uint ERROR_LOCK_VIOLATION = 33;
            const uint ERROR_OPEN_FAILED = 110;
            const uint ERROR_PATH_BUSY = 148;
            const uint ERROR_FILE_CHECKED_OUT = 220;

            FileSystemWatcher fsw = new FileSystemWatcher(string.IsNullOrEmpty(fi.DirectoryName) || !Directory.Exists(fi.DirectoryName) ? Directory.GetCurrentDirectory() : fi.DirectoryName);

            fsw.NotifyFilter = NotifyFilters.CreationTime | NotifyFilters.DirectoryName | NotifyFilters.FileName | NotifyFilters.LastAccess | NotifyFilters.LastWrite | NotifyFilters.Size;

            // register for Changed provided path (file) matches
            fsw.Changed += (sender, e) =>
            {
                if (e.FullPath.Equals(fi.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    // set the state of the event to signaled and proceed
                    are.Set();
                }
            };

            // disalbe until we have an exception
            fsw.EnableRaisingEvents = false;

            do
            {
                try
                {
                    return func();
                }
                catch (COMException ce)
                {
                    switch (unchecked((uint)ce.ErrorCode) & 0xFFFF)
                    {
                        case ERROR_ACCESS_DENIED:
                        case ERROR_SHARING_VIOLATION:
                        case ERROR_LOCK_VIOLATION:
                        case ERROR_OPEN_FAILED:
                        case ERROR_PATH_BUSY:
                        case ERROR_FILE_CHECKED_OUT:
                            fsw.EnableRaisingEvents = true;

                            // block until signaled or a maximum of 20000 ms.
                            are.WaitOne(20000);
                            break;

                        default:
                            throw;
                    }
                }
                catch (IOException)
                {
                    fsw.EnableRaisingEvents = true;

                    // block until signaled or a maximum of 20000 ms.
                    are.WaitOne(20000);
                }
            } while (8 > i++);

            return default(T);
        }
    }
}
