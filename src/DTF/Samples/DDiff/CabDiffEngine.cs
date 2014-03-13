using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using Microsoft.Deployment.Compression.Cab;

namespace Microsoft.Deployment.Samples.DDiff
{
	public class CabDiffEngine : IDiffEngine
	{
		public CabDiffEngine()
		{
		}

		private bool IsCabinetFile(string file)
		{
			using(FileStream fileStream = File.OpenRead(file))
			{
				return new CabEngine().IsArchive(fileStream);
			}
		}

		public virtual float GetDiffQuality(string diffInput1, string diffInput2, string[] options, IDiffEngineFactory diffFactory)
		{
			if(diffInput1 != null && File.Exists(diffInput1) &&
			   diffInput2 != null && File.Exists(diffInput2) &&
			   (IsCabinetFile(diffInput1) || IsCabinetFile(diffInput2)))
			{
				return .80f;
			}
			else
			{
				return 0;
			}
		}

		public bool GetDiff(string diffInput1, string diffInput2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory)
		{
			bool difference = false;
			IComparer caseInsComp = CaseInsensitiveComparer.Default;

			// TODO: Make this faster by extracting the whole cab at once.
			// TODO: Optimize for the match case by first comparing the whole cab files.

            CabInfo cab1 = new CabInfo(diffInput1);
            CabInfo cab2 = new CabInfo(diffInput2);
			IList<CabFileInfo> cabFilesList1 = cab1.GetFiles();
            IList<CabFileInfo> cabFilesList2 = cab2.GetFiles();
            CabFileInfo[] cabFiles1 = new CabFileInfo[cabFilesList1.Count];
            CabFileInfo[] cabFiles2 = new CabFileInfo[cabFilesList2.Count];
            cabFilesList1.CopyTo(cabFiles1, 0);
            cabFilesList2.CopyTo(cabFiles2, 0);
			string[] files1 = new string[cabFiles1.Length];
			string[] files2 = new string[cabFiles2.Length];
			for(int i1 = 0; i1 < cabFiles1.Length; i1++) files1[i1] = cabFiles1[i1].Name;
			for(int i2 = 0; i2 < cabFiles2.Length; i2++) files2[i2] = cabFiles2[i2].Name;
			Array.Sort(files1, cabFiles1, caseInsComp);
			Array.Sort(files2, cabFiles2, caseInsComp);


			for(int i1 = 0, i2 = 0; i1 < files1.Length || i2 < files2.Length; )
			{
				int comp;
				if(i1 == files1.Length)
				{
					comp = 1;
				}
				else if(i2 == files2.Length)
				{
					comp = -1;
				}
				else
				{
					comp = caseInsComp.Compare(files1[i1], files2[i2]);
				}
				if(comp < 0)
				{
					diffOutput.WriteLine("{0}< {1}", linePrefix, files1[i1]);
					i1++;
					difference = true;
				}
				else if(comp > 0)
				{
					diffOutput.WriteLine("{0}> {1}", linePrefix, files2[i2]);
					i2++;
					difference = true;
				}
				else
				{
					string tempFile1 = Path.GetTempFileName();
					string tempFile2 = Path.GetTempFileName();
					cabFiles1[i1].CopyTo(tempFile1, true);
					cabFiles2[i2].CopyTo(tempFile2, true);
					IDiffEngine diffEngine = diffFactory.GetDiffEngine(tempFile1, tempFile2, options);
					StringWriter sw = new StringWriter();
					if(diffEngine.GetDiff(tempFile1, tempFile2, options, sw, linePrefix + "    ", diffFactory))
					{
						diffOutput.WriteLine("{0}{1}", linePrefix, files1[i1]);
						diffOutput.Write(sw.ToString());
						difference = true;
					}

					File.SetAttributes(tempFile1, File.GetAttributes(tempFile1) & ~FileAttributes.ReadOnly);
					File.SetAttributes(tempFile2, File.GetAttributes(tempFile2) & ~FileAttributes.ReadOnly);
					try
					{
						File.Delete(tempFile1);
						File.Delete(tempFile2);
					}
					catch(IOException)
					{
#if DEBUG
						Console.WriteLine("Could not delete temporary files {0} and {1}", tempFile1, tempFile2);
#endif
					}
					i1++;
					i2++;
				}
			}

			return difference;
		}

		public virtual IDiffEngine Clone()
		{
			return new CabDiffEngine();
		}
	}
}
