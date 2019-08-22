// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
