//-------------------------------------------------------------------------------------------------
// <copyright file="RegistryHarvester.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Harvest WiX authoring from the registry.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.Diagnostics;
    using System.Globalization;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Text;

    using Microsoft.Win32;
    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// Harvest WiX authoring from the registry.
    /// </summary>
    public sealed class RegistryHarvester : IDisposable
    {
        private const string HKCRPathInHKLM = @"Software\Classes";
        private string remappedPath;
        private static readonly int majorOSVersion = Environment.OSVersion.Version.Major;
        private RegistryKey regKeyToOverride = Registry.LocalMachine;
        private IntPtr regRootToOverride = NativeMethods.HkeyLocalMachine;

        /// <summary>
        /// Instantiate a new RegistryHarvester.
        /// </summary>
        /// <param name="remap">Set to true to remap the entire registry to a private location for this process.</param>
        public RegistryHarvester(bool remap)
        {
            // Detect OS major version and set the hive to use when
            // redirecting registry writes. We want to redirect registry
            // writes to HKCU on Windows Vista and higher to avoid UAC
            // problems, and to HKLM on downlevel OS's.
            if (majorOSVersion >= 6)
            {
                regKeyToOverride = Registry.CurrentUser;
                regRootToOverride = NativeMethods.HkeyCurrentUser;
            }

            // create a path in the registry for redirected keys which is process-specific
            if (remap)
            {
                this.remappedPath = String.Concat(@"SOFTWARE\WiX\heat\", Process.GetCurrentProcess().Id.ToString(CultureInfo.InvariantCulture));

                // remove the previous remapped key if it exists
                this.RemoveRemappedKey();

                // remap the registry roots supported by MSI
                // note - order is important here - the hive being used to redirect
                // to must be overridden last to avoid creating the other override
                // hives in the wrong location in the registry. For example, if HKLM is
                // the redirect destination, overriding it first will cause other hives
                // to be overridden under HKLM\Software\WiX\heat\HKLM\Software\WiX\HKCR
                // instead of under HKLM\Software\WiX\heat\HKCR
                if (majorOSVersion < 6)
                {
                    this.RemapRegistryKey(NativeMethods.HkeyClassesRoot, String.Concat(this.remappedPath, @"\\HKEY_CLASSES_ROOT"));
                    this.RemapRegistryKey(NativeMethods.HkeyCurrentUser, String.Concat(this.remappedPath, @"\\HKEY_CURRENT_USER"));
                    this.RemapRegistryKey(NativeMethods.HkeyUsers, String.Concat(this.remappedPath, @"\\HKEY_USERS"));
                    this.RemapRegistryKey(NativeMethods.HkeyLocalMachine, String.Concat(this.remappedPath, @"\\HKEY_LOCAL_MACHINE"));
                }
                else
                {
                    this.RemapRegistryKey(NativeMethods.HkeyClassesRoot, String.Concat(this.remappedPath, @"\\HKEY_CLASSES_ROOT"));
                    this.RemapRegistryKey(NativeMethods.HkeyLocalMachine, String.Concat(this.remappedPath, @"\\HKEY_LOCAL_MACHINE"));
                    this.RemapRegistryKey(NativeMethods.HkeyUsers, String.Concat(this.remappedPath, @"\\HKEY_USERS"));
                    this.RemapRegistryKey(NativeMethods.HkeyCurrentUser, String.Concat(this.remappedPath, @"\\HKEY_CURRENT_USER"));

                    // Typelib registration on Windows Vista requires that the key 
                    // HKLM\Software\Classes exist, so add it to the remapped root
                    Registry.LocalMachine.CreateSubKey(HKCRPathInHKLM);
                }
            }
        }

        /// <summary>
        /// Close the RegistryHarvester and remove any remapped registry keys.
        /// </summary>
        public void Close()
        {
            // note - order is important here - we must quit overriding the hive
            // being used to redirect first
            if (majorOSVersion < 6)
            {
                NativeMethods.OverrideRegistryKey(NativeMethods.HkeyLocalMachine, IntPtr.Zero);
                NativeMethods.OverrideRegistryKey(NativeMethods.HkeyClassesRoot, IntPtr.Zero);
                NativeMethods.OverrideRegistryKey(NativeMethods.HkeyCurrentUser, IntPtr.Zero);
                NativeMethods.OverrideRegistryKey(NativeMethods.HkeyUsers, IntPtr.Zero);
            }
            else
            {
                NativeMethods.OverrideRegistryKey(NativeMethods.HkeyCurrentUser, IntPtr.Zero);
                NativeMethods.OverrideRegistryKey(NativeMethods.HkeyClassesRoot, IntPtr.Zero);
                NativeMethods.OverrideRegistryKey(NativeMethods.HkeyLocalMachine, IntPtr.Zero);
                NativeMethods.OverrideRegistryKey(NativeMethods.HkeyUsers, IntPtr.Zero);
            }

            this.RemoveRemappedKey();
        }

        /// <summary>
        /// Dispose the RegistryHarvester.
        /// </summary>
        public void Dispose()
        {
            this.Close();
        }

        /// <summary>
        /// Harvest all registry roots supported by Windows Installer.
        /// </summary>
        /// <returns>The registry keys and values in the registry.</returns>
        public Wix.RegistryValue[] HarvestRegistry()
        {
            ArrayList registryValues = new ArrayList();

            this.HarvestRegistryKey(Registry.ClassesRoot, registryValues);
            this.HarvestRegistryKey(Registry.CurrentUser, registryValues);
            this.HarvestRegistryKey(Registry.LocalMachine, registryValues);
            this.HarvestRegistryKey(Registry.Users, registryValues);

            return (Wix.RegistryValue[])registryValues.ToArray(typeof(Wix.RegistryValue));
        }

        /// <summary>
        /// Harvest a registry key.
        /// </summary>
        /// <param name="path">The path of the registry key to harvest.</param>
        /// <returns>The registry keys and values under the key.</returns>
        public Wix.RegistryValue[] HarvestRegistryKey(string path)
        {
            RegistryKey registryKey = null;
            ArrayList registryValues = new ArrayList();

            string[] parts = GetPathParts(path);

            try
            {
                switch (parts[0])
                {
                    case "HKEY_CLASSES_ROOT":
                        registryKey = Registry.ClassesRoot;
                        break;
                    case "HKEY_CURRENT_USER":
                        registryKey = Registry.CurrentUser;
                        break;
                    case "HKEY_LOCAL_MACHINE":
                        registryKey = Registry.LocalMachine;
                        break;
                    case "HKEY_USERS":
                        registryKey = Registry.Users;
                        break;
                    default:
                        // TODO: put a better exception here
                        throw new Exception();
                }

                if (1 < parts.Length)
                {
                    registryKey = registryKey.OpenSubKey(parts[1]);

                    if (null == registryKey)
                    {
                        throw new WixException(UtilErrors.UnableToOpenRegistryKey(parts[1]));
                    }
                }

                this.HarvestRegistryKey(registryKey, registryValues);
            }
            finally
            {
                if (null != registryKey)
                {
                    registryKey.Close();
                }
            }

            return (Wix.RegistryValue[])registryValues.ToArray(typeof(Wix.RegistryValue));
        }

        /// <summary>
        /// Gets the parts of a registry key's path.
        /// </summary>
        /// <param name="path">The registry key path.</param>
        /// <returns>The root and key parts of the registry key path.</returns>
        private static string[] GetPathParts(string path)
        {
            return path.Split(@"\".ToCharArray(), 2);
        }

        /// <summary>
        /// Harvest a registry key.
        /// </summary>
        /// <param name="registryKey">The registry key to harvest.</param>
        /// <param name="registryValues">The collected registry values.</param>
        private void HarvestRegistryKey(RegistryKey registryKey, ArrayList registryValues)
        {
            // harvest the sub-keys
            foreach (string subKeyName in registryKey.GetSubKeyNames())
            {
                using (RegistryKey subKey = registryKey.OpenSubKey(subKeyName))
                {
                    this.HarvestRegistryKey(subKey, registryValues);
                }
            }

            string[] parts = GetPathParts(registryKey.Name);

            Wix.RegistryRootType root;
            switch (parts[0])
            {
                case "HKEY_CLASSES_ROOT":
                    root = Wix.RegistryRootType.HKCR;
                    break;
                case "HKEY_CURRENT_USER":
                    root = Wix.RegistryRootType.HKCU;
                    break;
                case "HKEY_LOCAL_MACHINE":
                    // HKLM\Software\Classes is equivalent to HKCR
                    if (1 < parts.Length && parts[1].StartsWith(HKCRPathInHKLM, StringComparison.OrdinalIgnoreCase))
                    {
                        root = Wix.RegistryRootType.HKCR;
                        parts[1] = parts[1].Remove(0, HKCRPathInHKLM.Length);

                        if (0 < parts[1].Length)
                        {
                            parts[1] = parts[1].TrimStart('\\');
                        }

                        if (String.IsNullOrEmpty(parts[1]))
                        {
                            parts = new [] { parts[0] };
                        }
                    }
                    else
                    {
                        root = Wix.RegistryRootType.HKLM;
                    }
                    break;
                case "HKEY_USERS":
                    root = Wix.RegistryRootType.HKU;
                    break;
                default:
                    // TODO: put a better exception here
                    throw new Exception();
            }

            // harvest the values
            foreach (string valueName in registryKey.GetValueNames())
            {
                Wix.RegistryValue registryValue = new Wix.RegistryValue();

                registryValue.Action = Wix.RegistryValue.ActionType.write;

                registryValue.Root = root;

                if (1 < parts.Length)
                {
                    registryValue.Key = parts[1];
                }

                if (null != valueName && 0 < valueName.Length)
                {
                    registryValue.Name = valueName;
                }

                object value = registryKey.GetValue(valueName);

                if (value is byte[]) // binary
                {
                    StringBuilder hexadecimalValue = new StringBuilder();

                    // convert the byte array to hexadecimal
                    foreach (byte byteValue in (byte[])value)
                    {
                        hexadecimalValue.Append(byteValue.ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
                    }

                    registryValue.Type = Wix.RegistryValue.TypeType.binary;
                    registryValue.Value = hexadecimalValue.ToString();
                }
                else if (value is int) // integer
                {
                    registryValue.Type = Wix.RegistryValue.TypeType.integer;
                    registryValue.Value = ((int)value).ToString(CultureInfo.InvariantCulture);
                }
                else if (value is string[]) // multi-string
                {
                    registryValue.Type = Wix.RegistryValue.TypeType.multiString;

                    if (0 == ((string[])value).Length)
                    {
                        Wix.MultiStringValue multiStringValue = new Wix.MultiStringValue();

                        multiStringValue.Content = String.Empty;

                        registryValue.AddChild(multiStringValue);
                    }
                    else
                    {
                        foreach (string multiStringValueContent in (string[])value)
                        {
                            Wix.MultiStringValue multiStringValue = new Wix.MultiStringValue();

                            multiStringValue.Content = multiStringValueContent;

                            registryValue.AddChild(multiStringValue);
                        }
                    }
                }
                else if (value is string) // string, expandable (there is no way to differentiate a string and expandable value in .NET 1.1)
                {
                    registryValue.Type = Wix.RegistryValue.TypeType.@string;
                    registryValue.Value = (string)value;
                }
                else
                {
                    // TODO: put a better exception here
                    throw new Exception();
                }

                registryValues.Add(registryValue);
            }

            // If there were no subkeys and no values, we still need an element for this empty registry key.
            // But specifically avoid SOFTWARE\Classes because it shouldn't be harvested as an empty key.
            if (parts.Length > 1 && registryKey.SubKeyCount == 0 && registryKey.ValueCount == 0 &&
                !String.Equals(parts[1], HKCRPathInHKLM, StringComparison.OrdinalIgnoreCase))
            {
                Wix.RegistryValue emptyRegistryKey = new Wix.RegistryValue();
                emptyRegistryKey.Root = root;
                emptyRegistryKey.Key = parts[1];
                emptyRegistryKey.Type = Wix.RegistryValue.TypeType.@string;
                emptyRegistryKey.Value = String.Empty;
                emptyRegistryKey.Action = Wix.RegistryValue.ActionType.write;
                registryValues.Add(emptyRegistryKey);
            }
        }

        /// <summary>
        /// Remap a registry key to an alternative location.
        /// </summary>
        /// <param name="registryKey">The registry key to remap.</param>
        /// <param name="remappedPath">The path to remap the registry key to under HKLM.</param>
        private void RemapRegistryKey(IntPtr registryKey, string remappedPath)
        {
            IntPtr remappedKey = IntPtr.Zero;

            try
            {
                remappedKey = NativeMethods.OpenRegistryKey(regRootToOverride, remappedPath);

                NativeMethods.OverrideRegistryKey(registryKey, remappedKey);
            }
            finally
            {
                if (IntPtr.Zero != remappedKey)
                {
                    NativeMethods.CloseRegistryKey(remappedKey);
                }
            }
        }

        /// <summary>
        /// Remove the remapped registry key.
        /// </summary>
        private void RemoveRemappedKey()
        {
            try
            {
                regKeyToOverride.DeleteSubKeyTree(this.remappedPath);
            }
            catch (ArgumentException)
            {
                // ignore the error where the key does not exist
            }
        }

        /// <summary>
        /// The native methods for re-mapping registry keys.
        /// </summary>
        private sealed class NativeMethods
        {
            internal static readonly IntPtr HkeyClassesRoot = (IntPtr)unchecked((Int32)0x80000000);
            internal static readonly IntPtr HkeyCurrentUser = (IntPtr)unchecked((Int32)0x80000001);
            internal static readonly IntPtr HkeyLocalMachine = (IntPtr)unchecked((Int32)0x80000002);
            internal static readonly IntPtr HkeyUsers = (IntPtr)unchecked((Int32)0x80000003);

            private const uint GenericRead = 0x80000000;
            private const uint GenericWrite = 0x40000000;
            private const uint GenericExecute = 0x20000000;
            private const uint GenericAll = 0x10000000;
            private const uint StandardRightsAll = 0x001F0000;

            /// <summary>
            /// Opens a registry key.
            /// </summary>
            /// <param name="key">Base key to open.</param>
            /// <param name="path">Path to subkey to open.</param>
            /// <returns>Handle to new key.</returns>
            internal static IntPtr OpenRegistryKey(IntPtr key, string path)
            {
                IntPtr newKey = IntPtr.Zero;
                uint disposition = 0;
                uint sam = StandardRightsAll | GenericRead | GenericWrite | GenericExecute | GenericAll;

                if (0 != RegCreateKeyEx(key, path, 0, null, 0, sam, 0, out newKey, out disposition))
                {
                    throw new Exception();
                }

                return newKey;
            }

            /// <summary>
            /// Closes a previously open registry key.
            /// </summary>
            /// <param name="key">Handle to key to close.</param>
            internal static void CloseRegistryKey(IntPtr key)
            {
                if (0 != RegCloseKey(key))
                {
                    throw new Exception();
                }
            }

            /// <summary>
            /// Override a registry key.
            /// </summary>
            /// <param name="key">Handle of the key to override.</param>
            /// <param name="newKey">Handle to override key.</param>
            internal static void OverrideRegistryKey(IntPtr key, IntPtr newKey)
            {
                if (0 != RegOverridePredefKey(key, newKey))
                {
                    throw new Exception();
                }
            }

            /// <summary>
            /// Interop to RegCreateKeyW.
            /// </summary>
            /// <param name="key">Handle to base key.</param>
            /// <param name="subkey">Subkey to create.</param>
            /// <param name="reserved">Always 0</param>
            /// <param name="className">Just pass null.</param>
            /// <param name="options">Just pass 0.</param>
            /// <param name="desiredSam">Rights to registry key.</param>
            /// <param name="securityAttributes">Just pass null.</param>
            /// <param name="openedKey">Opened key.</param>
            /// <param name="disposition">Whether key was opened or created.</param>
            /// <returns>Handle to registry key.</returns>
            [DllImport("advapi32.dll", EntryPoint = "RegCreateKeyExW", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            private static extern int RegCreateKeyEx(IntPtr key, string subkey, uint reserved, string className, uint options, uint desiredSam, uint securityAttributes, out IntPtr openedKey, out uint disposition);

            /// <summary>
            /// Interop to RegCloseKey.
            /// </summary>
            /// <param name="key">Handle to key to close.</param>
            /// <returns>0 if success.</returns>
            [DllImport("advapi32.dll", EntryPoint = "RegCloseKey", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            private static extern int RegCloseKey(IntPtr key);

            /// <summary>
            /// Interop to RegOverridePredefKey.
            /// </summary>
            /// <param name="key">Handle to key to override.</param>
            /// <param name="newKey">Handle to override key.</param>
            /// <returns>0 if success.</returns>
            [DllImport("advapi32.dll", EntryPoint = "RegOverridePredefKey", CharSet = CharSet.Unicode, ExactSpelling = true, SetLastError = true)]
            private static extern int RegOverridePredefKey(IntPtr key, IntPtr newKey);
        }
    }
}
