//-------------------------------------------------------------------------------------------------
// <copyright file="ExampleInspectorExtension.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Toolset Inspector Example Extension.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The Windows Installer XML Toolset Example InspectorExtension.
    /// </summary>
    public sealed class ExampleInspectorExtension : InspectorExtension
    {
        public override void InspectIntermediate(Intermediate output)
        {
            foreach (Section section in output.Sections)
            {
                Table fileTable = section.Tables["File"];
                if (null != fileTable && 0 < fileTable.Rows.Count)
                {
                    Row fileRow = fileTable.Rows[0];
                    this.Core.OnMessage(ValidationWarnings.ExampleWarning(fileRow.SourceLineNumbers));

                    return;
                }
            }

            this.Core.OnMessage(ValidationErrors.ExampleError());
        }

        public override void InspectOutput(Output output)
        {
            Table fileTable = output.Tables["File"];
            if (null != fileTable && 0 < fileTable.Rows.Count)
            {
                Row fileRow = fileTable.Rows[0];
                this.Core.OnMessage(ValidationWarnings.ExampleWarning(fileRow.SourceLineNumbers));

                return;
            }

            this.Core.OnMessage(ValidationErrors.ExampleError());
        }

        public override void InspectTransform(Output transform)
        {
            Table fileTable = transform.Tables["File"];
            if (null != fileTable && 0 < fileTable.Rows.Count)
            {
                Row fileRow = fileTable.Rows[0];
                this.Core.OnMessage(ValidationWarnings.ExampleWarning(fileRow.SourceLineNumbers));

                return;
            }

            this.Core.OnMessage(ValidationErrors.ExampleError());
        }

        public override void InspectPatch(Output patch)
        {
            foreach (SubStorage transform in patch.SubStorages)
            {
                // Skip patch transforms.
                if (transform.Name.StartsWith("#"))
                {
                    continue;
                }

                Table fileTable = transform.Data.Tables["File"];
                if (null != fileTable && 0 < fileTable.Rows.Count)
                {
                    Row fileRow = fileTable.Rows[0];
                    this.Core.OnMessage(ValidationWarnings.AnotherExampleWarning(fileRow.SourceLineNumbers));

                    return;
                }
            }

            this.Core.OnMessage(ValidationErrors.ExampleError());
        }
    }
}
