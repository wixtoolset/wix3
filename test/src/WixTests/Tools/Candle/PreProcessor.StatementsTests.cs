// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Tools.Candle.PreProcessor
{
    using System;
    using System.IO;
    using System.Xml;
    using WixTest;

    /// <summary>
    /// Test how Candle handles preprocessing for statements.
    /// </summary>
    public class StatementTests : WixTests
    {
        private static readonly string TestDataDirectory = @"%WIX_ROOT%\test\data\Tools\Candle\PreProcessor\StatementTests";

        [NamedFact]
        [Description("Verify that Candle can preprocess an if statement.")]
        [Priority(1)]
        public void If()
        {
            string testFile = Path.Combine(StatementTests.TestDataDirectory, @"SharedData\Product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.PreProcessorParams.Add("MyVariable", "1");
            candle.Run();

            Verifier.VerifyWixObjProperty(candle.ExpectedOutputFiles[0], "MyProperty1", "foo");
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess elseif statement.")]
        [Priority(2)]
        public void ElseIf()
        {
            string testFile = Path.Combine(StatementTests.TestDataDirectory, @"SharedData\Product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.PreProcessorParams.Add("MyVariable", "2");
            candle.Run();

            Verifier.VerifyWixObjProperty(candle.ExpectedOutputFiles[0], "MyProperty2", "bar");
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess else statement.")]
        [Priority(2)]
        public void Else()
        {
            string testFile = Path.Combine(StatementTests.TestDataDirectory, @"SharedData\Product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.PreProcessorParams.Add("MyVariable", "3");
            candle.Run();

            Verifier.VerifyWixObjProperty(candle.ExpectedOutputFiles[0], "MyProperty3", "baz");
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess ifdef statement.")]
        [Priority(2)]
        public void IfDef()
        {
            string testFile = Path.Combine(StatementTests.TestDataDirectory, @"IfDef\Product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(testFile);
            candle.PreProcessorParams.Add("MyVariable", "10");
            candle.Run();

            Verifier.VerifyWixObjProperty(candle.ExpectedOutputFiles[0], "MyProperty", "foo");
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess ifndef statement.")]
        [Priority(2)]
        public void IfNDef()
        {
            string testFile = Path.Combine(StatementTests.TestDataDirectory, @"IfNDef\Product.wxs");

            string outputFile = Candle.Compile(testFile);

            Verifier.VerifyWixObjProperty(outputFile, "MyProperty", "bar");
        }

        [NamedFact]
        [Description("Verify that Candle can preprocess foreach statement.")]
        [Priority(2)]
        public void ForEach()
        {
            string testFile = Path.Combine(StatementTests.TestDataDirectory, @"ForEach\Product.wxs");

            string outputFile = Candle.Compile(testFile);

            for (int i = 1; i < 4; i++)
            {
                string expectedPropertyID = String.Concat("MyProperty", Convert.ToString(i));
                Verifier.VerifyWixObjProperty(outputFile, expectedPropertyID, Convert.ToString(i));
            }
        }
    }
}
