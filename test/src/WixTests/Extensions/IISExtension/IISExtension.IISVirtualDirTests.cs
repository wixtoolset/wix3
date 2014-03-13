//-----------------------------------------------------------------------
// <copyright file="IISExtension.IISVirtualDirTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>IIS Extension IISVirtualDir tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.IISExtension
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using WixTest.Verifiers.Extensions;

    /// <summary>
    /// IIS extension IISVirtualDir element tests
    /// </summary>
    [TestClass]
    public class IISVirtualDirTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\IISExtension\IISVirtualDirTests");

        [TestMethod]
        [Description("Verify that the (IIsHttpHeader, IIsMimeMap, IIsWebApplication, IIsWebVirtualDir,CustomAction) Tables are created in the MSI and have expected data")]
        [Priority(1)]
        public void IISVirtualDir_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(IISVirtualDirTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Verify Custom Action contains the right data
            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("ConfigureIIs", 1, "IIsSchedule", "ConfigureIIs"),
                new CustomActionTableData("ConfigureIIsExec", 3073, "IIsSchedule", "ConfigureIIsExec"),
                new CustomActionTableData("StartMetabaseTransaction", 11265, "IIsExecute", "StartMetabaseTransaction"),
                new CustomActionTableData("RollbackMetabaseTransaction", 11521, "IIsExecute", "RollbackMetabaseTransaction"),
                new CustomActionTableData("CommitMetabaseTransaction", 11777, "IIsExecute", "CommitMetabaseTransaction"),
                new CustomActionTableData("WriteMetabaseChanges", 11265, "IIsExecute", "WriteMetabaseChanges"));
           
            // Verify IIsHttpHeader contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.IIsHttpHeader,
                new TableRow(IIsHttpHeaderColumns.HttpHeader.ToString(), "VDirHttpHeader"),
                new TableRow(IIsHttpHeaderColumns.ParentType.ToString(), "1", false),
                new TableRow(IIsHttpHeaderColumns.ParentValue.ToString(), "vdir1"),
                new TableRow(IIsHttpHeaderColumns.Name.ToString(), "VDirHttpHeader"),
                new TableRow(IIsHttpHeaderColumns.Value.ToString(), "Http Header For VDir"),
                new TableRow(IIsHttpHeaderColumns.Attributes.ToString(), "0", false),
                new TableRow(IIsHttpHeaderColumns.Sequence.ToString(), string.Empty, false));
           
            // Verify IIsMimeMap contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.IIsMimeMap,
                new TableRow(IIsMimeMapColumns.MimeMap.ToString(), "BBMimeMapTest1"),
                new TableRow(IIsMimeMapColumns.ParentType.ToString(), "1", false),
                new TableRow(IIsMimeMapColumns.ParentValue.ToString(), "vdir1"),
                new TableRow(IIsMimeMapColumns.MimeType.ToString(), "application/test1"),
                new TableRow(IIsMimeMapColumns.Extension.ToString(), ".foo1"));
          
           // Verify IIsWebVirtualDir contains the right data
           Verifier.VerifyTableData(msiFile, MSITables.IIsWebVirtualDir,
               new TableRow(IIsWebVirtualDirColumns.VirtualDir.ToString(), "vdir1"),
               new TableRow(IIsWebVirtualDirColumns.Component_.ToString(), "TestWebSiteProductComponent"),
               new TableRow(IIsWebVirtualDirColumns.Web_.ToString(), "Test"),
               new TableRow(IIsWebVirtualDirColumns.Alias.ToString(), "test1"),
               new TableRow(IIsWebVirtualDirColumns.Directory_.ToString(), "TestWebSiteProductDirectory"),
               new TableRow(IIsWebVirtualDirColumns.DirProperties_.ToString(), "ReadAndExecute"),
               new TableRow(IIsWebVirtualDirColumns.Application_.ToString(), "VDirTestApp1"));

           // Verify IIsWebApplication contains the right data
           Verifier.VerifyTableData(msiFile, MSITables.IIsWebApplication,
               new TableRow(IIsWebApplicationColumns.Application.ToString(), "VDirTestApp1"),
               new TableRow(IIsWebApplicationColumns.Name.ToString(), "Virtual Directory Test ASP Application"),
               new TableRow(IIsWebApplicationColumns.Isolation.ToString(), "1", false),
               new TableRow(IIsWebApplicationColumns.AllowSessions.ToString(), string.Empty, false),
               new TableRow(IIsWebApplicationColumns.SessionTimeout.ToString(), string.Empty, false),
               new TableRow(IIsWebApplicationColumns.Buffer.ToString(), string.Empty, false),
               new TableRow(IIsWebApplicationColumns.ParentPaths.ToString(), string.Empty, false),
               new TableRow(IIsWebApplicationColumns.DefaultScript.ToString(), string.Empty),
               new TableRow(IIsWebApplicationColumns.ScriptTimeout.ToString(), string.Empty, false),
               new TableRow(IIsWebApplicationColumns.ServerDebugging.ToString(), string.Empty, false),
               new TableRow(IIsWebApplicationColumns.ClientDebugging.ToString(), string.Empty, false),
               new TableRow(IIsWebApplicationColumns.AppPool_.ToString(), string.Empty));
        }

        [TestMethod]
        [Description("Install the MSI. Verify that the Virtual directory for the web site was set correctly,Application Name is set to “Virtual Directory Test ASP Application”,custom Http headers has an entry for “VDirHttpHeader: Http Header For VDir”,Registered MIME type has an entry for “.foo1   application/test1 “ ")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISVirtualDir_Install()
        {
            string sourceFile = Path.Combine(IISVirtualDirTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the Virtual directory for the web site was set correctly
            Assert.IsTrue(IISVerifier.VirtualDirExist("test1", "Test web server"), "VirtualDir '{0}' in site '{1}' was not created on Install", "test1", "Test web server");

            // Verify that the Application Name is set to 'Virtual Directory Test ASP Application'
            Assert.IsTrue(IISVerifier.WebApplicationExist("Virtual Directory Test ASP Application", "test1", "Test web server"), "WebApplication '{0}' in site '{1}' was not created on Install", "Virtual Directory Test ASP Application", "Test web server");

            // Verify that the custom Http headers has an entry for 'VDirHttpHeader: Http Header For VDir'
            Assert.IsTrue(IISVerifier.CustomHeadderExist("VDirHttpHeader: Http Header For VDir", "test1", "Test web server"), "CustomHeadder '{0}' in site '{1}' was not created on Install", "VDirHttpHeader: Http Header For VDir", "Test web server");
            
            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the Virtual directory was removed
            Assert.IsFalse(IISVerifier.VirtualDirExist("test1", "Test web server"), "VirtualDir '{0}' in site '{1}' was not removed on Uninstall", "test1", "Test web server");
        }

        [TestMethod]
        [Description("Cancel installation. Verify that the VirtualDir was not created.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISVirtualDir_InstallFailure()
        {
            string sourceFile = Path.Combine(IISVirtualDirTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify that the Virtual directory was removed
            Assert.IsFalse(IISVerifier.VirtualDirExist("test1", "Test web server"), "VirtualDir '{0}' in site '{1}' was not removed on Rollback", "test1", "Test web server");
        }

        [TestMethod]
        [Description("Install the MSI. Verify that ALL the Virtual directory for the web site were set correctly.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void IISVirtualDir_MultipleDirectories_Install()
        {
            string sourceFile = Path.Combine(IISVirtualDirTests.TestDataDirectory, @"MultipleDirectories.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixIIsExtension");

            // Install  Msi
            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the Virtual directory for the web site was set correctly
            Assert.IsTrue(IISVerifier.VirtualDirExist("WebAppTest_Low", "Default Web Site"), "VirtualDir '{0}' in site '{1}' was not created on Install", "WebAppTest_Low", "Default Web Site");
            Assert.IsTrue(IISVerifier.VirtualDirExist("WebAppTest_Medium", "Default Web Site"), "VirtualDir '{0}' in site '{1}' was not created on Install", "WebAppTest_Medium", "Default Web Site"); 
            Assert.IsTrue(IISVerifier.VirtualDirExist("WebAppTest_High", "Default Web Site"), "VirtualDir '{0}' in site '{1}' was not created on Install", "WebAppTest_High", "Default Web Site");

            // UnInstall Msi
            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the Virtual directory was removed
            Assert.IsFalse(IISVerifier.VirtualDirExist("WebAppTest_Low", "Default Web Site"), "VirtualDir '{0}' in site '{1}' was not removed on Uninstall", "WebAppTest_Low", "Default Web Site");
            Assert.IsFalse(IISVerifier.VirtualDirExist("WebAppTest_Medium", "Default Web Site"), "VirtualDir '{0}' in site '{1}' was not removed on Uninstall", "WebAppTest_Medium", "Default Web Site");
            Assert.IsFalse(IISVerifier.VirtualDirExist("WebAppTest_High", "Default Web Site"), "VirtualDir '{0}' in site '{1}' was not removed on Uninstall", "WebAppTest_High", "Default Web Site");
        }


        [TestMethod]
        [Description("Verify that the expected Candle error is shown for an invalid Mime Map extension.")]
        [Priority(3)]
        public void IISVirtualDir_InvalidMimeMapExtension()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(IISVirtualDirTests.TestDataDirectory, @"InvalidMimeMapExtension.wxs"));
            candle.Extensions.Add("WixIIsExtension");
            
            candle.ExpectedWixMessages.Add(new WixMessage(5150, "The iis:MimeMap/@Extension attribute's value, '*.foo1', is not a valid mime map extension.  It must begin with a period.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5150;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that the expected Candle error is shown when web site attribute is defined for vitual dir that is nested in a web site element.")]
        [Priority(3)]
        public void IISVirtualDir_WebSiteRedifinition()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(IISVirtualDirTests.TestDataDirectory, @"WebSiteRedifinition.wxs"));
            candle.Extensions.Add("WixIIsExtension");

            candle.ExpectedWixMessages.Add(new WixMessage(5154, "The iis:WebVirtualDir/@WebSite attribute cannot be specified when the iis:WebVirtualDir element is nested under a WebSite element.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5154;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that the expected Candle error is shown when VirtualDir element has multiple WebApplication elements.")]
        [Priority(3)]
        public void IISVirtualDir_MultipleWebApplicationElements()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(IISVirtualDirTests.TestDataDirectory, @"MultipleWebApplicationElements.wxs"));
            candle.Extensions.Add("WixIIsExtension");

            candle.ExpectedWixMessages.Add(new WixMessage(5155, "The iis:WebVirtualDir element can have at most a single WebApplication specified. This can be either through the WebApplication attribute, or through a nested WebApplication element, but not both.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5155;
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that the expected Candle error is shown when VirtualDir element has an invlaid @Alias.")]
        [Priority(3)]
        public void IISVirtualDir_InvalidAliasValue()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(IISVirtualDirTests.TestDataDirectory, @"InvalidAliasValue.wxs"));
            candle.Extensions.Add("WixIIsExtension");

            candle.ExpectedWixMessages.Add(new WixMessage(5156, @"The iis:WebVirtualDir/@Alias attribute's value, '\test1', is invalid.  It cannot contain the character '\'.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 5156;
            candle.Run();
        }
    }
}
