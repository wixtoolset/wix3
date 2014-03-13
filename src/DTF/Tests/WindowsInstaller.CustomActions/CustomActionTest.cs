//-------------------------------------------------------------------------------------------------
// <copyright file="CustomActionTest.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Deployment.Test
{
    using System;
    using System.IO;
    using System.Text;
    using System.Collections.Generic;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using Microsoft.Deployment.WindowsInstaller;

    [TestClass]
    public class CustomActionTest
    {
        public CustomActionTest()
        {
        }

        [TestMethod]
        public void CustomActionTest1()
        {
            InstallLogModes logEverything =
                InstallLogModes.FatalExit |
                InstallLogModes.Error |
                InstallLogModes.Warning |
                InstallLogModes.User |
                InstallLogModes.Info |
                InstallLogModes.ResolveSource |
                InstallLogModes.OutOfDiskSpace |
                InstallLogModes.ActionStart |
                InstallLogModes.ActionData |
                InstallLogModes.CommonData |
                InstallLogModes.Progress |
                InstallLogModes.Initialize |
                InstallLogModes.Terminate |
                InstallLogModes.ShowDialog;

            Installer.SetInternalUI(InstallUIOptions.Silent);
            ExternalUIHandler prevHandler = Installer.SetExternalUI(
                WindowsInstallerTest.ExternalUILogger, logEverything);

            try
            {
                string[] customActions = new string[] { "SampleCA1", "SampleCA2" };
                #if DEBUG
                string caDir = @"..\..\..\..\..\build\debug\x86\";
                #else
                string caDir = @"..\..\..\..\..\build\ship\x86\";
                #endif
                caDir = Path.GetFullPath(caDir);
                string caFile = "Microsoft.Deployment.Samples.ManagedCA.dll";
                string caProduct = "CustomActionTest.msi";

                this.CreateCustomActionProduct(caProduct, caDir + caFile, customActions, false);

                Exception caughtEx = null;
                try
                {
                    Installer.InstallProduct(caProduct, String.Empty);
                }
                catch (Exception ex) { caughtEx = ex; }
                Assert.IsInstanceOfType(caughtEx, typeof(InstallCanceledException),
                    "Exception thrown while installing product: " + caughtEx);

                string arch = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITECTURE");
                string arch2 = Environment.GetEnvironmentVariable("PROCESSOR_ARCHITEW6432");
                if (arch == "AMD64" || arch2 == "AMD64")
                {
                    caDir = caDir.Replace("x86", "x64");

                    this.CreateCustomActionProduct(caProduct, caDir + caFile, customActions, true);

                    caughtEx = null;
                    try
                    {
                        Installer.InstallProduct(caProduct, String.Empty);
                    }
                    catch (Exception ex) { caughtEx = ex; }
                    Assert.IsInstanceOfType(caughtEx, typeof(InstallCanceledException),
                        "Exception thrown while installing 64bit product: " + caughtEx);
                }
            }
            finally
            {
                Installer.SetExternalUI(prevHandler, InstallLogModes.None);
            }
        }

        private void CreateCustomActionProduct(
            string msiFile, string customActionFile, IList<string> customActions, bool sixtyFourBit)
        {
            using (Database db = new Database(msiFile, DatabaseOpenMode.CreateDirect))
            {
                WindowsInstallerUtils.InitializeProductDatabase(db, sixtyFourBit);
                WindowsInstallerUtils.CreateTestProduct(db);

                if (!File.Exists(customActionFile))
                    throw new FileNotFoundException(customActionFile);

                using (Record binRec = new Record(2))
                {
                    binRec[1] = Path.GetFileName(customActionFile);
                    binRec.SetStream(2, customActionFile);

                    db.Execute("INSERT INTO `Binary` (`Name`, `Data`) VALUES (?, ?)", binRec);
                }

                using (Record binRec2 = new Record(2))
                {
                    binRec2[1] = "TestData";
                    binRec2.SetStream(2, new MemoryStream(Encoding.UTF8.GetBytes("This is a test data stream.")));

                    db.Execute("INSERT INTO `Binary` (`Name`, `Data`) VALUES (?, ?)", binRec2);
                }

                for (int i = 0; i < customActions.Count; i++)
                {
                    db.Execute(
                        "INSERT INTO `CustomAction` (`Action`, `Type`, `Source`, `Target`) VALUES ('{0}', 1, '{1}', '{2}')",
                        customActions[i],
                        Path.GetFileName(customActionFile),
                        customActions[i]);
                    db.Execute(
                        "INSERT INTO `InstallExecuteSequence` (`Action`, `Condition`, `Sequence`) VALUES ('{0}', '', {1})",
                        customActions[i],
                        101 + i);
                }

                db.Execute("INSERT INTO `Property` (`Property`, `Value`) VALUES ('SampleCATest', 'TestValue')");

                db.Commit();
            }
        }

        [TestMethod]
        public void CustomActionData()
        {
            string dataString = "Key1=Value1;Key2=;Key3;Key4=Value=4;Key5";
            string dataString2 = "Key1=;Key2=Value2;Key3;Key4;Key6=Value;;6=6;Key7=Value7";

            CustomActionData data = new CustomActionData(dataString);
            Assert.AreEqual<string>(dataString, data.ToString());

            data["Key1"] = String.Empty;
            data["Key2"] = "Value2";
            data["Key4"] = null;
            data.Remove("Key5");
            data["Key6"] = "Value;6=6";
            data["Key7"] = "Value7";

            Assert.AreEqual<string>(dataString2, data.ToString());

            MyDataClass myData = new MyDataClass();
            myData.Member1 = "test1";
            myData.Member2 = "test2";
            data.AddObject("MyData", myData);

            string myDataString = data.ToString();
            CustomActionData data2 = new CustomActionData(myDataString);

            MyDataClass myData2 = data2.GetObject<MyDataClass>("MyData");
            Assert.AreEqual<MyDataClass>(myData, myData2);

            List<string> myComplexDataObject = new List<string>();
            myComplexDataObject.Add("CValue1");
            myComplexDataObject.Add("CValue2");
            myComplexDataObject.Add("CValue3");

            CustomActionData myComplexData = new CustomActionData();
            myComplexData.AddObject("MyComplexData", myComplexDataObject);
            myComplexData.AddObject("NestedData", data);
            string myComplexDataString = myComplexData.ToString();

            CustomActionData myComplexData2 = new CustomActionData(myComplexDataString);
            List<string> myComplexDataObject2 = myComplexData2.GetObject<List<string>>("MyComplexData");

            Assert.AreEqual<int>(myComplexDataObject.Count, myComplexDataObject2.Count);
            for (int i = 0; i < myComplexDataObject.Count; i++)
            {
                Assert.AreEqual<string>(myComplexDataObject[i], myComplexDataObject2[i]);
            }

            data2 = myComplexData2.GetObject<CustomActionData>("NestedData");
            Assert.AreEqual<string>(data.ToString(), data2.ToString());
        }

        public class MyDataClass
        {
            public string Member1;
            public string Member2;

            public override bool Equals(object obj)
            {
                MyDataClass other = obj as MyDataClass;
                return other != null && this.Member1 == other.Member1 && this.Member2 == other.Member2;
            }

            public override int GetHashCode()
            {
                return (this.Member1 != null ? this.Member1.GetHashCode() : 0) ^
                       (this.Member2 != null ? this.Member2.GetHashCode() : 0);
            }
        }
    }
}
