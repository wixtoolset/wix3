// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
