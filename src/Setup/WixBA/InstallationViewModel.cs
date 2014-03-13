//-------------------------------------------------------------------------------------------------
// <copyright file="InstallationViewModel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The model of the installation view.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.UX
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Reflection;
    using System.Windows;
    using System.Windows.Input;
    using IO = System.IO;
    using Microsoft.Tools.WindowsInstallerXml;
    using Microsoft.Tools.WindowsInstallerXml.Bootstrapper;

    /// <summary>
    /// The states of the installation view model.
    /// </summary>
    public enum InstallationState
    {
        Initializing,
        DetectedAbsent,
        DetectedPresent,
        DetectedNewer,
        Applying,
        Applied,
        Failed,
    }

    /// <summary>
    /// The model of the installation view in WixBA.
    /// </summary>
    public class InstallationViewModel : PropertyNotifyBase
    {
        private RootViewModel root;

        private Dictionary<string, int> downloadRetries;
        private bool downgrade;

        private ICommand licenseCommand;
        private ICommand launchHomePageCommand;
        private ICommand launchNewsCommand;
        private ICommand installCommand;
        private ICommand repairCommand;
        private ICommand uninstallCommand;
        private ICommand tryAgainCommand;

        private string message;
        private DateTime cachePackageStart;
        private DateTime executePackageStart;

        /// <summary>
        /// Creates a new model of the installation view.
        /// </summary>
        public InstallationViewModel(RootViewModel root)
        {
            this.root = root;
            this.downloadRetries = new Dictionary<string, int>();

            this.root.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(this.RootPropertyChanged);

            WixBA.Model.Bootstrapper.DetectBegin += this.DetectBegin;
            WixBA.Model.Bootstrapper.DetectRelatedBundle += this.DetectedRelatedBundle;
            WixBA.Model.Bootstrapper.DetectComplete += this.DetectComplete;
            WixBA.Model.Bootstrapper.PlanPackageBegin += this.PlanPackageBegin;
            WixBA.Model.Bootstrapper.PlanComplete += this.PlanComplete;
            WixBA.Model.Bootstrapper.ApplyBegin += this.ApplyBegin;
            WixBA.Model.Bootstrapper.CacheAcquireBegin += this.CacheAcquireBegin;
            WixBA.Model.Bootstrapper.CacheAcquireComplete += this.CacheAcquireComplete;
            WixBA.Model.Bootstrapper.ExecutePackageBegin += this.ExecutePackageBegin;
            WixBA.Model.Bootstrapper.ExecutePackageComplete += this.ExecutePackageComplete;
            WixBA.Model.Bootstrapper.Error += this.ExecuteError;
            WixBA.Model.Bootstrapper.ResolveSource += this.ResolveSource;
            WixBA.Model.Bootstrapper.ApplyComplete += this.ApplyComplete;
        }

        void RootPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ("State" == e.PropertyName)
            {
                base.OnPropertyChanged("Title");
                base.OnPropertyChanged("CompleteEnabled");
                base.OnPropertyChanged("ExitEnabled");
                base.OnPropertyChanged("RepairEnabled");
                base.OnPropertyChanged("InstallEnabled");
                base.OnPropertyChanged("TryAgainEnabled");
                base.OnPropertyChanged("UninstallEnabled");
            }
        }

        /// <summary>
        /// Gets the title for the application.
        /// </summary>
        public string Version 
        {
            get { return String.Concat("v", WixBA.Model.Version.ToString()); }
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

        /// <summary>
        /// Gets and sets whether the view model considers this install to be a downgrade.
        /// </summary>
        public bool Downgrade
        {
            get
            {
                return this.downgrade;
            }

            set
            {
                if (this.downgrade != value)
                {
                    this.downgrade = value;
                    base.OnPropertyChanged("Downgrade");
                }
            }
        }

        public ICommand LaunchHomePageCommand
        {
            get
            {
                if (this.launchHomePageCommand == null)
                {
                    this.launchHomePageCommand = new RelayCommand(param => WixBA.LaunchUrl(WixDistribution.SupportUrl), param => true);
                }

                return this.launchHomePageCommand;
            }
        }

        public ICommand LaunchNewsCommand
        {
            get
            {
                if (this.launchNewsCommand == null)
                {
                    this.launchNewsCommand = new RelayCommand(param => WixBA.LaunchUrl(WixDistribution.NewsUrl), param => true);
                }

                return this.launchNewsCommand;
            }
        }

        public ICommand LicenseCommand
        {
            get
            {
                if (this.licenseCommand == null)
                {
                    this.licenseCommand = new RelayCommand(param => this.LaunchLicense(), param => true);
                }

                return this.licenseCommand;
            }
        }

        public bool LicenseEnabled
        {
            get { return this.LicenseCommand.CanExecute(this); }
        }

        public ICommand CloseCommand
        {
            get { return this.root.CloseCommand; }
        }

        public bool ExitEnabled
        {
            get { return this.root.State != InstallationState.Applying; }
        }

        public ICommand InstallCommand
        {
            get
            {
                if (this.installCommand == null)
                {
                    this.installCommand = new RelayCommand(param => WixBA.Plan(LaunchAction.Install), param => this.root.State == InstallationState.DetectedAbsent);
                }

                return this.installCommand;
            }
        }

        public bool InstallEnabled
        {
            get { return this.InstallCommand.CanExecute(this); }
        }

        public ICommand RepairCommand
        {
            get
            {
                if (this.repairCommand == null)
                {
                    this.repairCommand = new RelayCommand(param => WixBA.Plan(LaunchAction.Repair), param => this.root.State == InstallationState.DetectedPresent);
                }

                return this.repairCommand;
            }
        }

        public bool RepairEnabled
        {
            get { return this.RepairCommand.CanExecute(this); }
        }

        public bool CompleteEnabled
        {
            get { return this.root.State == InstallationState.Applied; }
        }

        public ICommand UninstallCommand
        {
            get
            {
                if (this.uninstallCommand == null)
                {
                    this.uninstallCommand = new RelayCommand(param => WixBA.Plan(LaunchAction.Uninstall), param => this.root.State == InstallationState.DetectedPresent);
                }

                return this.uninstallCommand;
            }
        }

        public bool UninstallEnabled
        {
            get { return this.UninstallCommand.CanExecute(this); }
        }

        public ICommand TryAgainCommand
        {
            get
            {
                if (this.tryAgainCommand == null)
                {
                    this.tryAgainCommand = new RelayCommand(param => WixBA.Plan(WixBA.Model.PlannedAction), param => this.root.State == InstallationState.Failed);
                }

                return this.tryAgainCommand;
            }
        }

        public bool TryAgainEnabled
        {
            get { return this.TryAgainCommand.CanExecute(this); }
        }

        public string Title
        {
            get
            {
                switch (this.root.State)
                {
                    case InstallationState.Initializing:
                        return "Initializing...";

                    case InstallationState.DetectedPresent:
                        return "Installed";

                    case InstallationState.DetectedNewer:
                        return "Newer version installed";

                    case InstallationState.DetectedAbsent:
                        return "Not installed";

                    case InstallationState.Applying:
                        switch (WixBA.Model.PlannedAction)
                        {
                            case LaunchAction.Install:
                                return "Installing...";

                            case LaunchAction.Repair:
                                return "Repairing...";

                            case LaunchAction.Uninstall:
                                return "Uninstalling...";

                            case LaunchAction.UpdateReplace:
                            case LaunchAction.UpdateReplaceEmbedded:
                                return "Updating...";

                            default:
                                return "Unexpected action state";
                        }

                    case InstallationState.Applied:
                        switch (WixBA.Model.PlannedAction)
                        {
                            case LaunchAction.Install:
                                return "Successfully installed";

                            case LaunchAction.Repair:
                                return "Successfully repaired";

                            case LaunchAction.Uninstall:
                                return "Successfully uninstalled";

                            case LaunchAction.UpdateReplace:
                            case LaunchAction.UpdateReplaceEmbedded:
                                return "Successfully updated";

                            default:
                                return "Unexpected action state";
                        }

                    case InstallationState.Failed:
                        if (this.root.Canceled)
                        {
                            return "Canceled";
                        }
                        else if (LaunchAction.Unknown != WixBA.Model.PlannedAction)
                        {
                            switch (WixBA.Model.PlannedAction)
                            {
                                case LaunchAction.Install:
                                    return "Failed to install";

                                case LaunchAction.Repair:
                                    return "Failed to repair";

                                case LaunchAction.Uninstall:
                                    return "Failed to uninstall";

                                case LaunchAction.UpdateReplace:
                                case LaunchAction.UpdateReplaceEmbedded:
                                    return "Failed to update";

                                default:
                                    return "Unexpected action state";
                            }
                        }
                        else
                        {
                            return "Unexpected failure";
                        }

                    default:
                        return "Unknown view model state";
                }
            }
        }

        /// <summary>
        /// Launches the license in the default viewer.
        /// </summary>
        private void LaunchLicense()
        {
            string folder = IO.Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            WixBA.LaunchUrl(IO.Path.Combine(folder, "License.htm"));
        }

        private void DetectBegin(object sender, DetectBeginEventArgs e)
        {
            this.root.State = e.Installed ? InstallationState.DetectedPresent : InstallationState.DetectedAbsent;
            WixBA.Model.PlannedAction = LaunchAction.Unknown;
        }

        private void DetectedRelatedBundle(object sender, DetectRelatedBundleEventArgs e)
        {
            if (e.Operation == RelatedOperation.Downgrade)
            {
                this.Downgrade = true;
            }
        }

        private void DetectComplete(object sender, DetectCompleteEventArgs e)
        {
            // Parse the command line string before any planning.
            this.ParseCommandLine();

            if (LaunchAction.Uninstall == WixBA.Model.Command.Action)
            {
                WixBA.Model.Engine.Log(LogLevel.Verbose, "Invoking automatic plan for uninstall");
                WixBA.Plan(LaunchAction.Uninstall);
            }
            else if (Hresult.Succeeded(e.Status))
            {
                // block if CLR v2 isn't available; sorry, it's needed for the MSBuild tasks
                if (WixBA.Model.Engine.EvaluateCondition("NETFRAMEWORK35_SP_LEVEL < 1"))
                {
                    string message = "WiX Toolset requires the .NET Framework 3.5.1 Windows feature to be enabled.";
                    WixBA.Model.Engine.Log(LogLevel.Verbose, message);

                    if (Display.Full == WixBA.Model.Command.Display)
                    {
                        WixBA.Dispatcher.Invoke((Action)delegate()
                            {
                                MessageBox.Show(message, "WiX Toolset", MessageBoxButton.OK, MessageBoxImage.Error);
                                if (null != WixBA.View)
                                {
                                    WixBA.View.Close();
                                }
                            }
                        );
                    }

                    this.root.State = InstallationState.Failed;
                    return;
                }

                if (this.Downgrade)
                {
                    // TODO: What behavior do we want for downgrade?
                    this.root.State = InstallationState.DetectedNewer;
                }

                if (LaunchAction.Layout == WixBA.Model.Command.Action)
                {
                    WixBA.PlanLayout();
                }
                else if (WixBA.Model.Command.Display != Display.Full)
                {
                    // If we're not waiting for the user to click install, dispatch plan with the default action.
                    WixBA.Model.Engine.Log(LogLevel.Verbose, "Invoking automatic plan for non-interactive mode.");
                    WixBA.Plan(WixBA.Model.Command.Action);
                }
            }
            else
            {
                this.root.State = InstallationState.Failed;
            }
        }

        private void PlanPackageBegin(object sender, PlanPackageBeginEventArgs e)
        {
            if (WixBA.Model.Engine.StringVariables.Contains("MbaNetfxPackageId") && e.PackageId.Equals(WixBA.Model.Engine.StringVariables["MbaNetfxPackageId"], StringComparison.Ordinal))
            {
                e.State = RequestState.None;
            }
        }

        private void PlanComplete(object sender, PlanCompleteEventArgs e)
        {
            if (Hresult.Succeeded(e.Status))
            {
                this.root.PreApplyState = this.root.State;
                this.root.State = InstallationState.Applying;
                WixBA.Model.Engine.Apply(this.root.ViewWindowHandle);
            }
            else
            {
                this.root.State = InstallationState.Failed;
            }
        }

        private void ApplyBegin(object sender, ApplyBeginEventArgs e)
        {
            this.downloadRetries.Clear();
        }

        private void CacheAcquireBegin(object sender, CacheAcquireBeginEventArgs e)
        {
            this.cachePackageStart = DateTime.Now;
        }

        private void CacheAcquireComplete(object sender, CacheAcquireCompleteEventArgs e)
        {
            this.AddPackageTelemetry("Cache", e.PackageOrContainerId ?? String.Empty, DateTime.Now.Subtract(this.cachePackageStart).TotalMilliseconds, e.Status);
        }

        private void ExecutePackageBegin(object sender, ExecutePackageBeginEventArgs e)
        {
            this.executePackageStart = e.ShouldExecute ? DateTime.Now : DateTime.MinValue;
        }

        private void ExecutePackageComplete(object sender, ExecutePackageCompleteEventArgs e)
        {
            if (DateTime.MinValue < this.executePackageStart)
            {
                this.AddPackageTelemetry("Execute", e.PackageId ?? String.Empty, DateTime.Now.Subtract(this.executePackageStart).TotalMilliseconds, e.Status);
                this.executePackageStart = DateTime.MinValue;
            }
        }

        private void ExecuteError(object sender, ErrorEventArgs e)
        {
            lock (this)
            {
                if (!this.root.Canceled)
                {
                    // If the error is a cancel coming from the engine during apply we want to go back to the preapply state.
                    if (InstallationState.Applying == this.root.State && (int)Error.UserCancelled == e.ErrorCode)
                    {
                        this.root.State = this.root.PreApplyState;
                    }
                    else
                    {
                        this.Message = e.ErrorMessage;

                        if (Display.Full == WixBA.Model.Command.Display)
                        {
                            // On HTTP authentication errors, have the engine try to do authentication for us.
                            if (ErrorType.HttpServerAuthentication == e.ErrorType || ErrorType.HttpProxyAuthentication == e.ErrorType)
                            {
                                e.Result = Result.TryAgain;
                            }
                            else // show an error dialog.
                            {
                                MessageBoxButton msgbox = MessageBoxButton.OK;
                                switch (e.UIHint & 0xF)
                                {
                                    case 0:
                                        msgbox = MessageBoxButton.OK;
                                        break;
                                    case 1:
                                        msgbox = MessageBoxButton.OKCancel;
                                        break;
                                    // There is no 2! That would have been MB_ABORTRETRYIGNORE.
                                    case 3:
                                        msgbox = MessageBoxButton.YesNoCancel;
                                        break;
                                    case 4:
                                        msgbox = MessageBoxButton.YesNo;
                                        break;
                                    // default: stay with MBOK since an exact match is not available.
                                }

                                MessageBoxResult result = MessageBoxResult.None;
                                WixBA.View.Dispatcher.Invoke((Action)delegate()
                                    {
                                        result = MessageBox.Show(WixBA.View, e.ErrorMessage, "WiX Toolset", msgbox, MessageBoxImage.Error);
                                    }
                                    );

                                // If there was a match from the UI hint to the msgbox value, use the result from the
                                // message box. Otherwise, we'll ignore it and return the default to Burn.
                                if ((e.UIHint & 0xF) == (int)msgbox)
                                {
                                    e.Result = (Result)result;
                                }
                            }
                        }
                    }
                }
                else // canceled, so always return cancel.
                {
                    e.Result = Result.Cancel;
                }
            }
        }

        private void ResolveSource(object sender, ResolveSourceEventArgs e)
        {
            int retries = 0;

            this.downloadRetries.TryGetValue(e.PackageOrContainerId, out retries);
            this.downloadRetries[e.PackageOrContainerId] = retries + 1;

            e.Result = retries < 3 && !String.IsNullOrEmpty(e.DownloadSource) ? Result.Download : Result.Ok;
        }

        private void ApplyComplete(object sender, ApplyCompleteEventArgs e)
        {
            WixBA.Model.Result = e.Status; // remember the final result of the apply.

            // If we're not in Full UI mode, we need to alert the dispatcher to stop and close the window for passive.
            if (Bootstrapper.Display.Full != WixBA.Model.Command.Display)
            {
                // If its passive, send a message to the window to close.
                if (Bootstrapper.Display.Passive == WixBA.Model.Command.Display)
                {
                    WixBA.Model.Engine.Log(LogLevel.Verbose, "Automatically closing the window for non-interactive install");
                    WixBA.Dispatcher.BeginInvoke((Action)delegate()
                    {
                        WixBA.View.Close();
                    }
                    );
                }
                else
                {
                    WixBA.Dispatcher.InvokeShutdown();
                }
            }
            else if (Hresult.Succeeded(e.Status) && LaunchAction.UpdateReplace == WixBA.Model.PlannedAction) // if we successfully applied an update close the window since the new Bundle should be running now.
            {
                WixBA.Model.Engine.Log(LogLevel.Verbose, "Automatically closing the window since update successful.");
                WixBA.Dispatcher.BeginInvoke((Action)delegate()
                {
                    WixBA.View.Close();
                }
                );
            }

            // Set the state to applied or failed unless the state has already been set back to the preapply state
            // which means we need to show the UI as it was before the apply started.
            if (this.root.State != this.root.PreApplyState)
            {
                this.root.State = Hresult.Succeeded(e.Status) ? InstallationState.Applied : InstallationState.Failed;
            }
        }

        private void ParseCommandLine()
        {
            // Get array of arguments based on the system parsing algorithm.
            string[] args = WixBA.Model.Command.GetCommandLineArgs();
            for (int i = 0; i < args.Length; ++i)
            {
                if (args[i].StartsWith("InstallFolder=", StringComparison.InvariantCultureIgnoreCase))
                {
                    // Allow relative directory paths. Also validates.
                    string[] param = args[i].Split(new char[] {'='}, 2);
                    this.root.InstallDirectory = IO.Path.Combine(Environment.CurrentDirectory, param[1]);
                }
            }
        }

        private void AddPackageTelemetry(string prefix, string id, double time, int result)
        {
            lock (this)
            {
                string key = String.Format("{0}Time_{1}", prefix, id);
                string value = time.ToString();
                WixBA.Model.Telemetry.Add(new KeyValuePair<string, string>(key, value));

                key = String.Format("{0}Result_{1}", prefix, id);
                value = String.Concat("0x", result.ToString("x"));
                WixBA.Model.Telemetry.Add(new KeyValuePair<string, string>(key, value));
            }
        }
    }
}
