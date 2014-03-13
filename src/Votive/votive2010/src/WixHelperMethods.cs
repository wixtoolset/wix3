//-------------------------------------------------------------------------------------------------
// <copyright file="WixHelperMethods.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Contains the WixHelperMethods class.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.VisualStudio
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Drawing.Imaging;
    using System.IO;
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Text;
    using System.Threading;
    using System.Windows.Forms;
    using System.Xml;
    using System.Xml.Schema;
    using Microsoft.Build.BuildEngine;
    using Microsoft.Tools.WindowsInstallerXml.VisualStudio.Controls;
    using Microsoft.VisualStudio;
    using Microsoft.VisualStudio.Package;
    using Microsoft.VisualStudio.Shell;
    using Microsoft.VisualStudio.Shell.Interop;

    using OleConstants = Microsoft.VisualStudio.OLE.Interop.Constants;
    using VsCommands = Microsoft.VisualStudio.VSConstants.VSStd97CmdID;
    using VsCommands2K = Microsoft.VisualStudio.VSConstants.VSStd2KCmdID;
    using VsMenus = Microsoft.VisualStudio.Package.VsMenus;

    /// <summary>
    /// Contains useful helper methods.
    /// </summary>
    internal static class WixHelperMethods
    {
        /// <summary>
        /// This is the node filter delegate.
        /// </summary>
        /// <param name="node">Node to be tested.</param>
        /// <param name="criteria">Filter criteria.</param>
        /// <returns>Returns if the node should be filtered or not.</returns>
        public delegate bool WixNodeFilter(HierarchyNode node, object criteria);

        /// <summary>
        /// VS colors for Visual Studio 2010
        /// We have to redefine them here until we start using the VS 2010 PIAs
        /// </summary>
        public enum Vs2010Color
        {
            /// <summary>
            /// VSCOLOR_BUTTONFACE
            /// </summary>
            VSCOLOR_BUTTONFACE = -196,

            /// <summary>
            /// VSCOLOR_BUTTONTEXT
            /// </summary>
            VSCOLOR_BUTTONTEXT = -199,

            /// <summary>
            /// VSCOLOR_WINDOW
            /// </summary>
            VSCOLOR_WINDOW = -217,
        }

        /// <summary>
        /// Adds the <see cref="Path.DirectorySeparatorChar"/> character to the end of the path if it doesn't already exist at the end.
        /// </summary>
        /// <param name="path">The string to add the trailing directory separator character to.</param>
        /// <returns>The original string with the specified character at the end.</returns>
        public static string EnsureTrailingDirectoryChar(string path)
        {
            return EnsureTrailingChar(path, Path.DirectorySeparatorChar);
        }

        /// <summary>
        /// Adds the specified character to the end of the string if it doesn't already exist at the end.
        /// </summary>
        /// <param name="value">The string to add the trailing character to.</param>
        /// <param name="charToEnsure">The character that will be at the end of the string upon return.</param>
        /// <returns>The original string with the specified character at the end.</returns>
        public static string EnsureTrailingChar(string value, char charToEnsure)
        {
            VerifyStringArgument(value, "value");

            if (value[value.Length - 1] != charToEnsure)
            {
                value += charToEnsure;
            }

            return value;
        }

        /// <summary>
        /// Gets a strongly-typed service from the environment, throwing an exception if the service cannot be retrieved.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to get (i.e. IVsShell).</typeparam>
        /// <typeparam name="TService">The service type to get (i.e. SvsShell).</typeparam>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> to use for retrieving the service.</param>
        /// <returns>An object that implements the interface from the environment.</returns>
        public static TInterface GetService<TInterface, TService>(IServiceProvider serviceProvider)
            where TInterface: class
            where TService: class
        {
            VerifyNonNullArgument(serviceProvider, "serviceProvider");

            TInterface service = serviceProvider.GetService(typeof(TService)) as TInterface;

            if (service == null)
            {
                string message = SafeStringFormat(CultureInfo.CurrentUICulture, WixStrings.CannotGetService, typeof(TInterface).Name);
                throw new InvalidOperationException(message);
            }

            return service;
        }

        /// <summary>
        /// Gets a strongly-typed service from the environment, throwing an exception if the service cannot be retrieved.
        /// This function returns null instead of throwing an exception when the service cannot be found and should be used
        /// only in methods invoked by property pages and forms to allow them to be editable in the VS desgners.
        /// </summary>
        /// <typeparam name="TInterface">The interface type to get (i.e. IVsShell).</typeparam>
        /// <typeparam name="TService">The service type to get (i.e. SvsShell).</typeparam>
        /// <param name="serviceProvider">A <see cref="IServiceProvider"/> to use for retrieving the service.</param>
        /// <returns>An object that implements the interface from the environment.</returns>
        public static TInterface GetServiceNoThrow<TInterface, TService>(IServiceProvider serviceProvider)
            where TInterface: class
            where TService: class
        {
            if (serviceProvider == null)
            {
                return null;
            }

            TInterface service = serviceProvider.GetService(typeof(TService)) as TInterface;

            return service;
        }

        /// <summary>
        /// Gets the font provided by the VS environment for dialog UI.
        /// </summary>
        /// <returns>Dialog font, or null if it is not available.</returns>
        public static Font GetDialogFont()
        {
            IUIHostLocale uiHostLocale = WixHelperMethods.GetServiceNoThrow<IUIHostLocale, IUIHostLocale>(WixPackage.Instance);
            if (uiHostLocale != null)
            {
                UIDLGLOGFONT[] pLOGFONT = new UIDLGLOGFONT[1];
                if (uiHostLocale.GetDialogFont(pLOGFONT) == 0)
                {
                    return Font.FromLogFont(pLOGFONT[0]);
                }
            }

            return null;
        }

        /// <summary>
        /// Returns a value indicating whether we can recover from the specified exception. If we can't recover,
        /// then it's expected that the caller will immediately call <see cref="Shutdown"/>.
        /// </summary>
        /// <param name="e">The <see cref="Exception"/> to test.</param>
        /// <returns>true if we cannot recover from the exception; otherwise, false.</returns>
        public static bool IsExceptionUnrecoverable(Exception e)
        {
            return (e is StackOverflowException);
        }

        /// <summary>
        /// Combines two registry paths.
        /// </summary>
        /// <param name="path1">The first path to combine.</param>
        /// <param name="path2">The second path to combine.</param>
        /// <returns>The concatenation of the first path with the second, delimeted with a '\'.</returns>
        public static string RegistryPathCombine(string path1, string path2)
        {
            VerifyStringArgument(path1, "path1");
            VerifyStringArgument(path2, "path2");

            return EnsureTrailingChar(path1, '\\') + path2;
        }

        /// <summary>
        /// Attempts to format the specified string by calling <see cref="System.String.Format(IFormatProvider, string, object[])"/>.
        /// If a <see cref="FormatException"/> is raised, then <paramref name="format"/> is returned.
        /// </summary>
        /// <param name="provider">An <see cref="IFormatProvider"/> that supplies culture-specific formatting information.</param>
        /// <param name="format">A string containing zero or more format items.</param>
        /// <param name="args">An object array containing zero or more objects to format.</param>
        /// <returns>A copy of <paramref name="format"/> in which the format items have been replaced by the string equivalent of the corresponding instances of object in args.</returns>
        public static string SafeStringFormat(IFormatProvider provider, string format, params object[] args)
        {
            string formattedString = format;

            try
            {
                if (args != null && args.Length > 0)
                {
                    formattedString = String.Format(provider, format, args);
                }
            }
            catch (FormatException)
            {
            }

            return formattedString;
        }

        /// <summary>
        /// Performs a ship assertion, which raises an assertion dialog. TODO: Generate a call stack and email it to some alias.
        /// </summary>
        /// <param name="condition">The condition to assert.</param>
        /// <param name="message">The message to show in the assertion.</param>
        /// <param name="args">An array of arguments for the formatted message.</param>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static void ShipAssert(bool condition, string message, params object[] args)
        {
            if (!condition)
            {
                VerifyStringArgument(message, "message");

                try
                {
                    // get the stack trace (not including this method)
                    StackTrace stack = new StackTrace(1, true);
                    string stackTrace = stack.ToString();

                    // create a StringBuilder to do our string concatenations
                    StringBuilder formattedMessage = new StringBuilder(message.Length + stackTrace.Length);

                    // append the message to the string
                    formattedMessage.Append(SafeStringFormat(CultureInfo.CurrentUICulture, message, args));

                    // append the stack trace
                    formattedMessage.Append(Environment.NewLine);
                    formattedMessage.Append(stackTrace);

                    // trace the message and show an assertion dialog
                    TraceFail(formattedMessage.ToString());
                }
                catch (Exception e)
                {
                    if (IsExceptionUnrecoverable(e))
                    {
                        Shutdown();
                    }

                    TraceFail("There was an exception while trying to perform a ShipAssert: {0}", e);
                }
            }
        }

        /// <summary>
        /// Shuts down the process by calling <see cref="Environment.FailFast"/>, which will write an event log
        /// entry and create a managed Watson dump.
        /// </summary>
        public static void Shutdown()
        {
            Environment.FailFast(WixStrings.CatastrophicError);
        }

        /// <summary>
        /// Shows an error message box with an OK button using the correct flags and title and optionally formats the message.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use.</param>
        /// <param name="message">An unformatted message to show.</param>
        /// <param name="args">The arguments to use for formatting the message.</param>
        public static void ShowErrorMessageBox(IServiceProvider serviceProvider, string message, params object[] args)
        {
            OLEMSGICON icon = OLEMSGICON.OLEMSGICON_CRITICAL;
            OLEMSGBUTTON buttons = OLEMSGBUTTON.OLEMSGBUTTON_OK;
            OLEMSGDEFBUTTON defaultButton = OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST;

            WixHelperMethods.ShowMessageBox(serviceProvider, buttons, icon, defaultButton, message, args);
        }

        /// <summary>
        /// Shows a message box using the correct flags and title and optionally formats the message.
        /// </summary>
        /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to use.</param>
        /// <param name="buttons">The buttons to show.</param>
        /// <param name="icon">The icon to show.</param>
        /// <param name="defaultButton">Determines which button has the default focus.</param>
        /// <param name="message">An unformatted message to show.</param>
        /// <param name="args">The arguments to use for formatting the message.</param>
        public static void ShowMessageBox(IServiceProvider serviceProvider, OLEMSGBUTTON buttons, OLEMSGICON icon, OLEMSGDEFBUTTON defaultButton, string message, params object[] args)
        {
            // format the message if required
            if (args != null && args.Length > 0)
            {
                message = String.Format(CultureInfo.CurrentUICulture, message, args);
            }

            // show the message box
            VsShellUtilities.ShowMessageBox(serviceProvider, message, String.Empty, icon, buttons, defaultButton);
        }

        /// <summary>
        /// Calls <see cref="Trace.Fail(string)"/> with a formatted message.
        /// </summary>
        /// <param name="message">The message to format.</param>
        /// <param name="args">The arguments to use in the format. Can be null or empty.</param>
        [Conditional("TRACE")]
        public static void TraceFail(string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                message = SafeStringFormat(CultureInfo.CurrentUICulture, message, args);
            }

            Trace.Fail(message);
        }

        /// <summary>
        /// Verifies that the specified argument is not null and throws an <see cref="ArgumentNullException"/> if it is.
        /// </summary>
        /// <param name="argument">The argument to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void VerifyNonNullArgument(object argument, string argumentName)
        {
            if (argument == null)
            {
                TraceFail("The argument '{0}' is null.", argumentName);
                throw new ArgumentNullException(argumentName);
            }
        }

        /// <summary>
        /// Verifies that the specified string argument is non-null and non-empty, asserting if it
        /// is not and throwing a new <see cref="ArgumentException"/>.
        /// </summary>
        /// <param name="argument">The argument to check.</param>
        /// <param name="argumentName">The name of the argument.</param>
        public static void VerifyStringArgument(string argument, string argumentName)
        {
            if (argument == null || argument.Length == 0 || argument.Trim().Length == 0)
            {
                string message = String.Format(CultureInfo.InvariantCulture, "The string argument '{0}' is null or empty.", argumentName);
                TraceFail("Invalid string argument", message);
                throw new ArgumentException(message, argumentName);
            }
        }

        /// <summary>
        /// Finds child nodes uner the parent node and places them in the currentList.
        /// </summary>
        /// <param name="currentList">List to be populated with the nodes.</param>
        /// <param name="parent">Parent node under which the nodes should be searched.</param>
        /// <param name="filter">Filter to be used while selecting the node.</param>
        /// <param name="criteria">Criteria to be used by the filter.</param>
        public static void FindNodes(IList<HierarchyNode> currentList, HierarchyNode parent, WixNodeFilter filter, object criteria)
        {
            if (currentList == null)
            {
                throw new ArgumentNullException("currentList");
            }

            if (parent == null)
            {
                throw new ArgumentNullException("parent");
            }

            if (filter == null)
            {
                throw new ArgumentNullException("filter");
            }

            for (HierarchyNode child = parent.FirstChild; child != null; child = child.NextSibling)
            {
                if (filter(child, criteria))
                {
                    currentList.Add(child);
                }

                WixHelperMethods.FindNodes(currentList, child, filter, criteria);
            }
        }

        /// <summary>
        /// Makes subPath relative with respect to basePath.
        /// </summary>
        /// <param name="basePath">Base folder path.</param>
        /// <param name="subPath">Path of the sub folder or file.</param>
        /// <returns>The relative path for the subPath if it shares the same root with basePath or subPath otherwise.</returns>
        /// <remarks>
        /// We introduced GetRelativePath method because the Microsoft.VisualStudio.Shell.PackageUtilities.MakeRelative() doesn't
        /// work as expected in some cases (as of 11/12/2007). For example:
        /// Test # 1
        /// Base Path:      C:\a\b\r\d\..\..\e\f
        /// Sub Path:       c:\a\b\e\f\g\h\..\i\j.txt
        /// Expected:       g\i\j.txt
        /// Actual:         c:\a\b\e\f\g\h\..\i\j.txt
        /// -------------
        /// Test # 2
        /// Base Path:      \\mghaznawks\a\e\f
        /// Sub Path:       \\mghaznawks\e\f\g\h\i\j.txt
        /// Expected:       \\mghaznawks\e\f\g\h\i\j.txt
        /// Actual:         ..\..\..\e\f\g\h\i\j.txt
        /// Note that the base root path is \\mghaznawks\a\   Ref: System.IO.Path.GetPathRoot(string)
        /// -------------
        /// Test # 3
        /// Base Path:      \\mghaznawks\C$\a\..\e\f
        /// Sub Path:       \\mghaznawks\D$\e\f\g\h\i\j.txt
        /// Expected:       \\mghaznawks\D$\e\f\g\h\i\j.txt
        /// Actual:         ..\..\..\..\..\D$\e\f\g\h\i\j.txt
        /// -------------
        /// Test # 4
        /// Base Path:      \\mghaznawks\C$\a\..\e\f
        /// Sub Path:       \\mghaznawks\c$\e\f\g\h\i\j.txt
        /// Expected:       g\h\i\j.txt
        /// Actual:         ..\..\..\..\..\c$\e\f\g\h\i\j.txt
        /// </remarks>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.ArgumentException.#ctor(System.String)")]
        public static string GetRelativePath(string basePath, string subPath)
        {
            VerifyStringArgument(basePath, "basePath");
            VerifyStringArgument(subPath, "subPath");

            if (!Path.IsPathRooted(basePath))
            {
                throw new ArgumentException("The 'basePath' is not rooted.");
            }

            if (!Path.IsPathRooted(subPath))
            {
                return subPath;
            }

            if (!String.Equals(Path.GetPathRoot(basePath), Path.GetPathRoot(subPath), StringComparison.OrdinalIgnoreCase))
            {
                // both paths have different roots so we can't make them relative
                return subPath;
            }

            // Url.MakeRelative method requires the base path to be ended with a '\' if it is a folder,
            // otherwise it considers it as a file so we need to make sure that the folder path is right
            basePath = WixHelperMethods.EnsureTrailingDirectoryChar(basePath.Trim());

            Url url = new Url(basePath);
            return url.MakeRelative(new Url(subPath));
        }

        /// <summary>
        /// Maps all pixels in a certain color in an image to a different color
        /// </summary>
        /// <param name="unmappedBitmap">Image to be mapped</param>
        /// <param name="originalColor">Pixes in this color will be changed to the new color</param>
        /// <param name="newColor">New color for the selected pixels</param>
        /// <returns>Image with pixels in the original color replaces by pixels in the new color</returns>
        [SuppressMessage("Microsoft.Design", "CA1031:DoNotCatchGeneralExceptionTypes")]
        public static Image MapBitmapColor(Image unmappedBitmap, Color originalColor, Color newColor)
        {
            Bitmap mappedBitmap;

            try
            {
                mappedBitmap = new Bitmap(unmappedBitmap);
                using (Graphics g = Graphics.FromImage(mappedBitmap))
                {
                    Size size = unmappedBitmap.Size;
                    Rectangle r = new Rectangle(new Point(0, 0), size);
                    ColorMap[] colorMaps = new ColorMap[1];
                    colorMaps[0] = new ColorMap();
                    colorMaps[0].OldColor = originalColor;
                    colorMaps[0].NewColor = newColor;

                    using (ImageAttributes imageAttributes = new ImageAttributes())
                    {
                        imageAttributes.SetRemapTable(colorMaps, ColorAdjustType.Bitmap);
                        g.DrawImage(unmappedBitmap, r, 0, 0, size.Width, size.Height, GraphicsUnit.Pixel, imageAttributes);
                    }
                }
            }
            catch (Exception)
            {
                // the documentation for Graphics.FromImage says it may throw a generic Exception type if the image is badly formed
                return unmappedBitmap;
            }

            return mappedBitmap;
        }

        /// <summary>
        /// Replaces the leading path with propertyName if it matches property value.
        /// </summary>
        /// <param name="sourcePath">Path to be tokenized.</param>
        /// <param name="propertyName">Property</param>
        /// <param name="propertyValue">Property Value</param>
        /// <returns>Empty string if input is not valid. Else will prefix the path with propertyName as applicable</returns>
        public static string ReplacePathWithBuildProperty(string sourcePath, string propertyName, string propertyValue)
        {
            if (String.IsNullOrEmpty(sourcePath))
            {
                return String.Empty;
            }

            if (Directory.Exists(propertyValue) || File.Exists(propertyValue))
            {
                FileInfo sourceFile = new FileInfo(sourcePath);
                FileInfo tokenValueFile = new FileInfo(propertyValue);

                if (sourceFile.FullName.StartsWith(tokenValueFile.FullName, StringComparison.OrdinalIgnoreCase))
                {
                    string path = sourceFile.FullName.Substring(tokenValueFile.FullName.Length).TrimStart(Path.DirectorySeparatorChar);
                    return Path.Combine(propertyName, path);
                }
            }

            return sourcePath;
        }

        /// <summary>
        /// Replaces the leading path with propertyName if it matches property value.
        /// </summary>
        /// <param name="sourcePath">Path to be tokenized.</param>
        /// <param name="propertyName">Property</param>
        /// <param name="propertyValue">Property Value</param>
        /// <returns>Empty string if input is not valid. Else will prefix the path with propertyName as applicable</returns>
        public static string ReplaceBuildPropertyWithPath(string sourcePath, string propertyName, string propertyValue)
        {
            if (String.IsNullOrEmpty(sourcePath))
            {
                return String.Empty;
            }

            string path = sourcePath;
            if (sourcePath.StartsWith(propertyName, StringComparison.OrdinalIgnoreCase))
            {
                path = sourcePath.Substring(propertyName.Length).TrimStart(Path.DirectorySeparatorChar);
                path = Path.Combine(propertyValue, path);
            }

            return path;
        }

        /// <summary>
        /// Opens WiX.chm and displays the specified topic.
        /// </summary>
        /// <param name="parent">The parent control.</param>
        /// <param name="topic">The topic to show.</param>
        /// <returns></returns>
        public static void ShowWixHelp(Control parent, string topic)
        {
            string wixHelpFile = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetExecutingAssembly().Location), @"..\doc\WiX.chm"));

            Help.ShowHelp(parent, wixHelpFile, HelpNavigator.Topic, topic);
        }

        /// <summary>
        /// Handles command status on source a node. Should be overridden by descendant nodes.
        /// </summary>
        /// <param name="node">A HierarchyNode that implements the IProjectSourceNode interface.</param>
        /// <param name="guidCmdGroup">A unique identifier of the command group. The pguidCmdGroup parameter can be NULL to specify the standard group.</param>
        /// <param name="cmd">The command to query status for.</param>
        /// <param name="result">An out parameter specifying the QueryStatusResult of the command.</param>
        /// <param name="returnCode">If the method succeeds, it returns S_OK. If it fails, it returns an error code.</param>
        /// <returns>Returns true if the status request is handled, false otherwise.</returns>
        internal static bool QueryStatusOnProjectSourceNode(HierarchyNode node, Guid guidCmdGroup, uint cmd, ref QueryStatusResult result, out int returnCode)
        {
            if (guidCmdGroup == VsMenus.guidStandardCommandSet2K)
            {
                IProjectSourceNode sourceNode = node as IProjectSourceNode;
                switch ((VsCommands2K)cmd)
                {
                    case VsCommands2K.SHOWALLFILES:
                        {
                            WixProjectNode projectNode = node.ProjectMgr as WixProjectNode;
                            result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                            if (projectNode != null && projectNode.ShowAllFilesEnabled)
                            {
                                result |= QueryStatusResult.LATCHED; // it should be displayed as pressed
                            }

                            returnCode = VSConstants.S_OK;
                            return true; // handled.
                        }

                    case VsCommands2K.INCLUDEINPROJECT:
                        // if it is a non member item node, the we support "Include In Project" command
                        if (sourceNode != null && sourceNode.IsNonMemberItem)
                        {
                            result |= QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                            returnCode = VSConstants.S_OK;
                            return true; // handled.
                        }

                        break;

                    case VsCommands2K.EXCLUDEFROMPROJECT:
                        // if it is a non member item node, then we don't support "Exclude From Project" command
                        if (sourceNode != null && sourceNode.IsNonMemberItem)
                        {
                            returnCode = (int)OleConstants.MSOCMDERR_E_NOTSUPPORTED;
                            return true; // handled.
                        }

                        break;
                }
            }

            if (VsMenus.guidStandardCommandSet97 == guidCmdGroup)
            {
                switch ((VsCommands)cmd)
                {
                    case VsCommands.AddNewItem:
                    case VsCommands.AddExistingItem:
                        result = QueryStatusResult.SUPPORTED | QueryStatusResult.ENABLED;
                        returnCode = VSConstants.S_OK;
                        return true;
                    case VsCommands.SetStartupProject:
                        result = QueryStatusResult.SUPPORTED | QueryStatusResult.INVISIBLE;
                        returnCode = VSConstants.S_OK;
                        return true;
                }
            }

            // just an arbitrary value, it doesn't matter if method hasn't handled the request
            returnCode = VSConstants.S_FALSE;

            // not handled
            return false;
        }

        /// <summary>
        /// Walks up in the hierarchy and ensures that all parent folder nodes of 'node' are included in the project.
        /// </summary>
        /// <param name="node">Start hierarchy node.</param>
        internal static void EnsureParentFolderIncluded(HierarchyNode node)
        {
            if (node == null)
            {
                throw new ArgumentNullException("node");
            }

            // use stack to make sure all parent folders are included in the project.
            Stack<WixFolderNode> stack = new Stack<WixFolderNode>();

            // Find out the parent folder nodes if any.
            WixFolderNode parentFolderNode = node.Parent as WixFolderNode;
            while (parentFolderNode != null && parentFolderNode.IsNonMemberItem)
            {
                stack.Push(parentFolderNode);
                parentFolderNode.CreateDirectory(); // ensure that the folder is there on file system
                parentFolderNode = parentFolderNode.Parent as WixFolderNode;
            }

            // include all parent folders in the project.
            while (stack.Count > 0)
            {
                WixFolderNode folderNode = stack.Pop();
                ((IProjectSourceNode)folderNode).IncludeInProject(false);
            }
        }

        /// <summary>
        /// Sets the colors of the passed control and all of its child controls by using the VS Colors services
        /// </summary>
        /// <param name="parent">Parent form/control</param>
        internal static void SetControlTreeColors(Control parent)
        {
            SetSingleControlColors(parent);

            if (parent.Controls != null)
            {
                foreach (Control child in parent.Controls)
                {
                    SetControlTreeColors(child);
                }
            }
        }

        /// <summary>
        /// Sets the colors of the control passed as a parameter
        /// </summary>
        /// <param name="control">Control on which the colors are being set</param>
        internal static void SetSingleControlColors(Control control)
        {
            control.ForeColor = GetVsColor(Vs2010Color.VSCOLOR_BUTTONTEXT);
            if (control is TextBox || control is ListBox || control is ListView || control is ComboBox ||
                control is WixBuildEventTextBox)
            {
                control.BackColor = GetVsColor(Vs2010Color.VSCOLOR_WINDOW);
            }
        }

        /// <summary>
        /// Returns a standard VS color or a system color, if the VS colors service is not available
        /// </summary>
        /// <param name="visualStudioColor">Color enum</param>
        /// <returns>The color itself</returns>
        internal static Color GetVsColor(Vs2010Color visualStudioColor)
        {
            uint win32Color = 0;
            IVsUIShell2 vsuiShell2 = WixPackage.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell2;
            if (vsuiShell2 != null && vsuiShell2.GetVSSysColorEx((Int32)visualStudioColor, out win32Color) == VSConstants.S_OK)
            {
                Color color = ColorTranslator.FromWin32((int)win32Color);
                return color;
            }

            // We need to fall back to some reasonable colors when we're not running in VS
            // to keep the forms/property pages editable in the designers
            switch (visualStudioColor)
            {
                case Vs2010Color.VSCOLOR_BUTTONFACE:
                    return SystemColors.ButtonFace;

                case Vs2010Color.VSCOLOR_BUTTONTEXT:
                    return SystemColors.ControlText;

                case Vs2010Color.VSCOLOR_WINDOW:
                    return SystemColors.Window;

                default:
                    return Color.Red;
            }
        }

        /// <summary>
        /// Refreshes the data in the property browser
        /// </summary>
        internal static void RefreshPropertyBrowser()
        {
            IVsUIShell vsuiShell = WixPackage.GetGlobalService(typeof(SVsUIShell)) as IVsUIShell;

            if (vsuiShell == null)
            {
                string message = WixHelperMethods.SafeStringFormat(CultureInfo.CurrentUICulture, WixStrings.CannotGetService, typeof(IVsUIShell).Name);
                throw new InvalidOperationException(message);
            }
            else
            {
                int hr = vsuiShell.RefreshPropertyBrowser(0);
                if (hr != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
        }

        /// <summary>
        /// Returns the WaitCursor as IDisposable.
        /// </summary>
        /// <returns>Returns the WaitCursor as IDisposable.</returns>
        internal static IDisposable NewWaitCursor()
        {
            return new WaitCursor();
        }

        /// <summary>
        /// Implements the 'Open in Windows Explorer' command
        /// </summary>
        /// <param name="folderPath">Folder to be shown in windows explorer</param>
        internal static void ExploreFolderInWindows(string folderPath)
        {
            Process.Start(folderPath);
        }

        /// <summary>
        /// Reloads all nodes which are not part of the project,
        /// ensuring that they match the current file structure on disk
        /// </summary>
        /// <param name="node">The selected hierarchy node</param>
        internal static void RefreshProject(HierarchyNode node)
        {
            WixProjectNode projectNode = node.ProjectMgr as WixProjectNode;

            if (projectNode.ShowAllFilesEnabled)
            {
                projectNode.ToggleShowAllFiles();
                projectNode.ToggleShowAllFiles();
            }
        }

        /// <summary>
        /// Removes the matching strings from the list of strings.
        /// </summary>
        /// <param name="values">list of strings</param>
        /// <param name="match">string to match</param>
        /// <returns>number of matches removed</returns>
        internal static int RemoveAllMatch(List<string> values, string match)
        {
            return values.RemoveAll(delegate(string s) { return s == match; });
        }

        /// <summary>
        /// WaitCursor internal class.
        /// </summary>
        private class WaitCursor : IDisposable
        {
            private Cursor currentCursor;

            /// <summary>
            /// Default Cursor.
            /// </summary>
            public WaitCursor()
            {
                this.currentCursor = Cursor.Current;
                Cursor.Current = Cursors.WaitCursor;
            }

            // =========================================================================================
            // IDisposable Members
            // =========================================================================================

            /// <summary>
            /// Disposes this object.
            /// </summary>
            public void Dispose()
            {
                if (this.currentCursor != null)
                {
                    lock (this)
                    {
                        if (this.currentCursor != null)
                        {
                            Cursor.Current = this.currentCursor;
                            this.currentCursor = null;
                        }
                    }
                }

                GC.SuppressFinalize(this);
            }
        }
    }
}