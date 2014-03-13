//-----------------------------------------------------------------------
// <copyright file="Components.RemoveFolderTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for the RemoveFolder element
// </summary>
//-----------------------------------------------------------------------

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;

    using WixTest;

    /// <summary>
    /// Tests for the CreateFolder 
    /// </summary>
    [TestClass]
    public class RemoveFolderTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\RemoveFolderTests");

        [TestMethod]
        [Description("Verify that a simple use of the RemoveFolder element adds the correct entry to the RemoveFile table")]
        [Priority(1)]
        public void SimpleRemoveFolder()
        {
            QuickTest.BuildMsiTest(Path.Combine(RemoveFolderTests.TestDataDirectory, @"SimpleRemoveFolder\product.wxs"), Path.Combine(RemoveFolderTests.TestDataDirectory, @"SimpleRemoveFolder\expected.msi"));
        }

        [TestMethod]
        [Description("Verify that multiple duplicate RemoveFolder elements cannot exist as children of Component")]
        [Priority(1)]
        public void DuplicateRemoveFolders()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RemoveFolderTests.TestDataDirectory, @"DuplicateRemoveFolders\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(130, "The primary key 'RemoveFolder1' is duplicated in table 'RemoveFile'.  Please remove one of the entries or rename a part of the primary key to avoid the collision.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 130;
            light.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error if the Directory attribute is used with the Property attribute")]
        [Priority(1)]
        public void DirectoryAttributeWithPropertyAttribute()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(RemoveFolderTests.TestDataDirectory, @"DirectoryAttributeWithPropertyAttribute\product.wxs"));
            candle.ExpectedWixMessages.Add(new WixMessage(35, "The RemoveFolder/@Property attribute cannot be specified when attribute Directory is present with value 'WixTestFolder'.", WixMessage.MessageTypeEnum.Error));
            candle .ExpectedExitCode = 35;
            candle.Run();
        }
    }
}
