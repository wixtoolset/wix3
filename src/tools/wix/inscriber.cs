// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using System;
    using System.CodeDom.Compiler;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Converts a wixout representation of an MSM database into a ComponentGroup the form of WiX source.
    /// </summary>
    public sealed class Inscriber : IMessageHandler
    {
        private TempFileCollection tempFiles;
        private TableDefinitionCollection tableDefinitions;
        private bool encounteredError;

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler MessageHandler;

        public Inscriber()
        {
            this.tableDefinitions = Installer.GetTableDefinitions();
        }

        /// <summary>
        /// Gets whether the inscriber has encountered an error while processing.
        /// </summary>
        /// <value>Flag if inscriber encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
        }

        /// <summary>
        /// Gets or sets the temp files collection.
        /// </summary>
        /// <value>The temp files collection.</value>
        public TempFileCollection TempFiles
        {
            get { return this.tempFiles; }
            set { this.tempFiles = value; }
        }

        /// <summary>
        /// Gets or sets the path to the temp files location.
        /// </summary>
        /// <value>The path to the temp files location.</value>
        public string TempFilesLocation
        {
            get
            {
                if (null == this.tempFiles)
                {
                    return null;
                }
                else
                {
                    return this.tempFiles.BasePath;
                }
            }
            set
            {
                this.DeleteTempFiles();

                if (null == value)
                {
                    this.tempFiles = new TempFileCollection();
                }
                else
                {
                    this.tempFiles = new TempFileCollection(value);
                }

                // ensure the base path exists
                Directory.CreateDirectory(this.tempFiles.BasePath);
            }
        }

        /// <summary>
        /// Extracts engine from attached container and updates engine with detached container signatures.
        /// </summary>
        /// <param name="bundleFile">Bundle with attached container.</param>
        /// <param name="outputFile">Bundle engine only.</param>
        /// <returns>True if bundle was updated.</returns>
        public bool InscribeBundleEngine(string bundleFile, string outputFile)
        {
            string tempFile = Path.Combine(this.TempFilesLocation, "bundle_engine_unsigned.exe");

            using (BurnReader reader = BurnReader.Open(bundleFile, this))
            using (FileStream writer = File.Open(tempFile, FileMode.Create, FileAccess.Write, FileShare.Read | FileShare.Delete))
            {
                reader.Stream.Seek(0, SeekOrigin.Begin);

                byte[] buffer = new byte[4 * 1024];
                int total = 0;
                int read = 0;
                do
                {
                    read = Math.Min(buffer.Length, (int)reader.EngineSize - total);

                    read = reader.Stream.Read(buffer, 0, read);
                    writer.Write(buffer, 0, read);

                    total += read;
                } while (total < reader.EngineSize && 0 < read);

                if (total != reader.EngineSize)
                {
                    throw new InvalidOperationException("Failed to copy engine out of bundle.");
                }

                // TODO: update writer with detached container signatures.
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            File.Move(tempFile, outputFile);
            Microsoft.Tools.WindowsInstallerXml.Cab.Interop.NativeMethods.ResetAcls(new string[] { outputFile }, 1);

            return true;
        }

        /// <summary>
        /// Updates engine with attached container information and adds attached container again.
        /// </summary>
        /// <param name="bundleFile">Bundle with attached container.</param>
        /// <param name="signedEngineFile">Signed bundle engine.</param>
        /// <param name="outputFile">Signed engine with attached container.</param>
        /// <returns>True if bundle was updated.</returns>
        public bool InscribeBundle(string bundleFile, string signedEngineFile, string outputFile)
        {
            bool inscribed = false;
            string tempFile = Path.Combine(this.TempFilesLocation, "bundle_engine_signed.exe");

            using (BurnReader reader = BurnReader.Open(bundleFile, this))
            {
                File.Copy(signedEngineFile, tempFile, true);

                // If there was an attached container on the original (unsigned) bundle, put it back.
                using (BurnWriter writer = BurnWriter.Open(tempFile, this))
                {
                    writer.AttachedContainers.Clear();
                    writer.RememberThenResetSignature();
                    foreach (ContainerSlot cntnr in reader.AttachedContainers)
                    {
                        if (cntnr.Size > 0)
                        {
                            reader.Stream.Seek(cntnr.Address, SeekOrigin.Begin);
                            writer.AppendContainer(reader.Stream, cntnr.Size, BurnCommon.Container.Attached);
                            inscribed = true;
                        }
                    }
                }
            }

            Directory.CreateDirectory(Path.GetDirectoryName(outputFile));
            if (File.Exists(outputFile))
            {
                File.Delete(outputFile);
            }
            File.Move(tempFile, outputFile);
            Microsoft.Tools.WindowsInstallerXml.Cab.Interop.NativeMethods.ResetAcls(new string[] { outputFile }, 1);

            return inscribed;
        }

        /// <summary>
        /// Updates database with signatures from external cabinets.
        /// </summary>
        /// <param name="databaseFile">Path to MSI database.</param>
        /// <param name="outputFile">Ouput for updated MSI database.</param>
        /// <param name="tidy">Clean up files.</param>
        /// <returns>True if database is updated.</returns>
        public bool InscribeDatabase(string databaseFile, string outputFile, bool tidy)
        {
            // Keeps track of whether we've encountered at least one signed cab or not - we'll throw a warning if no signed cabs were encountered
            bool foundUnsignedExternals = false;
            bool shouldCommit = false;

            FileAttributes attributes = File.GetAttributes(databaseFile);
            if (FileAttributes.ReadOnly == (attributes & FileAttributes.ReadOnly))
            {
                this.OnMessage(WixErrors.ReadOnlyOutputFile(databaseFile));
                return shouldCommit;
            }

            using (Database database = new Database(databaseFile, OpenDatabase.Transact))
            {
                // Just use the English codepage, because the tables we're importing only have binary streams / MSI identifiers / other non-localizable content
                int codepage = 1252;

                // list of certificates for this database (hash/identifier)
                Dictionary<string, string> certificates = new Dictionary<string, string>();

                // Reset the in-memory tables for this new database
                Table digitalSignatureTable = new Table(null, this.tableDefinitions["MsiDigitalSignature"]);
                Table digitalCertificateTable = new Table(null, this.tableDefinitions["MsiDigitalCertificate"]);

                // If any digital signature records exist that are not of the media type, preserve them
                if (database.TableExists("MsiDigitalSignature"))
                {
                    using (View digitalSignatureView = database.OpenExecuteView("SELECT `Table`, `SignObject`, `DigitalCertificate_`, `Hash` FROM `MsiDigitalSignature` WHERE `Table` <> 'Media'"))
                    {
                        while (true)
                        {
                            using (Record digitalSignatureRecord = digitalSignatureView.Fetch())
                            {
                                if (null == digitalSignatureRecord)
                                {
                                    break;
                                }

                                Row digitalSignatureRow = null;
                                digitalSignatureRow = digitalSignatureTable.CreateRow(null);

                                string table = digitalSignatureRecord.GetString(0);
                                string signObject = digitalSignatureRecord.GetString(1);

                                digitalSignatureRow[0] = table;
                                digitalSignatureRow[1] = signObject;
                                digitalSignatureRow[2] = digitalSignatureRecord.GetString(2);

                                if (false == digitalSignatureRecord.IsNull(3))
                                {
                                    // Export to a file, because the MSI API's require us to provide a file path on disk
                                    string hashPath = Path.Combine(this.TempFilesLocation, "MsiDigitalSignature");
                                    string hashFileName = string.Concat(table, ".", signObject, ".bin");

                                    Directory.CreateDirectory(hashPath);
                                    hashPath = Path.Combine(hashPath, hashFileName);

                                    using (FileStream fs = File.Create(hashPath))
                                    {
                                        int bytesRead;
                                        byte[] buffer = new byte[1024 * 4];

                                        while (0 != (bytesRead = digitalSignatureRecord.GetStream(3, buffer, buffer.Length)))
                                        {
                                            fs.Write(buffer, 0, bytesRead);
                                        }
                                    }

                                    digitalSignatureRow[3] = hashFileName;
                                }
                            }
                        }
                    }
                }

                // If any digital certificates exist, extract and preserve them 
                if (database.TableExists("MsiDigitalCertificate"))
                {
                    using (View digitalCertificateView = database.OpenExecuteView("SELECT * FROM `MsiDigitalCertificate`"))
                    {
                        while (true)
                        {
                            using (Record digitalCertificateRecord = digitalCertificateView.Fetch())
                            {
                                if (null == digitalCertificateRecord)
                                {
                                    break;
                                }

                                string certificateId = digitalCertificateRecord.GetString(1); // get the identifier of the certificate

                                // Export to a file, because the MSI API's require us to provide a file path on disk
                                string certPath = Path.Combine(this.TempFilesLocation, "MsiDigitalCertificate");
                                Directory.CreateDirectory(certPath);
                                certPath = Path.Combine(certPath, string.Concat(certificateId, ".cer"));

                                using (FileStream fs = File.Create(certPath))
                                {
                                    int bytesRead;
                                    byte[] buffer = new byte[1024 * 4];

                                    while (0 != (bytesRead = digitalCertificateRecord.GetStream(2, buffer, buffer.Length)))
                                    {
                                        fs.Write(buffer, 0, bytesRead);
                                    }
                                }

                                // Add it to our "add to MsiDigitalCertificate" table dictionary
                                Row digitalCertificateRow = digitalCertificateTable.CreateRow(null);
                                digitalCertificateRow[0] = certificateId;

                                // Now set the file path on disk where this binary stream will be picked up at import time
                                digitalCertificateRow[1] = string.Concat(certificateId, ".cer");

                                // Load the cert to get it's thumbprint
                                X509Certificate cert = X509Certificate.CreateFromCertFile(certPath);
                                X509Certificate2 cert2 = new X509Certificate2(cert);

                                certificates.Add(cert2.Thumbprint, certificateId);
                            }
                        }
                    }
                }

                using (View mediaView = database.OpenExecuteView("SELECT * FROM `Media`"))
                {
                    while (true)
                    {
                        using (Record mediaRecord = mediaView.Fetch())
                        {
                            if (null == mediaRecord)
                            {
                                break;
                            }

                            X509Certificate2 cert2 = null;
                            Row digitalSignatureRow = null;

                            string cabName = mediaRecord.GetString(4); // get the name of the cab
                            // If there is no cabinet or it's an internal cab, skip it.
                            if (String.IsNullOrEmpty(cabName) || cabName.StartsWith("#", StringComparison.Ordinal))
                            {
                                continue;
                            }

                            string cabId = mediaRecord.GetString(1); // get the ID of the cab
                            string cabPath = Path.Combine(Path.GetDirectoryName(databaseFile), cabName);

                            // If the cabs aren't there, throw an error but continue to catch the other errors
                            if (!File.Exists(cabPath))
                            {
                                this.OnMessage(WixErrors.WixFileNotFound(cabPath));
                                continue;
                            }

                            try
                            {
                                // Get the certificate from the cab
                                X509Certificate signedFileCert = X509Certificate.CreateFromSignedFile(cabPath);
                                cert2 = new X509Certificate2(signedFileCert);
                            }
                            catch (System.Security.Cryptography.CryptographicException e)
                            {
                                uint HResult = unchecked((uint)Marshal.GetHRForException(e));

                                // If the file has no cert, continue, but flag that we found at least one so we can later give a warning
                                if (0x80092009 == HResult) // CRYPT_E_NO_MATCH
                                {
                                    foundUnsignedExternals = true;
                                    continue;
                                }

                                // todo: exactly which HRESULT corresponds to this issue?
                                // If it's one of these exact platforms, warn the user that it may be due to their OS.
                                if ((5 == Environment.OSVersion.Version.Major && 2 == Environment.OSVersion.Version.Minor) || // W2K3
                                        (5 == Environment.OSVersion.Version.Major && 1 == Environment.OSVersion.Version.Minor)) // XP
                                {
                                    this.OnMessage(WixErrors.UnableToGetAuthenticodeCertOfFileDownlevelOS(cabPath, String.Format(CultureInfo.InvariantCulture, "HRESULT: 0x{0:x8}", HResult)));
                                }
                                else // otherwise, generic error
                                {
                                    this.OnMessage(WixErrors.UnableToGetAuthenticodeCertOfFile(cabPath, String.Format(CultureInfo.InvariantCulture, "HRESULT: 0x{0:x8}", HResult)));
                                }
                            }

                            // If we haven't added this cert to the MsiDigitalCertificate table, set it up to be added
                            if (!certificates.ContainsKey(cert2.Thumbprint))
                            {
                                // generate a stable identifier                                
                                string certificateGeneratedId = Common.GenerateIdentifier("cer", true, cert2.Thumbprint);

                                // Add it to our "add to MsiDigitalCertificate" table dictionary
                                Row digitalCertificateRow = digitalCertificateTable.CreateRow(null);
                                digitalCertificateRow[0] = certificateGeneratedId;

                                // Export to a file, because the MSI API's require us to provide a file path on disk
                                string certPath = Path.Combine(this.TempFilesLocation, "MsiDigitalCertificate");
                                Directory.CreateDirectory(certPath);
                                certPath = Path.Combine(certPath, string.Concat(cert2.Thumbprint, ".cer"));
                                File.Delete(certPath);

                                using (BinaryWriter writer = new BinaryWriter(File.Open(certPath, FileMode.Create)))
                                {
                                    writer.Write(cert2.RawData);
                                    writer.Close();
                                }

                                // Now set the file path on disk where this binary stream will be picked up at import time
                                digitalCertificateRow[1] = string.Concat(cert2.Thumbprint, ".cer");

                                certificates.Add(cert2.Thumbprint, certificateGeneratedId);
                            }

                            digitalSignatureRow = digitalSignatureTable.CreateRow(null);

                            digitalSignatureRow[0] = "Media";
                            digitalSignatureRow[1] = cabId;
                            digitalSignatureRow[2] = certificates[cert2.Thumbprint];
                        }
                    }
                }

                if (digitalCertificateTable.Rows.Count > 0)
                {
                    database.ImportTable(codepage, (IMessageHandler)this, digitalCertificateTable, this.TempFilesLocation, true);
                    shouldCommit = true;
                }

                if (digitalSignatureTable.Rows.Count > 0)
                {
                    database.ImportTable(codepage, (IMessageHandler)this, digitalSignatureTable, this.TempFilesLocation, true);
                    shouldCommit = true;
                }

                // TODO: if we created the table(s), then we should add the _Validation records for them.

                certificates = null;

                // If we did find external cabs but none of them were signed, give a warning
                if (foundUnsignedExternals)
                {
                    this.OnMessage(WixWarnings.ExternalCabsAreNotSigned(databaseFile));
                }

                if (shouldCommit)
                {
                    database.Commit();
                }
            }

            return shouldCommit;
        }

        /// <summary>
        /// Cleans up the temp files used by the Inscriber.
        /// </summary>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        public bool DeleteTempFiles()
        {
            if (null == this.tempFiles)
            {
                return true; // no work to do
            }
            else
            {
                bool deleted = Common.DeleteTempFiles(this.TempFilesLocation, this);

                if (deleted)
                {
                    ((IDisposable)this.tempFiles).Dispose();
                    this.tempFiles = null; // temp files have been deleted, no need to remember this now
                }

                return deleted;
            }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != errorEventArgs)
            {
                this.encounteredError = true;
            }

            if (null != this.MessageHandler)
            {
                this.MessageHandler(this, e);
                if (MessageLevel.Error == e.Level)
                {
                    this.encounteredError = true;
                }
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }
    }
}
