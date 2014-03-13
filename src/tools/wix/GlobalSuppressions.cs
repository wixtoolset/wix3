//-------------------------------------------------------------------------------------------------
// <copyright file="GlobalSuppressions.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Global suppression of inappropriate code analysis tools.
// </summary>
//-------------------------------------------------------------------------------------------------

// MSI-defined types
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Tools.WindowsInstallerXml.Cab")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Msi.InstallMessage")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1008:EnumsShouldHaveZeroValue", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Msi.InstallUILevels")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1027:MarkEnumsWithFlags", Scope = "type", Target = "Microsoft.Tools.WindowsInstallerXml.Msi.OpenDatabase")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1032:ImplementStandardExceptionConstructors")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Design", "CA1036:OverrideMethodsOnComparableTypes")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]

// .NET 2.0 requirement
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Intermediate.Load(System.String,Microsoft.Tools.WindowsInstallerXml.TableDefinitionCollection,System.Boolean,System.Boolean):Microsoft.Tools.WindowsInstallerXml.Intermediate")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Library.Load(System.IO.Stream,System.Uri,Microsoft.Tools.WindowsInstallerXml.TableDefinitionCollection,System.Boolean,System.Boolean):Microsoft.Tools.WindowsInstallerXml.Library")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Localization.Load(System.IO.Stream,System.Uri,Microsoft.Tools.WindowsInstallerXml.TableDefinitionCollection,System.Boolean):Microsoft.Tools.WindowsInstallerXml.Localization")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:DisposeObjectsBeforeLosingScope", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Output.Load(System.IO.Stream,System.Uri,System.Boolean,System.Boolean):Microsoft.Tools.WindowsInstallerXml.Output")]

// .NET 2.0 requirement
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2006:UseSafeHandleToEncapsulateNativeResources", Scope = "member", Target = "Microsoft.Tools.WindowsInstallerXml.Cab.WixCreateCab.handle")]

[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Msi", Scope = "namespace", Target = "Microsoft.Tools.WindowsInstallerXml.Msi")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "wix")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1709:IdentifiersShouldBeCasedCorrectly", MessageId = "wix")]
[assembly: System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "wx")]
