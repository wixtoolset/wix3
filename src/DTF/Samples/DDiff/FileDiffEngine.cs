using System;
using System.IO;

namespace Microsoft.Deployment.Samples.DDiff
{
	public class FileDiffEngine : IDiffEngine
	{
		public FileDiffEngine()
		{
		}

		public virtual float GetDiffQuality(string diffInput1, string diffInput2, string[] options, IDiffEngineFactory diffFactory)
		{
			if(diffInput1 != null && File.Exists(diffInput1) &&
			   diffInput2 != null && File.Exists(diffInput2))
			{
				return .10f;
			}
			else
			{
				return 0;
			}
		}

		public bool GetDiff(string diffInput1, string diffInput2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory)
		{
			bool difference = false;

			FileInfo file1 = new FileInfo(diffInput1);
			FileInfo file2 = new FileInfo(diffInput2);

			if(file1.Length != file2.Length)
			{
				diffOutput.WriteLine("{0}File size: {1} -> {2}", linePrefix, file1.Length, file2.Length);
				difference = true;
			}
			else
			{
				FileStream stream1 = file1.Open(FileMode.Open, FileAccess.Read, FileShare.Read);
				FileStream stream2 = file2.Open(FileMode.Open, FileAccess.Read, FileShare.Read);

				byte[] buf1 = new byte[512];
				byte[] buf2 = new byte[512];

				while(!difference)
				{
					int count1 = stream1.Read(buf1, 0, buf1.Length);
					int count2 = stream2.Read(buf2, 0, buf2.Length);

					for(int i = 0; i < count1; i++)
					{
						if(i == count2 || buf1[i] != buf2[i])
						{
							difference = true;
							break;
						}
					}
					if(count1 < buf1.Length) // EOF
					{
						break;
					}
				}

				stream1.Close();
				stream2.Close();

				if(difference)
				{
					diffOutput.WriteLine("{0}Files differ.", linePrefix);
				}
			}

			return difference;
		}

		public virtual IDiffEngine Clone()
		{
			return new FileDiffEngine();
		}
	}
}
