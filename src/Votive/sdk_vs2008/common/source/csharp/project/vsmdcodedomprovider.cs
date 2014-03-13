//-------------------------------------------------------------------------------------------------
// <copyright file="vsmdcodedomprovider.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------


using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.CodeDom.Compiler;
using Microsoft.VisualStudio.Designer.Interfaces;
using IOleServiceProvider = Microsoft.VisualStudio.OLE.Interop.IServiceProvider;
using IServiceProvider = System.IServiceProvider;

namespace Microsoft.VisualStudio.Package
{
	internal class VSMDCodeDomProvider : IVSMDCodeDomProvider
	{
		private CodeDomProvider provider;
		public VSMDCodeDomProvider(CodeDomProvider provider)
		{
			if (provider == null)
				throw new ArgumentNullException("provider");
			this.provider = provider;
		}

		#region IVSMDCodeDomProvider Members
		object IVSMDCodeDomProvider.CodeDomProvider
		{
			get { return provider; }
		}
		#endregion
	}
}
