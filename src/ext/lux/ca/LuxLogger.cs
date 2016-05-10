// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Lux.CustomActions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// Logging class for Lux CAs.
    /// </summary>
    public class LuxLogger
    {
        private Session session;

        /// <summary>
        /// Initializes a new instance of the LuxLogger class.
        /// </summary>
        /// <param name="session">MSI session handle</param>
        public LuxLogger(Session session)
        {
            this.session = session;
        }

        /// <summary>
        /// Logs a message to the MSI log using the MSI error table.
        /// </summary>
        /// <param name="id">Error message id</param>
        /// <param name="args">Arguments to the error message (will be inserted into the formatted error message).</param>
        public void Log(int id, params string[] args)
        {
            // field 0 is free, field 1 is the id, fields 2..n are arguments
            using (Record rec = new Record(1 + args.Length))
            {
                rec.SetInteger(1, id);
                for (int i = 0; i < args.Length; i++)
                {
                    rec.SetString(2 + i, args[i]);
                }

                this.session.Message(InstallMessage.User, rec);
            }
        }
    }
}
