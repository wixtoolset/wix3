//-------------------------------------------------------------------------------------------------
// <copyright file="AssemblyInfo.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Assembly information.
// </summary>
//-------------------------------------------------------------------------------------------------

using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;
using WixTest.BA;


[assembly: AssemblyTitle("TestBA")]
[assembly: AssemblyDescription("WiX Test Bootstrapper Application")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]
[assembly: Guid("47fe3d78-86ba-4a9a-8c24-872c840e8723")]
[assembly: CLSCompliantAttribute(true)]

// Identifies the class that derives from UserExperience and is the UX class that gets
// instantiated by the interop layer
[assembly: BootstrapperApplication(typeof(TestBA))]
