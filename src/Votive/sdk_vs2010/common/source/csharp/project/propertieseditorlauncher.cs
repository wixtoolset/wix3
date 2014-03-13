//-------------------------------------------------------------------------------------------------
// <copyright file="propertieseditorlauncher.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Designer.Interfaces;
using System;
using System.Collections;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Diagnostics;

using ErrorHandler = Microsoft.VisualStudio.ErrorHandler;

namespace Microsoft.VisualStudio.Package
{
    /// <summary>
    /// This class is used to enable launching the project properties
    /// editor from the Properties Browser.
    /// </summary>
    [CLSCompliant(false)]
    public class PropertiesEditorLauncher : ComponentEditor
    {
        private ServiceProvider serviceProvider;

        #region ctor
        public PropertiesEditorLauncher(ServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException("serviceProvider");

            this.serviceProvider = serviceProvider;
        }
        #endregion
        #region overridden methods
        /// <summary>
        /// Launch the Project Properties Editor (properties pages)
        /// </summary>
        /// <returns>If we succeeded or not</returns>
        public override bool EditComponent(ITypeDescriptorContext context, object component)
        {
            if (component is ProjectNodeProperties)
            {
                IVsPropertyPageFrame propertyPageFrame = (IVsPropertyPageFrame)serviceProvider.GetService((typeof(SVsPropertyPageFrame)));

                int hr = propertyPageFrame.ShowFrame(Guid.Empty);
                if (ErrorHandler.Succeeded(hr))
                    return true;
                else
                    propertyPageFrame.ReportError(hr);
            }

            return false;
        }
        #endregion

    }
}
