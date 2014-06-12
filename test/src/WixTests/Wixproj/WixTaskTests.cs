//-----------------------------------------------------------------------
// <copyright file="WixTaskTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//      Test that that the parameters supported by Candle,
//      Light and Lit are up to date in the MSBuild task.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Wixproj
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Text;
    using WixTest;

    public class WixTaskTests : WixTests
    {
        private static readonly string testWiXInstallerx86 = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Wixproj\WixTaskTests\ProductWixInstallerx86\ProductWixInstallerx86.wixproj");
        private static readonly string testWiXInstallerx64 = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Wixproj\WixTaskTests\ProductWixInstallerx64\ProductWixInstallerx64.wixproj");
        private static readonly string testWiXLibraryx86 = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Wixproj\WixTaskTests\ProductWixLibraryx86\ProductWixLibraryx86.wixproj");
        private static readonly string testWiXLibraryx64 = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Wixproj\WixTaskTests\ProductWixLibraryx64\ProductWixLibraryx64.wixproj");

        [NamedFact]
        [Priority(1)]
        [Description("Tests building WiX Project (candle and light tasks) with different configurations.")]
        public void BuildWiXInstaller()
        {
            // Configuration=Debug; Platform=x86;
            WixTaskTests.TestBuildWiXInstallerTasks(WixTaskTests.testWiXInstallerx86, "Build", "Debug", "x86");

            // Configuration=Release; Platform=x86;
            WixTaskTests.TestBuildWiXInstallerTasks(WixTaskTests.testWiXInstallerx86, "Build", "Release", "x86");

            // Configuration=Debug; Platform=x64;
            WixTaskTests.TestBuildWiXInstallerTasks(WixTaskTests.testWiXInstallerx64, "Build", "Debug", "x64");

            // Configuration=Releasee; Platform=x64;
            WixTaskTests.TestBuildWiXInstallerTasks(WixTaskTests.testWiXInstallerx64, "Build", "Release", "x64");
        }

        [NamedFact]
        [Priority(1)]
        [Description("Tests rebuilding WiX Project (candle and light tasks) with different configurations.")]
        public void RebuildWiXInstaller()
        {
            // Configuration=Debug; Platform=x86;
            WixTaskTests.TestBuildWiXInstallerTasks(WixTaskTests.testWiXInstallerx86, "Rebuild", "Debug", "x86");

            // Configuration=Release; Platform=x86;
            WixTaskTests.TestBuildWiXInstallerTasks(WixTaskTests.testWiXInstallerx86, "Rebuild", "Release", "x86");

            // Configuration=Debug; Platform=x64;
            WixTaskTests.TestBuildWiXInstallerTasks(WixTaskTests.testWiXInstallerx64, "Rebuild", "Debug", "x64");

            // Configuration=Release; Platform=x64;
            WixTaskTests.TestBuildWiXInstallerTasks(WixTaskTests.testWiXInstallerx64, "Rebuild", "Release", "x64");
        }

        [NamedFact]
        [Priority(2)]
        [Description("Tests cleaning WiX Project (candle and light tasks) with different configurations.")]
        public void CleanWiXInstaller()
        {
            // Configuration=Debug; Platform=x86;
            WixTaskTests.TestCleanWiXInstaller(WixTaskTests.testWiXInstallerx86, "Debug", "x86");

            // Configuration=Release; Platform=x86;
            WixTaskTests.TestCleanWiXInstaller(WixTaskTests.testWiXInstallerx86, "Release", "x86");

            // Configuration=Debug; Platform=x64;
            WixTaskTests.TestCleanWiXInstaller(WixTaskTests.testWiXInstallerx64, "Debug", "x64");

            // Configuration=Release; Platform=x64;
            WixTaskTests.TestCleanWiXInstaller(WixTaskTests.testWiXInstallerx64, "Release", "x64");
        }

        [NamedFact]
        [Priority(1)]
        [Description("Tests building WiX Library Project (candle and lit tasks) with different configurations.")]
        public void BuildWiXLibrary()
        {
            // Configuration=Debug; Platform=x86;
            WixTaskTests.TestBuildWiXLibraryTasks(WixTaskTests.testWiXLibraryx86, "Build", "Debug", "x86");

            // Configuration=Release; Platform=x86;
            WixTaskTests.TestBuildWiXLibraryTasks(WixTaskTests.testWiXLibraryx86, "Build", "Release", "x86");

            // Configuration=Debug; Platform=x64;
            WixTaskTests.TestBuildWiXLibraryTasks(WixTaskTests.testWiXLibraryx64, "Build", "Debug", "x64");

            // Configuration=Release; Platform=x64;
            WixTaskTests.TestBuildWiXLibraryTasks(WixTaskTests.testWiXLibraryx64, "Build", "Release", "x64");
        }

        [NamedFact]
        [Priority(1)]
        [Description("Tests rebuilding WiX Library Project (candle and lit tasks) with different configurations.")]
        public void RebuildWiXLibrary()
        {
            // Configuration=Debug; Platform=x86;
            WixTaskTests.TestBuildWiXLibraryTasks(WixTaskTests.testWiXLibraryx86, "Rebuild", "Debug", "x86");

            // Configuration=Release; Platform=x86;
            WixTaskTests.TestBuildWiXLibraryTasks(WixTaskTests.testWiXLibraryx86, "Rebuild", "Release", "x86");

            // Configuration=Debug; Platform=x64;
            WixTaskTests.TestBuildWiXLibraryTasks(WixTaskTests.testWiXLibraryx64, "Rebuild", "Debug", "x64");

            // Configuration=Release; Platform=x64;
            WixTaskTests.TestBuildWiXLibraryTasks(WixTaskTests.testWiXLibraryx64, "Rebuild", "Release", "x64");
        }

        [NamedFact]
        [Priority(2)]
        [Description("Tests cleaning WiX Library Project (candle and lit tasks) with different configurations.")]
        public void CleanWiXLibrary()
        {
            // Configuration=Debug; Platform=x86;
            WixTaskTests.TestCleanWiXLibrary(WixTaskTests.testWiXLibraryx86, "Debug", "x86");

            // Configuration=Release; Platform=x86;
            WixTaskTests.TestCleanWiXLibrary(WixTaskTests.testWiXLibraryx86, "Release", "x86");

            // Configuration=Debug; Platform=x64;
            WixTaskTests.TestCleanWiXLibrary(WixTaskTests.testWiXLibraryx64, "Debug", "x64");

            // Configuration=Release; Platform=x64;
            WixTaskTests.TestCleanWiXLibrary(WixTaskTests.testWiXLibraryx64, "Release", "x64");
        }

        /// <summary>
        /// Runs the MSBuild for a WiX Project passing build parameters and verifies that subtasks were ran correctly
        /// </summary>
        /// <param name="wixProjectFile">Path to the WiX Project to be build</param>
        /// <param name="target">Target parameter</param>
        /// <param name="configuration">Configuration parameter</param>
        /// <param name="architecture">Architecture parameter</param>
        /// <returns>The WixprojMSBuild object that was run</returns>
        private static WixprojMSBuild TestBuildWiXInstallerTasks(string wixProjectFile, string target, string configuration, string architecture)
        {
            // Initialize build parameters
            WixprojMSBuild wixprojMSBuild = new WixprojMSBuild();
            wixprojMSBuild.ProjectFile = wixProjectFile;
            wixprojMSBuild.Targets.Add(target);
            wixprojMSBuild.Properties.Add("Platform", architecture);
            wixprojMSBuild.Properties.Add("Configuration", configuration);
            wixprojMSBuild.OutputRootDirectory = Utilities.FileUtilities.GetUniqueFileName();
            wixprojMSBuild.Run();

            // Assert that the compilation ran correct and without errors
            wixprojMSBuild.AssertTaskExists("Candle");
            wixprojMSBuild.AssertNotExistsTaskSubstring("Candle", "error", true);
            wixprojMSBuild.AssertNotExistsTaskSubstring("Candle", "warning", true);
            wixprojMSBuild.AssertTaskSubstring("Candle", string.Format("-arch {0}", architecture));

            // Assert that the linking ran correct and without errors
            wixprojMSBuild.AssertTaskExists("Light");
            wixprojMSBuild.AssertNotExistsTaskSubstring("Light", "error", true);
            wixprojMSBuild.AssertNotExistsTaskSubstring("Light", "warning", true);

            return wixprojMSBuild;
        }

        /// <summary>
        /// Runs the MSBuild for a WiX Library Project passing build parameters and verifies that subtasks were ran correctly
        /// </summary>
        /// <param name="wixProjectFile">Path to the WiX Project to be build</param>
        /// <param name="target">Target parameter</param>
        /// <param name="configuration">Configuration parameter</param>
        /// <param name="architecture">Architecture parameter</param>
        /// <returns>The WixprojMSBuild object that was run</returns>
        private static WixprojMSBuild TestBuildWiXLibraryTasks(string wixProjectFile, string target, string configuration, string architecture)
        {
            // Initialize build parameters
            WixprojMSBuild wixprojMSBuild = new WixprojMSBuild();
            wixprojMSBuild.ProjectFile = wixProjectFile;
            wixprojMSBuild.Targets.Add(target);
            wixprojMSBuild.Properties.Add("Platform", architecture);
            wixprojMSBuild.Properties.Add("Configuration", configuration);
            wixprojMSBuild.OutputRootDirectory = Utilities.FileUtilities.GetUniqueFileName();
            wixprojMSBuild.Run();

            // Assert that the compilation ran correct and without errors
            wixprojMSBuild.AssertTaskExists("Candle");
            wixprojMSBuild.AssertNotExistsTaskSubstring("Candle", "error", true);
            wixprojMSBuild.AssertNotExistsTaskSubstring("Candle", "warning", true);
            wixprojMSBuild.AssertTaskSubstring("Candle", string.Format("-arch {0}", architecture));

            // Assert that the linking ran correct and without errors
            wixprojMSBuild.AssertTaskExists("Lit");
            wixprojMSBuild.AssertNotExistsTaskSubstring("Lit", "error", true);
            wixprojMSBuild.AssertNotExistsTaskSubstring("Lit", "warning", true);

            return wixprojMSBuild;
        }

        /// <summary>
        /// Runs MSBuild for a WiX Project with target parameter 'Clean' for particular configuration and architecture and verifies that subtasks were ran correctly
        /// </summary>
        /// <param name="wixProjectFile">Path to the WiX Project to be build</param>
        /// <param name="configuration">Configuration parameter</param>
        /// <param name="architecture">Architecture parameter</param>
        private static void TestCleanWiXInstaller(string wixProjectFile, string configuration, string architecture)
        {
            WixprojMSBuild wixprojMSBuild = WixTaskTests.TestBuildWiXInstallerTasks(wixProjectFile, "Build", configuration, architecture);

            wixprojMSBuild.OtherArguments = string.Format("/t:Clean /p:Configuration={0};Platform={1}", configuration, architecture);
            wixprojMSBuild.Run();
        }

        /// <summary>
        /// Runs MSBuild for a WiX Library Project with target parameter 'Clean' for particular configuration and architecture and verifies that subtasks were ran correctly
        /// </summary>
        /// <param name="wixProjectFile">Path to the WiX Project to be build</param>
        /// <param name="configuration">Configuration parameter</param>
        /// <param name="architecture">Architecture parameter</param>
        private static void TestCleanWiXLibrary(string wixProjectFile, string configuration, string architecture)
        {
            WixprojMSBuild wixprojMSBuild = WixTaskTests.TestBuildWiXLibraryTasks(wixProjectFile, "Build", configuration, architecture);

            wixprojMSBuild.OtherArguments = string.Format("/t:Clean /p:Configuration={0};Platform={1}", configuration, architecture);
            wixprojMSBuild.Run();
        }
    }
}
