//-----------------------------------------------------------------------
// <copyright file="UtilExtension.UserTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension User tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
    using Xunit;

    /// <summary>
    /// Util extension User element tests
    /// </summary>
    public class UserTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\UserTests");

        protected override void TestInitialize()
        {
            base.TestInitialize();

            // set the environment variable to store the current user information
            string username = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            Environment.SetEnvironmentVariable("tempdomain", username.Split('\\')[0]);
            Environment.SetEnvironmentVariable("tempusername", username.Split('\\')[1]);
        }

        [NamedFact]
        [Description("Verify that the (User and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void User_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(UserTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureUsers", 1, "ScaSchedule", "ConfigureUsers"),
                new CustomActionTableData("CreateUser", 11265, "ScaExecute", "CreateUser"),
                new CustomActionTableData("CreateUserRollback", 11521, "ScaExecute", "RemoveUser"),
                new CustomActionTableData("RemoveUser", 11841, "ScaExecute", "RemoveUser"));

            // Verify User table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.User,
                new TableRow(UserColumns.User.ToString(), "TEST_USER1"),
                new TableRow(UserColumns.Component_.ToString(), "Component1"),
                new TableRow(UserColumns.Name.ToString(), "testName1"),
                new TableRow(UserColumns.Domain.ToString(), string.Empty),
                new TableRow(UserColumns.Password.ToString(), "test123!@#"),
                new TableRow(UserColumns.Attributes.ToString(), "4", false));

            Verifier.VerifyTableData(msiFile, MSITables.User,
                new TableRow(UserColumns.User.ToString(), "TEST_USER2"),
                new TableRow(UserColumns.Component_.ToString(), "Component1"),
                new TableRow(UserColumns.Name.ToString(), "testName2"),
                new TableRow(UserColumns.Domain.ToString(), string.Empty),
                new TableRow(UserColumns.Password.ToString(), "test123!@#"),
                new TableRow(UserColumns.Attributes.ToString(), "297", false));

            Verifier.VerifyTableData(msiFile, MSITables.User,
               new TableRow(UserColumns.User.ToString(), "TEST_USER3"),
               new TableRow(UserColumns.Component_.ToString(), "Component1"),
               new TableRow(UserColumns.Name.ToString(), Environment.GetEnvironmentVariable("tempusername")),
               new TableRow(UserColumns.Domain.ToString(), Environment.GetEnvironmentVariable("tempdomain")),
               new TableRow(UserColumns.Password.ToString(), string.Empty),
               new TableRow(UserColumns.Attributes.ToString(), "512", false));

            // Verify Group table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.Group,
                new TableRow(GroupColumns.Group.ToString(), "ADMIN"),
                new TableRow(GroupColumns.Component_.ToString(), string.Empty),
                new TableRow(GroupColumns.Name.ToString(), "Administrators"),
                new TableRow(GroupColumns.Domain.ToString(), string.Empty));

            Verifier.VerifyTableData(msiFile, MSITables.Group,
                new TableRow(GroupColumns.Group.ToString(), "POWER_USER"),
                new TableRow(GroupColumns.Component_.ToString(), string.Empty),
                new TableRow(GroupColumns.Name.ToString(), "Power Users"),
                new TableRow(GroupColumns.Domain.ToString(), string.Empty));

            // Verify UserGroup table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.UserGroup,
                new TableRow(UserGroupColumns.User_.ToString(), "TEST_USER1"),
                new TableRow(UserGroupColumns.Group_.ToString(), "ADMIN"));

            Verifier.VerifyTableData(msiFile, MSITables.UserGroup,
                new TableRow(UserGroupColumns.User_.ToString(), "TEST_USER1"),
                new TableRow(UserGroupColumns.Group_.ToString(), "POWER_USER"));

            Verifier.VerifyTableData(msiFile, MSITables.UserGroup,
                new TableRow(UserGroupColumns.User_.ToString(), "TEST_USER2"),
                new TableRow(UserGroupColumns.Group_.ToString(), "POWER_USER")
                );
            Verifier.VerifyTableData(msiFile, MSITables.UserGroup,
              new TableRow(UserGroupColumns.User_.ToString(), "TEST_USER3"),
              new TableRow(UserGroupColumns.Group_.ToString(), "POWER_USER"));
        }

        [NamedFact]
        [Description("Verify that the users specified in the authoring are created as expected.")]
        [Priority(2)]
        [RuntimeTest]
        public void User_Install()
        {
            string sourceFile = Path.Combine(UserTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New User Information.
            UserVerifier.VerifyUserInformation(string.Empty, "testName1", true, false, false);
            UserVerifier.VerifyUserIsMemberOf(string.Empty, "testName1", "Administrators", "Power Users");

            UserVerifier.VerifyUserInformation(string.Empty, "testName2", true, true, true);
            UserVerifier.VerifyUserIsMemberOf(string.Empty, "testName2", "Power Users");

            UserVerifier.VerifyUserIsMemberOf(Environment.GetEnvironmentVariable("tempdomain"), Environment.GetEnvironmentVariable("tempusername"), "Power Users");

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserVerifier.UserExists(string.Empty, "testName1"), String.Format("User '{0}' was not removed on Uninstall", "testName1"));
            Assert.True(UserVerifier.UserExists(string.Empty, "testName2"), String.Format("User '{0}' was removed on Uninstall", "testName2"));

            // clean up
            UserVerifier.DeleteLocalUser("testName2");
            
            UserVerifier.VerifyUserIsNotMemberOf(Environment.GetEnvironmentVariable("tempdomain"), Environment.GetEnvironmentVariable("tempusername"), "Power Users");
        }

        [NamedFact]
        [Description("Verify the rollback action reverts all Users changes.")]
        [Priority(2)]
        [RuntimeTest]
        public void User_InstallFailure()
        {
            string sourceFile = Path.Combine(UserTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            // make sure the user accounts are deleted before we start
            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.DeleteLocalUser("testName2");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserVerifier.UserExists(string.Empty, "testName1"), String.Format("User '{0}' was not removed on Rollback", "testName1"));
            Assert.False(UserVerifier.UserExists(string.Empty, "testName2"), String.Format("User '{0}' was not removed on Rollback", "testName2"));

            UserVerifier.VerifyUserIsNotMemberOf(Environment.GetEnvironmentVariable("tempdomain"), Environment.GetEnvironmentVariable("tempusername"), "Power Users");
        }

        [NamedFact]
        [Description("Verify that the users specified in the authoring are created as expected on repair.")]
        [Priority(2)]
        [RuntimeTest]
        public void User_Repair()
        {
            string sourceFile = Path.Combine(UserTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            UserVerifier.DeleteLocalUser("testName1");
            UserVerifier.SetUserInformation(string.Empty, "testName2", true, false, false);

            MSIExec.RepairProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Validate New User Information.
            UserVerifier.VerifyUserInformation(string.Empty, "testName1", true, false, false);
            UserVerifier.VerifyUserIsMemberOf(string.Empty, "testName1", "Administrators", "Power Users");

            UserVerifier.VerifyUserInformation(string.Empty, "testName2", true, true, true);
            UserVerifier.VerifyUserIsMemberOf(string.Empty, "testName2", "Power Users");

            UserVerifier.VerifyUserIsMemberOf(Environment.GetEnvironmentVariable("tempdomain"), Environment.GetEnvironmentVariable("tempusername"), "Power Users");

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify Users marked as RemoveOnUninstall were removed.
            Assert.False(UserVerifier.UserExists(string.Empty, "testName1"), String.Format("User '{0}' was not removed on Uninstall", "testName1"));
            Assert.True(UserVerifier.UserExists(string.Empty, "testName2"), String.Format("User '{0}' was removed on Uninstall", "testName2"));

            // clean up
            UserVerifier.DeleteLocalUser("testName2");

            UserVerifier.VerifyUserIsNotMemberOf(Environment.GetEnvironmentVariable("tempdomain"), Environment.GetEnvironmentVariable("tempusername"), "Power Users");
        }

        [NamedFact]
        [Description("Verify that Installation fails if FailIfExisits is set.")]
        [Priority(2)]
        [RuntimeTest]
        public void User_FailIfExists()
        {
            string sourceFile = Path.Combine(UserTests.TestDataDirectory, @"FailIfExists.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            // Create 'existinguser'
            UserVerifier.CreateLocalUser("existinguser", "test123!@#");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify User still exists.
            bool userExists =UserVerifier.UserExists(string.Empty, "existinguser");

            // Delete the user first
            UserVerifier.DeleteLocalUser("existinguser");

            Assert.True(userExists, String.Format("User '{0}' was removed on Rollback", "existinguser"));

            // clean up
            UserVerifier.DeleteLocalUser("existinguser");
        }

        [NamedFact]
        [Description("Verify that a user cannot be created on a domain on which you dont have create user permission.")]
        [Priority(2)]
        [RuntimeTest]
        public void User_RestrictedDomain()
        {
            string sourceFile = Path.Combine(UserTests.TestDataDirectory, @"RestrictedDomain.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            // Create 'existinguser'
            UserVerifier.CreateLocalUser("existinguser", "test123!@#");

            string logFile = MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // clean up
            UserVerifier.DeleteLocalUser("existinguser");

            // Verify expected error message in the log file
            Assert.True(LogVerifier.MessageInLogFile(logFile, string.Format("ConfigureUsers:  Error 0x80070035: Failed to check existence of domain: {0}, user: testName1",Environment.GetEnvironmentVariable("tempdomain"))) ||
                LogVerifier.MessageInLogFile(logFile, "CreateUser:  Error 0x80070005: failed to create user: testName1"), String.Format("Could not find CreateUser error message in log file: '{0}'.", logFile));
        }

        [NamedFact]
        [Description("Verify that adding a user to a non-existent group does not fail the install when non-vital.")]
        [Priority(2)]
        [RuntimeTest]
        public void User_NonVitalUserGroup()
        {
            string sourceFile = Path.Combine(UserTests.TestDataDirectory, @"NonVitalUserGroup.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);
        }
    }
}
