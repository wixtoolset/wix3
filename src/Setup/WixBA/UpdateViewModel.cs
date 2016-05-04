// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

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
            // Don't check for updates if:
            //   the first check failed (no retry)
            //   if we are being run as an uninstall
            //   if we are not under a full UI.
            if ((UpdateState.Failed != this.State) && (LaunchAction.Uninstall != WixBA.Model.Command.Action) && (Display.Full == WixBA.Model.Command.Display))
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
            WixBA.Model.Engine.Log(LogLevel.Verbose, String.Format("Potential update v{0} from '{1}'; current version: v{2}", e.Version, e.UpdateLocation, WixBA.Model.Version));
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
            // Failed to process an update, re-queue a detect to allow the existing bundle to still install.
            if ((UpdateState.Failed != this.State) && !Hresult.Succeeded(e.Status))
            {
                this.State = UpdateState.Failed;
                WixBA.Model.Engine.Log(LogLevel.Verbose, String.Format("Failed to locate an update, status of 0x{0:X8}. Re-detecting with updates disabled.", e.Status));
                WixBA.Model.Engine.Detect();
            }
            // If we are uninstalling, we don't want to check or show an update
            // If we are checking, then the feed didn't find any valid enclosures
            // If we are initializing, we're either uninstalling or not a full UI
            else if ((LaunchAction.Uninstall == WixBA.Model.Command.Action) || (UpdateState.Initializing == this.State) || (UpdateState.Checking == this.State))
            {
                this.State = UpdateState.Unknown;
            }            
        }
    }
}
