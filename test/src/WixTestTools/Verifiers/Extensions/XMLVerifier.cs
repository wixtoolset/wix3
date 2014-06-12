//-------------------------------------------------------------------------------------------------
// <copyright file="XMLVerifier.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//      Contains methods for verification for XML Extension
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixTest.Verifiers.Extensions
{
    using System;
    using System.IO;
    using System.Xml;
    using System.Collections;
    using System.Collections.Generic;
    using Xunit;

    /// <summary>
    /// Contains methods for XML verification
    /// </summary>
    public static class XMLVerifier
    {
        /// <summary>
        /// Verifies that a specific XPath query output has a given value.
        /// </summary>
        /// <param name="xmlFilePath">Path to XML.</param>
        /// <param name="xpathQuery">XPath Query.</param>
        /// <param name="expectedValue">Expected Value.</param>
        public static void VerifyElementValue(string xmlFilePath, string xpathQuery, string expectedValue)
        {
            XmlNodeList resultNode = Verifier.QueryXML(xmlFilePath, xpathQuery, new XmlNamespaceManager(new NameTable()));
            Assert.Equal(1, resultNode.Count);

            string actualValue = string.Empty;
            foreach (XmlNode childNode in resultNode[0].ChildNodes)
            {
                if (XmlNodeType.Text == childNode.NodeType)
                {
                    actualValue = childNode.Value;
                    break;
                }
            }
            Assert.True(expectedValue == actualValue, String.Format("Unexpected value for query '{0}'", xpathQuery));
        }

        /// <summary>
        /// Verifies that a specific XPath query output has an attribute with a given name and value.
        /// </summary>
        /// <param name="xmlFilePath">Path to XML.</param>
        /// <param name="xpathQuery">XPath Query.</param>
        /// <param name="attributeName">Attribute Name.</param>
        /// <param name="expectedValue">Expected Attribute Value.</param>
        public static void VerifyAttributeValue(string xmlFilePath, string xpathQuery, string attributeName, string expectedValue)
        {
            XmlNodeList resultNode = Verifier.QueryXML(xmlFilePath, xpathQuery, new XmlNamespaceManager(new NameTable()));
            Assert.Equal(1, resultNode.Count);

            foreach (XmlAttribute attribute in resultNode[0].Attributes)
            {
                if (attribute.Name == attributeName)
                {
                    string actualValue = attribute.Value;
                    Assert.True(expectedValue == actualValue, String.Format("Unexpected value for attribute '{0}'. Actual: '{1}', Expected: '{2}'.", attributeName, actualValue, expectedValue));
                    return;
                }
            }

            
            Assert.True(false, String.Format("Query '{0}' output does NOT have attribute '{1}'.", xpathQuery, attributeName));
        }

        /// <summary>
        /// Verifies that a specific XPath query output exisits.
        /// </summary>
        /// <param name="xmlFilePath">Path to XML.</param>
        /// <param name="xpathQuery">XPath Query.</param>
        public static void VerifyElementExists(string xmlFilePath, string xpathQuery)
        {
            VerifyElementExists(xmlFilePath, xpathQuery, true);
        }

        /// <summary>
        /// Verifies that a specific XPath query output exisits.
        /// </summary>
        /// <param name="xmlFilePath">Path to XML.</param>
        /// <param name="xpathQuery">XPath Query.</param>
        /// <param name="expected">Fail if output does NOT exist.</param>
        public static void VerifyElementExists(string xmlFilePath, string xpathQuery, bool expected)
        {
            XmlNodeList resultNode = Verifier.QueryXML(xmlFilePath, xpathQuery, new XmlNamespaceManager(new NameTable()));
            if (true == expected)
            {
                Assert.Equal(1, resultNode.Count);
            }
            else
            {
                Assert.Equal(0, resultNode.Count);
            }
        }

        /// <summary>
        /// Compare two XML files and asserts they are identical
        /// </summary>
        /// <param name="acctualXMLFilePath">Acctual file path</param>
        /// <param name="expectedXMLFilePath">Expected fule path</param>
        public static void VerifyXMLFile(string acctualXMLFilePath, string expectedXMLFilePath)
        {
            string acctualFileText = File.ReadAllText(acctualXMLFilePath);
            string expectedFileText = File.ReadAllText(expectedXMLFilePath);

            Assert.True(acctualFileText.Equals(expectedFileText, StringComparison.InvariantCultureIgnoreCase), String.Format("XML Files '{0}' and '{1}' are not identical", acctualFileText, expectedFileText));
        }
    }
}
