//---------------------------------------------------------------------
// <copyright file="InstallationPart.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Part of the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.WindowsInstaller
{
    /// <summary>
    /// Subclasses of this abstract class represent an instance
    /// of a registered feature or component.
    /// </summary>
    public abstract class InstallationPart
    {
        private string id;
        private string productCode;
        private string userSid;
        private UserContexts context;

        internal InstallationPart(string id, string productCode)
	    : this(id, productCode, null, UserContexts.None)
        {
        }

        internal InstallationPart(string id, string productCode, string userSid, UserContexts context)
        {
            this.id = id;
            this.productCode = productCode;
            this.userSid = userSid;
            this.context = context;
        }

        internal string Id
        {
            get
            {
                return this.id;
            }
        }

        internal string ProductCode
        {
            get
            {
                return this.productCode;
            }
        }

        internal string UserSid
        {
            get
            {
                return this.userSid;
            }
        }

        internal UserContexts Context
        {
            get
            {
                return this.context;
            }
        }

        /// <summary>
        /// Gets the product that this item is a part of.
        /// </summary>
        public ProductInstallation Product
        {
            get
            {
                return this.productCode != null ?
                    new ProductInstallation(this.productCode, userSid, context) : null;
            }
        }

        /// <summary>
        /// Gets the current installation state of the item.
        /// </summary>
        public abstract InstallState State
        {
            get;
        }
    }

}
