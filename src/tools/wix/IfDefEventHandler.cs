//-------------------------------------------------------------------------------------------------
// <copyright file="IfDefEventHandler.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// Preprocessor ifdef/ifndef event handler and event args.
// </summary>
//-------------------------------------------------------------------------------------------------

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
