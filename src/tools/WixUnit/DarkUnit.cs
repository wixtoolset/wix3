// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// The Windows Installer XML Dark unit tester.
    /// </summary>
    internal sealed class DarkUnit
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private DarkUnit()
        {
        }

        /// <summary>
        /// Run a Dark unit test.
        /// </summary>
        /// <param name="element">The unit test element.</param>
        /// <param name="previousUnitResults">The previous unit test results.</param>
        /// <param name="args">The command arguments passed to WixUnit.</param>
        public static void RunUnitTest(XmlElement element, UnitResults previousUnitResults, ICommandArgs args)
        {
            string arguments = element.GetAttribute("Arguments");
            string expectedErrors = element.GetAttribute("ExpectedErrors");
            string expectedWarnings = element.GetAttribute("ExpectedWarnings");
            string tempDirectory = element.GetAttribute("TempDirectory");
            string toolsDirectory = element.GetAttribute("ToolsDirectory");

            string toolFile = Path.Combine(toolsDirectory, "dark.exe");
            StringBuilder commandLine = new StringBuilder(arguments);

            // handle wixunit arguments
            if (args.NoTidy)
            {
                commandLine.Append(" -notidy");
            }

            // determine the correct extension for the decompiled output
            string extension = ".wxs";
            if (0 <= arguments.IndexOf("-xo"))
            {
                extension = ".wixout";
            }

            // handle any previous outputs
            string outputFile = null;
            foreach (string databaseFile in previousUnitResults.OutputFiles)
            {
                commandLine.AppendFormat(" \"{0}\"", databaseFile);

                outputFile = Path.Combine(tempDirectory, String.Concat("decompiled_", Path.GetFileNameWithoutExtension(databaseFile), extension));
            }
            previousUnitResults.OutputFiles.Clear();

            commandLine.AppendFormat(" \"{0}\" -x \"{1}\"", outputFile, tempDirectory);
            previousUnitResults.OutputFiles.Add(outputFile);

            // run the tool
            ArrayList output = ToolUtility.RunTool(toolFile, commandLine.ToString());
            previousUnitResults.Errors.AddRange(ToolUtility.GetErrors(output, expectedErrors, expectedWarnings));
            previousUnitResults.Output.AddRange(output);
        }
    }
}
