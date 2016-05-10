// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Permissions;

[assembly: AssemblyDescription("Abstract base libraries for archive packing and unpacking")]
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]

[assembly: SecurityPermission(SecurityAction.RequestMinimum, Unrestricted = true)]

// SECURITY: Review carefully!
// This assembly is designed so that partially trusted callers should be able to
// do compression and extraction in a file path where they have limited
// file I/O permission. Or they can even do in-memory compression and extraction
// with absolutely no file I/O permission.
[assembly: AllowPartiallyTrustedCallers]
