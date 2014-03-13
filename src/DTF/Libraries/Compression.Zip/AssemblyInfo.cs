//---------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;

[assembly: AssemblyDescription("Managed libraries for zip archive packing and unpacking")]
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

// SECURITY: The UnmanagedCode assertions in the zip classes are safe, because
// only some very limited date-time unmanaged APIs are called.
[assembly: SecurityPermission(SecurityAction.RequestMinimum, Assertion = true, UnmanagedCode = true)]

// SECURITY: Review carefully!
// This assembly is designed so that partially trusted callers should be able to
// do zip compression and extraction in a file path where they have limited
// file I/O permission. Or they can even do in-memory compression and extraction
// with absolutely no file I/O permission.
[assembly: AllowPartiallyTrustedCallers]

[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Deployment.Compression.Zip")]
