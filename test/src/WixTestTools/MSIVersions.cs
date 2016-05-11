// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;

    /// <summary>
    /// MSI (Windows Installer) Versions
    /// </summary>
    public static class MSIVersions
    {
        /// <summary>
        /// An enumeration of MSI versions
        /// </summary>
        public enum Versions
        {
            MSI20,
            MSI30,
            MSI31,
            MSI45,
            MSI50
        }

        /// <summary>
        /// Returns a Version object representing the MSI version selected
        /// </summary>
        /// <param name="versions">The MSI version</param>
        /// <returns>A Version object</returns>
        public static Version GetVersion(Versions versions)
        {
            switch (versions)
            {
                case Versions.MSI20:
                    return new Version("2.0");
                case Versions.MSI30:
                    return new Version("3.0");
                case Versions.MSI31:
                    return new Version("3.1");
                case Versions.MSI45:
                    return new Version("4.5");
                case Versions.MSI50:
                    return new Version("5.0");
                default:
                    throw new ArgumentException(String.Format("{0} is an invalid value for this method", versions.ToString()));
            }
        }

        /// <summary>
        /// Returns a Version in the MSI/MSM Summary Information Version format
        /// </summary>
        /// <param name="version">The MSI version</param>
        /// <returns>A string representing the MSI/MSM Summary Information Version format</returns>
        /// <remarks>
        /// Example: Windows Installer version 4.5 is converted to '405'
        /// </remarks>
        public static string GetVersionInMSIFormat(Versions versions)
        {
            // Convert the version to the correct format
            Version version = MSIVersions.GetVersion(versions);
            int msiVersion = (version.Major * 100) + version.Minor;

            return Convert.ToString(msiVersion);
        }
    }
}
