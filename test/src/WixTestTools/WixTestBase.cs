//-----------------------------------------------------------------------
// <copyright file="WixTestBase.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using Microsoft.Win32;
    using WixTest.Verifiers;

    /// <summary>
    /// Base class for WiX tests.
    /// </summary>
    public class WixTestBase : ISupportNamedFacts, IDisposable
    {
        public static string Seed { get; private set; }

        public string TestName { get; private set; }

        public string TestFolder { get; private set; }

        public string TestDataFolder { get; private set; }

        public List<FileSystemInfo> TestArtifacts { get; private set; }

        public WixTestContext TestContext { get; private set; }

        /// <summary>
        /// Common extensions for building packages and bundles.
        /// </summary>
        protected static readonly string[] Extensions = new string[] { "WixBalExtension", "WixDependencyExtension", "WixTagExtension", "WixUtilExtension" };

        /// <summary>
        /// The name of the environment variable that stores the MSBuild directory
        /// </summary>
        private const string msBuildDirectoryEnvironmentVariable = "WixTestMSBuildDirectory";

        /// <summary>
        /// The name of the environment variable that states that the runtime tests are enabled on this machine
        /// </summary>
        private const string runtimeTestsEnabledEnvironmentVariable = "RuntimeTestsEnabled";

        /// <summary>
        /// The name of the environment variable that stores the WiX build output directory.
        /// </summary>
        private const string wixBuildPathDirectory = "WixBuildPathDirectory";

        /// <summary>
        /// The name of the environment variable that stores the WiX bin directory
        /// </summary>
        private const string wixToolsPathEnvironmentVariable = "WixToolsPath";

        /// <summary>
        /// The name of the environment variable that stores the wix.targets path
        /// </summary>
        private const string wixTargetsPathEnvironmentVariable = "WixTargetsPath";

        /// <summary>
        /// The name of the environment variable that stores the WixTasks.dll path
        /// </summary>
        private const string wixTasksPathEnvironmentVariable = "WixTasksPath";

        private bool cleanupFiles;

        private string originalCurrentDirectory;

        /// <summary>
        /// Initialzes the static values for the tests in the assembly.
        /// </summary>
        static WixTestBase()
        {
            WixTestBase.Seed = DateTime.Now.ToString("yyyy-MM-ddTHH.mm.ss");

            WixTestBase.SetMSBuildPaths();
            WixTestBase.SetWixToolsPathDirectory();
            WixTestBase.SetWixBuildDirectory();
        }

        /// <summary>
        /// Initialize Wix tests.
        /// </summary>
        /// <remarks>This method will check that a test has pre-reqs set.</remarks>
        /// <remarks>This method will end the execution of tests marked as IsRuntimeTest=true if the Runtime tests are not enabled on current machine.</remarks>
        /// <remarks>This method will end the execution of tests marked as Is64BitSpecificTest=true if the current OS is not a 64 bit OS.</remarks>
        /// <remarks>Create a unique directory for this test to store test artifacts</remarks>
        public WixTestBase()
        {

            // Check if test is a runtime test and if test is 64 bit specific
            //System.Reflection.MethodInfo testMethodInformation = this.GetType().GetMethod(this.TestContext.TestName);
            //TestPropertyAttribute[] customTestMethodProperties = (TestPropertyAttribute[])testMethodInformation.GetCustomAttributes(typeof(TestPropertyAttribute), false);

            //foreach (TestPropertyAttribute property in customTestMethodProperties)
            //{
            //    if (property.Name.Equals("IsRuntimeTest") && property.Value.Equals("true") && !this.IsRuntimeTestsEnabled && !Debugger.IsAttached)
            //    {
            //        Assert.Fail("Runtime tests are not enabled on this test environment. To enable Runtime tests set the environment variable '{0}'=true or run the tests under a debugger.", WixTests.runtimeTestsEnabledEnvironmentVariable);
            //    }

            //    if (property.Name.Equals("Is64BitSpecificTest") && property.Value.Equals("true") && !this.Is64BitMachine)
            //    {
            //        Assert.Fail("64-bit specific tests are not enabled on 32-bit machines.");
            //    }
            //}
        }

        /// <summary>
        /// Returns true if the current OS is a 64 bit OS
        /// </summary>
        public bool Is64BitMachine
        {
            get
            {
                bool isWow64Process;
                IsWow64Process(Process.GetCurrentProcess().Handle, out isWow64Process);
                // it is a 64 bit system iff this is a 64 bit process or a 32 bit process running on WoW
                return (IntPtr.Size == 8 || (IntPtr.Size == 4 && isWow64Process));
            }
        }

        /// <summary>
        /// Determines whether runtime tests are enabled on the current machine.
        /// </summary>
        public bool IsRuntimeTestsEnabled
        {
            get
            {
                string runtimeTestsEnabled = Environment.GetEnvironmentVariable(WixTestBase.runtimeTestsEnabledEnvironmentVariable);
                return "true".Equals(runtimeTestsEnabled, StringComparison.OrdinalIgnoreCase);
            }
        }

        /// <summary>
        /// Called by tests to initialize test name.
        /// </summary>
        public void SetFactName(string testNamespace, string testClass, string testMethod)
        {
            this.Initialize(testClass, testMethod);
        }

        /// <summary>
        /// Clean up after the test method.
        /// </summary>
        public void Dispose()
        {
            this.Cleanup(this.cleanupFiles);
        }

        /// <summary>
        /// Called by a test before any other operations can executed. Consider marking the test a [NamedFact] instead to
        /// have the initialization completed automatically.
        /// </summary>
        /// <param name="dataFolder">Indicates the folder where build files will be found. Defaults to the name of the test.</param>
        /// <param name="testName">Defaults to the name of the test method.</param>
        /// <returns>Initialized test context.</returns>
        protected WixTestContext Initialize(string dataFolder = null, string testName = null)
        {
            if (String.IsNullOrEmpty(testName) && String.IsNullOrEmpty(this.TestName))
            {
                StackTrace st = new StackTrace();
                StackFrame sf = st.GetFrame(1);
                sf.GetMethod();

                testName = sf.GetMethod().Name;
            }

            if (!String.IsNullOrEmpty(testName))
            {
                this.TestName = testName;
            }

            this.TestFolder = Path.Combine(Path.GetTempPath(), "wix_tests", WixTestBase.Seed, this.TestName);

            if (String.IsNullOrEmpty(dataFolder))
            {
                dataFolder = this.TestName;
            }

            string running = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            this.TestDataFolder = Path.Combine(running, dataFolder);

            this.TestArtifacts = new List<FileSystemInfo>();
            this.TestArtifacts.Add(new DirectoryInfo(this.TestFolder));

            Directory.CreateDirectory(this.TestFolder);

            this.originalCurrentDirectory = Directory.GetCurrentDirectory();
            Directory.SetCurrentDirectory(this.TestFolder);

            return this.TestContext = new WixTestContext() { Seed = WixTestBase.Seed, TestArtifacts = this.TestArtifacts, TestDataDirectory = this.TestDataFolder, TestDirectory = this.TestFolder, TestName = this.TestName };
        }

        /// <summary>
        /// Called by a test to indicate that the test completed and all test files can be cleaned up.
        /// </summary>
        protected void Completed()
        {
            this.cleanupFiles = true;
        }

        protected void DuplicateTestDataToTestFolder()
        {
            foreach (string source in Directory.GetFiles(this.TestDataFolder, "*", SearchOption.AllDirectories))
            {
                string target = Path.Combine(this.TestFolder, Path.GetFileName(source));
                File.Copy(source, target);
            }
        }

        protected PackageBuilder CreatePackage(string name, Dictionary<string, string> bindPaths = null, Dictionary<string, string> preprocessorVariables = null, string[] extensions = null)
        {
            PackageBuilder builder = new PackageBuilder(this.TestContext.TestName, name, this.TestContext.TestDataDirectory, this.TestContext.TestArtifacts);

            if (null != bindPaths)
            {
                builder.BindPaths = bindPaths;
            }

            if (null != preprocessorVariables)
            {
                builder.PreprocessorVariables = preprocessorVariables;
            }

            builder.Extensions = null == extensions ? WixTestBase.Extensions : extensions;

            return builder.Build();
        }

        protected BundleBuilder CreateBundle(string name, Dictionary<string, string> bindPaths = null, Dictionary<string, string> preprocessorVariables = null, string[] extensions = null)
        {
            BundleBuilder builder = new BundleBuilder(this.TestContext.TestName, name, this.TestContext.TestDataDirectory, this.TestContext.TestArtifacts);

            if (null != bindPaths)
            {
                builder.BindPaths = bindPaths;
            }

            if (null != preprocessorVariables)
            {
                builder.PreprocessorVariables = preprocessorVariables;
            }

            builder.Extensions = null == extensions ? WixTestBase.Extensions : extensions;

            return builder.Build();
        }

        /// <summary>
        /// Gets the test install directory for the current test.
        /// </summary>
        /// <param name="additionalPath">Additional subdirectories under the test install directory.</param>
        /// <returns>Full path to the test install directory.</returns>
        /// <remarks>
        /// The package or bundle must install into [ProgramFilesFolder]\~Test WiX\[TestName]\([Additional]).
        /// </remarks>
        protected string GetTestInstallFolder(string additionalPath = null)
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFilesX86), "~Test WiX", this.TestContext.TestName, additionalPath ?? String.Empty);
        }

        /// <summary>
        /// Opens and gets the test registry key for the current test.
        /// </summary>
        /// <param name="additionalPath">Additional subkeys under the test registry key.</param>
        /// <returns>Full path to the test registry key.</returns>
        /// <remarks>
        /// The package must write into HKLM\Software\WiX\Tests\[TestName]\([Additional]).
        /// </remarks>
        protected RegistryKey GetTestRegistryRoot(string additionalPath = null)
        {
            string key = String.Format(@"Software\WiX\Tests\{0}\{1}", this.TestContext.TestName, additionalPath ?? String.Empty);
            return Registry.LocalMachine.OpenSubKey(key, true);
        }

        /// <summary>
        /// Clean up after the test.
        /// </summary>
        /// <param name="removeFiles">True if the files should also be removed. False to leave them behind for debugging purposes.</param>
        private void Cleanup(bool removeFiles)
        {
            PackageBuilder.CleanupByUninstalling();
            MSIExec.UninstallAllInstalledProducts();
            BundleBuilder.CleanupByUninstalling();

            MsiVerifier.Reset();

            this.ResetRegistry();
            this.ResetDirectory();

            if (removeFiles)
            {
                foreach (FileSystemInfo artifact in this.TestArtifacts)
                {
                    try
                    {
                        if (artifact is DirectoryInfo)
                        {
                            Directory.Delete(artifact.FullName, true);
                        }
                        else
                        {
                            artifact.Delete();
                        }
                    }
                    catch
                    {
                    }
                }
            }
        }

        /// <summary>
        /// Reset the current folder back where it was before the test was initialized.
        /// </summary>
        /// <returns>Original current folder.</returns>
        private void ResetDirectory()
        {
            if (!String.IsNullOrEmpty(this.originalCurrentDirectory))
            {
                Directory.SetCurrentDirectory(this.originalCurrentDirectory);
            }
        }

        /// <summary>
        /// Reset the registry keys related to the test.
        /// </summary>
        private void ResetRegistry()
        {
            string key = String.Format(@"Software\WiX\Tests\{0}", this.TestName);
            Registry.LocalMachine.DeleteSubKeyTree(key, false);
            Registry.LocalMachine.DeleteSubKeyTree(@"Software\WiX\Tests\TestBAControl", false);
        }

        #region P/Invoke declarations
        /// <summary>
        /// Returns true if the current process is Wow64 process.
        /// </summary>
        /// <param name="hProcess">Process handle</param>
        /// <param name="wow64Process">Return bool</param>
        /// <returns></returns>
        [DllImport("kernel32.dll", SetLastError = true, CallingConvention = CallingConvention.Winapi)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool IsWow64Process([In] IntPtr hProcess, [Out] out bool wow64Process);
        #endregion

        /// <summary>
        /// Sets the default location for MSBuild.exe, wix.targets and WixTasks.dll
        /// </summary>
        private static void SetMSBuildPaths()
        {
            // MSBuild Directory
            string msBuildDirectory = Environment.GetEnvironmentVariable(WixTestBase.msBuildDirectoryEnvironmentVariable);
            if (null == msBuildDirectory)
            {
                // Default to MSBuild v3.5.
                msBuildDirectory = Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), @"Microsoft.NET\Framework\v3.5");
            }

            Settings.MSBuildDirectory = msBuildDirectory;

            // wix.targets
            string wixTargetsPath = Environment.GetEnvironmentVariable(WixTestBase.wixTargetsPathEnvironmentVariable);
            if (null != wixTargetsPath)
            {
                Settings.WixTargetsPath = wixTargetsPath;
            }
            else // check if the wix.targets file is next to the test assembly.
            {
                wixTargetsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "wix.targets");
                if (File.Exists(wixTargetsPath))
                {
                    Settings.WixTargetsPath = wixTargetsPath;
                }
                else
                {
                    Console.WriteLine("The environment variable '{0}' was not set. The location for wix.targets will not be explicitly specified to MSBuild.", WixTestBase.wixTargetsPathEnvironmentVariable);
                }
            }

            // WixTasks.dll
            string wixTasksPath = Environment.GetEnvironmentVariable(WixTestBase.wixTasksPathEnvironmentVariable);
            if (null != wixTasksPath)
            {
                Settings.WixTasksPath = wixTasksPath;
            }
            else // check if the WixTasks.dll is next to the test assembly.
            {
                wixTasksPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "WixTask.dll");
                if (File.Exists(wixTasksPath))
                {
                    Settings.WixTasksPath = wixTasksPath;
                }
                else
                {
                    Console.WriteLine("The environment variable '{0}' was not set. The location for WixTasks.dll will not be explicitly specified to MSBuild.", WixTestBase.wixTasksPathEnvironmentVariable);
                }
            }
        }

        /// <summary>
        /// Sets the default location for the WiX binaries
        /// </summary>
        private static void SetWixBuildDirectory()
        {
            string wixBuildPathDirectory = Environment.GetEnvironmentVariable(WixTestBase.wixBuildPathDirectory);
            if (null == wixBuildPathDirectory)
            {

                wixBuildPathDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

            Settings.WixBuildDirectory = wixBuildPathDirectory;
        }

        /// <summary>
        /// Sets the default location for the WiX binaries
        /// </summary>
        private static void SetWixToolsPathDirectory()
        {
            string wixToolsPathDirectory = Environment.GetEnvironmentVariable(WixTestBase.wixToolsPathEnvironmentVariable);
            if (null == wixToolsPathDirectory)
            {
                wixToolsPathDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            }

            Settings.WixToolDirectory = wixToolsPathDirectory;
        }
    }
}
