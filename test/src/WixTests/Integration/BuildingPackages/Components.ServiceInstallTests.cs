//-----------------------------------------------------------------------
// <copyright file="Components.ServiceInstallTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for configuring services that will be installed
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Tests for configuring Services of a component
    /// </summary>
    [TestClass]
    public class ServiceInstallTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\ServiceInstallTests");

        [TestMethod]
        [Description("Verify that a service can be added and that values are defaulted correctly")]
        [Priority(1)]
        public void ServiceInstall()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"ServiceInstall\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query = "SELECT `ServiceInstall` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            Verifier.VerifyQuery(msi, query, "SrvTest1");
        }

        [TestMethod]
        [Description("Verify that a user account can be specified when the ServiceType is ownProcess")]
        [Priority(1)]
        public void ValidAccount()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"ValidAccount\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query = "SELECT `StartName` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            Verifier.VerifyQuery(msi, query, "TestAccount");
        }

        [TestMethod]
        [Description("Verify that a user account cannot be specified when the ServiceType is not ownProcess")]
        [Priority(1)]
        [Ignore]
        [TestProperty("buglink", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=3011386&group_id=105970")]
        public void InvalidAccount()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"InvalidAccount\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query = "SELECT `StartName` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            Verifier.VerifyQuery(msi, query, "TestAccount");
        }

        [TestMethod]
        [Description("Verify that any characters, eg. '/' and '\' can be specified as command line arguments for the service")]
        [Priority(1)]
        public void Arguments()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"Arguments\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query = "SELECT `Arguments` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            Verifier.VerifyQuery(msi, query, @"\df/fdg");
        }

        [TestMethod]
        [Description("Verify that the service description is null if this attribute's value is Yes")]
        [Priority(1)]
        public void EraseDescription1()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"EraseDescription1\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query = "SELECT `Description` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            Verifier.VerifyQuery(msi, query, "[~]");
            //the description is "[~]" instead of null?
        }

        [TestMethod]
        [Description("Verify that the service description is not ignored if this attribute's value is No")]
        [Priority(1)]
        public void EraseDescription2()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"EraseDescription2\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query = "SELECT `Description` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            Verifier.VerifyQuery(msi, query, "my descriprion");
        }

        [TestMethod]
        [Description("Verify that there is an error if the ErrorControl attribute is missing")]
        [Priority(1)]
        public void MissingErrorControl()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"MissingErrorControl\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 107;
            candle.ExpectedWixMessages.Add(new WixMessage(107, "Schema validation failed with the following error at line 1, column 2744: The required attribute 'ErrorControl' is missing.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that ErrorControl can be properly set to its valid values: ignore, normal, critical")]
        [Priority(1)]
        public void ValidErrorControl()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"ValidErrorControl\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `ErrorControl` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            string query2 = "SELECT `ErrorControl` FROM `ServiceInstall` WHERE `Name`='SrvTest2'";
            string query3 = "SELECT `ErrorControl` FROM `ServiceInstall` WHERE `Name`='SrvTest3'";
            Verifier.VerifyQuery(msi, query1, "0");
            Verifier.VerifyQuery(msi, query2, "1");
            Verifier.VerifyQuery(msi, query3, "3");
        }

        [TestMethod]
        [Description("Verify that ErrorControl cannot be set to an invalid value")]
        [Priority(3)]
        public void InvalidErrorControl()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"InvalidErrorControl\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 21;
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The ServiceInstall/@ErrorControl attribute's value, 'abc', is not one of the legal options: 'ignore', 'normal', or 'critical'.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that Interactive can be set to Yes or No")]
        [Priority(1)]
        public void Interactive()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"Interactive\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `ServiceType` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            string query2 = "SELECT `ServiceType` FROM `ServiceInstall` WHERE `Name`='SrvTest2'";
            Verifier.VerifyQuery(msi, query1, "288");
            Verifier.VerifyQuery(msi, query2, "32");
        }

        [TestMethod]
        [Description("Verify that there is an error if the value of Name is an invalid service name")]
        [Priority(3)]
        public void Name()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"Name\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 104;
            candle.ExpectedWixMessages.Add(new WixMessage(104, "Not a valid source file; detail: An error occurred while parsing EntityName. Line 15, position 49.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error the Password attribute is set but the Account attribute is not")]
        [Priority(3)]
        [Ignore]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=3011388&group_id=105970")]
        public void Password()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"Password\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that all Windows Installer supported enumerations for Start are allowed (auto, demand, disabled)")]
        [Priority(1)]
        public void SupportedStartValues()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"SupportedStartValues\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `StartType` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            string query2 = "SELECT `StartType` FROM `ServiceInstall` WHERE `Name`='SrvTest2'";
            string query3 = "SELECT `StartType` FROM `ServiceInstall` WHERE `Name`='SrvTest3'";
            Verifier.VerifyQuery(msi, query1, "2");
            Verifier.VerifyQuery(msi, query2, "3");
            Verifier.VerifyQuery(msi, query3, "4");
        }

        [TestMethod]
        [Description("Verify that all Windows Installer unsupported enumerations for Start are not allowed (boot, system)")]
        [Priority(3)]
        public void UnsupportedStartValues()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"UnsupportedStartValues\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 10;
            candle.ExpectedWixMessages.Add(new WixMessage(73, "The ServiceInstall/@Start attribute's value, 'boot, is not supported by the Windows Installer.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(73, "The ServiceInstall/@Start attribute's value, 'system, is not supported by the Windows Installer.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(10, "The ServiceInstall/@Start attribute was not found; it is required.", WixMessage.MessageTypeEnum.Error));
            candle.IgnoreWixMessageOrder = true;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that all Windows Installer supported enumerations for Type are allowed (ownProcess, shareProcess)")]
        [Priority(1)]
        public void SupportedTypes()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"SupportedTypes\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `ServiceType` FROM `ServiceInstall` WHERE `Name`='SrvTest1'";
            string query2 = "SELECT `ServiceType` FROM `ServiceInstall` WHERE `Name`='SrvTest2'";
            Verifier.VerifyQuery(msi, query1, "32");
            Verifier.VerifyQuery(msi, query2, "16");
        }

        [TestMethod]
        [Description("Verify that all Windows Installer supported enumerations for Type are not allowed (kernelDriver, systemDriver)")]
        [Priority(3)]
        public void UnsupportedTypes()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"UnsupportedTypes\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 73;
            candle.ExpectedWixMessages.Add(new WixMessage(73, "The ServiceInstall/@Type attribute's value, 'kernelDriver, is not supported by the Windows Installer.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedWixMessages.Add(new WixMessage(73, "The ServiceInstall/@Type attribute's value, 'systemDriver, is not supported by the Windows Installer.", WixMessage.MessageTypeEnum.Error));
            candle.IgnoreWixMessageOrder = true;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that the Id of ServiceDependency can be the name of a previously installed service")]
        [Priority(1)]
        public void ServiceDependencyId1()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"ServiceDependencyId1\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `Dependencies` FROM `ServiceInstall` WHERE `Name`='SrvTest2'";
            Verifier.VerifyQuery(msi, query1, "SrvTestInstalled[~][~]");
        }

        [TestMethod]
        [Description("Verify that the Id of ServiceDependency can be the foreign key referring to another ServiceInstall/@Id")]
        [Priority(1)]
        public void ServiceDependencyId2()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"ServiceDependencyId2\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `Dependencies` FROM `ServiceInstall` WHERE `Name`='SrvTest2'";
            Verifier.VerifyQuery(msi, query1, "SrvTest1[~][~]");
        }

        [TestMethod]
        [Description("Verify that the Id of ServiceDependency can be a group of services")]
        [Priority(1)]
        public void ServiceDependencyId3()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"ServiceDependencyId3\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `Dependencies` FROM `ServiceInstall` WHERE `Name`='SrvTest2'";
            Verifier.VerifyQuery(msi, query1, "SrvTestInstalled1[~]SrvTestInstalled2[~]SrvTestInstalled3[~][~]");
        }

        [TestMethod]
        [Description("Verify that a ServiceInstall element can have multiple ServiceDependency children")]
        [Priority(1)]
        public void MultipleServiceDependencies()
        {
            string sourceFile = Path.Combine(ServiceInstallTests.TestDataDirectory, @"MultipleServiceDependencies\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `Dependencies` FROM `ServiceInstall` WHERE `Name`='SrvTest2'";
            Verifier.VerifyQuery(msi, query1, "+SrvTestInstalled1[~]+SrvTestInstalled2[~]+SrvTestInstalled3[~][~]");
        }

        [TestMethod]
        [Description("Verify that there is an error if ServiceDependency/@Id is a group of services, but ServiceDependency/@Group is 'No'")]
        [Priority(3)]
        [Ignore]
        [TestProperty("IsRuntimeTest", "true")]
        public void ServiceDependencyMissingGroupAttr()
        {
        }

        [TestMethod]
        [Description("Verify that there is an error if ServiceDependency/@Id is not a group of services, but ServiceDependency/@Group is 'No'")]
        [Priority(3)]
        [Ignore]
        [TestProperty("IsRuntimeTest", "true")]
        public void ServiceDependencyInvalidGroupAttr()
        {
        }
    }
}
