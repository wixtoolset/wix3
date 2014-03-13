//-----------------------------------------------------------------------
// <copyright file="InstanceTransforms.InstanceTransformTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for the building an MSI with an Instance Transform
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.InstanceTransforms
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;
    using Microsoft.Deployment.WindowsInstaller;
    using Microsoft.Deployment.WindowsInstaller.Package;

    /// <summary>
    /// Tests for the building an MSI with an Instance Transform
    /// </summary>
    [TestClass]
    public class InstanceTransformTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\InstanceTransforms\InstanceTransformTests");

        [TestMethod]
        [Description("Verify that Product/@Id element can use a '*' for its GUID")]
        [Priority(1)]
        public void AutoGenProductId()
        {
            string sourceFile = Path.Combine(InstanceTransformTests.TestDataDirectory, @"AutoGenProductId\product.wxs");
            string msi = Builder.BuildPackage(sourceFile);

            // Verify that an instance transforms was created
            string transformName = "Instance1.mst";
            string mst = Path.Combine(Path.GetDirectoryName(msi), transformName);

            // Extract the transform
            InstanceTransformTests.ExtractTransform(msi, transformName, mst);

            // Verify that the transform matches the expected transform
            string expectedTransform = Path.Combine(InstanceTransformTests.TestDataDirectory, @"AutoGenProductId\expected.mst");
            Verifier.VerifyResults(expectedTransform, mst);
        }


        [TestMethod]
        [Description("Verify that there is an error when two instances have the same Id")]
        [Priority(2)]
        public void DuplicateInstanceIds()
        {
            string sourceFile = Path.Combine(InstanceTransformTests.TestDataDirectory, @"DuplicateInstanceIds\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(91, "Duplicate symbol 'WixInstanceTransforms:Instance1' found.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedWixMessages.Add(new WixMessage(92, "Location of symbol related to previous error.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 92;
            light.Run();
        }

        [TestMethod]
        [Description("Verify that Instance/@ProductCode element can use a '*' for its GUID")]
        [Priority(3)]
        public void AutoGenInstanceProductCode()
        {
            string sourceFile = Path.Combine(InstanceTransformTests.TestDataDirectory, @"AutoGenInstanceProductCode\product.wxs");
            string msi = Builder.BuildPackage(sourceFile);

            // Verify that an instance transforms was created
            string transformName = "Instance1.mst";
            string mst = Path.Combine(Path.GetDirectoryName(msi), transformName);

            // Extract the transform
            InstanceTransformTests.ExtractTransform(msi, transformName, mst);

            // Verify that the base product code is the expected value
            Verifier.VerifyQuery(msi, "Select `Value` FROM `Property` WHERE `Property` = 'ProductCode'", "{4014E041-A968-4DE3-B43C-322DF9A19359}");

            // Verify that the transform changes the product code
            using (Database msiDatabase = new Database(msi, DatabaseOpenMode.ReadOnly))
            {
                msiDatabase.ApplyTransform(mst);
                string transformProductCode = null;
                using (View view = msiDatabase.OpenView("Select `Value` FROM `Property` WHERE `Property` = 'ProductCode'"))
                {
                    view.Execute();
                    Record record = view.Fetch();

                    if (null != record)
                    {
                        transformProductCode = Convert.ToString(record.GetString(1));
                    }
                }

                Assert.AreNotEqual("{4014E041-A968-4DE3-B43C-322DF9A19359}", transformProductCode, "The product code was not transformed by the instance transform.");
            }
        }

        //{E8441024-BBDA-4D08-B8B1-039C269CD374}
        [TestMethod]
        [Description("Verify that Instance/@UpgradeCode element can supply a GUID and modify the base Product/@UpgradeCode")]
        [Priority(4)]
        public void InstanceUpgradeCode()
        {
            string sourceFile = Path.Combine(InstanceTransformTests.TestDataDirectory, @"InstanceUpgradeCode\product.wxs");
            string msi = Builder.BuildPackage(sourceFile);

            // Verify that an instance transforms was created
            string transformName = "Instance1.mst";
            string mst = Path.Combine(Path.GetDirectoryName(msi), transformName);

            // Extract the transform
            InstanceTransformTests.ExtractTransform(msi, transformName, mst);

            // Verify that the base upgrade code is the expected value
            Verifier.VerifyQuery(msi, "Select `Value` FROM `Property` WHERE `Property` = 'UpgradeCode'", "{F907C172-70B8-4654-8D23-49FB3AE2ECB7}");

            // Verify that the transform changes the upgrade code
            using (Database msiDatabase = new Database(msi, DatabaseOpenMode.ReadOnly))
            {
                msiDatabase.ApplyTransform(mst);
                string transformUpgradeCode = null;
                using (View view = msiDatabase.OpenView("Select `Value` FROM `Property` WHERE `Property` = 'UpgradeCode'"))
                {
                    view.Execute();
                    using (Record record = view.Fetch())
                    {
                        if (null != record)
                        {
                            transformUpgradeCode = Convert.ToString(record.GetString(1));
                        }
                    }
                }

                Assert.AreEqual("{E8441024-BBDA-4D08-B8B1-039C269CD374}", transformUpgradeCode, "The upgrade code was not transformed by the instance transform.");
            }
        }
        /// <summary>
        /// Extracts a transform from an MSI
        /// </summary>
        /// <param name="msi">The MSI to extract from</param>
        /// <param name="transform">The name of the transform to extract</param>
        /// <param name="extractFile">The location to extract to</param>
        /// <remarks>
        /// Most of this code was copied from DTF's Microsoft.Deployment.WindowsInstaller.Package.PatchPackage class.
        /// </remarks>
        private static void ExtractTransform(string msi, string transform, string extractFile)
        {
            Database msiDatabase = new Database(msi, DatabaseOpenMode.ReadOnly);

            using (View view = msiDatabase.OpenView("SELECT `Name`, `Data` FROM `_Storages` WHERE `Name` = '{0}'", transform))
            {
                view.Execute();
                Record record = view.Fetch();
                if (record == null)
                {
                    Assert.Fail("Transform {0} not found in {1}", transform, msi);
                }
                using (record)
                {
                    Console.WriteLine("Extracting transform {0} to {1}", transform, extractFile);
                    record.GetStream("Data", extractFile);
                }
            }
        }
    }
}
