// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;

    /// <summary>
    /// Contains all the information registered by a bundle.
    /// </summary>
    public class BundleRegistration
    {
        public string[] AddonCodes { get; set; }

        public string CachePath { get; set; }

        public string DisplayName { get; set; }

        public string[] DetectCodes { get; set; }

        public string EngineVersion { get; set; }

        public int? EstimatedSize { get; set; }

        public int? Installed { get; set; }

        public string ModifyPath { get; set; }

        public string[] PatchCodes { get; set; }

        public string ProviderKey { get; set; }

        public string Publisher { get; set; }

        public string QuietUninstallString { get; set; }

        public string QuietUninstallCommand { get; set; }

        public string QuietUninstallCommandArguments { get; set; }

        public string Tag { get; set; }

        public string UninstallCommand { get; set; }

        public string UninstallCommandArguments { get; set; }

        public string UninstallString { get; set; }

        public string[] UpgradeCodes { get; set; }

        public string UrlInfoAbout { get; set; }

        public string UrlUpdateInfo { get; set; }

        public string Version { get; set; }
    }
}
