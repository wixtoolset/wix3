// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.UnitTests
{
    using System;
    using System.Collections;
    using System.IO;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Tools.WindowsInstallerXml;
    using System.Security.Cryptography;
    using System.Text;

    /// <summary>
    /// Unit tests for Preprocessor
    /// </summary>
    [TestClass]
    public class PreprocessorUnitTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\UnitTests\wix.dll\PreprocessorUnitTests\PreprocessAndComputeHash");
        string hash = null;

        [TestMethod]
        [Description("Unit test for the Process(String, Hashtable, out String) method")]
        [Priority(3)]
        public void PreprocessAndComputeHash()
        {
            Preprocessor preprocessor = new Preprocessor();

            // create a mock HT
            Hashtable vars = new Hashtable();
            vars.Add("var1", "val1");

            string sourceFile = Path.Combine(PreprocessorUnitTests.TestDataDirectory, "product.wxs");
            string expectedHash = "E835925A5EC286E24997E5D634702855E9077A37";

            preprocessor.ProcessedStream += new ProcessedStreamEventHandler(preprocessor_ProcessedStream);
            preprocessor.Process(sourceFile, vars);

            // verify that expected hash matches actual hash of preprocessed content
            Assert.AreEqual(expectedHash, this.hash, "Hashes for preprocessed file do not match");

            // now change a variable and compute hash again - should get different hash
            vars["var1"] = "val2";
            expectedHash = "C4A72E6D110F26AAE43EF6B8C468A44DED80A6BB";

            preprocessor.Process(sourceFile, vars);

            Assert.AreEqual(expectedHash, this.hash, "Hashes for preprocessed file do not match");
        }

        void preprocessor_ProcessedStream(object sender, ProcessedStreamEventArgs e)
        {
            ProcessedStreamEventArgs args = e as ProcessedStreamEventArgs;
            Stream processed = args.Processed;
            // save the old position of the stream
            long oldPosition = processed.Position;

            HashAlgorithm sha = new SHA1CryptoServiceProvider();
            processed.Position = 0;
            byte[] hashBytes = sha.ComputeHash(processed);

            StringBuilder sb = new StringBuilder();
            for (int i = 0, j = hashBytes.GetLength(0); i < j; ++i)
            {
                sb.AppendFormat("{0:X02}", hashBytes[i]);
            }
            this.hash = sb.ToString();
            // restore the old position
            processed.Position = oldPosition;
        }
    }
}
