// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Text;

    public delegate void IfDefEventHandler(object sender, IfDefEventArgs e);

    public class IfDefEventArgs : EventArgs
    {
        private SourceLineNumberCollection sourceLineNumbers;
        private bool isIfDef;
        private bool isDefined;
        private string variableName;

        public IfDefEventArgs(SourceLineNumberCollection sourceLineNumbers, bool isIfDef, bool isDefined, string variableName)
        {
            this.sourceLineNumbers = sourceLineNumbers;
            this.isIfDef = isIfDef;
            this.isDefined = isDefined;
            this.variableName = variableName;
        }

        public SourceLineNumberCollection SourceLineNumbers
        {
            get { return this.sourceLineNumbers; }
        }

        public bool IsDefined
        {
            get { return this.isDefined; }
        }

        public bool IsIfDef
        {
            get { return this.isIfDef; }
        }

        public string VariableName
        {
            get { return this.variableName; }
        }
    }
}
