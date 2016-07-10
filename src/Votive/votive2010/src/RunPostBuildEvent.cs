// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;

    /// <summary>
    /// Enumerates the values of the RunPostBuildEvent MSBuild property.
    /// </summary>
    public enum RunPostBuildEvent
    {
        /// <summary>
        /// The post-build event is always run.
        /// </summary>
        Always,

        /// <summary>
        /// The post-build event is only run when the build succeeds.
        /// </summary>
        OnBuildSuccess,

        /// <summary>
        /// The post-build event is only run if the project's output is updated.
        /// </summary>
        OnOutputUpdated,
    }
}
