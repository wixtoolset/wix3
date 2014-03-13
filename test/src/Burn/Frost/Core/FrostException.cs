//-----------------------------------------------------------------------
// <copyright file="FrostException.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Defines a Frost exception</summary>
//-----------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Test.Frost.Core
{
    using System;

    public class FrostException : Exception
    {
        public FrostException() : base(){ }
        public FrostException(string Reason) : base(Reason) { }
        public FrostException(params object[] ReasonStrings) : base(String.Concat(ReasonStrings)) { }
    }

    public class FrostNonExistentVariableException : FrostException
    {
        public FrostNonExistentVariableException(string VariableName) : base("Variable ", VariableName, " was not defined in the environment") { }
    }

    public class FrostConfigException : FrostException
    {
        public FrostConfigException(string Reason) : base(Reason) { }
        public FrostConfigException(params object[] ReasonStrings) : base(ReasonStrings) { }
    }
}
