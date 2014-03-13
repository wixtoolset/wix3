//-----------------------------------------------------------------------
// <copyright file="Components.ComponentTests.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
//     Tests for Components
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
    /// Tests for Components
    /// </summary>
    [TestClass]
    public class ComponentTests : WixTests
    {
        private static readonly string TestDataDirectory = Environment.ExpandEnvironmentVariables(@"%WIX_ROOT%\test\data\Integration\BuildingPackages\Components\ComponentTests");

        [TestMethod]
        [Description("Verify that a simple Component can be defined and that the expected default values are set")]
        [Priority(1)]
        public void SimpleComponent()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"SimpleComponent\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            Verifier.VerifyResults(Path.Combine(ComponentTests.TestDataDirectory, @"SimpleComponent\expected.msi"), msi, "Component");
        }

        [TestMethod]
        [Description("Verify that Components/ComponentGroups can be referenced and that ComponentGroups can be nested")]
        [Priority(1)]
        public void ComponentRefsAndGroups()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"ComponentRefsAndGroups\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            Verifier.VerifyResults(Path.Combine(ComponentTests.TestDataDirectory, @"ComponentRefsAndGroups\expected.msi"), msi, "Component", "Directory", "FeatureComponents");
        }

        [TestMethod]
        [Description("Verify that a floating component can be defined. The component ties itself to a Directory and a Feature through its attributes.")]
        [Priority(1)]
        public void FloatingComponent()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"FloatingComponent\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");

            // Verify that Component1 was created and has the correct Directory
            string query = "SELECT `Directory_` FROM `Component` WHERE `Component`='Component1'";
            Verifier.VerifyQuery(msi, query, "WixTestFolder");
        }

        [TestMethod]
        [Description("Verify that there is an error if a floating component references an undefined directory")]
        [Priority(3)]
        public void InvalidFloatingComponent()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"InvalidFloatingComponent\product.wxs");

            string wixobj = Candle.Compile(sourceFile);

            Light light = new Light();
            light.ObjectFiles.Add(wixobj);
            light.ExpectedExitCode = 94;
            light.ExpectedWixMessages.Add(new WixMessage(94, "Unresolved reference to symbol 'Directory:UndefinedDirectory' in section 'Product:*'.", WixMessage.MessageTypeEnum.Error));
            light.Run();
        }

        [TestMethod]
        [Description("Verify that circular references are detected amongst ComponentRefs and ComponentGroupRefs")]
        [Priority(2)]
        public void CircularReferences()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"CircularReferences\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedExitCode = 86;
            light.ExpectedWixMessages.Add(new WixMessage(86, "A circular reference of groups was detected. The infinite loop includes: ComponentGroup:ComponentGroup1. Group references must form a directed acyclic graph.", WixMessage.MessageTypeEnum.Error));
            light.Run();
        }

        [TestMethod]
        [Description("Verify that there is an error for an invalid component Id")]
        [Priority(2)]
        public void InvalidId()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"InvalidId\product.wxs");
            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 14;
            candle.ExpectedWixMessages.Add(new WixMessage(14, "The Component/@Id attribute's value, '@#$', is not a legal identifier.  Identifiers may contain ASCII characters A-Z, a-z, digits, underscores (_), or periods (.).  Every identifier must begin with either a letter or an underscore.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that a component's GUID can be set to an empty string to make it an unmanaged component")]
        [Priority(2)]
        public void UnmanagedComponent()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"UnmanagedComponent\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            // Verify that Component1's guid is empty
            string query = "SELECT `ComponentId` FROM `Component` WHERE `Component`='Component1'";
            Verifier.VerifyQuery(msi, query, null);
        }

        [TestMethod]
        [Description("Verify that there is an error for a component without a GUID")]
        [Priority(2)]
        public void MissingComponentGuid()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"MissingComponentGuid\product.wxs");
            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 230;
            candle.ExpectedWixMessages.Add(new WixMessage(230, "The Component/@Guid attribute's value '*' is not valid for this component because it does not meet the criteria for having an automatically generated guid. Components with 0 files cannot use an automatically generated guid. Create multiple components, each with one file, to use automatically generated guids.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that a component's resources are tied to the component's DiskId")]
        [Priority(2)]
        public void DiskIdInheritance()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"DiskIdInheritance\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            // Verify that Component2's sequence is changed from 2 to 1 for diskid inherited from parant element.
            //the default value should be 2 without diskid inheriting.
            string query = "SELECT `Sequence` FROM `File` WHERE `Component_`='Component2'";
            Verifier.VerifyQuery(msi, query, "1");
        }

        [TestMethod]
        [Description("Verify that there is an error for an invalid component GUID")]
        [Priority(3)]
        public void InvalidComponentGuid()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"InvalidComponentGuid\product.wxs");
            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 9;
            candle.ExpectedWixMessages.Add(new WixMessage(9, "The Component/@Guid attribute's value, '#$%', is not a legal guid value.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that a component's resources are tied to the component's DiskId unless DiskId is explicitly set on a resource")]
        [Priority(2)]
        public void DiskIdInheritanceOverride()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"DiskIdInheritance\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            // Verify that Component2's sequence is changed from 2 to 1 for diskid inherited from parant element.
            //the default value should be 2 without diskid inheriting.
            string query = "SELECT `Sequence` FROM `File` WHERE `Component_`='Component2'";
            Verifier.VerifyQuery(msi, query, "1");
        }

        [TestMethod]
        [Description("Verify that a component's directory can be set as the keypath")]
        [Priority(2)]
        public void ComponentKeyPath()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"ComponentKeyPath\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "0");
        }

        [TestMethod]
        [Description("Verify that a component can be shared")]
        [Priority(2)]
        public void Shared()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"ComponentShared\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "2048");
        }

        [TestMethod]
        [Description("Verify that a component can be marked as 64 bit")]
        [Priority(2)]
        public void Win64()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"Win64Component\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "256");
        }

        [TestMethod]
        [Description("Verify that the Win64 attribute overrides the command line -platforms/arch switch. All scenarios should be verified.")]
        [Priority(2)]
        public void Win64Override()
        {
            string testDirectory = Path.Combine(ComponentTests.TestDataDirectory, "Win64Component");
            //try to created a 32bit component by -arch x86, actually it will generate a 64bit component because win64="yes" is set in wxs file
            string msi_32bit = Builder.BuildPackage(testDirectory, "product.wxs", "product_32.msi", " -dIsWin64=no  -arch x86", "");
            Verifier.VerifyResults(Path.Combine(ComponentTests.TestDataDirectory, @"Win64Component\expected.msi"), msi_32bit, "Component");
        }

        [TestMethod]
        [Description("Verify that generated GUIDs for components take into account the bitness (32-bit vs 64-bit)")]
        [Priority(2)]
        [TestProperty("Bug Link", "http://sourceforge.net/tracker/index.php?func=detail&aid=1833513&group_id=105970&atid=642714")]
        public void Win64ComponentGeneratedGUID()
        {
            string testDirectory = Path.Combine(ComponentTests.TestDataDirectory, "Win64ComponentGeneratedGUID");
            string msi_32bit = Builder.BuildPackage(testDirectory, "product.wxs", "product_32.msi", " -dIsWin64=no", "");
            string msi_64bit = Builder.BuildPackage(testDirectory, "product.wxs", "product_64.msi", " -dIsWin64=yes  -arch x64", "");

            // get the component GUIDs from the resulting msi's
            string query = "SELECT `ComponentId` FROM `Component` WHERE `Component` = 'Component1'";
            string component_32bit_GUID = Verifier.Query(msi_32bit, query);
            string component_64bit_GUID = Verifier.Query(msi_64bit, query);
            Assert.AreNotEqual(component_32bit_GUID, component_64bit_GUID);
        }

        [TestMethod]
        [Description("Verify that there is an error if the component GUID is set to PUT-GUID-HERE")]
        [Priority(3)]
        public void PutGuidHere()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"PutGuidHere\product.wxs");
            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 40;
            candle.ExpectedWixMessages.Add(new WixMessage(40, "The Component/@Guid attribute's value, 'PUT-GUID-HERE', is not a legal Guid value.  A Guid needs to be generated and put in place of 'PUT-GUID-HERE' in the source file.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that a floating component's directory can be set as the keypath")]
        [Priority(3)]
        public void FloatingComponentKeyPath()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"FloatingComponentKeyPath\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Component` FROM `Component` WHERE `Component` = 'Component1'";
            Verifier.VerifyQuery(msi, query, "Component1");
        }

        [TestMethod]
        [Description("Verify that there is an error if a component is tied to an undefined feature")]
        [Priority(3)]
        public void InvalidComponentFeature()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"InvalidComponentFeature\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedExitCode = 204;
            light.ExpectedWixMessages.Add(new WixMessage(204, "ICE03: Not a valid foreign key; Table: FeatureComponents, Column: Feature_, Key(s): Feature2.test", WixMessage.MessageTypeEnum.Error));
            light.Run();
        }

        [TestMethod]
        [Description("Verify that a component can be tied to a feature by using the Feature attribute and tied to another feature through a ComponentRef")]
        [Priority(3)]
        public void ComponentFeatureAndReferenced()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"ComponentFeatureAndReferenced\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query1 = "SELECT `Component_` FROM `FeatureComponents` WHERE `Feature_` = 'Feature1'";
            string query2 = "SELECT `Component_` FROM `FeatureComponents` WHERE `Feature_` = 'Feature2'";
            Verifier.VerifyQuery(msi, query1, "test");
            Verifier.VerifyQuery(msi, query2, "test");
        }

        [TestMethod]
        [Description("Verify that there is an error if the component is set as the keypath and it contains a resource that is set as a keypath")]
        [Priority(3)]
        public void TwoKeyPaths()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"TwoKeyPaths\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 42;
            candle.ExpectedWixMessages.Add(new WixMessage(42, "The Component element has multiple key paths set.  The key path may only be set to 'yes' in extension elements that support it or one of the following locations: Component/@KeyPath, File/@KeyPath, RegistryValue/@KeyPath, or ODBCDataSource/@KeyPath.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that registry reflection can be disabled")]
        [Priority(3)]
        public void DisableRegistryReflection()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"DisableRegistryReflection\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "512");
        }

        [TestMethod]
        [Description("Verify that the run location of a component can be set to local, source or either")]
        [Priority(3)]
        public void Location()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"Location\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            string query2 = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test2'";
            string query3 = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test3'";
            Verifier.VerifyQuery(msi, query, "0");
            Verifier.VerifyQuery(msi, query2, "1");
            Verifier.VerifyQuery(msi, query3, "2");
        }

        [TestMethod]
        [Description("Verify that there is an error if the component run location is not set to local, source or either")]
        [Priority(3)]
        public void InvalidLocation()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"InvalidLocation\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.ExpectedExitCode = 21;
            candle.ExpectedWixMessages.Add(new WixMessage(21, "The Component/@Location attribute's value, 'either', is not one of the legal options: 'local', or 'source'.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that a component can be set to never be overwritten")]
        [Priority(3)]
        public void NeverOverwrite()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"NeverOverwrite\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "128");
        }

        [TestMethod]
        [Description("Verify that a component can be set be permanent (never uninstalled)")]
        [Priority(3)]
        public void Permanent()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"Permanent\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "16");
        }

        [TestMethod]
        [Description("Verify that a component can be set be permanent and unmanaged (no GUID)")]
        [Priority(3)]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&aid=2987553&group_id=105970&atid=642714")]
        public void PermanentUnmanagedComponent()
        {
            //there is a error when set be permanent and unmanaged (no GUID)
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"PermanentUnmanagedComponent\product.wxs");

            Candle candle = new Candle();
            candle.SourceFiles.Add(sourceFile);
            candle.Run();

            Light light = new Light(candle);
            light.ExpectedExitCode = 204;
            light.ExpectedWixMessages.Add(new WixMessage(204, "ICE92: The Component 'test' has no ComponentId and is marked as permanent.", WixMessage.MessageTypeEnum.Error));
            light.Run();
        }

        [TestMethod]
        [Description("Verify that an unmanaged component cannot be marked as shared")]
        [Priority(3)]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&atid=642714&aid=2987094&group_id=105970")]
        public void UnmanagedSharedComponent()
        {
            Candle candle = new Candle();
            candle.SourceFiles.Add(Path.Combine(ComponentTests.TestDataDirectory, @"UnmanagedSharedComponent\product.wxs"));
            candle.ExpectedExitCode = 193;
            candle.ExpectedWixMessages.Add(new WixMessage(193, "The Component/@Shared attribute's value, 'yes', cannot be specified with attribute Guid present with value ''.", WixMessage.MessageTypeEnum.Error));
            candle.Run();
        }

        [TestMethod]
        [Description("Verify that the component's key file is marked to have its reference count incremented")]
        [Priority(3)]
        public void SharedDllRefCount()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"SharedDllRefCount\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "8");
        }
        
        [TestMethod]
        [Description("Verify that there is an error if the component's key file is not a DLL but the SharedDllRefCount attribute is set to 'yes'")]
        [Priority(3)]
        [TestProperty("Bug Link", "https://sourceforge.net/tracker/?func=detail&aid=2987594&group_id=105970&atid=642714")]
        [Ignore]
        public void InvalidSharedDllRefCount()
        {
            //no erroe come up
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"InvalidSharedDllRefCount\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "8");
        }

        [TestMethod]
        [Description("Verify that a component can be marked as Transitive")]
        [Priority(3)]
        public void Transitive()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"Transitive\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "64");
        }

        [TestMethod]
        [Description("Verify that a component can be marked to be uninstall when it is superseded")]
        [Priority(3)]
        public void UninstallWhenSuperseded()
        {
            string sourceFile = Path.Combine(ComponentTests.TestDataDirectory, @"UninstallWhenSuperseded\product.wxs");
            string msi = Builder.BuildPackage(sourceFile, "test.msi");
            string query = "SELECT `Attributes` FROM `Component` WHERE `Component` = 'test'";
            Verifier.VerifyQuery(msi, query, "1024");
        }
    }
}