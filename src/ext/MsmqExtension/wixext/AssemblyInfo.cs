// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

using Microsoft.Tools.WindowsInstallerXml;
using Microsoft.Tools.WindowsInstallerXml.Extensions;

[assembly: AssemblyTitle("WiX Toolset MSMQ Extension")]
[assembly: AssemblyDescription("Windows Installer XML Toolset MSMQ Extension")]
[assembly: AssemblyCulture("")]
[assembly: CLSCompliant(true)]
[assembly: ComVisible(false)]
//[assembly: AssemblyDefaultHeatExtension(typeof(MsmqHeatExtension))]
[assembly: AssemblyDefaultWixExtension(typeof(MsmqExtension))]
