// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;

    /// <summary>
    /// Options for building the cabinet.
    /// </summary>
    public enum CabinetBuildOption
    {
        /// <summary>
        /// Build the cabinet and move it to the target location.
        /// </summary>
        BuildAndMove,

        /// <summary>
        /// Build the cabinet and copy it to the target location.
        /// </summary>
        BuildAndCopy,

        /// <summary>
        /// Just copy the cabinet to the target location.
        /// </summary>
        Copy
    }

    /// <summary>
    /// Bind stage of a file.. The reason we need this is to change the ResolveFile behavior based on if
    /// dynamic bindpath plugin is desirable. We cannot change the signature of ResolveFile since it might
    /// break existing implementers which derived from BinderFileManager
    /// </summary>
    public enum BindStage
    {
        /// <summary>
        /// Normal binding
        /// </summary>
        Normal,

        /// <summary>
        /// Bind the file path of the target build file
        /// </summary>
        Target,

        /// <summary>
        /// Bind the file path of the updated build file
        /// </summary>
        Updated,
    }

    /// <summary>
    /// Base class for creating a binder file manager.
    /// </summary>
    public class BinderFileManager
    {
        private IMessageHandler messageHandler;

        private string cabCachePath;
        private Output output;
        private bool reuseCabinets;
        private SubStorage activeSubstorage;
        private bool deltaBinaryPatch;
        private string tempFilesLocation;
        private bool suppressHardLinks;
        private Dictionary<BindStage, StringCollection> sourcePaths;
        private Dictionary<BindStage, StringCollection> bindPaths;
        private Dictionary<BindStage, NameValueCollection> namedBindPaths;

        /// <summary>
        /// Instantiate a new BinderFileManager.
        /// </summary>
        public BinderFileManager()
        {
            this.sourcePaths = new Dictionary<BindStage, StringCollection>();
            this.bindPaths = new Dictionary<BindStage, StringCollection>();
            this.namedBindPaths = new Dictionary<BindStage, NameValueCollection>();

            this.sourcePaths.Add(BindStage.Normal, new StringCollection());
            this.sourcePaths.Add(BindStage.Target, new StringCollection());
            this.sourcePaths.Add(BindStage.Updated, new StringCollection());

            this.bindPaths.Add(BindStage.Normal, new StringCollection());

            this.namedBindPaths.Add(BindStage.Normal, new NameValueCollection());
            this.namedBindPaths.Add(BindStage.Target, new NameValueCollection());
            this.namedBindPaths.Add(BindStage.Updated, new NameValueCollection());
        }

        /// <summary>
        /// Gets the bind paths to locate files.
        /// </summary>
        /// <value>The bind paths to locate files.</value>
        public StringCollection BindPaths
        {
            get { return this.bindPaths[BindStage.Normal]; }
        }

        /// <summary>
        /// Gets the named paths to locate files.
        /// </summary>
        /// <value>The named bind paths to locate files.</value>
        public NameValueCollection NamedBindPaths
        {
            get { return this.namedBindPaths[BindStage.Normal]; }
        }

        /// <summary>
        /// Gets or sets the path to cabinet cache.
        /// </summary>
        /// <value>The path to cabinet cache.</value>
        public string CabCachePath
        {
            get { return this.cabCachePath; }
            set { this.cabCachePath = value; }
        }

        /// <summary>
        /// Gets or sets the message handler used for file resolution.
        /// </summary>
        public IMessageHandler MessageHandler
        {
            get { return this.messageHandler; }
            set { this.messageHandler = value; }
        }

        /// <summary>
        /// Gets or sets the output object used for binding.
        /// </summary>
        /// <value>The output object.</value>
        public Output Output
        {
            get { return this.output; }
            set { this.output = value; }
        }

        /// <summary>
        /// Gets or sets the option to reuse cabinets in the cache.
        /// </summary>
        /// <value>The option to reuse cabinets in the cache.</value>
        public bool ReuseCabinets
        {
            get { return this.reuseCabinets; }
            set { this.reuseCabinets = value; }
        }

        /// <summary>
        /// Gets the collection of all source paths to intermediate files.
        /// </summary>
        /// <value>The collection of all source paths to intermediate files.</value>
        public StringCollection SourcePaths
        {
            get { return this.sourcePaths[BindStage.Normal]; }
        }

        /// <summary>
        /// Gets or sets the active subStorage used for binding.
        /// </summary>
        /// <value>The subStorage object.</value>
        public SubStorage ActiveSubStorage
        {
            get { return this.activeSubstorage; }
            set { this.activeSubstorage = value; }
        }

        /// <summary>
        /// Gets or sets the option to enable building binary delta patches.
        /// </summary>
        /// <value>The option to enable building binary delta patches.</value>
        public bool DeltaBinaryPatch
        {
            get { return this.deltaBinaryPatch; }
            set { this.deltaBinaryPatch = value; }
        }

        /// <summary>
        /// Gets or sets the path to the temp files location.
        /// </summary>
        /// <value>The path to the temp files location.</value>
        public string TempFilesLocation
        {
            get { return this.tempFilesLocation; }
            set { this.tempFilesLocation = value; }
        }

        /// <summary>
        /// Gets or sets the option to suppress hard links during build.
        /// </summary>
        /// <value>The option to suppress hard links during build.</value>
        public bool SuppressHardLinks
        {
            get { return this.suppressHardLinks; }
            set { this.suppressHardLinks = value; }
        }

        /// <summary>
        /// Gets the collection of paths to locate files during ResolveFile when BindStage is Target
        /// </summary>
        /// <value>The named bind paths to locate files.</value>
        public StringCollection TargetSourcePaths
        {
            get { return this.sourcePaths[BindStage.Target]; }
        }

        /// <summary>
        /// Gets the collection of paths to locate files during ResolveFile when BindStage is Updated
        /// </summary>
        /// <value>The named bind paths to locate files.</value>
        public StringCollection UpdatedSourcePaths
        {
            get { return this.sourcePaths[BindStage.Updated]; }
        }

        /// <summary>
        /// Gets the named bind paths to locate files during ResolveFile when the BindStage is Target
        /// </summary>
        /// <value>The named bind paths to locate files.</value>
        public NameValueCollection TargetNamedBindPaths
        {
            get { return this.namedBindPaths[BindStage.Target]; }
        }

        /// <summary>
        /// Gets the named bind paths to locate files during ResolveFile when the BindStage is Updated
        /// </summary>
        /// <value>The named bind paths to locate files.</value>
        public NameValueCollection UpdatedNamedBindPaths
        {
            get { return this.namedBindPaths[BindStage.Updated]; }
        }

        /// <summary>
        /// Gets the property if re-basing target is true or false
        /// </summary>
        /// <value>It returns true if target bind path is to be replaced, otherwise false.</value>
        public bool ReBaseTarget
        {
            get { return (0 != this.namedBindPaths[BindStage.Target].Count || 0 != this.sourcePaths[BindStage.Target].Count); }
        }

        /// <summary>
        /// Gets the property if re-basing updated build is true or false
        /// </summary>
        /// <value>It returns true if updated bind path is to be replaced, otherwise false.</value>
        public bool ReBaseUpdated
        {
            get { return (0 != this.namedBindPaths[BindStage.Updated].Count || 0 != this.sourcePaths[BindStage.Updated].Count); }
        }

        /// <summary>
        /// Compares two files to determine if they are equivalent.
        /// </summary>
        /// <param name="targetFile">The target file.</param>
        /// <param name="updatedFile">The updated file.</param>
        /// <returns>true if the files are equal; false otherwise.</returns>
        public virtual bool CompareFiles(string targetFile, string updatedFile)
        {
            FileInfo targetFileInfo = new FileInfo(targetFile);
            FileInfo updatedFileInfo = new FileInfo(updatedFile);

            if (targetFileInfo.Length != updatedFileInfo.Length)
            {
                return false;
            }

            using (FileStream targetStream = File.OpenRead(targetFile))
            {
                using (FileStream updatedStream = File.OpenRead(updatedFile))
                {
                    if (targetStream.Length != updatedStream.Length)
                    {
                        return false;
                    }

                    // Using a larger buffer than the default buffer of 4 * 1024 used by FileStream.ReadByte improves performance.
                    // The buffer size is based on user feedback. Based on performance results, a better buffer size may be determined.
                    byte[] targetBuffer = new byte[16 * 1024];
                    byte[] updatedBuffer = new byte[16 * 1024];
                    int targetReadLength;
                    int updatedReadLength;
                    do
                    {
                        targetReadLength = targetStream.Read(targetBuffer, 0, targetBuffer.Length);
                        updatedReadLength = updatedStream.Read(updatedBuffer, 0, updatedBuffer.Length);
                        
                        if (targetReadLength != updatedReadLength)
                        {
                            return false;
                        }

                        for (int i = 0; i < targetReadLength; ++i)
                        {
                            if (targetBuffer[i] != updatedBuffer[i])
                            {
                                return false;
                            }
                        }

                    } while (0 < targetReadLength);
                }
            }

            return true;
        }

        /// <summary>
        /// Resolves the source path of a file.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public virtual string ResolveFile(string source)
        {
            return null;
        }

        /// <summary>
        /// Resolves the source path of a file.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public virtual string ResolveFile(string source, string type, SourceLineNumberCollection sourceLineNumber)
        {
            return ResolveFile(source);
        }

        /// <summary>
        /// Resolves the source path of a file.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public virtual string ResolveFile(string source, string type, SourceLineNumberCollection sourceLineNumbers, BindStage bindStage)
        {
            // the following new local variables are used for bind path and protect the changes to object field.
            StringCollection currentBindPaths = null;
            NameValueCollection currentNamedBindPaths = null;
            StringCollection currentSourcePaths = null;

            if (String.IsNullOrEmpty(source))
            {
                throw new ArgumentNullException("source");
            }

            // Call the original override function first. If it returns an answer then return that,
            // otherwise using the default resolving logic
            string filePath = this.ResolveFile(source, type, sourceLineNumbers);
            if (!String.IsNullOrEmpty(filePath))
            {
                return filePath;
            }

            // Assign the correct bind path to file manager
            currentSourcePaths = this.sourcePaths[bindStage];
            currentNamedBindPaths = this.namedBindPaths[bindStage];
            if (BindStage.Target != bindStage && BindStage.Updated != bindStage)
            {
                currentBindPaths = this.bindPaths[bindStage];
            }
            else
            {
                currentBindPaths = this.sourcePaths[bindStage];
            }

            // If the path is rooted, it better exist or we're not going to find it.
            if (Path.IsPathRooted(source))
            {
                if (BinderFileManager.CheckFileExists(source))
                {
                    return source;
                }
            }
            else // not a rooted path so let's try applying all the different source resolution options.
            {
                const string bindPathOpenString = "!(bindpath.";

                if (source.StartsWith(bindPathOpenString, StringComparison.Ordinal) && source.IndexOf(')') != -1)
                {
                    int bindpathSignatureLength = bindPathOpenString.Length;
                    string name = source.Substring(bindpathSignatureLength, source.IndexOf(')') - bindpathSignatureLength);
                    string[] values = currentNamedBindPaths.GetValues(name);

                    if (null != values)
                    {
                        foreach (string bindPath in values)
                        {
                            // Parse out '\\' chars that separate the "bindpath" variable and the next part of the path, 
                            // because Path.Combine() thinks that rooted second paths don't need the first path.
                            string nameSection = string.Empty;
                            int nameStart = bindpathSignatureLength + 1 + name.Length;  // +1 for the closing bracket.

                            nameSection = source.Substring(nameStart).TrimStart('\\');
                            filePath = Path.Combine(bindPath, nameSection);

                            if (BinderFileManager.CheckFileExists(filePath))
                            {
                                return filePath;
                            }
                        }
                    }
                }
                else if (source.StartsWith("SourceDir\\", StringComparison.Ordinal) || source.StartsWith("SourceDir/", StringComparison.Ordinal))
                {
                    foreach (string bindPath in currentBindPaths)
                    {
                        filePath = Path.Combine(bindPath, source.Substring(10));
                        if (BinderFileManager.CheckFileExists(filePath))
                        {
                            return filePath;
                        }
                    }
                }
                else if (BinderFileManager.CheckFileExists(source))
                {
                    return source;
                }

                foreach (string path in currentSourcePaths)
                {
                    filePath = Path.Combine(path, source);
                    if (BinderFileManager.CheckFileExists(filePath))
                    {
                        return filePath;
                    }

                    if (source.StartsWith("SourceDir\\", StringComparison.Ordinal) || source.StartsWith("SourceDir/", StringComparison.Ordinal))
                    {
                        filePath = Path.Combine(path, source.Substring(10));
                        if (BinderFileManager.CheckFileExists(filePath))
                        {
                            return filePath;
                        }
                    }
                }
            }

            // Didn't find the file.
            throw new WixFileNotFoundException(sourceLineNumbers, source, type);
        }

        /// <summary>
        /// Resolves the source path of a file related to another file's source.
        /// </summary>
        /// <param name="source">Original source value.</param>
        /// <param name="relatedSource">Source related to original source.</param>
        /// <param name="type">Optional type of source file being resolved.</param>
        /// <param name="sourceLineNumbers">Optional source line of source file being resolved.</param>
        /// <param name="bindStage">The binding stage used to determine what collection of bind paths will be used</param>
        /// <returns>Should return a valid path for the stream to be imported.</returns>
        public virtual string ResolveRelatedFile(string source, string relatedSource, string type, SourceLineNumberCollection sourceLineNumbers, BindStage bindStage)
        {
            string resolvedSource = this.ResolveFile(source, type, sourceLineNumbers, bindStage);
            return Path.Combine(Path.GetDirectoryName(resolvedSource), relatedSource);
        }

        /// <summary>
        /// Resolves the source path of a cabinet file.
        /// </summary>
        /// <param name="fileRows">Collection of files in this cabinet.</param>
        /// <param name="cabinetPath">Path to cabinet to generate.  Path may be modified by delegate.</param>
        /// <returns>The CabinetBuildOption.  By default the cabinet is built and moved to its target location.</returns>
        [SuppressMessage("Microsoft.Design", "CA1045:DoNotPassTypesByReference")]
        public virtual CabinetBuildOption ResolveCabinet(FileRowCollection fileRows, ref string cabinetPath)
        {
            if (fileRows == null)
            {
                throw new ArgumentNullException("fileRows");
            }

            // no special behavior specified, use the default
            if (null == this.cabCachePath && !this.reuseCabinets)
            {
                return CabinetBuildOption.BuildAndMove;
            }

            // if a cabinet cache path was provided, change the location for the cabinet
            // to be built to
            if (null != this.cabCachePath)
            {
                string cabinetName = Path.GetFileName(cabinetPath);
                cabinetPath = Path.Combine(this.cabCachePath, cabinetName);
            }

            // if we still think we're going to reuse the cabinet check to see if the cabinet exists first
            if (this.reuseCabinets)
            {
                bool cabinetValid = false;

                if (BinderFileManager.CheckFileExists(cabinetPath))
                {
                    // check to see if
                    // 1. any files are added or removed
                    // 2. order of files changed or names changed
                    // 3. modified time changed
                    cabinetValid = true;

                    // Need to force garbage collection of WixEnumerateCab to ensure the handle
                    // associated with it is closed before it is reused.
                    using (Cab.WixEnumerateCab wixEnumerateCab = new Cab.WixEnumerateCab())
                    {
                        ArrayList fileList = wixEnumerateCab.Enumerate(cabinetPath);

                        if (fileRows.Count != fileList.Count)
                        {
                            cabinetValid = false;
                        }
                        else
                        {
                            int i = 0;
                            foreach (FileRow fileRow in fileRows)
                            {
                                // First check that the file identifiers match because that is quick and easy.
                                CabinetFileInfo cabFileInfo = fileList[i] as CabinetFileInfo;
                                cabinetValid = (cabFileInfo.FileId == fileRow.File);
                                if (cabinetValid)
                                {
                                    // Still valid so ensure the source time stamp hasn't changed. Thus we need
                                    // to convert the source file time stamp into a cabinet compatible data/time.
                                    FileInfo fileInfo = new FileInfo(fileRow.Source);
                                    ushort sourceCabDate;
                                    ushort sourceCabTime;

                                    Cab.Interop.CabInterop.DateTimeToCabDateAndTime(fileInfo.LastWriteTime, out sourceCabDate, out sourceCabTime);
                                    cabinetValid = (cabFileInfo.Date == sourceCabDate && cabFileInfo.Time == sourceCabTime)
                                        && (cabFileInfo.Size == fileInfo.Length);
                                }

                                if (!cabinetValid)
                                {
                                    break;
                                }

                                i++;
                            }
                        }
                    }
                }

                return (cabinetValid ? CabinetBuildOption.Copy : CabinetBuildOption.BuildAndCopy);
            }
            else // by default move the built cabinet
            {
                return CabinetBuildOption.BuildAndMove;
            }
        }

        /// <summary>
        /// Resolve the layout path of a media.
        /// </summary>
        /// <param name="mediaRow">The media's row.</param>
        /// <param name="layoutDirectory">The layout directory for the setup image.</param>
        /// <returns>The layout path for the media.</returns>
        public virtual string ResolveMedia(MediaRow mediaRow, string layoutDirectory)
        {
            if (mediaRow == null)
            {
                throw new ArgumentNullException("mediaRow");
            }

            string mediaLayoutDirectory = mediaRow.Layout;

            if (null == mediaLayoutDirectory)
            {
                mediaLayoutDirectory = layoutDirectory;
            }
            else if (!Path.IsPathRooted(mediaLayoutDirectory))
            {
                mediaLayoutDirectory = Path.Combine(layoutDirectory, mediaLayoutDirectory);
            }

            return mediaLayoutDirectory;
        }

        /// <summary>
        /// Resolves the URL to a file.
        /// </summary>
        /// <param name="url">URL that may be a format string for the id and fileName.</param>
        /// <param name="packageId">Identity of the package (if payload is not part of a package) the URL points to. NULL if not part of a package.</param>
        /// <param name="payloadId">Identity of the payload the URL points to.</param>
        /// <param name="fileName">File name the URL points at.</param>
        /// <param name="fallbackUrl">Optional URL to use if the URL provided is empty.</param>
        /// <returns>An absolute URL or null if no URL is provided.</returns>
        public virtual string ResolveUrl(string url, string fallbackUrl, string packageId, string payloadId, string fileName)
        {
            // If a URL was not specified but there is a fallback URL that has a format specifier in it
            // then use the fallback URL formatter for this URL.
            if (String.IsNullOrEmpty(url) && !String.IsNullOrEmpty(fallbackUrl))
            {
                string formattedFallbackUrl = String.Format(fallbackUrl, packageId, payloadId, fileName);
                if (!String.Equals(fallbackUrl, formattedFallbackUrl, StringComparison.OrdinalIgnoreCase))
                {
                    url = fallbackUrl;
                }
            }

            if (!String.IsNullOrEmpty(url))
            {
                string formattedUrl = String.Format(url, packageId, payloadId, fileName);

                Uri canonicalUri;
                if (Uri.TryCreate(formattedUrl, UriKind.Absolute, out canonicalUri))
                {
                    url = canonicalUri.AbsoluteUri;
                }
                else
                {
                    url = null;
                }
            }

            return url;
        }

        /// <summary>
        /// Copies a file.
        /// </summary>
        /// <param name="source">The file to copy.</param>
        /// <param name="destination">The destination file.</param>
        /// <param name="overwrite">true if the destination file can be overwritten; otherwise, false.</param>
        public virtual void CopyFile(string source, string destination, bool overwrite)
        {
            if (overwrite)
            {
                File.Delete(destination);
            }

            if (this.suppressHardLinks || !CreateHardLink(destination, source, IntPtr.Zero))
            {
#if DEBUG
                if (!this.suppressHardLinks)
                {
                    int er = Marshal.GetLastWin32Error();
                }
#endif

                File.Copy(source, destination, overwrite);
            }
        }

        /// <summary>
        /// Moves a file.
        /// </summary>
        /// <param name="source">The file to move.</param>
        /// <param name="destination">The destination file.</param>
        public virtual void MoveFile(string source, string destination)
        {
            File.Move(source, destination);
        }

        /// <summary>
        /// Create patch if needed. This runs in the cabinet building thread.
        /// </summary>
        /// <param name="fileRow">The FileRow of the file to create the delta for.</param>
        /// <param name="retainRangeWarning">true if the retain ranges were ignored to mismatches.</param>
        [SuppressMessage("Microsoft.Design", "CA1021:AvoidOutParameters")]
        public virtual void ResolvePatch(FileRow fileRow, out bool retainRangeWarning)
        {
            if (fileRow == null)
            {
                throw new ArgumentNullException("fileRow");
            }

            retainRangeWarning = false;
            if (this.deltaBinaryPatch && RowOperation.Modify == fileRow.Operation)
            {
                if (0 != (PatchAttributeType.IncludeWholeFile | fileRow.PatchAttributes))
                {
                    string deltaBase = Common.GenerateIdentifier("dlt", true, Common.GenerateGuid());
                    string deltaFile = Path.Combine(this.tempFilesLocation, String.Concat(deltaBase, ".dpf"));
                    string headerFile = Path.Combine(this.tempFilesLocation, String.Concat(deltaBase, ".phd"));
                    PatchAPI.PatchInterop.PatchSymbolFlagsType apiPatchingSymbolFlags = 0;
                    bool optimizePatchSizeForLargeFiles = false;

                    Table wixPatchIdTable = this.output.Tables["WixPatchId"];
                    if (null != wixPatchIdTable)
                    {
                        Row row = wixPatchIdTable.Rows[0];
                        if (null != row)
                        {
                            if (null != row[2])
                            {
                                optimizePatchSizeForLargeFiles = (1 == Convert.ToUInt32(row[2], CultureInfo.InvariantCulture));
                            }
                            if (null != row[3])
                            {
                                apiPatchingSymbolFlags = (PatchAPI.PatchInterop.PatchSymbolFlagsType)Convert.ToUInt32(row[3], CultureInfo.InvariantCulture);
                            }
                        }
                    }

                    if (PatchAPI.PatchInterop.CreateDelta(
                            deltaFile,
                            fileRow.Source,
                            fileRow.Symbols,
                            fileRow.RetainOffsets,
                            fileRow.PreviousSourceArray,
                            fileRow.PreviousSymbolsArray,
                            fileRow.PreviousIgnoreLengthsArray,
                            fileRow.PreviousIgnoreOffsetsArray,
                            fileRow.PreviousRetainLengthsArray,
                            fileRow.PreviousRetainOffsetsArray,
                            apiPatchingSymbolFlags,
                            optimizePatchSizeForLargeFiles,
                            out retainRangeWarning))
                    {
                        PatchAPI.PatchInterop.ExtractDeltaHeader(deltaFile, headerFile);
                        fileRow.Patch = headerFile;
                        fileRow.Source = deltaFile;
                    }
                }
            }
        }

        /// <summary>
        /// Checks if a path exists, and throws a well known error for invalid paths.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <returns>True if path exists.</returns>
        private static bool CheckFileExists(string path)
        {
            try
            {
                return File.Exists(path);
            }
            catch (ArgumentException)
            {
                throw new WixException(WixErrors.IllegalCharactersInPath(path));
            }
        }

        [DllImport("Kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        private static extern bool CreateHardLink(string lpFileName, string lpExistingFileName, IntPtr lpSecurityAttributes);
    }
}
