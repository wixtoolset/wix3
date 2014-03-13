//-------------------------------------------------------------------------------------------------
// <copyright file="VSDecompiler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The decompiler for the Windows Installer XML Toolset Visual Studio Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;

    using VS = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.VS;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The decompiler for the Windows Installer XML Toolset Visual Studio Extension.
    /// </summary>
    public sealed class VSDecompiler : DecompilerExtension
    {
        /// <summary>
        /// Decompiles an extension table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        public override void DecompileTable(Table table)
        {
            switch (table.Name)
            {
                case "HelpFile":
                    this.DecompileHelpFileTable(table);
                    break;
                case "HelpFileToNamespace":
                    this.DecompileHelpFileToNamespaceTable(table);
                    break;
                case "HelpFilter":
                    this.DecompileHelpFilterTable(table);
                    break;
                case "HelpFilterToNamespace":
                    this.DecompileHelpFilterToNamespaceTable(table);
                    break;
                case "HelpNamespace":
                    this.DecompileHelpNamespaceTable(table);
                    break;
                case "HelpPlugin":
                    this.DecompileHelpPluginTable(table);
                    break;
                default:
                    base.DecompileTable(table);
                    break;
            }
        }

        /// <summary>
        /// Decompile the HelpFile table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileHelpFileTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                VS.HelpFile helpFile = new VS.HelpFile();

                helpFile.Id = (string)row[0];

                helpFile.Name = (string)row[1];

                if (null != row[2])
                {
                    helpFile.Language = (int)row[2];
                }

                if (null != row[4])
                {
                    helpFile.Index = (string)row[4];
                }

                if (null != row[5])
                {
                    helpFile.Search = (string)row[5];
                }

                if (null != row[6])
                {
                    helpFile.AttributeIndex = (string)row[6];
                }

                if (null != row[7])
                {
                    helpFile.SampleLocation = (string)row[7];
                }
                
                if (this.Core.RootElement is Wix.Module)
                {
                    helpFile.SuppressCustomActions = VS.YesNoType.yes;
                }

                Wix.File file = (Wix.File)this.Core.GetIndexedElement("File", (string)row[3]);
                if (null != file)
                {
                    file.AddChild(helpFile);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "File_HxS", (string)row[3], "File"));
                }
            }
        }

        /// <summary>
        /// Decompile the HelpFileToNamespace table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileHelpFileToNamespaceTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                VS.HelpFileRef helpFileRef = new VS.HelpFileRef();

                helpFileRef.Id = (string)row[0];

                VS.HelpCollection helpCollection = (VS.HelpCollection)this.Core.GetIndexedElement("HelpNamespace", (string)row[1]);
                if (null != helpCollection)
                {
                    helpCollection.AddChild(helpFileRef);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "HelpNamespace_", (string)row[1], "HelpNamespace"));
                }
            }
        }

        /// <summary>
        /// Decompile the HelpFilter table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileHelpFilterTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                VS.HelpFilter helpFilter = new VS.HelpFilter();

                helpFilter.Id = (string)row[0];

                helpFilter.Name = (string)row[1];

                if (null != row[2])
                {
                    helpFilter.FilterDefinition = (string)row[2];
                }

                if (this.Core.RootElement is Wix.Module)
                {
                    helpFilter.SuppressCustomActions = VS.YesNoType.yes;
                }

                this.Core.RootElement.AddChild(helpFilter);
            }
        }

        /// <summary>
        /// Decompile the HelpFilterToNamespace table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileHelpFilterToNamespaceTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                VS.HelpFilterRef helpFilterRef = new VS.HelpFilterRef();

                helpFilterRef.Id = (string)row[0];

                VS.HelpCollection helpCollection = (VS.HelpCollection)this.Core.GetIndexedElement("HelpNamespace", (string)row[1]);
                if (null != helpCollection)
                {
                    helpCollection.AddChild(helpFilterRef);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "HelpNamespace_", (string)row[1], "HelpNamespace"));
                }
            }
        }

        /// <summary>
        /// Decompile the HelpNamespace table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileHelpNamespaceTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                VS.HelpCollection helpCollection = new VS.HelpCollection();

                helpCollection.Id = (string)row[0];

                helpCollection.Name = (string)row[1];

                if (null != row[3])
                {
                    helpCollection.Description = (string)row[3];
                }

                if (this.Core.RootElement is Wix.Module)
                {
                    helpCollection.SuppressCustomActions = VS.YesNoType.yes;
                }

                Wix.File file = (Wix.File)this.Core.GetIndexedElement("File", (string)row[2]);
                if (null != file)
                {
                    file.AddChild(helpCollection);
                }
                else if (0 != String.Compare(helpCollection.Id, "MS_VSIPCC_v80", StringComparison.Ordinal) &&
                    0 != String.Compare(helpCollection.Id, "MS.VSIPCC.v90", StringComparison.Ordinal))
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "File_Collection", (string)row[2], "File"));
                }
                this.Core.IndexElement(row, helpCollection);
            }
        }

        /// <summary>
        /// Decompile the HelpPlugin table.
        /// </summary>
        /// <param name="table">The table to decompile.</param>
        private void DecompileHelpPluginTable(Table table)
        {
            foreach (Row row in table.Rows)
            {
                VS.PlugCollectionInto plugCollectionInto = new VS.PlugCollectionInto();

                plugCollectionInto.TargetCollection = (string)row[1];

                if (null != row[2])
                {
                    plugCollectionInto.TableOfContents = (string)row[2];
                }

                if (null != row[3])
                {
                    plugCollectionInto.Attributes = (string)row[3];
                }

                if (null != row[4])
                {
                    plugCollectionInto.TargetTableOfContents = (string)row[4];
                }

                if (this.Core.RootElement is Wix.Module)
                {
                    plugCollectionInto.SuppressExternalNamespaces = VS.YesNoType.yes;
                }

                //we cannot do this work because we cannot get the FeatureComponent table
                //plugCollectionInto.TargetFeature = DecompileHelpComponents();
                
                VS.HelpCollection helpCollection = (VS.HelpCollection)this.Core.GetIndexedElement("HelpNamespace", (string)row[0]);
                if (null != helpCollection)
                {
                    helpCollection.AddChild(plugCollectionInto);
                }
                else
                {
                    this.Core.OnMessage(WixWarnings.ExpectedForeignRow(row.SourceLineNumbers, table.Name, row.GetPrimaryKey(DecompilerCore.PrimaryKeyDelimiter), "HelpNamespace_", (string)row[0], "HelpNamespace"));
                }
            }
        }
        //private string DecompileHelpComponents()
        //{
        //    throw new NotImplementedException();
        //    //Find both known compontents from FeatureComponents table and build feature list

        //    //remove components from FeatureComponents

        //    //return a space delimited list of features that mapped to our help components
        //    return String.Empty;
        //}
    }
}
