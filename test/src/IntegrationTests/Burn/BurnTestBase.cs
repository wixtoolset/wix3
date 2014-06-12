//-----------------------------------------------------------------------
// <copyright file="BurnTestBase.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-----------------------------------------------------------------------

namespace WixTest.BurnIntegrationTests
{
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
    using Microsoft.Win32;
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;

    /// <summary>
    /// Base classs for all Burn tests.
    /// </summary>
    public class BurnTestBase : WixTestBase
    {
        public static string PayloadCacheFolder = "Package Cache";
        public static string PerMachinePayloadCacheRoot = System.Environment.ExpandEnvironmentVariables(@"%ProgramData%\" + PayloadCacheFolder);
        public static string PerUserPayloadCacheRoot = System.Environment.ExpandEnvironmentVariables(@"%LOCALAPPDATA%\" + PayloadCacheFolder);

        public static string TestValueVerifyArguments = "VerifyArguments";

        /// <summary>
        /// Creates an instance of a <see cref="PackageBuilder"/>.
        /// </summary>
        /// <param name="name">The name of the package to create.</param>
        /// <param name="bindPaths">Additional bind paths for building the package.</param>
        /// <param name="preprocessorVariables">Preprocessor variables for building the package.</param>
        /// <param name="extensions">Extensions for building the package.</param>
        /// <returns>A new <see cref="PackageBuilder"/> initialized with the given data.</returns>
        protected PackageBuilder CreatePackage(string name, Dictionary<string, string> bindPaths = null, Dictionary<string, string> preprocessorVariables = null, string[] extensions = null)
        {
            string testDataDirectory = Path.Combine(this.TestContext.TestDataDirectory, @"Integration\BurnIntegrationTests\BasicTests");
            PackageBuilder builder = new PackageBuilder(this.TestContext.TestName, name, testDataDirectory, this.TestArtifacts);

            if (null != bindPaths)
            {
                builder.BindPaths = bindPaths;
            }

            if (null != preprocessorVariables)
            {
                builder.PreprocessorVariables = preprocessorVariables;
            }

            builder.Extensions = extensions ?? WixTestBase.Extensions;

            return builder.Build();
        }

        /// <summary>
        /// Creates an instance of a <see cref="BundleBuilder"/>.
        /// </summary>
        /// <param name="name">The name of the bundle to create.</param>
        /// <param name="bindPaths">Additional bind paths for building the bundle.</param>
        /// <param name="preprocessorVariables">Preprocessor variables for building the bundle.</param>
        /// <param name="extensions">Extensions for building the bundle.</param>
        /// <returns>A new <see cref="BundleBuilder"/> initialized with the given data.</returns>
        protected BundleBuilder CreateBundle(string name, Dictionary<string, string> bindPaths = null, Dictionary<string, string> preprocessorVariables = null, string[] extensions = null)
        {
            string testDataDirectory = Path.Combine(this.TestContext.TestDataDirectory, @"Integration\BurnIntegrationTests\BasicTests");
            BundleBuilder builder = new BundleBuilder(this.TestContext.TestName, name, testDataDirectory, this.TestArtifacts);

            if (null != bindPaths)
            {
                builder.BindPaths = bindPaths;
            }

            if (null != preprocessorVariables)
            {
                builder.PreprocessorVariables = preprocessorVariables;
            }

            builder.Extensions = extensions ?? WixTestBase.Extensions;

            return builder.Build();
        }

        /// <summary>
        /// Tries to load the bundle registration using the upgrade code.
        /// </summary>
        /// <param name="bundleUpgradeCode">Upgrade code of the bundle's registration to find.</param>
        /// <param name="registration">Registration for the bundle if found.</param>
        /// <returns>True if bundle is found, otherwise false.</returns>
        protected bool TryGetBundleRegistration(string bundleUpgradeCode, out BundleRegistration registration)
        {
            registration = null;

            if (!bundleUpgradeCode.StartsWith("{"))
            {
                bundleUpgradeCode = String.Concat("{", bundleUpgradeCode);
            }

            if (!bundleUpgradeCode.EndsWith("}"))
            {
                bundleUpgradeCode = String.Concat(bundleUpgradeCode, "}");
            }

            foreach (string uninstallSubKeyPath in new string[] {
                    "SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Uninstall",
                    "SOFTWARE\\Wow6432Node\\Microsoft\\Windows\\CurrentVersion\\Uninstall"
                })
            {
                using (RegistryKey uninstallSubKey = Registry.LocalMachine.OpenSubKey(uninstallSubKeyPath))
                {
                    if (null == uninstallSubKey)
                    {
                        continue;
                    }

                    foreach (string bundleId in uninstallSubKey.GetSubKeyNames())
                    {
                        using (RegistryKey idKey = uninstallSubKey.OpenSubKey(bundleId))
                        {
                            if (null == idKey)
                            {
                                continue;
                            }

                            string[] upgradeCodes = idKey.GetValue("BundleUpgradeCode") as string[];
                            if (null != upgradeCodes && upgradeCodes.Contains(bundleUpgradeCode, StringComparer.InvariantCultureIgnoreCase))
                            {
                                registration = new BundleRegistration();

                                registration.AddonCodes = idKey.GetValue("BundleAddonCode") as string[];
                                registration.CachePath = idKey.GetValue("BundleCachePath") as string;
                                registration.DetectCodes = idKey.GetValue("BundleDetectCode") as string[];
                                registration.PatchCodes = idKey.GetValue("BundlePatchCode") as string[];
                                registration.ProviderKey = idKey.GetValue("BundleProviderKey") as string;
                                registration.Tag = idKey.GetValue("BundleTag") as string;
                                registration.UpgradeCodes = idKey.GetValue("BundleUpgradeCode") as string[];
                                registration.Version = idKey.GetValue("BundleVersion") as string;
                                registration.DisplayName = idKey.GetValue("DisplayName") as string;
                                registration.EngineVersion = idKey.GetValue("EngineVersion") as string;
                                registration.EstimatedSize = idKey.GetValue("EstimatedSize") as int?;
                                registration.Installed = idKey.GetValue("Installed") as int?;
                                registration.ModifyPath = idKey.GetValue("ModifyPath") as string;
                                registration.Publisher = idKey.GetValue("Publisher") as string;
                                registration.UrlInfoAbout = idKey.GetValue("URLInfoAbout") as string;
                                registration.UrlUpdateInfo = idKey.GetValue("URLUpdateInfo") as string;

                                registration.QuietUninstallString = idKey.GetValue("QuietUninstallString") as string;
                                if (!String.IsNullOrEmpty(registration.QuietUninstallString))
                                {
                                    int closeQuote = registration.QuietUninstallString.IndexOf("\"", 1);
                                    registration.QuietUninstallCommand = registration.QuietUninstallString.Substring(1, closeQuote - 1).Trim();
                                    registration.QuietUninstallCommandArguments = registration.QuietUninstallString.Substring(closeQuote + 1).Trim();
                                }

                                registration.UninstallString = idKey.GetValue("UninstallString") as string;
                                if (!String.IsNullOrEmpty(registration.UninstallString))
                                {
                                    int closeQuote = registration.UninstallString.IndexOf("\"", 1);
                                    registration.UninstallCommand = registration.UninstallString.Substring(1, closeQuote - 1).Trim();
                                    registration.UninstallCommandArguments = registration.UninstallString.Substring(closeQuote + 1).Trim();
                                }

                                break;
                            }
                        }
                    }
                }

                if (null != registration)
                {
                    break;
                }
            }

            return null != registration;
        }

        protected bool TryGetDependencyProviderValue(string providerId, string name, out string value)
        {
            value = null;

            string key = String.Format(@"Installer\Dependencies\{0}", providerId);
            using (RegistryKey providerKey = Registry.ClassesRoot.OpenSubKey(key))
            {
                if (null == providerKey)
                {
                    return false;
                }

                value = providerKey.GetValue(name) as string;
                return value != null;
            }
        }

        protected bool DependencyDependentExists(string providerId, string dependentId)
        {
            string key = String.Format(@"Installer\Dependencies\{0}\Dependents\{1}", providerId, dependentId);
            using (RegistryKey dependentKey = Registry.ClassesRoot.OpenSubKey(key))
            {
                return null != dependentKey;
            }
        }

        /// <summary>
        /// Sets a test value in the registry to communicate with the TestBA.
        /// </summary>
        /// <param name="name">Name of the value to set.</param>
        /// <param name="value">Value to set. If this is null, the value is removed.</param>
        protected void SetBurnTestValue(string name, string value)
        {
            string key = String.Format(@"Software\WiX\Tests\TestBAControl\{0}", this.TestContext.TestName);
            using (RegistryKey testKey = Registry.LocalMachine.CreateSubKey(key))
            {
                if (String.IsNullOrEmpty(value))
                {
                    testKey.DeleteValue(name, false);
                }
                else
                {
                    testKey.SetValue(name, value);
                }
            }
        }

        /// <summary>
        /// Slows the cache progress of a package.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="delay">Sets or removes the delay on a package being cached.</param>
        protected void SetPackageSlowCache(string packageId, int? delay)
        {
            this.SetPackageState(packageId, "SlowCache", delay.HasValue ? delay.ToString() : null);
        }

        /// <summary>
        /// Cancels the cache of a package at a particular progress point.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="cancelPoint">Sets or removes the cancel progress on a package being cached.</param>
        protected void SetPackageCancelCacheAtProgress(string packageId, int? cancelPoint)
        {
            this.SetPackageState(packageId, "CancelCacheAtProgress", cancelPoint.HasValue ? cancelPoint.ToString() : null);
        }

        /// <summary>
        /// Slows the execute progress of a package.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="delay">Sets or removes the delay on a package being executed.</param>
        protected void SetPackageSlowExecute(string packageId, int? delay)
        {
            this.SetPackageState(packageId, "SlowExecute", delay.HasValue ? delay.ToString() : null);
        }

        /// <summary>
        /// Cancels the execute of a package at a particular progress point.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="cancelPoint">Sets or removes the cancel progress on a package being executed.</param>
        protected void SetPackageCancelExecuteAtProgress(string packageId, int? cancelPoint)
        {
            this.SetPackageState(packageId, "CancelExecuteAtProgress", cancelPoint.HasValue ? cancelPoint.ToString() : null);
        }

        /// <summary>
        /// Retries the files in use one or more times before canceling.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="cancelPoint">Sets or removes the retry count on a package's file in use message.</param>
        protected void SetPackageRetryExecuteFilesInUse(string packageId, int? retryCount)
        {
            this.SetPackageState(packageId, "RetryExecuteFilesInUse", retryCount.HasValue ? retryCount.ToString() : null);
        }

        /// <summary>
        /// Sets the requested state for a package that the TestBA will return to the engine during plan.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="state">State to request.</param>
        protected void SetPackageRequestedState(string packageId, RequestState state)
        {
            this.SetPackageState(packageId, "Requested", state.ToString());
        }

        /// <summary>
        /// Sets the requested state for a package that the TestBA will return to the engine during plan.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        /// <param name="state">State to request.</param>
        protected void SetPackageFeatureState(string packageId, string featureId, FeatureState state)
        {
            this.SetPackageState(packageId, String.Concat(featureId, "Requested"), state.ToString());
        }

        /// <summary>
        /// Sets the number of times to re-run the Detect phase.
        /// </summary>
        /// <param name="state">Number of times to run Detect (after the first, normal, Detect).</param>
        protected void SetRedetectCount(int redetectCount)
        {
            this.SetPackageState(null, "RedetectCount", redetectCount.ToString());
        }

        /// <summary>
        /// Resets the state for a package that the TestBA will return to the engine during plan.
        /// </summary>
        /// <param name="packageId">Package identity.</param>
        protected void ResetPackageStates(string packageId)
        {
            string key = String.Format(@"Software\WiX\Tests\TestBAControl\{0}\{1}", this.TestContext.TestName, packageId ?? String.Empty);
            Registry.LocalMachine.DeleteSubKey(key);
        }

        private void SetPackageState(string packageId, string name, string value)
        {
            string key = String.Format(@"Software\WiX\Tests\TestBAControl\{0}\{1}", this.TestContext.TestName, packageId ?? String.Empty);
            using (RegistryKey packageKey = Registry.LocalMachine.CreateSubKey(key))
            {
                if (String.IsNullOrEmpty(value))
                {
                    packageKey.DeleteValue(name, false);
                }
                else
                {
                    packageKey.SetValue(name, value);
                }
            }
        }
    }
}
