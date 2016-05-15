// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Text;
    using System.Xml;
    using System.Xml.XPath;
    using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Msi;
    using Xunit;
    using DTF = Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// Contains methods for test verification
    /// </summary>
    public static class Verifier
    {
        /// <summary>
        /// Verifies the value returned by an MSI query
        /// </summary>
        /// <param name="msi">The path to an MSI</param>
        /// <param name="query">An MSI query</param>
        /// <param name="expectedValue">The string that the query is expected to return</param>
        /// <remarks>
        /// Only works for queries that return a single value (ie. not a query that returns a row or set of rows)
        /// </remarks>
        public static void VerifyQuery(string msi, string query, string expectedValue)
        {
            string queryResult = Verifier.Query(msi, query);
            Assert.True(expectedValue == queryResult, String.Format("The query '{0}' on '{1}' did not return the expected results", query, msi));
        }

        /// <summary>
        /// Query an MSI table for a single value
        /// </summary>
        /// <param name="msi">The path to an MSI</param>
        /// <param name="sql">An MSI query</param>
        /// <returns>The results as a string or null if no results are returned</returns>
        /// <remarks>
        /// Returns the value of the first field in the first record
        /// </remarks>
        public static string Query(string msi, string query)
        {
            string result = null;

            using (Database database = new Database(msi, OpenDatabase.ReadOnly))
            {
                using (View view = database.OpenExecuteView(query))
                {
                    using (Microsoft.Tools.WindowsInstallerXml.Msi.Record record = view.Fetch())
                    {
                        if (null != record)
                        {
                            result = Convert.ToString(record.GetString(1));
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Query an MSI table for all records
        /// </summary>
        /// <param name="msi">The path to an MSI</param>
        /// <param name="sql">An MSI query</param>
        /// <returns>A list of records is returned</returns>
        /// <remarks>Uses DTF</remarks>
        public static List<DTF.Record> QueryAllRecords(string msi, string query)
        {
            List<DTF.Record> result = new List<DTF.Record>();

            using (DTF.Database database = new DTF.Database(msi, DTF.DatabaseOpenMode.ReadOnly))
            {
                using (DTF.View view = database.OpenView(query,null))
                {
                    view.Execute();

                    DTF.Record record = null;
                    while (null != (record = view.Fetch()))
                    {
                        // Copy record created by Fetch to record created manually to remove View reference
                        DTF.Record copyRecord = new DTF.Record(record.FieldCount);
                        for (int i = 0; i <= record.FieldCount; i++)
                        {
                            copyRecord[i] = record[i];
                        }
                        record.Close();
                        result.Add(copyRecord);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Verifies the codepage of a database
        /// </summary>
        /// <param name="msi">The path to the database</param>
        /// <param name="expectedCodepage">The expected codepage</param>
        public static void VerifyDatabaseCodepage(string msi, int expectedCodepage)
        {
            int actualCodepage = Verifier.GetDatabaseCodepage(msi);
            Assert.True(expectedCodepage == actualCodepage, String.Format("The codepage for {0} does not match the expected codepage", msi));
        }

        /// <summary>
        /// Gets the codepage of a database
        /// </summary>
        /// <param name="msi">The path to the database</param>
        /// <returns>The codepage if it was found, or -1 if it could not be found</returns>
        /// <remarks>
        /// Most of this code was copied from Unbinder.UnbindDatabase() in wix.csproj
        /// </remarks>
        public static int GetDatabaseCodepage(string msi)
        {
            int codepage = -1;

            Database database = new Database(msi, OpenDatabase.ReadOnly);

            string codepageIdt = String.Concat(Path.GetTempFileName(), ".idt");
            database.Export("_ForceCodepage", Path.GetDirectoryName(codepageIdt), Path.GetFileName(codepageIdt));
            using (StreamReader sr = File.OpenText(codepageIdt))
            {
                string line;

                while (null != (line = sr.ReadLine()))
                {
                    string[] data = line.Split('\t');

                    if (2 == data.Length)
                    {
                        codepage = Convert.ToInt32(data[0], CultureInfo.InvariantCulture);
                    }
                }
            }

            return codepage;
        }

        /// <summary>
        /// Properties of Msi Summary Information stream.
        /// For more information see: 
        ///     http://msdn.microsoft.com/en-us/library/aa372046(v=VS.85).aspx
        /// </summary>
        public enum MsiSummaryInformationProperty
        {
            /// <summary>
            /// The Codepage Summary property is the numeric value of the ANSI code page used for any strings that are stored in the summary information
            /// </summary>
            Codepage = 1,

            /// <summary>
            /// The Title Summary property briefly describes the type of the installer package. 
            /// Phrases such as "Installation Database" or "Transform" or "Patch" may be used for this property.
            /// </summary>
            Title = 2,

            /// <summary>
            /// The value of the Subject Summary property conveys the name of the product, transform, or patch that is installed by the package. 
            /// </summary>
            Subject = 3,

            /// <summary>
            /// The Author Summary property conveys the manufacturer of the installation package, transform, or patch package.
            /// </summary>
            Author = 4,

            /// <summary>
            /// The Keywords Summary property in installation databases or transforms contains a list of keywords. 
            /// </summary>
            Keywords = 5,

            /// <summary>
            /// The Comments Summary property conveys the general purpose of the installation package, transform, or patch package.
            /// </summary>
            Comments = 6,

            /// <summary>
            /// For an installation package, the Template Summary property indicates the platform and language versions that are compatible with this installation database. 
            /// The syntax of the Template Summary property information for an installation database is the following: 
            ///     [platform property];[language id][,language id][,...].
            /// </summary>
            TargetPlatformAndLanguage = 7,

            /// <summary>
            /// For an installation package, the Revision Number Summary property contains the package code for the installer package. 
            /// </summary>
            PackageCode = 9,

            /// <summary>
            /// The Create Time/Date Summary property conveys the time and date when an author created the installation package, transform, or patch package.
            /// </summary>
            CreatedDatatime = 12,

            /// <summary>
            /// The Last Saved Time/Date Summary property conveys the last time when this installation package, transform, or patch package was modified.
            /// </summary>
            /// 
            LastSavedDatatime = 13,

            /// <summary>
            /// The Page Count Summary property contains the minimum installer version required by the installation package.
            /// </summary>
            Schema = 14,

            /// <summary>
            /// In the summary information of an installation package, the Word Count Summary property indicates the type of source file image. 
            /// If this property is not present, it defaults to zero (0).
            /// </summary>
            WordCount = 15,

            /// <summary>
            /// The Creating Application Summary property conveys which application created the installer database. 
            /// </summary>
            CreatingApplication = 18,

            /// <summary>
            /// The Security Summary property conveys whether the package should be opened as read-only. 
            /// </summary>
            Security = 19
        };

        /// <summary>
        /// Verifies that the value of a summary information stream property matches the expected value
        /// </summary>
        /// <param name="msi">The MSI to verify</param>
        /// <param name="propertyIndex">The summary information stream property Id of the property to verify</param>
        /// <param name="expectedValue">The expected value</param>
        public static void VerifySummaryInformationProperty(string msi, int propertyIndex, string expectedValue)
        {
            string actualValue = Verifier.GetSummaryInformationProperty(msi, propertyIndex);
            Assert.True(expectedValue == actualValue, "The expected summary information property does not match the actual value");
        }

        /// <summary>
        /// Gets the value of a summary information stream property
        /// </summary>
        /// <param name="msi">The MSI to get the property from</param>
        /// <param name="propertyIndex">The summary information stream property Id of the property to get</param>
        /// <returns>Summary information stream property</returns>
        /// <remarks>
        /// This method reflects on wix.dll to use some of its internal methods for getting Summary Information data
        /// </remarks>
        public static string GetSummaryInformationProperty(string msi, int propertyIndex)
        {
            // Load the wix.dll assembly
            string wixDllLocation = Path.Combine(Settings.WixToolsDirectory, "wix.dll");
            Assembly wix = Assembly.LoadFile(wixDllLocation);

            // Find the SummaryInformation type
            string summaryInformationTypeName = "Microsoft.Tools.WindowsInstallerXml.Msi.SummaryInformation";
            Type summaryInformationType = wix.GetType(summaryInformationTypeName);
            if (null == summaryInformationType)
            {
                throw new NullReferenceException(String.Format("The Type {0} could not be found in {1}", summaryInformationTypeName, wixDllLocation));
            }

            // Find the SummaryInformation.GetProperty method
            BindingFlags getPropertyBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            string getPropertyMethodName = "GetProperty";
            MethodInfo getPropertyMethod = summaryInformationType.GetMethod(getPropertyMethodName, getPropertyBindingFlags);
            if (null == getPropertyMethod)
            {
                throw new NullReferenceException(String.Format("The Method {0} could not be found in {1}", getPropertyMethodName, summaryInformationTypeName));
            }

            // Find the SummaryInformation.Dispose method
            BindingFlags disposeBindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            string disposeMethodName = "Dispose";
            MethodInfo disposeMethod = summaryInformationType.GetMethod(disposeMethodName, disposeBindingFlags);
            if (null == disposeMethod)
            {
                throw new NullReferenceException(String.Format("The Method {0} could not be found in {1}", disposeMethodName, summaryInformationTypeName));
            }

            // Create an instance of a SummaryInformation object
            Object[] constructorArguments = { msi };
            BindingFlags constructorBindingFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance;
            Object instance = wix.CreateInstance(summaryInformationTypeName, false, constructorBindingFlags, null, constructorArguments, CultureInfo.InvariantCulture, null);

            // Call the SummaryInformation.GetProperty method
            Object[] arguments = { propertyIndex };
            string value = (string)getPropertyMethod.Invoke(instance, arguments);
            
            // Dispose this instance explicitly so it is disposed on the same thread, avoiding a ?bug? in MSIHANDLEs
            disposeMethod.Invoke(instance, null);

            return value;
        }

        /// <summary>
        /// Gets the value of a summary information stream property
        /// </summary>
        /// <param name="msi">The MSI to get the property from</param>
        /// <param name="index">The  summary information stream property enum ID(MsiSummaryInformationPropertyIndex) of the property to get</param>
        /// <returns>Summary information stream property</returns>
        /// <remarks>
        /// This method reflects on wix.dll to use some of its internal methods for getting Summary Information data
        /// </remarks>
        public static string GetMsiSummaryInformationProperty(string msi, MsiSummaryInformationProperty index)
        {
            return GetSummaryInformationProperty (msi ,(int)index );
        }

        /// <summary>
        /// Assert that two files do not contain any differences
        /// </summary>
        /// <param name="expectedResult">The expected result file.</param>
        /// <param name="actualResult">The actual result file.</param>
        public static void VerifyResults(string expectedResult, string actualResult)
        {
            Verifier.VerifyResults(expectedResult, actualResult, (string[])null);
        }

        /// <summary>
        /// Assert that two files do not contain any differences
        /// </summary>
        /// <param name="expectedResult">The expected result file.</param>
        /// <param name="actualResult">The actual result file.</param>
        /// <param name="table">The table to compare</param>
        public static void VerifyResults(string expectedResult, string actualResult, string table)
        {
            Verifier.VerifyResults(expectedResult, actualResult, new string[] { table });
        }

        /// <summary>
        /// Assert that two files do not contain any differences
        /// </summary>
        /// <param name="expectedResult">The expected result file.</param>
        /// <param name="actualResult">The actual result file.</param>
        /// <param name="tables">The list of tables to compare</param>
        public static void VerifyResults(string expectedResult, string actualResult, params string[] tables)
        {
            ArrayList differences = Verifier.CompareResults(expectedResult, actualResult, tables);

            if (0 != differences.Count)
            {
                foreach (string difference in differences)
                {
                    Console.WriteLine(difference);
                }

                Assert.True(false, String.Format("Expected output '{0}' did not match actual output '{1}'", expectedResult, actualResult));
            }
        }

        /// <summary>
        /// Compare two result files.
        /// </summary>
        /// <param name="expectedResult">The expected result file.</param>
        /// <param name="actualResult">The actual result file.</param>
        /// <returns>Any differences found.</returns>
        public static ArrayList CompareResults(string expectedResult, string actualResult)
        {
            return Verifier.CompareResults(expectedResult, actualResult, null);
        }

        /// <summary>
        /// Compare two result files.
        /// </summary>
        /// <param name="expectedResult">The expected result file.</param>
        /// <param name="actualResult">The actual result file.</param>
        /// <param name="tables">The list of tables to compare</param>
        /// <returns>Any differences found.</returns>
        public static ArrayList CompareResults(string expectedResult, string actualResult, params string[] tables)
        {
            ArrayList differences = new ArrayList();
            Output targetOutput;
            Output updatedOutput;
            expectedResult = Environment.ExpandEnvironmentVariables(expectedResult);
            actualResult = Environment.ExpandEnvironmentVariables(actualResult);

            OutputType outputType;
            string extension = Path.GetExtension(expectedResult);
            if (String.Compare(extension, ".msi", true, CultureInfo.InvariantCulture) == 0)
            {
                outputType = OutputType.Product;
            }
            else if (String.Compare(extension, ".msm", true, CultureInfo.InvariantCulture) == 0)
            {
                outputType = OutputType.Module;
            }
            else if (String.Compare(extension, ".msp", true, CultureInfo.InvariantCulture) == 0)
            {
                outputType = OutputType.Patch;
            }
            else if (String.Compare(extension, ".mst", true, CultureInfo.InvariantCulture) == 0)
            {
                outputType = OutputType.Transform;
            }
            else if (String.Compare(extension, ".pcp", true, CultureInfo.InvariantCulture) == 0)
            {
                outputType = OutputType.PatchCreation;
            }
            else if (String.Compare(extension, ".wixout", true, CultureInfo.InvariantCulture) == 0)
            {
                outputType = OutputType.Unknown;
            }
            else
            {
                throw new InvalidOperationException(String.Format(CultureInfo.InvariantCulture, "Cannot determine the type of msi database file based on file extension '{0}'.", extension));
            }

            if (outputType != OutputType.Unknown)
            {
                Unbinder unbinder = new Unbinder();
                unbinder.SuppressDemodularization = true;

                targetOutput = unbinder.Unbind(expectedResult, outputType, null);
                updatedOutput = unbinder.Unbind(actualResult, outputType, null);
            }
            else
            {
                targetOutput = Output.Load(expectedResult, false, false);
                updatedOutput = Output.Load(actualResult, false, false);
            }
            
            differences.AddRange(CompareOutput(targetOutput, updatedOutput, tables));

            // If the Output type is a Patch, then compare the patch's transforms
            if (outputType == OutputType.Patch)
            {
                // Compare transforms
                foreach (SubStorage targetTransform in targetOutput.SubStorages)
                {
                    SubStorage updatedTransform = null;

                    // Find the same transform in the other patch
                    foreach (SubStorage transform in updatedOutput.SubStorages)
                    {
                        if (transform.Name == targetTransform.Name)
                        {
                            updatedTransform = transform;
                            break;
                        }
                    }

                    if (null != updatedTransform)
                    {
                        // Both patch's have this transform
                        ArrayList transformDifferences = Verifier.CompareOutput(targetTransform.Data, updatedTransform.Data);

                        // add a description of the transforms being compared
                        if (0 < transformDifferences.Count)
                        {
                            transformDifferences.Insert(0, String.Concat("Differences found while comparing the transform ", targetTransform.Name, " from the two patches"));
                            differences.AddRange(transformDifferences);
                        }
                    }
                    else
                    {
                        differences.Add(String.Format("The {0} tranform has been dropped", targetTransform.Name));
                    }
                }

                // Check if the updated patch has had transforms added
                foreach (SubStorage updatedTransform in updatedOutput.SubStorages)
                {
                    SubStorage targetTransform = null;
                    foreach (SubStorage transform in targetOutput.SubStorages)
                    {
                        if (transform.Name == updatedTransform.Name)
                        {
                            targetTransform = transform;
                            break;
                        }
                    }

                    if (targetTransform == null)
                    {
                        differences.Add(String.Format("The {0} tranform has been added", updatedTransform.Name));
                    }
                }
            }

            // add a description of the files being compared
            if (0 < differences.Count)
            {
                differences.Insert(0, "Differences found while comparing:");
                differences.Insert(1, expectedResult);
                differences.Insert(2, actualResult);
            }

            return differences;
        }

        /// <summary>
        /// Given a list of column names, and expected values, the method composes an SQL query against the specified file; 
        /// the query results has to match to exactly one row; if not an assert is raised.
        /// </summary>
        /// <param name="msiFile">MSI File name</param>
        /// <param name="tableName">Table to query</param>
        /// <param name="columns">Optional list of key value pairs represinting fields names and expected values</param>
        public static void VerifyTableData(string msiFile, MSITables table, params TableRow[] columns)
        {
            if (null == columns || columns.Length < 1)
            {
                throw new ArgumentException("Columns can not be empty.","Columns");
            }

            string query = string.Format("SELECT * FROM `{0}` WHERE ", table.ToString());

            for (int i = 0; i < columns.Length; i++)
            {
                TableRow pair = columns[i];

                query += string.Format(" `{0}` ", pair.Key);
                if (pair.IsString)
                {
                    query += string.Format(" = '{0}' ", pair.Value);
                }
                else
                {
                    if (string.IsNullOrEmpty(pair.Value))
                    {
                        query += string.Format(" IS NULL ", pair.Value);
                    }
                    else
                    {
                        query += string.Format(" = {0} ", pair.Value);
                    }
                }

                if (i < (columns.Length - 1))
                {
                    query += " AND ";
                }
            }

            List<DTF.Record> result = Verifier.QueryAllRecords(msiFile, query);
            Assert.True(1 == result.Count, string.Format("Result count: {0} Rows, Expected: 1 Rows. Query:'{1}', MSI File '{2}'", result.Count, query, msiFile));
        }

        /// <summary>
        /// Assert is true if Data in custom action table is set correctly .
        /// </summary>
        /// <param name="msiFile">MSI File name</param>
        /// <param name="customActionList">List of CustomActionTableData</param>
        public static void VerifyCustomActionTableData(string msiFile, params CustomActionTableData[] customActionList)
        {
            if (null == customActionList || customActionList.Length < 1)
            {
                throw new ArgumentException("customActionList can not be empty.", "customActionList");
            }

            foreach (CustomActionTableData customAction in customActionList)
            {
                Verifier.VerifyTableData(msiFile, MSITables.CustomAction,
                  new TableRow(CustomActionColumns.Action.ToString(), customAction.Action),
                  new TableRow(CustomActionColumns.Source.ToString(), customAction.Source),
                  new TableRow(CustomActionColumns.Target.ToString(), customAction.Target),
                  new TableRow(CustomActionColumns.Type.ToString(), customAction.Type.ToString(), false)
                  );
            }
        }

        /// <summary>
        /// Verifies that a given table  exists in the msi
        /// </summary>
        /// <param name="msi">The MSI to verify</param>
        /// <param name="tableName">The Name of the table to check for</param>
        public static void VerifyTableExists(string msi, string tableName)
        {
            bool tableExists = Verifier.CheckTableExists(msi, tableName);
            Assert.True(tableExists, String.Format("Table '{0}' does not exist in msi '{1}'. It was expected to exist.", tableName, msi));
        }

        /// <summary>
        /// Verifies that a given table does not exist in the msi
        /// </summary>
        /// <param name="msi">The MSI to verify</param>
        /// <param name="tableName">The Name of the table to check for</param>
        public static void VerifyNotTableExists(string msi, string tableName)
        {
            bool tableExists = Verifier.CheckTableExists(msi, tableName);
            Assert.False(tableExists, String.Format("Table '{0}' exists in msi '{1}'. It was NOT expected to exist.", tableName, msi));
        }

        /// <summary>
        /// Checks if a given table exists in the msi
        /// </summary>
        /// <param name="msi">The MSI to verify</param>
        /// <param name="tableName">The Name of the table to check for</param>
        /// <returns>True if the table exists in the msi, false otherwise</returns>
        public static bool CheckTableExists(string msi, string tableName)
        {
            bool tableExists = false;

            using (DTF.Database database = new DTF.Database(msi, DTF.DatabaseOpenMode.ReadOnly))
            {
                tableExists = database.Tables.Contains(tableName);
            }

            return tableExists;
        }

        /// <summary>
        /// Verifies that a given table exists in the output
        /// </summary>
        /// <param name="output">Output object to verify.</param>
        /// <param name="tableName">Name of the table to verify.</param>
        /// <param name="wixoutFile">File where output object is stored. Only used to display error message.</param>
        public static void VerifyTableExists(Output output, string tableName, string wixoutFile)
        {
            bool tableExists = CheckTableExists(output, tableName);
            Assert.True(tableExists, String.Format("Table '{0}' does not exist in output '{1}'. It was expected to exist.", tableName, wixoutFile));
        }

        /// <summary>
        /// Verifies that a given table does not exists in the output
        /// </summary>
        /// <param name="output">Output object to verify.</param>
        /// <param name="tableName">Name of the table to verify.</param>
        /// <param name="wixoutFile">File where output object is stored. Only used to display error message.</param>
        public static void VerifyNotTableExists(Output output, string tableName, string wixoutFile)
        {
            bool tableExists = CheckTableExists(output, tableName);
            Assert.False(tableExists, String.Format("Table '{0}' exists in output '{1}'. It was NOT expected to exist.", tableName, wixoutFile));
        }

        /// <summary>
        /// Verifies that a given table exists in the output
        /// </summary>
        /// <param name="output">Output object to verify.</param>
        /// <param name="tableName">Name of the table to check for.</param>
        /// <returns>True if the table exists in the output, false otherwise</returns>
        public static bool CheckTableExists(Output output, string tableName)
        {
            bool tableExists = true;
            try
            {
                int i = output.Tables[tableName].Rows.Count;
            }
            catch (Exception)
            {
                tableExists = false;
            }

            return tableExists;
        }

        /// <summary>
        /// Query XML for a node list.
        /// </summary>
        /// <param name="xmlPath">Path to XML.</param>
        /// <param name="xpathQuery">XPath Query.</param>
        /// <param name="nsm">A NameSpaceManager.</param>
        /// <returns>List of XML Nodes.</returns>
        public static XmlNodeList QueryXML(string xmlPath, string xpathQuery, XmlNamespaceManager nsm)
        {
            // Load the xml document
            XmlDocument xmldoc = new XmlDocument();
            xmldoc.Load(xmlPath);

            XmlNodeList nodeList = xmldoc.SelectNodes(xpathQuery, nsm);

            return nodeList;
        }

        /// <summary>
        /// Query wixlib file.
        /// </summary>
        /// <param name="wixLibPath">Path to a wixlib</param>
        /// <param name="xpathQuery">XPath Query</param>
        /// <returns>List of XmlNodes that match the query</returns>
        /// <remarks>The namespaces that should be used are 'wix', 'lib' or 'loc'</remarks>
        public static XmlNodeList QueryWixLib(string wixLibPath, string xpathQuery)
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("wix", "http://schemas.microsoft.com/wix/2006/objects");
            xmlNamespaceManager.AddNamespace("lib", "http://schemas.microsoft.com/wix/2006/libraries");
            xmlNamespaceManager.AddNamespace("loc", "http://schemas.microsoft.com/wix/2006/localization");
            XmlNodeList nodeList = Verifier.QueryXML(wixLibPath, xpathQuery, xmlNamespaceManager);
            return nodeList;
        }

        /// <summary>
        /// Query wixobj file.
        /// </summary>
        /// <param name="wixobjPath">Path to a wixobj</param>
        /// <param name="xpathQuery">XPath Query</param>
        /// <returns>List of XmlNodes that match the query</returns>
        /// <remarks>The namespace that should be used is 'wix'</remarks>
        public static XmlNodeList QueryWixobj(string wixobjPath, string xpathQuery)
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("wix", "http://schemas.microsoft.com/wix/2006/objects");
            XmlNodeList nodeList = Verifier.QueryXML(wixobjPath, xpathQuery, xmlNamespaceManager);
            return nodeList;
        }

        /// <summary>
        /// Query wixout file.
        /// </summary>
        /// <param name="wixLibPath">Path to a wixout</param>
        /// <param name="xpathQuery">XPath Query</param>
        /// <returns>List of XmlNodes that match the query</returns>
        /// <remarks>The namespace that should be used is 'obj', 'out' or 'tbl'</remarks>
        public static XmlNodeList QueryWixout(string wixoutPath, string xpathQuery)
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("obj", "http://schemas.microsoft.com/wix/2006/objects");
            xmlNamespaceManager.AddNamespace("out", "http://schemas.microsoft.com/wix/2006/outputs");
            xmlNamespaceManager.AddNamespace("tbl", "http://schemas.microsoft.com/wix/2006/tables");
            XmlNodeList nodeList = Verifier.QueryXML(wixoutPath, xpathQuery, xmlNamespaceManager);
            return nodeList;
        }

        /// <summary>
        /// Query Burn-manifest.xml file.
        /// </summary>
        /// <param name="burnManifestPath">Path to the Burn_manifest file</param>
        /// <param name="xpathQuery">XPath Query</param>
        /// <returns>List of XmlNodes that match the query</returns>
        /// <remarks>The namespace that should be used is 'burn'</remarks>
        public static XmlNodeList QueryBurnManifest(string burnManifestPath, string xpathQuery)
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("burn", "http://schemas.microsoft.com/wix/2008/Burn");
            XmlNodeList nodeList = Verifier.QueryXML(burnManifestPath, xpathQuery, xmlNamespaceManager);
            return nodeList;
        }

        /// <summary>
        /// Query Burn-UxManifest.xml file.
        /// </summary>
        /// <param name="burnUxManifestPath">Path to the Burn-UxManifest file</param>
        /// <param name="xpathQuery">XPath Query</param>
        /// <returns>List of XmlNodes that match the query</returns>
        /// <remarks>The namespace that should be used is 'burnUx'</remarks>
        public static XmlNodeList QueryBurnUxManifest(string burnUxManifestPath, string xpathQuery)
        {
            XmlNamespaceManager xmlNamespaceManager = new XmlNamespaceManager(new NameTable());
            xmlNamespaceManager.AddNamespace("burnUx", "http://schemas.microsoft.com/wix/2010/UxManifest");
            XmlNodeList nodeList = Verifier.QueryXML(burnUxManifestPath, xpathQuery, xmlNamespaceManager);
            return nodeList;
        }


        /// <summary>
        /// Verification of wixlib for a localization string.
        /// </summary>
        /// <param name="libraryFile">Path to wix library file.</param>
        /// <param name="culture">culture to verify</param>
        /// <param name="stringId">string looking for</param>
        /// <param name="expectedValue">Expected value.</param>
        public static void VerifyWixLibLocString(string libraryFile, string culture, string stringId, string expectedValue)
        {
            string xpathQuery = String.Format(@" /lib:wixLibrary/loc:WixLocalization[@Culture='{0}']/loc:String[@Id='{1}']", culture, stringId);
            XmlNodeList stringNode = Verifier.QueryWixLib(libraryFile, xpathQuery);
            Assert.Equal(1, stringNode.Count);
            Assert.NotNull(stringNode[0].InnerText);

            string actualValue = stringNode[0].InnerText;
            Assert.True(expectedValue == actualValue, String.Format("Unexpected value for Loc String: '{0}'  with Cutlure: '{1}'", stringId, culture));
        }

        /// <summary>
        /// Verification of wixlib for Property table.
        /// </summary>
        /// <param name="libraryFile">Path to wix library file.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <param name="expectedValue">Expected value.</param>
        /// <remarks>The function checks against the first node, if multiple nodes have the same ID in diffrent sections</remarks>
        public static void VerifyWixLibProperty(string libraryFile, string propertyName, string expectedValue)
        {
            string xpathQuery = String.Format(@"/lib:wixLibrary/wix:section/wix:table[@name='Property']/wix:row/wix:field[text()='{0}']", propertyName);
            XmlNodeList propertyNode = Verifier.QueryWixLib(libraryFile, xpathQuery);
            Assert.True(propertyNode.Count > 0, "Expected at least 1 node to be returned");
            Assert.NotNull(propertyNode[0].NextSibling);

            string actualValue = propertyNode[0].NextSibling.InnerText;
            Assert.True(expectedValue == actualValue, String.Format("Unexpected value for Property {0}", propertyName));
        }

        /// <summary>
        /// Verification of wixobj for Property table.
        /// </summary>
        /// <param name="outputFile">Path to output file.</param>
        /// <param name="propertyName">Name of property.</param>
        /// <param name="expectedValue">Expected value.</param>
        public static void VerifyWixObjProperty(string outputFile, string propertyName, string expectedValue)
        {
            string xpathQuery = String.Format(@"//wix:wixObject/wix:section/wix:table[@name='Property']/wix:row/wix:field[text()='{0}']", propertyName);
            XmlNodeList propertyNode = Verifier.QueryWixobj(outputFile, xpathQuery);
            Assert.Equal(1, propertyNode.Count);
            Assert.NotNull(propertyNode[0].NextSibling);

            string actualValue = propertyNode[0].NextSibling.InnerText;
            Assert.True(expectedValue == actualValue, String.Format("Unexpected value for Property {0}", propertyName));
        }

        /// <summary>
        /// Compare two Outputs
        /// </summary>
        /// <param name="targetOutput">The expected output</param>
        /// <param name="updatedOutput">The actual output</param>
        /// <returns>Any differences found.</returns>
        private static ArrayList CompareOutput(Output targetOutput, Output updatedOutput)
        {
            return Verifier.CompareOutput(targetOutput, updatedOutput, null);
        }

        /// <summary>
        /// Compare two Outputs
        /// </summary>
        /// <param name="targetOutput">The expected output</param>
        /// <param name="updatedOutput">The actual output</param>
        /// <param name="tables">The list of tables to compare</param>
        /// <returns>Any differences found.</returns>
        private static ArrayList CompareOutput(Output targetOutput, Output updatedOutput, params string[] tables)
        {
            ArrayList differences = new ArrayList();

            Differ differ = new Differ();
            differ.SuppressKeepingSpecialRows = true;
            Output transform = differ.Diff(targetOutput, updatedOutput);

            foreach (Table table in transform.Tables)
            {
                if (null != tables && -1 == Array.IndexOf(tables, table.Name))
                {
                    // Skip this table
                    continue;
                }

                switch (table.Operation)
                {
                    case TableOperation.Add:
                        differences.Add(String.Format(CultureInfo.InvariantCulture, "The {0} table has been added.", table.Name));
                        break;
                    case TableOperation.Drop:
                        differences.Add(String.Format(CultureInfo.InvariantCulture, "The {0} table has been dropped.", table.Name));
                        continue;
                }

                // index the target rows for better error messages
                Hashtable targetRows = new Hashtable();
                Table targetTable = targetOutput.Tables[table.Name];
                if (null != targetTable)
                {
                    foreach (Row row in targetTable.Rows)
                    {
                        string primaryKey = row.GetPrimaryKey('/');

                        // only index rows with primary keys since these are the ones that can be modified
                        if (null != primaryKey)
                        {
                            targetRows.Add(primaryKey, row);
                        }
                    }
                }

                foreach (Row row in table.Rows)
                {
                    switch (row.Operation)
                    {
                        case RowOperation.Add:
                            differences.Add(String.Format(CultureInfo.InvariantCulture, "The {0} table, row '{1}' has been added.", table.Name, row.ToString()));
                            break;
                        case RowOperation.Delete:
                            differences.Add(String.Format(CultureInfo.InvariantCulture, "The {0} table, row '{1}' has been deleted.", table.Name, row.ToString()));
                            break;
                        case RowOperation.Modify:
                            if (!Verifier.Ignore(row))
                            {
                                string primaryKey = row.GetPrimaryKey('/');
                                Row targetRow = (Row)targetRows[primaryKey];

                                differences.Add(String.Format(CultureInfo.InvariantCulture, "The {0} table, row '{1}' has changed to '{2}'.", table.Name, targetRow.ToString(), row.ToString()));
                            }
                            break;
                        default:
                            throw new InvalidOperationException("Unknown diff row.");
                    }
                }
            }

            return differences;
        }

        /// <summary>
        /// Determines if the given row can be ignored when comparing results.
        /// </summary>
        /// <param name="row">The row to check.</param>
        /// <returns>True if the row can be ignored; otherwise, false.</returns>
        private static bool Ignore(Row row)
        {
            if ("_SummaryInformation" == row.Table.Name)
            {
                // check timestamp and version-dependent fields
                switch ((int)row[0])
                {
                    case 9:
                    case 12:
                    case 13:
                    case 18:
                        return true;
                }
            }
            else if ("Property" == row.Table.Name)
            {
                switch ((string)row[0])
                {
                    case "ProductCode":
                    case "WixPdbPath":
                        return true;
                }
            }
            else if ("MsiPatchMetadata" == row.Table.Name)
            {
                switch (row.GetPrimaryKey('/'))
                {
                    case "/CreationTimeUTC":
                        return true;
                }
            }

            return false;
        }
    }
}
