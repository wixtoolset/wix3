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
using System.Diagnostics.CodeAnalysis;

[assembly: AssemblyDescription("LINQ extensions for Windows Installer classes")]
[assembly: ComVisible(false)]
[assembly: CLSCompliant(true)]

[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Deployment.WindowsInstaller.Linq.QTable`1.System.Linq.IQueryable<TRecord>.CreateQuery(System.Linq.Expressions.Expression):System.Linq.IQueryable`1<TElement>")]
[assembly: SuppressMessage("Microsoft.Design", "CA1004:GenericMethodsShouldProvideTypeParameter", Scope = "member", Target = "Microsoft.Deployment.WindowsInstaller.Linq.QTable`1.System.Linq.IQueryable<TRecord>.Execute(System.Linq.Expressions.Expression):TResult")]
