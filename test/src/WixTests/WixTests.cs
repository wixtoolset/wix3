//-----------------------------------------------------------------------
// <copyright file="WixTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     - Contains methods that are shared across this assembly
//     - Performs some initialization before the tests are run
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Threading;
    using System.Reflection;
    using WixTest;
    using WixTest.Verifiers;
    using Microsoft.Win32;
    using Xunit;
    using Xunit.Sdk;

    /// <summary>
    /// Contains variables and methods used by this test assembly
    /// </summary>
    public abstract class WixTests : WixTestBase
    {
        private static readonly string envFlavor = "Flavor";

        private static string originalWixValue;
        private static int references = 0;

        /// <summary>
        /// The full path to BasicProduct.msi, which is a shared test file
        /// </summary>
        public static readonly string BasicProductMsi = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\SharedData\Baselines\BasicProduct.msi");

        /// <summary>
        /// The full path to BasicProduct.wxs, which is a shared test file
        /// </summary>
        public static readonly string BasicProductWxs = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\SharedData\Authoring\BasicProduct.wxs");

        /// <summary>
        /// The full path to PropertyFragment.wxs, which is a shared test file
        /// </summary>
        public static readonly string PropertyFragmentWxs = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\SharedData\Authoring\PropertyFragment.wxs");

        /// <summary>
        /// The location of the shared WiX authoring
        /// </summary>
        public static readonly string SharedAuthoringDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\SharedData\Authoring");

        /// <summary>
        /// The location of the baseline files
        /// </summary>
        public static readonly string SharedBaselinesDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\SharedData\Baselines");

        /// <summary>
        /// The location of the shared files
        /// </summary>
        public static readonly string SharedFilesDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\SharedData\Files");

        static WixTests()
        {
            WixTests.SetTestFalvor();
        }

        /// <summary>
        /// Initializes a WiX unit test class.
        /// </summary>
        public WixTests()
        {
            // Set the WIX environment variables.
            if (1 == Interlocked.Increment(ref WixTests.references))
            {
                WixTests.originalWixValue = Environment.GetEnvironmentVariable("WIX");

                string wixRoot = Environment.GetEnvironmentVariable("WIX_ROOT");
                if (String.IsNullOrEmpty(wixRoot))
                {
                    Environment.SetEnvironmentVariable("WIX", wixRoot);
                }
            }
        }

        ~WixTests()
        {
            if (0 == Interlocked.Decrement(ref WixTests.references))
            {
                Environment.SetEnvironmentVariable("WIX", WixTests.originalWixValue);
            }
        }

        private static void SetTestFalvor()
        {
            string flavor = Environment.GetEnvironmentVariable(WixTests.envFlavor);
            if (String.IsNullOrEmpty(flavor))
            {
                flavor = "debug";
                Console.WriteLine("The environment variable '{0}' was not set. Using the default build flavor '{1}' to run tests aginst.", WixTests.envFlavor, flavor);
            }

            Settings.Flavor = flavor;
        }
    }
}