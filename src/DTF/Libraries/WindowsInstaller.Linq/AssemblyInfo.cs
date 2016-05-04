// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;

[assembly: AssemblyDescription("LINQ extensions for Windows Installer classes")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Deployment.WindowsInstaller.Linq.QTable`1.System.Linq.IQueryable<TRecord>.CreateQuery(System.Linq.Expressions.Expression):System.Linq.IQueryable`1<TElement>")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Deployment.WindowsInstaller.Linq.QTable`1.System.Linq.IQueryable<TRecord>.Execute(System.Linq.Expressions.Expression):TResult")]
