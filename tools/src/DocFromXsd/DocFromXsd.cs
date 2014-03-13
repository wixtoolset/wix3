//-------------------------------------------------------------------------------------------------
// <copyright file="DocFromXsd.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace WixBuild.Tools.DocFromXsd
{
    using System;
    using System.IO;

    public class Program
    {
        private static int Main(string[] args)
        {
            CommandLine commandLine;
            if (!CommandLine.TryParseArguments(args, out commandLine))
            {
                CommandLine.ShowHelp();
                return 1;
            }

            Directory.CreateDirectory(commandLine.OutputFolder);

            XmlSchemaCompiler xsc = new XmlSchemaCompiler(commandLine.OutputFolder);
            xsc.CompileSchemas(commandLine.Files);

            return 0;
        }
    }
}
