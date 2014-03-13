//--------------------------------------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the GlobalSuppressions class.
// </summary>
//--------------------------------------------------------------------------------------------------

using System.Diagnostics.CodeAnalysis;

[assembly: SuppressMessage("Microsoft.Performance", "CA1812:AvoidUninstantiatedInternalClasses", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.VisualStudio.WixPackageSettings+MachineSettingEnum`1")]

[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Tools.WindowsInstallerXml.Build.Tasks", Justification = "Ignoring this warning...we want this namespace, but don't have enough classes to go in it right now to satisfy the rule.")]
[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls")]
[assembly: SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "votive")]
