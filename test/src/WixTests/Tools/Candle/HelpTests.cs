//-----------------------------------------------------------------------
// <copyright file="HelpTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Test how Candle handles the ? switch</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Candle.Help
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;
    
    /// <summary>
    /// Test how Candle handles the ? switch.
    /// </summary>
    [TestClass]
    public class HelpTests : WixTests
    {
        [TestMethod]
        [Description("Verify that Candle prints the appropriate help text.")]
        [Priority(1)]
        public void PrintHelp()
        {
            Candle candle = new Candle();
            this.AddExpectedHelpText(candle);
            candle.Run();
        }

        /// <summary>
        /// Add expected help text to candle object.
        /// </summary>
        /// <param name="candle">Candle object.</param>
        private void AddExpectedHelpText(Candle candle)
        {
            candle.ExpectedOutputStrings.Add(" usage:  candle.exe [-?] [-nologo] [-out outputFile] sourceFile [sourceFile ...]");
            candle.ExpectedOutputStrings.Add("-arch      x86, intel, x64, intel64, or ia64 (default: x86)");
            candle.ExpectedOutputStrings.Add("-d<name>[=<value>]  define a parameter for the preprocessor");
            candle.ExpectedOutputStrings.Add("-ext <extension>  extension assembly or \"class, assembly\"");
            candle.ExpectedOutputStrings.Add("-I<dir>    add to include search path");
            candle.ExpectedOutputStrings.Add("-nologo    skip printing candle logo information");
            candle.ExpectedOutputStrings.Add("-o[ut]     specify output file (default: write to current directory)");
            candle.ExpectedOutputStrings.Add("-p<file>   preprocess to a file (or stdout if no file supplied)");
            candle.ExpectedOutputStrings.Add("-pedantic  show pedantic messages");
            candle.ExpectedOutputStrings.Add("-sfdvital  suppress marking files as Vital by default");
            candle.ExpectedOutputStrings.Add("-ss        suppress schema validation of documents (performance boost)");
            candle.ExpectedOutputStrings.Add("-sw[N]     suppress all warnings or a specific message ID");
            candle.ExpectedOutputStrings.Add("           (example: -sw1009 -sw1103)");
            candle.ExpectedOutputStrings.Add("-swall     suppress all warnings (deprecated)");
            candle.ExpectedOutputStrings.Add("-trace     show source trace for errors, warnings, and verbose messages");
            candle.ExpectedOutputStrings.Add("-v         verbose output");
            candle.ExpectedOutputStrings.Add("-wx[N]     treat all warnings or a specific message ID as an error");
            candle.ExpectedOutputStrings.Add("           (example: -wx1009 -wx1103)"); 
            candle.ExpectedOutputStrings.Add("-wxall     treat all warnings as errors (deprecated)");
            candle.ExpectedOutputStrings.Add("-? | -help this help information");
            candle.ExpectedOutputStrings.Add("For more information see: http://wix.sourceforge.net");
       }
    }
}