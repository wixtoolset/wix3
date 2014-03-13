//-----------------------------------------------------------------------
// <copyright file="LightStaticMethods.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>A class that wraps Light</summary>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Specialized;

    /// <summary>
    /// A class that wraps Light
    /// </summary>
    public partial class Light : WixTool
    {
        public static string Link(string sourceFile)
        {
            return Light.Link(new string[] { sourceFile });
        }

        //public static string Link(string sourceFile, string otherArguments)
        //{
        //    return Light.Link(new string[] { sourceFile }, ootherArguments);
        //}

        //public static string Link(string sourceFile, string otherArguments, WixMessage[] expectedMessages)
        //{
        //    return Light.Link(new string[] { sourceFile }, otherArguments, expectedMessages);
        //}

        //public static string Link(string[] sourceFiles, string otherArguments)
        //{
        //    return Light.Link(sourceFiles, otherArguments, null);
        //}

        //public static string Link(string[] sourceFiles, string otherArguments, WixMessage[] expectedMessages)
        //{
        //    return Light.Link(sourceFiles, otherArguments, false, expectedMessages);
        //}

         

        public static string Link(
          string[] objectFiles,
          bool allowIdenticalRows = false,
          bool allowUnresolvedVariables = false,
          string bindPath = null,
          bool bindFiles = false,
          int cabbingThreads = 0,
          string cachedCabsPath = null,
          string cultures = null,
          WixMessage[] expectedWixMessages = null,
          string[] extensions = null,
          bool fileVersion = false,
          string[] ices = null,
          string[] locFiles = null,
          bool noTidy = false,
          string otherArguments = null,
          bool pedantic = false,
          bool reuseCab = false,
          bool setOutputFileIfNotSpecified = true,
          bool suppressACL = false,
          bool suppressAdmin = false,
          bool suppressADV = false,
          bool suppressAllWarnings = false,
          bool suppressAssemblies = false,
          bool suppressDroppingUnrealTables = false,
          string[] suppressedICEs = null,
          bool suppressFiles = false,
          bool suppressFileInfo = false,
          bool suppressIntermediateFileVersionCheck = false,
          bool suppressLayout = false,
          bool suppressMSIAndMSMValidation = false,
          bool suppressProcessingMSIAsmTable = false,
          bool suppressSchemaValidation = false,
          bool suppressUI = false,
          string[] suppressWarnings = null,
          bool tagSectionId = false,
          bool tagSectionIdAndGenerateWhenNull = false,
          bool treatAllWarningsAsErrors = false,
          int[] treatWarningsAsErrors = null,
          string unreferencedSymbolsFile = null,
          bool verbose = false,
          StringDictionary wixVariables = null,
          bool xmlOutput = false)
        {
            Light light = new Light();

            if (null == objectFiles || objectFiles.Length == 0)
            {
                throw new ArgumentException("objectFiles cannot be null or empty");
            }

            // set the passed arrguments
            light.ObjectFiles.AddRange(objectFiles);
            light.AllowIdenticalRows = allowIdenticalRows;
            light.AllowUnresolvedVariables = allowUnresolvedVariables;
            light.BindPath = bindPath;
            light.BindFiles = bindFiles;
            light.CabbingThreads = cabbingThreads;
            light.CachedCabsPath = cachedCabsPath;
            light.Cultures = cultures;
            if (null != expectedWixMessages)
            {
                light.ExpectedWixMessages.AddRange(expectedWixMessages);
            }
            if (null != extensions)
            {
                light.Extensions.AddRange(extensions);
            }
            light.FileVersion = fileVersion;
            if (null != ices)
            {
                light.ICEs.AddRange(ices);
            }
            if (null != locFiles)
            {
                light.LocFiles.AddRange(locFiles);
            }
            light.NoTidy = noTidy;
            light.OtherArguments = otherArguments;
            light.Pedantic = pedantic;
            light.ReuseCab = reuseCab;
            light.SetOutputFileIfNotSpecified = setOutputFileIfNotSpecified;
            light.SuppressACL = suppressACL;
            light.SuppressAdmin = suppressAdmin;
            light.SuppressADV = suppressADV;
            light.SuppressAllWarnings = suppressAllWarnings;
            light.SuppressAssemblies = suppressAssemblies;
            light.SuppressDroppingUnrealTables = suppressDroppingUnrealTables;
            if (null != suppressedICEs)
            {
                light.SuppressedICEs.AddRange(suppressedICEs);
            }
            light.SuppressFiles = suppressFiles;
            light.SuppressFileInfo = suppressFileInfo;
            light.SuppressIntermediateFileVersionCheck = suppressIntermediateFileVersionCheck;
            light.SuppressLayout = suppressLayout;
            light.SuppressMSIAndMSMValidation = suppressMSIAndMSMValidation;
            light.SuppressProcessingMSIAsmTable = suppressProcessingMSIAsmTable;
            light.SuppressSchemaValidation = suppressSchemaValidation;
            light.SuppressUI = suppressUI;
            if (null != suppressWarnings)
            {
                light.SuppressWarnings.AddRange(suppressWarnings);
            }
            light.TagSectionId = tagSectionId;
            light.TagSectionIdAndGenerateWhenNull = tagSectionIdAndGenerateWhenNull;
            light.TreatAllWarningsAsErrors = treatAllWarningsAsErrors;
            if (null != treatWarningsAsErrors)
            {
                light.TreatWarningsAsErrors.AddRange(treatWarningsAsErrors);
            }
            light.UnreferencedSymbolsFile = unreferencedSymbolsFile;
            light.Verbose = verbose;
            if (null != wixVariables)
            {
                foreach (string key in wixVariables.Keys)
                {
                    if (!light.WixVariables.ContainsKey(key))
                    {
                        light.WixVariables.Add(key, wixVariables[key]);
                    }
                    else
                    {
                        light.WixVariables[key] = wixVariables[key];
                    }
                }
            }
            light.XmlOutput = xmlOutput;

            light.Run();

            return light.OutputFile;
        }
    }
}
