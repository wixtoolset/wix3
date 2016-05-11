// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.CodeDom.Compiler;
    using System.ComponentModel;
    using System.Runtime.InteropServices;

    [ComVisible(true)]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    [Guid("2965A12F-AC7B-43A0-85DF-E4B2168478A4")]
    [GeneratedCodeAttribute("Microsoft.Tools.WindowsInstallerXml.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public interface IBootstrapperApplicationFactory
    {
        IBootstrapperApplication Create(
            [MarshalAs(UnmanagedType.Interface)] IBootstrapperEngine pEngine,
            ref Command command
            );
    }

    /// <summary>
    /// Command information passed from the engine for the user experience to perform.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    [GeneratedCodeAttribute("Microsoft.Tools.WindowsInstallerXml.Bootstrapper.InteropCodeGenerator", "1.0.0.0")]
    public struct Command
    {
        [MarshalAs(UnmanagedType.U4)] private readonly LaunchAction action;
        [MarshalAs(UnmanagedType.U4)] private readonly Display display;
        [MarshalAs(UnmanagedType.U4)] private readonly Restart restart;
        [MarshalAs(UnmanagedType.LPWStr)] private readonly string wzCommandLine;
        [MarshalAs(UnmanagedType.I4)] private readonly int nCmdShow;
        [MarshalAs(UnmanagedType.U4)] private readonly ResumeType resume;
        private readonly IntPtr hwndSplashScreen;
        [MarshalAs(UnmanagedType.I4)] private readonly RelationType relation;
        [MarshalAs(UnmanagedType.Bool)] private readonly bool passthrough;
        [MarshalAs(UnmanagedType.LPWStr)] private readonly string wzLayoutDirectory;

        /// <summary>
        /// Gets the action for the user experience to perform.
        /// </summary>
        public LaunchAction Action
        {
            get { return this.action; }
        }

        /// <summary>
        /// Gets the display level for the user experience.
        /// </summary>
        public Display Display
        {
            get { return this.display; }
        }

        /// <summary>
        /// Gets the action to perform if a reboot is required.
        /// </summary>
        public Restart Restart
        {
            get { return this.restart; }
        }

        /// <summary>
        /// Gets command line arguments that weren't processed by the engine. Can be null.
        /// </summary>
        [Obsolete("Use GetCommandLineArgs instead.")]
        public string CommandLine
        {
            get { return this.wzCommandLine; }
        }

        /// <summary>
        /// Gets layout directory.
        /// </summary>
        public string LayoutDirectory
        {
            get { return this.wzLayoutDirectory; }
        }

        /// <summary>
        /// Gets the method of how the engine was resumed from a previous installation step.
        /// </summary>
        public ResumeType Resume
        {
            get { return this.resume; }
        }

        /// <summary>
        /// Gets the handle to the splash screen window. If no splash screen was displayed this value will be IntPtr.Zero.
        /// </summary>
        public IntPtr SplashScreen
        {
            get { return this.hwndSplashScreen; }
        }

        /// <summary>
        /// If this was run from a related bundle, specifies the relation type.
        /// </summary>
        public RelationType Relation
        {
            get { return this.relation; }
        }

        /// <summary>
        /// If this was run from a backward compatible bundle.
        /// </summary>
        public bool Passthrough
        {
            get { return this.passthrough; }
        }

        /// <summary>
        /// Gets the command line arguments as a string array.
        /// </summary>
        /// <returns>
        /// Array of command line arguments not handled by the engine.
        /// </returns>
        /// <exception type="Win32Exception">The command line could not be parsed into an array.</exception>
        /// <remarks>
        /// This method uses the same parsing as the operating system which handles quotes and spaces correctly.
        /// </remarks>
        public string[] GetCommandLineArgs()
        {
            if (null == this.wzCommandLine)
            {
                return new string[0];
            }

            // Parse the filtered command line arguments into a native array.
            int argc = 0;

            // CommandLineToArgvW tries to treat the first argument as the path to the process,
            // which fails pretty miserably if your first argument is something like
            // FOO="C:\Program Files\My Company". So give it something harmless to play with.
            IntPtr argv = NativeMethods.CommandLineToArgvW("ignored " + this.wzCommandLine, out argc);

            if (IntPtr.Zero == argv)
            {
                // Throw an exception with the last error.
                throw new Win32Exception();
            }

            // Marshal each native array pointer to a managed string.
            try
            {
                // Skip "ignored" argument/hack.
                string[] args = new string[argc - 1];
                for (int i = 1; i < argc; ++i)
                {
                    IntPtr argvi = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i - 1] = Marshal.PtrToStringUni(argvi);
                }

                return args;
            }
            finally
            {
                NativeMethods.LocalFree(argv);
            }
        }
    }
}
