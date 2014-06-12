//-----------------------------------------------------------------------
// <copyright file="Logo.LogoTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Test how different Wix tools handle the NoLogo switch.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Common.Logo
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Text.RegularExpressions;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Test how different Wix tools handle the NoLogo switch.
    /// </summary>
    public class LogoTests : WixTests
    {
        private static readonly string LogoOutputRegexString = @"Microsoft \(R\) Windows Installer Xml {0} version 3\.6\.\d\d\d\d.0" + Environment.NewLine + @"Copyright \(C\) Microsoft Corporation\. All rights reserved\.";
        private List<WixTool> wixTools;
        
        protected override void TestInitialize()
        {
            base.TestInitialize();

            wixTools = new List<WixTool>();
            wixTools.Add(new Candle());
            wixTools.Add(new Dark());
            wixTools.Add(new Light());
            wixTools.Add(new Lit());
            wixTools.Add(new Pyro());
            wixTools.Add(new Smoke());
            wixTools.Add(new Torch());
        }

        [NamedFact]
        [Description("Verify that different Wix tools print the Logo information.")]
        [Priority(2)]
        public void PrintLogo()
        {
            foreach (WixTool wixTool in this.wixTools)
            {
                wixTool.NoLogo = false;
                wixTool.SetOutputFileIfNotSpecified = false;
                wixTool.ExpectedOutputRegexs.Add(new Regex(string.Format(LogoTests.LogoOutputRegexString, Regex.Escape(wixTool.ToolDescription))));
                wixTool.Run();
            }
        }

        [NamedFact]
        [Description("Verify that different Wix tools do not print the Logo information.")]
        [Priority(2)]
        public void PrintWithoutLogo()
        {
            bool missingLogo = false;
            string errorMessage = string.Empty;

            foreach (WixTool wixTool in this.wixTools)
            {
                wixTool.NoLogo = true;
                wixTool.SetOutputFileIfNotSpecified = false;

                Result result = wixTool.Run();

                Regex LogoOutputRegex = new Regex("(.)*" + string.Format(LogoTests.LogoOutputRegexString, Regex.Escape(wixTool.ToolDescription)) + "(.)*");

                if (LogoOutputRegex.IsMatch(result.StandardOutput))
                {
                    missingLogo = true;
                    errorMessage += string.Format("Wix Tool {0} prints the Logo information with -nolog set.{1}", wixTool.ToolDescription, Environment.NewLine);
                }
            }

            Assert.False(missingLogo, errorMessage);
        }


        [NamedFact]
        [Description("Verify that logo is printed before any other warnings/messages.")]
        [Priority(2)]
        public void LogoPrintingOrder()
        {
            bool missingLogo = false;
            string errorMessage = string.Empty;

            foreach (WixTool wixTool in this.wixTools)
            {
                wixTool.NoLogo = false;
                wixTool.SetOutputFileIfNotSpecified = false;
                wixTool.OtherArguments = " -InvalidCommandLineArgument";
                Result result = wixTool.Run();
                Regex LogoOutputRegex = new Regex(string.Format(LogoTests.LogoOutputRegexString, Regex.Escape(wixTool.ToolDescription)) + "(.)*");

                if (!LogoOutputRegex.IsMatch(result.StandardOutput))
                {
                    missingLogo = true;
                    errorMessage += string.Format("Wix Tool {0} Logo information does not show as the first line with -nolog set.{1}", wixTool.ToolDescription, Environment.NewLine);
                }
            }

            Assert.False(missingLogo, errorMessage);
        }
    }
}