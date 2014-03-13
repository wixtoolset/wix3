//-----------------------------------------------------------------------
// <copyright file="DarkStaticMethods.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Static Methods for Dark</summary>
//-----------------------------------------------------------------------
namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using System.IO;
    using System.Xml;

    /// <summary>
    /// Dark tool class.
    /// </summary>
    public partial class Dark : WixTool
    {
        public static string Decompile(
            string inputFile,
            string binaryPath = null,
            WixMessage[] expectedWixMessages = null,
            string[] extensions = null,
            bool noTidy = false,
            bool noLogo = false,
            string otherArguments = null,
            bool setOutputFileIfNotSpecified = true,
            bool suppressDroppingEmptyTables = false,
            bool suppressRelativeActionSequences = false,
            bool suppressUITables = false,
            int[] suppressWarnings = null,
            bool treatWarningsAsErrors = false,
            bool verbose = false,
            bool xmlOutput = false)
        {

            Dark dark = new Dark();

            // set the passed arrguments
            dark.InputFile = inputFile;
            dark.BinaryPath = binaryPath;
            if (null != expectedWixMessages)
            {
                dark.ExpectedWixMessages.AddRange(expectedWixMessages);
            }
            if (null != extensions)
            {
                dark.Extensions.AddRange(extensions);
            }
            dark.NoTidy = noTidy;
            dark.NoLogo = noLogo;
            dark.OtherArguments = otherArguments;
            dark.SetOutputFileIfNotSpecified = setOutputFileIfNotSpecified;
            dark.SuppressDroppingEmptyTables = suppressDroppingEmptyTables;
            dark.SuppressRelativeActionSequences = suppressRelativeActionSequences;
            dark.SuppressUITables = suppressUITables;
            if (null != suppressWarnings)
            {
                dark.SuppressWarnings.AddRange(suppressWarnings);
            }
            dark.TreatWarningsAsErrors = treatWarningsAsErrors;
            dark.Verbose = verbose;
            dark.XmlOutput = xmlOutput;

            dark.Run();

            return dark.OutputFile;
        }
    }
}
