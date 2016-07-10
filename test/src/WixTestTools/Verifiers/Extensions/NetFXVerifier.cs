// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Verifiers.Extensions
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.DirectoryServices;
    using System.Security.Cryptography.X509Certificates;

    /// <summary>
    /// Contains methods for NetFX Extension test verification
    /// </summary>
    public static class NetFXVerifier
    {
        public enum FrameworkVersion
        {
            NetFX20,
            NetFX40
        };

        public enum FrameworkArch
        {
            x86,
            x64
        };

        public static bool NativeImageExists(string fileName, FrameworkVersion version, FrameworkArch arch)
        {
            string assymblyFolder = Path.Combine(Environment.ExpandEnvironmentVariables("%WInDir%"), "assembly");
            string nativeImageFileName = Path.GetFileNameWithoutExtension(fileName) + ".ni" + Path.GetExtension(fileName);
            string nativeImageFolderName = "NativeImages";
            
            if (FrameworkVersion.NetFX20 == version)
            {
                nativeImageFolderName += "_v2.0.50727";
            }
            else if (FrameworkVersion.NetFX40 == version)
            {
                // version number will keep changing up untill 4.0 RTM
                nativeImageFolderName += "_v4.0.*";
            }

            if (FrameworkArch.x86 == arch)
            {
                nativeImageFolderName += "_32";
            }
            else if (FrameworkArch.x64 == arch)
            {
                nativeImageFolderName += "_64";
            }
            
            // search for all directories matching the widcard to suppor 4.0
            DirectoryInfo directory = new DirectoryInfo(assymblyFolder);
            DirectoryInfo[] nativeImageDirectoryList = directory.GetDirectories(nativeImageFolderName);

            if (null == nativeImageDirectoryList || nativeImageDirectoryList.Length < 1)
            {
                return false;
            }

            FileInfo[] nativeImageFileList = nativeImageDirectoryList[0].GetFiles(nativeImageFileName, SearchOption.AllDirectories);
            if (null == nativeImageFileList || nativeImageFileList.Length < 1)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
    }
}
