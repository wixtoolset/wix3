// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Text;

    public delegate void ResolvedVariableEventHandler(object sender, ResolvedVariableEventArgs e);

    public class ResolvedVariableEventArgs : EventArgs
    {
        private SourceLineNumberCollection sourceLineNumbers;
        private string variableName;
        private string variableValue;

        public ResolvedVariableEventArgs(SourceLineNumberCollection sourceLineNumbers, string variableName, string variableValue)
        {
            this.sourceLineNumbers = sourceLineNumbers;
            this.variableName = variableName;
            this.variableValue = variableValue;
        }

        public SourceLineNumberCollection SourceLineNumbers
        {
            get { return this.sourceLineNumbers; }
        }

        public string VariableName
        {
            get { return this.variableName; }
        }

        public string VariableValue
        {
            get { return this.variableValue; }
        }
    }
}
