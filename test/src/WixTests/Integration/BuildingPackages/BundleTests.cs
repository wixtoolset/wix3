//-----------------------------------------------------------------------
// <copyright file="BundleTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Base class for Bundle Tests
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Bundle
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Xml;
    using WixTest;
    using WixTest.Verifiers;
    using Xunit;

    /// <summary>
    /// Base class for Bundle Tests
    /// </summary>
    public class BundleTests : WixTests
    {
        protected static readonly string BundleSharedFilesDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Bundle\Files");
        protected static readonly string MsiPackageFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Packages\MsiPackage.msi");
        protected static readonly string MsiPackageProductCode = "{738D02BF-E231-4370-8209-E9FD4E1BE2A3}";
        protected static readonly string MspPackageFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Packages\MspPackage.msp");
        protected static readonly string MspPackagePatchCode = "{2DFFC5F8-9B0F-4510-92AE-FA3D38B8A47D}";
        protected static readonly string MsuPackageFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Packages\MsuPackage.msu");
        protected static readonly string ExePackageFile = Path.Combine(BundleTests.BundleSharedFilesDirectory, @"Packages\ExePackage.exe");

        // End-to-End tests


        #region Verification Methods

        /// <summary>
        /// Query Burn_Manifest.xml file.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are</param>
        /// <param name="xpathQuery">XPath Query</param>
        /// <returns>List of XmlNodes that match the query</returns>
        /// <remarks>The namespace that should be used is 'burn'</remarks>
        protected static XmlNodeList QueryBurnManifest(string embededResourcesDirectoryPath, string xpathQuery)
        {
            string burnManifestFilePath = Path.Combine(embededResourcesDirectoryPath, Builder.BurnManifestFileName);

            // verify that file dose exist
            Assert.True(File.Exists(burnManifestFilePath), string.Format("Burn manifest file was not created at '{0}'.", burnManifestFilePath));

            return Verifier.QueryBurnManifest(burnManifestFilePath, xpathQuery);
        }

        /// <summary>
        /// Query Burn-UxManifest.xml file.
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are</param>
        /// <param name="xpathQuery">XPath Query</param>
        /// <returns>List of XmlNodes that match the query</returns>
        /// <remarks>The namespace that should be used is 'burnUx'</remarks>
        protected static XmlNodeList QueryBurnUxManifest(string embededResourcesDirectoryPath, string xpathQuery)
        {
            string burnUxManifestFilePath = Path.Combine(embededResourcesDirectoryPath, Builder.UXManifestFileName);

            // verify that file dose exist
            Assert.True(File.Exists(burnUxManifestFilePath), string.Format("Burn Ux manifest file was not created at '{0}'.", burnUxManifestFilePath));

            return Verifier.QueryBurnUxManifest(burnUxManifestFilePath, xpathQuery);
        }

        /// <summary>
        /// Verifies the value of an attribute on an XML node.
        /// </summary>
        /// <param name="node">XML node to validate.</param>
        /// <param name="attributeName">Attribute name.</param>
        /// <param name="expectedValue">Expected attribute value.</param>
        protected static void VerifyAttributeValue(XmlNode node, string attributeName, string expectedValue)
        {
            if (null == node)
            {
                throw new ArgumentNullException("node", "node cannot be null.");
            }

            if (null != expectedValue)
            {
                Assert.True(expectedValue == node.Attributes[attributeName].Value,
                    string.Format("{0} @{1} value does not match expected. Actual: '{2}'. Expected:'{3}'.", node.Name, attributeName, node.Attributes[attributeName].Value, expectedValue));
            }
            else
            {
                Assert.True(node.Attributes[attributeName] == null, string.Format("{0} @{1} was defined it was not expected.", node.Name, attributeName));
            }
        }

        /// <summary>
        /// Verify the order of elements in BurnManifest.XML
        /// </summary>
        /// <param name="embededResourcesDirectoryPath">Output folder where all the embeded resources are</param>
        /// <param name="elementName">Elements to look for.</param>
        /// <param name="idAttributeName">The attribute to use as an Id for the elements</param>
        /// <param name="expectedOrderedValueList">Expected ordered list of the values of the id attribute.</param>
        public static void VerifyBurnManifestElementOrder(string embededResourcesDirectoryPath, string elementName, string idAttributeName, string[] expectedOrderedValueList)
        {
            string burnManifestXPath = string.Format(@"//burn:{0}", elementName);
            XmlNodeList burnManifestNodes = BundleTests.QueryBurnManifest(embededResourcesDirectoryPath, burnManifestXPath);

            BundleTests.VerifyElementOrder(burnManifestNodes, idAttributeName, expectedOrderedValueList);
        }

        /// <summary>
        /// Verify the order of an element in a list of XMLNodes.
        /// </summary>
        /// <param name="nodeList">List of nodes to look into.</param>
        /// <param name="idAttributeName">The attribute to use as an Id for the elements</param>
        /// <param name="expectedOrderedValueList">Expected ordered list of the values of the id attribute.</param>
        public static void VerifyElementOrder(XmlNodeList nodeList, string idAttributeName, string[] expectedOrderedValueList)
        {
            if (null == nodeList)
            {
                throw new ArgumentNullException("nodeList", "nodeList cannot be null.");
            }

            if (nodeList.Count < expectedOrderedValueList.Length)
            {
                Assert.True(false, String.Format("Could not find enough elements to compare. Actual node list has '{0}'. Expected was '{1}'.", nodeList.Count, expectedOrderedValueList.Length));
            }

            int i = 0;
            int j = 0;
            string actualOrder = string.Empty;

            for (i = 0; i < nodeList.Count; i++)
            {
                if (null == nodeList[i].Attributes[idAttributeName])
                {
                    Assert.True(false, String.Format("Element '{0}' does not have attribute @'{1}.", nodeList[i].Name, idAttributeName));
                }

                actualOrder += string.Format("-> {0}", nodeList[i].Attributes[idAttributeName].Value);

                if (nodeList[i].Attributes[idAttributeName].Value != expectedOrderedValueList[j])
                {
                    continue;       // not the expected value, skip it
                }
                else
                {
                    j++;            // match. move on in the list of expected values

                    if (j == expectedOrderedValueList.Length)
                    {
                        return;     // done matching.. return
                    }
                }
            }

            string expectedOrder = string.Empty;
            foreach (string value in expectedOrderedValueList)
            {
                expectedOrder += string.Format("-> {0}", value);
            }

            // remaining unmatched values
            Assert.True(false, String.Format("'@{0}={1}' was not the found in the correct order. Expected Order: '{2}'. Actual Order: '{3}'.", idAttributeName, expectedOrderedValueList[j], expectedOrder, actualOrder));
        }

        #endregion
        
    }
}
