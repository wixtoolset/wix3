//-------------------------------------------------------------------------------------------------
// <copyright file="Uuid.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <remarks>
//     The code in this file is derived from a sample implementation
//     found in RFC 4122 which has the following copyright notice:
//
//     Copyright (c) 1990- 1993, 1996 Open Software Foundation, Inc.
//     Copyright (c) 1989 by Hewlett-Packard Company, Palo Alto, Ca. &amp;
//     Digital Equipment Corporation, Maynard, Mass.
// 
//     To anyone who acknowledges that this file is provided "AS IS"
//     without any express or implied warranty: permission to use, copy,
//     modify, and distribute this file for any purpose is hereby
//     granted without fee, provided that the above copyright notices and
//     this notice appears in all source code copies, and that none of
//     the names of Open Software Foundation, Inc., Hewlett-Packard
//     Company, Microsoft, or Digital Equipment Corporation be used in
//     advertising or publicity pertaining to distribution of the software
//     without specific, written prior permission. Neither Open Software
//     Foundation, Inc., Hewlett-Packard Company, Microsoft, nor Digital
//     Equipment Corporation makes any representations about the
//     suitability of this software for any purpose.
// </remarks>
//
// <summary>
// Implementation of RFC 4122 - A Universally Unique Identifier (UUID) URN Namespace.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;
    using System.Net;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Implementation of RFC 4122 - A Universally Unique Identifier (UUID) URN Namespace.
    /// </summary>
    internal sealed class Uuid
    {
        /// <summary>
        /// Protect the constructor.
        /// </summary>
        private Uuid()
        {
        }

        /// <summary>
        /// Creates a version 3 name-based UUID.
        /// </summary>
        /// <param name="namespaceGuid">The namespace UUID.</param>
        /// <param name="value">The value.</param>
        /// <param name="backwardsCompatible">Flag to say to use MD5 instead of better SHA1.</param>
        /// <returns>The UUID for the given namespace and value.</returns>
        public static Guid NewUuid(Guid namespaceGuid, string value, bool backwardsCompatible)
        {
            byte[] namespaceBytes = namespaceGuid.ToByteArray();
            short uuidVersion = backwardsCompatible ? (short)0x3000 : (short)0x5000;

            // get the fields of the guid which are in host byte ordering
            int timeLow = BitConverter.ToInt32(namespaceBytes, 0);
            short timeMid = BitConverter.ToInt16(namespaceBytes, 4);
            short timeHiAndVersion = BitConverter.ToInt16(namespaceBytes, 6);

            // convert to network byte ordering
            timeLow = IPAddress.HostToNetworkOrder(timeLow);
            timeMid = IPAddress.HostToNetworkOrder(timeMid);
            timeHiAndVersion = IPAddress.HostToNetworkOrder(timeHiAndVersion);

            // get the bytes from the value
            byte[] valueBytes = Encoding.Unicode.GetBytes(value);

            // fill-in the hash input buffer
            byte[] buffer = new byte[namespaceBytes.Length + valueBytes.Length];
            Buffer.BlockCopy(BitConverter.GetBytes(timeLow), 0, buffer, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(timeMid), 0, buffer, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(timeHiAndVersion), 0, buffer, 6, 2);
            Buffer.BlockCopy(namespaceBytes, 8, buffer, 8, 8);
            Buffer.BlockCopy(valueBytes, 0, buffer, 16, valueBytes.Length);

            // perform the appropriate hash of the namespace and value
            byte[] hash;
            if (backwardsCompatible)
            {
                using (MD5 md5 = MD5.Create())
                {
                    hash = md5.ComputeHash(buffer);
                }
            }
            else
            {
                using (SHA1 sha1 = SHA1.Create())
                {
                    hash = sha1.ComputeHash(buffer);
                }
            }

            // get the fields of the hash which are in network byte ordering
            timeLow = BitConverter.ToInt32(hash, 0);
            timeMid = BitConverter.ToInt16(hash, 4);
            timeHiAndVersion = BitConverter.ToInt16(hash, 6);

            // convert to network byte ordering
            timeLow = IPAddress.NetworkToHostOrder(timeLow);
            timeMid = IPAddress.NetworkToHostOrder(timeMid);
            timeHiAndVersion = IPAddress.NetworkToHostOrder(timeHiAndVersion);

            // set the version and variant bits
            timeHiAndVersion &= 0x0FFF;
            timeHiAndVersion += uuidVersion;
            hash[8] &= 0x3F;
            hash[8] |= 0x80;

            // put back the converted values into a 128-bit value
            byte[] guidBits = new byte[16];
            Buffer.BlockCopy(hash, 0, guidBits, 0, 16);

            Buffer.BlockCopy(BitConverter.GetBytes(timeLow), 0, guidBits, 0, 4);
            Buffer.BlockCopy(BitConverter.GetBytes(timeMid), 0, guidBits, 4, 2);
            Buffer.BlockCopy(BitConverter.GetBytes(timeHiAndVersion), 0, guidBits, 6, 2);

            return new Guid(guidBits);
        }
    }
}
