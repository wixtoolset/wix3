//-------------------------------------------------------------------------------------------------
// <copyright file="ResolvedVariableEventHandler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Resolved variable event handler and event args.
// </summary>
//-------------------------------------------------------------------------------------------------

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
