//-----------------------------------------------------------------------
// <copyright file="UtilExtension.XmlFileTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Util Extension XmlFile tests</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Extensions.UtilExtension
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using WixTest.Verifiers;
    using WixTest.Verifiers.Extensions;
   
    /// <summary>
    /// Util extension XmlFile element tests
    /// </summary>
    [TestClass]
    public class XmlFileTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Extensions\UtilExtension\XmlFileTests");

        [TestMethod]
        [Description("Verify that the (XmlFile and CustomAction) Tables are created in the MSI and have expected data.")]
        [Priority(1)]
        public void XmlFile_VerifyMSITableData()
        {
            string sourceFile = Path.Combine(XmlFileTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            Verifier.VerifyCustomActionTableData(msiFile,
               new CustomActionTableData("SchedXmlFile", 1, "WixCA", "SchedXmlFile"),
                new CustomActionTableData("ExecXmlFile", 3073, "WixCA", "ExecXmlFile"),
                new CustomActionTableData("ExecXmlFileRollback", 3329, "WixCA", "ExecXmlFileRollback")
                );

            // Verify XmlFile table contains the right data
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "ChildAttribute1"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Child"),
                new TableRow(XmlFileColumns.Name.ToString(), string.Empty),
                new TableRow(XmlFileColumns.Value.ToString(), "again"),
                new TableRow(XmlFileColumns.Flags.ToString(), "0", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), string.Empty, false)
                );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "ChildAttribute3"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Child"),
                new TableRow(XmlFileColumns.Name.ToString(), "Long"),
                new TableRow(XmlFileColumns.Value.ToString(), "[LongValue]"),
                new TableRow(XmlFileColumns.Flags.ToString(), "0", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), string.Empty, false)
                );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "ChildAttributes"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Child/NewElement/NewElementChild/test1"),
                new TableRow(XmlFileColumns.Name.ToString(), string.Empty),
                new TableRow(XmlFileColumns.Value.ToString(), "Allarethesame"),
                new TableRow(XmlFileColumns.Flags.ToString(), "4", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), "5", false)
                );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "ChildSecondAttribute1"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Child[\\[]1[\\]]"),
                new TableRow(XmlFileColumns.Name.ToString(), "foo"),
                new TableRow(XmlFileColumns.Value.ToString(), "bar"),
                new TableRow(XmlFileColumns.Flags.ToString(), "0", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), string.Empty, false)
              );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "Delete"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Config[\\[]@key=\"abc\"[\\]]"),
                new TableRow(XmlFileColumns.Name.ToString(), string.Empty),
                new TableRow(XmlFileColumns.Value.ToString(), "CN=Something Else"),
                new TableRow(XmlFileColumns.Flags.ToString(), "2", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), string.Empty, false)
               );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "NewAttribute1"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root"),
                new TableRow(XmlFileColumns.Name.ToString(), "New"),
                new TableRow(XmlFileColumns.Value.ToString(), "hello"),
                new TableRow(XmlFileColumns.Flags.ToString(), "65536", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), string.Empty, false)
              );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "NewElement"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Child"),
                new TableRow(XmlFileColumns.Name.ToString(), "NewElement"),
                new TableRow(XmlFileColumns.Value.ToString(), "new element text"),
                new TableRow(XmlFileColumns.Flags.ToString(), "1", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), "1", false)
              );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "NewElementAttrib"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Child/NewElement"),
                new TableRow(XmlFileColumns.Name.ToString(), "EmptyAttr"),
                new TableRow(XmlFileColumns.Value.ToString(), string.Empty),
                new TableRow(XmlFileColumns.Flags.ToString(), "0", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), "3", false)
               );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "NewElementChild"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Child/NewElement"),
                new TableRow(XmlFileColumns.Name.ToString(), "NewElementChild"),
                new TableRow(XmlFileColumns.Value.ToString(), string.Empty),
                new TableRow(XmlFileColumns.Flags.ToString(), "1", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), "2", false)
               );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "SpecificAdd"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root/Config[\\[]@key=\"ghi\"[\\]]"),
                new TableRow(XmlFileColumns.Name.ToString(), "value"),
                new TableRow(XmlFileColumns.Value.ToString(), "CN=Something Else"),
                new TableRow(XmlFileColumns.Flags.ToString(), "0", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), string.Empty, false)
              );
            Verifier.VerifyTableData(msiFile, MSITables.XmlFile,
                new TableRow(XmlFileColumns.XmlFile.ToString(), "Text1"),
                new TableRow(XmlFileColumns.File.ToString(), "[#TestXmlFile1]"),
                new TableRow(XmlFileColumns.ElementPath.ToString(), "/Root"),
                new TableRow(XmlFileColumns.Name.ToString(), string.Empty),
                new TableRow(XmlFileColumns.Value.ToString(), "this is text"),
                new TableRow(XmlFileColumns.Flags.ToString(), "0", false),
                new TableRow(XmlFileColumns.Component_.ToString(), "Component1"),
                new TableRow(XmlFileColumns.Sequence.ToString(), string.Empty, false)
            );
        }

        [TestMethod]
        [Description("Verify that the expected changes are made to the xml file on install.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void XmlFile_Install()
        {
            string sourceFile = Path.Combine(XmlFileTests.TestDataDirectory, @"product.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the File was created.
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"WixTestFolder\test.xml");
            Assert.IsTrue(File.Exists(fileName), "XMLFile '{0}' was not created on Install.", fileName);

            // Verify XML File Transformation.
            VerifyXMLFileTransformation(fileName);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the file was removed
            Assert.IsFalse(File.Exists(fileName), "XMLFile '{0}' was not removed on Uninstall.", fileName);
        }

        [TestMethod]
        [Description("Verify that the expected changes are made to the xml file on install to a 64-bit specific folder.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        [TestProperty("Is64BitSpecificTest", "true")]
        public void XmlFile_Install_64bit()
        {
            string sourceFile = Path.Combine(XmlFileTests.TestDataDirectory, @"product_64.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the File was created.
            string fileName = Path.Combine(Environment.ExpandEnvironmentVariables(@"%ProgramW6432%"), @"WixTestFolder\test.xml");
            Assert.IsTrue(File.Exists(fileName), "XMLFile '{0}' was not created on Install.", fileName);

            // Verify XML File Transformation.
            VerifyXMLFileTransformation(fileName);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the file was removed
            Assert.IsFalse(File.Exists(fileName), "XMLFile '{0}' was not removed on Uninstall.", fileName);
        }

        [TestMethod]
        [Description("Verify that the changes made to the xml file are reverted on uninstall.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void XmlFile_ExistingFile()
        {
            string sourceFile = Path.Combine(XmlFileTests.TestDataDirectory, @"product.wxs");
            string targetFile = Utilities.FileUtilities.GetUniqueFileName();

            // Create new test.xml file to be used.
            File.Copy(Path.Combine(XmlFileTests.TestDataDirectory, @"test.xml"), targetFile, true);
            FileInfo file = new FileInfo(targetFile);
            if (file.IsReadOnly)
            {
                file.IsReadOnly = false;
            }

            // build the msi and pass targetfile as a param to the .wxs file
            string msiFile = Builder.BuildPackage(Environment.CurrentDirectory, sourceFile, "test.msi", string.Format("-dTargetFile=\"{0}\" -ext WixUtilExtension", targetFile), "-ext WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify XML File Transformation.
            VerifyXMLFileTransformation(targetFile);

            MSIExec.UninstallProduct(msiFile, MSIExec.MSIExecReturnCode.SUCCESS);

            // Verify that the file was removed
            Assert.IsTrue(File.Exists(targetFile), "XMLFile '{0}' was removed on Uninstall.", targetFile);

            // Verify that the changes were reverted except for permenant changes
            XMLVerifier.VerifyXMLFile(targetFile, Path.Combine(XmlFileTests.TestDataDirectory, @"expected.xml"));
        }

        [TestMethod]
        [Description("Verify that the expected changes are made to the xml file on rollback.")]
        [Priority(2)]
        [TestProperty("IsRuntimeTest", "true")]
        public void XmlFile_InstallFailure()
        {
            string sourceFile = Path.Combine(XmlFileTests.TestDataDirectory, @"product_fail.wxs");
            string msiFile = Builder.BuildPackage(sourceFile, "test.msi", "WixUtilExtension");

            MSIExec.InstallProduct(msiFile, MSIExec.MSIExecReturnCode.ERROR_INSTALL_FAILURE);

            // Verify that the file was removed
            string fileName = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"WixTestFolder\test.xml");
            Assert.IsFalse(File.Exists(fileName), "XMLFile '{0}' was not removed on Rollback.", fileName);
        }

        #region Helper Methods
        /// <summary>
        /// verifies the transformation done using XMLFile in XMLFile.wix
        /// </summary>
        /// <param name="fileName">XML file Path</param>
        private static void VerifyXMLFileTransformation(string fileName)
        {
            // /Root
            // Verify new attribute was added to root node (New=”hello”)
            XMLVerifier.VerifyAttributeValue(fileName, "/Root", "New", "hello");

            // Verify new Text was added to the root element
            XMLVerifier.VerifyElementValue(fileName, "/Root", "this is text");

            // /Root/Child
            // Verify “Child” node has a childnode created ”NewElement”
            XMLVerifier.VerifyElementExists(fileName, "/Root/Child/NewElement");

            // /Root/Child/NewElement
            // Verify “NewElement” as an attribute (“EmptyAttr=”)
            XMLVerifier.VerifyAttributeValue(fileName, "/Root/Child/NewElement", "EmptyAttr", string.Empty);

            // Verify “NewElement” has text value “new element text”
            XMLVerifier.VerifyElementValue(fileName, "/Root/Child/NewElement", "new element text");

            // Verify “NewElement” node has a childnode created ”NewElementchild”
            XMLVerifier.VerifyElementExists(fileName, "/Root/Child/NewElement/NewElementChild");

            // /Root/Child/NewElement/NewElementChild
            // Verify all test1 node have the text value “Allarethesame”
            XMLVerifier.VerifyElementValue(fileName, "/Root/Child/NewElement/NewElementChild/test1[1]", "Allarethesame");
            XMLVerifier.VerifyElementValue(fileName, "/Root/Child/NewElement/NewElementChild/test1[2]", "Allarethesame");
            XMLVerifier.VerifyElementValue(fileName, "/Root/Child/NewElement/NewElementChild/test1[3]", "Allarethesame");

            // /Root/Child[2]
            // Verify second child element has a new attribute added (foo=”bar”)
            XMLVerifier.VerifyAttributeValue(fileName, "/Root/Child[2]", "foo", "bar");

            // /Root/Config
            // Verify the node(<Config key='abc'/>) text has been deleted
            XMLVerifier.VerifyElementValue(fileName, "/Root/Config[@key='abc']", string.Empty);

            // Verify the attribute value has been changed from (<Config key='ghi' value='CN=Users'/>) to (<Config key='ghi' value='CN=Something else'/>
            XMLVerifier.VerifyAttributeValue(fileName, "/Root/Config[@key='ghi']", "value", "CN=Something Else");
        }
        #endregion
    }
}
