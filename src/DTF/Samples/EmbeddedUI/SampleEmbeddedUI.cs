//---------------------------------------------------------------------
// <copyright file="SampleEmbeddedUI.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>
// Sample embedded UI for the Deployment Tools Foundation project.
// </summary>
//---------------------------------------------------------------------

namespace Microsoft.Deployment.Samples.EmbeddedUI
{
    using System;
    using System.Collections.Generic;
    using System.Configuration;
    using System.Threading;
    using System.Windows;
    using System.Windows.Threading;
    using Microsoft.Deployment.WindowsInstaller;
    using Application = System.Windows.Application;

    public class SampleEmbeddedUI : IEmbeddedUI
    {
        private Thread appThread;
        private Application app;
        private SetupWizard setupWizard;
        private ManualResetEvent installStartEvent;
        private ManualResetEvent installExitEvent;

        /// <summary>
        /// Initializes the embedded UI.
        /// </summary>
        /// <param name="session">Handle to the installer which can be used to get and set properties.
        /// The handle is only valid for the duration of this method call.</param>
        /// <param name="resourcePath">Path to the directory that contains all the files from the MsiEmbeddedUI table.</param>
        /// <param name="internalUILevel">On entry, contains the current UI level for the installation. After this
        /// method returns, the installer resets the UI level to the returned value of this parameter.</param>
        /// <returns>True if the embedded UI was successfully initialized; false if the installation
        /// should continue without the embedded UI.</returns>
        /// <exception cref="InstallCanceledException">The installation was canceled by the user.</exception>
        /// <exception cref="InstallerException">The embedded UI failed to initialize and
        /// causes the installation to fail.</exception>
        public bool Initialize(Session session, string resourcePath, ref InstallUIOptions internalUILevel)
        {
            if (session != null)
            {
                if ((internalUILevel & InstallUIOptions.Full) != InstallUIOptions.Full)
                {
                    // Don't show custom UI when the UI level is set to basic.
                    return false;

                    // An embedded UI could display an alternate dialog sequence for reduced or
                    // basic modes, but it's not implemented here. We'll just fall back to the
                    // built-in MSI basic UI.
                }

                if (String.Equals(session["REMOVE"], "All", StringComparison.OrdinalIgnoreCase))
                {
                    // Don't show custom UI when uninstalling.
                    return false;

                    // An embedded UI could display an uninstall wizard, it's just not imlemented here.
                }
            }

            // Start the setup wizard on a separate thread.
            this.installStartEvent = new ManualResetEvent(false);
            this.installExitEvent = new ManualResetEvent(false);
            this.appThread = new Thread(this.Run);
            this.appThread.SetApartmentState(ApartmentState.STA);
            this.appThread.Start();

            // Wait for the setup wizard to either kickoff the install or prematurely exit.
            int waitResult = WaitHandle.WaitAny(new WaitHandle[] { this.installStartEvent, this.installExitEvent });
            if (waitResult == 1)
            {
                // The setup wizard set the exit event instead of the start event. Cancel the installation.
                throw new InstallCanceledException();
            }
            else
            {
                // Start the installation with a silenced internal UI.
                // This "embedded external UI" will handle message types except for source resolution.
                internalUILevel = InstallUIOptions.NoChange | InstallUIOptions.SourceResolutionOnly;
                return true;
            }
        }

        /// <summary>
        /// Processes information and progress messages sent to the user interface.
        /// </summary>
        /// <param name="messageType">Message type.</param>
        /// <param name="messageRecord">Record that contains message data.</param>
        /// <param name="buttons">Message box buttons.</param>
        /// <param name="icon">Message box icon.</param>
        /// <param name="defaultButton">Message box default button.</param>
        /// <returns>Result of processing the message.</returns>
        public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord,
            MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
        {
            // Synchronously send the message to the setup wizard window on its thread.
            object result = this.setupWizard.Dispatcher.Invoke(DispatcherPriority.Send,
                new Func<MessageResult>(delegate()
                {
                    return this.setupWizard.ProcessMessage(messageType, messageRecord, buttons, icon, defaultButton);
                }));
            return (MessageResult) result;
        }

        /// <summary>
        /// Shuts down the embedded UI at the end of the installation.
        /// </summary>
        /// <remarks>
        /// If the installation was canceled during initialization, this method will not be called.
        /// If the installation was canceled or failed at any later point, this method will be called at the end.
        /// </remarks>
        public void Shutdown()
        {
            // Wait for the user to exit the setup wizard.
            this.setupWizard.Dispatcher.BeginInvoke(DispatcherPriority.Normal,
                new Action(delegate()
                {
                    this.setupWizard.EnableExit();
                }));
            this.appThread.Join();
        }

        /// <summary>
        /// Creates the setup wizard and runs the application thread.
        /// </summary>
        private void Run()
        {
            this.app = new Application();
            this.setupWizard = new SetupWizard(this.installStartEvent);
            this.setupWizard.InitializeComponent();
            this.app.Run(this.setupWizard);
            this.installExitEvent.Set();
        }
    }
}
