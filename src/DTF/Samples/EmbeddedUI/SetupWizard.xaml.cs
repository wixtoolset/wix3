//---------------------------------------------------------------------
// <copyright file="SetupWizard.xaml.cs" company="Outercurve Foundation">
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
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Data;
    using System.Windows.Documents;
    using System.Windows.Input;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using System.Windows.Navigation;
    using System.Windows.Shapes;
    using Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// Interaction logic for SetupWizard.xaml
    /// </summary>
    public partial class SetupWizard : Window
    {
        private ManualResetEvent installStartEvent;
        private InstallProgressCounter progressCounter;
        private bool canceled;

        public SetupWizard(ManualResetEvent installStartEvent)
        {
            this.installStartEvent = installStartEvent;
            this.progressCounter = new InstallProgressCounter(0.5);
        }

        public MessageResult ProcessMessage(InstallMessage messageType, Record messageRecord,
            MessageButtons buttons, MessageIcon icon, MessageDefaultButton defaultButton)
        {
            try
            {
                this.progressCounter.ProcessMessage(messageType, messageRecord);
                this.progressBar.Value = this.progressBar.Minimum +
                    this.progressCounter.Progress * (this.progressBar.Maximum - this.progressBar.Minimum);
                this.progressLabel.Content = "" + (int) Math.Round(100 * this.progressCounter.Progress) + "%";

                switch (messageType)
                {
                    case InstallMessage.Error:
                    case InstallMessage.Warning:
                    case InstallMessage.Info:
                        string message = String.Format("{0}: {1}", messageType, messageRecord);
                        this.LogMessage(message);
                        break;
                }

                if (this.canceled)
                {
                    this.canceled = false;
                    return MessageResult.Cancel;
                }
            }
            catch (Exception ex)
            {
                this.LogMessage(ex.ToString());
                this.LogMessage(ex.StackTrace);
            }

            return MessageResult.OK;
        }

        private void LogMessage(string message)
        {
            this.messagesTextBox.Text += Environment.NewLine + message;
            this.messagesTextBox.ScrollToEnd();
        }

        internal void EnableExit()
        {
            this.progressBar.Visibility = Visibility.Hidden;
            this.progressLabel.Visibility = Visibility.Hidden;
            this.cancelButton.Visibility = Visibility.Hidden;
            this.exitButton.Visibility = Visibility.Visible;
        }

        private void installButton_Click(object sender, RoutedEventArgs e)
        {
            this.installButton.Visibility = Visibility.Hidden;
            this.progressBar.Visibility = Visibility.Visible;
            this.progressLabel.Visibility = Visibility.Visible;
            this.installStartEvent.Set();
        }

        private void exitButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void cancelButton_Click(object sender, RoutedEventArgs e)
        {
            if (this.installButton.Visibility == Visibility.Visible)
            {
                this.Close();
            }
            else
            {
                this.canceled = true;
                this.cancelButton.IsEnabled = false;
            }
        }
    }
}
