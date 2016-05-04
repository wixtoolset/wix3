// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Tests.Integration.BuildingPackages.Components
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using WixTest;

    /// <summary>
    /// Tests for the CreateFolder 
    /// </summary>
    public class CreateFolderTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\CreateFolderTests");

        [NamedFact]
        [Description("Verify that a simple use of the CreateFolder element adds the correct entries to the CreateFolder table")]
        [Priority(1)]
        public void SimpleCreateFolder()
        {
            QuickTest.BuildMsiTest(Path.Combine(CreateFolderTests.TestDataDirectory, @"SimpleCreateFolder\product.wxs"), Path.Combine(CreateFolderTests.TestDataDirectory, @"SimpleCreateFolder\expected.msi"));
        }

        [NamedFact]
        [Description("Verify that there is an error if two CreateFolder elements try to create the same folder")]
        [Priority(2)]
        public void CreateDuplicateFolders1()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateDupicatedFolder1\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(130, "The primary key 'WixTestFolder/Component1' is duplicated in table 'CreateFolder'.  Please remove one of the entries or rename a part of the primary key to avoid the collision.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 130;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that there is an error if an undefined Directory is referenced")]
        [Priority(2)]
        public void CreateUndefinedFolder()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateUndefinedFolder\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(94, "Unresolved reference to symbol 'Directory:UndefinedFolder' in section 'Product:*'.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 94;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that a folder can be created with specific permissions")]
        [Priority(2)]
        public void CreateFolderWithPermissions()
        {
            QuickTest.BuildMsiTest(Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateFolderWithPermissions\product.wxs"), Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateFolderWithPermissions\expected.msi"));
        }

        [NamedFact]
        [Description("Verify that there is an error if one CreateFolder uses its default parent folder and a second CreateFolder explicitly references its parent folder")]
        [Priority(3)]
        public void CreateDuplicateFolders2()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateDupicatedFolder2\product.wxs"));
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedWixMessages.Add(new WixMessage(130, "The primary key 'WixTestFolder/Component1' is duplicated in table 'CreateFolder'.  Please remove one of the entries or rename a part of the primary key to avoid the collision.", WixMessage.MessageTypeEnum.Error));
            light.ExpectedExitCode = 130;
            light.Run();
        }

        [NamedFact]
        [Description("Verify that a CreateFolder defaults correctly when it is in a floating component")]
        [Priority(3)]
        public void CreateFolderInFloatingComponent()
        {
            QuickTest.BuildMsiTest(Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateFolderInFloatingComponent\product.wxs"), Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateFolderInFloatingComponent\expected.msi"));
        }

        [NamedFact]
        [Description("Verify that a folder can be created with multiple overlapping permissions")]
        [Priority(3)]
        public void CreateFolderWithMultiplePermissions()
        {
            QuickTest.BuildMsiTest(Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateFolderWithMultiplePermissions\product.wxs"), Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateFolderWithMultiplePermissions\expected.msi"));
        }

        [NamedFact]
        [Description("Verify that shortcuts can be added to created folders")]
        [Priority(3)]
        public void CreateFolderShortcut()
        {
            QuickTest.BuildMsiTest(Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateFolderShortcut\product.wxs"), Path.Combine(CreateFolderTests.TestDataDirectory, @"CreateFolderShortcut\expected.msi"));
        }
    }
}
