// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Reflection;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using System.Xml.Schema;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Yes/no type (kinda like a boolean).
    /// </summary>
    public enum YesNoType
    {
        /// <summary>Value not set; equivalent to null for reference types.</summary>
        NotSet,

        /// <summary>The no value.</summary>
        No,

        /// <summary>The yes value.</summary>
        Yes,

        /// <summary>Not a valid yes or no value.</summary>
        IllegalValue,
    }

    /// <summary>
    /// Yes, No, Default xml simple type.
    /// </summary>
    public enum YesNoDefaultType
    {
        /// <summary>Value not set; equivalent to null for reference types.</summary>
        NotSet,

        /// <summary>The default value.</summary>
        Default,

        /// <summary>The no value.</summary>
        No,

        /// <summary>The yes value.</summary>
        Yes,

        /// <summary>Not a valid yes, no or default value.</summary>
        IllegalValue,
    }

    /// <summary>
    /// Yes, No, Always xml simple type.
    /// </summary>
    public enum YesNoAlwaysType
    {
        /// <summary>Value not set; equivalent to null for reference types.</summary>
        NotSet,

        /// <summary>The always value.</summary>
        Always,

        /// <summary>The no value.</summary>
        No,

        /// <summary>The yes value.</summary>
        Yes,

        /// <summary>Not a valid yes, no or always value.</summary>
        IllegalValue,
    }

    /// <summary>
    /// A set of rules describing the whitespace rules for an attribute.
    /// </summary>
    public enum EmptyRule
    {
        /// <summary>
        /// The trimmed value cannot be empty.
        /// </summary>
        MustHaveNonWhitespaceCharacters,

        /// <summary>
        /// The trimmed value can be empty, but the value itself cannot be empty.
        /// </summary>
        CanBeWhitespaceOnly,

        /// <summary>
        /// The value can be empty.
        /// </summary>
        CanBeEmpty
    }

    public enum ValueListKind
    {
        /// <summary>
        /// A list of values with nothing before the final value.
        /// </summary>
        None,

        /// <summary>
        /// A list of values with 'and' before the final value.
        /// </summary>
        And,

        /// <summary>
        /// A list of values with 'or' before the final value.
        /// </summary>
        Or
    }

    /// <summary>
    /// Core class for the compiler.
    /// </summary>
    public sealed class CompilerCore : IMessageHandler
    {
        internal const string W3SchemaPrefix = "http://www.w3.org/";
        public const int IntegerNotSet = int.MinValue;
        public const int IllegalInteger = int.MinValue + 1;
        public const int DefaultMaximumUncompressedMediaSize = 200; // Default value is 200 MB
        public const int MinValueOfMaxCabSizeForLargeFileSplitting = 20; // 20 MB
        public const int MaxValueOfMaxCabSizeForLargeFileSplitting = 2 * 1024; // 2048 MB (i.e. 2 GB)
        public const long LongNotSet = long.MinValue;
        public const long IllegalLong = long.MinValue + 1;
        public const string IllegalGuid = "IllegalGuid";
        public static readonly Version IllegalVersion = new Version(Int32.MaxValue, Int32.MaxValue, Int32.MaxValue, Int32.MaxValue);

        private static readonly Regex AmbiguousFilename = new Regex(@"^.{6}\~\d", RegexOptions.Compiled);

        private const string LegalShortFilenameCharacters = @"[^\\\?|><:/\*""\+,;=\[\]\. ]"; // illegal: \ ? | > < : / * " + , ; = [ ] . (space)
        private static readonly Regex LegalShortFilename = new Regex(String.Concat("^", LegalShortFilenameCharacters, @"{1,8}(\.", LegalShortFilenameCharacters, "{0,3})?$"), RegexOptions.Compiled);

        private const string IllegalLongFilenameCharacters = @"[\\\?|><:/\*""]"; // illegal: \ ? | > < : / * "
        private static readonly Regex IllegalLongFilename = new Regex(IllegalLongFilenameCharacters, RegexOptions.Compiled);

        private const string LegalLongFilenameCharacters = @"[^\\\?|><:/\*""]";  // opposite of illegal above.
        private static readonly Regex LegalLongFilename = new Regex(String.Concat("^", LegalLongFilenameCharacters, @"{1,259}$"), RegexOptions.Compiled);

        private const string LegalRelativeLongFilenameCharacters = @"[^\?|><:/\*""]"; // (like legal long, but we allow '\') illegal: ? | > < : / * "
        private static readonly Regex LegalRelativeLongFilename = new Regex(String.Concat("^", LegalRelativeLongFilenameCharacters, @"{1,259}$"), RegexOptions.Compiled);

        private const string LegalWildcardShortFilenameCharacters = @"[^\\|><:/""\+,;=\[\]\. ]"; // illegal: \ | > < : / " + , ; = [ ] . (space)
        private static readonly Regex LegalWildcardShortFilename = new Regex(String.Concat("^", LegalWildcardShortFilenameCharacters, @"{1,16}(\.", LegalWildcardShortFilenameCharacters, "{0,6})?$"));

        private const string LegalWildcardLongFilenameCharacters = @"[^\\|><:/""]"; // illegal: \ | > < : / "
        private static readonly Regex LegalWildcardLongFilename = new Regex(String.Concat("^", LegalWildcardLongFilenameCharacters, @"{1,259}$"));

        private static readonly Regex PutGuidHere = new Regex(@"PUT\-GUID\-(?:\d+\-)?HERE", RegexOptions.Singleline);

        private static readonly List<string> DisallowedMsiProperties = new List<string>(
            new string[] { "ACTION", "ADDLOCAL", "ADDSOURCE", "ADDDEFAULT", "ADVERTISE", "ALLUSERS", "REBOOT", "REINSTALL", "REINSTALLMODE", "REMOVE" });

        // Built-in variables (from burn\engine\variable.cpp, "vrgBuiltInVariables", around line 113)
        private static readonly List<String> BuiltinBundleVariables = new List<string>(
            new string[] {
                "AdminToolsFolder",
                "AppDataFolder",
                "CommonAppDataFolder",
                "CommonFiles64Folder",
                "CommonFilesFolder",
                "CompatibilityMode",
                "Date",
                "DesktopFolder",
                "FavoritesFolder",
                "FontsFolder",
                "InstallerName",
                "InstallerVersion",
                "LocalAppDataFolder",
                "LogonUser",
                "MyPicturesFolder",
                "NTProductType",
                "NTSuiteBackOffice",
                "NTSuiteDataCenter",
                "NTSuiteEnterprise",
                "NTSuitePersonal",
                "NTSuiteSmallBusiness",
                "NTSuiteSmallBusinessRestricted",
                "NTSuiteWebServer",
                "PersonalFolder",
                "Privileged",
                "ProgramFiles64Folder",
                "ProgramFiles6432Folder",
                "ProgramFilesFolder",
                "ProgramMenuFolder",
                "RebootPending",
                "SendToFolder",
                "ServicePackLevel",
                "StartMenuFolder",
                "StartupFolder",
                "System64Folder",
                "SystemFolder",
                "TempFolder",
                "TemplateFolder",
                "TerminalServer",
                "UserUILanguageID",
                "UserLanguageID",
                "VersionMsi",
                "VersionNT",
                "VersionNT64",
                "WindowsFolder",
                "WindowsVolume",
                "WixBundleAction",
                "WixBundleForcedRestartPackage",
                "WixBundleElevated",
                "WixBundleInstalled",
                "WixBundleProviderKey",
                "WixBundleTag",
                "WixBundleVersion",
            });

        private TableDefinitionCollection tableDefinitions;
        private Hashtable extensions;
        private Intermediate intermediate;
        private bool showPedanticMessages;

        private Section activeSection;
        private bool encounteredError;
        private XmlSchema schema;

        private Platform currentPlatform;
        private bool fipsCompliant;

        /// <summary>
        /// Constructor for all compiler core.
        /// </summary>
        /// <param name="intermediate">The Intermediate object representing compiled source document.</param>
        /// <param name="tableDefinitions">The loaded table definition collection.</param>
        /// <param name="extensions">The WiX extensions collection.</param>
        /// <param name="messageHandler">The message handler.</param>
        internal CompilerCore(Intermediate intermediate, TableDefinitionCollection tableDefinitions, Hashtable extensions, MessageEventHandler messageHandler, XmlSchema schema)
        {
            this.tableDefinitions = tableDefinitions;
            this.extensions = extensions;
            this.intermediate = intermediate;
            this.MessageHandler = messageHandler;
            this.schema = schema;
        }

        /// <summary>
        /// Event for messages.
        /// </summary>
        private event MessageEventHandler MessageHandler;

        /// <summary>
        /// Gets the section the compiler is currently emitting symbols into.
        /// </summary>
        /// <value>The section the compiler is currently emitting symbols into.</value>
        public Section ActiveSection
        {
            get { return this.activeSection; }
        }

        /// <summary>
        /// Gets or sets the platform which the compiler will use when defaulting 64-bit attributes and elements.
        /// </summary>
        /// <value>The platform which the compiler will use when defaulting 64-bit attributes and elements.</value>
        public Platform CurrentPlatform
        {
            get { return this.currentPlatform; }
            set { this.currentPlatform = value; }
        }

        /// <summary>
        /// Gets whether the compiler core encountered an error while processing.
        /// </summary>
        /// <value>Flag if core encountered an error during processing.</value>
        public bool EncounteredError
        {
            get { return this.encounteredError; }
        }

        /// <summary>
        /// Gets or sets the option to show pedantic messages.
        /// </summary>
        /// <value>The option to show pedantic messages.</value>
        public bool ShowPedanticMessages
        {
            get { return this.showPedanticMessages; }
            set { this.showPedanticMessages = value; }
        }

        /// <summary>
        /// Gets or sets if the compiler should use FIPS compliant algorithms.
        /// </summary>
        /// <value>true if the compiler should use FIPS compliant algorithms.</value>
        public bool FipsCompliant
        {
            get { return this.fipsCompliant; }
            set { this.fipsCompliant = value; }
        }

        /// <summary>
        /// Gets the table definitions used by the compiler core.
        /// </summary>
        /// <value>Table definition collection.</value>
        public TableDefinitionCollection TableDefinitions
        {
            get { return this.tableDefinitions; }
        }

        /// <summary>
        /// Convert a bit array into an int value.
        /// </summary>
        /// <param name="bits">The bit array to convert.</param>
        /// <returns>The converted int value.</returns>
        public static int ConvertBitArrayToInt32(BitArray bits)
        {
            if (null == bits)
            {
                throw new ArgumentNullException("bits");
            }

            Debug.Assert(32 == bits.Length);

            int[] intArray = new int[1];

            bits.CopyTo(intArray, 0);

            return intArray[0];
        }

        /// <summary>
        /// Sets a bit in a bit array based on the index at which an attribute name was found in a string array.
        /// </summary>
        /// <param name="attributeNames">Array of attributes that map to bits.</param>
        /// <param name="attributeName">Name of attribute to check.</param>
        /// <param name="attributeValue">Value of attribute to check.</param>
        /// <param name="bits">The bit array in which the bit will be set if found.</param>
        /// <param name="offset">The offset into the bit array.</param>
        /// <returns>true if the bit was set; false otherwise.</returns>
        public static bool NameToBit(string[] attributeNames, string attributeName, YesNoType attributeValue, BitArray bits, int offset)
        {
            for (int i = 0; i < attributeNames.Length; i++)
            {
                if (attributeNames[i] == attributeName)
                {
                    if (YesNoType.Yes == attributeValue)
                    {
                        bits.Set(i + offset, true);
                    }

                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Verifies that a filename is ambiguous.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <returns>true if the filename is ambiguous; false otherwise.</returns>
        public static bool IsAmbiguousFilename(string filename)
        {
            if (null == filename || 0 == filename.Length)
            {
                return false;
            }

            return CompilerCore.AmbiguousFilename.IsMatch(filename);
        }

        /// <summary>
        /// Verifies that a value is a legal identifier.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <returns>true if the value is an identifier; false otherwise.</returns>
        public static bool IsIdentifier(string value)
        {
            return Common.IsIdentifier(value);
        }

        /// <summary>
        /// Verifies if an identifier is a valid loc identifier.
        /// </summary>
        /// <param name="identifier">Identifier to verify.</param>
        /// <returns>True if the identifier is a valid loc identifier.</returns>
        public static bool IsValidLocIdentifier(string identifier)
        {
            if (null == identifier || 0 == identifier.Length)
            {
                return false;
            }

            Match match = Common.WixVariableRegex.Match(identifier);

            return (match.Success && "loc" == match.Groups["namespace"].Value && 0 == match.Index && identifier.Length == match.Length);
        }

        /// <summary>
        /// Verifies if a filename is a valid short filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>True if the filename is a valid short filename</returns>
        public static bool IsValidShortFilename(string filename, bool allowWildcards)
        {
            if (null == filename || 0 == filename.Length)
            {
                return false;
            }

            if (allowWildcards)
            {
                if (CompilerCore.LegalWildcardShortFilename.IsMatch(filename))
                {
                    bool foundPeriod = false;
                    int beforePeriod = 0;
                    int afterPeriod = 0;

                    // count the number of characters before and after the period
                    // '*' is not counted because it may represent zero characters
                    foreach (char character in filename)
                    {
                        if ('.' == character)
                        {
                            foundPeriod = true;
                        }
                        else if ('*' != character)
                        {
                            if (foundPeriod)
                            {
                                afterPeriod++;
                            }
                            else
                            {
                                beforePeriod++;
                            }
                        }
                    }

                    if (8 >= beforePeriod && 3 >= afterPeriod)
                    {
                        return true;
                    }
                }

                return false;
            }
            else
            {
                return CompilerCore.LegalShortFilename.IsMatch(filename);
            }
        }

        /// <summary>
        /// Verifies if a filename is a valid long filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>True if the filename is a valid long filename</returns>
        public static bool IsValidLongFilename(string filename, bool allowWildcards)
        {
            return IsValidLongFilename(filename, allowWildcards, false);
        }

        /// <summary>
        /// Verifies if a filename is a valid long filename.
        /// </summary>
        /// <param name="filename">Filename to verify.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="allowRelative">true if relative paths are allowed in the filename.</param>
        /// <returns>True if the filename is a valid long filename</returns>
        public static bool IsValidLongFilename(string filename, bool allowWildcards, bool allowRelative)
        {
            if (null == filename || 0 == filename.Length)
            {
                return false;
            }

            // check for a non-period character (all periods is not legal)
            bool nonPeriodFound = false;
            foreach (char character in filename)
            {
                if ('.' != character)
                {
                    nonPeriodFound = true;
                    break;
                }
            }

            if (allowWildcards)
            {
                return (nonPeriodFound && CompilerCore.LegalWildcardLongFilename.IsMatch(filename));
            }
            else if (allowRelative)
            {
                return (nonPeriodFound && CompilerCore.LegalRelativeLongFilename.IsMatch(filename));
            }
            else
            {
                return (nonPeriodFound && CompilerCore.LegalLongFilename.IsMatch(filename));
            }
        }

        /// <summary>
        /// Replaces the illegal filename characters to create a legal name.
        /// </summary>
        /// <param name="filename">Filename to make valid.</param>
        /// <param name="replace">Replacement string for invalid characters in filename.</param>
        /// <returns>Valid filename.</returns>
        public static string MakeValidLongFileName(string filename, string replace)
        {
            return CompilerCore.IllegalLongFilename.Replace(filename, replace);
        }

        /// <summary>
        /// Generates a short file/directory name using an identifier and long file/directory name as input.
        /// </summary>
        /// <param name="longName">The long file/directory name.</param>
        /// <param name="keepExtension">The option to keep the extension on generated short names.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="args">Any additional information to include in the hash for the generated short name.</param>
        /// <returns>The generated 8.3-compliant short file/directory name.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the File table is generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        public string GenerateShortName(string longName, bool keepExtension, bool allowWildcards, params string[] args)
        {
            if (String.IsNullOrEmpty(longName))
            {
                throw new ArgumentNullException("longName");
            }

            Debug.Assert(null != longName);

            // canonicalize the long name if its not a localization identifier (they are case-sensitive)
            if (!CompilerCore.IsValidLocIdentifier(longName))
            {
                longName = longName.ToLower(CultureInfo.InvariantCulture);
            }

            // collect all the data
            ArrayList strings = new ArrayList();
            strings.Add(longName);
            strings.AddRange(args);

            // prepare for hashing
            string stringData = String.Join("|", (string[])strings.ToArray(typeof(string)));
            byte[] data = Encoding.Unicode.GetBytes(stringData);

            // hash the data
            byte[] hash;

            if (this.fipsCompliant)
            {
                using (SHA1 sha1 = new SHA1CryptoServiceProvider())
                {
                    hash = sha1.ComputeHash(data);
                }
            }
            else
            {
                using (MD5 md5 = new MD5CryptoServiceProvider())
                {
                    hash = md5.ComputeHash(data);
                }
            }


            // generate the short file/directory name without an extension
            StringBuilder shortName = new StringBuilder(Convert.ToBase64String(hash));
            shortName.Remove(8, shortName.Length - 8);
            shortName.Replace('/', '_');
            shortName.Replace('+', '-');

            if (keepExtension)
            {
                string extension = Path.GetExtension(longName);

                if (4 < extension.Length)
                {
                    extension = extension.Substring(0, 4);
                }

                shortName.Append(extension);

                // check the generated short name to ensure its still legal (the extension may not be legal)
                if (!CompilerCore.IsValidShortFilename(shortName.ToString(), allowWildcards))
                {
                    // remove the extension (by truncating the generated file name back to the generated characters)
                    shortName.Length -= extension.Length;
                }
            }

            return shortName.ToString().ToLower(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Verifies the given string is a valid product version.
        /// </summary>
        /// <param name="version">The product version to verify.</param>
        /// <returns>True if version is a valid product version</returns>
        public static bool IsValidProductVersion(string version)
        {
            if (!Common.IsValidBinderVariable(version))
            {
                Version ver = new Version(version);

                if (255 < ver.Major || 255 < ver.Minor || 65535 < ver.Build)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Verifies the given string is a valid module or bundle version.
        /// </summary>
        /// <param name="version">The version to verify.</param>
        /// <returns>True if version is a valid module or bundle version.</returns>
        public static bool IsValidModuleOrBundleVersion(string version)
        {
            return Common.IsValidModuleOrBundleVersion(version);
        }

        /// <summary>
        /// Get an node's inner text and trims any extra whitespace.
        /// </summary>
        /// <param name="node">The node with inner text to be trimmed.</param>
        /// <returns>The node's inner text trimmed.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public static string GetTrimmedInnerText(XmlNode node)
        {
            string value = node.InnerText;
            if (0 < value.Length)
            {
                value = value.Trim();
            }

            return value;
        }

        /// <summary>
        /// Gets node's inner text and ensure's it is safe for use in a condition by trimming any extra whitespace.
        /// </summary>
        /// <param name="node">The node to ensure inner text is a condition.</param>
        /// <returns>The value converted into a safe condition.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public static string GetConditionInnerText(XmlNode node)
        {
            string value = node.InnerText;
            if (0 < value.Length)
            {
                value = value.Trim();
                value = value.Replace('\t', ' ');
                value = value.Replace('\r', ' ');
                value = value.Replace('\n', ' ');
            }
            else // return null for a non-existant condition
            {
                value = null;
            }

            return value;
        }

        /// <summary>
        /// Creates a row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        /// <param name="tableName">Name of table to create row in.</param>
        /// <returns>New row.</returns>
        public Row CreateRow(SourceLineNumberCollection sourceLineNumbers, string tableName)
        {
            return this.CreateRow(sourceLineNumbers, tableName, this.activeSection);
        }

        /// <summary>
        /// Creates a row in the active given <paramref name="section"/>.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        /// <param name="tableName">Name of table to create row in.</param>
        /// <param name="section">The section to which the row is added. If null, the row is added to the active section.</param>
        /// <returns>New row.</returns>
        internal Row CreateRow(SourceLineNumberCollection sourceLineNumbers, string tableName, Section section)
        {
            if (null == section)
            {
                throw new ArgumentNullException("section");
            }

            TableDefinition tableDefinition = this.tableDefinitions[tableName];
            Table table = section.Tables.EnsureTable(section, tableDefinition);

            return table.CreateRow(sourceLineNumbers);
        }

        /// <summary>
        /// Creates a WixVariable row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        public void CreateWixVariableRow(SourceLineNumberCollection sourceLineNumbers, string id, string value, bool overridable)
        {
            WixVariableRow wixVariableRow = (WixVariableRow)this.CreateRow(sourceLineNumbers, "WixVariable");
            wixVariableRow.Id = id;
            wixVariableRow.Value = value;
            wixVariableRow.Overridable = overridable;
        }

        /// <summary>
        /// Creates a (bundle) Variable row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        public void CreateVariableRow(SourceLineNumberCollection sourceLineNumbers, string name, string value, string type, bool hidden, bool persisted)
        {
            VariableRow variableRow = (VariableRow)this.CreateRow(sourceLineNumbers, "Variable");
            variableRow.Id = name;
            variableRow.Value = value;
            variableRow.Type = type;
            variableRow.Hidden = hidden;
            variableRow.Persisted = persisted;
        }

        /// <summary>
        /// Creates a WixApprovedExeForElevation row in the active session.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        public void CreateWixApprovedExeForElevationRow(SourceLineNumberCollection sourceLineNumbers, string id, string key, string valueName, BundleApprovedExeForElevationAttributes attributes)
        {
            WixApprovedExeForElevationRow wixApprovedExeForElevationRow = (WixApprovedExeForElevationRow)this.CreateRow(sourceLineNumbers, "WixApprovedExeForElevation");
            wixApprovedExeForElevationRow.Id = id;
            wixApprovedExeForElevationRow.Key = key;
            wixApprovedExeForElevationRow.ValueName = valueName;
            wixApprovedExeForElevationRow.Attributes = attributes;
        }

        /// <summary>
        /// Creates a WixCatalog row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        public void CreateWixCatalogRow(SourceLineNumberCollection sourceLineNumbers, string id, string sourceFile)
        {
            WixCatalogRow wixCatalogRow = (WixCatalogRow)this.CreateRow(sourceLineNumbers, "WixCatalog");
            wixCatalogRow.Id = id;
            wixCatalogRow.SourceFile = sourceFile;
        }

        /// <summary>
        /// Adds a patch resource reference to the list of resoures to be filtered when producing a patch. This method should only be used when processing children of a patch family.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of current row.</param>
        /// <param name="tableName">Name of table to create row in.</param>
        /// <param name="primaryKey">Array of keys that make up the primary key of the table.</param>
        /// <returns>New row.</returns>
        public void AddPatchFamilyChildReference(SourceLineNumberCollection sourceLineNumbers, string tableName, string[] primaryKey)
        {
            string joinedPrimaryKey = String.Join("/", primaryKey);
            Row patchReferenceRow = this.CreateRow(sourceLineNumbers, "WixPatchRef");
            patchReferenceRow[0] = tableName;
            patchReferenceRow[1] = joinedPrimaryKey;
        }

        /// <summary>
        /// Creates a Registry row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of the current row.</param>
        /// <param name="root">The registry entry root.</param>
        /// <param name="key">The registry entry key.</param>
        /// <param name="name">The registry entry name.</param>
        /// <param name="value">The registry entry value.</param>
        /// <param name="componentId">The component which will control installation/uninstallation of the registry entry.</param>
        /// <param name="escapeLeadingHash">If true, "escape" leading '#' characters so the value is written as a REG_SZ.</param>
        [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase", Justification = "Changing the way this string normalizes would result " +
                         "in a change to the way the Registry table is generated, potentially causing extra churn in patches on an MSI built from an older version of WiX. " +
                         "Furthermore, there is no security hole here, as the strings won't need to make a round trip")]
        public string CreateRegistryRow(SourceLineNumberCollection sourceLineNumbers, int root, string key, string name, string value, string componentId, bool escapeLeadingHash)
        {
            string id = null;

            if (!this.EncounteredError)
            {
                if (-1 > root || 3 < root)
                {
                    throw new ArgumentOutOfRangeException("root");
                }

                if (null == key)
                {
                    throw new ArgumentNullException("key");
                }

                if (null == componentId)
                {
                    throw new ArgumentNullException("componentId");
                }

                // escape the leading '#' character for string registry values
                if (escapeLeadingHash && null != value && value.StartsWith("#", StringComparison.Ordinal))
                {
                    value = String.Concat("#", value);
                }

                id = this.GenerateIdentifier("reg", componentId, root.ToString(CultureInfo.InvariantCulture.NumberFormat), key.ToLower(CultureInfo.InvariantCulture), (null != name ? name.ToLower(CultureInfo.InvariantCulture) : name));
                Row row = this.CreateRow(sourceLineNumbers, "Registry");
                row[0] = id;
                row[1] = root;
                row[2] = key;
                row[3] = name;
                row[4] = value;
                row[5] = componentId;
            }

            return id;
        }

        /// <summary>
        /// Creates a Registry row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source and line number of the current row.</param>
        /// <param name="root">The registry entry root.</param>
        /// <param name="key">The registry entry key.</param>
        /// <param name="name">The registry entry name.</param>
        /// <param name="value">The registry entry value.</param>
        /// <param name="componentId">The component which will control installation/uninstallation of the registry entry.</param>
        public string CreateRegistryRow(SourceLineNumberCollection sourceLineNumbers, int root, string key, string name, string value, string componentId)
        {
            return this.CreateRegistryRow(sourceLineNumbers, root, key, name, value, componentId, true);
        }

        /// <summary>
        /// Create a WixSimpleReference row in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="tableName">The table name of the simple reference.</param>
        /// <param name="primaryKeys">The primary keys of the simple reference.</param>
        public void CreateWixSimpleReferenceRow(SourceLineNumberCollection sourceLineNumbers, string tableName, params string[] primaryKeys)
        {
            if (!this.EncounteredError)
            {
                WixSimpleReferenceRow wixSimpleReferenceRow = (WixSimpleReferenceRow)this.CreateRow(sourceLineNumbers, "WixSimpleReference");
                wixSimpleReferenceRow.TableName = tableName;
                wixSimpleReferenceRow.PrimaryKeys = String.Join("/", primaryKeys);
            }
        }

        /// <summary>
        /// A row in the WixGroup table is added for this child node and its parent node.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information for the row.</param>
        /// <param name="parentType">Type of child's complex reference parent.</param>
        /// <param name="parentId">Id of the parenet node.</param>
        /// <param name="childType">Complex reference type of child</param>
        /// <param name="childId">Id of the Child Node.</param>
        public void CreateWixGroupRow(SourceLineNumberCollection sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, ComplexReferenceChildType childType, string childId)
        {
            if (!this.EncounteredError)
            {
                if (null == parentId || ComplexReferenceParentType.Unknown == parentType)
                {
                    return;
                }

                if (null == childId)
                {
                    throw new ArgumentNullException("childId");
                }

                Row WixGroupRow = this.CreateRow(sourceLineNumbers, "WixGroup");
                WixGroupRow[0] = parentId;
                WixGroupRow[1] = Enum.GetName(typeof(ComplexReferenceParentType), parentType);
                WixGroupRow[2] = childId;
                WixGroupRow[3] = Enum.GetName(typeof(ComplexReferenceChildType), childType);
           }
        }

        /// <summary>
        /// Add the appropriate rows to make sure that the given table shows up
        /// in the resulting output.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line numbers.</param>
        /// <param name="tableName">Name of the table to ensure existance of.</param>
        public void EnsureTable(SourceLineNumberCollection sourceLineNumbers, string tableName)
        {
            if (!this.EncounteredError)
            {
                Row row = this.CreateRow(sourceLineNumbers, "WixEnsureTable");
                row[0] = tableName;

                // We don't add custom table definitions to the tableDefinitions collection,
                // so if it's not in there, it better be a custom table. If the Id is just wrong,
                // instead of a custom table, we get an unresolved reference at link time.
                if (!this.tableDefinitions.Contains(tableName))
                {
                    this.CreateWixSimpleReferenceRow(sourceLineNumbers, "WixCustomTable", tableName);
                }
            }
        }

        /// <summary>
        /// Get an attribute value and displays an error if the value is empty.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            return Common.GetAttributeValue(sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly, this.OnMessage);
        }

        /// <summary>
        /// Get an attribute value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="canBeEmpty">If true, no error is raised on empty value. If false, an error is raised.</param>
        /// <returns>The attribute's value.</returns>
        /// <remarks>Provided for backwards compatibility.</remarks>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool canBeEmpty)
        {
            return Common.GetAttributeValue(sourceLineNumbers, attribute, canBeEmpty ? EmptyRule.CanBeEmpty : EmptyRule.CanBeWhitespaceOnly, this.OnMessage);
        }

        /// <summary>
        /// Get an attribute value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="emptyRule">A rule for the contents of the value. If the contents do not follow the rule, an error is thrown.</param>
        /// <returns>The attribute's value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, EmptyRule emptyRule)
        {
            return Common.GetAttributeValue(sourceLineNumbers, attribute, emptyRule, this.OnMessage);
        }

        /// <summary>
        /// Get a valid code page by web name or number from a string attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>A valid code page integer value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public int GetAttributeCodePageValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            try
            {
                int codePage = Common.GetValidCodePage(value);
                return codePage;
            }
            catch (NotSupportedException)
            {
                this.OnMessage(WixErrors.IllegalCodepageAttribute(sourceLineNumbers, value, attribute.OwnerElement.Name, attribute.Name));
            }

            return IllegalInteger;
        }

                /// <summary>
        /// Get a valid code page by web name or number from a string attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>A valid code page integer value or variable expression.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeLocalizableCodePageValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            return this.GetAttributeLocalizableCodePageValue(sourceLineNumbers, attribute, false);
        }

        /// <summary>
        /// Get a valid code page by web name or number from a string attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="onlyAscii">Whether to allow Unicode (UCS) or UTF code pages.</param>
        /// <returns>A valid code page integer value or variable expression.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeLocalizableCodePageValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool onlyAnsi)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            // allow for localization of code page names and values
            if (IsValidLocIdentifier(value))
            {
                return value;
            }

            try
            {
                int codePage = Common.GetValidCodePage(value, false, onlyAnsi, sourceLineNumbers);
                return codePage.ToString(CultureInfo.InvariantCulture);
            }
            catch (NotSupportedException)
            {
                // not a valid windows code page
                this.OnMessage(WixErrors.IllegalCodepageAttribute(sourceLineNumbers, value, attribute.OwnerElement.Name, attribute.Name));
            }
            catch (WixException e)
            {
                this.OnMessage(e.Error);
            }

            return null;
        }

        /// <summary>
        /// Get an integer attribute value and displays an error for an illegal integer value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's integer value or a special value if an error occurred during conversion.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public int GetAttributeIntegerValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, int minimum, int maximum)
        {
            return Common.GetAttributeIntegerValue(sourceLineNumbers, attribute, minimum, maximum, this.OnMessage);
        }

        /// <summary>
        /// Get a long integral attribute value and displays an error for an illegal long value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's long value or a special value if an error occurred during conversion.</returns>
        public long GetAttributeLongValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, long minimum, long maximum)
        {
            Debug.Assert(minimum > LongNotSet && minimum > IllegalLong, "The legal values for this attribute collide with at least one sentinel used during parsing.");

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                try
                {
                    long longValue = Convert.ToInt64(value, CultureInfo.InvariantCulture.NumberFormat);

                    if (LongNotSet == longValue || IllegalLong == longValue)
                    {
                        this.OnMessage(WixErrors.IntegralValueSentinelCollision(sourceLineNumbers, longValue));
                    }
                    else if (minimum > longValue || maximum < longValue)
                    {
                        this.OnMessage(WixErrors.IntegralValueOutOfRange(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, longValue, minimum, maximum));
                        longValue = IllegalLong;
                    }

                    return longValue;
                }
                catch (FormatException)
                {
                    this.OnMessage(WixErrors.IllegalLongValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
                catch (OverflowException)
                {
                    this.OnMessage(WixErrors.IllegalLongValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
            }

            return IllegalLong;
        }

        /// <summary>
        /// Get a date time attribute value and display errors for illegal values.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>Int representation of the date time.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public int GetAttributeDateTimeValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                try
                {
                    DateTime date = DateTime.Parse(value, CultureInfo.InvariantCulture.DateTimeFormat);

                    return ((((date.Year - 1980) * 512) + (date.Month * 32 + date.Day)) * 65536) +
                        (date.Hour * 2048) + (date.Minute * 32) + (date.Second / 2);
                }
                catch (ArgumentOutOfRangeException)
                {
                    this.OnMessage(WixErrors.InvalidDateTimeFormat(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
                catch (FormatException)
                {
                    this.OnMessage(WixErrors.InvalidDateTimeFormat(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
                catch (OverflowException)
                {
                    this.OnMessage(WixErrors.InvalidDateTimeFormat(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
            }

            return IllegalInteger;
        }

        /// <summary>
        /// Get an integer attribute value or localize variable and displays an error for
        /// an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <returns>The attribute's integer value or localize variable as a string or a special value if an error occurred during conversion.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeLocalizableIntegerValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, int minimum, int maximum)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            Debug.Assert(minimum > IntegerNotSet && minimum > IllegalInteger, "The legal values for this attribute collide with at least one sentinel used during parsing.");

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (IsValidLocIdentifier(value) || Common.IsValidBinderVariable(value))
                {
                    return value;
                }
                else
                {
                    try
                    {
                        int integer = Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat);

                        if (IntegerNotSet == integer || IllegalInteger == integer)
                        {
                            this.OnMessage(WixErrors.IntegralValueSentinelCollision(sourceLineNumbers, integer));
                        }
                        else if (minimum > integer || maximum < integer)
                        {
                            this.OnMessage(WixErrors.IntegralValueOutOfRange(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, integer, minimum, maximum));
                            integer = IllegalInteger;
                        }

                        return value;
                    }
                    catch (FormatException)
                    {
                        this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                    }
                    catch (OverflowException)
                    {
                        this.OnMessage(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Get a guid attribute value and displays an error for an illegal guid value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="generatable">Determines whether the guid can be automatically generated.</param>
        /// <returns>The attribute's guid value or a special value if an error occurred.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeGuidValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool generatable)
        {
            return this.GetAttributeGuidValue(sourceLineNumbers, attribute, generatable, false);
        }

        /// <summary>
        /// Get a guid attribute value and displays an error for an illegal guid value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="generatable">Determines whether the guid can be automatically generated.</param>
        /// <param name="canBeEmpty">If true, no error is raised on empty value. If false, an error is raised.</param>
        /// <returns>The attribute's guid value or a special value if an error occurred.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        [SuppressMessage("Microsoft.Performance", "CA1807:AvoidUnnecessaryStringCreation")]
        public string GetAttributeGuidValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool generatable, bool canBeEmpty)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute, canBeEmpty);

            if (0 == value.Length && canBeEmpty)
            {
                return value;
            }
            else if (0 < value.Length)
            {
                // If the value starts and ends with braces or parenthesis, accept that and strip them off.
                if ((value.StartsWith("{", StringComparison.Ordinal) && value.EndsWith("}", StringComparison.Ordinal))
                    || (value.StartsWith("(", StringComparison.Ordinal) && value.EndsWith(")", StringComparison.Ordinal)))
                {
                    value = value.Substring(1, value.Length - 2);
                }

                try
                {
                    Guid guid;

                    if (generatable)
                    {
                        // question mark syntax is for backwards-compatibility until its removed
                        if ("????????-????-????-????-????????????" == value)
                        {
                            this.OnMessage(WixWarnings.DeprecatedQuestionMarksGuid(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name));
                            return "*";
                        }
                        else if ("*" == value)
                        {
                            return value;
                        }
                    }

                    if (CompilerCore.PutGuidHere.IsMatch(value))
                    {
                        this.OnMessage(WixErrors.ExampleGuid(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                        return IllegalGuid;
                    }
                    else if (value.StartsWith("!(loc", StringComparison.Ordinal) || value.StartsWith("$(loc", StringComparison.Ordinal) || value.StartsWith("!(wix", StringComparison.Ordinal))
                    {
                        return value;
                    }
                    else
                    {
                        guid = new Guid(value);
                    }

                    string uppercaseGuid = guid.ToString().ToUpper(CultureInfo.InvariantCulture);

                    if (this.showPedanticMessages)
                    {
                        if (uppercaseGuid != value)
                        {
                            this.OnMessage(WixErrors.GuidContainsLowercaseLetters(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                        }
                    }

                    return String.Concat("{", uppercaseGuid, "}");
                }
                catch (FormatException)
                {
                    this.OnMessage(WixErrors.IllegalGuidValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
            }

            return IllegalGuid;
        }

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeIdentifierValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            return Common.GetAttributeIdentifierValue(sourceLineNumbers, attribute, this.OnMessage);
        }

        /// <summary>
        /// Gets a yes/no value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public YesNoType GetAttributeYesNoValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                switch (Wix.Enums.ParseYesNoType(value))
                {
                    case Wix.YesNoType.no:
                        return YesNoType.No;
                    case Wix.YesNoType.yes:
                        return YesNoType.Yes;
                    case Wix.YesNoType.NotSet:
                        // Previous code never returned 'NotSet'!
                        break;
                    default:
                        this.OnMessage(WixErrors.IllegalYesNoValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                        break;
                }
            }

            return YesNoType.IllegalValue;
        }

        /// <summary>
        /// Gets a yes/no/default value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoDefaultType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public YesNoDefaultType GetAttributeYesNoDefaultValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                switch (Wix.Enums.ParseYesNoDefaultType(value))
                {
                    case Wix.YesNoDefaultType.@default:
                        return YesNoDefaultType.Default;
                    case Wix.YesNoDefaultType.no:
                        return YesNoDefaultType.No;
                    case Wix.YesNoDefaultType.yes:
                        return YesNoDefaultType.Yes;
                    case Wix.YesNoDefaultType.NotSet:
                        // Previous code never returned 'NotSet'!
                        break;
                    default:
                        this.OnMessage(WixErrors.IllegalYesNoDefaultValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                        break;
                }
            }

            return YesNoDefaultType.IllegalValue;
        }

        /// <summary>
        /// Gets a yes/no/always value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's YesNoAlwaysType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public YesNoAlwaysType GetAttributeYesNoAlwaysValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                switch (Wix.Enums.ParseYesNoAlwaysType(value))
                {
                    case Wix.YesNoAlwaysType.@always:
                        return YesNoAlwaysType.Always;
                    case Wix.YesNoAlwaysType.no:
                        return YesNoAlwaysType.No;
                    case Wix.YesNoAlwaysType.yes:
                        return YesNoAlwaysType.Yes;
                    case Wix.YesNoAlwaysType.NotSet:
                        // Previous code never returned 'NotSet'!
                        break;
                    default:
                        this.OnMessage(WixErrors.IllegalYesNoAlwaysValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                        break;
                }
            }

            return YesNoAlwaysType.IllegalValue;
        }

        /// <summary>
        /// Gets a short filename value and displays an error for an illegal short filename value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>The attribute's short filename value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeShortFilename(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool allowWildcards)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (!CompilerCore.IsValidShortFilename(value, allowWildcards) && !CompilerCore.IsValidLocIdentifier(value))
                {
                    this.OnMessage(WixErrors.IllegalShortFilename(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
                else if (CompilerCore.IsAmbiguousFilename(value))
                {
                    this.OnMessage(WixWarnings.AmbiguousFileOrDirectoryName(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
            }

            return value;
        }

        /// <summary>
        /// Gets a long filename value and displays an error for an illegal long filename value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <returns>The attribute's long filename value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeLongFilename(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool allowWildcards)
        {
            return GetAttributeLongFilename(sourceLineNumbers, attribute, allowWildcards, false);
        }

        /// <summary>
        /// Gets a long filename value and displays an error for an illegal long filename value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowWildcards">true if wildcards are allowed in the filename.</param>
        /// <param name="allowRelative">true if relative paths are allowed in the filename.</param>
        /// <returns>The attribute's long filename value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeLongFilename(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool allowWildcards, bool allowRelative)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (!CompilerCore.IsValidLongFilename(value, allowWildcards, allowRelative) && !CompilerCore.IsValidLocIdentifier(value))
                {
                    if (allowRelative)
                    {
                        this.OnMessage(WixErrors.IllegalRelativeLongFilename(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                    }
                    else
                    {
                        this.OnMessage(WixErrors.IllegalLongFilename(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                    }
                }
                else if (allowRelative)
                {
                    string normalizedPath = value.Replace('\\', '/');
                    if (normalizedPath.StartsWith("../", StringComparison.Ordinal) || normalizedPath.Contains("/../"))
                    {
                        this.OnMessage(WixWarnings.PayloadMustBeRelativeToCache(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                    }
                }
                else if (CompilerCore.IsAmbiguousFilename(value))
                {
                    this.OnMessage(WixWarnings.AmbiguousFileOrDirectoryName(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
            }

            return value;
        }

        /// <summary>
        /// Gets a version value or possibly a binder variable and displays an error for an illegal version value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowBinderVariable">Allow the version to be provided by a binder variable.</param>
        /// <returns>The attribute's version value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeVersionValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool allowBinderVariable)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (allowBinderVariable && Common.ContainsValidBinderVariable(value))
                {
                    return value;
                }
                else
                {
                    try
                    {
                        return new Version(value).ToString();
                    }
                    catch (FormatException) // illegal integer in version
                    {
                        this.OnMessage(WixErrors.IllegalVersionValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                    }
                    catch (ArgumentException)
                    {
                        this.OnMessage(WixErrors.IllegalVersionValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a RegistryRoot value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's RegisitryRootType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public Wix.RegistryRootType GetAttributeRegistryRootValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            return GetAttributeRegistryRootValue(sourceLineNumbers, attribute, true);
        }

        /// <summary>
        /// Gets a RegistryRoot value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowHkmu">Whether HKMU is considered a valid value.</param>
        /// <returns>The attribute's RegisitryRootType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public Wix.RegistryRootType GetAttributeRegistryRootValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool allowHkmu)
        {
            Wix.RegistryRootType registryRoot = Wix.RegistryRootType.NotSet;
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                registryRoot = Wix.Enums.ParseRegistryRootType(value);

                if (Wix.RegistryRootType.IllegalValue == registryRoot || (!allowHkmu && Wix.RegistryRootType.HKMU == registryRoot))
                {
                    // TODO: Find a way to expose the valid values programatically!
                    if (allowHkmu)
                    {
                        this.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value,
                            "HKMU", "HKCR", "HKCU", "HKLM", "HKU"));
                    }
                    else
                    {
                        this.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value,
                            "HKCR", "HKCU", "HKLM", "HKU"));
                    }
                }
            }

            return registryRoot;
        }

        /// <summary>
        /// Gets a RegistryRoot as a MsiInterop.MsidbRegistryRoot value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="allowHkmu">Whether HKMU is returned as -1 (true), or treated as an error (false).</param>
        /// <returns>The attribute's RegisitryRootType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public int GetAttributeMsidbRegistryRootValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, bool allowHkmu)
        {
            Wix.RegistryRootType registryRoot = this.GetAttributeRegistryRootValue(sourceLineNumbers, attribute, allowHkmu);

            switch (registryRoot)
            {
                case Wix.RegistryRootType.NotSet:
                    return CompilerCore.IntegerNotSet;
                case Wix.RegistryRootType.HKCR:
                    return Msi.Interop.MsiInterop.MsidbRegistryRootClassesRoot;
                case Wix.RegistryRootType.HKCU:
                    return Msi.Interop.MsiInterop.MsidbRegistryRootCurrentUser;
                case Wix.RegistryRootType.HKLM:
                    return Msi.Interop.MsiInterop.MsidbRegistryRootLocalMachine;
                case Wix.RegistryRootType.HKU:
                    return Msi.Interop.MsiInterop.MsidbRegistryRootUsers;
                case Wix.RegistryRootType.HKMU:
                    // This is gross, but there was *one* registry root parsing instance
                    // (in Compiler.ParseRegistrySearchElement()) that did not explicitly
                    // handle HKMU and it fell through to the default error case. The
                    // others treated it as -1, which is what we do here.
                    if (allowHkmu)
                    {
                        return -1;
                    }
                    break;
            }

            return CompilerCore.IntegerNotSet;
        }

        /// <summary>
        /// Gets an InstallUninstallType value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's InstallUninstallType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public Wix.InstallUninstallType GetAttributeInstallUninstallValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            Wix.InstallUninstallType installUninstall = Wix.InstallUninstallType.NotSet;
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                installUninstall = Wix.Enums.ParseInstallUninstallType(value);

                if (Wix.InstallUninstallType.IllegalValue == installUninstall)
                {
                    // TODO: Find a way to expose the valid values programatically!
                    this.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value,
                         "install", "uninstall", "both"));
                }
            }

            return installUninstall;
        }

        /// <summary>
        /// Gets an ExitType value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's ExitType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public Wix.ExitType GetAttributeExitValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            Wix.ExitType exitValue = Wix.ExitType.NotSet;
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                exitValue = Wix.Enums.ParseExitType(value);

                if (Wix.ExitType.IllegalValue == exitValue)
                {
                    // TODO: Find a way to expose the valid values programatically!
                    this.OnMessage(WixErrors.IllegalAttributeValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value,
                         "success", "cancel", "error", "suspend"));
                }
            }

            return exitValue;
        }

        /// <summary>
        /// Gets an MsiProperty name value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeMsiPropertyNameValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (CompilerCore.DisallowedMsiProperties.Contains(value))
                {
                    string illegalValues = CompilerCore.CreateValueList(ValueListKind.Or, CompilerCore.DisallowedMsiProperties);
                    this.OnMessage(WixWarnings.DisallowedMsiProperty(sourceLineNumbers, value, illegalValues));
                }
            }

            return value;
        }

        /// <summary>
        /// Gets a Bundle variable value and displays an error for an illegal value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <returns>The attribute's value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public string GetAttributeBundleVariableValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            string value = this.GetAttributeValue(sourceLineNumbers, attribute);

            if (0 < value.Length)
            {
                if (CompilerCore.BuiltinBundleVariables.Contains(value))
                {
                    string illegalValues = CompilerCore.CreateValueList(ValueListKind.Or, CompilerCore.BuiltinBundleVariables);
                    this.OnMessage(WixErrors.IllegalAttributeValueWithIllegalList(sourceLineNumbers,
                        attribute.OwnerElement.Name, attribute.Name, value, illegalValues));
                }
            }

            return value;
        }

        /// <summary>
        /// Checks if the string contains a property (i.e. "foo[Property]bar")
        /// </summary>
        /// <param name="possibleProperty">String to evaluate for properties.</param>
        /// <returns>True if a property is found in the string.</returns>
        public static bool ContainsProperty(string possibleProperty)
        {
            return Common.ContainsProperty(possibleProperty);
        }

        /// <summary>
        /// Return an identifier based on passed file name
        /// </summary>
        /// <param name="name">File name to generate identifer from</param>
        /// <returns></returns>
        public static string GetIdentifierFromName(string name)
        {
            return Common.GetIdentifierFromName(name);
        }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public string GenerateIdentifier(string prefix, params string[] args)
        {
            return Common.GenerateIdentifier(prefix, this.fipsCompliant, args);
        }

        /// <summary>
        /// Attempts to use an extension to parse the attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Current line number for element.</param>
        /// <param name="element">Element containing attribute to be parsed.</param>
        /// <param name="attribute">Attribute to be parsed.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public void ParseExtensionAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement element, XmlAttribute attribute)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            // ignore elements defined by the W3C because we'll assume they are always right
            if (!attribute.NamespaceURI.StartsWith(CompilerCore.W3SchemaPrefix, StringComparison.Ordinal))
            {
                CompilerExtension extension = this.FindExtension(attribute.NamespaceURI);

                if (null != extension)
                {
                    extension.ParseAttribute(sourceLineNumbers, element, attribute);
                }
                else
                {
                    this.OnMessage(WixErrors.UnhandledExtensionAttribute(sourceLineNumbers, element.Name, attribute.Name, attribute.NamespaceURI));
                }
            }
        }

        /// <summary>
        /// Attempts to use an extension to parse the attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Current line number for element.</param>
        /// <param name="element">Element containing attribute to be parsed.</param>
        /// <param name="attribute">Attribute to be parsed.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public void ParseExtensionAttribute(SourceLineNumberCollection sourceLineNumbers, XmlElement element, XmlAttribute attribute, Dictionary<string, string> contextValues)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            // ignore elements defined by the W3C because we'll assume they are always right
            if (!attribute.NamespaceURI.StartsWith(CompilerCore.W3SchemaPrefix, StringComparison.Ordinal))
            {
                CompilerExtension extension = this.FindExtension(attribute.NamespaceURI);

                if (null != extension)
                {
                    extension.ParseAttribute(sourceLineNumbers, element, attribute, contextValues);
                }
                else
                {
                    this.OnMessage(WixErrors.UnhandledExtensionAttribute(sourceLineNumbers, element.Name, attribute.Name, attribute.NamespaceURI));
                }
            }
        }

        /// <summary>
        /// Attempts to use an extension to parse the element.
        /// </summary>
        /// <param name="sourceLineNumbers">Current line number for element.</param>
        /// <param name="parentElement">Element containing element to be parsed.</param>
        /// <param name="element">Element to be parsed.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public void ParseExtensionElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element, params string[] contextValues)
        {
            CompilerExtension extension = this.FindExtension(element.NamespaceURI);

            if (null != extension)
            {
                extension.ParseElement(sourceLineNumbers, parentElement, element, contextValues);
            }
            else
            {
                SourceLineNumberCollection childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);

                this.OnMessage(WixErrors.UnhandledExtensionElement(childSourceLineNumbers, parentElement.Name, element.Name, element.NamespaceURI));
            }
        }

        /// <summary>
        /// Attempts to use an extension to parse the element, with support for setting component keypath.
        /// </summary>
        /// <param name="sourceLineNumbers">Current line number for element.</param>
        /// <param name="parentElement">Element containing element to be parsed.</param>
        /// <param name="element">Element to be parsed.</param>
        /// <param name="contextValues">Extra information about the context in which this element is being parsed.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public CompilerExtension.ComponentKeypathType ParseExtensionElement(SourceLineNumberCollection sourceLineNumbers, XmlElement parentElement, XmlElement element, ref string keyPath, params string[] contextValues)
        {
            CompilerExtension.ComponentKeypathType keyType = CompilerExtension.ComponentKeypathType.None;
            CompilerExtension extension = this.FindExtension(element.NamespaceURI);

            if (null != extension)
            {
                keyType = extension.ParseElement(sourceLineNumbers, parentElement, element, ref keyPath, contextValues);
            }
            else
            {
                SourceLineNumberCollection childSourceLineNumbers = Preprocessor.GetSourceLineNumbers(element);

                this.OnMessage(WixErrors.UnhandledExtensionElement(childSourceLineNumbers, parentElement.Name, element.Name, element.NamespaceURI));
            }

            return keyType;
        }

        /// <summary>
        /// Displays an unexpected attribute error if the attribute is not
        /// the namespace attribute.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The unexpected attribute.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public void UnexpectedAttribute(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute)
        {
            // ignore elements defined by the W3C because we'll assume they are always right
            if (!((String.Equals(attribute.Prefix, "xmlns", StringComparison.Ordinal) ||
                 String.Equals(attribute.LocalName, "xmlns", StringComparison.Ordinal)) &&
                 attribute.NamespaceURI.StartsWith(CompilerCore.W3SchemaPrefix, StringComparison.Ordinal) ||
                 attribute.NamespaceURI.StartsWith(this.schema.TargetNamespace, StringComparison.Ordinal)))
            {
                this.OnMessage(WixErrors.UnexpectedAttribute(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name));
            }
        }

        /// <summary>
        /// Display an unexepected element error.
        /// </summary>
        /// <param name="parentElement">The parent element.</param>
        /// <param name="childElement">The unexpected child element.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public void UnexpectedElement(XmlNode parentElement, XmlNode childElement)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(childElement);

            this.OnMessage(WixErrors.UnexpectedElement(sourceLineNumbers, parentElement.Name, childElement.Name));
        }

        /// <summary>
        /// Display an unsupported extension attribute error.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="extensionAttribute">The extension attribute.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public void UnsupportedExtensionAttribute(SourceLineNumberCollection sourceLineNumbers, XmlAttribute extensionAttribute)
        {
            Common.UnsupportedExtensionAttribute(sourceLineNumbers, extensionAttribute, this.OnMessage);
        }

        /// <summary>
        /// Display an unsupported extension element error.
        /// </summary>
        /// <param name="parentElement">The parent element.</param>
        /// <param name="extensionElement">The extension element.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public void UnsupportedExtensionElement(XmlNode parentElement, XmlNode extensionElement)
        {
            SourceLineNumberCollection sourceLineNumbers = Preprocessor.GetSourceLineNumbers(extensionElement);

            this.OnMessage(WixErrors.UnsupportedExtensionElement(sourceLineNumbers, parentElement.Name, extensionElement.Name));
        }

        /// <summary>
        /// Verifies that the calling assembly version is equal to or newer than the given <paramref name="requiredVersion"/>.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="requiredVersion">The version required of the calling assembly.</param>
        public void VerifyRequiredVersion(SourceLineNumberCollection sourceLineNumbers, string requiredVersion)
        {
            // an null or empty string means any version will work
            if (!string.IsNullOrEmpty(requiredVersion))
            {
                Assembly caller = Assembly.GetCallingAssembly();
                AssemblyName name = caller.GetName();
                FileVersionInfo fv = FileVersionInfo.GetVersionInfo(caller.Location);

                Version versionRequired = new Version(requiredVersion);
                Version versionCurrent = new Version(fv.FileVersion);

                if (versionRequired > versionCurrent)
                {
                    if (this.GetType().Assembly.Equals(caller))
                    {
                        this.OnMessage(WixErrors.InsufficientVersion(sourceLineNumbers, versionCurrent, versionRequired));
                    }
                    else
                    {
                        this.OnMessage(WixErrors.InsufficientVersion(sourceLineNumbers, versionCurrent, versionRequired, name.Name));
                    }
                }
            }
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs e)
        {
            WixErrorEventArgs errorEventArgs = e as WixErrorEventArgs;

            if (null != errorEventArgs)
            {
                this.encounteredError = true;
            }

            if (null != this.MessageHandler)
            {
                this.MessageHandler(this, e);
                if (MessageLevel.Error == e.Level)
                {
                    this.encounteredError = true;
                }
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }

        /// <summary>
        /// Creates a new section and makes it the active section in the core.
        /// </summary>
        /// <param name="id">Unique identifier for the section.</param>
        /// <param name="type">Type of section to create.</param>
        /// <param name="codepage">Codepage for the resulting database for this ection.</param>
        /// <returns>New section.</returns>
        internal Section CreateActiveSection(string id, SectionType type, int codepage)
        {
            Section newSection = this.CreateSection(id, type, codepage);
            this.activeSection = newSection;

            return newSection;
        }

        /// <summary>
        /// Creates a new section.
        /// </summary>
        /// <param name="id">Unique identifier for the section.</param>
        /// <param name="type">Type of section to create.</param>
        /// <param name="codepage">Codepage for the resulting database for this ection.</param>
        /// <returns>New section.</returns>
        internal Section CreateSection(string id, SectionType type, int codepage)
        {
            Section newSection = new Section(id, type, codepage);
            this.intermediate.Sections.Add(newSection);

            return newSection;
        }

        /// <summary>
        /// Creates a WiX complex reference in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="parentType">The parent type.</param>
        /// <param name="parentId">The parent id.</param>
        /// <param name="parentLanguage">The parent language.</param>
        /// <param name="childType">The child type.</param>
        /// <param name="childId">The child id.</param>
        /// <param name="isPrimary">Whether the child is primary.</param>
        internal void CreateWixComplexReferenceRow(SourceLineNumberCollection sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, string parentLanguage, ComplexReferenceChildType childType, string childId, bool isPrimary)
        {
            if (!this.EncounteredError)
            {
                WixComplexReferenceRow wixComplexReferenceRow = (WixComplexReferenceRow)this.CreateRow(sourceLineNumbers, "WixComplexReference");
                wixComplexReferenceRow.ParentId = parentId;
                wixComplexReferenceRow.ParentType = parentType;
                wixComplexReferenceRow.ParentLanguage = parentLanguage;
                wixComplexReferenceRow.ChildId = childId;
                wixComplexReferenceRow.ChildType = childType;
                wixComplexReferenceRow.IsPrimary = isPrimary;
            }
        }

        /// <summary>
        /// Creates WixComplexReference and WixGroup rows in the active section.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information.</param>
        /// <param name="parentType">The parent type.</param>
        /// <param name="parentId">The parent id.</param>
        /// <param name="parentLanguage">The parent language.</param>
        /// <param name="childType">The child type.</param>
        /// <param name="childId">The child id.</param>
        /// <param name="isPrimary">Whether the child is primary.</param>
        public void CreateComplexReference(SourceLineNumberCollection sourceLineNumbers, ComplexReferenceParentType parentType, string parentId, string parentLanguage, ComplexReferenceChildType childType, string childId, bool isPrimary)
        {
            this.CreateWixComplexReferenceRow(sourceLineNumbers, parentType, parentId, parentLanguage, childType, childId, isPrimary);
            this.CreateWixGroupRow(sourceLineNumbers, parentType, parentId, childType, childId);
        }

        /// <summary>
        /// Finds a compiler extension by namespace URI.
        /// </summary>
        /// <param name="namespaceUri">URI for namespace the extension supports.</param>
        /// <returns>Found compiler extension or null if nothing matches namespace URI.</returns>
        private CompilerExtension FindExtension(string namespaceUri)
        {
            return (CompilerExtension)this.extensions[namespaceUri];
        }

        /// <summary>
        /// Creates a version 3 name-based UUID.
        /// </summary>
        /// <param name="namespaceGuid">The namespace UUID.</param>
        /// <param name="value">The value.</param>
        /// <returns>The generated GUID for the given namespace and value.</returns>
        public static string NewGuid(Guid namespaceGuid, string value)
        {
            return Uuid.NewUuid(namespaceGuid, value, false).ToString("B").ToUpper(CultureInfo.InvariantCulture);
        }

        public static string CreateValueList(ValueListKind kind, params string[] values)
        {
            return CreateValueList(kind, (IEnumerable<string>)values);
        }

        public static string CreateValueList(ValueListKind kind, IEnumerable<string> values)
        {
            // Ideally, we could denote the list kind (and the list itself) directly in the
            // message XML, and detect and expand in the MessageHandler.GenerateMessageString()
            // method.  Doing so would make vararg-style messages much easier, but impacts
            // every single message we format.  For now, callers just have to know when a
            // message takes a list of values in a single string argument, the caller will
            // have to do the expansion themselves.  (And, unfortunately, hard-code the knowledge
            // that the list is an 'and' or 'or' list.)

            // For a localizable solution, we need to be able to get the list format string
            // from resources. We aren't currently localized right now, so the values are
            // just hard-coded.
            const string valueFormat = "'{0}'";
            const string valueSeparator = ", ";
            string terminalTerm = String.Empty;

            switch (kind)
            {
                case ValueListKind.None:
                    terminalTerm = "";
                    break;
                case ValueListKind.And:
                    terminalTerm = "and ";
                    break;
                case ValueListKind.Or:
                    terminalTerm = "or ";
                    break;
            }

            StringBuilder list = new StringBuilder();

            // This weird construction helps us determine when we're adding the last value
            // to the list.  Instead of adding them as we encounter them, we cache the current
            // value and append the *previous* one.
            string previousValue = null;
            bool haveValues = false;
            foreach (string value in values)
            {
                if (null != previousValue)
                {
                    if (haveValues)
                    {
                        list.Append(valueSeparator);
                    }
                    list.AppendFormat(valueFormat, previousValue);
                    haveValues = true;
                }

                previousValue = value;
            }

            // If we have no previous value, that means that the list contained no values, and
            // something has gone very wrong.
            Debug.Assert(null != previousValue);
            if (null != previousValue)
            {
                if (haveValues)
                {
                    list.Append(valueSeparator);
                    list.Append(terminalTerm);
                }
                list.AppendFormat(valueFormat, previousValue);
                haveValues = true;
            }

            return list.ToString();
        }
    }
}
