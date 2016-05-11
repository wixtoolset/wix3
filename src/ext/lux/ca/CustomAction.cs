// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Lux.CustomActions
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// MSI Custom Actions
    /// </summary>
    public class CustomActions
    {
        #region Custom Actions
        /// <summary>
        /// Runs through the Lux unit tests table at the end of the immediate CA
        /// sequence (before InstallFinalize).
        /// </summary>
        /// <param name="session">MSI session handle</param>
        /// <returns>Always returns ActionResult.UserExit.</returns>
        [CustomAction]
        public static ActionResult WixRunImmediateUnitTests(Session session)
        {
            LuxUnitTestFactory factory = new LuxUnitTestFactory();
            LuxLogger logger = new LuxLogger(session);
            string sql = String.Concat("SELECT `WixUnitTest`, `CustomAction_`, `Property`, `Operator`, `Value`, `Expression`, `Condition`, `ValueSeparator`, `NameValueSeparator`, `Index` FROM ", Constants.LuxTableName, " WHERE `Mutation` IS NULL");
            string mutation = session[Constants.LuxMutationRunningProperty];
            if (!String.IsNullOrEmpty(mutation))
            {
                sql = String.Concat(sql, " OR `Mutation` = '", mutation, "'");
            }

            using (View view = session.Database.OpenView(sql))
            {
                view.Execute();

                foreach (Record rec in view)
                {
                    using (rec)
                    {
                        LuxUnitTest unitTest = factory.CreateUnitTest(session, rec, logger);
                        if (null != unitTest)
                        {
                            if (unitTest.IsTestConditionMet())
                            {
                                unitTest.RunTest();
                            }
                            unitTest.LogResult();
                        }
                    }
                }
            }

            return ActionResult.UserExit;    
        }
        #endregion
    }
}
