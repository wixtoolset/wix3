//--------------------------------------------------------------------------------------------------
// <copyright file="RunPostBuildEvent.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the RunPostBuildEvent class.
// </summary>
//--------------------------------------------------------------------------------------------------

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
