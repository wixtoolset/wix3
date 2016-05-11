// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Globalization;
using System.Runtime.InteropServices;

using Microsoft.VisualStudio.Package;
using VSLangProj;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.VisualStudio.Package.Automation
{
    [SuppressMessage("Microsoft.Interoperability", "CA1405:ComVisibleTypeBaseTypesShouldBeComVisible")]
    [CLSCompliant(false), ComVisible(true)]
    public class OAComReference : OAReferenceBase<ComReferenceNode>
    {
        public OAComReference(ComReferenceNode comReference) :
            base(comReference)
        {
        }

        #region Reference override
        public override string Culture
        {
            get
            {
                int locale = 0;
                try
                {
                    locale = int.Parse(BaseReferenceNode.LCID, CultureInfo.InvariantCulture);
                }
                catch (System.FormatException)
                {
                    // Do Nothing
                }
                if (0 == locale)
                {
                    return string.Empty;
                }
                CultureInfo culture = new CultureInfo(locale);
                return culture.Name;
            }
        }
        public override string Identity
        {
            get
            {
                return String.Format(CultureInfo.InvariantCulture, "{0}\\{1}", BaseReferenceNode.TypeGuid.ToString("B"), this.Version);
            }
        }
        public override int MajorVersion
        {
            get { return BaseReferenceNode.MajorVersionNumber; }
        }
        public override int MinorVersion
        {
            get { return BaseReferenceNode.MinorVersionNumber; }
        }
        public override string Name
        {
            get { return BaseReferenceNode.Caption; }
        }
        public override VSLangProj.prjReferenceType Type
        {
            get
            {
                return VSLangProj.prjReferenceType.prjReferenceTypeActiveX;
            }
        }
        public override string Version
        {
            get
            {
                Version version = new Version(BaseReferenceNode.MajorVersionNumber, BaseReferenceNode.MinorVersionNumber);
                return version.ToString();
            }
        }
        #endregion
    }
}
