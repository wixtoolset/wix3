//-----------------------------------------------------------------------
// <copyright file="Frost.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// <summary>Defines a Frost object</summary>
//-----------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Test.Frost.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Xml;
    using System.Reflection;
    using System.IO;

    using Microsoft.Tools.WindowsInstallerXml.Test.Frost;

    /// <summary>
    /// Fake Burn engine. Allows for control of actions to take and values to return from 
    /// normal engine calls.
    /// </summary>
    public class Frost
    {
        private static readonly string ConfigFilename = "FrostManifest.xml";

        private static object QueueLockObject = new object();
        private static object AsyncStatusLockObject = new object();
        private static int AsyncCallQty = Enum.GetNames(typeof(AsyncCallIDs)).Length + (int)AsyncCallIDs.NoOp;

        private enum AsyncCallIDs { NoOp = -1, Detect, Plan, Apply, Suspend, Reboot };
        private enum AsyncCallState { Waiting, Running, Done };
        private AsyncCallState[] AsyncCallStatus;
        private Queue<AsyncCallIDs> AsyncCallsQueue;

        private Thread AsyncManagerThread;

        private bool RunMessageManager;
        private bool StillRunning;
        private XmlDocument TestCaseManifest;
        private XmlNode TestCaseEngineDefinitionNode;
        private XmlNode TestCasePackagesNode;

        private AsyncCallIDs MessageQueueCalls
        {
            get
            {
                lock(QueueLockObject)
                {
                    if(this.AsyncCallsQueue.Count == 0)
                    {
                        return AsyncCallIDs.NoOp;
                    }

                    return this.AsyncCallsQueue.Dequeue();
                }
            }
            set
            {
                lock(QueueLockObject)
                {
                    this.AsyncCallsQueue.Enqueue(value);
                }
            }
        }

        //Init values
        private SETUP_ACTION InitCommandAction;
        private SETUP_DISPLAY InitCommandDisplay;
        private SETUP_RESTART InitCommandRestart;
        private SETUP_RESUME InitCommandResume;
        private int InitCommandShow;

        private XmlNode DetectExternalCallDescriptor;
        private XmlNode PlanExternalCallDescriptor;
        private XmlNode ApplyExternalCallDescriptor;
        private XmlNode SuspendExternalCallDescriptor;
        private XmlNode RebootExternalCallDescriptor;

        public SETUP_ACTION PlanCallArgument;
        public static Logger EngineLogger;
        public IFrostUserExperience BurnUXInterface;
        public Package[] Packages;
        public Variables TestEnvironment;
        public string CommandLineArguments;

        /// <summary>
        /// Creates an engine instance that reads the first TestCase node in the manifest
        /// </summary>
        public Frost() : this("") { }
        
        /// <summary>
        /// Creates an engine instance with the specified test case identifier
        /// </summary>
        /// <param name="TestCaseID">The ID of the TestCase node in the manifest. (this is an attribute in the node)</param>
        public Frost(string TestCaseID)
        {
            StillRunning = false;
            TestEnvironment = new Variables();
            TestCaseManifest = new XmlDocument();

            if (!System.IO.File.Exists(ConfigFilename))
            {
                throw new FrostConfigException("Unable to find file \"", ConfigFilename, "\"");
            }

            try
            {
                TestCaseManifest.Load(ConfigFilename);
            }
            catch (XmlException e)
            {
                throw new FrostConfigException("The config file has an XML error", Environment.NewLine, e.ToString());
            }

            SetupLogger(TestCaseManifest.DocumentElement.SelectSingleNode("Logger"));

            AsyncCallStatus = new AsyncCallState[AsyncCallQty];
            
            this.SwitchTestCase(TestCaseID);

            //Non-blocking calls: these are handled by the worker thread
            CFrostEngine.DetectEvent += this.DetectEventDelegateFunction;

            CFrostEngine.PlanEvent += this.PlanEventDelegateFunction;

            CFrostEngine.ApplyEvent += this.ApplyEventDelegateFunction;

            CFrostEngine.SuspendEvent += this.SuspendEventDelegateFunction;

            CFrostEngine.RebootEvent += this.RebootEventDelegateFunction;

            //Blocking calls. These are defined straight in the engine, and executed in the main thread

            CFrostEngine.ElevateEvent += this.ElevateEventDelegateFunction;

            CFrostEngine.GetPackageCountEvent += this.GetPackageCountEventDelegateFunction;

            CFrostEngine.GetCommandLineEvent += this.GetStringEventDelegateFunction;

            CFrostEngine.GetVariableNumericEvent += this.GetVariableNumericDelegateFunction;
            
            CFrostEngine.GetVariableStringEvent += this.GetVariableStringDelegateFunction;

            CFrostEngine.GetVariableVersionEvent += this.GetVariableVersionDelegateFunction;

            CFrostEngine.SetVariableNumericEvent += this.SetVariableNumericDelegateFunction;
        
            CFrostEngine.SetVariableStringEvent += this.SetVariableStringDelegateFunction;
        
            CFrostEngine.SetVariableVersionEvent += this.SetVariableVersionDelegateFunction;
            
            CFrostEngine.FormatStringEvent += this.FormatStringDelegateFunction;
            
            CFrostEngine.EscapeStringEvent += this.EscapeStringDelegateFunction;
        
            CFrostEngine.EvaluateConditionEvent += this.EvaluateConditionEventFunction;
            
            CFrostEngine.LogEvent += this.LogDelegateFunction;

            CFrostEngine.SetSourceEvent += this.SetSourceDelegateFunction;

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Frost object created");
        }

        /// <summary>
        /// Flag that is set when the engine calls StartUXInterface (true) or StopUXInterface (False)
        /// Defaults to false
        /// </summary>
        public bool isRunning
        {
            get
            {
                return StillRunning;
            }
        }
        
        /// <summary>
        /// Checks to see if the worker thread is executing on Detect, Plan or Apply
        /// </summary>
        public bool isRunningThreads
        {
            get
            {
                lock (AsyncStatusLockObject)
                {
                    for (int i = 0; i < AsyncCallQty; i++)
                    {
                        if (this.AsyncCallStatus[i] == AsyncCallState.Running)
                        {
                            return true;
                        }
                    }

                    return false;
                }
            }
        }
        
        /// <summary>
        /// Check to see if worker thread has executed all calls (Detect, Plan and Apply)
        /// </summary>
        public bool isDone
        {
            get
            {
                lock (AsyncStatusLockObject)
                {
                    for (int i = 0; i < AsyncCallQty; i++)
                    {
                        if (this.AsyncCallStatus[i] != AsyncCallState.Done)
                        {
                            return false;
                        }
                    }

                    return true;
                }
            }
        }

        /// <summary>
        /// Switches the engine to the specified test case, resetting all the used values
        /// </summary>
        /// <param name="TestCaseID">the ID of the test case in the manifest (this is an attribute in the node)</param>
        /// <returns>true if the test case is switched, false if the engine is running and no change is done</returns>
        public bool SwitchTestCase(string TestCaseID)
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Switching Test case");
            EngineLogger.WriteLog(LoggingLevel.INFO, "Target test case to switch to: ", TestCaseID);

            if (this.StillRunning)
            {
                EngineLogger.WriteLog(LoggingLevel.ERROR, "The engine is still running, cannot switch test case");
                return false;
            }

            XmlNode TargetTestCase;
            if (string.IsNullOrEmpty(TestCaseID))
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Picking first TestCase in manifest");
                TargetTestCase = this.TestCaseManifest.DocumentElement.SelectSingleNode("TestCase");
            }
            else
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Picking ", TestCaseID, " TestCase");
                TargetTestCase = this.TestCaseManifest.DocumentElement.SelectSingleNode(string.Concat("TestCase[@ID='", TestCaseID, "']"));
            }

            if (TargetTestCase == null)
            {
                EngineLogger.WriteLog(LoggingLevel.ERROR, "Did not find specified test case");
                if (string.IsNullOrEmpty(TestCaseID))
                {
                    throw new FrostConfigException("Manifest does not define any TestCase node");
                }

                throw new FrostConfigException("Manifest does not contain test case ", TestCaseID);
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up packages");
            XmlNodeList TargetPackageNodes = TargetTestCase.SelectNodes("Packages/Package");
            this.TestCasePackagesNode = TargetTestCase.SelectSingleNode("Packages");
            EngineLogger.WriteLog(LoggingLevel.INFO, "Number of packages: ", TargetPackageNodes.Count);

            this.Packages = new Package[TargetPackageNodes.Count];

            for (int i = 0; i < this.Packages.Length; i++)
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up package #", i + 1);
                this.Packages[i] = new Package(TargetPackageNodes[i]);
                EngineLogger.WriteLog(LoggingLevel.INFO, this.Packages[i]);
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up engine values");
            this.TestCaseEngineDefinitionNode = TargetTestCase.SelectSingleNode("EngineValues");
            if (this.TestCaseEngineDefinitionNode == null)
            {
                throw new FrostConfigException("The test case does not define a EngineValues node");
            }

            this.SetupEngineVariables();

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up Variables");
            XmlNodeList TargetVariableNodes = TargetTestCase.SelectNodes("Variables/Variable");

            foreach (XmlNode ThisVariable in TargetVariableNodes)
            {
                this.TestEnvironment[ThisVariable.Attributes["Name"].Value] = Variables.ValueParser(ThisVariable);
                EngineLogger.WriteLog(LoggingLevel.INFO, "Variable Name: ", ThisVariable.Attributes["Name"].Value, Environment.NewLine,
                                                         "Variable Value: ", ThisVariable.InnerText, Environment.NewLine);
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up the Async call status and worker thread.");
            for (int i = 0; i < AsyncCallQty; i++)
            {
                this.AsyncCallStatus[i] = AsyncCallState.Waiting;
            }

            this.RunMessageManager = false;
            this.AsyncCallsQueue = new Queue<AsyncCallIDs>();
            this.AsyncManagerThread = new Thread(new ThreadStart(this.AsyncFunctionsManager));

            return true;
        }

        /// <summary>
        /// Starts the engine with the specified InitCommand values in the manifest's test case.
        /// </summary>
        public void StartUXInterface()
        {
            SETUP_COMMAND InitCommand = new SETUP_COMMAND();
            InitCommand.action = this.InitCommandAction;
            InitCommand.display = this.InitCommandDisplay;
            InitCommand.restart = this.InitCommandRestart;

            StartUXInterface(InitCommand);
        }

        /// <summary>
        /// Starts the engine with the arguments passed
        /// </summary>
        /// <param name="TheAction">The type of init action the engine will start with</param>
        /// <param name="TheDisplay">The way the engine will initialize the display</param>
        /// <param name="TheRestart">???</param>
        public void StartUXInterface(SETUP_ACTION TheAction, SETUP_DISPLAY TheDisplay, SETUP_RESTART TheRestart)
        {
            SETUP_COMMAND InitCommand = new SETUP_COMMAND();
            InitCommand.action = TheAction;
            InitCommand.display = TheDisplay;
            InitCommand.restart = TheRestart;

            StartUXInterface(InitCommand);
        }

        /// <summary>
        /// Starts the engine with the specified SETUP_COMMAND object
        /// </summary>
        /// <param name="TheCommand">The init command object that contains the Action, Display and Restart enum values</param>
        public void StartUXInterface(SETUP_COMMAND TheCommand)
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Starting worker thread");
            this.StillRunning = true;
            this.RunMessageManager = true;
            this.AsyncManagerThread.Start();

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling CFrostEngine.CreateUX(0, SETUP_COMMAND, ref IFrostUserExperience)");
            EngineLogger.WriteLog(LoggingLevel.INFO, "SETUP_COMMAND.action: ", TheCommand.action, Environment.NewLine);
            EngineLogger.WriteLog(LoggingLevel.INFO, "SETUP_COMMAND.display: ", TheCommand.display, Environment.NewLine);
            EngineLogger.WriteLog(LoggingLevel.INFO, "SETUP_COMMAND.restart: ", TheCommand.restart, Environment.NewLine);
            HRESULTS CreateUXReturn = CFrostEngine.CreateUX(0, TheCommand, ref this.BurnUXInterface);

            if (CreateUXReturn != HRESULTS.HR_S_OK)
            {
                throw new FrostException("Error while calling UXTestWrapper.CreateUX() returned ", CreateUXReturn.ToString());
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.Initialize(int, SETUP_RESUME)");
            EngineLogger.WriteLog(LoggingLevel.INFO, "InitCommandShow: ", this.InitCommandShow);
            EngineLogger.WriteLog(LoggingLevel.INFO, "InitCommandResume: ", this.InitCommandResume);
            HRESULTS UXInitReturn = BurnUXInterface.Initialize(this.InitCommandShow, this.InitCommandResume);

            if (UXInitReturn != HRESULTS.HR_S_OK)
            {
                throw new FrostException("Error while calling IBurnUX.Initialize(", this.InitCommandShow.ToString(), ") returned ", UXInitReturn.ToString());
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.Run()");
            HRESULTS UXRunReturn = BurnUXInterface.Run();

            if (UXInitReturn != HRESULTS.HR_S_OK)
            {
                throw new FrostException("Error while calling IBURNUX.Run() returned ", UXRunReturn.ToString());
            }
        }

        /// <summary>
        /// Stops the engine, forcing the worker thread to stop even if it's running
        /// </summary>
        public void StopUXInterface()
        {
            StopUXInterface(true);
        }

        /// <summary>
        /// Atempts to stop the engine, based on the parameter passed
        /// </summary>
        /// <param name="Forced">(true)forces the worker thread to end if it is running a process</param>
        /// <returns>true if it stops the engine, false if there was a process running and not forced to stop</returns>
        public bool StopUXInterface(bool Forced)
        {
            if (this.isRunning)
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Stopping UX Interface");
                if (this.isRunningThreads)
                {
                    if (Forced)
                    {
                        EngineLogger.WriteLog(LoggingLevel.TRACE, "Forcing Working thread closed");
                        this.AsyncManagerThread.Abort();
                    }
                    else
                    {
                        EngineLogger.WriteLog(LoggingLevel.ERROR, "Unable to stop UX because thread is still running");
                        return false;
                    }
                }

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.Uninitialize()");
                this.BurnUXInterface.Uninitialize();
                this.RunMessageManager = false;

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Waiting for worker thread to quit");
                while (this.AsyncManagerThread.ThreadState == ThreadState.Running)
                {
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Waiting 0.1 seconds");
                    Thread.Sleep(100);
                }

                this.CommandLineArguments = String.Empty;
                this.StillRunning = false;
            }
            return true;
        }

        private void DetectEventDelegateFunction(object Caller, ResultReturnArgs RRArgs)
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Queueing a Detect call");
            this.MessageQueueCalls = AsyncCallIDs.Detect;

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining the ReturnValue for Detect delegate");
            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("Detect/DelegateReturnValue", HRESULTS.HR_S_OK);

            EngineLogger.WriteLog(LoggingLevel.INFO, "Return Value: ", RetVal);

            RRArgs.ResultToReturn = RetVal;
        }

        private void DetectEventInternalFunction()
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Starting Detect call internal function");
            this.AsyncCallStatus[(int)AsyncCallIDs.Detect] = AsyncCallState.Running;

            if (this.DetectExternalCallDescriptor == null)
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Doing default action");
                OptionalNodeProcessing("Detect/OnBegin", null);

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnDetectBegin(uint)");
                EngineLogger.WriteLog(LoggingLevel.INFO, "Number of packages: ", this.Packages.Length);
                
                this.BurnUXInterface.OnDectectBegin((uint)this.Packages.Length);
                
                HRESULTS OverallDetectionResult = HRESULTS.HR_S_OK;
                for (uint i = 0; i < this.Packages.Length; i++)
                {
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Processing package #", i);
                    OptionalNodeProcessing(this.Packages[i].PackageID, "OnDetectBegin", null);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnDetectPackageBegin(string)");
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Package ID: ", this.Packages[i].PackageID);
                    this.BurnUXInterface.OnDetectPackageBegin(this.Packages[i].PackageID);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining a return value for OnDetectPackageComplete");
                    HRESULTS PackageDetectionStatus = (HRESULTS)OptionalNodeProcessing(this.Packages[i].PackageID, "OnDetectComplete", HRESULTS.HR_S_OK);

                    this.BurnUXInterface.OnDetectPackageComplete(this.Packages[i].PackageID, PackageDetectionStatus, this.Packages[i].CurrentState);

                    if (OverallDetectionResult == HRESULTS.HR_S_OK && PackageDetectionStatus != HRESULTS.HR_S_OK)
                    {
                        OverallDetectionResult = PackageDetectionStatus;
                    }
                }

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining a return value for OnDetectComplete()");
                OverallDetectionResult = (HRESULTS)OptionalNodeProcessing("Detect/OnComplete", OverallDetectionResult);

                this.BurnUXInterface.OnDetectComplete(OverallDetectionResult);
            }
            else
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Using external assembly");
                this.ExternalFunctionCall(this.DetectExternalCallDescriptor);
            }

            this.AsyncCallStatus[(int)AsyncCallIDs.Detect] = AsyncCallState.Done;
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Detect internal function complete");
        }

        private void PlanEventDelegateFunction(object Caller, SetupActionArgs SAArgs)
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Queueing a Plan call");
            this.MessageQueueCalls = AsyncCallIDs.Plan;
            this.PlanCallArgument = SAArgs.SetupAction;

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining return value for Delegate call");
            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("Plan/DelegateReturnValue", HRESULTS.HR_S_OK);

            SAArgs.ResultToReturn = RetVal;
        }

        private void PlanEventInternalFunction()
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Starting Plan internal function call");
            this.AsyncCallStatus[(int)AsyncCallIDs.Plan] = AsyncCallState.Running;

            if (this.PlanExternalCallDescriptor == null)
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Using default function");
                OptionalNodeProcessing("Plan/OnBegin", null);

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnPlanBegin()");
                EngineLogger.WriteLog(LoggingLevel.INFO, "Number of packages: ", this.Packages.Length);
                this.BurnUXInterface.OnPlanBegin((uint)this.Packages.Length);
                
                HRESULTS PlanOverallCompleteStatus = HRESULTS.HR_S_OK;
                for (int i = 0; i < this.Packages.Length; i++)
                {
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Processing package #", i);
                    OptionalNodeProcessing(this.Packages[i].PackageID, "OnPlanBegin", null);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnPlanPackageBegin()");
                    EngineLogger.WriteLog(LoggingLevel.INFO, "PackageID: ", this.Packages[i].PackageID);
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Init Package RequestedState: ", this.Packages[i].RequestedState);
                    this.BurnUXInterface.OnPlanPackageBegin(this.Packages[i].PackageID, ref this.Packages[i].RequestedState);

                    EngineLogger.WriteLog(LoggingLevel.INFO, "Final Package RequestedState: ", this.Packages[i].RequestedState);

                    this.Packages[i].DefineExecuteAndRollbackActions();

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up return value for OnPlanPackageComplete()");
                    HRESULTS PackageCompleteResult = (HRESULTS)OptionalNodeProcessing(this.Packages[i].PackageID, "OnPlanComplete", HRESULTS.HR_S_OK);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnPlanPackageComplete");
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Package ID: ", this.Packages[i].PackageID);
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Package Complete Result: ", PackageCompleteResult);
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Package Current State: ", this.Packages[i].CurrentState);
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Package Requested State: ", this.Packages[i].RequestedState);
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Package Execute State: ", this.Packages[i].ExecuteState);
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Package Rollback state: ", this.Packages[i].RollbackState);
                    this.BurnUXInterface.OnPlanPackageComplete(this.Packages[i].PackageID,
                                                               PackageCompleteResult,
                                                               this.Packages[i].CurrentState,
                                                               this.Packages[i].RequestedState,
                                                               this.Packages[i].ExecuteState,
                                                               this.Packages[i].RollbackState);

                    if (PlanOverallCompleteStatus == HRESULTS.HR_S_OK && PackageCompleteResult != HRESULTS.HR_S_OK)
                    {
                        EngineLogger.WriteLog(LoggingLevel.TRACE, "Changing default Plan Complete return value");
                        EngineLogger.WriteLog(LoggingLevel.INFO, "Value to change: ", PackageCompleteResult);
                        PlanOverallCompleteStatus = PackageCompleteResult;
                    }
                }

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Reading Plan complete return value");
                PlanOverallCompleteStatus = (HRESULTS)OptionalNodeProcessing("Plan/OnComplete", PlanOverallCompleteStatus);

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnPlanComplete()");
                EngineLogger.WriteLog(LoggingLevel.INFO, "Hresult: ", PlanOverallCompleteStatus);
                this.BurnUXInterface.OnPlanComplete(PlanOverallCompleteStatus);
            }
            else
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Using external function");
                this.ExternalFunctionCall(this.PlanExternalCallDescriptor);
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Plan call complete");
            this.AsyncCallStatus[(int)AsyncCallIDs.Plan] = AsyncCallState.Done;
        }
        
        private void ApplyEventDelegateFunction(object Caller, ResultReturnArgs RRArgs)
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Queueing an Event call");
            this.MessageQueueCalls = AsyncCallIDs.Apply;

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining the value for delegate return value");
            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("Apply/DelegateReturnValue", HRESULTS.HR_S_OK);

            RRArgs.ResultToReturn = RetVal;
        }

        private void ApplyEventInternalFunction()
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Starting Apply internal function");
            this.AsyncCallStatus[(int)AsyncCallIDs.Apply] = AsyncCallState.Running;

            if (this.ApplyExternalCallDescriptor == null)
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Doing default function");
                OptionalNodeProcessing("Apply/OnBegin", null);

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnApplyBegin()");
                this.BurnUXInterface.OnApplyBegin();


                EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up sleep for Registration calls");
                HRESULTS RegistrationCompleteStatus = HRESULTS.HR_S_OK;
                OptionalNodeProcessing("Apply/RegistrationBegin", null);

                //TODO: How do I tell if I do Register vs Unregister?
                if (this.InitCommandAction == SETUP_ACTION.SETUP_ACTION_UNINSTALL)
                {
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnUnregisterBegin()");
                    this.BurnUXInterface.OnUnregisterBegin();

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining value for UnregisterComplete HResult");
                    RegistrationCompleteStatus = (HRESULTS)OptionalNodeProcessing("Apply/RegistrationComplete", HRESULTS.HR_S_OK);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnUnregisterComplete()");
                    this.BurnUXInterface.OnUnregisterComplete(RegistrationCompleteStatus);
                }
                else
                {
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnRegisterBegin()");
                    this.BurnUXInterface.OnRegisterBegin();

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining value for RegisterComplete HResult");
                    RegistrationCompleteStatus = (HRESULTS)OptionalNodeProcessing("Apply/RegistrationComplete", HRESULTS.HR_S_OK);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "CAlling IFrostUserExperience.OnRegisterComplete()");
                    this.BurnUXInterface.OnRegisterComplete(RegistrationCompleteStatus);
                }

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining sleep for OnExecuteBegin()");
                OptionalNodeProcessing("Apply/ExecuteBegin", null);

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnExecuteBegin()");
                EngineLogger.WriteLog(LoggingLevel.INFO, "Number of packages: ", this.Packages.Length);
                this.BurnUXInterface.OnExecuteBegin((uint)this.Packages.Length);

                uint ProgressTotal = 0;
                bool DoRestart = false;
                for (int i = 0; i < this.Packages.Length; i++)
                {
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Processing package #", i);
                    OptionalNodeProcessing(this.Packages[i].PackageID, "OnExecuteBegin", null);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calculating value for fExecute");
                    bool fExecute = (this.Packages[i].ExecuteState != PKG_ACTION_STATE.PKG_ACTION_STATE_NONE &&
                                     this.Packages[i].ExecuteState != PKG_ACTION_STATE.PKG_ACTION_STATE_UNINSTALL);

                    fExecute = (bool)OptionalNodeProcessing(this.Packages[i].PackageID, "RequiresRestart", fExecute);

                    //TODO: How do I know to pass true/false to OnExecutePackageBegin?
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnExecutePackageBegin()");
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Package id: ", this.Packages[i].PackageID);
                    EngineLogger.WriteLog(LoggingLevel.INFO, "fExecute: ", fExecute);
                    this.BurnUXInterface.OnExecutePackageBegin(this.Packages[i].PackageID, fExecute);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Checking OnCache complete value/execution");
                    //TODO: Can Cache happen if requested state is not Cache?
                    //TODO: Cache only happens if ACtionState for package is Recache?
                    string PackageCacheXPath = String.Concat("Package[@ID='", this.Packages[i].PackageID, "']/OnCacheComplete");
                    XmlNode PackageCacheNode = this.TestCasePackagesNode.SelectSingleNode(PackageCacheXPath);
                    if (PackageCacheNode != null)
                    {
                        HRESULTS CacheResult = (HRESULTS)OptionalNodeProcessing(PackageCacheNode, HRESULTS.HR_S_OK);
                        EngineLogger.WriteLog(LoggingLevel.TRACE, "OnCacheComplete defined, calling IFrostUserExperience.OnCacheComplete()");
                        EngineLogger.WriteLog(LoggingLevel.INFO, "HResult: ", CacheResult);
                        this.BurnUXInterface.OnCacheComplete(CacheResult);
                    }

                    uint ThisPackageContribution = (uint)OptionalNodeProcessing(this.Packages[i].PackageID, "ProgressContribution", 0);
                    ProgressTotal += ThisPackageContribution;
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnProgress()");
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Current Contribution: ", ThisPackageContribution);
                    EngineLogger.WriteLog(LoggingLevel.INFO, "Progress Total: ", ProgressTotal);
                    this.BurnUXInterface.OnProgress(ThisPackageContribution, ProgressTotal);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting OnExecutePackageComplete result status");
                    HRESULTS PackageCompleteResult = (HRESULTS)OptionalNodeProcessing(this.Packages[i].PackageID, "OnExecuteComplete", HRESULTS.HR_S_OK);

                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnExecutePackageComplete()");
                    EngineLogger.WriteLog(LoggingLevel.INFO, "HResult: ", PackageCompleteResult);
                    this.BurnUXInterface.OnExecutePackageComplete(this.Packages[i].PackageID, PackageCompleteResult);

                    //TODO: Restart required is a per package value?
                    if (!DoRestart && (bool)OptionalNodeProcessing(this.Packages[i].PackageID, "RequiresRestart", false))
                    {
                        EngineLogger.WriteLog(LoggingLevel.TRACE, "Package #", i, " has requested a restart");
                        DoRestart = true;
                    }
                }

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up hresult for OnExecuteComplete");
                HRESULTS ExecuteCompleteResult = (HRESULTS)OptionalNodeProcessing("Apply/ExecuteComplete", HRESULTS.HR_S_OK);

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnExecuteComplete");
                EngineLogger.WriteLog(LoggingLevel.INFO, "HResult: ", ExecuteCompleteResult);
                this.BurnUXInterface.OnExecuteComplete(ExecuteCompleteResult);

                DoRestart = (bool)OptionalNodeProcessing("Apply/OnRestartRequired", DoRestart);

                if (DoRestart)
                {
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnRestartRequired()");
                    this.BurnUXInterface.OnRestartRequired();
                }

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining HResult for OnApplyComplete()");
                HRESULTS ApplyCompleteResult = (HRESULTS)OptionalNodeProcessing("Apply/OnComplete", HRESULTS.HR_S_OK);

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IFrostUserExperience.OnApplyComplete()");
                EngineLogger.WriteLog(LoggingLevel.INFO, "Hresult: ", ApplyCompleteResult);
                this.BurnUXInterface.OnApplyComplete(ApplyCompleteResult);
            }
            else
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling external function");
                this.ExternalFunctionCall(this.ApplyExternalCallDescriptor);
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Apply Internal function complete");
            this.AsyncCallStatus[(int)AsyncCallIDs.Apply] = AsyncCallState.Done;
        }

        private void SuspendEventDelegateFunction(object Caller, ResultReturnArgs RRArgs)
        {
            this.MessageQueueCalls = AsyncCallIDs.Suspend;
            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("Suspend/DelegateReturnValue", HRESULTS.HR_S_OK);
            RRArgs.ResultToReturn = RetVal;
        }

        private void SuspendEventInternalFunction()
        {
            this.AsyncCallStatus[(int)AsyncCallIDs.Suspend] = AsyncCallState.Running;
            
            if (this.SuspendExternalCallDescriptor == null)
            {

            }
            else
            {
                this.ExternalFunctionCall(this.SuspendExternalCallDescriptor);
            }
            
            this.AsyncCallStatus[(int)AsyncCallIDs.Suspend] = AsyncCallState.Done;
        }

        private void RebootEventDelegateFunction(object Caller, ResultReturnArgs RRArgs)
        {
            this.MessageQueueCalls = AsyncCallIDs.Reboot;
            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("Reboot/DelegateReturnValue", HRESULTS.HR_S_OK);
            RRArgs.ResultToReturn = RetVal;
        }

        private void RebootEventInternalFunction()
        {
            this.AsyncCallStatus[(int)AsyncCallIDs.Reboot] = AsyncCallState.Running;
            
            if (this.RebootExternalCallDescriptor == null)
            {
            }
            else
            {
                this.ExternalFunctionCall(this.RebootExternalCallDescriptor);
            }

            this.AsyncCallStatus[(int)AsyncCallIDs.Reboot] = AsyncCallState.Done;
        }
        
        private void ElevateEventDelegateFunction(object Caller, ResultReturnArgs RRArgs)
        {
            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("Elevate/DelegateReturnValue", HRESULTS.HR_S_OK);
            RRArgs.ResultToReturn = RetVal;
        }
        
        private void GetPackageCountEventDelegateFunction(object Caller, PackageCountEventArgs PCEArgs)
        {
            PCEArgs.PackageCount = (uint)OptionalNodeProcessing("GetPackageCount/PackageCount", this.Packages.Length);
            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("GetPackageCount/DelegateReturnValue", HRESULTS.HR_S_OK);
            PCEArgs.ResultToReturn = RetVal;
        }
        
        private void GetStringEventDelegateFunction(object Caller, StringEventArgs SEArgs)
        {
            SEArgs.StringValue = (string)OptionalNodeProcessing("GetString/StringValue", this.CommandLineArguments);
            HRESULTS RetVal =(HRESULTS)OptionalNodeProcessing("GetString/DelegateReturnValue", HRESULTS.HR_S_OK);
            SEArgs.ResultToReturn = RetVal;
        }
        
        private void GetVariableNumericDelegateFunction(object Caller, LongIntEventArgs LIEArgs)
        {
            LIEArgs.Number = (Int64)this.TestEnvironment[LIEArgs.StringValue];
            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("GetVariableNumeric/DelegateReturnValue", HRESULTS.HR_S_OK);
            LIEArgs.ResultToReturn = RetVal;
        }
            
        private void GetVariableStringDelegateFunction(object Caller, StringVariableEventArgs SVEArgs)
        {
            HRESULTS RetVal = HRESULTS.HR_S_OK;

            if (this.TestEnvironment.VariableExists(SVEArgs.StringName))
            {
                SVEArgs.StringValue = (string)this.TestEnvironment[SVEArgs.StringName];
            }
            else
            {
                RetVal = HRESULTS.HR_FAILURE;
            }

            RetVal = (HRESULTS)OptionalNodeProcessing("GetVariableString/DelegateReturnValue", RetVal);
            SVEArgs.ResultToReturn = RetVal;
        }

        private void GetVariableVersionDelegateFunction(object Caller, LongIntEventArgs LIEArgs)
        {
            HRESULTS RetVal = HRESULTS.HR_S_OK;

            if (this.TestEnvironment.VariableExists(LIEArgs.StringValue))
            {
                LIEArgs.Number = (long)this.TestEnvironment[LIEArgs.StringValue];
            }
            else
            {
                RetVal = HRESULTS.HR_FAILURE;
            }

            RetVal = (HRESULTS)OptionalNodeProcessing("GetVariableVersion/DelegateReturnValue", RetVal);
            LIEArgs.ResultToReturn = RetVal;
        }

        private void SetVariableNumericDelegateFunction(object Caller, LongIntEventArgs LIEArgs)
        {
            this.TestEnvironment[LIEArgs.StringValue] = LIEArgs.Number;

            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("SetVariableNumeric/DelegateReturnValue", HRESULTS.HR_S_OK);
            LIEArgs.ResultToReturn = RetVal;
        }

        private void SetVariableStringDelegateFunction(object Caller, StringVariableEventArgs SVEArgs)
        {
            this.TestEnvironment[SVEArgs.StringName] = SVEArgs.StringValue;

            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("SetVariableString/DelegateReturnValue", HRESULTS.HR_S_OK);
            SVEArgs.ResultToReturn = RetVal;
        }
        
        private void SetVariableVersionDelegateFunction(object Caller, LongIntEventArgs LIEArgs)
        {
            this.TestEnvironment[LIEArgs.StringValue] = LIEArgs.Number;

            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("SetVariableVersion/DelegateReturnValue", HRESULTS.HR_S_OK);
            LIEArgs.ResultToReturn = RetVal;
        }
            
        private void FormatStringDelegateFunction(object Caller, FormatStringEventArgs FSEArgs)
        {
        }
            
        private void EscapeStringDelegateFunction(object Caller, FormatStringEventArgs FSEArgs)
        {
        }
        
        private void EvaluateConditionEventFunction(object Caller, ConditionalEventArgs CEArgs)
        {
            CEArgs.EvalResult = EvaluateConditional(CEArgs.StringValue);

            HRESULTS RetVal = (HRESULTS)OptionalNodeProcessing("EvaluateConditional/DelegateReturnValue", HRESULTS.HR_S_OK);
            CEArgs.ResultToReturn = RetVal;
        }
            
        private void LogDelegateFunction(object Caller, LogEventArgs LEArgs)
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, LEArgs.MessageLogLevel, " ", LEArgs.StringValue);
        }

        private void SetSourceDelegateFunction(object Caller, StringEventArgs SEArgs)
        {
        }

        private object RetrieveEnvironmentVariable(string VariableName, object DefaultValue)
        {
            if (!this.TestEnvironment.VariableExists(VariableName))
            {
                return DefaultValue;
            }

            return this.TestEnvironment[VariableName];
        }

        private object RetrieveEnvironmentVariable(string VariableName)
        {
            if (!this.TestEnvironment.VariableExists(VariableName))
            {
                throw new FrostNonExistentVariableException(VariableName);
            }

            return this.TestEnvironment[VariableName];
        }

        /// <summary>
        /// Evaluates a string as a conditional statement.
        /// </summary>
        /// <param name="ConditionArg">the string that describes the condition</param>
        /// <returns>true/false based on the evaluated condition</returns>
        /// <remarks>
        /// Conditional operators are: <![CDATA[=, <>, <, >, <=, >=]]>
        /// Boolean operators are: AND, OR, NOT
        /// Conditionals can be separated with parens
        /// Variable operands can only be Bool, Int and String
        /// Constant operands are string (if encased in ") or ints
        /// Strings not encased in " are considered variable names
        /// Non-existent variable names are treated as empty strings.
        /// Empty strings are false, any other value is true.
        /// There should be no white space between the operator and the operands
        /// String operations are equal/not equal. Any other raises an error
        /// </remarks>
        /// <example>
        /// Variable Values:
        /// TesterTrue : true   (bool)
        /// TesterFalse: false  (bool)
        /// TesterTres : 3      (int)
        /// TesterTree : "Tree"
        /// TesterSimba: "Hakuna Matatta"
        /// 
        /// Conditional strings:
        /// "NonExistentVariable"                                                                  : FALSE
        /// "TesterTree"                                                                           : TRUE
        /// "TesterSimba="Lion King""                                                              : FALSE
        /// "TesterTrue OR TesterFalse"                                                            : TRUE
        /// "((TesterTres=2) OR (TesterTres=1) OR (TesterFalse) OR TesterTree="Chupa Cabra")"      : FALSE
        /// "((TesterTres=4 AND TesterTres=3) OR (TesterTrue))"                                    : TRUE
        /// "((TesterTres=3 AND TesterTree="Tree") AND TesterFalse)"                               : FALSE
        /// "(TesterTrue AND (TesterFalse OR (TesterTres=3 AND TesterTree="Tree"))) AND TesterTrue": TRUE
        /// "((TesterTres=3 AND TesterTree="Tree") OR TesterFalse)"                                : TRUE 
        /// </example>
        private bool EvaluateConditional(string ConditionArg)
        {
            bool LastCheck = false;
            string Condition = ConditionArg.Trim();

            while (!string.IsNullOrEmpty(Condition))
            {
                if (Condition.StartsWith("("))
                {
                    LastCheck = EvaluateConditional(ExtractParenTerm(ref Condition));
                }
                else if (Condition.StartsWith("NOT "))
                {
                    Condition = Condition.Substring(4);

                    if (Condition.StartsWith("("))
                    {
                        LastCheck = !EvaluateConditional(ExtractParenTerm(ref Condition));
                    }
                    else
                    {
                        LastCheck = !EvaluateSingleConditional(ExtractConditionTerm(ref Condition));
                    }
                }
                else
                {
                    if (Condition.StartsWith("("))
                    {
                        LastCheck = EvaluateConditional(ExtractParenTerm(ref Condition));
                    }
                    else
                    {
                        LastCheck = EvaluateSingleConditional(ExtractConditionTerm(ref Condition));
                    }
                }

                if (string.IsNullOrEmpty(Condition))
                {
                    break;
                }

                Condition = Condition.Trim();

                if (Condition.StartsWith("AND "))
                {
                    Condition = Condition.Substring(4);

                    if (LastCheck == false)
                    {
                        return false;
                    }
                }
                else if (Condition.StartsWith("OR "))
                {
                    Condition = Condition.Substring(3);

                    if (LastCheck == true)
                    {
                        return true;
                    }
                }
            }

            return LastCheck;
        }

        private string ExtractParenTerm(ref string ConditionString)
        {
            int ParenCounter = 1;
            int SubstringCounter = 0;

            for (int i = 1; i < ConditionString.Length; i++)
            {
                ++SubstringCounter;
                if (ConditionString[i] == '(')
                { 
                    ++ParenCounter;
                }

                if (ConditionString[i] == ')')
                {
                    --ParenCounter;
                }

                if (ParenCounter == 0)
                {
                    break;
                }
            }

            string RetVal = ConditionString.Substring(1, SubstringCounter - 1);

            if (SubstringCounter + 2 < ConditionString.Length)
            {
                ConditionString = ConditionString.Substring(SubstringCounter + 2);
            }
            else
            {
                ConditionString = "";
            }

            return RetVal;
        }
        
        private string ExtractConditionTerm(ref string ConditionString)
        {
            int Traverser = 0;
            while (ConditionString[Traverser] != ' ' && Traverser < ConditionString.Length)
            {
                if (ConditionString[Traverser] == '"')
                {
                    ++Traverser;
                    while (ConditionString[Traverser] != '"')
                    {
                        ++Traverser;
                    }

                    continue;
                }
                ++Traverser;
            }

            string RetVal = ConditionString.Substring(0, Traverser);

            if (Traverser == ConditionString.Length)
            {
                ConditionString = "";
            }
            else
            {
                ConditionString = ConditionString.Substring(Traverser + 1);
            }

            return RetVal;
        }

        private bool EvaluateSingleConditional(string Condition)
        {
            string[] Operands = null;
            bool RetVal = false;
            int OperatorID = 0;
            string[] Operators = { "=", "<>", "<", ">", "<=", ">=" };

            while (OperatorID < Operators.Length)
            {
                if (Condition.Contains(Operators[OperatorID]))
                {
                    Operands = Condition.Split(new string[] { Operators[OperatorID] }, StringSplitOptions.RemoveEmptyEntries);
                    Operands[0] = Operands[0].Trim();
                    Operands[1] = Operands[1].Trim();
                    break;
                }

                ++OperatorID;
            }

            if (OperatorID >= Operators.Length)
            {
                this.TestEnvironment.VariablesLock.WaitOne();

                if (this.TestEnvironment.VariableExists(Condition))
                {
                    if (this.TestEnvironment[Condition].GetType() == typeof(string) && string.IsNullOrEmpty((string)this.TestEnvironment[Condition]))
                    {
                        RetVal = false;
                    }
                    else if (this.TestEnvironment[Condition].GetType() == typeof(bool))
                    {
                        RetVal = (bool)this.TestEnvironment[Condition];
                    }
                    else
                    {
                        RetVal = true;
                    }
                }
                else
                {
                    RetVal = false;
                }

                this.TestEnvironment.VariablesLock.Release();

                return RetVal;
            }

            this.TestEnvironment.VariablesLock.WaitOne();

            object[] OperandValues = new object[2];
            OperandValues[0] = GetOperandValue(Operands[0]);
            OperandValues[1] = GetOperandValue(Operands[1]);

            if (OperandValues[0].GetType() != OperandValues[1].GetType())
            {
                this.TestEnvironment.VariablesLock.Release();
                throw new FrostException("Type mismatch while trying to perform comparison. Left operand [", Operands[0], " is of type ", OperandValues[0].GetType().ToString(), " and right operand [", Operands[1], " is of type ", OperandValues[1].GetType().ToString());
            }

            if (OperandValues[0].GetType() != typeof(string) && OperandValues[0].GetType() != typeof(int))
            {
                this.TestEnvironment.VariablesLock.Release();
                throw new FrostException("Trying to perform comparison ", Condition, " with one of the operands being of type ", OperandValues[0].GetType().ToString());
            }

            if (Operators[OperatorID] != "=" && Operators[OperatorID] != "<>" && OperandValues[0].GetType() == typeof(string))
            {
                this.TestEnvironment.VariablesLock.Release();
                throw new FrostException("Unsupported operation [", Operators[OperatorID], "] for string type");
            }

            switch (Operators[OperatorID])
            {
                case "=":
                    if (OperandValues[0].GetType() == typeof(string))
                    {
                        RetVal = string.Compare((string)OperandValues[0], (string)OperandValues[1]) == 0;
                    }
                    else
                    {
                        RetVal = (int)OperandValues[0] == (int)OperandValues[1];
                    }
                    break;
                case "<>":
                    if (OperandValues[0].GetType() == typeof(string))
                    {
                        RetVal = !(string.Compare((string)OperandValues[0], (string)OperandValues[1]) == 0);
                    }
                    else
                    {
                        RetVal = (int)OperandValues[0] != (int)OperandValues[1];
                    }
                    break;
                case "<":
                    RetVal = (int)OperandValues[0] < (int)OperandValues[1];
                    break;
                case ">":
                    RetVal = (int)OperandValues[0] > (int)OperandValues[1];
                    break;
                case "<=":
                    RetVal = (int)OperandValues[0] <= (int)OperandValues[1];
                    break;
                case ">=":
                    RetVal = (int)OperandValues[0] >= (int)OperandValues[1];
                    break;
            }

            this.TestEnvironment.VariablesLock.Release();
            return RetVal;
        }

        private object GetOperandValue(string Operand)
        {
            if (Operand.StartsWith("\"") && Operand.EndsWith("\""))
            {
                return Operand.Trim(new char[] { '"' });
            }

            int PotentialRetVal = 0;
            if (int.TryParse(Operand, out PotentialRetVal))
            {
                return PotentialRetVal;
            }

            if (this.TestEnvironment.VariableExists(Operand))
            {
                return this.TestEnvironment[Operand];
            }

            return "";
        }

        private void AsyncFunctionsManager()
        {
            while (this.RunMessageManager)
            {
                AsyncCallIDs ThisCall = this.MessageQueueCalls;
                while (ThisCall != AsyncCallIDs.NoOp)
                {
                    if (this.AsyncCallStatus[(int)ThisCall] == AsyncCallState.Done && AsyncCallRunsOnce(ThisCall))
                    {
                        ThisCall = this.MessageQueueCalls;
                        continue;
                    }

                    lock (AsyncStatusLockObject)
                    {
                        this.AsyncCallStatus[(int)ThisCall] = AsyncCallState.Running;
                    }

                    switch (ThisCall)
                    {
                        case AsyncCallIDs.Detect:
                            this.DetectEventInternalFunction();
                            break;
                        case AsyncCallIDs.Plan:
                            this.PlanEventInternalFunction();
                            break;
                        case AsyncCallIDs.Apply:
                            this.ApplyEventInternalFunction();
                            break;
                        case AsyncCallIDs.Reboot:
                            this.RebootEventInternalFunction();
                            break;
                        case AsyncCallIDs.Suspend:
                            this.SuspendEventInternalFunction();
                            break;
                    }

                    lock (AsyncStatusLockObject)
                    {
                        this.AsyncCallStatus[(int)ThisCall] = AsyncCallState.Done;
                    }

                    ThisCall = this.MessageQueueCalls;
                }

                Thread.Sleep(100);
            }
        }

        private bool AsyncCallRunsOnce(AsyncCallIDs TheID)
        {
            return TheID == AsyncCallIDs.Detect || TheID == AsyncCallIDs.Apply;
        }

        private object OptionalNodeProcessing(string PackageID, string XPath, object DefaultValue)
        {
            string PackageXPath = String.Concat("Package[@ID='", PackageID, "']/", XPath);
            return OptionalNodeProcessing(this.TestCasePackagesNode.SelectSingleNode(PackageXPath), DefaultValue);
        }

        private object OptionalNodeProcessing(string XPath, object DefaultValue)
        {
            return OptionalNodeProcessing(this.TestCaseEngineDefinitionNode.SelectSingleNode(XPath), DefaultValue);
        }

        private object OptionalNodeProcessing(XmlNode TargetNode, object DefaultValue)
        {
            if (TargetNode == null)
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Not changing object. Not taking actions");
                EngineLogger.WriteLog(LoggingLevel.INFO, "Object value: ", DefaultValue);
                return DefaultValue;
            }

            XmlAttribute SleepAttribute = TargetNode.Attributes["Sleep"];
            if (SleepAttribute != null)
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Atempting to sleep");
                EngineLogger.WriteLog(LoggingLevel.INFO, "Sleep value: ", SleepAttribute.Value);
                Int32 SleepCatcher = 0;
                if (Int32.TryParse(SleepAttribute.Value, out SleepCatcher))
                {
                    EngineLogger.WriteLog(LoggingLevel.TRACE, "Sleeping");
                    Thread.Sleep(SleepCatcher);
                }
                else
                {
                    EngineLogger.WriteLog(LoggingLevel.ERROR, "Unable to parse value into sleep. Not perforimg sleep action");
                }
            }

            XmlNode OnErrorNodeSearch = TargetNode.SelectSingleNode("OnError");
            if (OnErrorNodeSearch != null)
            {
                EngineLogger.WriteLog(LoggingLevel.TRACE, "Performing OnError");
                string sPackageID = String.Empty;
                uint Code = 0;
                string sError = string.Empty;
                uint Hint = 0;

                XmlAttribute PackageIDAttribute = TargetNode.ParentNode.Attributes["ID"];
                if (PackageIDAttribute != null)
                {
                    sPackageID = PackageIDAttribute.Value;
                }

                Code = (uint)Variables.ValueParser("OnErrorCode", "UINT", OnErrorNodeSearch.Attributes["Code"].Value);
                Hint = (uint)Variables.ValueParser("OnErrorHint", "UINT", OnErrorNodeSearch.Attributes["Hint"].Value);
                sError = (string)Variables.ValueParser("OnErrorMessage", "STRING", OnErrorNodeSearch.InnerText);

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling IBurnUserExperience.OnError");
                EngineLogger.WriteLog(LoggingLevel.INFO, "PackageID : ", sPackageID);
                EngineLogger.WriteLog(LoggingLevel.INFO, "Error code: ", Code);
                EngineLogger.WriteLog(LoggingLevel.INFO, "Error Hint: ", Hint);
                EngineLogger.WriteLog(LoggingLevel.INFO, "Error Msg : ", sError);
                this.BurnUXInterface.OnError(sPackageID, Code, sError, Hint);
            }

            if (TargetNode.Attributes["Type"] != null)
            {
                if (OnErrorNodeSearch != null && TargetNode.Attributes["Value"] == null)
                {
                    throw new FrostConfigException("The optional node ", TargetNode.Name, " defines a value to overwrite with an OnError node and doesnt use a Value attribute. Value attribute must be used in this case");
                }

                EngineLogger.WriteLog(LoggingLevel.TRACE, "Changing the value of the object");
                return Variables.ValueParser(TargetNode, true);
            }

            return DefaultValue;
        }

        private void SetupLogger(XmlNode LoggerDefinitionNode)
        {
            int LogLevel = 0;
            int LogOutput = 0;
            string LogFileName = String.Empty;
            string LogTimestamp = String.Empty;

            if (LoggerDefinitionNode != null)
            {
                XmlAttribute LogLevelAttribute = LoggerDefinitionNode.Attributes["Level"];
                if (!Int32.TryParse(LogLevelAttribute.Value, out LogLevel))
                {
                    LoggingLevel StringedLevel;
                    try
                    {
                        StringedLevel = (LoggingLevel)Enum.Parse(typeof(LoggingLevel), LogLevelAttribute.Value, true);
                    }
                    catch (Exception)
                    {
                        throw new FrostConfigException("Unable to translate ", LogLevelAttribute.Value, " into LoggingLevel enum");
                    }

                    LogLevel = (int)StringedLevel;
                }

                XmlNode OutputDefinitions = LoggerDefinitionNode.SelectSingleNode("StdOut");
                if (OutputDefinitions != null)
                {
                    LogOutput += (int)Logger.OutputType.STDOUT;
                }

                OutputDefinitions = LoggerDefinitionNode.SelectSingleNode("FileOut");
                if (OutputDefinitions != null)
                {
                    LogOutput += (int)Logger.OutputType.FILE;

                    XmlNode FileValueExtractor = OutputDefinitions.SelectSingleNode("FileName");
                    if (FileValueExtractor == null)
                    {
                        throw new FrostConfigException("Logger definition for File output does not define a FileName node");
                    }

                    LogFileName = (string)Variables.ValueParser(FileValueExtractor);

                    FileValueExtractor = OutputDefinitions.SelectSingleNode("Timestamp");
                    if (FileValueExtractor != null)
                    {
                        LogTimestamp = (string)Variables.ValueParser(FileValueExtractor);
                    }
                }
            }

            Frost.EngineLogger = new Logger(LogOutput, LogFileName, LogLevel, LogTimestamp);

        }

        private void SetupEngineVariables()
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up Init values");
            this.InitCommandAction = (SETUP_ACTION)Variables.ValueParser(this.TestCaseEngineDefinitionNode.SelectSingleNode("InitCommand/Action"), true);
            this.InitCommandDisplay = (SETUP_DISPLAY)Variables.ValueParser(this.TestCaseEngineDefinitionNode.SelectSingleNode("InitCommand/Display"), true);
            this.InitCommandRestart = (SETUP_RESTART)Variables.ValueParser(this.TestCaseEngineDefinitionNode.SelectSingleNode("InitCommand/Restart"), true);
            this.InitCommandResume = (SETUP_RESUME)Variables.ValueParser(this.TestCaseEngineDefinitionNode.SelectSingleNode("InitCommand/Resume"), true);
            this.InitCommandShow = (int)Variables.ValueParser(this.TestCaseEngineDefinitionNode.SelectSingleNode("InitCommand/Show"), true);

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up Detect Values");
            this.DetectExternalCallDescriptor = this.TestCaseEngineDefinitionNode.SelectSingleNode("Detect/ExternalCall");

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up Plan Values");
            this.PlanExternalCallDescriptor = this.TestCaseEngineDefinitionNode.SelectSingleNode("Plan/ExternalCall");

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up apply values");
            this.ApplyExternalCallDescriptor = this.TestCaseEngineDefinitionNode.SelectSingleNode("Apply/ExternalCall");

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up Suspend values");
            this.SuspendExternalCallDescriptor = this.TestCaseEngineDefinitionNode.SelectSingleNode("Suspend/ExternalCall");

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting up Reboot values");
            this.RebootExternalCallDescriptor = this.TestCaseEngineDefinitionNode.SelectSingleNode("Reboot/ExternalCall");
        }

        private void ExternalFunctionCall(XmlNode CallDescriptor)
        {
            EngineLogger.WriteLog(LoggingLevel.TRACE, "Executing external assembly");

            if (CallDescriptor.Attributes["DLL"] == null)
            {
                throw new FrostConfigException("External call descriptor does not define a DLL attribute");
            }

            string DLLName = CallDescriptor.Attributes["DLL"].Value;
            EngineLogger.WriteLog(LoggingLevel.INFO, "Dll name: ", DLLName);

            if (string.IsNullOrEmpty(DLLName))
            {
                throw new FrostConfigException("External call descriptor defines a DLL with empty string");
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining the type that contains the function to call");
            if (CallDescriptor.Attributes["Type"] == null)
            {
                throw new FrostConfigException("External call descriptor does not define a Type attribute");
            }

            string TargetTypeName = CallDescriptor.Attributes["Type"].Value;
            EngineLogger.WriteLog(LoggingLevel.INFO, "Type: ", TargetTypeName);

            if (string.IsNullOrEmpty(TargetTypeName))
            {
                throw new FrostConfigException("External call descriptor defines a Type with empty string");
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining the method name to use");
            if (CallDescriptor.Attributes["Method"] == null)
            {
                throw new FrostConfigException("External call descriptor does not define a Method attribute");
            }

            string TargetMethodName = CallDescriptor.Attributes["Method"].Value;
            EngineLogger.WriteLog(LoggingLevel.INFO, "Method name: ", TargetMethodName);

            if (string.IsNullOrEmpty(TargetMethodName))
            {
                throw new FrostConfigException("External call descriptor defines a Method name with empty string");
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Creating the Assembly object");
            Assembly TargetAssembly;
            if (!System.IO.File.Exists(DLLName))
            {
                throw new FrostConfigException("External call descriptor specifies dll = \"", DLLName, "\" and it doesnt exist");
            }

            try
            {
                TargetAssembly = Assembly.LoadFrom(DLLName);
            }
            catch (Exception e)
            {
                throw new FrostConfigException("Error loading DLL \"", DLLName,  "\":\n", e.ToString());
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Creating the Type object");
            Type TargetType;
            try
            {
                TargetType = TargetAssembly.GetType(TargetTypeName);
            }
            catch (Exception e)
            {
                throw new FrostConfigException("Unable to create type \"", TargetTypeName, "\" for dll ", DLLName, ":\n", e.ToString());
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Instantiating the type");
            object InstantiatedType;
            try
            {
                InstantiatedType = Activator.CreateInstance(TargetType);
            }
            catch (Exception e)
            {
                throw new FrostException("Instantiating object ", TargetType, " threw an exception:", Environment.NewLine, e.ToString());
            }
            if (InstantiatedType == null)
            {
                throw new FrostException("Instantiating a ", TargetType, " returned a null object");
            }

            EngineLogger.WriteLog(LoggingLevel.TRACE, "Calling the method");
            try
            {
                TargetType.InvokeMember(TargetMethodName, BindingFlags.InvokeMethod | BindingFlags.Default, null, InstantiatedType, new object[] { this });
            }
            catch (MethodAccessException)
            {
                throw new FrostException("The external function ", TargetMethodName, " is a class initializer of ", TargetType);
            }
            catch (MissingMethodException)
            {
                throw new FrostException("The external function ", TargetMethodName, " was not found in ", TargetType);
            }
            catch (TargetException)
            {
                throw new FrostException("The extarnal function ", TargetMethodName, " in ", TargetType.ToString(), " cannot be invoked");
            }
            catch (AmbiguousMatchException)
            {
                throw new FrostException("More than one method in ", TargetType.ToString(), " matches ", TargetMethodName, "(Frost)");
            }
        }
    }
}
