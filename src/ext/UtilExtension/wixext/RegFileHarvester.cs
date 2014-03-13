//-------------------------------------------------------------------------------------------------
// <copyright file="RegFileHarvester.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Harvest WiX authoring from a reg file.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using Microsoft.Win32;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring for a reg file.
    /// </summary>
    public sealed class RegFileHarvester : HarvesterExtension
    {
        private static readonly string ComponentPrefix = "cmp";

        /// <summary>
        /// Current line in the reg file being processed.
        /// </summary>
        private int currentLineNumber = 0;

        /// <summary>
        /// Flag indicating whether this is a unicode registry file.
        /// </summary>
        private bool unicodeRegistry;

        /// <summary>
        /// Harvest a file.
        /// </summary>
        /// <param name="argument">The path of the file.</param>
        /// <returns>A harvested file.</returns>
        public override Wix.Fragment[] Harvest(string argument)
        {
            if (null == argument)
            {
                throw new ArgumentNullException("argument");
            }

            // Harvest the keys from the registry file
            Wix.Fragment fragment = this.HarvestRegFile(argument);

            return new Wix.Fragment[] { fragment };
        }

        /// <summary>
        /// Harvest a reg file.
        /// </summary>
        /// <param name="path">The path of the file.</param>
        /// <returns>A harvested registy file.</returns>
        public Wix.Fragment HarvestRegFile(string path)
        {
            if (null == path)
            {
                throw new ArgumentNullException("path");
            }

            if (!File.Exists(path))
            {
                throw new WixException(UtilErrors.FileNotFound(path));
            }

            Wix.Directory directory = new Wix.Directory();
            directory.Id = "TARGETDIR";

            // Use absolute paths
            path = Path.GetFullPath(path);
            FileInfo file = new FileInfo(path);

            using (StreamReader sr = file.OpenText())
            {
                string line;
                this.currentLineNumber = 0;

                while (null != (line = this.GetNextLine(sr)))
                {
                    if (line.StartsWith(@"Windows Registry Editor Version 5.00"))
                    {
                        this.unicodeRegistry = true;
                    }
                    else if (line.StartsWith(@"REGEDIT4"))
                    {
                        this.unicodeRegistry = false;
                    }
                    else if (line.StartsWith(@"[HKEY_CLASSES_ROOT\"))
                    {
                        this.ConvertKey(sr, ref directory, Wix.RegistryRootType.HKCR, line.Substring(19, line.Length - 20));
                    }
                    else if (line.StartsWith(@"[HKEY_CURRENT_USER\"))
                    {
                        this.ConvertKey(sr, ref directory, Wix.RegistryRootType.HKCU, line.Substring(19, line.Length - 20));
                    }
                    else if (line.StartsWith(@"[HKEY_LOCAL_MACHINE\"))
                    {
                        this.ConvertKey(sr, ref directory, Wix.RegistryRootType.HKLM, line.Substring(20, line.Length - 21));
                    }
                    else if (line.StartsWith(@"[HKEY_USERS\"))
                    {
                        this.ConvertKey(sr, ref directory, Wix.RegistryRootType.HKU, line.Substring(12, line.Length - 13));
                    }
                }
            }

            Console.WriteLine("Processing complete");

            Wix.Fragment fragment = new Wix.Fragment();
            fragment.AddChild(directory);

            return fragment;
        }

        /// <summary>
        /// Converts the registry key to a WiX component element.
        /// </summary>
        /// <param name="sr">The registry file stream.</param>
        /// <param name="directory">A WiX directory reference.</param>
        /// <param name="root">The root key.</param>
        /// <param name="line">The current line.</param>
        private void ConvertKey(StreamReader sr, ref Wix.Directory directory, Wix.RegistryRootType root, string line)
        {
            Wix.Component component = new Wix.Component();

            component.Id = this.Core.GenerateIdentifier(ComponentPrefix, line);
            component.KeyPath = Wix.YesNoType.yes;

            this.ConvertValues(sr, ref component, root, line);
            directory.AddChild(component);
        }

        /// <summary>
        /// Converts the registry values to WiX regisry key element.
        /// </summary>
        /// <param name="sr">The registry file stream.</param>
        /// <param name="component">A WiX component reference.</param>
        /// <param name="root">The root key.</param>
        /// <param name="line">The current line.</param>
        private void ConvertValues(StreamReader sr, ref Wix.Component component, Wix.RegistryRootType root, string line)
        {
            string name = null;
            string value = null;
            Wix.RegistryValue.TypeType type;
            Wix.RegistryKey registryKey = new Wix.RegistryKey();

            registryKey.Root = root;
            registryKey.Key = line;

            while (this.GetValue(sr, ref name, ref value, out type))
            {
                Wix.RegistryValue registryValue = new Wix.RegistryValue();
                ArrayList charArray;

                // Don't specifiy name for default attribute
                if (!string.IsNullOrEmpty(name))
                {
                    registryValue.Name = name;
                }

                registryValue.Type = type;

                switch (type)
                {
                    case Wix.RegistryValue.TypeType.binary:
                        registryValue.Value = value.Replace(",", string.Empty).ToUpper();
                        break;

                    case Wix.RegistryValue.TypeType.integer:
                        registryValue.Value = Int32.Parse(value, NumberStyles.HexNumber).ToString();
                        break;

                    case Wix.RegistryValue.TypeType.expandable:
                        charArray = this.ConvertCharList(value);
                        value = string.Empty;

                        // create the string, remove the terminating null
                        for (int i = 0; i < charArray.Count; i++)
                        {
                            if ('\0' != (char)charArray[i])
                            {
                                value += charArray[i];
                            }
                        }

                        registryValue.Value = value;
                        break;

                    case Wix.RegistryValue.TypeType.multiString:
                        charArray = this.ConvertCharList(value);
                        value = string.Empty;

                        // Convert the character array to a string so we can simply split it at the nulls, ignore the final null null.
                        for (int i = 0; i < (charArray.Count - 2); i++)
                        {
                            value += charArray[i];
                        }

                        // Although the value can use [~] the preffered way is to use MultiStringValue
                        string[] parts = value.Split("\0".ToCharArray());
                        foreach (string part in parts)
                        {
                            Wix.MultiStringValue multiStringValue = new Wix.MultiStringValue();
                            multiStringValue.Content = part;
                            registryValue.AddChild(multiStringValue);
                        }

                        break;

                    case Wix.RegistryValue.TypeType.@string:
                        // Remove \\ and \"
                        value = value.ToString().Replace("\\\"", "\"");
                        value = value.ToString().Replace(@"\\", @"\");
                        // Escape [ and ]
                        value = value.ToString().Replace(@"[", @"[\[]");
                        value = value.ToString().Replace(@"]", @"[\]]");
                        // This undoes the duplicate escaping caused by the second replace
                        value = value.ToString().Replace(@"[\[[\]]", @"[\[]");
                        // Escape $
                        value = value.ToString().Replace(@"$", @"$$");

                        registryValue.Value = value;
                        break;

                    default:
                        throw new ApplicationException(String.Format("Did not recognize the type of reg value on line {0}", this.currentLineNumber));
                }

                registryKey.AddChild(registryValue);
            }

            // Make sure empty keys are created
            if (null == value)
            {
                registryKey.ForceCreateOnInstall = Wix.YesNoType.yes;
            }

            component.AddChild(registryKey);
        }

        /// <summary>
        /// Parse a value from a line.
        /// </summary>
        /// <param name="sr">Reader for the reg file.</param>
        /// <param name="name">Name of the value.</param>
        /// <param name="value">Value of the value.</param>
        /// <param name="type">Type of the value.</param>
        /// <returns>true if the value can be parsed, false otherwise.</returns>
        private bool GetValue(StreamReader sr, ref string name, ref string value, out Wix.RegistryValue.TypeType type)
        {
            string line = this.GetNextLine(sr);

            if (null == line || 0 == line.Length)
            {
                type = 0;
                return false;
            }

            string[] parts;

            if (line.StartsWith("@"))
            {
                // Special case for default value
                parts = line.Trim().Split("=".ToCharArray(), 2);

                name = null;
            }
            else
            {
                parts = line.Trim().Split("=".ToCharArray());

                // It is valid to have an '=' in the name or the data. This is probably a string so the separator will be '"="'.
                if (2 != parts.Length)
                {
                    string[] stringSeparator = new string[] { "\"=\"" };
                    parts = line.Trim().Split(stringSeparator, StringSplitOptions.None);

                    if (2 != parts.Length)
                    {
                        // Line still no parsed correctly
                        throw new ApplicationException(String.Format("Cannot parse value: {0} at line {1}.", line, this.currentLineNumber));
                    }

                    // Put back quotes stripped by Split()
                    parts[0] += "\"";
                    parts[1] = "\"" + parts[1];
                }

                name = parts[0].Substring(1, parts[0].Length - 2);
            }

            if (parts[1].StartsWith("hex:"))
            {
                // binary
                value = parts[1].Substring(4);
                type = Wix.RegistryValue.TypeType.binary;
            }
            else if (parts[1].StartsWith("dword:")) 
            {
                // dword
                value = parts[1].Substring(6);
                type = Wix.RegistryValue.TypeType.integer;
            }
            else if (parts[1].StartsWith("hex(2):")) 
            {
                // expandable string
                value = parts[1].Substring(7);
                type = Wix.RegistryValue.TypeType.expandable;
            }
            else if (parts[1].StartsWith("hex(7):")) 
            {
                // multi-string
                value = parts[1].Substring(7);
                type = Wix.RegistryValue.TypeType.multiString;
            }
            else if (parts[1].StartsWith("hex(")) 
            {
                // Give a better error when we find something that isn't supported
                // by specifying the type that isn't supported.
                string unsupportedType = "";

                if (parts[1].StartsWith("hex(0")) { unsupportedType = "REG_NONE"; }
                else if (parts[1].StartsWith("hex(6")) { unsupportedType = "REG_LINK"; }
                else if (parts[1].StartsWith("hex(8")) { unsupportedType = "REG_RESOURCE_LIST"; }
                else if (parts[1].StartsWith("hex(9")) { unsupportedType = "REG_FULL_RESOURCE_DESCRIPTOR"; }
                else if (parts[1].StartsWith("hex(a")) { unsupportedType = "REG_RESOURCE_REQUIREMENTS_LIST"; }
                else if (parts[1].StartsWith("hex(b")) { unsupportedType = "REG_QWORD"; }

                // REG_NONE(0), REG_LINK(6), REG_RESOURCE_LIST(8), REG_FULL_RESOURCE_DESCRIPTOR(9), REG_RESOURCE_REQUIREMENTS_LIST(a), REG_QWORD(b)
                this.Core.OnMessage(UtilWarnings.UnsupportedRegistryType(parts[0], this.currentLineNumber, unsupportedType));

                type = 0;
                return false;
            }
            else if (parts[1].StartsWith("\"")) 
            {
                // string
                value = parts[1].Substring(1, parts[1].Length - 2);
                type = Wix.RegistryValue.TypeType.@string;
            }
            else
            {
                // unsupported value
                throw new ApplicationException(String.Format("Unsupported registry value {0} at line {1}.", line, this.currentLineNumber));
            }

            return true;
        }

        /// <summary>
        /// Get the next line from the reg file input stream.
        /// </summary>
        /// <param name="sr">Reader for the reg file.</param>
        /// <returns>The next line.</returns>
        private string GetNextLine(StreamReader sr)
        {
            string line;
            string totalLine = null;

            while (null != (line = sr.ReadLine()))
            {
                bool stop = true;

                this.currentLineNumber++;
                line = line.Trim();
                Console.Write("Processing line: {0}\r", currentLineNumber);

                if (line.EndsWith("\\"))
                {
                    stop = false;
                    line = line.Substring(0, line.Length - 1);
                }

                if (null == totalLine)
                {
                    // first line
                    totalLine = line;
                }
                else
                {
                    // other lines
                    totalLine += line;
                }

                // break if there is no more info for this line
                if (stop)
                {
                    break;
                }
            }

            return totalLine;
        }

        /// <summary>
        /// Convert a character list into the proper WiX format for either unicode or ansi lists.
        /// </summary>
        /// <param name="charList">List of characters.</param>
        /// <returns>Array of characters.</returns>
        private ArrayList ConvertCharList(string charList)
        {
            if (string.IsNullOrEmpty(charList))
            {
                return new ArrayList();
            }

            string[] strChars = charList.Split(",".ToCharArray());

            ArrayList charArray = new ArrayList();

            if (this.unicodeRegistry)
            {
                if (0 != strChars.Length % 2)
                {
                    throw new ApplicationException(String.Format("Problem parsing Expandable string data at line {0}, its probably not Unicode.", this.currentLineNumber));
                }

                for (int i = 0; i < strChars.Length; i += 2)
                {
                    string chars = strChars[i + 1] + strChars[i];
                    int unicodeInt = Int32.Parse(chars, NumberStyles.HexNumber);
                    char unicodeChar = (char)unicodeInt;
                    charArray.Add(unicodeChar);
                }
            }
            else
            {
                for (int i = 0; i < strChars.Length; i++)
                {
                    char charValue = (char)Int32.Parse(strChars[i], NumberStyles.HexNumber);
                    charArray.Add(charValue);
                }
            }

            return charArray;
        }
    }
}
