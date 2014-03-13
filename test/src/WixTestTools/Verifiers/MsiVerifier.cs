//-------------------------------------------------------------------------------------------------
// <copyright file="MsiVerifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//      Contains methods for verification of MSIs.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixTest.Verifiers
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using WixTest.Utilities;

    /// <summary>
    /// The MsiVerifier contains methods for verification of MSIs.
    /// </summary>
    public class MsiVerifier
    {
        private static Dictionary<string, string> PathsToProductCodes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Gets whether the product defined by the package <paramref name="path"/> is installed.
        /// </summary>
        /// <param name="path">The path to the package to test.</param>
        /// <returns>True if the package is installed; otherwise, false.</returns>
        public static bool IsPackageInstalled(string path)
        {
            string productCode;
            if (!PathsToProductCodes.TryGetValue(path, out productCode))
            {
                productCode = MsiUtils.GetMSIProductCode(path);
                PathsToProductCodes.Add(path, productCode);
            }

            return MsiUtils.IsProductInstalled(productCode);
        }

        /// <summary>
        /// Gets whether the product defined by the package <paramref name="productCode"/> is installed.
        /// </summary>
        /// <param name="path">The product code of the package to test.</param>
        /// <returns>True if the package is installed; otherwise, false.</returns>
        public static bool IsProductInstalled(string productCode)
        {
            return MsiUtils.IsProductInstalled(productCode);
        }

        /// <summary>
        /// Resets the verifier to remove any cached results from previous tests.
        /// </summary>
        public static void Reset()
        {
            PathsToProductCodes.Clear();
        }
    }
}
