//-----------------------------------------------------------------------
// <copyright file="Help.HelpTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Test that the help is printed correctly
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Tools.Light.Help
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;

    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using WixTest;

    /// <summary>
    /// Test that the help is printed correctly
    /// </summary>
    [TestClass]
    public class HelpTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Tools\Light\Help\HelpTests");

        [TestMethod]
        [Description("Verify that Light help text is printed correctly")]
        [Priority(2)]
        public void Help()
        {
            Light light = new Light();
            light.Help = true;

            light.ExpectedOutputStrings.Add("usage:  light.exe [-?] [-b bindPath] [-nologo] [-out outputFile] objectFile [objectFile ...]");
            light.ExpectedOutputStrings.Add("-ai        allow identical rows, identical rows will be treated as a warning");
            light.ExpectedOutputStrings.Add("-au        (experimental) allow unresolved references");
            light.ExpectedOutputStrings.Add("-b <path>  specify a binder path to locate all files");
            light.ExpectedOutputStrings.Add("           (default: current directory)");
            light.ExpectedOutputStrings.Add("           prefix the path with 'name:' where 'name' is the name of your");
            light.ExpectedOutputStrings.Add("           named bindpath.");
            light.ExpectedOutputStrings.Add("-bcgg      use backwards compatible guid generation algorithm");
            light.ExpectedOutputStrings.Add("           (almost never needed)");
            light.ExpectedOutputStrings.Add("-bf        bind files into a wixout (only valid with -xo option)");
            light.ExpectedOutputStrings.Add("-cc <path> path to cache built cabinets (will not be deleted after linking)");
            light.ExpectedOutputStrings.Add("-ct <N>    number of threads to use when creating cabinets");
            light.ExpectedOutputStrings.Add("           (default: %NUMBER_OF_PROCESSORS%)");
            light.ExpectedOutputStrings.Add("-cub <file.cub> additional .cub file containing ICEs to run");
            light.ExpectedOutputStrings.Add("-cultures:<cultures>  semicolon or comma delimited list of localized");
            light.ExpectedOutputStrings.Add("          string cultures to load from .wxl files and libraries.");
            light.ExpectedOutputStrings.Add("         Precedence of cultures is from left to right.");
            light.ExpectedOutputStrings.Add("-d<name>[=<value>]  define a wix variable, with or without a value.");
            light.ExpectedOutputStrings.Add(" -dcl:level set default cabinet compression level");
            light.ExpectedOutputStrings.Add("           (low, medium, high, none, mszip; mszip default)");
            light.ExpectedOutputStrings.Add("-dut       drop unreal tables from the output image");
            light.ExpectedOutputStrings.Add("-ext <extension>  extension assembly or \"class, assembly\"");
            light.ExpectedOutputStrings.Add("-fv        add a 'fileVersion' entry to the MsiAssemblyName table");
            light.ExpectedOutputStrings.Add("           (rarely needed)");
            light.ExpectedOutputStrings.Add("-ice:<ICE>   run a specific internal consistency evaluator (ICE)");
            light.ExpectedOutputStrings.Add("-loc <loc.wxl>  read localization strings from .wxl file");
            light.ExpectedOutputStrings.Add("-nologo    skip printing light logo information");
            light.ExpectedOutputStrings.Add("-notidy    do not delete temporary files (useful for debugging)");
            light.ExpectedOutputStrings.Add("-o[ut]     specify output file (default: write to current directory)");
            light.ExpectedOutputStrings.Add("-pdbout <output.wixpdb>  save the WixPdb to a specific file");
            light.ExpectedOutputStrings.Add("           (default: same name as output with wixpdb extension)");
            light.ExpectedOutputStrings.Add("-pedantic  show pedantic messages");
            light.ExpectedOutputStrings.Add("-reusecab  reuse cabinets from cabinet cache");
            light.ExpectedOutputStrings.Add("-sa        suppress assemblies: do not get assembly name information");
            light.ExpectedOutputStrings.Add("           for assemblies");
            light.ExpectedOutputStrings.Add("-sacl      suppress resetting ACLs");
            light.ExpectedOutputStrings.Add("           (useful when laying out image to a network share)");
            light.ExpectedOutputStrings.Add("-sadmin    suppress default admin sequence actions");
            light.ExpectedOutputStrings.Add("-sadv      suppress default adv sequence actions");
            light.ExpectedOutputStrings.Add("-sf        suppress files: do not get any file information");
            light.ExpectedOutputStrings.Add("           (equivalent to -sa and -sh)");
            light.ExpectedOutputStrings.Add("-sh        suppress file info: do not get hash, version, language, etc");
            light.ExpectedOutputStrings.Add("-sice:<ICE>  suppress an internal consistency evaluator (ICE)");
            light.ExpectedOutputStrings.Add("-sl        suppress layout");
            light.ExpectedOutputStrings.Add("-sloc      suppress localization");
            light.ExpectedOutputStrings.Add("-sma       suppress processing the data in MsiAssembly table");
            light.ExpectedOutputStrings.Add("-spdb      suppress outputting the WixPdb");
            light.ExpectedOutputStrings.Add("-ss        suppress schema validation of documents (performance boost)");
            light.ExpectedOutputStrings.Add("-sts       suppress tagging sectionId attribute on rows");
            light.ExpectedOutputStrings.Add("-sui       suppress default UI sequence actions");
            light.ExpectedOutputStrings.Add("-sv        suppress intermediate file version mismatch checking");
            light.ExpectedOutputStrings.Add("-sval      suppress MSI/MSM validation");
            light.ExpectedOutputStrings.Add("-sw[N]     suppress all warnings or a specific message ID");
            light.ExpectedOutputStrings.Add("-swall     suppress all warnings (deprecated)");
            light.ExpectedOutputStrings.Add("-usf <output.xml>  unreferenced symbols file");
            light.ExpectedOutputStrings.Add("-v         verbose output");
            light.ExpectedOutputStrings.Add("-wx[N]     treat all warnings or a specific message ID as an error");
            light.ExpectedOutputStrings.Add("-wxall     treat all warnings as errors (deprecated)");
            light.ExpectedOutputStrings.Add("-xo        output wixout format instead of MSI format");
            light.ExpectedOutputStrings.Add("-? | -help this help information");
            
            light.Run();
        }
    }
}