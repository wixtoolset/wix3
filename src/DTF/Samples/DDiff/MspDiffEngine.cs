using System;
using System.IO;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Deployment.WindowsInstaller.Package;

namespace Microsoft.Deployment.Samples.DDiff
{
	public class MspDiffEngine : MsiDiffEngine
	{
		public MspDiffEngine()
		{
		}

		private string GetPatchTargetOption(string[] options)
		{
			for(int i = 0; i < options.Length - 1; i++)
			{
				switch(options[i].ToLower())
				{
					case "/p": goto case "-patchtarget";
					case "-p": goto case "-patchtarget";
					case "/patchtarget": goto case "-patchtarget";
					case "-patchtarget": return options[i+1];
				}
			}
			return null;
		}

		public override float GetDiffQuality(string diffInput1, string diffInput2, string[] options, IDiffEngineFactory diffFactory)
		{
			if(diffInput1 != null && File.Exists(diffInput1) &&
				diffInput2 != null && File.Exists(diffInput2) &&
				GetPatchTargetOption(options) != null &&
				(IsMspPatch(diffInput1) && IsMspPatch(diffInput2)))
			{
				return .80f;
			}
			else if(diffInput1 != null && File.Exists(diffInput1) &&
				diffInput2 != null && File.Exists(diffInput2) &&
				GetPatchTargetOption(options) == null &&
				(IsMspPatch(diffInput1) && IsMsiDatabase(diffInput2)) ||
				(IsMsiDatabase(diffInput1) && IsMspPatch(diffInput2)))
			{
				return .75f;
			}
			else
			{
				return 0;
			}
		}

		public override bool GetDiff(string diffInput1, string diffInput2, string[] options, TextWriter diffOutput, string linePrefix, IDiffEngineFactory diffFactory)
		{
			bool difference = false;

			InstallPackage db1, db2;
			if(IsMspPatch(diffInput1))
			{
				string patchTargetDbFile = GetPatchTargetOption(options);
				if(patchTargetDbFile == null) patchTargetDbFile = diffInput2;
				string tempPatchedDbFile = Path.GetTempFileName();
				File.Copy(patchTargetDbFile, tempPatchedDbFile, true);
				File.SetAttributes(tempPatchedDbFile, File.GetAttributes(tempPatchedDbFile) & ~System.IO.FileAttributes.ReadOnly);
				db1 = new InstallPackage(tempPatchedDbFile, DatabaseOpenMode.Direct);
				db1.ApplyPatch(new PatchPackage(diffInput1), null);
				db1.Commit();
			}
			else
			{
				db1 = new InstallPackage(diffInput1, DatabaseOpenMode.ReadOnly);
			}
			if(IsMspPatch(diffInput2))
			{
				string patchTargetDbFile = GetPatchTargetOption(options);
				if(patchTargetDbFile == null) patchTargetDbFile = diffInput1;
				string tempPatchedDbFile = Path.GetTempFileName();
				File.Copy(patchTargetDbFile, tempPatchedDbFile, true);
				File.SetAttributes(tempPatchedDbFile, File.GetAttributes(tempPatchedDbFile) & ~System.IO.FileAttributes.ReadOnly);
				db2 = new InstallPackage(tempPatchedDbFile, DatabaseOpenMode.Direct);
				db2.ApplyPatch(new PatchPackage(diffInput2), null);
				db2.Commit();
			}
			else
			{
				db2 = new InstallPackage(diffInput2, DatabaseOpenMode.ReadOnly);
			}

			if(GetSummaryInfoDiff(db1, db2, options, diffOutput, linePrefix, diffFactory)) difference = true;
			if(GetDatabaseDiff(db1, db2, options, diffOutput, linePrefix, diffFactory)) difference = true;
			if(GetStreamsDiff(db1, db2, options, diffOutput, linePrefix, diffFactory)) difference = true;

			db1.Close();
			db2.Close();

			try
			{
				if(IsMspPatch(diffInput1)) File.Delete(db1.FilePath);
				if(IsMspPatch(diffInput1)) File.Delete(db2.FilePath);
			}
			catch(IOException)
			{
				#if DEBUG
				Console.WriteLine("Could not delete temporary files {0} and {1}", db1.FilePath, db2.FilePath);
				#endif
			}

			if(IsMspPatch(diffInput1) && IsMspPatch(diffInput2))
			{
				Database dbp1 = new Database(diffInput1, DatabaseOpenMode.ReadOnly);
				Database dbp2 = new Database(diffInput2, DatabaseOpenMode.ReadOnly);

				if(GetStreamsDiff(dbp1, dbp2, options, diffOutput, linePrefix, diffFactory)) difference = true;
				dbp1.Close();
				dbp2.Close();
			}

			return difference;
		}

		public override IDiffEngine Clone()
		{
			return new MspDiffEngine();
		}
	}
}
