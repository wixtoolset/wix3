// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.UX
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Windows.Input;
    using Threading = System.Windows.Threading;
    using WinForms = System.Windows.Forms;

    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

    /// <summary>
    /// The WiX toolset user experience.
    /// </summary>
    public class WixBA : BootstrapperApplication
    {
        /// <summary>
        /// Gets the global model.
        /// </summary>
        static public Model Model { get; private set; }

        /// <summary>
        /// Gets the global view.
        /// </summary>
        static public RootView View { get; private set; }
        // TODO: We should refactor things so we dont have a global View.

        /// <summary>
        /// Gets the global dispatcher.
        /// </summary>
        static public Threading.Dispatcher Dispatcher { get; private set; }

        /// <summary>
        /// Launches the default web browser to the provided URI.
        /// </summary>
        /// <param name="uri">URI to open the web browser.</param>
        public static void LaunchUrl(string uri)
        {
            // Switch the wait cursor since shellexec can take a second or so.
            Cursor cursor = WixBA.View.Cursor;
            WixBA.View.Cursor = Cursors.Wait;

            try
            {
                Process process = new Process();
                process.StartInfo.FileName = uri;
                process.StartInfo.UseShellExecute = true;
                process.StartInfo.Verb = "open";

                process.Start();
            }
            finally
            {
                WixBA.View.Cursor = cursor; // back to the original cursor.
            }
        }

        /// <summary>
        /// Starts planning the appropriate action.
        /// </summary>
        /// <param name="action">Action to plan.</param>
        public static void Plan(LaunchAction action)
        {
            WixBA.Model.PlannedAction = action;
            WixBA.Model.Engine.Plan(WixBA.Model.PlannedAction);
        }

        public static void PlanLayout()
        {
            // Either default or set the layout directory
            if (String.IsNullOrEmpty(WixBA.Model.Command.LayoutDirectory))
            {
                WixBA.Model.LayoutDirectory = Directory.GetCurrentDirectory();

                // Ask the user for layout folder if one wasn't provided and we're in full UI mode
                if (WixBA.Model.Command.Display == Display.Full)
                {
                    WixBA.Dispatcher.Invoke((Action)delegate()
                    {
                        WinForms.FolderBrowserDialog browserDialog = new WinForms.FolderBrowserDialog();
                        browserDialog.RootFolder = Environment.SpecialFolder.MyComputer;

                        // Default to the current directory.
                        browserDialog.SelectedPath = WixBA.Model.LayoutDirectory;
                        WinForms.DialogResult result = browserDialog.ShowDialog();

                        if (WinForms.DialogResult.OK == result)
                        {
                            WixBA.Model.LayoutDirectory = browserDialog.SelectedPath;
                            WixBA.Plan(WixBA.Model.Command.Action);
                        }
                        else
                        {
                            WixBA.View.Close();
                        }
                    }
                    );
                }
            }
            else
            {
                WixBA.Model.LayoutDirectory = WixBA.Model.Command.LayoutDirectory;
                WixBA.Plan(WixBA.Model.Command.Action);
            }
        }

        /// <summary>
        /// Thread entry point for WiX Toolset UX.
        /// </summary>
        protected override void Run()
        {
            this.Engine.Log(LogLevel.Verbose, "Running the WiX BA.");
            WixBA.Model = new Model(this);
            WixBA.Dispatcher = Threading.Dispatcher.CurrentDispatcher;
            RootViewModel viewModel = new RootViewModel();

            // Kick off detect which will populate the view models.
            this.Engine.Detect();

            // Create a Window to show UI.
            if (WixBA.Model.Command.Display == Display.Passive ||
                WixBA.Model.Command.Display == Display.Full)
            {
                this.Engine.Log(LogLevel.Verbose, "Creating a UI.");
                WixBA.View = new RootView(viewModel);
                WixBA.View.Show();
            }

            Threading.Dispatcher.Run();

            this.PostTelemetry();
            this.Engine.Quit(WixBA.Model.Result);
        }

        private void PostTelemetry()
        {
            string result = String.Concat("0x", WixBA.Model.Result.ToString("x"));

            StringBuilder telemetryData = new StringBuilder();
            foreach (KeyValuePair<string, string> kvp in WixBA.Model.Telemetry)
            {
                telemetryData.AppendFormat("{0}={1}+", kvp.Key, kvp.Value);
            }
            telemetryData.AppendFormat("Result={0}", result);

            byte[] data = Encoding.UTF8.GetBytes(telemetryData.ToString());

            try
            {
                HttpWebRequest post = WixBA.Model.CreateWebRequest(String.Format(WixDistribution.TelemetryUrlFormat, WixBA.Model.Version.ToString(), result));
                post.Method = "POST";
                post.ContentType = "application/x-www-form-urlencoded";
                post.ContentLength = data.Length;

                using (Stream postStream = post.GetRequestStream())
                {
                    postStream.Write(data, 0, data.Length);
                }

                HttpWebResponse response = (HttpWebResponse)post.GetResponse();
            }
            catch (ArgumentException)
            {
            }
            catch (FormatException)
            {
            }
            catch (OverflowException)
            {
            }
            catch (ProtocolViolationException)
            {
            }
            catch (WebException)
            {
            }
        }
    }
}
