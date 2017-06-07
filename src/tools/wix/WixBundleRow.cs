// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Bundle info for binding Bundles.
    /// </summary>
    public class WixBundleRow : Row
    {
        /// <summary>
        /// Creates a WixBundleRow row that does not belong to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="tableDef">TableDefinition this WixBundleRow row belongs to and should get its column definitions from.</param>
        public WixBundleRow(SourceLineNumberCollection sourceLineNumbers, TableDefinition tableDef) :
            base(sourceLineNumbers, tableDef)
        {
        }

        /// <summary>
        /// Creates a WixBundleRow row that belongs to a table.
        /// </summary>
        /// <param name="sourceLineNumbers">Original source lines for this row.</param>
        /// <param name="table">Table this WixBundleRow row belongs to and should get its column definitions from.</param>
        public WixBundleRow(SourceLineNumberCollection sourceLineNumbers, Table table) :
            base(sourceLineNumbers, table)
        {
        }

        public string Version
        {
            get { return (string)this.Fields[0].Data; }
            set { this.Fields[0].Data = value; }
        }

        public string Copyright
        {
            get { return (string)this.Fields[1].Data; }
            set { this.Fields[1].Data = value; }
        }

        public string Name
        {
            get { return (string)this.Fields[2].Data; }
            set { this.Fields[2].Data = value; }
        }

        public string AboutUrl
        {
            get { return (string)this.Fields[3].Data; }
            set { this.Fields[3].Data = value; }
        }

        public int DisableModify
        {
            get { return (null == this.Fields[4].Data) ? 0 : (int)this.Fields[4].Data; }
            set { this.Fields[4].Data = value; }
        }

        public bool DisableRemove
        {
            get { return (null != this.Fields[5].Data && 0 != (int)this.Fields[5].Data); }
            set { this.Fields[5].Data = value ? 1 : 0; }
        }

        // There is no 6. It used to be DisableRepair.

        public string HelpTelephone
        {
            get { return (string)this.Fields[7].Data; }
            set { this.Fields[7].Data = value; }
        }

        public string HelpLink
        {
            get { return (string)this.Fields[8].Data; }
            set { this.Fields[8].Data = value; }
        }

        public string Publisher
        {
            get { return (string)this.Fields[9].Data; }
            set { this.Fields[9].Data = value; }
        }

        public string UpdateUrl
        {
            get { return (string)this.Fields[10].Data; }
            set { this.Fields[10].Data = value; }
        }

        public YesNoDefaultType Compressed
        {
            get { return (null == this.Fields[11].Data) ? YesNoDefaultType.Default : (0 == (int)this.Fields[11].Data) ? YesNoDefaultType.No : YesNoDefaultType.Yes; }
            set { this.Fields[11].Data = (int)value; }
        }

        public PackagingType DefaultPackagingType
        {
            get { return (YesNoDefaultType.No == this.Compressed) ? PackagingType.External : PackagingType.Embedded; }
        }

        public string LogPathPrefixExtension
        {
            get { return (string)this.Fields[12].Data ?? String.Empty; }
            set { this.Fields[12].Data = value; }
        }

        public string LogPathVariable
        {
            get
            {
                string[] logVariableAndPrefixExtension = this.LogPathPrefixExtension.Split('|');
                return logVariableAndPrefixExtension[0];
            }
        }

        public string LoggingBaseFolder
        {
            get
            {
                string[] logVariableAndPrefixExtension = this.LogPathPrefixExtension.Split('|');
                return logVariableAndPrefixExtension[1];
            }
        }

        public string LogPrefix
        {
            get
            {
                string[] logVariableAndPrefixExtension = this.LogPathPrefixExtension.Split('|');
                if (3 > logVariableAndPrefixExtension.Length)
                {
                    return String.Empty;
                }
                string logPrefixAndExtension = logVariableAndPrefixExtension[2];
                int extensionIndex = logPrefixAndExtension.LastIndexOf('.');
                return logPrefixAndExtension.Substring(0, extensionIndex);
            }
        }

        public string LogExtension
        {
            get
            {
                string[] logVariableAndPrefixExtension = this.LogPathPrefixExtension.Split('|');
                if (3 > logVariableAndPrefixExtension.Length)
                {
                    return String.Empty;
                }
                string logPrefixAndExtension = logVariableAndPrefixExtension[2];
                int extensionIndex = logPrefixAndExtension.LastIndexOf('.');
                return logPrefixAndExtension.Substring(extensionIndex + 1);
            }
        }

        public string IconPath
        {
            get { return (string)this.Fields[13].Data; }
            set { this.Fields[13].Data = value; }
        }

        public string SplashScreenBitmapPath
        {
            get { return (string)this.Fields[14].Data; }
            set { this.Fields[14].Data = value; }
        }

        public string Condition
        {
            get { return (string)this.Fields[15].Data; }
            set { this.Fields[15].Data = value; }
        }

        public string Tag
        {
            get { return (string)this.Fields[16].Data; }
            set { this.Fields[16].Data = value; }
        }

        public Platform Platform
        {
            get { return (Platform)Enum.Parse(typeof(Platform), (string)this.Fields[17].Data); }
            set { this.Fields[17].Data = value.ToString(); }
        }

        public string ParentName
        {
            get { return (string)this.Fields[18].Data; }
            set { this.Fields[18].Data = value; }
        }

        public string UpgradeCode
        {
            get { return (string)this.Fields[19].Data; }
            set { this.Fields[19].Data = value; }
        }

        public Guid BundleId
        {
            get
            {
                if (null == this.Fields[20].Data)
                {
                    this.Fields[20].Data = Guid.NewGuid().ToString("B");
                }

                return new Guid((string)this.Fields[20].Data);
            }

            set { this.Fields[20].Data = value.ToString(); }
        }

        public string ProviderKey
        {
            get
            {
                if (null == this.Fields[21].Data)
                {
                    this.Fields[21].Data = this.BundleId.ToString("B");
                }

                return (string)this.Fields[21].Data;
            }

            set { this.Fields[21].Data = value; }
        }

        public bool PerMachine
        {
            get { return (null != this.Fields[22].Data && 0 != (int)this.Fields[22].Data); }
            set { this.Fields[22].Data = value ? 1 : 0; }
        }
    }
}
