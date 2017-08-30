// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// The finalize harvester mutator for the Windows Installer XML Toolset Utility Extension.
    /// </summary>
    public sealed class UtilFinalizeHarvesterMutator : MutatorExtension
    {
        private ArrayList components;
        private ArrayList directories;
        private SortedList directoryPaths;
        private Hashtable filePaths;
        private ArrayList files;
        private ArrayList registryValues;
        private bool suppressCOMElements;
        private bool suppressVB6COMElements;
        private string preprocessorVariable;

        /// <summary>
        /// Instantiate a new UtilFinalizeHarvesterMutator.
        /// </summary>
        public UtilFinalizeHarvesterMutator()
        {
            this.components = new ArrayList();
            this.directories = new ArrayList();
            this.directoryPaths = new SortedList();
            this.filePaths = new Hashtable();
            this.files = new ArrayList();
            this.registryValues = new ArrayList();
        }

        /// <summary>
        /// Gets or sets the preprocessor variable for substitution.
        /// </summary>
        /// <value>The preprocessor variable for substitution.</value>
        public string PreprocessorVariable
        {
            get { return this.preprocessorVariable; }
            set { this.preprocessorVariable = value; }
        }

        /// <summary>
        /// Gets the sequence of the extension.
        /// </summary>
        /// <value>The sequence of the extension.</value>
        public override int Sequence
        {
            get { return 2000; }
        }

        /// <summary>
        /// Gets or sets the option to suppress COM elements.
        /// </summary>
        /// <value>The option to suppress COM elements.</value>
        public bool SuppressCOMElements
        {
            get { return this.suppressCOMElements; }
            set { this.suppressCOMElements = value; }
        }

        /// <summary>
        /// Gets or sets the option to suppress VB6 COM elements.
        /// </summary>
        /// <value>The option to suppress VB6 COM elements.</value>
        public bool SuppressVB6COMElements
        {
            get { return this.suppressVB6COMElements; }
            set { this.suppressVB6COMElements = value; }
        }

        /// <summary>
        /// Mutate a WiX document.
        /// </summary>
        /// <param name="wix">The Wix document element.</param>
        public override void Mutate(Wix.Wix wix)
        {
            this.components.Clear();
            this.directories.Clear();
            this.directoryPaths.Clear();
            this.filePaths.Clear();
            this.files.Clear();
            this.registryValues.Clear();

            // index elements in this wix document
            this.IndexElement(wix);

            this.MutateDirectories();
            this.MutateFiles();
            this.MutateRegistryValues();

            // must occur after all the registry values have been formatted
            this.MutateComponents();
        }

        /// <summary>
        /// Index an element.
        /// </summary>
        /// <param name="element">The element to index.</param>
        private void IndexElement(Wix.ISchemaElement element)
        {
            if (element is Wix.Component)
            {
                // Component elements only need to be indexed if COM registry values will be strongly typed
                if (!this.suppressCOMElements)
                {
                    this.components.Add(element);
                }
            }
            else if (element is Wix.Directory)
            {
                this.directories.Add(element);
            }
            else if (element is Wix.File)
            {
                this.files.Add(element);
            }
            else if (element is Wix.RegistryValue)
            {
                this.registryValues.Add(element);
            }

            // index the child elements
            if (element is Wix.IParentElement)
            {
                foreach (Wix.ISchemaElement childElement in ((Wix.IParentElement)element).Children)
                {
                    this.IndexElement(childElement);
                }
            }
        }

        /// <summary>
        /// Mutate the components.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "These strings won't be round-tripped, and have no security impact.")]
        private void MutateComponents()
        {
            if (suppressVB6COMElements)
            {
                // Search for VB6 specific COM registrations
                foreach (Wix.Component component in this.components)
                {
                    ArrayList vb6RegistryValues = new ArrayList();

                    foreach (Wix.RegistryValue registryValue in component[typeof(Wix.RegistryValue)])
                    {
                        if (Wix.RegistryValue.ActionType.write == registryValue.Action && Wix.RegistryRootType.HKCR == registryValue.Root)
                        {
                            string[] parts = registryValue.Key.Split('\\');

                            if (String.Equals(parts[0], "CLSID", StringComparison.OrdinalIgnoreCase))
                            {
                                // Search for the VB6 CLSID {D5DE8D20-5BB8-11D1-A1E3-00A0C90F2731}
                                if (2 <= parts.Length)
                                {
                                    if (String.Equals(parts[1], "{D5DE8D20-5BB8-11D1-A1E3-00A0C90F2731}", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!vb6RegistryValues.Contains(registryValue))
                                        {
                                            vb6RegistryValues.Add(registryValue);
                                        }
                                    }
                                }
                            }
                            else if (String.Equals(parts[0], "TypeLib", StringComparison.OrdinalIgnoreCase))
                            {
                                // Search for the VB6 TypeLibs {EA544A21-C82D-11D1-A3E4-00A0C90AEA82} or {000204EF-0000-0000-C000-000000000046}
                                if (2 <= parts.Length)
                                {
                                    if (String.Equals(parts[1], "{EA544A21-C82D-11D1-A3E4-00A0C90AEA82}", StringComparison.OrdinalIgnoreCase) ||
                                        String.Equals(parts[1], "{000204EF-0000-0000-C000-000000000046}", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (!vb6RegistryValues.Contains(registryValue))
                                        {
                                            vb6RegistryValues.Add(registryValue);
                                        }
                                    }
                                }
                            }
                            else if (String.Equals(parts[0], "Interface", StringComparison.OrdinalIgnoreCase))
                            {
                                // Search for any Interfaces that reference the VB6 TypeLibs {EA544A21-C82D-11D1-A3E4-00A0C90AEA82} or {000204EF-0000-0000-C000-000000000046}
                                if (3 <= parts.Length)
                                {
                                    if (String.Equals(parts[2], "TypeLib", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (String.Equals(registryValue.Value, "{EA544A21-C82D-11D1-A3E4-00A0C90AEA82}", StringComparison.OrdinalIgnoreCase) ||
                                            String.Equals(registryValue.Value, "{000204EF-0000-0000-C000-000000000046}", StringComparison.OrdinalIgnoreCase))
                                        {
                                            // Having found a match we have to loop through again finding the matching Interface entries
                                            foreach (Wix.RegistryValue regValue in component[typeof(Wix.RegistryValue)])
                                            {
                                                if (Wix.RegistryValue.ActionType.write == regValue.Action && Wix.RegistryRootType.HKCR == regValue.Root)
                                                {
                                                    string[] rvparts = regValue.Key.Split('\\');
                                                    if (String.Equals(rvparts[0], "Interface", StringComparison.OrdinalIgnoreCase))
                                                    {
                                                        if (2 <= rvparts.Length)
                                                        {
                                                            if (String.Equals(rvparts[1], parts[1], StringComparison.OrdinalIgnoreCase))
                                                            {
                                                                if (!vb6RegistryValues.Contains(regValue))
                                                                {
                                                                    vb6RegistryValues.Add(regValue);
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }

                    // Remove all the VB6 specific COM registry values
                    foreach (Object entry in vb6RegistryValues)
                    {
                        component.RemoveChild((Wix.RegistryValue)entry);
                    }
                }
            }

            foreach (Wix.Component component in this.components)
            {
                SortedList indexedElements = CollectionsUtil.CreateCaseInsensitiveSortedList();
                SortedList indexedRegistryValues = CollectionsUtil.CreateCaseInsensitiveSortedList();
                List<Wix.RegistryValue> duplicateRegistryValues = new List<Wix.RegistryValue>();

                // index all the File elements
                foreach (Wix.File file in component[typeof(Wix.File)])
                {
                    indexedElements.Add(String.Concat("file/", file.Id), file);
                }

                // group all the registry values by the COM element they would correspond to and
                // create a COM element for each group
                foreach (Wix.RegistryValue registryValue in component[typeof(Wix.RegistryValue)])
                {
                    if (!String.IsNullOrEmpty(registryValue.Key) && Wix.RegistryValue.ActionType.write == registryValue.Action && Wix.RegistryRootType.HKCR == registryValue.Root && Wix.RegistryValue.TypeType.@string == registryValue.Type)
                    {
                        string index = null;
                        string[] parts = registryValue.Key.Split('\\');

                        // create a COM element for COM registration and index it
                        if (1 <= parts.Length)
                        {
                            if (String.Equals(parts[0], "AppID", StringComparison.OrdinalIgnoreCase))
                            {
                                // only work with GUID AppIds here
                                if (2 <= parts.Length && parts[1].StartsWith("{", StringComparison.Ordinal) && parts[1].EndsWith("}", StringComparison.Ordinal))
                                {
                                    index = String.Concat(parts[0], '/', parts[1]);

                                    if (!indexedElements.Contains(index))
                                    {
                                        Wix.AppId appId = new Wix.AppId();
                                        appId.Id = parts[1].ToUpper(CultureInfo.InvariantCulture);
                                        indexedElements.Add(index, appId);
                                    }
                                }
                            }
                            else if (String.Equals(parts[0], "CLSID", StringComparison.OrdinalIgnoreCase))
                            {
                                if (2 <= parts.Length)
                                {
                                    index = String.Concat(parts[0], '/', parts[1]);

                                    if (!indexedElements.Contains(index))
                                    {
                                        Wix.Class wixClass = new Wix.Class();
                                        wixClass.Id = parts[1].ToUpper(CultureInfo.InvariantCulture);
                                        indexedElements.Add(index, wixClass);
                                    }
                                }
                            }
                            else if (String.Equals(parts[0], "Component Categories", StringComparison.OrdinalIgnoreCase))
                            {
                                // If this is the .NET Component Category it should not end up in the authoring. Therefore, add
                                // the registry key to the duplicate list to ensure it gets removed later.
                                if (String.Equals(parts[1], "{62C8FE65-4EBB-45e7-B440-6E39B2CDBF29}", StringComparison.OrdinalIgnoreCase))
                                {
                                    duplicateRegistryValues.Add(registryValue);
                                }
                                else
                                {
                                    // TODO: add support for Component Categories to the compiler.
                                }
                            }
                            else if (String.Equals(parts[0], "Interface", StringComparison.OrdinalIgnoreCase))
                            {
                                if (2 <= parts.Length)
                                {
                                    index = String.Concat(parts[0], '/', parts[1]);

                                    if (!indexedElements.Contains(index))
                                    {
                                        Wix.Interface wixInterface = new Wix.Interface();
                                        wixInterface.Id = parts[1].ToUpper(CultureInfo.InvariantCulture);
                                        indexedElements.Add(index, wixInterface);
                                    }
                                }
                            }
                            else if (String.Equals(parts[0], "TypeLib", StringComparison.Ordinal))
                            {
                                if (3 <= parts.Length)
                                {
                                    // use a special index to ensure progIds are processed before classes
                                    index = String.Concat(".typelib/", parts[1], '/', parts[2]);

                                    if (!indexedElements.Contains(index))
                                    {
                                        Version version = TypeLibraryHarvester.ParseHexVersion(parts[2]);
                                        if (version != null)
                                        {
                                            Wix.TypeLib typeLib = new Wix.TypeLib();
                                            typeLib.Id = parts[1].ToUpper(CultureInfo.InvariantCulture);
                                            typeLib.MajorVersion = version.Major;
                                            typeLib.MinorVersion = version.Minor;
                                            indexedElements.Add(index, typeLib);
                                        }
                                        else // not a valid type library registry value
                                        {
                                            index = null;
                                        }
                                    }
                                }
                            }
                            else if (parts[0].StartsWith(".", StringComparison.Ordinal))
                            {
                                // extension
                            }
                            else // ProgId (hopefully)
                            {
                                // use a special index to ensure progIds are processed before classes
                                index = String.Concat(".progid/", parts[0]);

                                if (!indexedElements.Contains(index))
                                {
                                    Wix.ProgId progId = new Wix.ProgId();
                                    progId.Id = parts[0];
                                    indexedElements.Add(index, progId);
                                }
                            }
                        }

                        // index the RegistryValue element according to the COM element it corresponds to
                        if (null != index)
                        {
                            SortedList registryValues = (SortedList)indexedRegistryValues[index];

                            if (null == registryValues)
                            {
                                registryValues = CollectionsUtil.CreateCaseInsensitiveSortedList();
                                indexedRegistryValues.Add(index, registryValues);
                            }

                            try
                            {
                                registryValues.Add(String.Concat(registryValue.Key, '/', registryValue.Name), registryValue);
                            }
                            catch (ArgumentException)
                            {
                                duplicateRegistryValues.Add(registryValue);

                                if (String.IsNullOrEmpty(registryValue.Value))
                                {
                                    this.Core.OnMessage(UtilWarnings.DuplicateDllRegistryEntry(String.Concat(registryValue.Key, '/', registryValue.Name), component.Id));
                                }
                                else
                                {
                                    this.Core.OnMessage(UtilWarnings.DuplicateDllRegistryEntry(String.Concat(registryValue.Key, '/', registryValue.Name), registryValue.Value, component.Id));
                                }
                            }
                        }
                    }
                }

                foreach (Wix.RegistryValue removeRegistryEntry in duplicateRegistryValues)
                {
                    component.RemoveChild(removeRegistryEntry);
                }

                // set various values on the COM elements from their corresponding registry values
                Hashtable indexedProcessedRegistryValues = new Hashtable();
                foreach (DictionaryEntry entry in indexedRegistryValues)
                {
                    Wix.ISchemaElement element = (Wix.ISchemaElement)indexedElements[entry.Key];
                    string parentIndex = null;
                    SortedList registryValues = (SortedList)entry.Value;

                    // element-specific variables (for really tough situations)
                    string classAppId = null;
                    bool threadingModelSet = false;

                    foreach (Wix.RegistryValue registryValue in registryValues.Values)
                    {
                        string[] parts = registryValue.Key.ToLower(CultureInfo.InvariantCulture).Split('\\');
                        bool processed = false;

                        if (element is Wix.AppId)
                        {
                            Wix.AppId appId = (Wix.AppId)element;

                            if (2 == parts.Length)
                            {
                                if (null == registryValue.Name)
                                {
                                    appId.Description = registryValue.Value;
                                    processed = true;
                                }
                            }
                        }
                        else if (element is Wix.Class)
                        {
                            Wix.Class wixClass = (Wix.Class)element;

                            if (2 == parts.Length)
                            {
                                if (null == registryValue.Name)
                                {
                                    wixClass.Description = registryValue.Value;
                                    processed = true;
                                }
                                else if (String.Equals(registryValue.Name, "AppID", StringComparison.OrdinalIgnoreCase))
                                {
                                    classAppId = registryValue.Value;
                                    processed = true;
                                }
                            }
                            else if (3 == parts.Length)
                            {
                                Wix.Class.ContextType contextType = Wix.Class.ContextType.None;

                                switch (parts[2])
                                {
                                    case "control":
                                        wixClass.Control = Wix.YesNoType.yes;
                                        processed = true;
                                        break;
                                    case "inprochandler":
                                        if (null == registryValue.Name)
                                        {
                                            if (null == wixClass.Handler)
                                            {
                                                wixClass.Handler = "1";
                                                processed = true;
                                            }
                                            else if ("2" == wixClass.Handler)
                                            {
                                                wixClass.Handler = "3";
                                                processed = true;
                                            }
                                        }
                                        break;
                                    case "inprochandler32":
                                        if (null == registryValue.Name)
                                        {
                                            if (null == wixClass.Handler)
                                            {
                                                wixClass.Handler = "2";
                                                processed = true;
                                            }
                                            else if ("1" == wixClass.Handler)
                                            {
                                                wixClass.Handler = "3";
                                                processed = true;
                                            }
                                        }
                                        break;
                                    case "inprocserver":
                                        contextType = Wix.Class.ContextType.InprocServer;
                                        break;
                                    case "inprocserver32":
                                        contextType = Wix.Class.ContextType.InprocServer32;
                                        break;
                                    case "insertable":
                                        wixClass.Insertable = Wix.YesNoType.yes;
                                        processed = true;
                                        break;
                                    case "localserver":
                                        contextType = Wix.Class.ContextType.LocalServer;
                                        break;
                                    case "localserver32":
                                        contextType = Wix.Class.ContextType.LocalServer32;
                                        break;
                                    case "progid":
                                        if (null == registryValue.Name)
                                        {
                                            Wix.ProgId progId = (Wix.ProgId)indexedElements[String.Concat(".progid/", registryValue.Value)];

                                            // verify that the versioned ProgId appears under this Class element
                                            // if not, toss the entire element
                                            if (null == progId || wixClass != progId.ParentElement)
                                            {
                                                element = null;
                                            }
                                            else
                                            {
                                                processed = true;
                                            }
                                        }
                                        break;
                                    case "programmable":
                                        wixClass.Programmable = Wix.YesNoType.yes;
                                        processed = true;
                                        break;
                                    case "typelib":
                                        if (null == registryValue.Name)
                                        {
                                            foreach (DictionaryEntry indexedEntry in indexedElements)
                                            {
                                                string key = (string)indexedEntry.Key;
                                                Wix.ISchemaElement possibleTypeLib = (Wix.ISchemaElement)indexedEntry.Value;

                                                if (key.StartsWith(".typelib/", StringComparison.Ordinal) &&
                                                    0 == String.Compare(key, 9, registryValue.Value, 0, registryValue.Value.Length, StringComparison.OrdinalIgnoreCase))
                                                {
                                                    // ensure the TypeLib is nested under the same thing we want the Class under
                                                    if (null == parentIndex || indexedElements[parentIndex] == possibleTypeLib.ParentElement)
                                                    {
                                                        parentIndex = key;
                                                        processed = true;
                                                    }
                                                }
                                            }
                                        }
                                        break;
                                    case "version":
                                        if (null == registryValue.Name)
                                        {
                                            wixClass.Version = registryValue.Value;
                                            processed = true;
                                        }
                                        break;
                                    case "versionindependentprogid":
                                        if (null == registryValue.Name)
                                        {
                                            Wix.ProgId progId = (Wix.ProgId)indexedElements[String.Concat(".progid/", registryValue.Value)];

                                            // verify that the version independent ProgId appears somewhere
                                            // under this Class element - if not, toss the entire element
                                            if (null == progId || wixClass != progId.ParentElement)
                                            {
                                                // check the parent of the parent
                                                if (null == progId || null == progId.ParentElement || wixClass != progId.ParentElement.ParentElement)
                                                {
                                                    element = null;
                                                }
                                            }

                                            processed = true;
                                        }
                                        break;
                                }

                                if (Wix.Class.ContextType.None != contextType)
                                {
                                    wixClass.Context |= contextType;

                                    if (null == registryValue.Name)
                                    {
                                        if ((registryValue.Value.StartsWith("[!", StringComparison.Ordinal) || registryValue.Value.StartsWith("[#", StringComparison.Ordinal))
                                            && registryValue.Value.EndsWith("]", StringComparison.Ordinal))
                                        {
                                            parentIndex = String.Concat("file/", registryValue.Value.Substring(2, registryValue.Value.Length - 3));
                                            processed = true;
                                        }
                                        else if (String.Equals(Path.GetFileName(registryValue.Value), "mscoree.dll", StringComparison.OrdinalIgnoreCase))
                                        {
                                            wixClass.ForeignServer = "mscoree.dll";
                                            processed = true;
                                        }
                                        else if (String.Equals(Path.GetFileName(registryValue.Value), "msvbvm60.dll", StringComparison.OrdinalIgnoreCase))
                                        {
                                            wixClass.ForeignServer = "msvbvm60.dll";
                                            processed = true;
                                        }
                                        else
                                        {
                                            // Some servers are specifying relative paths (which the above code doesn't find)
                                            // If there's any ambiguity leave it alone and let the developer figure it out when it breaks in the compiler

                                            bool possibleDuplicate = false;
                                            string possibleParentIndex = null;

                                            foreach (Wix.File file in this.files)
                                            {
                                                if (String.Equals(registryValue.Value, Path.GetFileName(file.Source), StringComparison.OrdinalIgnoreCase))
                                                {
                                                    if (null == possibleParentIndex)
                                                    {
                                                        possibleParentIndex = String.Concat("file/", file.Id);
                                                    }
                                                    else
                                                    {
                                                        possibleDuplicate = true;
                                                        break;
                                                    }
                                                }
                                            }

                                            if (!possibleDuplicate)
                                            {
                                                if (null == possibleParentIndex)
                                                {
                                                    wixClass.ForeignServer = registryValue.Value;
                                                    processed = true;
                                                }
                                                else
                                                {
                                                    parentIndex = possibleParentIndex;
                                                    wixClass.RelativePath = Microsoft.Tools.WindowsInstallerXml.Serialize.YesNoType.yes;
                                                    processed = true;
                                                }
                                            }
                                        }
                                    }
                                    else if (String.Equals(registryValue.Name, "ThreadingModel", StringComparison.OrdinalIgnoreCase))
                                    {
                                        Wix.Class.ThreadingModelType threadingModel;

                                        if (String.Equals(registryValue.Value, "apartment", StringComparison.OrdinalIgnoreCase))
                                        {
                                            threadingModel = Wix.Class.ThreadingModelType.apartment;
                                            processed = true;
                                        }
                                        else if (String.Equals(registryValue.Value, "both", StringComparison.OrdinalIgnoreCase))
                                        {
                                            threadingModel = Wix.Class.ThreadingModelType.both;
                                            processed = true;
                                        }
                                        else if (String.Equals(registryValue.Value, "free", StringComparison.OrdinalIgnoreCase))
                                        {
                                            threadingModel = Wix.Class.ThreadingModelType.free;
                                            processed = true;
                                        }
                                        else if (String.Equals(registryValue.Value, "neutral", StringComparison.OrdinalIgnoreCase))
                                        {
                                            threadingModel = Wix.Class.ThreadingModelType.neutral;
                                            processed = true;
                                        }
                                        else if (String.Equals(registryValue.Value, "rental", StringComparison.OrdinalIgnoreCase))
                                        {
                                            threadingModel = Wix.Class.ThreadingModelType.rental;
                                            processed = true;
                                        }
                                        else if (String.Equals(registryValue.Value, "single", StringComparison.OrdinalIgnoreCase))
                                        {
                                            threadingModel = Wix.Class.ThreadingModelType.single;
                                            processed = true;
                                        }
                                        else
                                        {
                                            continue;
                                        }

                                        if (!threadingModelSet || wixClass.ThreadingModel == threadingModel)
                                        {
                                            wixClass.ThreadingModel = threadingModel;
                                            threadingModelSet = true;
                                        }
                                        else
                                        {
                                            element = null;
                                            break;
                                        }
                                    }
                                }
                            }
                            else if (4 == parts.Length)
                            {
                                if (String.Equals(parts[2], "implemented categories", StringComparison.Ordinal))
                                {
                                    switch (parts[3])
                                    {
                                        case "{7dd95801-9882-11cf-9fa9-00aa006c42c4}":
                                            wixClass.SafeForScripting = Wix.YesNoType.yes;
                                            processed = true;
                                            break;
                                        case "{7dd95802-9882-11cf-9fa9-00aa006c42c4}":
                                            wixClass.SafeForInitializing = Wix.YesNoType.yes;
                                            processed = true;
                                            break;
                                    }
                                }
                            }
                        }
                        else if (element is Wix.Interface)
                        {
                            Wix.Interface wixInterface = (Wix.Interface)element;

                            if (2 == parts.Length && null == registryValue.Name)
                            {
                                wixInterface.Name = registryValue.Value;
                                processed = true;
                            }
                            else if (3 == parts.Length)
                            {
                                switch (parts[2])
                                {
                                    case "proxystubclsid":
                                        if (null == registryValue.Name)
                                        {
                                            wixInterface.ProxyStubClassId = registryValue.Value.ToUpper(CultureInfo.InvariantCulture);
                                            processed = true;
                                        }
                                        break;
                                    case "proxystubclsid32":
                                        if (null == registryValue.Name)
                                        {
                                            wixInterface.ProxyStubClassId32 = registryValue.Value.ToUpper(CultureInfo.InvariantCulture);
                                            processed = true;
                                        }
                                        break;
                                    case "nummethods":
                                        if (null == registryValue.Name)
                                        {
                                            wixInterface.NumMethods = Convert.ToInt32(registryValue.Value, CultureInfo.InvariantCulture);
                                            processed = true;
                                        }
                                        break;
                                    case "typelib":
                                        if (String.Equals("Version", registryValue.Name, StringComparison.OrdinalIgnoreCase))
                                        {
                                            parentIndex = String.Concat(parentIndex, registryValue.Value);
                                            processed = true;
                                        }
                                        else if (null == registryValue.Name) // TypeLib guid
                                        {
                                            parentIndex = String.Concat(".typelib/", registryValue.Value, '/', parentIndex);
                                            processed = true;
                                        }
                                        break;
                                }
                            }
                        }
                        else if (element is Wix.ProgId)
                        {
                            Wix.ProgId progId = (Wix.ProgId)element;

                            if (null == registryValue.Name)
                            {
                                if (1 == parts.Length)
                                {
                                    progId.Description = registryValue.Value;
                                    processed = true;
                                }
                                else if (2 == parts.Length)
                                {
                                    if (String.Equals(parts[1], "CLSID", StringComparison.OrdinalIgnoreCase))
                                    {
                                        parentIndex = String.Concat("CLSID/", registryValue.Value);
                                        processed = true;
                                    }
                                    else if (String.Equals(parts[1], "CurVer", StringComparison.OrdinalIgnoreCase))
                                    {
                                        // If a progId points to its own ProgId with CurVer, it isn't meaningful, so ignore it
                                        if (!String.Equals(progId.Id, registryValue.Value, StringComparison.OrdinalIgnoreCase))
                                        {
                                            // this registry value should usually be processed second so the
                                            // version independent ProgId should be under the versioned one
                                            parentIndex = String.Concat(".progid/", registryValue.Value);
                                            processed = true;
                                        }
                                    }
                                }
                            }
                        }
                        else if (element is Wix.TypeLib)
                        {
                            Wix.TypeLib typeLib = (Wix.TypeLib)element;

                            if (null == registryValue.Name)
                            {
                                if (3 == parts.Length)
                                {
                                    typeLib.Description = registryValue.Value;
                                    processed = true;
                                }
                                else if (4 == parts.Length)
                                {
                                    if (String.Equals(parts[3], "flags", StringComparison.OrdinalIgnoreCase))
                                    {
                                        int flags = Convert.ToInt32(registryValue.Value, CultureInfo.InvariantCulture);

                                        if (0x1 == (flags & 0x1))
                                        {
                                            typeLib.Restricted = Wix.YesNoType.yes;
                                        }

                                        if (0x2 == (flags & 0x2))
                                        {
                                            typeLib.Control = Wix.YesNoType.yes;
                                        }

                                        if (0x4 == (flags & 0x4))
                                        {
                                            typeLib.Hidden = Wix.YesNoType.yes;
                                        }

                                        if (0x8 == (flags & 0x8))
                                        {
                                            typeLib.HasDiskImage = Wix.YesNoType.yes;
                                        }

                                        processed = true;
                                    }
                                    else if (String.Equals(parts[3], "helpdir", StringComparison.OrdinalIgnoreCase))
                                    {
                                        if (registryValue.Value.StartsWith("[", StringComparison.Ordinal) && (registryValue.Value.EndsWith("]", StringComparison.Ordinal)
                                            || registryValue.Value.EndsWith("]\\", StringComparison.Ordinal)))
                                        {
                                            typeLib.HelpDirectory = registryValue.Value.Substring(1, registryValue.Value.LastIndexOf(']') - 1);
                                        }
                                        else if (0 == String.Compare(registryValue.Value, Environment.SystemDirectory, StringComparison.OrdinalIgnoreCase)) // VB6 DLLs register their help directory as SystemFolder
                                        {
                                            typeLib.HelpDirectory = "SystemFolder";
                                        }
                                        else if (null != component.Directory) // -sfrag has not been specified
                                        {
                                            typeLib.HelpDirectory = component.Directory;
                                        }
                                        else if (component.ParentElement is Wix.Directory) // -sfrag has been specified
                                        {
                                            typeLib.HelpDirectory = ((Wix.Directory)component.ParentElement).Id;
                                        }
                                        else if (component.ParentElement is Wix.DirectoryRef) // -sfrag has been specified
                                        {
                                            typeLib.HelpDirectory = ((Wix.DirectoryRef)component.ParentElement).Id;
                                        }

                                        //If the helpdir has not matched a known directory, drop it because it cannot be resolved.
                                        processed = true;
                                    }
                                }
                                else if (5 == parts.Length && String.Equals("win32", parts[4], StringComparison.OrdinalIgnoreCase))
                                {
                                    typeLib.Language = Convert.ToInt32(parts[3], CultureInfo.InvariantCulture);

                                    if ((registryValue.Value.StartsWith("[!", StringComparison.Ordinal) || registryValue.Value.StartsWith("[#", StringComparison.Ordinal))
                                        && registryValue.Value.EndsWith("]", StringComparison.Ordinal))
                                    {
                                        parentIndex = String.Concat("file/", registryValue.Value.Substring(2, registryValue.Value.Length - 3));
                                    }

                                    processed = true;
                                }
                            }
                        }

                        // index the processed registry values by their corresponding COM element
                        if (processed)
                        {
                            indexedProcessedRegistryValues.Add(registryValue, element);
                        }
                    }

                    // parent the COM element
                    if (null != element)
                    {
                        if (null != parentIndex)
                        {
                            Wix.IParentElement parentElement = (Wix.IParentElement)indexedElements[parentIndex];

                            if (null != parentElement)
                            {
                                parentElement.AddChild(element);
                            }
                        }
                        else if (0 < indexedProcessedRegistryValues.Count)
                        {
                            component.AddChild(element);
                        }

                        // special handling for AppID since it doesn't fit the general model
                        if (null != classAppId)
                        {
                            Wix.AppId appId = (Wix.AppId)indexedElements[String.Concat("AppID/", classAppId)];

                            // move the Class element under the AppId (and put the AppId under its old parent)
                            if (null != appId)
                            {
                                // move the AppId element
                                ((Wix.IParentElement)appId.ParentElement).RemoveChild(appId);
                                ((Wix.IParentElement)element.ParentElement).AddChild(appId);

                                // move the Class element
                                ((Wix.IParentElement)element.ParentElement).RemoveChild(element);
                                appId.AddChild(element);
                            }
                        }
                    }
                }

                // remove the RegistryValue elements which were converted into COM elements
                // that were successfully nested under the Component element
                foreach (DictionaryEntry entry in indexedProcessedRegistryValues)
                {
                    Wix.ISchemaElement element = (Wix.ISchemaElement)entry.Value;
                    Wix.RegistryValue registryValue = (Wix.RegistryValue)entry.Key;

                    while (null != element)
                    {
                        if (element == component)
                        {
                            ((Wix.IParentElement)registryValue.ParentElement).RemoveChild(registryValue);
                            break;
                        }

                        element = element.ParentElement;
                    }
                }
            }
        }

        /// <summary>
        /// Mutate the directories.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "These strings won't be round-tripped, and have no security impact.")]
        private void MutateDirectories()
        {
            foreach (Wix.Directory directory in this.directories)
            {
                string path = directory.FileSource;

                // create a new directory element without the FileSource attribute
                if (null != path)
                {
                    Wix.Directory newDirectory = new Wix.Directory();

                    newDirectory.Id = directory.Id;
                    newDirectory.Name = directory.Name;

                    foreach (Wix.ISchemaElement element in directory.Children)
                    {
                        newDirectory.AddChild(element);
                    }

                    ((Wix.IParentElement)directory.ParentElement).AddChild(newDirectory);
                    ((Wix.IParentElement)directory.ParentElement).RemoveChild(directory);

                    if (null != newDirectory.Id)
                    {
                        this.directoryPaths[path.ToLower(CultureInfo.InvariantCulture)] = String.Concat("[", newDirectory.Id, "]");
                    }
                }
            }
        }

        /// <summary>
        /// Mutate the files.
        /// </summary>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "These strings won't be round-tripped, and have no security impact.")]
        private void MutateFiles()
        {
            string sourceDirSubstitution = this.preprocessorVariable;
            if (sourceDirSubstitution != null)
            {
                string prefix = "$(";
                if (sourceDirSubstitution.StartsWith("wix.", StringComparison.Ordinal))
                {
                    prefix = "!(";
                }
                sourceDirSubstitution = String.Concat(prefix, sourceDirSubstitution, ")");
            }

            foreach (Wix.File file in this.files)
            {
                if (null != file.Id && null != file.Source)
                {
                    string fileSource = this.Core.ResolveFilePath(file.Source);

                    // index the long path
                    this.filePaths[fileSource.ToLower(CultureInfo.InvariantCulture)] = String.Concat("[#", file.Id, "]");

                    // index the long path as a URL for assembly harvesting
                    Uri fileUri = new Uri(fileSource);
                    this.filePaths[fileUri.ToString().ToLower(CultureInfo.InvariantCulture)] = String.Concat("file:///[#", file.Id, "]");

                    // index the short path
                    string shortPath = NativeMethods.GetShortPathName(fileSource);
                    this.filePaths[shortPath.ToLower(CultureInfo.InvariantCulture)] = String.Concat("[!", file.Id, "]");

                    // escape literal $ characters
                    file.Source = file.Source.Replace("$", "$$");
                    
                    if (null != sourceDirSubstitution && file.Source.StartsWith("SourceDir\\", StringComparison.Ordinal))
                    {
                        file.Source = file.Source.Substring(9).Insert(0, sourceDirSubstitution);
                    }
                }
            }
        }

        /// <summary>
        /// Mutate an individual registry string, according to a collection of replacement items.
        /// </summary>
        /// <param name="value">The string to mutate.</param>
        /// <param name="replace">The collection of items to replace within the string.</param>
        /// <value>The mutated registry string.</value>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "These strings won't be round-tripped, and have no security impact.")]
        private string MutateRegistryString(string value, ICollection replace)
        {
            int index;
            string lowercaseValue = value.ToLower(CultureInfo.InvariantCulture);

            foreach (DictionaryEntry entry in replace)
            {
                while (0 <= (index = lowercaseValue.IndexOf((string)entry.Key, StringComparison.Ordinal)))
                {
                    value = value.Remove(index, ((string)entry.Key).Length);
                    value = value.Insert(index, (string)entry.Value);
                    lowercaseValue = value.ToLower(CultureInfo.InvariantCulture);
                }
            }

            return value;
        }

        /// <summary>
        /// Mutate the registry values.
        /// </summary>
        private void MutateRegistryValues()
        {
            ArrayList reversedDirectoryPaths = new ArrayList();

            // reverse the indexed directory paths to ensure the longest paths are found first
            foreach (DictionaryEntry entry in this.directoryPaths)
            {
                reversedDirectoryPaths.Insert(0, entry);
            }

            foreach (Wix.RegistryValue registryValue in this.registryValues)
            {
                // Multi-string values are stored as children - their "Value" member is null
                if (Wix.RegistryValue.TypeType.multiString == registryValue.Type)
                {
                    foreach (Wix.MultiStringValue multiStringValue in registryValue.Children)
                    {
                        // first replace file paths with their MSI tokens
                        multiStringValue.Content = MutateRegistryString(multiStringValue.Content, (ICollection)this.filePaths);
                        // next replace directory paths with their MSI tokens
                        multiStringValue.Content = MutateRegistryString(multiStringValue.Content, (ICollection)reversedDirectoryPaths);
                    }
                }
                else
                {
                    // first replace file paths with their MSI tokens
                    registryValue.Value = MutateRegistryString(registryValue.Value, (ICollection)this.filePaths);
                    // next replace directory paths with their MSI tokens
                    registryValue.Value = MutateRegistryString(registryValue.Value, (ICollection)reversedDirectoryPaths);
                }
            }
        }

        /// <summary>
        /// The native methods for grabbing machine-specific short file paths.
        /// </summary>
        private class NativeMethods
        {

            /// <summary>
            /// Gets the short name for a file.
            /// </summary>
            /// <param name="fullPath">Fullpath to file on disk.</param>
            /// <returns>Short name for file.</returns>
            internal static string GetShortPathName(string fullPath)
            {
                StringBuilder shortPath = new StringBuilder();

                uint result = GetShortPathName(fullPath, null, 0);

                if (0 == result)
                {
                    int err = System.Runtime.InteropServices.Marshal.GetLastWin32Error();
                    throw new System.Runtime.InteropServices.COMException(String.Concat("Failed to get short path name for file: ", fullPath), err);
                }

                return shortPath.ToString();
            }

            /// <summary>
            /// Gets the short name for a file.
            /// </summary>
            /// <param name="longPath">Long path to convert to short path.</param>
            /// <param name="shortPath">Short path from long path.</param>
            /// <param name="buffer">Size of short path.</param>
            /// <returns>zero if success.</returns>
            [DllImport("kernel32.dll", EntryPoint = "GetShortPathNameW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            internal static extern uint GetShortPathName(string longPath, StringBuilder shortPath, [MarshalAs(UnmanagedType.U4)]int buffer);
        }
    }
}
