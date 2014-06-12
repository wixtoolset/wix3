//-------------------------------------------------------------------------------------------------
// <copyright file="FileVerifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//      Contains methods for verification for files and directories
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixTest.Verifiers
{
    using System;
    using System.IO;
    using System.Security.Cryptography;
    using Xunit;

    /// <summary>
    /// The FileVerifier contains methods for verification for files and directories
    /// </summary>
    public class FileVerifier
    {
        /// <summary>
        /// Computes the SHA1 hash for a file and returns the output formated as a string.
        /// </summary>
        /// <param name="filePath">File to compute the SHA1 hash for.</param>
        /// <returns>String representation of the SHA1 hash of the input file.</returns>
        public static string ComputeFileSHA1Hash(string filePath)
        {
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] result = sha.ComputeHash(new FileStream(filePath, FileMode.Open,FileAccess.Read));

            // convert the byte array into string
            string hash = string.Empty;
            foreach (byte value in result)
            {
                hash += string.Format("{0:X2}", value);
            }

            return hash;
        }

        /// <summary>
        /// Verify two files are identical, though comparing hashes.
        /// </summary>
        /// <param name="file1Path">Path to the first file</param>
        /// <param name="file2Path">Path to the second file</param>
        public static void VerifyFilesAreIdentical(string file1Path, string file2Path)
        {
            string fileHash1 = ComputeFileSHA1Hash(file1Path);
            string fileHash2 = ComputeFileSHA1Hash(file2Path);

            Assert.True(fileHash1 == fileHash2, String.Format("Files '{0}' and '{1}' have diffrent hash values. The files are not identical.", file1Path, file2Path));
        }
    }
}
