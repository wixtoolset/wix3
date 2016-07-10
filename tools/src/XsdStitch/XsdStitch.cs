// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstaller.Tools
{
    using System;
    using System.Collections;
    using System.Collections.Specialized;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Xml.Schema;

    /// <summary>
    /// Stitch together a main schema and several extension schemas using
    /// reference information provided by the XmlSchemaExtension.
    /// </summary>
    public sealed class XsdStitch
    {
        private const string XmlSchemaExtensionNamespace = "http://schemas.microsoft.com/wix/2005/XmlSchemaExtension";

        private StringCollection extensionSchemaFiles;
        private string mainSchemaFile;
        private string outputFile;
        private bool showLogo;
        private bool showHelp;
        private Hashtable anys = new Hashtable();
        private ArrayList anyAttributeElements = new ArrayList();

        /// <summary>
        /// Instantiate a new XsdStich class.
        /// </summary>
        private XsdStitch()
        {
            this.extensionSchemaFiles = new StringCollection();
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        [STAThread]
        public static int Main(string[] args)
        {
            XsdStitch xsdStich = new XsdStitch();
            return xsdStich.Run(args);
        }

        /// <summary>
        /// Run the application with the given arguments.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        /// <returns>The number of errors that were found.</returns>
        private int Run(string[] args)
        {
            XmlSchema mainSchema = null;
            XmlSchemaCollection schemas = new XmlSchemaCollection();

            try
            {
                this.ParseCommandLine(args);

                if (this.showLogo)
                {
                    Assembly thisAssembly = Assembly.GetExecutingAssembly();

                    Console.WriteLine("Microsoft (R) Windows Installer Xsd Stitch version {0}", thisAssembly.GetName().Version.ToString());
                    Console.WriteLine("Copyright (C) Microsoft Corporation 2006. All rights reserved.");
                    Console.WriteLine();
                }

                if (this.showHelp)
                {
                    Console.WriteLine(" usage:  xsdStitch.exe mainSchema.xsd stitched.xsd");
                    Console.WriteLine();
                    Console.WriteLine("   -ext extSchema.xsd  adds an extension schema to the main schema");
                    Console.WriteLine("   -nologo             suppress displaying the logo information");
                    Console.WriteLine("   -?                  this help information");
                    Console.WriteLine();

                    return 0;
                }

                XmlTextReader mainSchemaReader = null;

                // load the main schema
                try
                {
                    mainSchemaReader = new XmlTextReader(this.mainSchemaFile);
                    mainSchema = XmlSchema.Read(mainSchemaReader, null);

                    schemas.Add(mainSchema);
                }
                finally
                {
                    if (null != mainSchemaReader)
                    {
                        mainSchemaReader.Close();
                    }
                }

                StringCollection addedSchemas = new StringCollection();

                // load the extension schemas
                foreach (string extensionSchemaFile in this.extensionSchemaFiles)
                {
                    XmlTextReader reader = null;
                    try
                    {
                        string schemaFilename = Path.GetFileNameWithoutExtension(extensionSchemaFile);
                        if (addedSchemas.Contains(schemaFilename))
                        {
                            int duplicateNameCounter = 2;

                            while (addedSchemas.Contains(String.Concat(schemaFilename,duplicateNameCounter)))
                            {
                                duplicateNameCounter++;
                            }

                            schemaFilename = String.Concat(schemaFilename, duplicateNameCounter);
                         }

                        addedSchemas.Add(schemaFilename);
                        reader = new XmlTextReader(extensionSchemaFile);
                        XmlSchema extensionSchema = XmlSchema.Read(reader, null);
                        mainSchema.Namespaces.Add(schemaFilename, extensionSchema.TargetNamespace);
                        schemas.Add(extensionSchema);

                        // add an import for the extension schema
                        XmlSchemaImport import = new XmlSchemaImport();
                        import.Namespace = extensionSchema.TargetNamespace;
                        import.SchemaLocation = Path.GetFileName(extensionSchemaFile);
                        mainSchema.Includes.Add(import);
                    }
                    finally
                    {
                        if (null != reader)
                        {
                            reader.Close();
                        }
                    }
                }

                foreach (XmlSchema schema in schemas)
                {
                    if (schema != mainSchema)
                    {
                        foreach (XmlSchemaElement element in schema.Elements.Values)
                        {
                            // retrieve explicitly-specified parent elements
                            if (element.Annotation != null)
                            {
                                foreach (XmlSchemaObject obj in element.Annotation.Items)
                                {
                                    XmlSchemaAppInfo appInfo = obj as XmlSchemaAppInfo;

                                    if (appInfo != null)
                                    {
                                        foreach (XmlNode node in appInfo.Markup)
                                        {
                                            XmlElement markupElement = node as XmlElement;

                                            if (markupElement != null && markupElement.LocalName == "parent" && markupElement.NamespaceURI == XmlSchemaExtensionNamespace)
                                            {
                                                string parentNamespace = markupElement.GetAttribute("namespace");
                                                string parentRef = markupElement.GetAttribute("ref");

                                                if (parentNamespace.Length == 0)
                                                {
                                                    throw new ApplicationException("The parent element is missing the namespace attribute.");
                                                }

                                                if (parentRef.Length == 0)
                                                {
                                                    throw new ApplicationException("The parent element is missing the ref attribute.");
                                                }

                                                XmlQualifiedName parentQualifiedName = new XmlQualifiedName(parentRef, parentNamespace);

                                                XmlSchemaElement parentElement = (XmlSchemaElement)mainSchema.Elements[parentQualifiedName];

                                                XmlSchemaComplexType complexType = (XmlSchemaComplexType)parentElement.ElementType;
                                                if (complexType.Particle != null)
                                                {
                                                    XmlSchemaElement elementRef = new XmlSchemaElement();
                                                    elementRef.RefName = element.QualifiedName;

                                                    this.InsertElement(complexType.Particle, elementRef);
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }

                        foreach (XmlSchemaAttribute attribute in schema.Attributes.Values)
                        {
                            if (attribute.Annotation != null)
                            {
                                foreach (XmlSchemaObject obj in attribute.Annotation.Items)
                                {
                                    XmlSchemaAppInfo appInfo = obj as XmlSchemaAppInfo;

                                    if (appInfo != null)
                                    {
                                        foreach (XmlNode node in appInfo.Markup)
                                        {
                                            XmlElement markupElement = node as XmlElement;

                                            if (markupElement != null && markupElement.LocalName == "parent" && markupElement.NamespaceURI == XmlSchemaExtensionNamespace)
                                            {
                                                string parentNamespace = markupElement.GetAttribute("namespace");
                                                string parentRef = markupElement.GetAttribute("ref");

                                                if (parentNamespace.Length == 0)
                                                {
                                                    throw new ApplicationException("The parent element is missing the namespace attribute.");
                                                }

                                                if (parentRef.Length == 0)
                                                {
                                                    throw new ApplicationException("The parent element is missing the ref attribute.");
                                                }

                                                XmlQualifiedName parentQualifiedName = new XmlQualifiedName(parentRef, parentNamespace);

                                                XmlSchemaElement parentElement = (XmlSchemaElement)mainSchema.Elements[parentQualifiedName];

                                                XmlSchemaComplexType complexType = (XmlSchemaComplexType)parentElement.ElementType;
                                                if (complexType.Particle != null)
                                                {
                                                    XmlSchemaAttribute attributeRef = new XmlSchemaAttribute();
                                                    attributeRef.RefName = attribute.QualifiedName;

                                                    anyAttributeElements.Add(complexType);
                                                    complexType.Attributes.Add(attribute);
                                                }
                                            }
                                        }
                                    }
                                }
                            }

                        }
                    }
                }

                // remove the any items
                foreach (DictionaryEntry entry in this.anys)
                {
                    XmlSchemaAny any = (XmlSchemaAny)entry.Key;
                    XmlSchemaParticle particle = (XmlSchemaParticle)entry.Value;

                    if (particle is XmlSchemaChoice)
                    {
                        XmlSchemaChoice choice = (XmlSchemaChoice)particle;

                        choice.Items.Remove(any);
                    }
                    else if (particle is XmlSchemaSequence)
                    {
                        XmlSchemaSequence sequence = (XmlSchemaSequence)particle;

                        sequence.Items.Remove(any);
                    }
                }

                // remove anyAttribute items
                foreach (XmlSchemaComplexType complexType in this.anyAttributeElements)
                {
                    complexType.AnyAttribute = null;
                }

                XmlTextWriter writer = null;

                try
                {
                    writer = new XmlTextWriter(this.outputFile, System.Text.Encoding.Default);
                    writer.Indentation = 4;
                    writer.IndentChar = ' ';
                    writer.Formatting = Formatting.Indented;

                    mainSchema.Write(writer);
                }
                finally
                {
                    writer.Close();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("xsdStitch.exe : fatal error XSDS0001 : {0}\r\n\n\nStack Trace:\r\n{1}", e.Message, e.StackTrace);

                return 1;
            }

            return 0;
        }

        /// <summary>
        /// Insert an element above an existing any item.
        /// </summary>
        /// <param name="particle">The particle in which the any item should be found.</param>
        /// <param name="element">The element to insert.</param>
        /// <returns>true if the element was inserted; false otherwise.</returns>
        private bool InsertElement(XmlSchemaParticle particle, XmlSchemaElement element)
        {
            if (particle is XmlSchemaChoice)
            {
                XmlSchemaChoice choice = (XmlSchemaChoice)particle;

                for (int i = 0; i < choice.Items.Count; i++)
                {
                    XmlSchemaParticle childParticle = (XmlSchemaParticle)choice.Items[i];

                    if (childParticle is XmlSchemaAny)
                    {
                        // index this any element for later removal
                        this.anys[childParticle] = choice;

                        choice.Items.Insert(i, element);
                        return true;
                    }
                    else
                    {
                        if (this.InsertElement(childParticle, element))
                        {
                            return true;
                        }
                    }
                }
            }
            else if (particle is XmlSchemaSequence)
            {
                XmlSchemaSequence sequence = (XmlSchemaSequence)particle;

                for (int i = 0; i < sequence.Items.Count; i++)
                {
                    XmlSchemaParticle childParticle = (XmlSchemaParticle)sequence.Items[i];

                    if (childParticle is XmlSchemaAny)
                    {
                        // index this any element for later removal
                        this.anys[childParticle] = sequence;

                        sequence.Items.Insert(i, element);
                        return true;
                    }
                    else
                    {
                        if (this.InsertElement(childParticle, element))
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        /// <summary>
        /// Parse the commandline arguments.
        /// </summary>
        /// <param name="args">The commandline arguments.</param>
        private void ParseCommandLine(string[] args)
        {
            for (int i = 0; i < args.Length; ++i)
            {
                string arg = args[i];
                if (null == arg || string.Empty == arg) // skip blank arguments
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);

                    switch (parameter)
                    {
                        case "?":
                            this.showHelp = true;
                            break;
                        case "nologo":
                            this.showLogo = false;
                            break;
                        default: // other parameters
                            if (parameter.StartsWith("ext"))
                            {
                                if (args.Length < ++i || '/' == args[i][0] || '-' == args[i][0])
                                {
                                    throw new Exception();
                                }

                                this.extensionSchemaFiles.Add(args[i]);
                            }
                            else
                            {
                                throw new ArgumentException("Invalid argument.", parameter);
                            }
                            break;
                    }
                }
                else if ('@' == arg[0])
                {
                    using (StreamReader reader = new StreamReader(arg.Substring(1)))
                    {
                        string line;
                        ArrayList newArgs = new ArrayList();

                        while (null != (line = reader.ReadLine()))
                        {
                            string newArg = "";
                            bool betweenQuotes = false;
                            for (int j = 0; j < line.Length; ++j)
                            {
                                // skip whitespace
                                if (!betweenQuotes && (' ' == line[j] || '\t' == line[j]))
                                {
                                    if ("" != newArg)
                                    {
                                        newArgs.Add(newArg);
                                        newArg = null;
                                    }

                                    continue;
                                }

                                // if we're escaping a quote
                                if ('\\' == line[j] && j < line.Length - 1 && '"' == line[j + 1])
                                {
                                    ++j;
                                }
                                else if ('"' == line[j])   // if we've hit a new quote
                                {
                                    betweenQuotes = !betweenQuotes;
                                    continue;
                                }

                                newArg = String.Concat(newArg, line[j]);
                            }

                            if ("" != newArg)
                            {
                                newArgs.Add(newArg);
                            }
                        }
                        string[] ar = (string[])newArgs.ToArray(typeof(string));
                        this.ParseCommandLine(ar);
                    }
                }
                else if (null == this.mainSchemaFile)
                {
                    this.mainSchemaFile = Path.GetFullPath(arg);
                }
                else if (null == this.outputFile)
                {
                    this.outputFile = Path.GetFullPath(arg);
                }
                else
                {
                    throw new ArgumentException(String.Format("Unknown argument: '{0}'", arg));
                }
            }

            if (null == this.outputFile)
            {
                this.showHelp = true;
            }
        }
    }
}
