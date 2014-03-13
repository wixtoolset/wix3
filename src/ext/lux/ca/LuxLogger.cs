//-------------------------------------------------------------------------------------------------
// <copyright file="LuxLogger.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Lux unit-test framework logging classes.
// </summary>
//-------------------------------------------------------------------------------------------------


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
