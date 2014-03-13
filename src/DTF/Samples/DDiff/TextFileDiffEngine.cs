using System;
using System.IO;
using System.Diagnostics;

namespace Microsoft.Deployment.Samples.DDiff
{
	public class TextFileDiffEngine : IDiffEngine
	{
		public TextFileDiffEngine()
		{
		}

		private bool IsTextFile(string file)
		{
			// Guess whether this is a text file by reading the first few bytes and checking for non-ascii chars.

			bool isText = true;
			FileStream stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read);
			byte[] buf = new byte[256];
			int count = stream.Read(buf, 0, buf.Length);
			for(int i = 0; i < count; i++)
			{
				if((buf[i] & 0x80) != 0)
				{
					isText = false;
					break;
				}
			}
			stream.Close();
			return isText;
		}

		public float GetDiffQuality(string diffInput1, string diffInput2, string[] options, IDiffEngineFactory diffFactory)
		{
			if(diffInput1 != null && File.Exists(diffInput1) &&
				diffInput2 != null && File.Exists(diffInput2) &&
				(IsTextFile(diffInput1) && IsTextFile(diffInput2)))
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
			try
			{
				bool difference = false;
				ProcessStartInfo psi = new ProcessStartInfo("diff.exe");
				psi.Arguments = String.Format("\"{0}\" \"{1}\"", diffInput1, diffInput2);
				psi.WorkingDirectory = null;
				psi.UseShellExecute = false;
				psi.WindowStyle = ProcessWindowStyle.Hidden;
				psi.RedirectStandardOutput = true;
				Process proc = Process.Start(psi);

				string line;
				while((line = proc.StandardOutput.ReadLine()) != null)
				{
					diffOutput.WriteLine("{0}{1}", linePrefix, line);
					difference = true;
				}

				proc.WaitForExit();
				return difference;
			}
			catch(System.ComponentModel.Win32Exception)  // If diff.exe is not found, just compare the bytes
			{
				return new FileDiffEngine().GetDiff(diffInput1, diffInput2, options, diffOutput, linePrefix, diffFactory);
			}
		}

		public IDiffEngine Clone()
		{
			return new TextFileDiffEngine();
		}
	}
}
