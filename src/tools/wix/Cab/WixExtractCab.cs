//-------------------------------------------------------------------------------------------------
// <copyright file="WixExtractCab.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Wrapper class around interop with wixcab.dll to extract files from a cabinet.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Cab
{
    using System;
    using System.Runtime.InteropServices;
    using Microsoft.Tools.WindowsInstallerXml.Cab.Interop;

    /// <summary>
    /// Wrapper class around interop with wixcab.dll to extract files from a cabinet.
    /// </summary>
    public sealed class WixExtractCab : IDisposable
    {
        private bool disposed;

        /// <summary>
        /// Creates a cabinet extractor.
        /// </summary>
        public WixExtractCab()
        {
            NativeMethods.ExtractCabBegin();
        }

        /// <summary>
        /// Destructor for cabinet extraction.
        /// </summary>
        ~WixExtractCab()
        {
            this.Dispose();
        }

        /// <summary>
        /// Extracts all the files from a cabinet to a directory.
        /// </summary>
        /// <param name="cabinetFile">Cabinet file to extract from.</param>
        /// <param name="extractDir">Directory to extract files to.</param>
        public void Extract(string cabinetFile, string extractDir) 
        {
            if (null == cabinetFile)
            {
                throw new ArgumentNullException("cabinetFile");
            }

            if (null == extractDir)
            {
                throw new ArgumentNullException("extractDir");
            }

            if (this.disposed)
            {
                throw new ObjectDisposedException("WixExtractCab");
            }

            if (!extractDir.EndsWith("\\", StringComparison.Ordinal))
            {
                extractDir = String.Concat(extractDir, "\\");
            }

            NativeMethods.ExtractCab(cabinetFile, extractDir);
        }

        /// <summary>
        /// Disposes the managed and unmanaged objects in this object.
        /// </summary>
        public void Dispose()
        {
            if (!this.disposed)
            {
                NativeMethods.ExtractCabFinish();

                GC.SuppressFinalize(this);
                this.disposed = true;
            }
        }
    }
}
