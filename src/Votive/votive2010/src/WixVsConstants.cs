// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;

    /// <summary>
    /// Contains constants for VS integration related APIs
    /// </summary>
    //// Keep these in alphabetical order for easy referencing.
    internal static class WixVsConstants
    {
        // Command id and command set guid for Generate Code Metrics menu command. we are using these
        // private variables because we could not find such ids in the API. We should remove these variables
        // and use the API whenever available.
        public const uint CommandExploreFolderInWindows = 1635;
        public const uint CommandGenerateCodeMetricsAnalyzeMenu = 1027;
        public const uint CommandGenerateCodeMetricsContextMenu = 768;
        public const uint CommandRefreshToolbox = 4137;

        public static readonly Guid GuidGenerateCodeMetrics = new Guid("{79989dd6-4c13-4d10-9872-73538668d037}");
        public static readonly Guid GuidRefreshToolbox = new Guid("C90DA239-5787-47F4-8477-14580555AD76");
    }
}
