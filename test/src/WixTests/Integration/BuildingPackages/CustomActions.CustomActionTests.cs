// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.CustomActions
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Text;
    using WixTest;
    using Xunit;

    /// <summary>
    /// Tests for custom actions
    /// </summary>
    public class CustomActionTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\CustomActions\CustomActionTests");

        [NamedFact]
        [Description("Verify that a custom action missing required attributes fails")]
        [Trait("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1983810&group_id=105970&atid=642714")]
        [Priority(2)]
        public void MissingRequiredAttributes()
        {
            // Run Candle
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(CustomActionTests.TestDataDirectory, @"MissingRequiredAttributes\product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(37, "The CustomAction/@ExeCommand attribute cannot be specified without attribute BinaryKey, Directory, FileKey, or Property present.", WixMessage.MessageTypeEnum.Error));
            candle.ExpectedExitCode = 37;
            candle.Run();
        }

        [NamedFact]
        [Description("Verify that a custom action can be created")]
        [Priority(1)]
        public void SimpleCustomAction()
        {
            string msi = Builder.BuildPackage(Path.Combine(CustomActionTests.TestDataDirectory, @"SimpleCustomAction\product.wxs"));

            Verifier.VerifyResults(Path.Combine(CustomActionTests.TestDataDirectory, @"SimpleCustomAction\expected.msi"), msi, "CustomAction", "InstallExecuteSequence");
        }
    }
}
