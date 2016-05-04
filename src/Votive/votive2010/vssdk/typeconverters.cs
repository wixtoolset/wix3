// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Designer.Interfaces;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml;
using System.Diagnostics;

namespace Microsoft.VisualStudio.Package
{
    public class OutputTypeConverter : EnumConverter
    {
        public OutputTypeConverter() : base(typeof(OutputType))
        { 
        
        }
        
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType.IsEquivalentTo(typeof(string))) return true;

            return base.CanConvertFrom(context, sourceType);
        }
        
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            string str = value as string;

            if (str != null) {
                if (str == SR.GetString(SR.Exe, culture)) return OutputType.Exe;
                if (str == SR.GetString(SR.Library, culture)) return OutputType.Library;
                if (str == SR.GetString(SR.WinExe, culture)) return OutputType.WinExe;
            }

            return base.ConvertFrom(context, culture, value);
        }
        
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType.IsEquivalentTo(typeof(string)))
            {
                string result = null;
                // In some cases if multiple nodes are selected the windows form engine
                // calls us with a null value if the selected node's property values are not equal
                if (value != null)
                {
                    result = SR.GetString(((OutputType)value).ToString(), culture);
                }
                else
                {
                    result = SR.GetString(OutputType.Library.ToString(), culture);
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
        
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }
        
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            return new StandardValuesCollection(new OutputType[] { OutputType.Exe, OutputType.Library, OutputType.WinExe });
        }
    }

    public class DebugModeConverter : EnumConverter
    {

        public DebugModeConverter()
            : base(typeof(DebugMode))
        {
        
        }
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType.IsEquivalentTo(typeof(string))) return true;

            return base.CanConvertFrom(context, sourceType);
        }
        
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            string str = value as string;

            if (str != null) {
                if (str == SR.GetString(SR.Program, culture)) return DebugMode.Program;

                if (str == SR.GetString(SR.Project, culture)) return DebugMode.Project;

                if (str == SR.GetString(SR.URL, culture)) return DebugMode.URL;
            }

            return base.ConvertFrom(context, culture, value);
        }
        
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) 
        {
            if (destinationType.IsEquivalentTo(typeof(string)))
            {
                string result = null;
                // In some cases if multiple nodes are selected the windows form engine
                // calls us with a null value if the selected node's property values are not equal
                if (value != null)
                {
                    result = SR.GetString(((DebugMode)value).ToString(), culture);
                }
                else
                {
                    result = SR.GetString(DebugMode.Program.ToString(), culture);
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
        
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }
        
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            return new StandardValuesCollection(new DebugMode[] { DebugMode.Program, DebugMode.Project, DebugMode.URL });
        }
    }

    public class BuildActionConverter : TypeConverter
    {
        List<BuildAction> buildActions = new List<BuildAction>();

        public BuildActionConverter()
        {
            ResetBuildActionsToDefaults();
        }

        public void ResetBuildActionsToDefaults()
        {
            this.buildActions.Clear();
            this.buildActions.Add(BuildAction.None);
            this.buildActions.Add(BuildAction.Compile);
            this.buildActions.Add(BuildAction.Content);
            this.buildActions.Add(BuildAction.EmbeddedResource);
        }

        public void RegisterBuildAction(BuildAction buildAction)
        {
            if (!this.buildActions.Contains(buildAction))
            {
                this.buildActions.Add(buildAction);
            }
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType.IsEquivalentTo(typeof(string))) return true;
            return false;
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType.IsEquivalentTo(typeof(string));
        }


        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string s = value as string;
            if (s != null) return new BuildAction(s);
            return null;
        }


        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType.IsEquivalentTo(typeof(string)))
            {
                return ((BuildAction)value).Name;
            }

            return null;
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(buildActions);
        }
    }

    public class CopyToOutputDirectoryConverter : EnumConverter
    {

        public CopyToOutputDirectoryConverter()
            : base(typeof(CopyToOutputDirectory))
        {

        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType.IsEquivalentTo(typeof(string))) return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            string str = value as string;

            if (str != null)
            {
                if (str == SR.GetString(SR.CopyAlways, culture)) return CopyToOutputDirectory.Always;

                if (str == SR.GetString(SR.CopyIfNewer, culture)) return CopyToOutputDirectory.PreserveNewest;

                if (str == SR.GetString(SR.DoNotCopy, culture)) return CopyToOutputDirectory.DoNotCopy;
            }
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType.IsEquivalentTo(typeof(string)))
            {
                string result = null;

                // In some cases if multiple nodes are selected the windows form engine
                // calls us with a null value if the selected node's property values are not equal
                if (value != null)
                {
                    if (((CopyToOutputDirectory)value) == CopyToOutputDirectory.DoNotCopy)
                        result = SR.GetString(SR.DoNotCopy, culture);
                    if (((CopyToOutputDirectory)value) == CopyToOutputDirectory.Always)
                        result = SR.GetString(SR.CopyAlways, culture);
                    if (((CopyToOutputDirectory)value) == CopyToOutputDirectory.PreserveNewest)
                        result = SR.GetString(SR.CopyIfNewer, culture);
                }
                else
                {
                    result = "";
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool GetStandardValuesSupported(ITypeDescriptorContext context)
        {
            return true;
        }

        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context)
        {
            return new StandardValuesCollection(new CopyToOutputDirectory[] { CopyToOutputDirectory.Always, CopyToOutputDirectory.DoNotCopy, CopyToOutputDirectory.PreserveNewest });
        }
    }



    public class PlatformTypeConverter : EnumConverter
    {

        public PlatformTypeConverter()
            : base(typeof(PlatformType))
        {
        }
        
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType.IsEquivalentTo(typeof(string))) return true;

            return base.CanConvertFrom(context, sourceType);
        }
        
        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            string str = value as string;

            if (str != null) {
                if (str == SR.GetString(SR.v1, culture)) return PlatformType.v1;

                if (str == SR.GetString(SR.v11, culture)) return PlatformType.v11;

                if (str == SR.GetString(SR.v2, culture)) return PlatformType.v2;

                if (str == SR.GetString(SR.cli1, culture)) return PlatformType.cli1;
            }

            return base.ConvertFrom(context, culture, value);
        }
        
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            if (destinationType.IsEquivalentTo(typeof(string)))
            {
                string result = null;
                // In some cases if multiple nodes are selected the windows form engine
                // calls us with a null value if the selected node's property values are not equal
                if (value != null)
                {
                    result = SR.GetString(((PlatformType)value).ToString(), culture);
                }
                else
                {
                    result = SR.GetString(PlatformType.notSpecified.ToString(), culture);
                }

                if (result != null) return result;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
        
        public override bool GetStandardValuesSupported(ITypeDescriptorContext context) {
            return true;
        }
        
        public override TypeConverter.StandardValuesCollection GetStandardValues(ITypeDescriptorContext context) {
            return new StandardValuesCollection(new PlatformType[] { PlatformType.v1, PlatformType.v11, PlatformType.v2, PlatformType.cli1 });
        }
    }
}
