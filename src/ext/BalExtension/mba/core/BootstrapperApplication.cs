//-------------------------------------------------------------------------------------------------
// <copyright file="BootstrapperApplication.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The default user experience.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Bootstrapper
{
    using System;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Threading;

    /// <summary>
    /// The default bootstrapper application.
    /// </summary>
    [ClassInterface(ClassInterfaceType.None)]
    public abstract class BootstrapperApplication : MarshalByRefObject, IBootstrapperApplication
    {
        private Engine engine;
        private Command command;
        private bool applying;

        /// <summary>
        /// Creates a new instance of the <see cref="BootstrapperApplication"/> class.
        /// </summary>
        protected BootstrapperApplication()
        {
            this.engine = null;
            this.applying = false;
        }

        /// <summary>
        /// Fired when the engine is starting up the bootstrapper application.
        /// </summary>
        public event EventHandler<StartupEventArgs> Startup;

        /// <summary>
        /// Fired when the engine is shutting down the bootstrapper application.
        /// </summary>
        public event EventHandler<ShutdownEventArgs> Shutdown;

        /// <summary>
        /// Fired when the system is shutting down or user is logging off.
        /// </summary>
        /// <remarks>
        /// <para>To prevent shutting down or logging off, set <see cref="ResultEventArgs.Result"/> to
        /// <see cref="Result.Cancel"/>; otherwise, set it to <see cref="Result.Ok"/>.</para>
        /// <para>By default setup will prevent shutting down or logging off between
        /// <see cref="BootstrapperApplication.ApplyBegin"/> and <see cref="BootstrapperApplication.ApplyComplete"/>.
        /// Derivatives can change this behavior by overriding <see cref="BootstrapperApplication.OnSystemShutdown"/>
        /// or handling <see cref="BootstrapperApplication.SystemShutdown"/>.</para>
        /// <para>If <see cref="SystemShutdownEventArgs.Reasons"/> contains <see cref="EndSessionReasons.Critical"/>
        /// the bootstrapper cannot prevent the shutdown and only has a few seconds to save state or perform any other
        /// critical operations before being closed by the operating system.</para>
        /// <para>This event may be fired on a different thread.</para>
        /// </remarks>
        public event EventHandler<SystemShutdownEventArgs> SystemShutdown;

        /// <summary>
        /// Fired when the overall detection phase has begun.
        /// </summary>
        public event EventHandler<DetectBeginEventArgs> DetectBegin;

        /// <summary>
        /// Fired when a forward compatible bundle is detected.
        /// </summary>
        public event EventHandler<DetectForwardCompatibleBundleEventArgs> DetectForwardCompatibleBundle;

        /// <summary>
        /// Fired when the update detection phase has begun.
        /// </summary>
        public event EventHandler<DetectUpdateBeginEventArgs> DetectUpdateBegin;

        /// <summary>
        /// Fired when the update detection phase has completed.
        /// </summary>
        public event EventHandler<DetectUpdateCompleteEventArgs> DetectUpdateComplete;

        /// <summary>
        /// Fired when the detection for a prior bundle has begun.
        /// </summary>
        public event EventHandler<DetectPriorBundleEventArgs> DetectPriorBundle;

        /// <summary>
        /// Fired when a related bundle has been detected for a bundle.
        /// </summary>
        public event EventHandler<DetectRelatedBundleEventArgs> DetectRelatedBundle;

        /// <summary>
        /// Fired when the detection for a specific package has begun.
        /// </summary>
        public event EventHandler<DetectPackageBeginEventArgs> DetectPackageBegin;

        /// <summary>
        /// Fired when a package was not detected but a package using the same provider key was.
        /// </summary>
        public event EventHandler<DetectCompatiblePackageEventArgs> DetectCompatiblePackage;

        /// <summary>
        /// Fired when a related MSI package has been detected for a package.
        /// </summary>
        public event EventHandler<DetectRelatedMsiPackageEventArgs> DetectRelatedMsiPackage;

        /// <summary>
        /// Fired when an MSP package detects a target MSI has been detected.
        /// </summary>
        public event EventHandler<DetectTargetMsiPackageEventArgs> DetectTargetMsiPackage;

        /// <summary>
        /// Fired when a feature in an MSI package has been detected.
        /// </summary>
        public event EventHandler<DetectMsiFeatureEventArgs> DetectMsiFeature;

        /// <summary>
        /// Fired when the detection for a specific package has completed.
        /// </summary>
        public event EventHandler<DetectPackageCompleteEventArgs> DetectPackageComplete;

        /// <summary>
        /// Fired when the detection phase has completed.
        /// </summary>
        public event EventHandler<DetectCompleteEventArgs> DetectComplete;

        /// <summary>
        /// Fired when the engine has begun planning the installation.
        /// </summary>
        public event EventHandler<PlanBeginEventArgs> PlanBegin;

        /// <summary>
        /// Fired when the engine has begun planning for a related bundle.
        /// </summary>
        public event EventHandler<PlanRelatedBundleEventArgs> PlanRelatedBundle;

        /// <summary>
        /// Fired when the engine has begun planning the installation of a specific package.
        /// </summary>
        public event EventHandler<PlanPackageBeginEventArgs> PlanPackageBegin;

        /// <summary>
        /// Fired when the engine plans a new, compatible package using the same provider key.
        /// </summary>
        public event EventHandler<PlanCompatiblePackageEventArgs> PlanCompatiblePackage;

        /// <summary>
        /// Fired when the engine is about to plan the target MSI of a MSP package.
        /// </summary>
        public event EventHandler<PlanTargetMsiPackageEventArgs> PlanTargetMsiPackage;

        /// <summary>
        /// Fired when the engine is about to plan a feature in an MSI package.
        /// </summary>
        public event EventHandler<PlanMsiFeatureEventArgs> PlanMsiFeature;

        /// <summary>
        /// Fired when the engine has completed planning the installation of a specific package.
        /// </summary>
        public event EventHandler<PlanPackageCompleteEventArgs> PlanPackageComplete;

        /// <summary>
        /// Fired when the engine has completed planning the installation.
        /// </summary>
        public event EventHandler<PlanCompleteEventArgs> PlanComplete;

        /// <summary>
        /// Fired when the engine has begun installing the bundle.
        /// </summary>
        public event EventHandler<ApplyBeginEventArgs> ApplyBegin;

        /// <summary>
        /// Fired when the engine is about to start the elevated process.
        /// </summary>
        public event EventHandler<ElevateEventArgs> Elevate;

        /// <summary>
        /// Fired when the engine has begun registering the location and visibility of the bundle.
        /// </summary>
        public event EventHandler<RegisterBeginEventArgs> RegisterBegin;

        /// <summary>
        /// Fired when the engine has completed registering the location and visibility of the bundle.
        /// </summary>
        public event EventHandler<RegisterCompleteEventArgs> RegisterComplete;

        /// <summary>
        /// Fired when the engine has begun removing the registration for the location and visibility of the bundle.
        /// </summary>
        public event EventHandler<UnregisterBeginEventArgs> UnregisterBegin;

        /// <summary>
        /// Fired when the engine has completed removing the registration for the location and visibility of the bundle.
        /// </summary>
        public event EventHandler<UnregisterCompleteEventArgs> UnregisterComplete;

        /// <summary>
        /// Fired when the engine has begun caching the installation sources.
        /// </summary>
        public event EventHandler<CacheBeginEventArgs> CacheBegin;

        /// <summary>
        /// Fired when the engine has begun caching a specific package.
        /// </summary>
        public event EventHandler<CachePackageBeginEventArgs> CachePackageBegin;

        /// <summary>
        /// Fired when the engine has begun acquiring the installation sources.
        /// </summary>
        public event EventHandler<CacheAcquireBeginEventArgs> CacheAcquireBegin;

        /// <summary>
        /// Fired when the engine has progress acquiring the installation sources.
        /// </summary>
        public event EventHandler<CacheAcquireProgressEventArgs> CacheAcquireProgress;

        /// <summary>
        /// Fired by the engine to allow the user experience to change the source
        /// using <see cref="M:Engine.SetLocalSource"/> or <see cref="M:Engine.SetDownloadSource"/>.
        /// </summary>
        public event EventHandler<ResolveSourceEventArgs> ResolveSource;

        /// <summary>
        /// Fired when the engine has completed the acquisition of the installation sources.
        /// </summary>
        public event EventHandler<CacheAcquireCompleteEventArgs> CacheAcquireComplete;

        /// <summary>
        /// Fired when the engine begins the verification of the acquired installation sources.
        /// </summary>
        public event EventHandler<CacheVerifyBeginEventArgs> CacheVerifyBegin;

        /// <summary>
        /// Fired when the engine complete the verification of the acquired installation sources.
        /// </summary>
        public event EventHandler<CacheVerifyCompleteEventArgs> CacheVerifyComplete;

        /// <summary>
        /// Fired when the engine has completed caching a specific package.
        /// </summary>
        public event EventHandler<CachePackageCompleteEventArgs> CachePackageComplete;

        /// <summary>
        /// Fired after the engine has cached the installation sources.
        /// </summary>
        public event EventHandler<CacheCompleteEventArgs> CacheComplete;

        /// <summary>
        /// Fired when the engine has begun installing packages.
        /// </summary>
        public event EventHandler<ExecuteBeginEventArgs> ExecuteBegin;

        /// <summary>
        /// Fired when the engine has begun installing a specific package.
        /// </summary>
        public event EventHandler<ExecutePackageBeginEventArgs> ExecutePackageBegin;

        /// <summary>
        /// Fired when the engine executes one or more patches targeting a product.
        /// </summary>
        public event EventHandler<ExecutePatchTargetEventArgs> ExecutePatchTarget;

        /// <summary>
        /// Fired when the engine has encountered an error.
        /// </summary>
        public event EventHandler<ErrorEventArgs> Error;

        /// <summary>
        /// Fired when the engine has changed progress for the bundle installation.
        /// </summary>
        public event EventHandler<ProgressEventArgs> Progress;

        /// <summary>
        /// Fired when Windows Installer sends an installation message.
        /// </summary>
        public event EventHandler<ExecuteMsiMessageEventArgs> ExecuteMsiMessage;

        /// <summary>
        /// Fired when Windows Installer sends a files in use installation message.
        /// </summary>
        public event EventHandler<ExecuteFilesInUseEventArgs> ExecuteFilesInUse;

        /// <summary>
        /// Fired when the engine has completed installing a specific package.
        /// </summary>
        public event EventHandler<ExecutePackageCompleteEventArgs> ExecutePackageComplete;

        /// <summary>
        /// Fired when the engine has completed installing packages.
        /// </summary>
        public event EventHandler<ExecuteCompleteEventArgs> ExecuteComplete;

        /// <summary>
        /// Fired by the engine to request a restart now or inform the user a manual restart is required later.
        /// </summary>
        public event EventHandler<RestartRequiredEventArgs> RestartRequired;

        /// <summary>
        /// Fired when the engine has completed installing the bundle.
        /// </summary>
        public event EventHandler<ApplyCompleteEventArgs> ApplyComplete;

        /// <summary>
        /// Fired by the engine while executing on payload.
        /// </summary>
        public event EventHandler<ExecuteProgressEventArgs> ExecuteProgress;

        /// <summary>
        /// Fired when the engine has completed launching the preapproved executable.
        /// </summary>
        public event EventHandler<LaunchApprovedExeCompleteArgs> LaunchApprovedExeComplete;

        /// <summary>
        /// Specifies whether this bootstrapper should run asynchronously. The default is true.
        /// </summary>
        public virtual bool AsyncExecution
        {
            get { return true; }
        }

        /// <summary>
        /// Gets the <see cref="Command"/> information for how the UX should be started.
        /// </summary>
        public Command Command
        {
            get { return this.command; }
            internal set { this.command = value; }
        }

        /// <summary>
        /// Gets the <see cref="Engine"/> for interaction with the Engine.
        /// </summary>
        public Engine Engine
        {
            get { return this.engine; }
            internal set { this.engine = value; }
        }

        /// <summary>
        /// Entry point that is called when the bootstrapper application is ready to run.
        /// </summary>
        protected abstract void Run();

        /// <summary>
        /// Called by the engine on startup of the bootstrapper application.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnStartup(StartupEventArgs args)
        {
            EventHandler<StartupEventArgs> handler = this.Startup;
            if (null != handler)
            {
                handler(this, args);
            }

            if (this.AsyncExecution)
            {
                this.Engine.Log(LogLevel.Verbose, "Creating BA thread to run asynchronously.");
                Thread uiThread = new Thread(this.Run);
                uiThread.Name = "UIThread";
                uiThread.SetApartmentState(ApartmentState.STA);
                uiThread.Start();
            }
            else
            {
                this.Engine.Log(LogLevel.Verbose, "Creating BA thread to run synchronously.");
                this.Run();
            }
        }

        /// <summary>
        /// Called by the engine to uninitialize the user experience.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnShutdown(ShutdownEventArgs args)
        {
            EventHandler<ShutdownEventArgs> handler = this.Shutdown;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the system is shutting down or the user is logging off.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        /// <remarks>
        /// <para>To prevent shutting down or logging off, set <see cref="ResultEventArgs.Result"/> to
        /// <see cref="Result.Cancel"/>; otherwise, set it to <see cref="Result.Ok"/>.</para>
        /// <para>By default setup will prevent shutting down or logging off between
        /// <see cref="BootstrapperApplication.ApplyBegin"/> and <see cref="BootstrapperApplication.ApplyComplete"/>.
        /// Derivatives can change this behavior by overriding <see cref="BootstrapperApplication.OnSystemShutdown"/>
        /// or handling <see cref="BootstrapperApplication.SystemShutdown"/>.</para>
        /// <para>If <see cref="SystemShutdownEventArgs.Reasons"/> contains <see cref="EndSessionReasons.Critical"/>
        /// the bootstrapper cannot prevent the shutdown and only has a few seconds to save state or perform any other
        /// critical operations before being closed by the operating system.</para>
        /// <para>This method may be called on a different thread.</para>
        /// </remarks>
        protected virtual void OnSystemShutdown(SystemShutdownEventArgs args)
        {
            EventHandler<SystemShutdownEventArgs> handler = this.SystemShutdown;
            if (null != handler)
            {
                handler(this, args);
            }
            else if (null != args)
            {
                // Allow requests to shut down when critical or not applying.
                bool critical = EndSessionReasons.Critical == (EndSessionReasons.Critical & args.Reasons);
                args.Result = (critical || !this.applying) ? Result.Ok : Result.Cancel;
            }
        }

        /// <summary>
        /// Called when the overall detection phase has begun.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectBegin(DetectBeginEventArgs args)
        {
            EventHandler<DetectBeginEventArgs> handler = this.DetectBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the update detection phase has begun.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectForwardCompatibleBundle(DetectForwardCompatibleBundleEventArgs args)
        {
            EventHandler<DetectForwardCompatibleBundleEventArgs> handler = this.DetectForwardCompatibleBundle;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the update detection phase has begun.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectUpdateBegin(DetectUpdateBeginEventArgs args)
        {
            EventHandler<DetectUpdateBeginEventArgs> handler = this.DetectUpdateBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the update detection phase has completed.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectUpdateComplete(DetectUpdateCompleteEventArgs args)
        {
            EventHandler<DetectUpdateCompleteEventArgs> handler = this.DetectUpdateComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the detection for a prior bundle has begun.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectPriorBundle(DetectPriorBundleEventArgs args)
        {
            EventHandler<DetectPriorBundleEventArgs> handler = this.DetectPriorBundle;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when a related bundle has been detected for a bundle.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectRelatedBundle(DetectRelatedBundleEventArgs args)
        {
            EventHandler<DetectRelatedBundleEventArgs> handler = this.DetectRelatedBundle;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the detection for a specific package has begun.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectPackageBegin(DetectPackageBeginEventArgs args)
        {
            EventHandler<DetectPackageBeginEventArgs> handler = this.DetectPackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when a package was not detected but a package using the same provider key was.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectCompatiblePackage(DetectCompatiblePackageEventArgs args)
        {
            EventHandler<DetectCompatiblePackageEventArgs> handler = this.DetectCompatiblePackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when a related MSI package has been detected for a package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectRelatedMsiPackage(DetectRelatedMsiPackageEventArgs args)
        {
            EventHandler<DetectRelatedMsiPackageEventArgs> handler = this.DetectRelatedMsiPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when an MSP package detects a target MSI has been detected.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectTargetMsiPackage(DetectTargetMsiPackageEventArgs args)
        {
            EventHandler<DetectTargetMsiPackageEventArgs> handler = this.DetectTargetMsiPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when an MSI feature has been detected for a package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectMsiFeature(DetectMsiFeatureEventArgs args)
        {
            EventHandler<DetectMsiFeatureEventArgs> handler = this.DetectMsiFeature;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the detection for a specific package has completed.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectPackageComplete(DetectPackageCompleteEventArgs args)
        {
            EventHandler<DetectPackageCompleteEventArgs> handler = this.DetectPackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the detection phase has completed.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnDetectComplete(DetectCompleteEventArgs args)
        {
            EventHandler<DetectCompleteEventArgs> handler = this.DetectComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has begun planning the installation.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanBegin(PlanBeginEventArgs args)
        {
            EventHandler<PlanBeginEventArgs> handler = this.PlanBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has begun planning for a prior bundle.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanRelatedBundle(PlanRelatedBundleEventArgs args)
        {
            EventHandler<PlanRelatedBundleEventArgs> handler = this.PlanRelatedBundle;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has begun planning the installation of a specific package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanPackageBegin(PlanPackageBeginEventArgs args)
        {
            EventHandler<PlanPackageBeginEventArgs> handler = this.PlanPackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine plans a new, compatible package using the same provider key.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanCompatiblePackage(PlanCompatiblePackageEventArgs args)
        {
            EventHandler<PlanCompatiblePackageEventArgs> handler = this.PlanCompatiblePackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine is about to plan the target MSI of a MSP package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanTargetMsiPackage(PlanTargetMsiPackageEventArgs args)
        {
            EventHandler<PlanTargetMsiPackageEventArgs> handler = this.PlanTargetMsiPackage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine is about to plan an MSI feature of a specific package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanMsiFeature(PlanMsiFeatureEventArgs args)
        {
            EventHandler<PlanMsiFeatureEventArgs> handler = this.PlanMsiFeature;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when then engine has completed planning the installation of a specific package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanPackageComplete(PlanPackageCompleteEventArgs args)
        {
            EventHandler<PlanPackageCompleteEventArgs> handler = this.PlanPackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has completed planning the installation.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnPlanComplete(PlanCompleteEventArgs args)
        {
            EventHandler<PlanCompleteEventArgs> handler = this.PlanComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has begun installing the bundle.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnApplyBegin(ApplyBeginEventArgs args)
        {
            EventHandler<ApplyBeginEventArgs> handler = this.ApplyBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine is about to start the elevated process.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnElevate(ElevateEventArgs args)
        {
            EventHandler<ElevateEventArgs> handler = this.Elevate;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has begun registering the location and visibility of the bundle.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnRegisterBegin(RegisterBeginEventArgs args)
        {
            EventHandler<RegisterBeginEventArgs> handler = this.RegisterBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has completed registering the location and visilibity of the bundle.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnRegisterComplete(RegisterCompleteEventArgs args)
        {
            EventHandler<RegisterCompleteEventArgs> handler = this.RegisterComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has begun removing the registration for the location and visibility of the bundle.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnUnregisterBegin(UnregisterBeginEventArgs args)
        {
            EventHandler<UnregisterBeginEventArgs> handler = this.UnregisterBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has completed removing the registration for the location and visibility of the bundle.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnUnregisterComplete(UnregisterCompleteEventArgs args)
        {
            EventHandler<UnregisterCompleteEventArgs> handler = this.UnregisterComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine begins to cache the installation sources.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheBegin(CacheBeginEventArgs args)
        {
            EventHandler<CacheBeginEventArgs> handler = this.CacheBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine when it begins to cache a specific package.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCachePackageBegin(CachePackageBeginEventArgs args)
        {
            EventHandler<CachePackageBeginEventArgs> handler = this.CachePackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine begins to cache the container or payload.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheAcquireBegin(CacheAcquireBeginEventArgs args)
        {
            EventHandler<CacheAcquireBeginEventArgs> handler = this.CacheAcquireBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has progressed on caching the container or payload.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheAcquireProgress(CacheAcquireProgressEventArgs args)
        {
            EventHandler<CacheAcquireProgressEventArgs> handler = this.CacheAcquireProgress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine to allow the user experience to change the source
        /// using <see cref="M:Engine.SetLocalSource"/> or <see cref="M:Engine.SetDownloadSource"/>.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnResolveSource(ResolveSourceEventArgs args)
        {
            EventHandler<ResolveSourceEventArgs> handler = this.ResolveSource;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine complets caching of the container or payload.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheAcquireComplete(CacheAcquireCompleteEventArgs args)
        {
            EventHandler<CacheAcquireCompleteEventArgs> handler = this.CacheAcquireComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has started verify the payload.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheVerifyBegin(CacheVerifyBeginEventArgs args)
        {
            EventHandler<CacheVerifyBeginEventArgs> handler = this.CacheVerifyBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine completes verification of the payload.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCacheVerifyComplete(CacheVerifyCompleteEventArgs args)
        {
            EventHandler<CacheVerifyCompleteEventArgs> handler = this.CacheVerifyComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine completes caching a specific package.
        /// </summary>
        /// <param name="args"></param>
        protected virtual void OnCachePackageComplete(CachePackageCompleteEventArgs args)
        {
            EventHandler<CachePackageCompleteEventArgs> handler = this.CachePackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called after the engine has cached the installation sources.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnCacheComplete(CacheCompleteEventArgs args)
        {
            EventHandler<CacheCompleteEventArgs> handler = this.CacheComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has begun installing packages.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteBegin(ExecuteBeginEventArgs args)
        {
            EventHandler<ExecuteBeginEventArgs> handler = this.ExecuteBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has begun installing a specific package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecutePackageBegin(ExecutePackageBeginEventArgs args)
        {
            EventHandler<ExecutePackageBeginEventArgs> handler = this.ExecutePackageBegin;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine executes one or more patches targeting a product.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecutePatchTarget(ExecutePatchTargetEventArgs args)
        {
            EventHandler<ExecutePatchTargetEventArgs> handler = this.ExecutePatchTarget;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has encountered an error.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnError(ErrorEventArgs args)
        {
            EventHandler<ErrorEventArgs> handler = this.Error;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has changed progress for the bundle installation.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnProgress(ProgressEventArgs args)
        {
            EventHandler<ProgressEventArgs> handler = this.Progress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when Windows Installer sends an installation message.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteMsiMessage(ExecuteMsiMessageEventArgs args)
        {
            EventHandler<ExecuteMsiMessageEventArgs> handler = this.ExecuteMsiMessage;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when Windows Installer sends a file in use installation message.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteFilesInUse(ExecuteFilesInUseEventArgs args)
        {
            EventHandler<ExecuteFilesInUseEventArgs> handler = this.ExecuteFilesInUse;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has completed installing a specific package.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecutePackageComplete(ExecutePackageCompleteEventArgs args)
        {
            EventHandler<ExecutePackageCompleteEventArgs> handler = this.ExecutePackageComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has completed installing packages.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteComplete(ExecuteCompleteEventArgs args)
        {
            EventHandler<ExecuteCompleteEventArgs> handler = this.ExecuteComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine to request a restart now or inform the user a manual restart is required later.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnRestartRequired(RestartRequiredEventArgs args)
        {
            EventHandler<RestartRequiredEventArgs> handler = this.RestartRequired;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called when the engine has completed installing the bundle.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnApplyComplete(ApplyCompleteEventArgs args)
        {
            EventHandler<ApplyCompleteEventArgs> handler = this.ApplyComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine while executing on payload.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnExecuteProgress(ExecuteProgressEventArgs args)
        {
            EventHandler<ExecuteProgressEventArgs> handler = this.ExecuteProgress;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Called by the engine after trying to launch the preapproved executable.
        /// </summary>
        /// <param name="args">Additional arguments for this event.</param>
        protected virtual void OnLaunchApprovedExeComplete(LaunchApprovedExeCompleteArgs args)
        {
            EventHandler<LaunchApprovedExeCompleteArgs> handler = this.LaunchApprovedExeComplete;
            if (null != handler)
            {
                handler(this, args);
            }
        }

        #region IBootstrapperApplication Members

        void IBootstrapperApplication.OnStartup()
        {
            StartupEventArgs args = new StartupEventArgs();
            this.OnStartup(args);
        }

        Result IBootstrapperApplication.OnShutdown()
        {
            ShutdownEventArgs args = new ShutdownEventArgs();
            this.OnShutdown(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnSystemShutdown(EndSessionReasons dwEndSession, int nRecommendation)
        {
            SystemShutdownEventArgs args = new SystemShutdownEventArgs(dwEndSession, nRecommendation);
            this.OnSystemShutdown(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnDetectBegin(bool fInstalled, int cPackages)
        {
            DetectBeginEventArgs args = new DetectBeginEventArgs(fInstalled, cPackages);
            this.OnDetectBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnDetectForwardCompatibleBundle(string wzBundleId, RelationType relationType, string wzBundleTag, bool fPerMachine, long version, int nRecommendation)
        {
            DetectForwardCompatibleBundleEventArgs args = new DetectForwardCompatibleBundleEventArgs(wzBundleId, relationType, wzBundleTag, fPerMachine, version, nRecommendation);
            this.OnDetectForwardCompatibleBundle(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnDetectUpdateBegin(string wzUpdateLocation, int nRecommendation)
        {
            DetectUpdateBeginEventArgs args = new DetectUpdateBeginEventArgs(wzUpdateLocation, nRecommendation);
            this.OnDetectUpdateBegin(args);

            return args.Result;
        }

        void IBootstrapperApplication.OnDetectUpdateComplete(int hrStatus, string wzUpdateLocation)
        {
            this.OnDetectUpdateComplete(new DetectUpdateCompleteEventArgs(hrStatus, wzUpdateLocation));
        }

        Result IBootstrapperApplication.OnDetectRelatedBundle(string wzProductCode, RelationType relationType, string wzBundleTag, bool fPerMachine, long version, RelatedOperation operation)
        {
            DetectRelatedBundleEventArgs args = new DetectRelatedBundleEventArgs(wzProductCode, relationType, wzBundleTag, fPerMachine, version, operation);
            this.OnDetectRelatedBundle(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnDetectPackageBegin(string wzPackageId)
        {
            DetectPackageBeginEventArgs args = new DetectPackageBeginEventArgs(wzPackageId);
            this.OnDetectPackageBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnDetectCompatiblePackage(string wzPackageId, string wzCompatiblePackageId)
        {
            DetectCompatiblePackageEventArgs args = new DetectCompatiblePackageEventArgs(wzPackageId, wzCompatiblePackageId);
            this.OnDetectCompatiblePackage(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnDetectRelatedMsiPackage(string wzPackageId, string wzProductCode, bool fPerMachine, long version, RelatedOperation operation)
        {
            DetectRelatedMsiPackageEventArgs args = new DetectRelatedMsiPackageEventArgs(wzPackageId, wzProductCode, fPerMachine, version, operation);
            this.OnDetectRelatedMsiPackage(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnDetectTargetMsiPackage(string wzPackageId, string wzProductCode, PackageState patchState)
        {
            DetectTargetMsiPackageEventArgs args = new DetectTargetMsiPackageEventArgs(wzPackageId, wzProductCode, patchState);
            this.OnDetectTargetMsiPackage(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnDetectMsiFeature(string wzPackageId, string wzFeatureId, FeatureState state)
        {
            DetectMsiFeatureEventArgs args = new DetectMsiFeatureEventArgs(wzPackageId, wzFeatureId, state);
            this.OnDetectMsiFeature(args);

            return args.Result;
        }

        void IBootstrapperApplication.OnDetectPackageComplete(string wzPackageId, int hrStatus, PackageState state)
        {
            this.OnDetectPackageComplete(new DetectPackageCompleteEventArgs(wzPackageId, hrStatus, state));
        }

        void IBootstrapperApplication.OnDetectComplete(int hrStatus)
        {
            this.OnDetectComplete(new DetectCompleteEventArgs(hrStatus));
        }

        Result IBootstrapperApplication.OnPlanBegin(int cPackages)
        {
            PlanBeginEventArgs args = new PlanBeginEventArgs(cPackages);
            this.OnPlanBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnPlanRelatedBundle(string wzBundleId, ref RequestState pRequestedState)
        {
            PlanRelatedBundleEventArgs args = new PlanRelatedBundleEventArgs(wzBundleId, pRequestedState);
            this.OnPlanRelatedBundle(args);

            pRequestedState = args.State;
            return args.Result;
        }

        Result IBootstrapperApplication.OnPlanPackageBegin(string wzPackageId, ref RequestState pRequestedState)
        {
            PlanPackageBeginEventArgs args = new PlanPackageBeginEventArgs(wzPackageId, pRequestedState);
            this.OnPlanPackageBegin(args);

            pRequestedState = args.State;
            return args.Result;
        }

        Result IBootstrapperApplication.OnPlanCompatiblePackage(string wzPackageId, ref RequestState pRequestedState)
        {
            PlanCompatiblePackageEventArgs args = new PlanCompatiblePackageEventArgs(wzPackageId, pRequestedState);
            this.OnPlanCompatiblePackage(args);

            pRequestedState = args.State;
            return args.Result;
        }

        Result IBootstrapperApplication.OnPlanTargetMsiPackage(string wzPackageId, string wzProductCode, ref RequestState pRequestedState)
        {
            PlanTargetMsiPackageEventArgs args = new PlanTargetMsiPackageEventArgs(wzPackageId, wzProductCode, pRequestedState);
            this.OnPlanTargetMsiPackage(args);

            pRequestedState = args.State;
            return args.Result;
        }

        Result IBootstrapperApplication.OnPlanMsiFeature(string wzPackageId, string wzFeatureId, ref FeatureState pRequestedState)
        {
            PlanMsiFeatureEventArgs args = new PlanMsiFeatureEventArgs(wzPackageId, wzFeatureId, pRequestedState);
            this.OnPlanMsiFeature(args);

            pRequestedState = args.State;
            return args.Result;
        }

        void IBootstrapperApplication.OnPlanPackageComplete(string wzPackageId, int hrStatus, PackageState state, RequestState requested, ActionState execute, ActionState rollback)
        {
            this.OnPlanPackageComplete(new PlanPackageCompleteEventArgs(wzPackageId, hrStatus, state, requested, execute, rollback));
        }

        void IBootstrapperApplication.OnPlanComplete(int hrStatus)
        {
            this.OnPlanComplete(new PlanCompleteEventArgs(hrStatus));
        }

        Result IBootstrapperApplication.OnApplyBegin()
        {
            this.applying = true;

            ApplyBeginEventArgs args = new ApplyBeginEventArgs();
            this.OnApplyBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnElevate()
        {
            ElevateEventArgs args = new ElevateEventArgs();
            this.OnElevate(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnRegisterBegin()
        {
            RegisterBeginEventArgs args = new RegisterBeginEventArgs();
            this.OnRegisterBegin(args);

            return args.Result;
        }

        void IBootstrapperApplication.OnRegisterComplete(int hrStatus)
        {
            this.OnRegisterComplete(new RegisterCompleteEventArgs(hrStatus));
        }

        void IBootstrapperApplication.OnUnregisterBegin()
        {
            this.OnUnregisterBegin(new UnregisterBeginEventArgs());
        }

        void IBootstrapperApplication.OnUnregisterComplete(int hrStatus)
        {
            this.OnUnregisterComplete(new UnregisterCompleteEventArgs(hrStatus));
        }

        Result IBootstrapperApplication.OnCacheBegin()
        {
            CacheBeginEventArgs args = new CacheBeginEventArgs();
            this.OnCacheBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnCachePackageBegin(string wzPackageId, int cCachePayloads, long dw64PackageCacheSize)
        {
            CachePackageBeginEventArgs args = new CachePackageBeginEventArgs(wzPackageId, cCachePayloads, dw64PackageCacheSize);
            this.OnCachePackageBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnCacheAcquireBegin(string wzPackageOrContainerId, string wzPayloadId, CacheOperation operation, string wzSource)
        {
            CacheAcquireBeginEventArgs args = new CacheAcquireBeginEventArgs(wzPackageOrContainerId, wzPayloadId, operation, wzSource);
            this.OnCacheAcquireBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnCacheAcquireProgress(string wzPackageOrContainerId, string wzPayloadId, long dw64Progress, long dw64Total, int dwOverallPercentage)
        {
            CacheAcquireProgressEventArgs args = new CacheAcquireProgressEventArgs(wzPackageOrContainerId, wzPayloadId, dw64Progress, dw64Total, dwOverallPercentage);
            this.OnCacheAcquireProgress(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnResolveSource(string wzPackageOrContainerId, string wzPayloadId, string wzLocalSource, string wzDownloadSource)
        {
            ResolveSourceEventArgs args = new ResolveSourceEventArgs(wzPackageOrContainerId, wzPayloadId, wzLocalSource, wzDownloadSource);
            this.OnResolveSource(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnCacheAcquireComplete(string wzPackageOrContainerId, string wzPayloadId, int hrStatus, int nRecommendation)
        {
            CacheAcquireCompleteEventArgs args = new CacheAcquireCompleteEventArgs(wzPackageOrContainerId, wzPayloadId, hrStatus, nRecommendation);
            this.OnCacheAcquireComplete(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnCacheVerifyBegin(string wzPackageId, string wzPayloadId)
        {
            CacheVerifyBeginEventArgs args = new CacheVerifyBeginEventArgs(wzPackageId, wzPayloadId);
            this.OnCacheVerifyBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnCacheVerifyComplete(string wzPackageId, string wzPayloadId, int hrStatus, int nRecommendation)
        {
            CacheVerifyCompleteEventArgs args = new CacheVerifyCompleteEventArgs(wzPackageId, wzPayloadId, hrStatus, nRecommendation);
            this.OnCacheVerifyComplete(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnCachePackageComplete(string wzPackageId, int hrStatus, int nRecommendation)
        {
            CachePackageCompleteEventArgs args = new CachePackageCompleteEventArgs(wzPackageId, hrStatus, nRecommendation);
            this.OnCachePackageComplete(args);

            return args.Result;
        }

        void IBootstrapperApplication.OnCacheComplete(int hrStatus)
        {
            this.OnCacheComplete(new CacheCompleteEventArgs(hrStatus));
        }

        Result IBootstrapperApplication.OnExecuteBegin(int cExecutingPackages)
        {
            ExecuteBeginEventArgs args = new ExecuteBeginEventArgs(cExecutingPackages);
            this.OnExecuteBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnExecutePackageBegin(string wzPackageId, bool fExecute)
        {
            ExecutePackageBeginEventArgs args = new ExecutePackageBeginEventArgs(wzPackageId, fExecute);
            this.OnExecutePackageBegin(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnExecutePatchTarget(string wzPackageId, string wzTargetProductCode)
        {
            ExecutePatchTargetEventArgs args = new ExecutePatchTargetEventArgs(wzPackageId, wzTargetProductCode);
            this.OnExecutePatchTarget(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnError(ErrorType errorType, string wzPackageId, int dwCode, string wzError, int dwUIHint, int cData, string[] rgwzData, int nRecommendation)
        {
            ErrorEventArgs args = new ErrorEventArgs(errorType, wzPackageId, dwCode, wzError, dwUIHint, rgwzData, nRecommendation);
            this.OnError(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnProgress(int dwProgressPercentage, int dwOverallPercentage)
        {
            ProgressEventArgs args = new ProgressEventArgs(dwProgressPercentage, dwOverallPercentage);
            this.OnProgress(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnExecuteMsiMessage(string wzPackageId, InstallMessage mt, int uiFlags, string wzMessage, int cData, string[] rgwzData, int nRecommendation)
        {
            ExecuteMsiMessageEventArgs args = new ExecuteMsiMessageEventArgs(wzPackageId, mt, uiFlags, wzMessage, rgwzData, nRecommendation);
            this.OnExecuteMsiMessage(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnExecuteFilesInUse(string wzPackageId, int cFiles, string[] rgwzFiles)
        {
            ExecuteFilesInUseEventArgs args = new ExecuteFilesInUseEventArgs(wzPackageId, rgwzFiles);
            this.OnExecuteFilesInUse(args);

            return args.Result;
        }

        Result IBootstrapperApplication.OnExecutePackageComplete(string wzPackageId, int hrExitCode, ApplyRestart restart, int nRecommendation)
        {
            ExecutePackageCompleteEventArgs args = new ExecutePackageCompleteEventArgs(wzPackageId, hrExitCode, restart, nRecommendation);
            this.OnExecutePackageComplete(args);

            return args.Result;
        }

        void IBootstrapperApplication.OnExecuteComplete(int hrStatus)
        {
            this.OnExecuteComplete(new ExecuteCompleteEventArgs(hrStatus));
        }

        Result IBootstrapperApplication.OnApplyComplete(int hrStatus, ApplyRestart restart)
        {
            ApplyCompleteEventArgs args = new ApplyCompleteEventArgs(hrStatus, restart);
            this.OnApplyComplete(args);

            this.applying = false;

            return args.Result;
        }

        Result IBootstrapperApplication.OnExecuteProgress(string wzPackageId, int dwProgressPercentage, int dwOverallPercentage)
        {
            ExecuteProgressEventArgs args = new ExecuteProgressEventArgs(wzPackageId, dwProgressPercentage, dwOverallPercentage);
            this.OnExecuteProgress(args);

            return args.Result;
        }

        void IBootstrapperApplication.OnLaunchApprovedExeComplete(int hrStatus, int processId)
        {
            this.OnLaunchApprovedExeComplete(new LaunchApprovedExeCompleteArgs(hrStatus, processId));
        }

        #endregion
    }
}
