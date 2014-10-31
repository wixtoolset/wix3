//-------------------------------------------------------------------------------------------------
// <copyright file="buildpropertypage.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Package;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace Microsoft.VisualStudio.Package
{
	/// <summary>
	/// Enumerated list of the properties shown on the build property page
	/// </summary>
	internal enum BuildPropertyPageTag
	{
		OutputPath
	}

	/// <summary>
	/// Defines the properties on the build property page and the logic the binds the properties to project data (load and save)
	/// </summary>
	[CLSCompliant(false), ComVisible(true), Guid("9B3DEA40-7F29-4a17-87A4-00EE08E8241E")]
	public class BuildPropertyPage : SettingsPage
	{
		#region fields
		private string outputPath;

		public BuildPropertyPage()
		{
			this.Name = SR.GetString(SR.BuildCaption, CultureInfo.CurrentUICulture);
		}
		#endregion

		#region properties
		[SRCategoryAttribute(SR.BuildCaption)]
		[LocDisplayName(SR.OutputPath)]
		[SRDescriptionAttribute(SR.OutputPathDescription)]
		public string OutputPath
		{
			get { return this.outputPath; }
			set { this.outputPath = value; this.IsDirty = true; }
		}
		#endregion

		#region overridden methods
		public override string GetClassName()
		{
			return this.GetType().FullName;
		}

		protected override void BindProperties()
		{
			if (this.ProjectMgr == null)
			{
				Debug.Assert(false);
				return;
			}

			this.outputPath = this.GetConfigProperty(BuildPropertyPageTag.OutputPath.ToString());
		}

		protected override int ApplyChanges()
		{
			if (this.ProjectMgr == null)
			{
				Debug.Assert(false);
				return VSConstants.E_INVALIDARG;
			}

			this.SetConfigProperty(BuildPropertyPageTag.OutputPath.ToString(), this.outputPath);
			this.IsDirty = false;
			return VSConstants.S_OK;
		}
		#endregion
	}
}
