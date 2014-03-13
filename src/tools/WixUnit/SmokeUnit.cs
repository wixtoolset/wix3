//-------------------------------------------------------------------------------------------------
// <copyright file="SmokeUnit.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML Smoke unit tester.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.IO;
    using System.Text;
    using System.Xml;

    /// <summary>
    /// The Windows Installer XML Smoke unit tester.
    /// </summary>
    internal sealed class SmokeUnit
    {
        /// <summary>
        /// Private constructor to prevent instantiation of static class.
        /// </summary>
        private SmokeUnit()
        {
        }

        /// <summary>
        /// Run a Smoke unit test.
        /// </summary>
        /// <param name="element">The unit test element.</param>
        /// <param name="previousUnitResults">The previous unit test results.</param>
        /// <param name="args">The command arguments passed to WixUnit.</param>
        public static void RunUnitTest(XmlElement element, UnitResults previousUnitResults, ICommandArgs args)
        {
            string arguments = element.GetAttribute("Arguments");
            string expectedErrors = element.GetAttribute("ExpectedErrors");
            string expectedWarnings = element.GetAttribute("ExpectedWarnings");
            string extensions = element.GetAttribute("Extensions");
            string toolsDirectory = element.GetAttribute("ToolsDirectory");

            string toolFile = Path.Combine(toolsDirectory, "Smoke.exe");
            StringBuilder commandLine = new StringBuilder(arguments);

            // handle extensions
            if (!String.IsNullOrEmpty(extensions))
            {
                foreach (string extension in extensions.Split(';'))
                {
                    commandLine.AppendFormat(" -ext \"{0}\"", extension);
                }
            }

            // run the tool
            ArrayList output = ToolUtility.RunTool(toolFile, commandLine.ToString());
            previousUnitResults.Errors.AddRange(ToolUtility.GetErrors(output, expectedErrors, expectedWarnings));
            previousUnitResults.Output.AddRange(output);
        }
    }
}
