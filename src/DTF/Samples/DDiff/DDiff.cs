using System;
using System.IO;
using System.Text;

namespace Microsoft.Deployment.Samples.DDiff
{
	public class DDiff
	{
        public static void Usage(TextWriter w)
        {
			w.WriteLine("Usage: DDiff target1 target2 [options]");
			w.WriteLine("Example: DDiff d:\\dir1 d:\\dir2");
			w.WriteLine("Example: DDiff patch1.msp patch2.msp /patchtarget target.msi");
            w.WriteLine();
            w.WriteLine("Options:");
            w.WriteLine("  /o [filename]     Output results to text file (UTF8)");
            w.WriteLine("  /p [package.msi]  Diff patches relative to target MSI");
        }

		public static int Main(string[] args)
		{
			if(args.Length < 2)
			{
                Usage(Console.Out);
				return -1;
			}

			string input1 = args[0];
			string input2 = args[1];
			string[] options = new string[args.Length - 2];
			for(int i = 0; i < options.Length; i++) options[i] = args[i+2];

			TextWriter output = Console.Out;

			for(int i = 0; i < options.Length - 1; i++)
			{
				switch(options[i].ToLower())
				{
					case "/o": goto case "-output";
					case "-o": goto case "-output";
					case "/output": goto case "-output";
					case "-output": output = new StreamWriter(options[i+1], false, Encoding.UTF8); break;
				}
			}

			IDiffEngineFactory diffFactory = new BestQualityDiffEngineFactory(new IDiffEngine[]
			{
				new DirectoryDiffEngine(),
				new FileDiffEngine(),
				new VersionedFileDiffEngine(),
				new TextFileDiffEngine(),
				new MsiDiffEngine(),
				new CabDiffEngine(),
				new MspDiffEngine(),
			});

			IDiffEngine diffEngine = diffFactory.GetDiffEngine(input1, input2, options);
			if(diffEngine != null)
			{
				bool different = diffEngine.GetDiff(input1, input2, options, output, "", diffFactory);
				return different ? 1 : 0;
			}
			else
			{
				Console.Error.WriteLine("Dont know how to diff those inputs.");
				return -1;
			}
		}
	}
}
