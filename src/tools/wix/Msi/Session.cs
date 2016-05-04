// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
