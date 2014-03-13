//-------------------------------------------------------------------------------------------------
// <copyright file="LinqTest.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Deployment.WindowsInstaller.Linq;

namespace Microsoft.Deployment.Test
{
    [TestClass]
    public class LinqTest
    {
        private void InitLinqTestDatabase(QDatabase db)
        {
            WindowsInstallerUtils.InitializeProductDatabase(db);
            WindowsInstallerUtils.CreateTestProduct(db);

            db.Execute(
                "INSERT INTO `Feature` (`Feature`, `Title`, `Description`, `Level`, `Attributes`) VALUES ('{0}', '{1}', '{2}', {3}, {4})",
                "TestFeature2",
                "Test Feature 2",
                "Test Feature 2 Description",
                1,
                (int) FeatureAttributes.None);

            WindowsInstallerUtils.AddRegistryComponent(
                db, "TestFeature2", "MyTestRegComp",
                Guid.NewGuid().ToString("B"),
                "SOFTWARE\\Microsoft\\DTF\\Test",
                "MyTestRegComp", "test");
            WindowsInstallerUtils.AddRegistryComponent(
                db, "TestFeature2", "MyTestRegComp2",
                Guid.NewGuid().ToString("B"),
                "SOFTWARE\\Microsoft\\DTF\\Test",
                "MyTestRegComp2", "test2");
            WindowsInstallerUtils.AddRegistryComponent(
                db, "TestFeature2", "excludeComp",
                Guid.NewGuid().ToString("B"),
                "SOFTWARE\\Microsoft\\DTF\\Test",
                "MyTestRegComp3", "test3");

            db.Commit();

            db.Log = Console.Out;
        }

        [TestMethod]
        public void LinqSimple()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);

                var comps = from c in db.Components
                            select c;

                int count = 0;
                foreach (var c in comps)
                {
                    Console.WriteLine(c);
                    count++;
                }

                Assert.AreEqual<int>(4, count);
            }
        }

        [TestMethod]
        public void LinqWhereNull()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);

                var features = from f in db.Features
                               where f.Description != null
                               select f;

                int count = 0;
                foreach (var f in features)
                {
                    Console.WriteLine(f);
                    Assert.AreEqual<string>("TestFeature2", f.Feature);
                    count++;
                }

                Assert.AreEqual<int>(1, count);

                var features2 = from f in db.Features
                                where f.Description == null
                                select f;

                count = 0;
                foreach (var f in features2)
                {
                    Console.WriteLine(f);
                    Assert.AreEqual<string>("TestFeature1", f.Feature);
                    count++;
                }

                Assert.AreEqual<int>(1, count);
            }
        }

        [TestMethod]
        public void LinqWhereOperators()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);

                for (int i = 0; i < 100; i++)
                {
                    var newFile = db.Files.NewRecord();
                    newFile.File = "TestFile" + i;
                    newFile.Component_ = "TestComponent";
                    newFile.FileName = "TestFile" + i + ".txt";
                    newFile.FileSize = i % 10;
                    newFile.Sequence = i;
                    newFile.Insert();
                }

                var files1 = from f in db.Files where f.Sequence < 40 select f;
                Assert.AreEqual<int>(40, files1.AsEnumerable().Count());

                var files2 = from f in db.Files where f.Sequence <= 40 select f;
                Assert.AreEqual<int>(41, files2.AsEnumerable().Count());

                var files3 = from f in db.Files where f.Sequence > 40 select f;
                Assert.AreEqual<int>(59, files3.AsEnumerable().Count());

                var files4 = from f in db.Files where f.Sequence >= 40 select f;
                Assert.AreEqual<int>(60, files4.AsEnumerable().Count());

                var files5 = from f in db.Files where 40 < f.Sequence select f;
                Assert.AreEqual<int>(59, files5.AsEnumerable().Count());

                var files6 = from f in db.Files where 40 <= f.Sequence select f;
                Assert.AreEqual<int>(60, files6.AsEnumerable().Count());

                var files7 = from f in db.Files where 40 > f.Sequence select f;
                Assert.AreEqual<int>(40, files7.AsEnumerable().Count());

                var files8 = from f in db.Files where 40 >= f.Sequence select f;
                Assert.AreEqual<int>(41, files8.AsEnumerable().Count());

                var files9 = from f in db.Files where f.Sequence == 40 select f;
                Assert.AreEqual<int>(40, files9.AsEnumerable().First().Sequence);

                var files10 = from f in db.Files where f.Sequence != 40 select f;
                Assert.AreEqual<int>(99, files10.AsEnumerable().Count());

                var files11 = from f in db.Files where 40 == f.Sequence select f;
                Assert.AreEqual<int>(40, files11.AsEnumerable().First().Sequence);

                var files12 = from f in db.Files where 40 != f.Sequence select f;
                Assert.AreEqual<int>(99, files12.AsEnumerable().Count());
            }
        }

        [TestMethod]
        public void LinqShapeSelect()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);

                Console.WriteLine("Running LINQ query 1.");
                var features1 = from f in db.Features
                                select new { Name = f.Feature,
                                             Desc = f.Description };

                int count = 0;
                foreach (var f in features1)
                {
                    Console.WriteLine(f);
                    count++;
                }

                Assert.AreEqual<int>(2, count);

                Console.WriteLine();
                Console.WriteLine("Running LINQ query 2.");
                var features2 = from f in db.Features
                                where f.Description != null
                                select new { Name = f.Feature,
                                             Desc = f.Description.ToLower() };

                count = 0;
                foreach (var f in features2)
                {
                    Console.WriteLine(f);
                    Assert.AreEqual<string>("TestFeature2", f.Name);
                    count++;
                }

                Assert.AreEqual<int>(1, count);
            }
        }

        [TestMethod]
        public void LinqUpdateNullableString()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);

                string newDescription = "New updated feature description.";

                var features = from f in db.Features
                               where f.Description != null
                               select f;

                int count = 0;
                foreach (var f in features)
                {
                    Console.WriteLine(f);
                    Assert.AreEqual<string>("TestFeature2", f.Feature);
                    f.Description = newDescription;
                    count++;
                }

                Assert.AreEqual<int>(1, count);

                var features2 = from f in db.Features
                                where f.Description == newDescription
                                select f;
                count = 0;
                foreach (var f in features2)
                {
                    Console.WriteLine(f);
                    Assert.AreEqual<string>("TestFeature2", f.Feature);
                    f.Description = null;
                    count++;
                }

                Assert.AreEqual<int>(1, count);

                var features3 = from f in db.Features
                                where f.Description == null
                                select f.Feature;
                count = 0;
                foreach (var f in features3)
                {
                    Console.WriteLine(f);
                    count++;
                }

                Assert.AreEqual<int>(2, count);

                db.Commit();
            }
        }

        [TestMethod]
        public void LinqInsertDelete()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);

                var newProp = db.Properties.NewRecord();
                newProp.Property = "TestNewProp1";
                newProp.Value = "TestNewValue";
                newProp.Insert();

                string prop = (from p in db.Properties
                            where p.Property == "TestNewProp1"
                            select p.Value).AsEnumerable().First();
                Assert.AreEqual<string>("TestNewValue", prop);

                newProp.Delete();

                int propCount = (from p in db.Properties
                                 where p.Property == "TestNewProp1"
                                 select p.Value).AsEnumerable().Count();
                Assert.AreEqual<int>(0, propCount);

                db.Commit();
            }
        }

        [TestMethod]
        public void LinqQueryQRecord()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);

                var installFilesSeq = (from a in db["InstallExecuteSequence"]
                                       where a["Action"] == "InstallFiles"
                                       select a["Sequence"]).AsEnumerable().First();
                Assert.AreEqual<string>("4000", installFilesSeq);
            }
        }

        [TestMethod]
        public void LinqOrderBy()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);

                var actions = from a in db.InstallExecuteSequences
                              orderby a.Sequence
                              select a.Action;
                foreach (var a in actions)
                {
                    Console.WriteLine(a);
                }

                var files = from f in db.Files
                            orderby f.FileSize, f["Sequence"]
                            where f.Attributes == FileAttributes.None
                            select f;

                foreach (var f in files)
                {
                    Console.WriteLine(f);
                }
            }
        }

        [TestMethod]
        public void LinqTwoWayJoin()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);
                int count;

                var regs = from r in db.Registries
                           join c in db["Component"] on r.Component_ equals c["Component"]
                           where c["Component"] == "MyTestRegComp" &&
                                 r.Root == RegistryRoot.UserOrMachine
                           select new { Reg = r.Registry, Dir = c["Directory_"] };

                count = 0;
                foreach (var r in regs)
                {
                    Console.WriteLine(r);
                    count++;
                }
                Assert.AreEqual<int>(1, count);

                var regs2 = from r in db.Registries
                            join c in db.Components on r.Component_ equals c.Component
                            where c.Component == "MyTestRegComp" &&
                                  r.Root == RegistryRoot.UserOrMachine
                            select new { Reg = r, Dir = c.Directory_ };

                count = 0;
                foreach (var r in regs2)
                {
                    Assert.IsNotNull(r.Reg.Registry);
                    Console.WriteLine(r);
                    count++;
                }
                Assert.AreEqual<int>(1, count);

                var regs3 = from r in db.Registries
                            join c in db.Components on r.Component_ equals c.Component
                            where c.Component == "MyTestRegComp" &&
                                  r.Root == RegistryRoot.UserOrMachine
                            select r;

                count = 0;
                foreach (var r in regs3)
                {
                    Assert.IsNotNull(r.Registry);
                    Console.WriteLine(r);
                    count++;
                }
                Assert.AreEqual<int>(1, count);

            }
        }

        [TestMethod]
        public void LinqFourWayJoin()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);
                int count;

                IList<string> pretest = db.ExecuteStringQuery(
                    "SELECT `Feature`.`Feature` " +
                    "FROM `Feature`, `FeatureComponents`, `Component`, `Registry` " +
                    "WHERE `Feature`.`Feature` = `FeatureComponents`.`Feature_` " +
                    "AND `FeatureComponents`.`Component_` = `Component`.`Component` " +
                    "AND `Component`.`Component` = `Registry`.`Component_` " +
                    "AND (`Registry`.`Registry` = 'MyTestRegCompReg1')");
                Assert.AreEqual<int>(1, pretest.Count);

                var features = from f in db.Features
                               join fc in db.FeatureComponents on f.Feature equals fc.Feature_
                               join c in db.Components on fc.Component_ equals c.Component
                               join r in db.Registries on c.Component equals r.Component_
                               where r.Registry == "MyTestRegCompReg1"
                               select f.Feature;

                count = 0;
                foreach (var featureName in features)
                {
                    Console.WriteLine(featureName);
                    count++;
                }
                Assert.AreEqual<int>(1, count);

            }
        }

        [TestMethod]
        public void EnumTable()
        {
            using (QDatabase db = new QDatabase("testlinq.msi", DatabaseOpenMode.Create))
            {
                this.InitLinqTestDatabase(db);
                int count = 0;
                foreach (var comp in db.Components)
                {
                    Console.WriteLine(comp);
                    count++;
                }
                Assert.AreNotEqual<int>(0, count);
            }
        }

        [TestMethod]
        public void DatabaseAsQueryable()
        {
            using (Database db = new Database("testlinq.msi", DatabaseOpenMode.Create))
            {
                WindowsInstallerUtils.InitializeProductDatabase(db);
                WindowsInstallerUtils.CreateTestProduct(db);

                var comps = from c in db.AsQueryable().Components
                            select c;

                int count = 0;
                foreach (var c in comps)
                {
                    Console.WriteLine(c);
                    count++;
                }

                Assert.AreEqual<int>(1, count);
            }
        }

        [TestMethod]
        public void EnumProducts()
        {
            var products = from p in ProductInstallation.AllProducts
                           where p.Publisher == "Outercurve Foundation"
                           select new { Name = p.ProductName,
                                        Ver = p.ProductVersion,
                                        Company = p.Publisher,
                                        InstallDate = p.InstallDate,
                                        PackageCode = p.AdvertisedPackageCode };

            foreach (var p in products)
            {
                Console.WriteLine(p);
                Assert.IsTrue(p.Company == "Outercurve Foundation");
            }
        }

        [TestMethod]
        public void EnumFeatures()
        {
            foreach (var p in ProductInstallation.AllProducts)
            {
                Console.WriteLine(p.ProductName);

                foreach (var f in p.Features)
                {
                    Console.WriteLine("\t" + f.FeatureName);
                }
            }
        }

        [TestMethod]
        public void EnumComponents()
        {
            var comps = from c in ComponentInstallation.AllComponents
                        where c.State == InstallState.Local &&
                              c.Product.Publisher == "Outercurve Foundation"
                        select c.Path;

            int count = 0;
            foreach (var c in comps)
            {
                if (++count == 100) break;

                Console.WriteLine(c);
            }

            Assert.AreEqual<int>(100, count);
        }
    }

}
