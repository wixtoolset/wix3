// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Build.Tasks
{
    using System;
    using System.Collections.Generic;
    using Microsoft.Build.Utilities;
    using Microsoft.Build.Framework;
    using System.IO;

    /// <summary>
    /// This task searches for paths to references using the order specified in SearchPaths.
    /// </summary>
    public class ResolveWixReferences : Task
    {
        /// <summary>
        /// Token value used in SearchPaths to indicate that the item's HintPath metadata should
        /// be searched as a full file path to resolve the reference.  
        /// Must match wix.targets, case sensitive.
        /// </summary>
        private const string HintPathToken = "{HintPathFromItem}";

        /// <summary>
        /// Token value used in SearchPaths to indicate that the item's Identity should
        /// be searched as a full file path to resolve the reference.  
        /// Must match wix.targets, case sensitive.
        /// </summary>
        private const string RawFileNameToken = "{RawFileName}";

        /// <summary>
        /// The list of references to resolve.
        /// </summary>
        [Required]
        public ITaskItem[] WixReferences
        {
            get;
            set;
        }

        /// <summary>
        /// The directories or special locations that are searched to find the files 
        /// on disk that represent the references. The order in which the search paths are listed 
        /// is important. For each reference, the list of paths is searched from left to right. 
        /// When a file that represents the reference is found, that search stops and the search 
        /// for the next reference starts. 
        /// 
        /// This parameter accepts the following types of values: 
        ///     A directory path. 
        ///     {HintPathFromItem}: Specifies that the task will examine the HintPath metadata 
        ///                         of the base item. 
        ///     TODO : {CandidateAssemblyFiles}: Specifies that the task will examine the files 
        ///                                      passed in through the CandidateAssemblyFiles parameter. 
        ///     TODO : {Registry:_AssemblyFoldersBase_, _RuntimeVersion_, _AssemblyFoldersSuffix_}: 
        ///     TODO : {AssemblyFolders}: Specifies the task will use the Visual Studio.NET 2003 
        ///                               finding-assemblies-from-registry scheme. 
        ///     TODO : {GAC}: Specifies the task will search in the GAC. 
        ///     {RawFileName}: Specifies the task will consider the Include value of the item to be 
        ///                    an exact path and file name. 
        /// </summary>
        public string[] SearchPaths
        {
            get;
            set;
        }

        /// <summary>
        /// The filename extension(s) to be checked when searching.
        /// </summary>
        public string[] SearchFilenameExtensions
        {
            get;
            set;
        }

        /// <summary>
        /// Output items that contain the same metadata as input references and have been resolved to full paths.
        /// </summary>
        [Output]
        public ITaskItem[] ResolvedWixReferences
        {
            get;
            private set;
        }

        /// <summary>
        /// Resolves reference paths by searching for referenced items using the specified SearchPaths.
        /// </summary>
        /// <returns>True on success, or throws an exception on failure.</returns>
        public override bool Execute()
        {
            List<ITaskItem> resolvedReferences = new List<ITaskItem>();

            foreach (ITaskItem reference in this.WixReferences)
            {
                ITaskItem resolvedReference = ResolveWixReferences.ResolveReference(reference, this.SearchPaths, this.SearchFilenameExtensions, this.Log);

                this.Log.LogMessage(MessageImportance.Low, "Resolved path {0}", resolvedReference.ItemSpec);
                resolvedReferences.Add(resolvedReference);
            }

            this.ResolvedWixReferences = resolvedReferences.ToArray();
            return true;
        }

        /// <summary>
        /// Resolves a single reference item by searcheing for referenced items using the specified SearchPaths.
        /// This method is made public so the resolution logic can be reused by other tasks.
        /// </summary>
        /// <param name="reference">The referenced item.</param>
        /// <param name="searchPaths">The paths to search.</param>
        /// <param name="searchFilenameExtensions">Filename extensions to check.</param>
        /// <param name="log">Logging helper.</param>
        /// <returns>The resolved reference item, or the original reference if it could not be resolved.</returns>
        public static ITaskItem ResolveReference(ITaskItem reference, string[] searchPaths, string[] searchFilenameExtensions, TaskLoggingHelper log)
        {
            if (reference == null)
            {
                throw new ArgumentNullException("reference");
            }

            if (searchPaths == null)
            {
                // Nothing to search, so just return the original reference item.
                return reference;
            }

            if (searchFilenameExtensions == null)
            {
                searchFilenameExtensions = new string[] { };
            }

            // Copy all the metadata from the source
            TaskItem resolvedReference = new TaskItem(reference);
            log.LogMessage(MessageImportance.Low, "WixReference: {0}", reference.ItemSpec);

            // Now find the resolved path based on our order of precedence
            foreach (string searchPath in searchPaths)
            {
                log.LogMessage(MessageImportance.Low, "Trying {0}", searchPath);
                if (searchPath.Equals(HintPathToken, StringComparison.Ordinal))
                {
                    string path = reference.GetMetadata("HintPath");
                    log.LogMessage(MessageImportance.Low, "Trying path {0}", path);
                    if (File.Exists(path))
                    {
                        resolvedReference.ItemSpec = path;
                        break;
                    }
                }
                else if (searchPath.Equals(RawFileNameToken, StringComparison.Ordinal))
                {
                    log.LogMessage(MessageImportance.Low, "Trying path {0}", resolvedReference.ItemSpec);
                    if (File.Exists(resolvedReference.ItemSpec))
                    {
                        break;
                    }

                    if (ResolveWixReferences.ResolveFilenameExtensions(resolvedReference,
                        resolvedReference.ItemSpec, searchFilenameExtensions, log))
                    {
                        break;
                    }
                }
                else
                {
                    string path = Path.Combine(searchPath, Path.GetFileName(reference.ItemSpec));
                    log.LogMessage(MessageImportance.Low, "Trying path {0}", path);
                    if (File.Exists(path))
                    {
                        resolvedReference.ItemSpec = path;
                        break;
                    }

                    if (ResolveWixReferences.ResolveFilenameExtensions(resolvedReference,
                        path, searchFilenameExtensions, log))
                    {
                        break;
                    }
                }
            }

            // Normalize the item path
            resolvedReference.ItemSpec = resolvedReference.GetMetadata("FullPath");

            return resolvedReference;
        }

        /// <summary>
        /// Helper method for checking filename extensions when resolving references.
        /// </summary>
        /// <param name="reference">The reference being resolved.</param>
        /// <param name="basePath">Full filename path without extension.</param>
        /// <param name="filenameExtensions">Filename extensions to check.</param>
        /// <param name="log">Logging helper.</param>
        /// <returns>True if the item was resolved, else false.</returns>
        private static bool ResolveFilenameExtensions(ITaskItem reference, string basePath, string[] filenameExtensions, TaskLoggingHelper log)
        {
            foreach (string filenameExtension in filenameExtensions)
            {
                string path = basePath + filenameExtension;
                log.LogMessage(MessageImportance.Low, "Trying path {0}", path);
                if (File.Exists(path))
                {
                    reference.ItemSpec = path;
                    return true;
                }
            }

            return false;
        }
    }
}
