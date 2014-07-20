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
        private UpdateState state;
        private ICommand updateCommand;
        

        public UpdateViewModel(RootViewModel root)
        {
            this.root = root;
            WixBA.Model.Bootstrapper.DetectUpdateBegin += this.DetectUpdateBegin;
            WixBA.Model.Bootstrapper.DetectUpdate += this.DetectUpdate;
            WixBA.Model.Bootstrapper.DetectUpdateComplete += this.DetectUpdateComplete;

            this.State = UpdateState.Initializing;

        }

        public bool CheckingEnabled
        {
            get { return this.State == UpdateState.Initializing || this.State == UpdateState.Checking || this.State == UpdateState.Unknown; }
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
            if (UpdateState.Failed != this.State)
            {
                this.State = UpdateState.Checking;
                e.Result = Result.Ok;
            }
        }
        
        private void DetectUpdate(object sender, Bootstrapper.DetectUpdateEventArgs e)
        {
            // The list of updates is sorted in descending version, so the first callback should be the largest update available.
            // This update should be either larger than ours (so we are out of date), the same as ours (so we are current)
            // or smaller than ours (we have a private build). If we really wanted to, we could leave the e.Result alone and
            // enumerate all of the updates.
            if (e.Version > WixBA.Model.Version)
            {
                WixBA.Model.Engine.SetUpdate(null, e.UpdateLocation, e.Size, UpdateHashType.None, null);
                this.State = UpdateState.Available;
                e.Result = Result.Ok;
            }
            else if (e.Version <= WixBA.Model.Version)
            {
                this.State = UpdateState.Current;
                e.Result = Result.Cancel;
            }

        }

        private void DetectUpdateComplete(object sender, Bootstrapper.DetectUpdateCompleteEventArgs e)
        {
            if (((int) Result.Cancel != e.Status) && ((int) Result.Ok != e.Status))
            {
                this.State = UpdateState.Failed;
                WixBA.Model.Engine.Detect();
            } else if (UpdateState.Checking == this.State) 
            {
                this.State = UpdateState.Current;
            }
        }
    }
}
