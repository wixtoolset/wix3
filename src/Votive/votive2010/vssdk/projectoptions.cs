// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Specialized;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Package
{

	public class ProjectOptions : System.CodeDom.Compiler.CompilerParameters
	{

		public ModuleKindFlags ModuleKind = ModuleKindFlags.ConsoleApplication;

		public bool EmitManifest = true;

		[SuppressMessage("Microsoft.Naming", "CA1702:CompoundWordsShouldBeCasedCorrectly", MessageId = "PreProcessor")]
		public StringCollection DefinedPreProcessorSymbols;

		public string XMLDocFileName;

		public string RecursiveWildcard;

		public StringCollection ReferencedModules;

		public string Win32Icon;

		public bool PDBOnly;

		public bool Optimize;

		public bool IncrementalCompile;

		public int[] SuppressedWarnings;

		public bool CheckedArithmetic;

		public bool AllowUnsafeCode;

		public bool DisplayCommandLineHelp;

		public bool SuppressLogo;

		public long BaseAddress;

		public string BugReportFileName;

		/// <devdoc>must be an int if not null</devdoc>
		public object CodePage;

		public bool EncodeOutputInUTF8;

		[SuppressMessage("Microsoft.Naming", "CA1704:IdentifiersShouldBeSpelledCorrectly", MessageId = "Qualifiy")]
		public bool FullyQualifiyPaths;

		public int FileAlignment;

		public bool NoStandardLibrary;

		public StringCollection AdditionalSearchPaths;

		public bool HeuristicReferenceResolution;

		public string RootNamespace;

		public bool CompileAndExecute;

		/// <devdoc>must be an int if not null.</devdoc>
		public object UserLocaleId;

		public PlatformType TargetPlatform;

		public string TargetPlatformLocation;

		public virtual string GetOptionHelp()
		{
			return null;
		}
	}
}
