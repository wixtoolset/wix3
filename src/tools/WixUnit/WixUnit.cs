//-------------------------------------------------------------------------------------------------
// <copyright file="WixUnit.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Windows Installer XML unit test runner.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Unit
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.CodeDom.Compiler;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Xml;
    using System.Xml.Schema;
    using System.Xml.Serialization;

    using Microsoft.Tools.WindowsInstallerXml;

    /// <summary>
    /// The Windows Installer XML unit test runner.
    /// </summary>
    public sealed class WixUnit : ICommandArgs
    {
        private const string XmlNamespace = "http://schemas.microsoft.com/wix/2006/WixUnit";
        private readonly string failedTestsFile = Path.Combine(Path.GetTempPath(), "WixUnitFailedTests.xml");

        private object lockObject = new object();
        private AutoResetEvent unitTestsComplete = new AutoResetEvent(false);

        private int completedUnitTests;
        private Dictionary<string, int> failedUnitTests = new Dictionary<string, int>();
        private int totalUnitTests;

        private ConsoleMessageHandler messageHandler = new ConsoleMessageHandler("WUNT", "WixUnit");
        private List<KeyValuePair<string, string>> environmentVariables = new List<KeyValuePair<string, string>>();
        private bool noTidy;
        private bool rerunFailedTests = false;
        private bool showHelp;
        private bool singleThreaded = false;
        private TempFileCollection tempFileCollection;
        private Queue unitTestElements = new Queue();
        private ArrayList unitTests = new ArrayList();
        private string unitTestsFile;
        private bool updateTests;
        private bool validate;
        private bool verbose;

        /// <summary>
        /// Constructor to prevent instantiating a static class.
        /// </summary>
        private WixUnit()
        {
        }

        /// <summary>
        /// Whether to remove intermediate build files.
        /// </summary>
        public bool NoTidy
        {
            get { return this.noTidy; }
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command line arguments.</param>
        /// <returns>The error code for the application.</returns>
        public static int Main(string[] args)
        {
            WixUnit wixUnit = new WixUnit();
            return wixUnit.Run(args);
        }

        /// <summary>
        /// Recursively loops through a directory, changing an attribute on all of the underlying files.
        /// An example is to add/remove the ReadOnly flag from each file.
        /// </summary>
        /// <param name="path">The directory path to start deleting from.</param>
        /// <param name="fileAttribute">The FileAttribute to change on each file.</param>
        /// <param name="markAttribute">If true, add the attribute to each file. If false, remove it.</param>
        private static void RecursiveFileAttributes(string path, FileAttributes fileAttribute, bool markAttribute)
        {
            foreach (string subDirectory in Directory.GetDirectories(path))
            {
                RecursiveFileAttributes(subDirectory, fileAttribute, markAttribute);
            }

            foreach (string filePath in Directory.GetFiles(path))
            {
                FileAttributes attributes = File.GetAttributes(filePath);
                if (markAttribute)
                {
                    attributes = attributes | fileAttribute; // add to list of attributes
                }
                else if (fileAttribute == (attributes & fileAttribute)) // if attribute set
                {
                    attributes = attributes ^ fileAttribute; // remove from list of attributes
                }
                File.SetAttributes(filePath, attributes);
            }
        }


        /// <summary>
        /// Main running method for the application.
        /// </summary>
        /// <param name="args">Commandline arguments to the application.</param>
        /// <returns>Returns the application error code.</returns>
        private int Run(string[] args)
        {
            int beginTickCount = Environment.TickCount;

            try
            {
                this.tempFileCollection = new TempFileCollection();

                Environment.SetEnvironmentVariable("WixUnitTempDir", this.tempFileCollection.BasePath, EnvironmentVariableTarget.Process);
                this.ParseCommandline(args);

                // get the assemblies
                Assembly wixUnitAssembly = this.GetType().Assembly;
                FileVersionInfo fv = FileVersionInfo.GetVersionInfo(wixUnitAssembly.Location);

                if (this.showHelp)
                {
                    Console.WriteLine("WixUnit version {0}", fv.FileVersion);
                    Console.WriteLine("Copyright (C) Outercurve Foundation. All rights reserved.");
                    Console.WriteLine();
                    Console.WriteLine(" usage: WixUnit [-?] tests.xml");
                    Console.WriteLine();
                    Console.WriteLine("   -env:<var>=<value>  Sets an environment variable to the value for the current process");
                    Console.WriteLine("   -notidy             Do not delete temporary files (for checking results)");
                    Console.WriteLine("   -rf                 Re-run the failed test from the last run");
                    Console.WriteLine("   -st                 Run the tests on a single thread");                    
                    Console.WriteLine("   -test:<Test_name>   Run only the specified test (may use wildcards)");
                    Console.WriteLine("   -update             Prompt user to auto-update a test if expected and actual output files do not match");
                    Console.WriteLine("   -v                  Verbose output");
                    Console.WriteLine("   -val                Run MSI validation for light unit tests");

                    return 0;
                }

                // set the environment variables for the process only
                foreach (KeyValuePair<string, string> environmentVariable in this.environmentVariables)
                {
                    Environment.SetEnvironmentVariable(environmentVariable.Key, environmentVariable.Value, EnvironmentVariableTarget.Process);
                }

                // load the schema
                XmlReader schemaReader = null;
                XmlSchemaCollection schemas = null;
                try
                {
                    schemas = new XmlSchemaCollection();

                    schemaReader = new XmlTextReader(wixUnitAssembly.GetManifestResourceStream("Microsoft.Tools.WindowsInstallerXml.Unit.unitTests.xsd"));
                    XmlSchema schema = XmlSchema.Read(schemaReader, null);
                    schemas.Add(schema);
                }
                finally
                {
                    if (schemaReader != null)
                    {
                        schemaReader.Close();
                    }
                }

                // load the unit tests
                XmlTextReader reader = null;
                XmlDocument doc = new XmlDocument();
                try
                {
                    reader = new XmlTextReader(this.unitTestsFile);
                    XmlValidatingReader validatingReader = new XmlValidatingReader(reader);
                    validatingReader.Schemas.Add(schemas);

                    // load the xml into a DOM
                    doc.Load(validatingReader);
                }
                catch (XmlException e)
                {
                    SourceLineNumber sourceLineNumber = new SourceLineNumber(this.unitTestsFile, e.LineNumber);
                    SourceLineNumberCollection sourceLineNumbers = new SourceLineNumberCollection(new SourceLineNumber[] { sourceLineNumber });

                    throw new WixException(WixErrors.InvalidXml(sourceLineNumbers, "unitTests", e.Message));
                }
                catch (XmlSchemaException e)
                {
                    SourceLineNumber sourceLineNumber = new SourceLineNumber(this.unitTestsFile, e.LineNumber);
                    SourceLineNumberCollection sourceLineNumbers = new SourceLineNumberCollection(new SourceLineNumber[] { sourceLineNumber });

                    throw new WixException(WixErrors.SchemaValidationFailed(sourceLineNumbers, e.Message, e.LineNumber, e.LinePosition));
                }
                finally
                {
                    if (reader != null)
                    {
                        reader.Close();
                    }
                }

                // check the document element
                if ("UnitTests" != doc.DocumentElement.LocalName || XmlNamespace != doc.DocumentElement.NamespaceURI)
                {
                    throw new InvalidOperationException("Unrecognized document element.");
                }

                // create a regular expression of the selected tests
                Regex selectedUnitTests = new Regex(String.Concat("^", String.Join("$|^", (string[])this.unitTests.ToArray(typeof(string))), "$"), RegexOptions.IgnoreCase | RegexOptions.Singleline);

                // find the unit tests
                foreach (XmlNode node in doc.DocumentElement)
                {
                    if (XmlNodeType.Element == node.NodeType)
                    {
                        switch (node.LocalName)
                        {
                            case "UnitTest":
                                XmlElement unitTestElement = (XmlElement)node;
                                string unitTestName = unitTestElement.GetAttribute("Name");

                                if (selectedUnitTests.IsMatch(unitTestName))
                                {
                                    unitTestElement.SetAttribute("TempDirectory", this.tempFileCollection.BasePath);
                                    this.unitTestElements.Enqueue(node);
                                }
                                break;
                        }
                    }
                }

                if (this.unitTests.Count > 0)
                {
                    this.totalUnitTests = this.unitTestElements.Count;
                    int numThreads;

                    if (this.updateTests || this.singleThreaded)
                    {
                        // If the tests are running with the -update switch, they must run on one thread
                        // so that all execution is paused when the user is prompted to update a test.
                        numThreads = 1;
                    }
                    else
                    {
                        // create a thread for each processor
                        numThreads = Convert.ToInt32(Environment.GetEnvironmentVariable("NUMBER_OF_PROCESSORS"), CultureInfo.InvariantCulture);
                    }

                    Thread[] threads = new Thread[numThreads];

                    for (int i = 0; i < threads.Length; i++)
                    {
                        threads[i] = new Thread(new ThreadStart(this.RunUnitTests));
                        threads[i].Start();
                    }

                    // wait for all threads to finish
                    foreach (Thread thread in threads)
                    {
                        thread.Join();
                    }

                    // report the results
                    Console.WriteLine();
                    int elapsedTime = (Environment.TickCount - beginTickCount) / 1000;
                    if (0 < this.failedUnitTests.Count)
                    {
                        Console.WriteLine("Summary of failed tests:");
                        Console.WriteLine();

                        // Put the failed tests into an ArrayList, which will get serialized
                        ArrayList serializedFailedTests = new ArrayList();
                        foreach (string failedTest in this.failedUnitTests.Keys)
                        {
                            serializedFailedTests.Add(failedTest);
                            Console.WriteLine("{0}. {1}", this.failedUnitTests[failedTest], failedTest);
                        }

                        Console.WriteLine();
                        Console.WriteLine("Re-run the failed tests with the -rf option");
                        Console.WriteLine();
                        Console.WriteLine("Failed {0} out of {1} unit test{2} ({3} seconds).", this.failedUnitTests.Count, this.totalUnitTests, (1 != this.completedUnitTests ? "s" : ""), elapsedTime);

                        using (XmlWriter writer = XmlWriter.Create(this.failedTestsFile))
                        {
                            XmlSerializer serializer = new XmlSerializer(serializedFailedTests.GetType());
                            serializer.Serialize(writer, serializedFailedTests);
                            writer.Close();
                        }
                    }
                    else
                    {
                        Console.WriteLine("Successful unit tests: {0} ({1} seconds).", this.completedUnitTests, elapsedTime);
                    }
                    Console.WriteLine();
                }
                else
                {
                    Console.WriteLine("No unit tests were selected.");
                }
            }
            catch (WixException we)
            {
                this.messageHandler.Display(this, we.Error);
            }
            catch (Exception e)
            {
                this.messageHandler.Display(this, WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
                if (e is NullReferenceException)
                {
                    throw;
                }
            }
            finally
            {
                if (this.noTidy)
                {
                    Console.WriteLine();
                    Console.WriteLine("The notidy option was specified, temporary files can be found at:");
                    Console.WriteLine(this.tempFileCollection.BasePath);
                }
                else
                {
                    // try three times and give up with a warning if the temp files aren't gone by then
                    const int RetryLimit = 3;

                    for (int i = 0; i < RetryLimit; i++)
                    {
                        try
                        {
                            Directory.Delete(this.tempFileCollection.BasePath, true);   // toast the whole temp directory
                            break; // no exception means we got success the first time
                        }
                        catch (UnauthorizedAccessException)
                        {
                            if (0 == i) // should only need to unmark readonly once - there's no point in doing it again and again
                            {
                                RecursiveFileAttributes(this.tempFileCollection.BasePath, FileAttributes.ReadOnly, false); // toasting will fail if any files are read-only. Try changing them to not be.
                            }
                            else
                            {
                                break;
                            }
                        }
                        catch (DirectoryNotFoundException)
                        {
                            // if the path doesn't exist, then there is nothing for us to worry about
                            break;
                        }
                        catch (IOException) // directory in use
                        {
                            if (i == (RetryLimit - 1)) // last try failed still, give up
                            {
                                break;
                            }
                            Thread.Sleep(300);  // sleep a bit before trying again
                        }
                    }
                }
            }

            return this.failedUnitTests.Count;
        }

        /// <summary>
        /// Run the unit tests.
        /// </summary>
        private void RunUnitTests()
        {
            try
            {
                while (true)
                {
                    XmlElement unitTestElement;

                    lock (this.unitTestElements)
                    {
                        // check if there are any more cabinets to create
                        if (0 == this.unitTestElements.Count)
                        {
                            break;
                        }

                        unitTestElement = (XmlElement)this.unitTestElements.Dequeue();
                    }

                    // create a cabinet
                    this.RunUnitTest(unitTestElement);
                }
            }
            catch (WixException we)
            {
                this.messageHandler.Display(this, we.Error);
            }
            catch (Exception e)
            {
                this.messageHandler.Display(this, WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));
            }
        }

        /// <summary>
        /// Run a unit test.
        /// </summary>
        /// <param name="unitTestElement">The unit test to run.</param>
        private void RunUnitTest(XmlElement unitTestElement)
        {
            string name = unitTestElement.GetAttribute("Name");
            string tempDirectory = Path.Combine(unitTestElement.GetAttribute("TempDirectory"), name);
            UnitResults unitResults = new UnitResults();

            try
            {
                // ensure the temp directory exists
                Directory.CreateDirectory(tempDirectory);

                foreach (XmlNode node in unitTestElement.ChildNodes)
                {
                    if (XmlNodeType.Element == node.NodeType)
                    {
                        XmlElement unitElement = (XmlElement)node;

                        // add inherited attributes from the parent "UnitTest" element
                        foreach (XmlAttribute attribute in unitTestElement.Attributes)
                        {
                            if (attribute.NamespaceURI.Length == 0)
                            {
                                unitElement.SetAttribute(attribute.Name, attribute.Value);
                            }
                        }

                        // add inherited attributes from the grandparent "UnitTests" element
                        foreach (XmlAttribute attribute in unitTestElement.ParentNode.Attributes)
                        {
                            if (attribute.NamespaceURI.Length == 0)
                            {
                                unitElement.SetAttribute(attribute.Name, attribute.Value);
                            }
                        }
                        unitElement.SetAttribute("TempDirectory", tempDirectory);

                        switch (node.LocalName)
                        {
                            case "Candle":
                                CandleUnit.RunUnitTest(unitElement, unitResults, this);
                                break;
                            case "Compare":
                                CompareUnit.RunUnitTest(unitElement, unitResults, this.updateTests, this);
                                break;
                            case "Dark":
                                DarkUnit.RunUnitTest(unitElement, unitResults, this);
                                break;
                            case "Heat":
                                HeatUnit.RunUnitTest(unitElement, unitResults, this);
                                break;
                            case "Insignia":
                                InsigniaUnit.RunUnitTest(unitElement, unitResults, this);
                                break;
                            case "Light":
                                // If WixUnit was not run with -val then suppress MSI validation
                                if (!this.validate && ("true" != unitElement.GetAttribute("ForceValidation")))
                                {
                                    string arguments = unitElement.GetAttribute("Arguments");
                                    if (!arguments.Contains("-sval"))
                                    {
                                        unitElement.SetAttribute("Arguments", String.Concat(arguments, " -sval"));
                                    }
                                }

                                LightUnit.RunUnitTest(unitElement, unitResults, this.updateTests, this);
                                break;
                            case "Lit":
                                LitUnit.RunUnitTest(unitElement, unitResults, this);
                                break;
                            case "Process":
                                ProcessUnit.RunUnitTest(unitElement, unitResults, this);
                                break;
                            case "Pyro":
                                PyroUnit.RunUnitTest(unitElement, unitResults, this.updateTests, this);
                                break;
                            case "Torch":
                                TorchUnit.RunUnitTest(unitElement, unitResults, this.updateTests, this);
                                break;
                            case "WixProj":
                                bool skipValidation = (!this.validate);
                                WixProjUnit.RunUnitTest(unitElement, unitResults, this.verbose, skipValidation, this.updateTests, this);
                                break;
                            case "Smoke":
                                SmokeUnit.RunUnitTest(unitElement, unitResults, this);
                                break;
                        }

                        // check for errors
                        if (unitResults.Errors.Count > 0)
                        {
                            break;
                        }
                    }
                }
            }
            catch (WixException we)
            {
                string message = this.messageHandler.GetMessageString(this, we.Error);

                if (null != message)
                {
                    unitResults.Errors.Add(message);
                    unitResults.Output.Add(message);
                }
            }
            catch (Exception e)
            {
                string message = this.messageHandler.GetMessageString(this, WixErrors.UnexpectedException(e.Message, e.GetType().ToString(), e.StackTrace));

                if (null != message)
                {
                    unitResults.Errors.Add(message);
                    unitResults.Output.Add(message);
                }
            }

            lock (this.lockObject)
            {
                Console.Write("{0} of {1} - {2}: ", ++this.completedUnitTests, this.totalUnitTests, name.PadRight(30, '.'));

                if (unitResults.Errors.Count > 0)
                {
                    this.failedUnitTests.Add(name, this.completedUnitTests);

                    Console.WriteLine("Failed");

                    if (this.verbose)
                    {
                        foreach (string line in unitResults.Output)
                        {
                            Console.WriteLine(line);
                        }
                        Console.WriteLine();
                    }
                    else
                    {
                        foreach (string line in unitResults.Errors)
                        {
                            Console.WriteLine(line);
                        }
                        Console.WriteLine();
                    }
                }
                else
                {
                    Console.WriteLine("Success");

                    if (this.verbose)
                    {
                        foreach (string line in unitResults.Output)
                        {
                            Console.WriteLine(line);
                        }
                        Console.WriteLine();
                    }
                }

                if (this.totalUnitTests == this.completedUnitTests)
                {
                    this.unitTestsComplete.Set();
                }
            }
        }

        /// <summary>
        /// Parse the command line arguments.
        /// </summary>
        /// <param name="args">Command line arguments.</param>
        private void ParseCommandline(string[] args)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith("-") || arg.StartsWith("/"))
                {
                    string parameter = arg.Substring(1);

                    if ("?" == parameter)
                    {
                        this.showHelp = true;
                        return;
                    }
                    else if (parameter.StartsWith("env:"))
                    {
                        parameter = parameter.Substring("env:".Length);
                        int equalPos = parameter.IndexOf('=');
                        if (0 > equalPos)
                        {
                            throw new ArgumentException("env parameters require a name=value pair.");
                        }
                        string name = parameter.Substring(0, equalPos);
                        string value = parameter.Substring(equalPos + 1);
                        this.environmentVariables.Add(new KeyValuePair<string, string>(name, value));
                    }
                    else if ("notidy" == parameter)
                    {
                        this.noTidy = true;
                    }
                    else if ("rf" == parameter)
                    {
                        this.rerunFailedTests = true;
                        ArrayList previouslyFailedTests = null;

                        if (File.Exists(this.failedTestsFile))
                        {
                            XmlReader reader = null;

                            try
                            {
                                reader = XmlReader.Create(this.failedTestsFile);
                                XmlSerializer serializer = new XmlSerializer(typeof(ArrayList));
                                previouslyFailedTests = (ArrayList)serializer.Deserialize(reader);
                            }
                            catch (InvalidOperationException e)
                            {
                                Console.WriteLine(String.Concat("There was an error loading the failed tests from ", this.failedTestsFile));
                                Console.WriteLine(e.Message);
                            }
                            finally
                            {
                                if (null != reader)
                                {
                                    reader.Close();
                                }

                                if (File.Exists(this.failedTestsFile))
                                {
                                    File.Delete(this.failedTestsFile);
                                }
                            }
                        }

                        if (null != previouslyFailedTests)
                        {
                            this.unitTests.AddRange(previouslyFailedTests);
                        }
                    }
                    else if ("st" == parameter)
                    {
                        this.singleThreaded = true;
                    }
                    else if (parameter.StartsWith("test:"))
                    {
                        this.unitTests.Add(parameter.Substring(5));
                    }
                    else if ("update" == parameter)
                    {
                        this.updateTests = true;
                    }
                    else if ("v" == parameter)
                    {
                        this.verbose = true;
                    }
                    else if ("val" == parameter)
                    {
                        this.validate = true;
                    }
                    else
                    {
                        throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Unrecognized commandline parameter '{0}'.", arg));
                    }
                }
                else if (null == this.unitTestsFile)
                {
                    this.unitTestsFile = arg;
                }
                else
                {
                    throw new ArgumentException(String.Format(CultureInfo.InvariantCulture, "Unrecognized argument '{0}'.", arg));
                }
            }

            // no individual unit tests were selected, so match all unit tests
            if (0 == this.unitTests.Count && !this.rerunFailedTests)
            {
                this.unitTests.Add(".*");
            }
        }
    }
}
