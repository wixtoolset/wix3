// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Security.Cryptography;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;

    public enum PackagingType
    {
        Unknown,
        Embedded,
        External,
    }

    public interface IPayloadInfo
    {
        string SourceFile { get; set; }
        int FileSize { get; set; }
        string Version { get; set; }
        string ProductName { get; set; }
        string Description { get; set; }
        string Hash { get; set; }
        string PublicKey { get; set; }
        string Thumbprint { get; set; }
        bool SuppressSignatureValidation { get; set; }
    }

    /// <summary>
    /// Specialization of a row for the PayloadInfo table.
    /// </summary>
    public class PayloadInfoRow : Row, IPayloadInfo
    {
        private static readonly Version EmptyVersion = new Version(0, 0, 0, 0);

        /// <summary>
        /// Creates a PayloadInfoRow row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this Media row belongs to and should get its column definitions from.</param>
        public PayloadInfoRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a PayloadInfoRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this Media row belongs to and should get its column definitions from.</param>
        public PayloadInfoRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        public static PayloadInfoRow Create(SourceLineNumberCollection sourceLineNumbers, Output output, string id, string name, string sourceFile,
            bool contentFile, bool suppressSignatureValidation, string downloadUrl, string container, PackagingType packaging)
        {
            Table table = output.Tables["PayloadInfo"];
            PayloadInfoRow row = (PayloadInfoRow)table.CreateRow(sourceLineNumbers);

            row.Id = id;
            row.Name = name;
            row.SourceFile = sourceFile;
            row.ContentFile = contentFile;
            row.SuppressSignatureValidation = suppressSignatureValidation;
            row.DownloadUrl = downloadUrl;
            row.Container = container;
            row.Packaging = packaging;

            PayloadInfoRow.ResolvePayloadInfo(row);
            return row;
        }

        public string Id
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        public string Name
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        public string SourceFile
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        public string DownloadUrl
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        public string Compressed
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        public PackagingType Packaging
        {
            get
            {
                PackagingType type = WindowsInstallerXml.PackagingType.Unknown;

                if (null != this.Fields[4].Data)
                {
                    switch ((int)this.Fields[4].Data)
                    {
                        case 0:
                            type = PackagingType.External;
                            break;
                        case 1:
                            type = PackagingType.Embedded;
                            break;
                    }
                }

                return type;
            }

            set
            {
                switch (value)
                {
                    case PackagingType.External:
                        this.Fields[4].Data = 0;
                        break;
                    case PackagingType.Embedded:
                        this.Fields[4].Data = 1;
                        break;
                    case PackagingType.Unknown:
                        this.Fields[4].Data = null;
                        break;
                }
            }
        }

        public string UnresolvedSourceFile
        {
            get { return (string)this.Fields[5].Data; }
            set { this.Fields[5].Data = value; }
        }

        public bool SuppressSignatureValidation
        {
            get { return (null != this.Fields[6].Data) && (1 == (int)this.Fields[6].Data); }
            set { this.Fields[6].Data = value ? 1 : 0; }
        }

        public int FileSize
        {
            get { return (int)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }

        public string Version
        {
            get { return (string)this.Fields[8].Data; }
            set { this.Fields[8].Data = value; }
        }

        public string ProductName
        {
            get { return (string)this.Fields[9].Data; }
            set { this.Fields[9].Data = value; }
        }

        public string Description
        {
            get { return (string)this.Fields[10].Data; }
            set { this.Fields[10].Data = value; }
        }

        public string Hash
        {
            get { return (string)this.Fields[11].Data; }
            set { this.Fields[11].Data = value; }
        }

        public string PublicKey
        {
            get { return (string)this.Fields[12].Data; }
            set { this.Fields[12].Data = value; }
        }

        public string Thumbprint
        {
            get { return (string)this.Fields[13].Data; }
            set { this.Fields[13].Data = value; }
        }

        public string FullFileName
        {
            get { return String.IsNullOrEmpty(this.SourceFile) ? String.Empty : Path.GetFullPath(this.SourceFile); }
        }

        public string CatalogId
        {
            get { return (string)this.Fields[14].Data; }
            set { this.Fields[14].Data = value; }
        }

        public string Container
        {
            get { return (string)this.Fields[15].Data; }
            set { this.Fields[15].Data = value; }
        }

        public bool ContentFile
        {
            get { return (null != this.Fields[16].Data) && (1 == (int)this.Fields[16].Data); }
            set { this.Fields[16].Data = value ? 1 : 0; }
        }

        public string EmbeddedId
        {
            get { return (string)this.Fields[17].Data; }
            set { this.Fields[17].Data = value; }
        }

        public bool LayoutOnly
        {
            get { return (null != this.Fields[18].Data) && (1 == (int)this.Fields[18].Data); }
            set { this.Fields[18].Data = value ? 1 : 0; }
        }

        public string ParentPackagePayload
        {
            get { return (string)this.Fields[19].Data; }
            set { this.Fields[19].Data = value; }
        }

        public void FillFromPayloadRow(Output output, Row payloadRow)
        {
            SourceLineNumberCollection sourceLineNumbers = payloadRow.SourceLineNumbers;

            this[0] = payloadRow[0];
            this[1] = payloadRow[1];
            this[2] = (string)payloadRow[2] ?? String.Empty;
            this[3] = payloadRow[3];
            this[4] = payloadRow[4];
            this[5] = payloadRow[5] ?? String.Empty;
            this[6] = payloadRow[6];

            // payload files sourced from a cabinet (think WixExtension with embedded binary wixlib) are considered "non-content files".
            ObjectField field = (ObjectField)payloadRow.Fields[2];
            this.ContentFile = String.IsNullOrEmpty(field.CabinetFileId);

            ResolvePayloadInfo(this);

            return;
        }

        public static void ResolvePayloadInfo(IPayloadInfo payloadInfo)
        {
            if (String.IsNullOrEmpty(payloadInfo.SourceFile))
            {
                return;
            }

            FileInfo fileInfo = new FileInfo(payloadInfo.SourceFile);

            if (null != fileInfo)
            {
                payloadInfo.FileSize = (int)fileInfo.Length;
                payloadInfo.Hash = Common.GetFileHash(fileInfo);

                // Try to get the certificate if payloadInfo is a signed file and we're not suppressing signature validation for payloadInfo payload.
                X509Certificate2 certificate = null;
                try
                {
                    if (!payloadInfo.SuppressSignatureValidation)
                    {
                        certificate = new X509Certificate2(fileInfo.FullName);
                    }
                }
                catch (CryptographicException) // we don't care about non-signed files.
                {
                }

                // If there is a certificate, remember its hashed public key identifier and thumbprint.
                if (null != certificate)
                {
                    byte[] publicKeyIdentifierHash = new byte[128];
                    uint publicKeyIdentifierHashSize = (uint)publicKeyIdentifierHash.Length;

                    Microsoft.Tools.WindowsInstallerXml.Cab.Interop.NativeMethods.HashPublicKeyInfo(certificate.Handle, publicKeyIdentifierHash, ref publicKeyIdentifierHashSize);
                    StringBuilder sb = new StringBuilder(((int)publicKeyIdentifierHashSize + 1) * 2);
                    for (int i = 0; i < publicKeyIdentifierHashSize; ++i)
                    {
                        sb.AppendFormat("{0:X2}", publicKeyIdentifierHash[i]);
                    }

                    payloadInfo.PublicKey = sb.ToString();
                    payloadInfo.Thumbprint = certificate.Thumbprint;
                }
            }

            FileVersionInfo versionInfo = FileVersionInfo.GetVersionInfo(payloadInfo.SourceFile);

            if (null != versionInfo)
            {
                // Use the fixed version info block for the file since the resource text may not be a dotted quad.
                Version version = new Version(versionInfo.ProductMajorPart, versionInfo.ProductMinorPart, versionInfo.ProductBuildPart, versionInfo.ProductPrivatePart);

                if (EmptyVersion != version)
                {
                    payloadInfo.Version = version.ToString();
                }

                payloadInfo.ProductName = versionInfo.ProductName;
                payloadInfo.Description = versionInfo.FileDescription;
            }
        }
    }
}
