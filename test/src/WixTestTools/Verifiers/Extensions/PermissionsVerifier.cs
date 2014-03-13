//-------------------------------------------------------------------------------------------------
// <copyright file="PermissionsVerifier.cs" company="Outercurve Foundation">
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
    using System.IO;
    using System.Xml;
    using System.Collections;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using System.Text;
    using System.Runtime.InteropServices;
    using System.Security.AccessControl;
    using System.Security.Principal;
    using Microsoft.Win32;

    /// <summary>
    /// Contains methods for Permissions verification
    /// </summary>
    public static class PermissionsVerifier
    {
        /// <summary>
        /// Remove Permissions on a Registry key
        /// </summary>
        /// <param name="userName">the user name to remove permissions for</param>
        /// <param name="root">the root hive</param>
        /// <param name="subKey">the key</param>
        public static void RemoveRegistryKeyPermission(string userName, Microsoft.Win32.RegistryKey root, string subKey)
        {
            RegistryKey registryKey = root.OpenSubKey(subKey);
            RegistrySecurity registrySecurity = registryKey.GetAccessControl();
            AuthorizationRuleCollection accessRules = registrySecurity.GetAccessRules(true, true, typeof(NTAccount));

            foreach (RegistryAccessRule accessRule in accessRules)
            {
                if (userName.ToLowerInvariant().Equals(accessRule.IdentityReference.Value.ToLowerInvariant()))
                {
                    registrySecurity.RemoveAccessRule(accessRule);
                }
            }
        }

        /// <summary>
        /// Verify that a specific user has access rights to a specific Registry Key
        /// </summary>
        /// <param name="userName">Name of the user to check for</param>
        /// <param name="root">Root key for the registry key</param>
        /// <param name="subKey">Registry key to check for</param>
        /// <param name="rights">Expected access rights</param>
        public static void VerifyRegistryKeyPermission(string userName, Microsoft.Win32.RegistryKey root, string subKey, RegistryRights rights)
        {
            RegistryKey registryKey = root.OpenSubKey(subKey);
            RegistrySecurity registrySecurity = registryKey.GetAccessControl();
            AuthorizationRuleCollection accessRules = registrySecurity.GetAccessRules(true, true, typeof(NTAccount));

            foreach (RegistryAccessRule accessRule in accessRules)
            {
                if (userName.ToLowerInvariant().Equals(accessRule.IdentityReference.Value.ToLowerInvariant()))
                {
                    if ((accessRule.RegistryRights & rights) == rights)
                    {
                        return;
                    }
                }
            }

            Assert.Fail(string.Format("User '{0}' do not have the correct permessions to RegistryKey '{1}/{2}'.", userName, root.ToString(), subKey));
        }

        /// <summary>
        /// Remove Permissions on a file
        /// </summary>
        /// <param name="userName">the user name to remove permissions for</param>
        /// <param name="filePath">the file</param>
        public static void RemoveFilePermession(string userName, string filePath)
        {
            FileSecurity fileSecurity = File.GetAccessControl(filePath);
            AuthorizationRuleCollection accessRules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));


            foreach (FileSystemAccessRule accessRule in accessRules)
            {
                if (userName.ToLowerInvariant().Equals(accessRule.IdentityReference.Value.ToLowerInvariant()))
                {
                    fileSecurity.RemoveAccessRule(accessRule);
                }

            }
        }

        /// <summary>
        /// Verify that a specific user has access rights to a specific File
        /// </summary>
        /// <param name="userName">Name of the user to check for</param>
        /// <param name="filePath">Full path to the file to check</param>
        /// <param name="rights"><>Expected access rights/param>
        public static void VerifyFilePermession(string userName, string filePath, System.Security.AccessControl.FileSystemRights rights)
        {
            FileSecurity fileSecurity = System.IO.File.GetAccessControl(filePath);
            AuthorizationRuleCollection accessRules = fileSecurity.GetAccessRules(true, true, typeof(NTAccount));


            foreach (FileSystemAccessRule accessRule in accessRules)
            {
                if (userName.ToLowerInvariant().Equals(accessRule.IdentityReference.Value.ToLowerInvariant()))
                {
                    if ((accessRule.FileSystemRights & rights) == rights)
                    {
                        return;
                    }
                }

            }

            Assert.Fail(string.Format("User '{0}' do not have permession to File '{1}'.", userName, filePath));
        }

        /// <summary>
        /// Remove Permissions on a folder
        /// </summary>
        /// <param name="userName">the user name to remove permissions for</param>
        /// <param name="filePath">the file</param>
        public static void RemoveFolderPermession(string userName, string directoryPath)
        {
            DirectorySecurity directorySecurity = Directory.GetAccessControl(directoryPath);
            AuthorizationRuleCollection accessRules = directorySecurity.GetAccessRules(true, true, typeof(NTAccount));

            foreach (FileSystemAccessRule accessRule in accessRules)
            {
                if (userName.ToLowerInvariant().Equals(accessRule.IdentityReference.Value.ToLowerInvariant()))
                {
                    directorySecurity.RemoveAccessRule(accessRule);
                }
            }
        }

        /// <summary>
        /// Verify that a specific user has access rights to a specific Folder
        /// </summary>
        /// <param name="userName">Name of the user to check for</param>
        /// <param name="filePath">Full path to the folder to check</param>
        /// <param name="rights"><>Expected access rights/param>
        public static void VerifyFolderPermession(string userName, string directoryPath, System.Security.AccessControl.FileSystemRights rights)
        {
            DirectorySecurity directorySecurity = Directory.GetAccessControl(directoryPath);
            AuthorizationRuleCollection accessRules = directorySecurity.GetAccessRules(true, true, typeof(NTAccount));

            foreach (FileSystemAccessRule accessRule in accessRules)
            {
                if (userName.ToLowerInvariant().Equals(accessRule.IdentityReference.Value.ToLowerInvariant()))
                {
                    if ((accessRule.FileSystemRights & rights) == rights)
                    {
                        return;
                    }
                }
            }

            Assert.Fail(string.Format("User '{0}' do not have permession to folder '{1}'.", userName, directoryPath));
        }

        /// <summary>
        /// Verify permissions on a share
        /// </summary>
        /// <param name="userName">usename to check for</param>
        /// <param name="shareName">the name of the share</param>
        /// <param name="rights">the expected rights</param>
        public static void VerifySharePermession(string userName, string shareName, ACCESS_MASK rights)
        {
            ACCESS_MASK accessMask = GetSharePermissions(shareName, userName);
            Assert.IsTrue(
                (accessMask & rights) != 0, 
                "User '{0}' access rights to Share '{1}' do not match expected. Actual: '{2}'. Expected: '{3}'. ", userName, shareName, accessMask, rights);
        }

        /// <summary>
        /// Returns the user permissions on a share
        /// </summary>
        /// <param name="shareName">the full path to the shaer</param>
        /// <param name="userName">the user to check for</param>
        /// <returns>the user rights</returns>
        public static ACCESS_MASK GetSharePermissions(string shareName, string userName)
        {
            IntPtr ownerSid = IntPtr.Zero;
            IntPtr groupSid = IntPtr.Zero;
            IntPtr dacl = IntPtr.Zero;
            IntPtr sacl = IntPtr.Zero;
            IntPtr securityDescriptor = IntPtr.Zero;

            try
            {
                // get the security informaton of the object
                int returnValue;
                SECURITY_INFORMATION flags = SECURITY_INFORMATION.DACL_SECURITY_INFORMATION;
                returnValue = GetNamedSecurityInfo(shareName, SE_OBJECT_TYPE.SE_LMSHARE, flags, out ownerSid, out groupSid, out dacl, out sacl, out securityDescriptor);
                if (returnValue != ERROR_SUCCESS)
                {
                    throw new System.Runtime.InteropServices.ExternalException(string.Format("Cannot retrieve security info entries. Last Error: {0}.", Marshal.GetLastWin32Error()));
                }

                // get the user SID
                byte[] userSID = GetUserSID(userName);

                // build a trustee object for the user
                TRUSTEE trustee = new TRUSTEE();
                IntPtr pTrustee = Marshal.AllocHGlobal(Marshal.SizeOf(trustee));
                Marshal.StructureToPtr(trustee, pTrustee, false);
                BuildTrusteeWithSid(pTrustee, userSID);

                // get the access rights
                ACCESS_MASK accessRights = 0;
                GetEffectiveRightsFromAcl(dacl, pTrustee, ref accessRights);

                return accessRights;
            }
            finally
            {
                // clean up
                LocalFree(ownerSid);
                LocalFree(groupSid);
                LocalFree(dacl);
                LocalFree(sacl);
                LocalFree(securityDescriptor);
            }
        }

        /// <summary>
        /// Returns the user Security Identifier (SID) given a username
        /// </summary>
        /// <param name="accountName">usename</param>
        /// <returns>SID</returns>
        [STAThread]
        private static byte[] GetUserSID(string accountName)
        {
            byte[] Sid = null;
            uint cbSid = 0;
            StringBuilder referencedDomainName = new StringBuilder();
            uint cchReferencedDomainName = (uint)referencedDomainName.Capacity;
            SID_NAME_USE sidUse;

            int lastError = ERROR_SUCCESS;
            if (!LookupAccountName(null, accountName, Sid, ref cbSid, referencedDomainName, ref cchReferencedDomainName, out sidUse))
            {
                lastError = Marshal.GetLastWin32Error();
                if (lastError == ERROR_INSUFFICIENT_BUFFER || lastError == ERROR_INVALID_FLAGS)
                {
                    Sid = new byte[cbSid];
                    referencedDomainName.EnsureCapacity((int)cchReferencedDomainName);
                    lastError = ERROR_SUCCESS;
                    if (!LookupAccountName(null, accountName, Sid, ref cbSid, referencedDomainName, ref cchReferencedDomainName, out sidUse))
                        lastError = Marshal.GetLastWin32Error();
                }
            }
            else
            {
                throw new ArgumentException(string.Format("User Not Found: Could not get the SID for User '{0}'.", accountName));
            }

            return Sid;
        }

        #region P/Invoke declarations

        // Constants
        const int ERROR_SUCCESS = 0;
        const int ERROR_INSUFFICIENT_BUFFER = 122;
        const int ERROR_INVALID_FLAGS = 1004; // On Windows Server 2003 this error is/can be returned, but processing can still continue
        const int FILE_FLAG_BACKUP_SEMANTICS = 0x02000000;

        // Enums
        enum SECURITY_INFORMATION
        {
            OWNER_SECURITY_INFORMATION = 1,
            GROUP_SECURITY_INFORMATION = 2,
            DACL_SECURITY_INFORMATION = 4,
            SACL_SECURITY_INFORMATION = 8,
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct TRUSTEE
        {
            public IntPtr pMultipleTruste;   // must be null
            public MULTIPLE_TRUSTEE_OPERATION MultipleTrusteeOperation;
            public TRUSTEE_FORM TrusteeForm;
            public TRUSTEE_TYPE TrusteeType;
            public IntPtr ptstrName;
        }
       
        enum MULTIPLE_TRUSTEE_OPERATION
        {
            NO_MULTIPLE_TRUSTEE,
            TRUSTEE_IS_IMPERSONATE
        }
       
        enum TRUSTEE_FORM
        {
            TRUSTEE_IS_SID,
            TRUSTEE_IS_NAME,
            TRUSTEE_BAD_FORM,
            TRUSTEE_IS_OBJECTS_AND_SID,
            TRUSTEE_IS_OBJECTS_AND_NAME
        }
        
        enum TRUSTEE_TYPE
        {
            TRUSTEE_IS_UNKNOWN,
            TRUSTEE_IS_USER,
            TRUSTEE_IS_GROUP,
            TRUSTEE_IS_DOMAIN,
            TRUSTEE_IS_ALIAS,
            TRUSTEE_IS_WELL_KNOWN_GROUP,
            TRUSTEE_IS_DELETED,
            TRUSTEE_IS_INVALID,
            TRUSTEE_IS_COMPUTER
        }

        enum SE_OBJECT_TYPE
        {
            SE_UNKNOWN_OBJECT_TYPE,
            SE_FILE_OBJECT,
            SE_SERVICE,
            SE_PRINTER,
            SE_REGISTRY_KEY,
            SE_LMSHARE,
            SE_KERNEL_OBJECT,
            SE_WINDOW_OBJECT,
            SE_DS_OBJECT,
            SE_DS_OBJECT_ALL,
            SE_PROVIDER_DEFINED_OBJECT,
            SE_WMIGUID_OBJECT,
            SE_REGISTRY_WOW64_32KEY
        }

        enum SID_NAME_USE
        {
            SidTypeUser = 1,
            SidTypeGroup,
            SidTypeDomain,
            SidTypeAlias,
            SidTypeWellKnownGroup,
            SidTypeDeletedAccount,
            SidTypeInvalid,
            SidTypeUnknown,
            SidTypeComputer
        }

        /// <summary>
        /// Access Rights flags
        /// </summary>
        [Flags]
        public enum ACCESS_MASK : uint
        {
            DELETE = 0x00010000,
            READ_CONTROL = 0x00020000,
            WRITE_DAC = 0x00040000,
            WRITE_OWNER = 0x00080000,
            SYNCHRONIZE = 0x00100000,

            STANDARD_RIGHTS_REQUIRED = 0x000f0000,

            STANDARD_RIGHTS_READ = 0x00020000,
            STANDARD_RIGHTS_WRITE = 0x00020000,
            STANDARD_RIGHTS_EXECUTE = 0x00020000,

            STANDARD_RIGHTS_ALL = 0x001f0000,

            SPECIFIC_RIGHTS_ALL = 0x0000ffff,

            ACCESS_SYSTEM_SECURITY = 0x01000000,

            MAXIMUM_ALLOWED = 0x02000000,

            GENERIC_READ = 0x80000000,
            GENERIC_WRITE = 0x40000000,
            GENERIC_EXECUTE = 0x20000000,
            GENERIC_ALL = 0x10000000,

            DESKTOP_READOBJECTS = 0x00000001,
            DESKTOP_CREATEWINDOW = 0x00000002,
            DESKTOP_CREATEMENU = 0x00000004,
            DESKTOP_HOOKCONTROL = 0x00000008,
            DESKTOP_JOURNALRECORD = 0x00000010,
            DESKTOP_JOURNALPLAYBACK = 0x00000020,
            DESKTOP_ENUMERATE = 0x00000040,
            DESKTOP_WRITEOBJECTS = 0x00000080,
            DESKTOP_SWITCHDESKTOP = 0x00000100,

            WINSTA_ENUMDESKTOPS = 0x00000001,
            WINSTA_READATTRIBUTES = 0x00000002,
            WINSTA_ACCESSCLIPBOARD = 0x00000004,
            WINSTA_CREATEDESKTOP = 0x00000008,
            WINSTA_WRITEATTRIBUTES = 0x00000010,
            WINSTA_ACCESSGLOBALATOMS = 0x00000020,
            WINSTA_EXITWINDOWS = 0x00000040,
            WINSTA_ENUMERATE = 0x00000100,
            WINSTA_READSCREEN = 0x00000200,

            WINSTA_ALL_ACCESS = 0x0000037f
        }

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern void BuildTrusteeWithSid(
            IntPtr pTrustee,
            [MarshalAs(UnmanagedType.LPArray)] byte[] pSid);

        [DllImport("advapi32.dll", SetLastError = true)]
        static extern int GetNamedSecurityInfo(
            String objectName,
            SE_OBJECT_TYPE objectType,
            SECURITY_INFORMATION securityInfo,
            out IntPtr sidOwner,
            out IntPtr sidGroup,
            out IntPtr dacl,
            out IntPtr sacl,
            out IntPtr securityDescriptor);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern UInt32 GetEffectiveRightsFromAcl(
            IntPtr pacl,
            IntPtr pTrustee,
            ref ACCESS_MASK pAccessRights);

        [DllImport("advapi32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        static extern bool LookupAccountName(
            string lpSystemName,
            string lpAccountName,
            [MarshalAs(UnmanagedType.LPArray)] byte[] Sid,
            ref uint cbSid,
            StringBuilder ReferencedDomainName,
            ref uint cchReferencedDomainName,
            out SID_NAME_USE peUse);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern IntPtr LocalFree(
            IntPtr handle
        );
        #endregion
    }
}