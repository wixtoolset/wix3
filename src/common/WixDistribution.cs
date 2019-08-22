// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Reflection;
using System.Resources;

[assembly: AssemblyCompany(".NET Foundation")]
[assembly: AssemblyCopyright("Copyright (c) .NET Foundation and contributors. All rights reserved.")]
[assembly: AssemblyProduct("Windows Installer XML Toolset")]

#if DEBUG
    [assembly: AssemblyConfiguration("DEBUG")]
#else
    [assembly: AssemblyConfiguration("")]
#endif
[assembly: NeutralResourcesLanguage("en-US")]

namespace Microsoft.Tools.WindowsInstallerXml
{
    /// <summary>
    /// Distribution specific strings.
    /// </summary>
    internal static class WixDistribution
    {
        /// <summary>
        /// News URL for the distribution.
        /// </summary>
        public static string NewsUrl = "http://wixtoolset.org/news/";

        /// <summary>
        /// Short product name for the distribution.
        /// </summary>
        public static string ShortProduct = "WiX Toolset";

        /// <summary>
        /// Support URL for the distribution.
        /// </summary>
        public static string SupportUrl = "http://wixtoolset.org/";

        /// <summary>
        /// Telemetry URL format for the distribution.
        /// </summary>
        public static string TelemetryUrlFormat = "http://wixtoolset.org/telemetry/v{0}/?r={1}";

        /// <summary>
        /// VS Extensions Landing page Url for the distribution.
        /// </summary>
        public static string VSExtensionsLandingUrl = "http://wixtoolset.org/releases/";

        public static string ReplacePlaceholders(string original, Assembly assembly)
        {
            if (null != assembly)
            {
                FileVersionInfo fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location);

                original = original.Replace("[FileComments]", fileVersion.Comments);
                original = original.Replace("[FileCopyright]", fileVersion.LegalCopyright);
                original = original.Replace("[FileProductName]", fileVersion.ProductName);
                original = original.Replace("[FileVersion]", fileVersion.FileVersion);

                if (original.Contains("[FileVersionMajorMinor]"))
                {
                    Version version = new Version(fileVersion.FileVersion);
                    original = original.Replace("[FileVersionMajorMinor]", String.Concat(version.Major, ".", version.Minor));
                }

                AssemblyCompanyAttribute company;
                if (WixDistribution.TryGetAttribute(assembly, out company))
                {
                    original = original.Replace("[AssemblyCompany]", company.Company);
                }

                AssemblyCopyrightAttribute copyright;
                if (WixDistribution.TryGetAttribute(assembly, out copyright))
                {
                    original = original.Replace("[AssemblyCopyright]", copyright.Copyright);
                }

                AssemblyDescriptionAttribute description;
                if (WixDistribution.TryGetAttribute(assembly, out description))
                {
                    original = original.Replace("[AssemblyDescription]", description.Description);
                }

                AssemblyProductAttribute product;
                if (WixDistribution.TryGetAttribute(assembly, out product))
                {
                    original = original.Replace("[AssemblyProduct]", product.Product);
                }

                AssemblyTitleAttribute title;
                if (WixDistribution.TryGetAttribute(assembly, out title))
                {
                    original = original.Replace("[AssemblyTitle]", title.Title);
                }
            }

            original = original.Replace("[NewsUrl]", WixDistribution.NewsUrl);
            original = original.Replace("[ShortProduct]", WixDistribution.ShortProduct);
            original = original.Replace("[SupportUrl]", WixDistribution.SupportUrl);
            return original;
        }

        private static bool TryGetAttribute<T>(Assembly assembly, out T attribute) where T : Attribute
        {
            attribute = null;

            object[] customAttributes = assembly.GetCustomAttributes(typeof(T), false);
            if (null != customAttributes && 0 < customAttributes.Length)
            {
                attribute = customAttributes[0] as T;
            }

            return null != attribute;
        }
    }
}
