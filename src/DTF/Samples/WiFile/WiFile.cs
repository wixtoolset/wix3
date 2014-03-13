using System;
using System.IO;
using System.Reflection;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;
using Microsoft.Deployment.WindowsInstaller;
using Microsoft.Deployment.WindowsInstaller.Package;

[assembly: AssemblyDescription("Windows Installer package file extraction and update tool")]


/// <summary>
/// Shows sample use of the InstallPackage class.
/// </summary>
public class WiFile
{
	public static void Usage(TextWriter w)
	{
		w.WriteLine("Usage: WiFile.exe package.msi /l [filename,filename2,...]");
		w.WriteLine("Usage: WiFile.exe package.msi /x [filename,filename2,...]");
		w.WriteLine("Usage: WiFile.exe package.msi /u [filename,filename2,...]");
		w.WriteLine();
		w.WriteLine("Lists (/l), extracts (/x) or updates (/u) files in an MSI or MSM.");
		w.WriteLine("Files are extracted using their source path relative to the package.");
		w.WriteLine("Specified filenames do not include paths.");
		w.WriteLine("Filenames may be a pattern such as *.exe or file?.dll");
	}

    [SuppressMessage("Microsoft.Globalization", "CA1308:NormalizeStringsToUppercase")]
	public static int Main(string[] args)
	{
		if(!(args.Length == 2 || args.Length == 3))
		{
			Usage(Console.Out);
			return -1;
		}

		string msiFile = args[0];

		string option = args[1].ToLowerInvariant();
		if(option.StartsWith("-", StringComparison.Ordinal)) option = "/" + option.Substring(1);

		string[] fileNames = null;
		if(args.Length == 3)
		{
			fileNames = args[2].Split(',');
		}

		try
		{
			switch(option)
			{
				case "/l":
					using(InstallPackage pkg = new InstallPackage(msiFile, DatabaseOpenMode.ReadOnly))
					{
						pkg.Message += new InstallPackageMessageHandler(Console.WriteLine);
						IEnumerable<string> fileKeys = (fileNames != null ? FindFileKeys(pkg, fileNames) : pkg.Files.Keys);

						foreach(string fileKey in fileKeys)
						{
							Console.WriteLine(pkg.Files[fileKey]);
						}
					}
					break;

				case "/x":
					using(InstallPackage pkg = new InstallPackage(msiFile, DatabaseOpenMode.ReadOnly))
					{
						pkg.Message += new InstallPackageMessageHandler(Console.WriteLine);
						ICollection<string> fileKeys = FindFileKeys(pkg, fileNames);

						pkg.ExtractFiles(fileKeys);
					}
					break;

				case "/u":
					using(InstallPackage pkg = new InstallPackage(msiFile, DatabaseOpenMode.Transact))
					{
						pkg.Message += new InstallPackageMessageHandler(Console.WriteLine);
                        ICollection<string> fileKeys = FindFileKeys(pkg, fileNames);

						pkg.UpdateFiles(fileKeys);
						pkg.Commit();
					}
					break;

				default:
                    Usage(Console.Out);
					return -1;
			}
		}
		catch(InstallerException iex)
		{
			Console.WriteLine("Error: " + iex.Message);
			return iex.ErrorCode != 0 ? iex.ErrorCode : 1;
		}
		catch(FileNotFoundException fnfex)
		{
			Console.WriteLine(fnfex.Message);
			return 2;
		}
		catch(Exception ex)
		{
			Console.WriteLine("Error: " + ex.Message);
			return 1;
		}
		return 0;
	}

	static ICollection<string> FindFileKeys(InstallPackage pkg, ICollection<string> fileNames)
	{
		List<string> fileKeys = null;
		if(fileNames != null)
		{
			fileKeys = new List<string>();
			foreach(string fileName in fileNames)
			{
				string[] foundFileKeys = null;
				if(fileName.IndexOfAny(new char[] { '*', '?' }) >= 0)
				{
					foundFileKeys = pkg.FindFiles(FilePatternToRegex(fileName));
				}
				else
				{
					foundFileKeys = pkg.FindFiles(fileName);
				}
				fileKeys.AddRange(foundFileKeys);
			}
			if(fileKeys.Count == 0)
			{
				throw new FileNotFoundException("Files not found in package.");
			}
		}
		return fileKeys;
	}

	static Regex FilePatternToRegex(string pattern)
	{
		return new Regex("^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$",
			RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);
	}
}
