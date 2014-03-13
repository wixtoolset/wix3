//-------------------------------------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

// This file is used by Code Analysis to maintain SuppressMessage 
// attributes that are applied to this project.
// Project-level suppressions either have no target or are given 
// a specific target and scoped to a namespace, type, member, etc.
//
// To add a suppression to this file, right-click the message in the 
// Error List, point to "Suppress Message(s)", and click 
// "In Project Suppression File".
// You do not need to add suppressions to this file manually.

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA2210:AssembliesShouldHaveValidStrongNames")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "hwnd", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.Engine.#Apply(System.IntPtr)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "hwnd", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.Engine.#Elevate(System.IntPtr)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1034:NestedTypesShouldNotBeVisible", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.Engine+Variables`1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectMsiFeatureEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectRelatedMsiPackageEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectTargetMsiPackageEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ResultEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ResultStatusEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.StatusEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1012:AbstractTypesShouldNotHaveConstructors", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperException")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Recache", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ActionState.#Recache")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1054:UriParametersShouldNotBeStrings", MessageId = "2#", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.Engine.#SetDownloadSource(System.String,System.String,System.String,System.String,System.String)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Usage", "CA2217:DoNotMarkEnumsWithFlags", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.EndSessionReasons")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.EndSessionReasons")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "Logoff", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.EndSessionReasons.#Logoff")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Addon", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.RelationType.#Addon")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Globalization", "CA1303:Do not pass literals as localized parameters", MessageId = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.Engine.Log(Microsoft.Tools.WindowsInstallerXml.Bootstrapper.LogLevel,System.String)", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#OnStartup(Microsoft.Tools.WindowsInstallerXml.Bootstrapper.StartupEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Sha", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.UpdateHashType.#Sha1")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#OnDetectMsiFeature(Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectMsiFeatureEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#OnDetectRelatedMsiPackage(Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectRelatedMsiPackageEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#OnDetectTargetMsiPackage(Microsoft.Tools.WindowsInstallerXml.Bootstrapper.DetectTargetMsiPackageEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#OnExecuteMsiMessage(Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ExecuteMsiMessageEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#OnPlanMsiFeature(Microsoft.Tools.WindowsInstallerXml.Bootstrapper.PlanMsiFeatureEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#OnPlanTargetMsiPackage(Microsoft.Tools.WindowsInstallerXml.Bootstrapper.PlanTargetMsiPackageEventArgs)")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#DetectMsiFeature")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#DetectRelatedMsiPackage")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#DetectTargetMsiPackage")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#ExecuteMsiMessage")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#PlanMsiFeature")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.ExecuteMsiMessageEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.PlanMsiFeatureEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.PlanTargetMsiPackageEventArgs")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Bootstrapper.BootstrapperApplication.#PlanTargetMsiPackage")]
