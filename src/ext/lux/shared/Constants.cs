//-------------------------------------------------------------------------------------------------
// <copyright file="Constants.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Definitions shared across Lux projects.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Lux
{
    /// <summary>
    /// Constants shared across Lux projects.
    /// </summary>
    public class Constants
    {
        public const string LuxNamespace = "http://schemas.microsoft.com/wix/2009/Lux";
        public const string LuxCustomActionName = "WixRunImmediateUnitTests";
        public const string LuxMutationRunningProperty = "WIXLUX_RUNNING_MUTATION";
        public const string LuxTableName = "WixUnitTest";

        /// <summary>
        /// Error table ids.
        /// </summary>
        public const int TestIdMinimumSuccess = 27110;
        public const int TestPassedExpressionTrue = 27110;
        public const int TestPassedPropertyValueMatch = 27111;
        public const int TestSkipped = 27112;
        public const int TestIdMaximumSuccess = 27112;

        public const int TestIdMinimumFailure = 27120;
        public const int TestUnknownResult = 27120;
        public const int TestUnknownOperation = 27121;
        public const int TestNotCreated = 27122;
        public const int TestFailedExpressionFalse = 27123;
        public const int TestFailedExpressionSyntaxError = 27124;
        public const int TestFailedPropertyValueMismatch = 27125;
        public const int TestFailedIndexOutOfBounds = 27126;
        public const int TestFailedExpectedEvenNameValueContent = 27127;
        public const int TestFailedIndexUnknown = 27128;
        public const int TestIdMaximumFailure = 27128;
    }
}