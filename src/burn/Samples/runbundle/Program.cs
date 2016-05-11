// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Wix.Samples
{
    using System;
    using System.Linq;
    using Wix.Samples;

    /// <summary>
    /// Example executable that installs then immediately uninstalls a bundle showing progress.
    /// </summary>
    class Program
    {
        static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.WriteLine("Must provide the path to the bundle to install then uninstall.");
                return -1;
            }

            BundleRunner runner = new BundleRunner(args[0]);
            runner.Error += Program.OnError;
            runner.Progress += Program.OnProgress;

            Console.WriteLine("Installing: {0}", runner.Path);
            int exitCode = runner.Run(String.Join(" ", args.Skip(1).ToArray()));
            if (0 == exitCode)
            {
                Console.WriteLine("\r\nUninstalling: {0}", runner.Path);
                exitCode = runner.Run("-uninstall");
            }

            return exitCode;
        }

        static void OnError(object sender, BundleErrorEventArgs e)
        {
            Console.WriteLine("error: {0}, uiHint: {1}, message: {2}", e.Code, e.UIHint, e.Message);
        }

        static void OnProgress(object sender, BundleProgressEventArgs e)
        {
            Console.WriteLine("progresss: {0}%", e.Progress);
        }
    }
}
