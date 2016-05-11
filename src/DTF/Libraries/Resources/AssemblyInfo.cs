// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Security;
using System.Security.Permissions;

[assembly: AssemblyDescription("Classes for reading and writing resource data in executable files")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

// SECURITY: The UnmanagedCode assertions in the resource classes are safe, because
// appropriate demands are made for file I/O permissions before reading/writing files.
[assembly: SecurityPermission(SecurityAction.RequestMinimum, Assertion = true, UnmanagedCode = true)]

// SECURITY: Review carefully!
// This assembly is designed so that partially trusted callers should be able to
// read and write file version info in a path where they have limited
// file I/O permission.
[assembly: AllowPartiallyTrustedCallers]


[assembly: SuppressMessage("Microsoft.Design", "CA1033:InterfaceMethodsShouldBeCallableByChildTypes", Scope = "member", Target = "Microsoft.Deployment.Resources.ResourceCollection.#System.Collections.Generic.ICollection`1<Microsoft.Deployment.Resources.Resource>.IsReadOnly")]
