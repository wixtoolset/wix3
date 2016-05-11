// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
