using System;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;

namespace Microsoft.Deployment.Samples.DDiff
{
	public class VersionedFileDiffEngine : IDiffEngine
	{
		public VersionedFileDiffEngine()
		{
		}

		private bool IsVersionedFile(string file)
		{
			return Installer.GetFileVersion(file) != "";
		}

		public float GetDiffQuality(string diffInput1, string diffInput2, string[] options, IDiffEngineFactory diffFactory)
		{
			if(diffInput1 != null && File.Exists(diffInput1) &&
			   diffInput2 != null && File.Exists(diffInput2) &&
			   (IsVersionedFile(diffInput1) || IsVersionedFile(diffInput2)))
			{
				return .20f;
			}
			else
			{
				return 0;
			}
		}

		public bool GetDiff(string diffInput1, string diffInput2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory)
		{
			bool difference = false;

			string ver1 = Installer.GetFileVersion(diffInput1);
			string ver2 = Installer.GetFileVersion(diffInput2);

			if(ver1 != ver2)
			{
				diffOutput.WriteLine("{0}File version: {1} -> {2}", linePrefix, ver1, ver2);
				difference = true;
			}
			else
			{
				FileStream stream1 = new FileStream(diffInput1, FileMode.Open, FileAccess.Read, FileShare.Read);
				FileStream stream2 = new FileStream(diffInput2, FileMode.Open, FileAccess.Read, FileShare.Read);

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
					diffOutput.WriteLine("{0}File versions match but bits differ.", linePrefix);
				}
			}

			return difference;
		}

		public IDiffEngine Clone()
		{
			return new VersionedFileDiffEngine();
		}
	}
}
