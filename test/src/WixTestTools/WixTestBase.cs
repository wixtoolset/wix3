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
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Threading;
    using WixTest.Utilities;
    using WixTest.Verifiers;
    using Xunit;
    using Xunit.Sdk;

    /// <summary>
    /// Base class for WiX tests.
    /// </summary>
    public abstract class WixTestBase : IDisposable, ITestClass, IUseFixture<WixTestContext>
    {
        private static string originalWixRootValue;
        private static readonly string projectDirectory;
        private static int references = 0;
        private static readonly string seed;

        // The name of the environment variable that stores the MSBuild directory
        private static readonly string envWixTestMSBuildDirectory = "WixTestMSBuildDirectory";

        // The name of the environment variable that stores the WiX build output directory.
        private static readonly string envWixBuildPathDirectory = "WixBuildPathDirectory";

        // The name of the environment variable that stores the WiX bin directory
        private static readonly string envWixToolsPath = "WixToolsPath";

        // The name of the environment variable that stores the wix.targets path
        private static readonly string envWixTargetsPath = "WixTargetsPath";

        // The name of the environment variable that stores the WixTasks.dll path
        private static readonly string envWixTasksPath = "WixTasksPath";

        /// <summary>
        /// Common extensions for building packages and bundles.
        /// </summary>
        protected static readonly string[] Extensions = new string[]
        {
            "WixBalExtension",
            "WixDependencyExtension",
            "WixIIsExtension",
            "WixTagExtension",
            "WixUtilExtension",
        };

        private bool cleanArtifacts;
        private Stack<string> currentDirectories = new Stack<string>();

        /// <summary>
        /// Initialize static variables and settings.
        /// </summary>
        static WixTestBase()
        {
            WixTestBase.seed = DateTime.Now.ToString("yyyy-MM-ddTHH.mm.ss");
            WixTestBase.projectDirectory = FileUtilities.GetDirectoryNameOfFileAbove("wix.proj");

            Settings.Seed = seed;

            WixTestBase.InitializeSettings();
        }

        /// <summary>
        /// Initializes the test base class.
        /// </summary>
        public WixTestBase()
        {
            if (1 == Interlocked.Increment(ref references))
            {
                WixTestBase.originalWixRootValue = Environment.GetEnvironmentVariable("WIX_ROOT");
                Environment.SetEnvironmentVariable("WIX_ROOT", WixTestBase.projectDirectory);
            }
        }

        ~WixTestBase()
        {
            if (0 == Interlocked.Decrement(ref WixTestBase.references))
            {
                Environment.SetEnvironmentVariable("WIX_ROOT", WixTestBase.originalWixRootValue);
            }
        }

        /// <summary>
        /// A list of test artifacts for the current test.
        /// </summary>
        public List<FileSystemInfo> TestArtifacts { get; private set; }

        /// <summary>
        /// The test context for the current test.
        /// </summary>
        public WixTestContext TestContext { get; private set; }

        /// <summary>
        /// Called by a test case to indicate the test is completed and test artifacts can be cleaned up.
        /// </summary>
        protected void Complete()
        {
            this.cleanArtifacts = true;
        }

        /// <summary>
        /// Initializes the test class.
        /// </summary>
        protected virtual void ClassInitialize()
        {
        }

        /// <summary>
        /// Initializes a single test case.
        /// </summary>
        protected virtual void TestInitialize()
        {
        }

        /// <summary>
        /// Uninitializes a single test case.
        /// </summary>
        protected virtual void TestUninitialize()
        {
        }

        /// <summary>
        /// Uninitializes the test class.
        /// </summary>
        protected virtual void ClassUninitialize()
        {
        }

        void IUseFixture<WixTestContext>.SetFixture(WixTestContext data)
        {
            this.ClassInitialize();
        }

        void ITestClass.TestInitialize(string testNamespace, string testClass, string testMethod)
        {
            this.InitializeContext(testNamespace, testClass, testMethod);
            this.TestInitialize();
        }

        void ITestClass.TestUninitialize(MethodResult result)
        {
            WixTestContext context = this.TestContext;
            if (null != context)
            {
                context.TestResult = result;
            }

            this.TestUninitialize();
            this.CleanUp();
        }

        void IDisposable.Dispose()
        {
            this.ClassUninitialize();
            this.CleanUp();
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
        /// Gets the test registry key for the current test.
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

        private static void InitializeSettings()
        {
            // Best effort to locate MSBuild.
            IEnumerable<string> msbuildDirectories = new string[]
            {
                Environment.GetEnvironmentVariable(WixTestBase.envWixTestMSBuildDirectory),
                Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), @"Micorosft.NET\Framework\v4.0.30319"),
                Path.Combine(Environment.GetEnvironmentVariable("SystemRoot"), @"Microsoft.NET\Framework\v3.5"),
            };

            foreach (string msbuildDirectory in msbuildDirectories)
            {
                if (!String.IsNullOrEmpty(msbuildDirectory) && Directory.Exists(msbuildDirectory))
                {
                    Settings.MSBuildDirectory = msbuildDirectory;
                    break;
                }
            }

            // Set the directory for the build output.
            Settings.WixBuildDirectory = Environment.GetEnvironmentVariable(WixTestBase.envWixBuildPathDirectory) ?? Environment.CurrentDirectory;
            Settings.WixToolsDirectory = Environment.GetEnvironmentVariable(WixTestBase.envWixToolsPath) ?? Environment.CurrentDirectory;

            // Set the locations of wix.targets and wixtasks.dll using the build output as default.
            string path = Environment.GetEnvironmentVariable(WixTestBase.envWixTargetsPath);
            if (String.IsNullOrEmpty(path))
            {
                path = Path.Combine(Settings.WixToolsDirectory, "wix.targets");
            }

            if (File.Exists(path))
            {
                Settings.WixTargetsPath = path;
            }
            else
            {
                Console.WriteLine("The environment variable '{0}' was not set. The location for wix.targets will not be explicitly specified to MSBuild.", WixTestBase.envWixTargetsPath);
            }

            path = Environment.GetEnvironmentVariable(WixTestBase.envWixTasksPath);
            if (String.IsNullOrEmpty(path))
            {
                path = Path.Combine(Settings.WixToolsDirectory, "WixTasks.dll");
            }

            if (File.Exists(path))
            {
                Settings.WixTasksPath = path;
            }
            else
            {
                Console.WriteLine("The environment variable '{0}' was not set. The location for WixTasks.dll will not be explicitly specified to MSBuild.", WixTestBase.envWixTasksPath);
            }
        }

        private void InitializeContext(string testNamespace, string testClass, string testName)
        {
            // Clear the existing test context so its not used incorrectly.
            this.TestContext = null;

            // Set up the new test context for the current test.
            WixTestContext context = new WixTestContext()
            {
                Seed = WixTestBase.seed,
            };

            if (String.IsNullOrEmpty(testName))
            {
                StackTrace st = new StackTrace();
                StackFrame sf = st.GetFrame(1);
                sf.GetMethod();

                context.TestName = sf.GetMethod().Name;
            }
            else
            {
                context.TestName = testName;
            }

            context.TestDirectory = Path.Combine(Path.GetTempPath(), "wix_tests", WixTestBase.seed, context.TestName);
            Directory.CreateDirectory(context.TestDirectory);

            // Make sure we can resolve to our test data directory.
            string path = Environment.GetEnvironmentVariable("WIX_ROOT") ?? WixTestBase.projectDirectory;
            if (!String.IsNullOrEmpty(path))
            {
                path = Path.Combine(path, @"test\data\");
            }
            else
            {
                throw new InvalidOperationException("The WIX_ROOT environment variable is not defined. The current test case cannot continue.");
            }

            // Always store the root test data directory for those tests that need it.
            context.DataDirectory = path;

            // Special handling for the WixTest project's tests.
            if (testNamespace.StartsWith("WixTest.Tests."))
            {
                path = Path.Combine(path, testNamespace.Substring("WixTest.Tests.".Length).Replace('.', '\\'), testClass);
            }

            context.TestDataDirectory = path;

            this.TestArtifacts = new List<FileSystemInfo>();
            this.TestArtifacts.Add(new DirectoryInfo(context.TestDirectory));

            // Keep track of the current directory stack and change to the current test directory.
            this.currentDirectories.Push(Directory.GetCurrentDirectory());
            Directory.SetCurrentDirectory(context.TestDirectory);

            this.TestContext = context;
        }

        private void CleanUp()
        {
            PackageBuilder.CleanupByUninstalling();
            MSIExec.UninstallAllInstalledProducts();
            BundleBuilder.CleanupByUninstalling();

            MsiVerifier.Reset();

            this.ResetRegistry();
            this.ResetDirectory();

            if (this.cleanArtifacts)
            {
                foreach (FileSystemInfo artifact in this.TestArtifacts)
                {
                    try
                    {
                        DirectoryInfo dir = artifact as DirectoryInfo;
                        if (null != dir)
                        {
                            dir.Delete(true);
                        }
                        else
                        {
                            artifact.Delete();
                        }
                    }
                    catch
                    {
                        Debug.WriteLine(String.Format("Failed to delete '{0}'.", artifact.FullName));
                    }
                }
            }
        }

        private void ResetDirectory()
        {
            if (0 < this.currentDirectories.Count)
            {
                string path = this.currentDirectories.Pop();
                if (!String.IsNullOrEmpty(path))
                {
                    Directory.SetCurrentDirectory(path);
                }
            }
        }

        private void ResetRegistry()
        {
            if (null != this.TestContext)
            {
                string key = String.Format(@"Software\WiX\Tests\{0}", this.TestContext.TestName);
                Registry.LocalMachine.DeleteSubKeyTree(key, false);
            }

            Registry.LocalMachine.DeleteSubKeyTree(@"Software\WiX\Tests\TestBAControl", false);
        }
    }
}
