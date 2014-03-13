//-----------------------------------------------------------------------
// <copyright file="UserUtilities.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     - Contains methods used for getting values from another users environment.
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Utilities
{
    using System.Collections.Generic;
    using System.IO;

    public class UserUtilities
    {
        /// <summary>
        /// To get the temp paths of all users in given system
        /// </summary>
        /// <returns>List containing user temp paths</returns>
        public static List<string> GetAllUserTempPaths()
        {
            List<string> tempDirs = new List<string>();

            string tempPath = System.Environment.ExpandEnvironmentVariables("%TEMP%");
            string userName = System.Environment.ExpandEnvironmentVariables("%USERNAME%");
            string userRoot = tempPath.Substring(0, tempPath.ToLower().IndexOf(userName.ToLower()));

            foreach (string directory in Directory.GetDirectories(userRoot))
            {
                string userTemp = tempPath.Replace(Path.Combine(userRoot, userName), directory);
                if (Directory.Exists(userTemp))
                {
                    tempDirs.Add(userTemp);
                }
            }

            return tempDirs;
        }

        /// <summary>
        /// To get LocalAppData path of all users
        /// </summary>
        /// <returns>List containing user LocalAppData path</returns>
        public static List<string> GetAllUserLocalAppDataPaths()
        {
            List<string> localAddDataDirs = new List<string>();

            string localAppData = System.Environment.ExpandEnvironmentVariables("%LOCALAPPDATA%");
            string userName = System.Environment.ExpandEnvironmentVariables("%USERNAME%");
            string userRoot = localAppData.Substring(0, localAppData.ToLower().IndexOf(userName.ToLower()));

            foreach (string directory in Directory.GetDirectories(userRoot))
            {
                string userLocalAppData = localAppData.Replace(Path.Combine(userRoot, userName), directory);
                if (Directory.Exists(userLocalAppData))
                {
                    localAddDataDirs.Add(userLocalAppData);
                }
            }

            return localAddDataDirs;

        }

        /// <summary>
        /// Returns the full path to the cache root folder for the specified user
        /// </summary>
        /// <param name="userName">name of user</param>
        /// <param name="folderName">folder name of the cache (i.e. Bundle cache or package cache can be in different root folders)</param>
        /// <returns></returns>
        public static string GetCacheRoot(string userName, string folderName)
        {
            string cacheRoot = string.Empty;

            // non admin (normal user) installed a per-user package
            foreach (string localAppData in UserUtilities.GetAllUserLocalAppDataPaths())
            {
                if (localAppData.Contains(userName))
                {
                    cacheRoot = Path.Combine(localAppData, folderName);
                    break;
                }
            }
            return cacheRoot;
        }

    }
}
