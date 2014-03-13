namespace Microsoft.Deployment.Samples.ManagedCA
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using Microsoft.Deployment.WindowsInstaller;

    public class SampleCAs
    {
        [CustomAction]
        public static ActionResult SampleCA1(Session session)
        {
            using (Record msgRec = new Record(0))
            {
                msgRec[0] = "Hello from SampleCA1!" +
                    "\r\nCLR version is v" + Environment.Version;
                session.Message(InstallMessage.Info, msgRec);
                session.Message(InstallMessage.User, msgRec);
            }

            session.Log("Testing summary info...");
            SummaryInfo summInfo = session.Database.SummaryInfo;
            session.Log("MSI PackageCode = {0}", summInfo.RevisionNumber);
            session.Log("MSI ModifyDate = {0}", summInfo.LastSaveTime);

            string testProp = session["SampleCATest"];
            session.Log("Simple property test: [SampleCATest]={0}.", testProp);

            session.Log("Testing subdirectory extraction...");
            string testFilePath = "testsub\\SampleCAs.cs";
            if (!File.Exists(testFilePath))
            {
                session.Log("Subdirectory extraction failed. File not found: " + testFilePath);
                return ActionResult.Failure;
            }
            else
            {
                session.Log("Found file extracted in subdirectory.");
            }

            session.Log("Testing record stream extraction...");
            string tempFile = null;
            try
            {
                tempFile = Path.GetTempFileName();
                using (View binView = session.Database.OpenView(
                    "SELECT `Binary`.`Data` FROM `Binary`, `CustomAction` " +
                    "WHERE `CustomAction`.`Target` = 'SampleCA1' AND " +
                    "`CustomAction`.`Source` = `Binary`.`Name`"))
                {
                    binView.Execute();
                    using (Record binRec = binView.Fetch())
                    {
                        binRec.GetStream(1, tempFile);
                    }
                }

                session.Log("CA binary file size: {0}", new FileInfo(tempFile).Length);
                string binFileVersion = Installer.GetFileVersion(tempFile);
                session.Log("CA binary file version: {0}", binFileVersion);
            }
            finally
            {
                if (tempFile != null && File.Exists(tempFile))
                {
                    File.Delete(tempFile);
                }
            }

            session.Log("Testing record stream reading...");
            using (View binView2 = session.Database.OpenView("SELECT `Data` FROM `Binary` WHERE `Name` = 'TestData'"))
            {
                binView2.Execute();
                using (Record binRec2 = binView2.Fetch())
                {
                    Stream stream = binRec2.GetStream("Data");
                    string testData = new StreamReader(stream, System.Text.Encoding.UTF8).ReadToEnd();
                    session.Log("Test data: " + testData);
                }
            }

            session.Log("Listing components");
            using (View compView = session.Database.OpenView(
                "SELECT `Component` FROM `Component`"))
            {
                compView.Execute();
                foreach (Record compRec in compView)
                {
                    using (compRec)
                    {
                        session.Log("\t{0}", compRec["Component"]);
                    }
                }
            }

            session.Log("Testing the ability to access an external MSI database...");
            string tempDbFile = Path.GetTempFileName();
            using (Database tempDb = new Database(tempDbFile, DatabaseOpenMode.CreateDirect))
            {
                // Just create an empty database.
            }
            using (Database tempDb2 = new Database(tempDbFile))
            {
                // See if we can open and query the database.
                IList<string> tables = tempDb2.ExecuteStringQuery("SELECT `Name` FROM `_Tables`");
                session.Log("Found " + tables.Count + " tables in the newly created database.");
            }
            File.Delete(tempDbFile);

            return ActionResult.Success;
        }

        [CustomAction("SampleCA2")]
        public static ActionResult SampleCustomAction2(Session session)
        {
            using (Record msgRec = new Record(0))
            {
                msgRec[0] = "Hello from SampleCA2!";
                session.Message(InstallMessage.Info, msgRec);
                session.Message(InstallMessage.User, msgRec);
            }
            return ActionResult.UserExit;
        }
    }
}
