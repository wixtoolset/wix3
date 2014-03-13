//-------------------------------------------------------------------------------------------------
// <copyright file="FirewallConstants.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Constants used by FirewallExtension
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Text;

    static class FirewallConstants
    {
        // from icftypes.h
        public const int NET_FW_IP_PROTOCOL_TCP = 6;
        public const int NET_FW_IP_PROTOCOL_UDP = 17;

        // from icftypes.h
        public const int NET_FW_PROFILE2_DOMAIN = 0x0001;
        public const int NET_FW_PROFILE2_PRIVATE = 0x0002;
        public const int NET_FW_PROFILE2_PUBLIC = 0x0004;
        public const int NET_FW_PROFILE2_ALL = 0x7FFFFFFF;
    }
}
