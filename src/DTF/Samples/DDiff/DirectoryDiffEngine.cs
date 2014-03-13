using System;
using System.IO;
using System.Collections;

namespace Microsoft.Deployment.Samples.DDiff
{
	public class DirectoryDiffEngine : IDiffEngine
	{
		public DirectoryDiffEngine()
		{
		}

		public virtual float GetDiffQuality(string diffInput1, string diffInput2, string[] options, IDiffEngineFactory diffFactory)
		{
			if(diffInput1 != null && Directory.Exists(diffInput1) &&
			   diffInput2 != null && Directory.Exists(diffInput2))
			{
				return .70f;
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

			string[] files1 = Directory.GetFiles(diffInput1);
			string[] files2 = Directory.GetFiles(diffInput2);
			for(int i1 = 0; i1 < files1.Length; i1++)
			{
				files1[i1] = Path.GetFileName(files1[i1]);
			}
			for(int i2 = 0; i2 < files2.Length; i2++)
			{
				files2[i2] = Path.GetFileName(files2[i2]);
			}
			Array.Sort(files1, caseInsComp);
			Array.Sort(files2, caseInsComp);

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
					string file1 = Path.Combine(diffInput1, files1[i1]);
					string file2 = Path.Combine(diffInput2, files2[i2]);
					IDiffEngine diffEngine = diffFactory.GetDiffEngine(file1, file2, options);
					StringWriter sw = new StringWriter();
					if(diffEngine.GetDiff(file1, file2, options, sw, linePrefix + "    ", diffFactory))
					{
						diffOutput.WriteLine("{0}{1}", linePrefix, files1[i1]);
						diffOutput.Write(sw.ToString());
						difference = true;
					}
					i1++;
					i2++;
				}
			}

			string[] dirs1 = Directory.GetDirectories(diffInput1);
			string[] dirs2 = Directory.GetDirectories(diffInput2);
			for(int i1 = 0; i1 < dirs1.Length; i1++)
			{
				dirs1[i1] = Path.GetFileName(dirs1[i1]);
			}
			for(int i2 = 0; i2 < dirs2.Length; i2++)
			{
				dirs2[i2] = Path.GetFileName(dirs2[i2]);
			}
			Array.Sort(dirs1, caseInsComp);
			Array.Sort(dirs2, caseInsComp);

			for(int i1 = 0, i2 = 0; i1 < dirs1.Length || i2 < dirs2.Length; )
			{
				int comp;
				if(i1 == dirs1.Length)
				{
					comp = 1;
				}
				else if(i2 == dirs2.Length)
				{
					comp = -1;
				}
				else
				{
					comp = caseInsComp.Compare(dirs1[i1], dirs2[i2]);
				}
				if(comp < 0)
				{
					diffOutput.WriteLine("{0}< {1}", linePrefix, dirs1[i1]);
					i1++;
					difference = true;
				}
				else if(comp > 0)
				{
					diffOutput.WriteLine("{0}> {1}", linePrefix, dirs2[i2]);
					i2++;
					difference = true;
				}
				else
				{
					string dir1 = Path.Combine(diffInput1, dirs1[i1]);
					string dir2 = Path.Combine(diffInput2, dirs2[i2]);
					IDiffEngine diffEngine = diffFactory.GetDiffEngine(dir1, dir2, options);
					StringWriter sw = new StringWriter();
					if(diffEngine.GetDiff(dir1, dir2, options, sw, linePrefix + "    ", diffFactory))
					{
						diffOutput.WriteLine("{0}{1}\\", linePrefix, dirs1[i1]);
						diffOutput.Write(sw.ToString());
						difference = true;
					}
					i1++;
					i2++;
				}
			}
			return difference;
		}

		public virtual IDiffEngine Clone()
		{
			return new DirectoryDiffEngine();
		}
	}
}
