//-----------------------------------------------------------------------
// <copyright file="Components.ServiceControlTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for controlling services that are installed
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
    /// Tests for controlling services that are installed
    /// </summary>
    [TestClass]
    public class ServiceControlTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\ServiceControlTests");

        [TestMethod]
        [Description("Verify that a service can be configured and that values are defaulted correctly")]
        [Priority(1)]
        public void ServiceControl()
        {
            string sourceFile = Path.Combine(ServiceControlTests.TestDataDirectory, @"ServiceControl\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `Action` FROM `InstallExecuteSequence` WHERE `Action` = 'DeleteServices'";
            string query2 = "SELECT `Action` FROM `InstallExecuteSequence` WHERE `Action` = 'StartServices'";
            string query3 = "SELECT `Action` FROM `InstallExecuteSequence` WHERE `Action` = 'StopServices'";
            Verifier.VerifyQuery(msi, query1, "DeleteServices");
            Verifier.VerifyQuery(msi, query2, "StartServices");
            Verifier.VerifyQuery(msi, query3, "StopServices");
        }

        [TestMethod]
        [Description("Verify that there is an error if the service has an invalid Name")]
        [Priority(3)]
        public void InvalidServiceControlName()
        {
            string sourceFile = Path.Combine(ServiceControlTests.TestDataDirectory, @"InvalidServiceControlName\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 104;
            candle.ExpectedWixMessages.Add(new WixMessage(104, "Not a valid source file; detail: An error occurred while parsing EntityName. Line 16, position 46.", WixMessage.MessageTypeEnum.Error));
            candle.IgnoreExtraWixMessages = true;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that Wait can be set to yes or no")]
        [Priority(1)]
        public void Wait()
        {
            string sourceFile = Path.Combine(ServiceControlTests.TestDataDirectory, @"Wait\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `Wait` FROM `ServiceControl` WHERE `ServiceControl` = 'Service1'";
            string query2 = "SELECT `Wait` FROM `ServiceControl` WHERE `ServiceControl` = 'Service2'";
            Verifier.VerifyQuery(msi, query1, "1");
            Verifier.VerifyQuery(msi, query2, "0");
        }

        [TestMethod]
        [Description("Verify that there is an error for conflicting actions. See code comments for details.")]
        [Priority(1)]
        [Ignore]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=3023803&group_id=105970")]      
        public void ConflictingActions()
        {
            // Remove and Start on install
            // Remove and Start on both
            
            // Stop and Start on install
            // Stop and Start on both

            // Remove and stop on uninstall

        }

        [TestMethod]
        [Description("Verify that a service argument can be specified")]
        [Priority(1)]
        public void ServiceArgument()
        {
            string sourceFile = Path.Combine(ServiceControlTests.TestDataDirectory, @"ServiceArgument\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `Arguments` FROM `ServiceControl` WHERE `ServiceControl` = 'Service1'";
            Verifier.VerifyQuery(msi, query1, "Arg1");
        }

        [TestMethod]
        [Description("Verify that multiple ServiceArgument can be specified")]
        [Priority(1)]
        public void MultipleServiceArguments()
        {
            string sourceFile = Path.Combine(ServiceControlTests.TestDataDirectory, @"MultipleServiceArguments\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            string query1 = "SELECT `Arguments` FROM `ServiceControl` WHERE `ServiceControl` = 'Service1'";
            Verifier.VerifyQuery(msi, query1, "Arg1[~]Arg2");
        }

        [TestMethod]
        [Description("Verify that DeleteServices action is scheduled with ServiceControl attribute Remove=both")]
        [Priority(2)]
        public void RemoveBoth()
        {
            string msi = Builder.BuildPackage(Path.Combine(ServiceControlTests.TestDataDirectory, @"RemoveBoth\product.wxs"));
      
            string query = "SELECT `Action` FROM `InstallExecuteSequence` WHERE `Action` = 'DeleteServices'";
            Verifier.VerifyQuery(msi, query, "DeleteServices");
        }
    }
}
