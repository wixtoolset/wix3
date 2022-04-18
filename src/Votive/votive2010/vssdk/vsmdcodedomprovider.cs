// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
