//-------------------------------------------------------------------------------------------------
// <copyright file="CustomAction.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The Lux unit-test framework custom action classes.
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
