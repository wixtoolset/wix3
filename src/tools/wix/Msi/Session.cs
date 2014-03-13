//-------------------------------------------------------------------------------------------------
// <copyright file="Session.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// Controls the installation process.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Msi
{
    using System;
    using System.ComponentModel;
    using System.Globalization;
    using Microsoft.Tools.WindowsInstallerXml.Msi.Interop;

    /// <summary>
    /// Controls the installation process.
    /// </summary>
    internal sealed class Session : MsiHandle
    {
        /// <summary>
        /// Instantiate a new Session.
        /// </summary>
        /// <param name="database">The database to open.</param>
        public Session(Database database)
        {
            string packagePath = String.Format(CultureInfo.InvariantCulture, "#{0}", (uint)database.Handle);

            uint handle = 0;
            int error = MsiInterop.MsiOpenPackage(packagePath, out handle);
            if (0 != error)
            {
                throw new MsiException(error);
            }
            this.Handle = handle;
        }

        /// <summary>
        /// Executes a built-in action, custom action, or user-interface wizard action.
        /// </summary>
        /// <param name="action">Specifies the action to execute.</param>
        public void DoAction(string action)
        {
            int error = MsiInterop.MsiDoAction(this.Handle, action);
            if (0 != error)
            {
                throw new MsiException(error);
            }
        }
    }
}
