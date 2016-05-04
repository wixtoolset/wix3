// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Resources;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;
using System.Diagnostics.CodeAnalysis;

[assembly: AssemblyDescription("Managed libraries for cabinet archive packing and unpacking")]
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

// SECURITY: The UnmanagedCode assertions in the cabinet classes are safe, because the
// assertions are not propogated through calls to the provided callbacks.  So there
// is no way that a partially-trusted malicious client could trick a trusted cabinet
// class into executing its own unmanaged code.
[assembly: SecurityPermission(SecurityAction.RequestMinimum, Assertion=true, UnmanagedCode=true)]

// SECURITY: Review carefully!
// This assembly is designed so that partially trusted callers should be able to
// do cabinet compression and extraction in a file path where they have limited
// file I/O permission. Or they can even do in-memory compression and extraction
// with absolutely no file I/O permission.
[assembly: AllowPartiallyTrustedCallers]

[assembly: SuppressMessage("Microsoft.Design", "CA1020:AvoidNamespacesWithFewTypes", Scope = "namespace", Target = "Microsoft.Deployment.Compression.Cab")]
