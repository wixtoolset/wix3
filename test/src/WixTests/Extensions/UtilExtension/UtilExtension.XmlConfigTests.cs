// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
    /// Util extension XmlConfig element tests
    /// </summary>
    public class XmlConfigTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\XmlConfigTests");

        [NamedFact]
        [Description("Verify that the (XmlConfig and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void XmlConfig_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(XmlConfigTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(Environment.CurrentDirectory, sourceFile, "test.msi", "-ext WixUtilExtension", "-ext WixUtilExtension -sice:ICE03");
            Verifier.VerifyCustomActionTableData(msiFile,
                new CustomActionTableData("SchedXmlConfig", 1, "WixCA", "SchedXmlConfig"),
                new CustomActionTableData("ExecXmlConfig", 3073, "WixCA", "ExecXmlConfig"),
                new CustomActionTableData("ExecXmlConfigRollback", 3329, "WixCA", "ExecXmlConfigRollback"));

            // Verify XmlConfig table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.XmlConfig,
                new TableRow(XmlConfigColumns.XmlConfig.ToString(), "NewAttribute1Install"),
                new TableRow(XmlConfigColumns.File.ToString(), "[#TestXmlConfig1]"),
                new TableRow(XmlConfigColumns.ElementPath.ToString(), "/Root"),
                new TableRow(XmlConfigColumns.VerifyPath.ToString(), string.Empty),
                new TableRow(XmlConfigColumns.Name.ToString(), "New"),
                new TableRow(XmlConfigColumns.Value.ToString(), "hello"),
                new TableRow(XmlConfigColumns.Flags.ToString(), "274", false),
                new TableRow(XmlConfigColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlConfigColumns.Sequence.ToString(), string.Empty, false));

            Verifier.VerifyTableData(msiFile, MSITables.XmlConfig,
                new TableRow(XmlConfigColumns.XmlConfig.ToString(), "NewAttribute1Uninstall"),
                new TableRow(XmlConfigColumns.File.ToString(), "[#TestXmlConfig1]"),
                new TableRow(XmlConfigColumns.ElementPath.ToString(), "/Root"),
                new TableRow(XmlConfigColumns.VerifyPath.ToString(), string.Empty),
                new TableRow(XmlConfigColumns.Name.ToString(), "New"),
                new TableRow(XmlConfigColumns.Value.ToString(), "[NewAttributeValue]"),
                new TableRow(XmlConfigColumns.Flags.ToString(), "546", false),
                new TableRow(XmlConfigColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlConfigColumns.Sequence.ToString(), string.Empty, false));

            Verifier.VerifyTableData(msiFile, MSITables.XmlConfig,
               new TableRow(XmlConfigColumns.XmlConfig.ToString(), "NewNodeChild"),
               new TableRow(XmlConfigColumns.File.ToString(), "[#TestXmlConfig1]"),
               new TableRow(XmlConfigColumns.ElementPath.ToString(), "/Root/Child[\\[]@key=\"foo\"[\\]]"),
               new TableRow(XmlConfigColumns.VerifyPath.ToString(), "/Root/Child[\\[]@key=\"foo\"[\\]]/GrandChild"),
               new TableRow(XmlConfigColumns.Name.ToString(), "GrandChild"),
               new TableRow(XmlConfigColumns.Value.ToString(), "hi mom"),
               new TableRow(XmlConfigColumns.Flags.ToString(), "273", false),
               new TableRow(XmlConfigColumns.Component_.ToString(), "Component1"),
               new TableRow(XmlConfigColumns.Sequence.ToString(), "2", false));

            Verifier.VerifyTableData(msiFile, MSITables.XmlConfig,
               new TableRow(XmlConfigColumns.XmlConfig.ToString(), "NewNodeChildNewAttribute"),
               new TableRow(XmlConfigColumns.File.ToString(), "[#TestXmlConfig1]"),
               new TableRow(XmlConfigColumns.ElementPath.ToString(), "NewNodeChild"),
               new TableRow(XmlConfigColumns.VerifyPath.ToString(), string.Empty),
               new TableRow(XmlConfigColumns.Name.ToString(), "name"),
               new TableRow(XmlConfigColumns.Value.ToString(), "Junior"),
               new TableRow(XmlConfigColumns.Flags.ToString(), "0", false),
               new TableRow(XmlConfigColumns.Component_.ToString(), "Component1"),
               new TableRow(XmlConfigColumns.Sequence.ToString(), string.Empty, false));

            Verifier.VerifyTableData(msiFile, MSITables.XmlConfig,
             new TableRow(XmlConfigColumns.XmlConfig.ToString(), "NewNodeNewAttribute"),
             new TableRow(XmlConfigColumns.File.ToString(), "[#TestXmlConfig1]"),
             new TableRow(XmlConfigColumns.ElementPath.ToString(), "NewNodeInst"),
             new TableRow(XmlConfigColumns.VerifyPath.ToString(), string.Empty),
             new TableRow(XmlConfigColumns.Name.ToString(), "key"),
             new TableRow(XmlConfigColumns.Value.ToString(), "foo"),
             new TableRow(XmlConfigColumns.Flags.ToString(), "0", false),
             new TableRow(XmlConfigColumns.Component_.ToString(), "Component1"),
             new TableRow(XmlConfigColumns.Sequence.ToString(), string.Empty, false));

            Verifier.VerifyTableData(msiFile, MSITables.XmlConfig,
              new TableRow(XmlConfigColumns.XmlConfig.ToString(), "NewNodeInst"),
              new TableRow(XmlConfigColumns.File.ToString(), "[#TestXmlConfig1]"),
              new TableRow(XmlConfigColumns.ElementPath.ToString(), "/Root"),
              new TableRow(XmlConfigColumns.VerifyPath.ToString(), "/Root/Child[\\[]@key=\"foo\"[\\]]"),
              new TableRow(XmlConfigColumns.Name.ToString(), "Child"),
              new TableRow(XmlConfigColumns.Value.ToString(), "this is text"),
              new TableRow(XmlConfigColumns.Flags.ToString(), "273", false),
              new TableRow(XmlConfigColumns.Component_.ToString(), "Component1"),
              new TableRow(XmlConfigColumns.Sequence.ToString(), "1", false));

            Verifier.VerifyTableData(msiFile, MSITables.XmlConfig,
              new TableRow(XmlConfigColumns.XmlConfig.ToString(), "NewNodeUninst"),
              new TableRow(XmlConfigColumns.File.ToString(), "[#TestXmlConfig1]"),
              new TableRow(XmlConfigColumns.ElementPath.ToString(), "/Root"),
              new TableRow(XmlConfigColumns.VerifyPath.ToString(), "/Root/Child[\\[]@key=\"foo\"[\\]]"),
              new TableRow(XmlConfigColumns.Name.ToString(), string.Empty),
              new TableRow(XmlConfigColumns.Value.ToString(), string.Empty),
              new TableRow(XmlConfigColumns.Flags.ToString(), "545", false),
              new TableRow(XmlConfigColumns.Component_.ToString(), "Component1"),
              new TableRow(XmlConfigColumns.Sequence.ToString(), "1", false));
        }

        [NamedFact]
        [Description("Verify that the correct transformations are done to the XML file on install and uninstall.")]
        [Priority(2)]
        [RuntimeTest]
        public void XmlConfig_Install()
        {
            string sourceFile = Path.Combine(XmlConfigTests.TestDataDirectory, @"product.wxs");
            string targetFile = Utilities.FileUtilities.GetUniqueFileName();

            // Create new test.xml file to be used.
            File.Copy(Path.Combine(XmlConfigTests.TestDataDirectory, @"test.xml"), targetFile, true);
            FileInfo file = new FileInfo(targetFile);
            if (file.IsReadOnly)
            {
                file.IsReadOnly = false;
            }

            // build the msi and pass targetfile as a param to the .wxs file
            string msiFile = Builder.BuildPackage(Environment.CurrentDirectory, sourceFile, "test.msi", string.Format("-dTargetFile=\"{0}\" -ext WixUtilExtension", targetFile), "-ext WixUtilExtension -sice:ICE03");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify XML File Transformation.
            VerifyXMLConfigInstallTransformation(targetFile);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the file was removed
            Assert.True(File.Exists(targetFile), String.Format("XML file '{0}' was removed on Uninstall.", targetFile));

            // Verify XML File Transformation.
            VerifyXMLConfigUninstallTransformation(targetFile);
        }

        [NamedFact]
        [Description("Verify that the correct transformations are done to the XML file on install and uninstall.")]
        [Priority(2)]
        [RuntimeTest]
        [Is64BitSpecificTest]
        public void XmlConfig_Install_64bit()
        {
            string sourceFile = Path.Combine(XmlConfigTests.TestDataDirectory, @"product_64.wxs");
            string targetFile = Path.Combine(Environment.ExpandEnvironmentVariables(@"%ProgramW6432%"), @"WixTestFolder\test.xml");

            // build the msi and pass targetfile as a param to the .wxs file
            string msiFile = Builder.BuildPackage(Environment.CurrentDirectory, sourceFile, "test.msi", string.Format("-dTargetFile=\"{0}\" -ext WixUtilExtension", targetFile), "-ext WixUtilExtension -sice:ICE03");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify XML File Transformation.
            VerifyXMLConfigInstallTransformation(targetFile);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the file was removed
            Assert.False(File.Exists(targetFile), String.Format("XML file '{0}' was removed on Uninstall.", targetFile));
        }

        [NamedFact]
        [Description("Verify that the correct transformations are done to the XML file on Rollback.")]
        [Priority(2)]
        [RuntimeTest]
        public void XmlConfig_InstallFailure()
        {
            string sourceFile = Path.Combine(XmlConfigTests.TestDataDirectory, @"product_fail.wxs");
            string targetFile = Utilities.FileUtilities.GetUniqueFileName();

            // Create new test.xml file to be used.
            File.Copy(Path.Combine(XmlConfigTests.TestDataDirectory, @"test.xml"), targetFile, true);
            FileInfo file = new FileInfo(targetFile);
            if (file.IsReadOnly)
            {
                file.IsReadOnly = false;
            }

            // build the msi and pass targetfile as a param to the .wxs file
            string msiFile = Builder.BuildPackage(Environment.CurrentDirectory, sourceFile, "test.msi", string.Format("-dTargetFile=\"{0}\" -ext WixUtilExtension", targetFile), "-ext WixUtilExtension -sice:ICE03");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify that the file was removed
            Assert.True(File.Exists(targetFile), string.Format("XML file '{0}' was removed on Rollback.", targetFile));

            // Verify XML File has not changed.
            XMLVerifier.VerifyXMLFile(targetFile,Path.Combine(XmlConfigTests.TestDataDirectory, @"test.xml"));
        }

        #region Helper Methods
        /// <summary>
        /// Verify XML File Transformation on Install.
        /// </summary>
        /// <param name="fileName">XML file path</param>
        private static void VerifyXMLConfigInstallTransformation(string fileName)
        {
            // /Root
            // Verify new attribute was added to root node (New=�hello�)
            XMLVerifier.VerifyAttributeValue(fileName, "/Root", "New", "hello");

            // /Root/Child
            // Verify �Child� node was created
            XMLVerifier.VerifyElementExists(fileName, "/Root/Child[@key='foo']");

            // Verify �Child� node has the right text
            XMLVerifier.VerifyElementValue(fileName, "/Root/Child[@key='foo']", "this is text");

            // /Root/Child/GrandChild
            // Verify �GrandChild� has an attribute (�name=Junior�)
            XMLVerifier.VerifyAttributeValue(fileName, "/Root/Child/GrandChild", "name", "Junior");

            // Verify �GrandChild� has the right text
            XMLVerifier.VerifyElementValue(fileName, "/Root/Child/GrandChild", "hi mom");

            // /Root/Fragment/Child
            // Verify Fragment/Child element has a new attribute added (Id=�XmlConfig�)
            XMLVerifier.VerifyAttributeValue(fileName, "/Root/Fragment/Child", "Id", "XmlConfig");

            // Verify Fragment/Child element has no inner text
            XMLVerifier.VerifyElementValue(fileName, "/Root/Fragment/Child", string.Empty);
        }

        /// <summary>
        /// Verify XML File Transformation on Uninstall.
        /// </summary>
        /// <param name="fileName">XML file path</param>
        private static void VerifyXMLConfigUninstallTransformation(string fileName)
        {
            // Verify XML File Transformation on Uninstall.
            // /Root
            // Verify new attribute 'New' was removed from Root
            XMLVerifier.VerifyElementExists(fileName, "/Root[New='hello']", false);

            // /Root/Child
            // Verify �Child� node was deleted
            XMLVerifier.VerifyElementExists(fileName, "/Root/Child[@key='foo']", false);

            // /Root/Child/GrandChild
            // Verify �GrandChild� was deleted
            XMLVerifier.VerifyElementExists(fileName, "/Root/Child/GrandChild", false);

            // /Root/Fragment/Child
            // Verify Fragment/Child element still has a new attribute added (Id=�XmlConfig�)
            XMLVerifier.VerifyAttributeValue(fileName, "/Root/Fragment/Child", "Id", "XmlConfig");

            // Verify Fragment/Child element still has no inner text
            XMLVerifier.VerifyElementValue(fileName, "/Root/Fragment/Child", string.Empty);
        }
        #endregion
    }
}
