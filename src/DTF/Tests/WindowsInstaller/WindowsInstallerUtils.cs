//-------------------------------------------------------------------------------------------------
// <copyright file="WindowsInstallerUtils.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Deployment.Test
{
    using System;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;

    public class WindowsInstallerUtils
    {
        public static void InitializeProductDatabase(Database db)
        {
            InitializeProductDatabase(db, false);
        }

        public static void InitializeProductDatabase(Database db, bool sixtyFourBit)
        {
            db.SummaryInfo.CodePage = (short) Encoding.Default.CodePage;
            db.SummaryInfo.Title = "Windows Installer Test";
            db.SummaryInfo.Subject = db.SummaryInfo.Title;
            db.SummaryInfo.Author = typeof(WindowsInstallerUtils).Assembly.FullName;
            db.SummaryInfo.CreatingApp = db.SummaryInfo.Author;
            db.SummaryInfo.Comments = typeof(WindowsInstallerUtils).FullName + ".CreateBasicDatabase()";
            db.SummaryInfo.Keywords = "Installer,MSI,Database";
            db.SummaryInfo.PageCount = 300;
            db.SummaryInfo.WordCount = 0;
            db.SummaryInfo.RevisionNumber = Guid.NewGuid().ToString("B").ToUpper();
            db.SummaryInfo.Template = (sixtyFourBit ? "x64" : "Intel") + ";0";

            foreach (TableInfo tableInfo in Schema.Tables)
            {
                db.Execute(tableInfo.SqlCreateString);
            }

            db.Execute("INSERT INTO `Directory` (`Directory`, `DefaultDir`) VALUES ('TARGETDIR', 'SourceDir')");
            db.Execute("INSERT INTO `Directory` (`Directory`, `Directory_Parent`, `DefaultDir`) VALUES ('ProgramFilesFolder', 'TARGETDIR', '.')");

            foreach (Action action in Sequence.InstallExecute)
            {
                db.Execute("INSERT INTO `InstallExecuteSequence` (`Action`, `Sequence`) VALUES ('{0}', {1})",
                    action.Name, action.Sequence);
            }
        }

        public const string UpgradeCode = "{05955FE8-005F-4695-A81F-D559338065BB}";

        public static void CreateTestProduct(Database db)
        {
            Guid productGuid = Guid.NewGuid();

            string[] properties = new string[]
            {
                "ProductCode", productGuid.ToString("B").ToUpper(),
                "UpgradeCode", UpgradeCode,
                "ProductName", "Windows Installer Test Product " + productGuid.ToString("P").ToUpper(),
                "ProductVersion", "1.0.0.0000",
            };

            using (View view = db.OpenView("INSERT INTO `Property` (`Property`, `Value`) VALUES (?, ?)"))
            {
                using (Record rec = new Record(2))
                {
                    for (int i = 0; i < properties.Length; i += 2)
                    {
                        rec[1] = properties[i];
                        rec[2] = properties[i + 1];
                        view.Execute(rec);
                    }
                }
            }

            int randomId = new Random().Next(10000);
            string productDir = "TestDir" + randomId;
            db.Execute(
                "INSERT INTO `Directory` (`Directory`, `Directory_Parent`, `DefaultDir`) " +
                "VALUES ('TestDir', 'ProgramFilesFolder', 'TestDir|{0}:.')", productDir);

            string compId = Guid.NewGuid().ToString("B").ToUpper();
            db.Execute(
                "INSERT INTO `Component` " +
                    "(`Component`, `ComponentId`, `Directory_`, `Attributes`, `KeyPath`) " +
                    "VALUES ('{0}', '{1}', '{2}', {3}, '{4}')",
                "TestRegComp1",
                compId,
                "TestDir",
                (int) ComponentAttributes.RegistryKeyPath,
                "TestReg1");

            string productReg = "TestReg" + randomId;
            db.Execute(
                "INSERT INTO `Registry` (`Registry`, `Root`, `Key`, `Component_`) VALUES ('{0}', {1}, '{2}', '{3}')",
                "TestReg1",
                -1,
                @"Software\Microsoft\Windows Installer Test\" + productReg,
                "TestRegComp1");

            db.Execute(
                "INSERT INTO `Feature` (`Feature`, `Title`, `Level`, `Attributes`) VALUES ('{0}', '{1}', {2}, {3})",
                "TestFeature1",
                "Test Feature 1",
                1,
                (int) FeatureAttributes.None);

            db.Execute(
                "INSERT INTO `FeatureComponents` (`Feature_`, `Component_`) VALUES ('{0}', '{1}')",
                "TestFeature1",
                "TestRegComp1");
        }

        public static void AddFeature(Database db, string featureName)
        {
            db.Execute(
                "INSERT INTO `Feature` (`Feature`, `Title`, `Level`, `Attributes`) VALUES ('{0}', '{1}', {2}, {3})",
                featureName,
                featureName,
                1,
                (int) FeatureAttributes.None);
        }

        public static void AddRegistryComponent(Database db,
            string featureName, string compName, string compId,
            string keyName, string keyValueName, string value)
        {
            db.Execute(
                "INSERT INTO `Component` " +
                    "(`Component`, `ComponentId`, `Directory_`, `Attributes`, `KeyPath`) " +
                    "VALUES ('{0}', '{1}', '{2}', {3}, '{4}')",
                compName,
                compId,
                "TestDir",
                (int) ComponentAttributes.RegistryKeyPath,
                compName + "Reg1");
            db.Execute(
                "INSERT INTO `Registry` (`Registry`, `Root`, `Key`, `Name`, `Value`, `Component_`) VALUES ('{0}', {1}, '{2}', '{3}', '{4}', '{5}')",
                compName + "Reg1",
                -1,
                @"Software\Microsoft\Windows Installer Test\" + keyName,
                keyValueName,
                value,
                compName);
            db.Execute(
                "INSERT INTO `FeatureComponents` (`Feature_`, `Component_`) VALUES ('{0}', '{1}')",
                featureName,
                compName);
        }

        public static void AddFileComponent(Database db,
            string featureName, string compName, string compId,
            string fileKey, string fileName)
        {
            db.Execute(
                "INSERT INTO `Component` " +
                    "(`Component`, `ComponentId`, `Directory_`, `Attributes`, `KeyPath`) " +
                    "VALUES ('{0}', '{1}', '{2}', {3}, '{4}')",
                compName,
                compId,
                "TestDir",
                (int) ComponentAttributes.None,
                fileKey);
            db.Execute(
                "INSERT INTO `File` " +
                    "(`File`, `Component_`, `FileName`, `FileSize`, `Attributes`, `Sequence`) " +
                    "VALUES ('{0}', '{1}', '{2}', 1, 0, 1)",
                fileKey,
                compName,
                fileName);
            db.Execute(
                "INSERT INTO `FeatureComponents` (`Feature_`, `Component_`) VALUES ('{0}', '{1}')",
                featureName,
                compName);
        }
    }
}
