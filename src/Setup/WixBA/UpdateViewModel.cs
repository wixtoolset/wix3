//-------------------------------------------------------------------------------------------------
// <copyright file="UpdateViewModel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The model of the update view.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.UX
{
    using System;
    using System.ComponentModel;
    using System.Linq;
    using System.Net;
    using System.ServiceModel.Syndication;
    using System.Windows.Input;
    using System.Xml;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

    /// <summary>
    /// The states of the update view model.
    /// </summary>
    public enum UpdateState
    {
        Unknown,
        Initializing,
        Checking,
        Current,
        Available,
        Failed,
    }

    /// <summary>
    /// The model of the update view.
    /// </summary>
    public class UpdateViewModel : PropertyNotifyBase
    {
        private RootViewModel root;

        private readonly string AppSyndicationNamespace = "http://appsyndication.org/2006/appsyn";

        private UpdateState state;
        private BackgroundWorker worker;

        private ICommand updateCommand;

        public UpdateViewModel(RootViewModel root)
        {
            this.root = root;
            WixBA.Model.Bootstrapper.DetectUpdateBegin += this.DetectUpdateBegin;
            WixBA.Model.Bootstrapper.DetectComplete += DetectComplete;

            this.State = UpdateState.Initializing;

            this.worker = new BackgroundWorker();
            this.worker.DoWork += new DoWorkEventHandler(worker_DoWork);
        }

        public bool CheckingEnabled
        {
            get { return this.State == UpdateState.Initializing || this.State == UpdateState.Checking; }
        }

        public bool IsUpToDate
        {
            get { return this.State == UpdateState.Current; }
        }

        public ICommand UpdateCommand
        {
            get
            {
                if (this.updateCommand == null)
                {
                    this.updateCommand = new RelayCommand(param => WixBA.Plan(LaunchAction.UpdateReplace), param => this.State == UpdateState.Available);
                }

                return this.updateCommand;
            }
        }

        public bool UpdateEnabled
        {
            get { return this.UpdateCommand.CanExecute(this); }
        }

        /// <summary>
        /// Gets and sets the state of the update view model.
        /// </summary>
        public UpdateState State
        {
            get
            {
                return this.state;
            }

            set
            {
                if (this.state != value)
                {
                    this.state = value;
                    base.OnPropertyChanged("State");
                    base.OnPropertyChanged("Title");
                    base.OnPropertyChanged("CheckingEnabled");
                    base.OnPropertyChanged("IsUpToDate");
                    base.OnPropertyChanged("UpdateEnabled");
                }
            }
        }

        /// <summary>
        /// Gets and sets the title of the update view model.
        /// </summary>
        public string Title
        {
            get
            {
                switch (this.state)
                {
                    case UpdateState.Initializing:
                        return "Initializing update detection...";

                    case UpdateState.Checking:
                        return "Checking for updates...";

                    case UpdateState.Current:
                        return "Up to date";

                    case UpdateState.Available:
                        return "Newer version available";

                    case UpdateState.Failed:
                        return "Failed to check for updates";

                    case UpdateState.Unknown:
                        return "Check for updates.";

                    default:
                        return "Unexpected state";
                }
            }
        }

        private void DetectUpdateBegin(object sender, Bootstrapper.DetectUpdateBeginEventArgs e)
        {
            this.State = UpdateState.Checking;

            this.worker.RunWorkerAsync(e.UpdateLocation);
        }

        private void DetectComplete(object sender, Bootstrapper.DetectCompleteEventArgs e)
        {
            // If we never started checking, assume we're up to date.
            if (UpdateState.Initializing == this.State)
            {
                this.State = UpdateState.Current;
            }
        }

        /// <summary>
        /// Worker thread to check for updates.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Arguments.</param>
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            bool succeeded = false;
            string updateFeedUrl = (string)e.Argument;

            try
            {
                HttpWebRequest request = WixBA.Model.CreateWebRequest(updateFeedUrl);
                HttpWebResponse response = (HttpWebResponse)request.GetResponse();

                if (response.StatusCode == HttpStatusCode.OK)
                {
                    SyndicationFeed feed;
                    using (XmlReader reader = XmlReader.Create(response.GetResponseStream()))
                    {
                        feed = SyndicationFeed.Load(reader);
                    }

                    var updates = from entry in feed.Items
                                  from link in entry.Links
                                  from extension in entry.ElementExtensions
                                  where String.Equals(link.RelationshipType, "enclosure", StringComparison.Ordinal) &&
                                        String.Equals(extension.OuterNamespace, this.AppSyndicationNamespace, StringComparison.Ordinal) &&
                                        String.Equals(extension.OuterName, "version", StringComparison.Ordinal)
                                  select new Update()
                                  {
                                      Url = link.Uri.AbsoluteUri,
                                      Size = link.Length,
                                      Version = new Version(extension.GetObject<string>())
                                  };

                    Update update = updates.Where(u => u.Version > WixBA.Model.Version).OrderByDescending(u => u.Version).FirstOrDefault();
                    if (update == null)
                    {
                        WixBA.Model.Engine.SetUpdate(null, null, 0, UpdateHashType.None, null);
                        this.State = UpdateState.Current;
                    }
                    else
                    {
                        WixBA.Model.Engine.SetUpdate(null, update.Url, update.Size, UpdateHashType.None, null);
                        this.State = UpdateState.Available;
                    }

                    succeeded = true;
                }
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
            catch (WebException)
            {
            }
            catch (XmlException)
            {
            }

            if (!succeeded)
            {
                WixBA.Model.Engine.SetUpdate(null, null, 0, UpdateHashType.None, null);
                this.State = UpdateState.Failed;
            }
        }

        /// <summary>
        /// Helper class to store AppSyndication URLs associated with their version.
        /// </summary>
        private class Update
        {
            public string Url { get; set; }
            public long Size { get; set; }
            public Version Version { get; set; }
        }
    }
}
