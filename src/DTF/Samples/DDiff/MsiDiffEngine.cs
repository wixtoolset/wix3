// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Deployment.WindowsInstaller.Package;

namespace Microsoft.Deployment.Samples.DDiff
{
    public class MsiDiffEngine : IDiffEngine
    {
        public MsiDiffEngine()
        {
        }

        protected bool IsMsiDatabase(string file)
        {
            // TODO: use something smarter?
            switch(Path.GetExtension(file).ToLower())
            {
                case ".msi":  return true;
                case ".msm":  return true;
                case ".pcp":  return true;
                default    :  return false;
            }
        }

        protected bool IsMspPatch(string file)
        {
            // TODO: use something smarter?
            switch(Path.GetExtension(file).ToLower())
            {
                case ".msp":  return true;
                default    :  return false;
            }
        }

        public virtual float GetDiffQuality(string diffInput1, string diffInput2, string[] options, IDiffEngineFactory diffFactory)
        {
            if(diffInput1 != null && File.Exists(diffInput1) &&
               diffInput2 != null && File.Exists(diffInput2) &&
               (IsMsiDatabase(diffInput1) || IsMsiDatabase(diffInput2)))
            {
                return .70f;
            }
            else if(diffInput1 != null && File.Exists(diffInput1) &&
                    diffInput2 != null && File.Exists(diffInput2) &&
                    (IsMspPatch(diffInput1) || IsMspPatch(diffInput2)))
            {
                return .60f;
            }
            else
            {
                return 0;
            }
        }

        public virtual bool GetDiff(string diffInput1, string diffInput2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory)
        {
            bool difference = false;
            Database db1 = new Database(diffInput1, DatabaseOpenMode.ReadOnly);
            Database db2 = new Database(diffInput2, DatabaseOpenMode.ReadOnly);

            if(GetSummaryInfoDiff(db1, db2, options, diffOutput, linePrefix, diffFactory)) difference = true;
            if(GetDatabaseDiff(db1, db2, options, diffOutput, linePrefix, diffFactory)) difference = true;
            if(GetStreamsDiff(db1, db2, options, diffOutput, linePrefix, diffFactory)) difference = true;

            db1.Close();
            db2.Close();
            return difference;
        }

        protected bool GetSummaryInfoDiff(Database db1, Database db2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory)
        {
            bool difference = false;

            SummaryInfo summInfo1 = db1.SummaryInfo;
            SummaryInfo summInfo2 = db2.SummaryInfo;
            if(summInfo1.Title          != summInfo2.Title         ) { diffOutput.WriteLine("{0}SummaryInformation.Title {{{1}}}->{{{2}}}", linePrefix, summInfo1.Title, summInfo2.Title); difference = true; }
            if(summInfo1.Subject        != summInfo2.Subject       ) { diffOutput.WriteLine("{0}SummaryInformation.Subject {{{1}}}->{{{2}}}", linePrefix, summInfo1.Subject, summInfo2.Subject); difference = true; }
            if(summInfo1.Author         != summInfo2.Author        ) { diffOutput.WriteLine("{0}SummaryInformation.Author {{{1}}}->{{{2}}}", linePrefix, summInfo1.Author, summInfo2.Author); difference = true; }
            if(summInfo1.Keywords       != summInfo2.Keywords      ) { diffOutput.WriteLine("{0}SummaryInformation.Keywords {{{1}}}->{{{2}}}", linePrefix, summInfo1.Keywords, summInfo2.Keywords); difference = true; }
            if(summInfo1.Comments       != summInfo2.Comments      ) { diffOutput.WriteLine("{0}SummaryInformation.Comments {{{1}}}->{{{2}}}", linePrefix, summInfo1.Comments, summInfo2.Comments); difference = true; }
            if(summInfo1.Template       != summInfo2.Template      ) { diffOutput.WriteLine("{0}SummaryInformation.Template {{{1}}}->{{{2}}}", linePrefix, summInfo1.Template, summInfo2.Template); difference = true; }
            if(summInfo1.LastSavedBy    != summInfo2.LastSavedBy   ) { diffOutput.WriteLine("{0}SummaryInformation.LastSavedBy {{{1}}}->{{{2}}}", linePrefix, summInfo1.LastSavedBy, summInfo2.LastSavedBy); difference = true; }
            if(summInfo1.RevisionNumber != summInfo2.RevisionNumber) { diffOutput.WriteLine("{0}SummaryInformation.RevisionNumber {{{1}}}->{{{2}}}", linePrefix, summInfo1.RevisionNumber, summInfo2.RevisionNumber); difference = true; }
            if(summInfo1.CreatingApp    != summInfo2.CreatingApp   ) { diffOutput.WriteLine("{0}SummaryInformation.CreatingApp {{{1}}}->{{{2}}}", linePrefix, summInfo1.CreatingApp, summInfo2.CreatingApp); difference = true; }
            if(summInfo1.LastPrintTime  != summInfo2.LastPrintTime ) { diffOutput.WriteLine("{0}SummaryInformation.LastPrintTime {{{1}}}->{{{2}}}", linePrefix, summInfo1.LastPrintTime, summInfo2.LastPrintTime); difference = true; }
            if(summInfo1.CreateTime     != summInfo2.CreateTime    ) { diffOutput.WriteLine("{0}SummaryInformation.CreateTime {{{1}}}->{{{2}}}", linePrefix, summInfo1.CreateTime, summInfo2.CreateTime); difference = true; }
            if(summInfo1.LastSaveTime   != summInfo2.LastSaveTime  ) { diffOutput.WriteLine("{0}SummaryInformation.LastSaveTime {{{1}}}->{{{2}}}", linePrefix, summInfo1.LastSaveTime, summInfo2.LastSaveTime); difference = true; }
            if(summInfo1.CodePage       != summInfo2.CodePage      ) { diffOutput.WriteLine("{0}SummaryInformation.Codepage {{{1}}}->{{{2}}}", linePrefix, summInfo1.CodePage, summInfo2.CodePage); difference = true; }
            if(summInfo1.PageCount      != summInfo2.PageCount     ) { diffOutput.WriteLine("{0}SummaryInformation.PageCount {{{1}}}->{{{2}}}", linePrefix, summInfo1.PageCount, summInfo2.PageCount); difference = true; }
            if(summInfo1.WordCount      != summInfo2.WordCount     ) { diffOutput.WriteLine("{0}SummaryInformation.WordCount {{{1}}}->{{{2}}}", linePrefix, summInfo1.WordCount, summInfo2.WordCount); difference = true; }
            if(summInfo1.CharacterCount != summInfo2.CharacterCount) { diffOutput.WriteLine("{0}SummaryInformation.CharacterCount {{{1}}}->{{{2}}}", linePrefix, summInfo1.CharacterCount, summInfo2.CharacterCount); difference = true; }
            if(summInfo1.Security       != summInfo2.Security      ) { diffOutput.WriteLine("{0}SummaryInformation.Security {{{1}}}->{{{2}}}", linePrefix, summInfo1.Security, summInfo2.Security); difference = true; }
            summInfo1.Close();
            summInfo2.Close();

            return difference;
        }

        protected bool GetDatabaseDiff(Database db1, Database db2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory)
        {
            bool difference = false;

            string tempFile = Path.GetTempFileName();
            if(db2.GenerateTransform(db1, tempFile))
            {
                difference = true;

                Database db = db1;
                db.ViewTransform(tempFile);

                string row, column, change;
                using (View view = db.OpenView("SELECT `Table`, `Column`, `Row`, `Data`, `Current` " +
                    "FROM `_TransformView` ORDER BY `Table`, `Row`"))
                {
                    view.Execute();
                    
                    foreach (Record rec in view) using (rec)
                    {
                        column = String.Format("{0} {1}", rec[1], rec[2]);
                        change = "";
                        if (rec.IsNull(3))
                        {
                            row = "<DDL>";
                            if (!rec.IsNull(4))
                            {
                                change = "[" + rec[5] + "]: " + DecodeColDef(rec.GetInteger(4));
                            }
                        }
                        else
                        {
                            row = "[" + String.Join(",", rec.GetString(3).Split('\t')) + "]";
                            if (rec.GetString(2) != "INSERT" && rec.GetString(2) != "DELETE")
                            {
                                column = String.Format("{0}.{1}", rec[1], rec[2]);
                                change = "{" + rec[5] + "}->{" + rec[4] + "}";
                            }
                        }

                        diffOutput.WriteLine("{0}{1,-25} {2} {3}", linePrefix, column, row, change);
                    }
                }
            }
            File.Delete(tempFile);

            return difference;
        }

        private string DecodeColDef(int colDef)
        {
            const int icdLong       = 0x0000;
            const int icdShort      = 0x0400;
            const int icdObject     = 0x0800;
            const int icdString     = 0x0C00;
            const int icdTypeMask   = 0x0F00;
            const int icdNullable   = 0x1000;
            const int icdPrimaryKey = 0x2000;

            string def = "";
            switch(colDef & (icdTypeMask))
            {
                case icdLong  :  def = "LONG";  break;
                case icdShort :  def = "SHORT";  break;
                case icdObject:  def = "OBJECT";  break;
                case icdString:  def = "CHAR[" + (colDef & 0xFF) + "]";  break;
            }
            if((colDef & icdNullable) != 0)
            {
                def = def + " NOT NULL";
            }
            if((colDef & icdPrimaryKey) != 0)
            {
                def = def + " PRIMARY KEY";
            }
            return def;
        }

        protected bool GetStreamsDiff(Database db1, Database db2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory)
        {
            bool difference = false;

            IList<string> streams1List = db1.ExecuteStringQuery("SELECT `Name` FROM `_Streams`");
            IList<string> streams2List = db2.ExecuteStringQuery("SELECT `Name` FROM `_Streams`");
            string[] streams1 = new string[streams1List.Count];
            string[] streams2 = new string[streams2List.Count];
            streams1List.CopyTo(streams1, 0);
            streams2List.CopyTo(streams2, 0);

            IComparer caseInsComp = CaseInsensitiveComparer.Default;
            Array.Sort(streams1, caseInsComp);
            Array.Sort(streams2, caseInsComp);

            for (int i1 = 0, i2 = 0; i1 < streams1.Length || i2 < streams2.Length; )
            {
                int comp;
                if (i1 == streams1.Length)
                {
                    comp = 1;
                }
                else if (i2 == streams2.Length)
                {
                    comp = -1;
                }
                else
                {
                    comp = caseInsComp.Compare(streams1[i1], streams2[i2]);
                }
                if(comp < 0)
                {
                    diffOutput.WriteLine("{0}< {1}", linePrefix, streams1[i1]);
                    i1++;
                    difference = true;
                }
                else if(comp > 0)
                {
                    diffOutput.WriteLine("{0}> {1}", linePrefix, streams2[i2]);
                    i2++;
                    difference = true;
                }
                else
                {
                    if(streams1[i1] != ("" + ((char)5) + "SummaryInformation"))
                    {
                        string tempFile1 = Path.GetTempFileName();
                        string tempFile2 = Path.GetTempFileName();
                    
                        using (View view = db1.OpenView(String.Format("SELECT `Data` FROM `_Streams` WHERE `Name` = '{0}'", streams1[i1])))
                        {
                            view.Execute();

                            using (Record rec = view.Fetch())
                            {
                                rec.GetStream(1, tempFile1);
                            }
                        }

                        using (View view = db2.OpenView(String.Format("SELECT `Data` FROM `_Streams` WHERE `Name` = '{0}'", streams2[i2])))
                        {
                            view.Execute();

                            using (Record rec = view.Fetch())
                            {
                                rec.GetStream(1, tempFile2);
                            }
                        }

                        IDiffEngine diffEngine = diffFactory.GetDiffEngine(tempFile1, tempFile2, options);
                        StringWriter sw = new StringWriter();
                        if(diffEngine.GetDiff(tempFile1, tempFile2, options, sw, linePrefix + "    ", diffFactory))
                        {
                            diffOutput.WriteLine("{0}{1}", linePrefix, streams1[i1]);
                            diffOutput.Write(sw.ToString());
                            difference = true;
                        }

                        File.Delete(tempFile1);
                        File.Delete(tempFile2);
                    }
                    i1++;
                    i2++;
                }
            }

            return difference;
        }

        public virtual IDiffEngine Clone()
        {
            return new MsiDiffEngine();
        }
    }
}
