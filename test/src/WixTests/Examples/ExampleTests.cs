//-----------------------------------------------------------------------
// <copyright file="ExampleTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Example tests for WiX</summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Examples
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using Xunit;

    /// <summary>
    /// Example tests for WiX
    /// </summary>
    public class ExampleTests : WixTests
    {
        [NamedFact]
        [Description("An example test that verifies an MSI is built correctly")]
        [Priority(3)]
        public void ExampleTest1()
        {
            // Use the BuildPackage method to build an MSI from source
            string actualMSI = Builder.BuildPackage(Path.Combine(this.TestContext.DataDirectory, @"SharedData\Authoring\BasicProduct.wxs"));
            
            // The expected MSI to compare against
            string expectedMSI = Path.Combine(this.TestContext.DataDirectory, @"SharedData\Baselines\MSIs\BasicProduct.msi");

            // Use the VerifyResults method to compare the actual and expected MSIs
            Verifier.VerifyResults(expectedMSI, actualMSI);
        }

        [NamedFact]
        [Description("An example test that checks for a Light warning and queries the resulting MSI")]
        [Priority(3)]
        public void ExampleTest2()
        {
            // Compile a wxs file
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(this.TestContext.DataDirectory, @"Examples\ExampleTest2\product.wxs"));
            candle.Run();

            // Create a Light object that uses some properties of the Candle object
            Light light = new Light(candle);

            // Define the Light warning that we expect to see
            WixMessage LGHT1079 = new WixMessage(1079, WixMessage.MessageTypeEnum.Warning);
            light.ExpectedWixMessages.Add(LGHT1079);

            // Link
            light.Run();

            // Query the resulting MSI for verification
            string query = "SELECT `Value` FROM `Property` WHERE `Property` = 'Manufacturer'";
            Verifier.VerifyQuery(light.OutputFile, query, "Microsoft Corporation");
        }

        [NamedFact]
        [Description("An example test that verifies an ICE violation is caught by smoke")]
        [Priority(3)]
        public void ExampleTest3()
        {
            string testDirectory = Path.Combine(this.TestContext.DataDirectory, @"Examples\ExampleTest3");

            // Build the MSI that will be run against Smoke. Pass the -sval argument to delay validation until Smoke is run
            string msi = Builder.BuildPackage(testDirectory, "product.wxs", Path.Combine(this.TestContext.TestDirectory, "product.msi"), null, "-sval");

            // Create a new Smoke object
            Smoke smoke = new Smoke();
            smoke.DatabaseFiles.Add(msi);
            smoke.CubFiles.Add(Path.Combine(this.TestContext.DataDirectory, @"Examples\ExampleTest3\test.cub"));

            // Define the expected ICE error
            WixMessage LGHT1076 = new WixMessage(1076, "ICE1000: Component 'ExtraICE.0.ProductComponent' installs into directory 'TARGETDIR', which will get installed into the volume with the most free space unless explicitly set.", WixMessage.MessageTypeEnum.Warning);
            smoke.ExpectedWixMessages.Add(LGHT1076);

            // Run Smoke and keep a reference to the Result object that is returned by the Run() method
            Result result = smoke.Run();

            // Use the Result object to verify the exit code
            // Note: checking for an exit code of 0 is done implicitly in the Run() method but
            // this is just for demonstration purposes.
            Assert.True(0 == result.ExitCode, "Actual exit code did not match expected exit code");
        }

        [NamedFact]
        [Description("An example of how to use QuickTest")]
        [Priority(3)]
        public void ExampleTest4()
        {
            QuickTest.BuildMsiTest(Path.Combine(this.TestContext.DataDirectory, @"SharedData\Authoring\BasicProduct.wxs"), Path.Combine(this.TestContext.DataDirectory, @"SharedData\Baselines\MSIs\BasicProduct.msi"));
        }
    }
}
