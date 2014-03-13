//-------------------------------------------------------------------------------------------------
// <copyright file="RegistryVerifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//      Contains methods for verification for Sql Extension
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixTest.Verifiers.Extensions
{
    using System;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using Microsoft.Win32;
    using System.Runtime.InteropServices;
    using System.Text;

    /// <summary>
    /// Contains methods for Registry verification
    /// </summary>
    public static class RegistryVerifier
    {
        /// <summary>
        /// Checks if a registry key exists
        /// </summary>
        /// <param name="hive">The hive to search within</param>
        /// <param name="keyName">The name of the key to look for</param>
        /// <returns>True if the key is found, false otherwise</returns>
        /// <remarks>
        ///     This method calls directelly windows native registry methods to bypass WOW32 regiction. 
        ///     Search will look in both 64-bit and 32-bit locations before it fails
        /// </remarks>
        public static bool RegistryKeyExists(RegistryHive hive, string keyName)
        {
            IntPtr hiveKey = new IntPtr((int)hive);
            IntPtr subKey = IntPtr.Zero;
            bool exists = false;

            // open the key
            int result = RegOpenKeyEx(hiveKey, keyName, 0, RegSAM.WOW64_64Key | RegSAM.Write , out subKey);
            if (result == ERROR_SUCCESS && subKey != IntPtr.Zero)
            {
                exists = true;
            }

            // close the key
            if (subKey != IntPtr.Zero)
            {
                RegCloseKey(subKey);
            }

            return exists;
        }

        /// <summary>
        /// Verifies a registry key value 
        /// </summary>
        /// <param name="hive">The hive to search within</param>
        /// <param name="keyName">The name of the key to look for</param>
        /// <param name="valueName">The name of the value to look for</param>
        /// <param name="expectedValue">The expected value to compare against</param>
        /// <remarks>
        ///     This method calls directelly windows native registry methods to bypass WOW32 regiction. 
        ///     Search will look in both 64-bit and 32-bit locations before it fails
        /// </remarks>
        public static void VerifyRegistryKeyValue(RegistryHive hive, string keyName, string valueName, string expectedValue)
        {
            IntPtr hiveKey = new IntPtr((int)hive);
            IntPtr subKey = IntPtr.Zero;
            string actualValue = string.Empty;

            // open the key
            int result = RegOpenKeyEx(hiveKey, keyName, 0, RegSAM.WOW64_64Key | RegSAM.Write | RegSAM.QueryValue, out subKey);
            if (result == ERROR_SUCCESS && subKey != IntPtr.Zero)
            {
                // read the value
                StringBuilder keyValue = new StringBuilder((int)MAX_DATA_SIZE);
                uint keyValueSize = MAX_DATA_SIZE;
                KeyType keyValueType;

                result = RegQueryValueEx(subKey, valueName, 0, out keyValueType, keyValue, ref keyValueSize);
                if (result == ERROR_SUCCESS)
                {
                    actualValue = keyValue.ToString();
                }
            }
            
            // close the key
            if (subKey != IntPtr.Zero)
            {
                RegCloseKey(subKey);
            }

            // do the validation
            Assert.IsTrue(actualValue.Trim().Equals(expectedValue.Trim(), StringComparison.InvariantCultureIgnoreCase),
                "Registry Key '{0}\\{1}' does NOT have the expected value; Actual: '{2}', Expected: '{3}'.",
                keyName, valueName, actualValue, expectedValue);
        }
       
        #region  P/Invoke declarations

        // Constants
        private static readonly uint MAX_DATA_SIZE = 1024;
        private static readonly int ERROR_SUCCESS = 0;

        // Enums
        [Flags]
        enum RegSAM
        {
            QueryValue = 0x0001,
            SetValue = 0x0002,
            CreateSubKey = 0x0004,
            EnumerateSubKeys = 0x0008,
            Notify = 0x0010,
            CreateLink = 0x0020,
            WOW64_32Key = 0x0200,
            WOW64_64Key = 0x0100,
            WOW64_Res = 0x0300,
            Read = 0x00020019,
            Write = 0x00020006,
            Execute = 0x00020019,
            AllAccess = 0x000f003f
        }

        enum KeyType : uint
        {
            REG_NONE = 0,
            REG_SZ = 1,
            REG_EXPAND_SZ = 2,
            REG_BINARY = 3,
            REG_DWORD_LITTLE_ENDIAN = 4,
            REG_DWORD_BIG_ENDIAN = 5,
            REG_LINK = 6,
            REG_MULTI_SZ = 7
        };

        // Methods
        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegOpenKeyEx", SetLastError = true)]
        private static extern int RegOpenKeyEx(
            IntPtr hKey,
            string subKey,
            uint options,
            RegSAM sam,
            out IntPtr phkResult);

        [DllImport("advapi32.dll", CharSet = CharSet.Unicode, EntryPoint = "RegQueryValueExW", SetLastError = true)]
        private static extern int RegQueryValueEx(
            IntPtr hKey,
            string lpValueName,
            int lpReserved,
            out KeyType lpType,
            System.Text.StringBuilder lpData,
            ref uint lpcbData);

        [DllImport("advapi32.dll", SetLastError = true)]
        private static extern int RegCloseKey(
            IntPtr hKey);

        #endregion
    }
}