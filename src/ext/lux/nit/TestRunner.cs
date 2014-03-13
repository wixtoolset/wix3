//-------------------------------------------------------------------------------------------------
// <copyright file="TestRunner.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// External UI handler that monitors messages sent by WixRunImmediateUnitTests to report test
// progress and status.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Lux
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.ComponentModel;
    using System.IO;
    using System.Xml;
    using Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// External UI handler that monitors messages sent by WixRunImmediateUnitTests to report test
    /// progress and status.
    /// </summary>
    public class TestRunner : IMessageHandler
    {
        private bool runningTests;
        private int passes;
        private int failures;
        private Session session;

        /// <summary>
        /// Event for messages.
        /// </summary>
        public event MessageEventHandler Message;

        /// <summary>
        /// Gets or sets the list of test packages to run.
        /// </summary>
        public List<string> InputFiles
        {
            get;
            set;
        }

        /// <summary>
        /// Sets up an external UI handler and runs the package.
        /// </summary>
        /// <param name="passes">Number of passing tests.</param>
        /// <param name="failures">Number of failing tests.</param>
        public void RunTests(out int passes, out int failures)
        {
            InstallUIOptions previousUILevel = Installer.SetInternalUI(InstallUIOptions.Silent);
            ExternalUIRecordHandler previousUIHandler = Installer.SetExternalUI(this.UIRecordHandler, InstallLogModes.FatalExit | InstallLogModes.Error | /*InstallLogModes.Info | */ InstallLogModes.User | InstallLogModes.ActionStart);

            try
            {
                foreach (string package in this.InputFiles)
                {
                    using (this.session = Installer.OpenPackage(package, false))
                    {
                        IList<string> mutations = this.session.Database.ExecuteStringQuery("SELECT DISTINCT `Mutation` FROM `WixUnitTest`");
                        foreach (string mutation in mutations)
                        {
                            if (!String.IsNullOrEmpty(mutation))
                            {
                                this.OnMessage(NitVerboses.RunningMutation(mutation));
                                this.session[Constants.LuxMutationRunningProperty] = mutation;
                            }

                            try
                            {
                                this.session.DoAction("INSTALL");
                            }
                            catch (InstallCanceledException)
                            {
                                ; // expected
                            }
                            catch (InstallerException ex)
                            {
                                this.OnMessage(NitErrors.PackageFailed(ex.Message));
                                ++this.failures;
                            }
                        }
                    }
                }
            }
            finally
            {
                Installer.SetExternalUI(previousUIHandler, InstallLogModes.None);
                Installer.SetInternalUI(previousUILevel);
            }

            passes = this.passes;
            failures = this.failures;
        }

        /// <summary>
        /// Handler for external UI messages.
        /// </summary>
        /// <param name="messageType">The type of message.</param>
        /// <param name="messageRecord">The message details.</param>
        /// <param name="buttons">Buttons to show (unused).</param>
        /// <param name="icon">The icon to show (unused).</param>
        /// <param name="defaultButton">The default button (unused).</param>
        /// <returns>How the message was handled.</returns>
        public MessageResult UIRecordHandler(
            InstallMessage messageType,
            Record messageRecord,
            MessageButtons buttons,
            MessageIcon icon,
            MessageDefaultButton defaultButton)
        {
#if False
            Console.WriteLine("Message type {0}: {1}", messageType.ToString(), this.session.FormatRecord(messageRecord));
#endif

            if (!this.session.IsClosed && 1 <= messageRecord.FieldCount)
            {
                switch (messageType)
                {
                    case InstallMessage.ActionStart:
                        // only try to interpret the messages if they're coming from WixRunImmediateUnitTests
                        string action = messageRecord.GetString(1);
                        this.runningTests = Constants.LuxCustomActionName == action;
                        return MessageResult.OK;

                    case InstallMessage.User:
                        if (this.runningTests)
                        {
                            string message = messageRecord.ToString();
                            int id = messageRecord.GetInteger(1);

                            if (Constants.TestIdMinimumSuccess <= id && Constants.TestIdMaximumSuccess >= id)
                            {
                                this.OnMessage(NitVerboses.TestPassed(message));
                                ++this.passes;
                            }
                            else if (Constants.TestIdMinimumFailure <= id && Constants.TestIdMaximumFailure >= id)
                            {
                                this.OnMessage(NitErrors.TestFailed(message));
                                ++this.failures;
                            }
                        }

                        return MessageResult.OK;

                    case InstallMessage.Error:
                    case InstallMessage.FatalExit:
                        this.OnMessage(NitErrors.PackageFailed(this.session.FormatRecord(messageRecord)));
                        return MessageResult.Error;
                }
            }

            return MessageResult.OK;
        }

        /// <summary>
        /// Sends a message to the message delegate if there is one.
        /// </summary>
        /// <param name="mea">Message event arguments.</param>
        public void OnMessage(MessageEventArgs mea)
        {
            WixErrorEventArgs errorEventArgs = mea as WixErrorEventArgs;

            if (null != this.Message)
            {
                this.Message(this, mea);
            }
            else if (null != errorEventArgs)
            {
                throw new WixException(errorEventArgs);
            }
        }
    }
}
