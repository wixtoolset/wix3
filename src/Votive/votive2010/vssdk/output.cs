// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using MSBuildExecution = Microsoft.Build.Execution;

namespace Microsoft.VisualStudio.Package
{
	class Output : IVsOutput2
	{
		private MSBuildExecution.ProjectItemInstance output;
		private ProjectNode project;

		/// <summary>
		/// Constructor for IVSOutput2 implementation
		/// </summary>
		/// <param name="projectManager">Project that produce this output</param>
		/// <param name="configuration">Configuration that produce this output</param>
		/// <param name="outputAssembly">MSBuild generated item corresponding to the output assembly (by default, these would be of type MainAssembly</param>
		public Output(ProjectNode projectManager, MSBuildExecution.ProjectItemInstance outputAssembly)
		{
			if (projectManager == null)
				throw new ArgumentNullException("projectManager");
			if (outputAssembly == null)
				throw new ArgumentNullException("outputAssembly");

			project = projectManager;
			output = outputAssembly;
		}

		#region IVsOutput2 Members

		public int get_CanonicalName(out string pbstrCanonicalName)
		{
			// Get the output assembly path (including the name)
			pbstrCanonicalName = output.GetMetadataValue(ProjectFileConstants.FinalOutputPath);
			if (String.IsNullOrEmpty(pbstrCanonicalName))
			{
				pbstrCanonicalName = output.EvaluatedInclude;
			}
			Debug.Assert(!String.IsNullOrEmpty(pbstrCanonicalName), "Output Assembly not defined");

			// Make sure we have a full path
			if (!System.IO.Path.IsPathRooted(pbstrCanonicalName))
			{
				pbstrCanonicalName = new Url(project.BaseURI, pbstrCanonicalName).AbsoluteUrl;
			}
			return VSConstants.S_OK;
		}

		/// <summary>
		/// This path must start with file:/// if it wants other project
		/// to be able to reference the output on disk.
		/// If the output is not on disk, then this requirement does not
		/// apply as other projects probably don't know how to access it.
		/// </summary>
		public virtual int get_DeploySourceURL(out string pbstrDeploySourceURL)
		{
			string path = output.GetMetadataValue(ProjectFileConstants.FinalOutputPath);
			if (String.IsNullOrEmpty(path))
			{
				throw new InvalidOperationException();
			}
			if (path.Length < 9 || String.Compare(path.Substring(0, 8), "file:///", StringComparison.OrdinalIgnoreCase) != 0)
				path = "file:///" + path;
			pbstrDeploySourceURL = path;
			return VSConstants.S_OK;
		}

		public int get_DisplayName(out string pbstrDisplayName)
		{
			return this.get_CanonicalName(out pbstrDisplayName);
		}

		public virtual int get_Property(string szProperty, out object pvar)
		{
			pvar = null;
			String value = output.GetMetadataValue(szProperty);
			// If we don't have a value, we are expected to return unimplemented
			if (String.IsNullOrEmpty(value))
				throw new NotImplementedException();
			pvar = value;
			return VSConstants.S_OK;

		}

		public int get_RootRelativeURL(out string pbstrRelativePath)
		{
			pbstrRelativePath = String.Empty;
			object variant;
			// get the corresponding property
			if (ErrorHandler.Succeeded(this.get_Property("TargetPath", out variant))
				&& variant != null && variant is string)
			{
				pbstrRelativePath = (string)variant;
			}
			return VSConstants.S_OK;
		}

		public virtual int get_Type(out Guid pguidType)
		{
			pguidType = Guid.Empty;
			throw new NotImplementedException();
		}

		#endregion
}
}
