// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Globalization;
    using System.IO;
    using System.Security.Cryptography;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Common Wix utility methods and types.
    /// </summary>
    internal sealed class Common
    {
        //-------------------------------------------------------------------------------------------------
        // Layout of an Access Mask (from http://technet.microsoft.com/en-us/library/cc783530(WS.10).aspx)
        //
        //  -------------------------------------------------------------------------------------------------
        //  |31|30|29|28|27|26|25|24|23|22|21|20|19|18|17|16|15|14|13|12|11|10|09|08|07|06|05|04|03|02|01|00|
        //  -------------------------------------------------------------------------------------------------
        //  |GR|GW|GE|GA| Reserved  |AS|StandardAccessRights|        Object-Specific Access Rights          |
        //
        //  Key
        //  GR = Generic Read
        //  GW = Generic Write
        //  GE = Generic Execute
        //  GA = Generic All
        //  AS = Right to access SACL
        //
        // TODO: what is the expected decompile behavior if a bit is found that is not explicitly enumerated
        //
        //-------------------------------------------------------------------------------------------------
        // Generic Access Rights (per WinNT.h)
        // ---------------------
        // GENERIC_ALL                      (0x10000000L)
        // GENERIC_EXECUTE                  (0x20000000L)
        // GENERIC_WRITE                    (0x40000000L)
        // GENERIC_READ                     (0x80000000L)
        internal static readonly string[] GenericPermissions = { "GenericAll", "GenericExecute", "GenericWrite", "GenericRead" };

        // Standard Access Rights (per WinNT.h)
        // ----------------------
        // DELETE                           (0x00010000L)
        // READ_CONTROL                     (0x00020000L)
        // WRITE_DAC                        (0x00040000L)
        // WRITE_OWNER                      (0x00080000L)
        // SYNCHRONIZE                      (0x00100000L)
        internal static readonly string[] StandardPermissions = { "Delete", "ReadPermission", "ChangePermission", "TakeOwnership", "Synchronize" };

        // Object-Specific Access Rights
        // =============================
        // Directory Access Rights (per WinNT.h)
        // -----------------------
        // FILE_LIST_DIRECTORY       ( 0x0001 )
        // FILE_ADD_FILE             ( 0x0002 )
        // FILE_ADD_SUBDIRECTORY     ( 0x0004 )
        // FILE_READ_EA              ( 0x0008 )
        // FILE_WRITE_EA             ( 0x0010 )
        // FILE_TRAVERSE             ( 0x0020 )
        // FILE_DELETE_CHILD         ( 0x0040 )
        // FILE_READ_ATTRIBUTES      ( 0x0080 )
        // FILE_WRITE_ATTRIBUTES     ( 0x0100 )
        internal static readonly string[] FolderPermissions = { "Read", "CreateFile", "CreateChild", "ReadExtendedAttributes", "WriteExtendedAttributes", "Traverse", "DeleteChild", "ReadAttributes", "WriteAttributes" };

        // Registry Access Rights (per TODO)
        // ----------------------
        internal static readonly string[] RegistryPermissions = { "Read", "Write", "CreateSubkeys", "EnumerateSubkeys", "Notify", "CreateLink" };

        // File Access Rights (per WinNT.h)
        // ------------------
        // FILE_READ_DATA            ( 0x0001 )
        // FILE_WRITE_DATA           ( 0x0002 )
        // FILE_APPEND_DATA          ( 0x0004 )
        // FILE_READ_EA              ( 0x0008 )
        // FILE_WRITE_EA             ( 0x0010 )
        // FILE_EXECUTE              ( 0x0020 )
        // via mask FILE_ALL_ACCESS  ( 0x0040 )
        // FILE_READ_ATTRIBUTES      ( 0x0080 )
        // FILE_WRITE_ATTRIBUTES     ( 0x0100 )
        //
        // STANDARD_RIGHTS_REQUIRED  (0x000F0000L)
        // FILE_ALL_ACCESS           (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x1FF)
        internal static readonly string[] FilePermissions = { "Read", "Write", "Append", "ReadExtendedAttributes", "WriteExtendedAttributes", "Execute", "FileAllRights", "ReadAttributes", "WriteAttributes" };

        internal static readonly string[] ReservedFileNames = { "CON", "PRN", "AUX", "NUL", "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9", "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9" };

        internal static readonly Regex WixVariableRegex = new Regex(@"(\!|\$)\((?<namespace>loc|wix|bind|bindpath)\.(?<fullname>(?<name>[_A-Za-z][0-9A-Za-z_]+)(\.(?<scope>[_A-Za-z][0-9A-Za-z_\.]*))?)(\=(?<value>.+?))?\)", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.ExplicitCapture);

        internal const char CustomRowFieldSeparator = '\x85';

        private static readonly Regex PropertySearch = new Regex(@"\[[#$!]?[a-zA-Z_][a-zA-Z0-9_\.]*]", RegexOptions.Singleline);
        private static readonly Regex AddPrefix = new Regex(@"^[^a-zA-Z_]", RegexOptions.Compiled);
        private static readonly Regex LegalIdentifierCharacters = new Regex(@"^[_A-Za-z][0-9A-Za-z_\.]*$", RegexOptions.Compiled);
        private static readonly Regex IllegalIdentifierCharacters = new Regex(@"[^A-Za-z0-9_\.]|\.{2,}", RegexOptions.Compiled); // non 'words' and assorted valid characters

        /// <summary>
        /// Protect the constructor.
        /// </summary>
        private Common()
        {
        }

        /// <summary>
        /// Cleans up the temp files.
        /// </summary>
        /// <param name="path">The temporary directory to delete.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <returns>True if all files were deleted, false otherwise.</returns>
        internal static bool DeleteTempFiles(string path, IMessageHandler messageHandler)
        {
            // try three times and give up with a warning if the temp files aren't gone by then
            int retryLimit = 3;
            bool removedReadOnly = false;

            for (int i = 0; i < retryLimit; i++)
            {
                try
                {
                    Directory.Delete(path, true);   // toast the whole temp directory
                    break; // no exception means we got success the first time
                }
                catch (UnauthorizedAccessException)
                {
                    if (!removedReadOnly) // should only need to unmark readonly once - there's no point in doing it again and again
                    {
                        removedReadOnly = true;
                        RecursiveFileAttributes(path, FileAttributes.ReadOnly, false, messageHandler); // toasting will fail if any files are read-only. Try changing them to not be.
                    }
                    else
                    {
                        messageHandler.OnMessage(WixWarnings.AccessDeniedForDeletion(null, path));
                        return false;
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    // if the path doesn't exist, then there is nothing for us to worry about
                    break;
                }
                catch (IOException) // directory in use
                {
                    if (i == (retryLimit - 1)) // last try failed still, give up
                    {
                        messageHandler.OnMessage(WixWarnings.DirectoryInUse(null, path));
                        return false;
                    }
                    System.Threading.Thread.Sleep(300);  // sleep a bit before trying again
                }
            }

            return true;
        }

        /// <summary>
        /// Gets a valid code page from the given web name or integer value.
        /// </summary>
        /// <param name="value">A code page web name or integer value as a string.</param>
        /// <exception cref="ArgumentOutOfRangeException">The value is an integer less than 0 or greater than 65535.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="NotSupportedException">The value doesn't not represent a valid code page name or integer value.</exception>
        internal static int GetValidCodePage(string value)
        {
            return GetValidCodePage(value, false);
        }

        /// <summary>
        /// Gets a valid code page from the given web name or integer value.
        /// </summary>
        /// <param name="value">A code page web name or integer value as a string.</param>
        /// <param name="allowNoChange">Whether to allow -1 which does not change the database code pages. This may be the case with wxl files.</param>
        /// <returns>A valid code page number.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is an integer less than 0 or greater than 65535.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="NotSupportedException">The value doesn't not represent a valid code page name or integer value.</exception>
        /// <exception cref="WixException">The code page is invalid for summary information.</exception>
        internal static int GetValidCodePage(string value, bool allowNoChange)
        {
            return GetValidCodePage(value, allowNoChange, false, null);
        }

        /// <summary>
        /// Gets a valid code page from the given web name or integer value.
        /// </summary>
        /// <param name="value">A code page web name or integer value as a string.</param>
        /// <param name="allowNoChange">Whether to allow -1 which does not change the database code pages. This may be the case with wxl files.</param>
        /// <param name="onlyAnsi">Whether to allow Unicode (UCS) or UTF code pages.</param>
        /// <param name="sourceLineNumbers">Source line information for the current authoring.</param>
        /// <returns>A valid code page number.</returns>
        /// <exception cref="ArgumentOutOfRangeException">The value is an integer less than 0 or greater than 65535.</exception>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="NotSupportedException">The value doesn't not represent a valid code page name or integer value.</exception>
        /// <exception cref="WixException">The code page is invalid for summary information.</exception>
        internal static int GetValidCodePage(string value, bool allowNoChange, bool onlyAnsi, SourceLineNumberCollection sourceLineNumbers)
        {
            int codePage;
            Encoding enc;

            if (null == value)
            {
                throw new ArgumentNullException("value");
            }

            try
            {
                // check if a integer as a string was passed
                if (int.TryParse(value, out codePage))
                {
                    if (0 == codePage)
                    {
                        // 0 represents a neutral database
                        return 0;
                    }
                    else if (allowNoChange && -1 == codePage)
                    {
                        // -1 means no change to the database code page
                        return -1;
                    }

                    enc = Encoding.GetEncoding(codePage);
                }
                else
                {
                    enc = Encoding.GetEncoding(value);
                }

                // Windows Installer parses some code page references
                // as unsigned shorts which fail to open the database.
                if (onlyAnsi)
                {
                    codePage = enc.CodePage;
                    if (0 > codePage || short.MaxValue < codePage)
                    {
                        throw new WixException(WixErrors.InvalidSummaryInfoCodePage(sourceLineNumbers, codePage));
                    }
                }

                return enc.CodePage;
            }
            catch (ArgumentException ex)
            {
                // rethrow as NotSupportedException since either can be thrown
                // if the system does not support the specified code page
                throw new NotSupportedException(ex.Message, ex);
            }
        }

        /// <summary>
        /// Verifies if an identifier is a valid binder variable name.
        /// </summary>
        /// <param name="name">Binder variable name to verify.</param>
        /// <returns>True if the identifier is a valid binder variable name.</returns>
        public static bool IsValidBinderVariable(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return false;
            }

            Match match = Common.WixVariableRegex.Match(name);

            return (match.Success && ("bind" == match.Groups["namespace"].Value || "wix" == match.Groups["namespace"].Value) && 0 == match.Index && name.Length == match.Length);
        }

        /// <summary>
        /// Verifies if a string contains a valid binder variable name.
        /// </summary>
        /// <param name="name">String to verify.</param>
        /// <returns>True if the string contains a valid binder variable name.</returns>
        public static bool ContainsValidBinderVariable(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                return false;
            }

            Match match = Common.WixVariableRegex.Match(name);

            return match.Success && ("bind" == match.Groups["namespace"].Value || "wix" == match.Groups["namespace"].Value);
        }

        /// <summary>
        /// Get the value of an attribute with type YesNoType.
        /// </summary>
        /// <param name="sourceLineNumbers">Source information for the value.</param>
        /// <param name="elementName">Name of the element for this attribute, used for a possible exception.</param>
        /// <param name="attributeName">Name of the attribute.</param>
        /// <param name="value">Value to process.</param>
        /// <returns>Returns true for a value of 'yes' and false for a value of 'no'.</returns>
        /// <exception cref="WixException">Thrown when the attribute's value is not 'yes' or 'no'.</exception>
        internal static bool IsYes(SourceLineNumberCollection sourceLineNumbers, string elementName, string attributeName, string value)
        {
            switch (value)
            {
                case "no":
                    return false;
                case "yes":
                    return true;
                default:
                    throw new WixException(WixErrors.IllegalAttributeValue(sourceLineNumbers, elementName, attributeName, value, "no", "yes"));
            }
        }

        /// <summary>
        /// Verifies the given string is a valid module or bundle version.
        /// </summary>
        /// <param name="version">The version to verify.</param>
        /// <returns>True if version is a valid module or bundle version.</returns>
        public static bool IsValidModuleOrBundleVersion(string version)
        {
            if (!Common.IsValidBinderVariable(version))
            {
                Version ver = null;

                try
                {
                    ver = new Version(version);
                }
                catch (ArgumentException)
                {
                    return false;
                }

                if (65535 < ver.Major || 65535 < ver.Minor || 65535 < ver.Build || 65535 < ver.Revision)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Generate a new Windows Installer-friendly guid.
        /// </summary>
        /// <returns>A new guid.</returns>
        internal static string GenerateGuid()
        {
            return Guid.NewGuid().ToString("B").ToUpper(CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Generate an identifier by hashing data from the row.
        /// </summary>
        /// <param name="prefix">Three letter or less prefix for generated row identifier.</param>
        /// <param name="fipsCompliant">Tells the algorithm to hash with a FIPS compliant hash.</param>
        /// <param name="args">Information to hash.</param>
        /// <returns>The generated identifier.</returns>
        [SuppressMessage("Microsoft.Globalization", "CA1303:DoNotPassLiteralsAsLocalizedParameters", MessageId = "System.InvalidOperationException.#ctor(System.String)")]
        public static string GenerateIdentifier(string prefix, bool fipsCompliant, params string[] args)
        {
            string stringData = String.Join("|", args);
            byte[] data = Encoding.Unicode.GetBytes(stringData);

            // hash the data
            byte[] hash;

            if (fipsCompliant)
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

            // build up the identifier
            StringBuilder identifier = new StringBuilder(35, 35);
            identifier.Append(prefix);

            // hard coded to 16 as that is the most bytes that can be used to meet the length requirements. SHA1 is 20 bytes.
            for (int i = 0; i < 16; i++)
            {
                identifier.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
            }

            return identifier.ToString();
        }

        /// <summary>
        /// Return an identifier based on passed file/directory name
        /// </summary>
        /// <param name="name">File/directory name to generate identifer from</param>
        /// <returns>A version of the name that is a legal identifier.</returns>
        internal static string GetIdentifierFromName(string name)
        {
            string result = IllegalIdentifierCharacters.Replace(name, "_"); // replace illegal characters with "_".

            // MSI identifiers must begin with an alphabetic character or an
            // underscore. Prefix all other values with an underscore.
            if (AddPrefix.IsMatch(name))
            {
                result = String.Concat("_", result);
            }

            return result;
        }

        /// <summary>
        /// Checks if the string contains a property (i.e. "foo[Property]bar")
        /// </summary>
        /// <param name="possibleProperty">String to evaluate for properties.</param>
        /// <returns>True if a property is found in the string.</returns>
        internal static bool ContainsProperty(string possibleProperty)
        {
            return PropertySearch.IsMatch(possibleProperty);
        }

        /// <summary>
        /// Recursively loops through a directory, changing an attribute on all of the underlying files.
        /// An example is to add/remove the ReadOnly flag from each file.
        /// </summary>
        /// <param name="path">The directory path to start deleting from.</param>
        /// <param name="fileAttribute">The FileAttribute to change on each file.</param>
        /// <param name="messageHandler">The message handler.</param>
        /// <param name="markAttribute">If true, add the attribute to each file. If false, remove it.</param>
        private static void RecursiveFileAttributes(string path, FileAttributes fileAttribute, bool markAttribute, IMessageHandler messageHandler)
        {
            foreach (string subDirectory in Directory.GetDirectories(path))
            {
                RecursiveFileAttributes(subDirectory, fileAttribute, markAttribute, messageHandler);
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

                try
                {
                    File.SetAttributes(filePath, attributes);
                }
                catch (UnauthorizedAccessException)
                {
                    messageHandler.OnMessage(WixWarnings.AccessDeniedForSettingAttributes(null, filePath));
                }
            }
        }

        internal static string GetFileHash(FileInfo fileInfo)
        {
            byte[] hashBytes;
            using (SHA1Managed managed = new SHA1Managed())
            {
                using (FileStream stream = fileInfo.OpenRead())
                {
                    hashBytes = managed.ComputeHash(stream);
                }
            }

            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < hashBytes.Length; i++)
            {
                sb.AppendFormat("{0:X2}", hashBytes[i]);
            }

            return sb.ToString();
        }

        /// <summary>
        /// Get an attribute value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="emptyRule">A rule for the contents of the value. If the contents do not follow the rule, an error is thrown.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        internal static string GetAttributeValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, EmptyRule emptyRule, Action<MessageEventArgs> messageHandler)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            if ((emptyRule == EmptyRule.MustHaveNonWhitespaceCharacters && String.IsNullOrEmpty(attribute.Value.Trim())) ||
                (emptyRule == EmptyRule.CanBeWhitespaceOnly && String.IsNullOrEmpty(attribute.Value)))
            {
                if (null != messageHandler)
                {
                    messageHandler(WixErrors.IllegalEmptyAttributeValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name));
                }

                return String.Empty;
            }

            return attribute.Value;
        }

        /// <summary>
        /// Verifies that a value is a legal identifier.
        /// </summary>
        /// <param name="value">The value to verify.</param>
        /// <returns>true if the value is an identifier; false otherwise.</returns>
        public static bool IsIdentifier(string value)
        {
            if (!String.IsNullOrEmpty(value))
            {
                if (LegalIdentifierCharacters.IsMatch(value))
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Get an identifier attribute value and displays an error for an illegal identifier value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's identifier value or a special value if an error occurred.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        internal static string GetAttributeIdentifierValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, Action<MessageEventArgs> messageHandler)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            string value = Common.GetAttributeValue(sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly, messageHandler);

            if (Common.IsIdentifier(value))
            {
                if (72 < value.Length && null != messageHandler)
                {
                    messageHandler(WixWarnings.IdentifierTooLong(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }

                return value;
            }
            else
            {
                if (value.StartsWith("[", StringComparison.Ordinal) && value.EndsWith("]", StringComparison.Ordinal) && null != messageHandler)
                {
                    messageHandler(WixErrors.IllegalIdentifierLooksLikeFormatted(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
                else if (null != messageHandler)
                {
                    messageHandler(WixErrors.IllegalIdentifier(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }

                return String.Empty;
            }
        }

        /// <summary>
        /// Get an integer attribute value and displays an error for an illegal integer value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="minimum">The minimum legal value.</param>
        /// <param name="maximum">The maximum legal value.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's integer value or a special value if an error occurred during conversion.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        public static int GetAttributeIntegerValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, int minimum, int maximum, Action<MessageEventArgs> messageHandler)
        {
            if (null == attribute)
            {
                throw new ArgumentNullException("attribute");
            }

            Debug.Assert(minimum > CompilerCore.IntegerNotSet && minimum > CompilerCore.IllegalInteger, "The legal values for this attribute collide with at least one sentinel used during parsing.");

            string value = Common.GetAttributeValue(sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly, messageHandler);

            if (0 < value.Length)
            {
                try
                {
                    int integer = Convert.ToInt32(value, CultureInfo.InvariantCulture.NumberFormat);

                    if (CompilerCore.IntegerNotSet == integer || CompilerCore.IllegalInteger == integer)
                    {
                        messageHandler(WixErrors.IntegralValueSentinelCollision(sourceLineNumbers, integer));
                    }
                    else if (minimum > integer || maximum < integer)
                    {
                        messageHandler(WixErrors.IntegralValueOutOfRange(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, integer, minimum, maximum));
                        integer = CompilerCore.IllegalInteger;
                    }

                    return integer;
                }
                catch (FormatException)
                {
                    messageHandler(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
                catch (OverflowException)
                {
                    messageHandler(WixErrors.IllegalIntegerValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                }
            }

            return CompilerCore.IllegalInteger;
        }

        /// <summary>
        /// Gets a yes/no value and displays an error for an illegal yes/no value.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="attribute">The attribute containing the value to get.</param>
        /// <param name="messageHandler">A delegate that receives error messages.</param>
        /// <returns>The attribute's YesNoType value.</returns>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        internal static YesNoType GetAttributeYesNoValue(SourceLineNumberCollection sourceLineNumbers, XmlAttribute attribute, Action<MessageEventArgs> messageHandler)
        {
            string value = Common.GetAttributeValue(sourceLineNumbers, attribute, EmptyRule.CanBeWhitespaceOnly, messageHandler);

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
                        if (null != messageHandler)
                        {
                            messageHandler(WixErrors.IllegalYesNoValue(sourceLineNumbers, attribute.OwnerElement.Name, attribute.Name, value));
                        }
                        break;
                }
            }

            return YesNoType.IllegalValue;
        }

        /// <summary>
        /// Display an unsupported extension attribute error.
        /// </summary>
        /// <param name="sourceLineNumbers">Source line information about the owner element.</param>
        /// <param name="extensionAttribute">The extension attribute.</param>
        [SuppressMessage("Microsoft.Design", "CA1059:MembersShouldNotExposeCertainConcreteTypes")]
        internal static void UnsupportedExtensionAttribute(SourceLineNumberCollection sourceLineNumbers, XmlAttribute extensionAttribute, Action<MessageEventArgs> messageHandler)
        {
            // ignore elements defined by the W3C because we'll assume they are always right
            if (!extensionAttribute.NamespaceURI.StartsWith(CompilerCore.W3SchemaPrefix, StringComparison.Ordinal) && null != messageHandler)
            {
                messageHandler(WixErrors.UnsupportedExtensionAttribute(sourceLineNumbers, extensionAttribute.OwnerElement.Name, extensionAttribute.Name));
            }
        }

    }
}
