//-------------------------------------------------------------------------------------------------
// <copyright file="ProgressViewModel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

namespace Microsoft.Tools.WindowsInstallerXml.UX
{
    public class ProgressViewModel : PropertyNotifyBase
    {
        private RootViewModel root;
        private Dictionary<string, int> executingPackageOrderIndex;

        private int progressPhases;
        private int progress;
        private int cacheProgress;
        private int executeProgress;
        private string message;

        public ProgressViewModel(RootViewModel root)
        {
            this.root = root;
            this.executingPackageOrderIndex = new Dictionary<string, int>();

            this.root.PropertyChanged += this.RootPropertyChanged;

            WixBA.Model.Bootstrapper.ExecuteMsiMessage += this.ExecuteMsiMessage;
            WixBA.Model.Bootstrapper.ExecuteProgress += this.ApplyExecuteProgress;
            WixBA.Model.Bootstrapper.PlanBegin += this.PlanBegin;
            WixBA.Model.Bootstrapper.PlanPackageComplete += this.PlanPackageComplete;
            WixBA.Model.Bootstrapper.Progress += this.ApplyProgress;
            WixBA.Model.Bootstrapper.CacheAcquireProgress += this.CacheAcquireProgress;
            WixBA.Model.Bootstrapper.CacheComplete += this.CacheComplete;
        }

        public bool ProgressEnabled
        {
            get { return this.root.State == InstallationState.Applying; }
        }

        public int Progress
        {
            get
            {
                return this.progress;
            }

            set
            {
                if (this.progress != value)
                {
                    this.progress = value;
                    base.OnPropertyChanged("Progress");
                }
            }
        }

        public string Message
        {
            get
            {
                return this.message;
            }

            set
            {
                if (this.message != value)
                {
                    this.message = value;
                    base.OnPropertyChanged("Message");
                }
            }
        }

        void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ("State" == e.PropertyName)
            {
                base.OnPropertyChanged("ProgressEnabled");
            }
        }

        private void PlanBegin(object sender, PlanBeginEventArgs e)
        {
            lock (this)
            {
                this.progressPhases = (LaunchAction.Layout == WixBA.Model.PlannedAction) ? 1 : 2;
                this.executingPackageOrderIndex.Clear();
            }
        }

        private void PlanPackageComplete(object sender, PlanPackageCompleteEventArgs e)
        {
            if (ActionState.None != e.Execute)
            {
                lock (this)
                {
                    Debug.Assert(!this.executingPackageOrderIndex.ContainsKey(e.PackageId));
                    this.executingPackageOrderIndex.Add(e.PackageId, this.executingPackageOrderIndex.Count);
                }
            }
        }

        private void ExecuteMsiMessage(object sender, ExecuteMsiMessageEventArgs e)
        {
            lock (this)
            {
                if (e.MessageType == InstallMessage.ActionStart)
                {
                    this.Message = e.Message;
                }

                e.Result = this.root.Canceled ? Result.Cancel : Result.Ok;
            }
        }

        private void ApplyProgress(object sender, ProgressEventArgs e)
        {
            lock (this)
            {
                e.Result = this.root.Canceled ? Result.Cancel : Result.Ok;
            }
        }

        private void CacheAcquireProgress(object sender, CacheAcquireProgressEventArgs e)
        {
            lock (this)
            {
                this.cacheProgress = e.OverallPercentage;
                this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;
                e.Result = this.root.Canceled ? Result.Cancel : Result.Ok;
            }
        }

        private void CacheComplete(object sender, CacheCompleteEventArgs e)
        {
            lock (this)
            {
                this.cacheProgress = 100;
                this.Progress = (this.cacheProgress + this.executeProgress) / this.progressPhases;
            }
        }

        private void ApplyExecuteProgress(object sender, ExecuteProgressEventArgs e)
        {
            lock (this)
            {

                this.executeProgress = e.OverallPercentage;
                this.Progress = (this.cacheProgress + this.executeProgress) / 2; // always two phases if we hit execution.

                if (WixBA.Model.Command.Display == Display.Embedded)
                {
                    WixBA.Model.Engine.SendEmbeddedProgress(e.ProgressPercentage, this.Progress);
                }

                e.Result = this.root.Canceled ? Result.Cancel : Result.Ok;
            }
        }
    }
}
