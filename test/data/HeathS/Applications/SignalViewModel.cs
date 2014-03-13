//-------------------------------------------------------------------------------------------------
// <copyright file="SignalViewModel.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//
// <summary>
// ViewModel for Signal test controller.
// </summary>
//-------------------------------------------------------------------------------------------------

using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Security.AccessControl;
using System.Threading;
using System.Windows.Input;

namespace Signal
{
    public sealed class SignalViewModel : ObservableObject, IDisposable
    {
        private const int WM_QUERYENDSESSION = 0x11;

        private static readonly IntPtr FALSE = IntPtr.Zero;
        private static readonly IntPtr TRUE = new IntPtr(1);

        private static readonly string SucceedEventName = @"Global\WixWaitForEventSucceed";
        private static readonly string FailEventName = @"Global\WixWaitForEventFail";
        private static readonly string WaitMutexName = @"Global\WixWaitForMutex";

        private EventWaitHandle succeedEvent;
        private EventWaitHandle failEvent;
        private Mutex waitMutex;
        private bool ownsMutex;
        private ObservableCollection<Message> messages;
        private bool topMost;
        private bool preventShutdown;

        public SignalViewModel()
        {
            this.SetSucceedEventCommand = new RelayCommand(
                param => this.SetSucceedEvent()
                    );

            this.SetFailEventCommand = new RelayCommand(
                param => this.SetFailEvent()
                    );

            // Create the events and mutex to signal later.
            this.succeedEvent = SignalViewModel.CreateEvent(SignalViewModel.SucceedEventName, true);
            this.failEvent = SignalViewModel.CreateEvent(SignalViewModel.FailEventName, true);
            this.waitMutex = SignalViewModel.CreateMutex(SignalViewModel.WaitMutexName);

            this.messages = new ObservableCollection<Message>();

            this.TopMost = true;
            this.PreventShutdown = true;
        }

        ~SignalViewModel()
        {
            this.Dispose(false);
        }

        public ICommand SetSucceedEventCommand { get; private set; }
        public ICommand SetFailEventCommand { get; private set; }

        public bool Wait
        {
            get { return this.ownsMutex; }
            set
            {
                if (value)
                {
                    // Attempt to grab the mutex as quickly as possible.
                    if (this.waitMutex.WaitOne(1))
                    {
                        this.ownsMutex = true;
                        this.messages.Add(new Message("Grabbed ownership of the global mutex."));
                    }
                    else
                    {
                        this.ownsMutex = false;
                        this.messages.Add(new Message("Failed to grab ownership of the mutex.", MessageType.Error));
                    }
                }
                else if (this.ownsMutex)
                {
                    this.waitMutex.ReleaseMutex();
                    this.ownsMutex = false;
                    this.messages.Add(new Message("Released ownership of the global mutex."));
                }

                // Force the view to requery in case we didn't get ownership.
                this.OnPropertyChanged("Wait");
            }
        }

        public ICollection Messages
        {
            get { return this.messages; }
        }

        public bool TopMost
        {
            get { return this.topMost; }
            set
            {
                if (value != this.topMost)
                {
                    this.topMost = value;
                    this.OnPropertyChanged("TopMost");
                }
            }
        }

        public bool PreventShutdown
        {
            get { return this.preventShutdown; }
            set
            {
                if (value != this.preventShutdown)
                {
                    this.preventShutdown = value;
                    this.OnPropertyChanged("PreventShutdown");
                }
            }
        }

        internal void SetSucceedEvent()
        {
            this.succeedEvent.Set();
            this.messages.Add(new Message("Set the succeed event."));
        }

        internal void SetFailEvent()
        {
            this.failEvent.Set();
            this.messages.Add(new Message("Set the failed event."));
        }

        internal IntPtr WindowProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            switch (msg)
            {
                case WM_QUERYENDSESSION:
                    handled = true;

                    if (this.preventShutdown)
                    {
                        this.messages.Add(new Message("Denied a request to shut down the application.", MessageType.Warning));
                        return FALSE;
                    }

                    return TRUE;
            }

            handled = false;
            return IntPtr.Zero;
        }

        private void Dispose(bool disposing)
        {
            if (this.ownsMutex)
            {
                try
                {
                    // Attempt to release the mutex if it hasn't already.
                    this.waitMutex.ReleaseMutex();
                }
                catch (ApplicationException)
                {
                }
            }
        }

        void IDisposable.Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        private static EventWaitHandle CreateEvent(string name, bool auto)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            // Allow everyone to signal the event from medium or higher integrity levels.
            EventWaitHandleSecurity security = new EventWaitHandleSecurity();
            SignalViewModel.SetSecurity(security);

            // Create the event without signaling.
            EventResetMode mode = auto ? EventResetMode.AutoReset : EventResetMode.ManualReset;
            bool created = false;
            EventWaitHandle evt = new EventWaitHandle(false, EventResetMode.AutoReset, name, out created, security);

            return evt;
        }

        private static Mutex CreateMutex(string name)
        {
            if (String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException("name");
            }

            // Allow Everyone to signal the mutex from medium or higher integrity levels.
            MutexSecurity security = new MutexSecurity();
            SignalViewModel.SetSecurity(security);

            // Create the mutex without taking ownership.
            bool created = false;
            Mutex mtx = new Mutex(false, name, out created, security);

            return mtx;
        }

        private static void SetSecurity(ObjectSecurity security)
        {
            if (null == security)
            {
                throw new ArgumentNullException("security");
            }

            // Set the DACL.
            security.SetSecurityDescriptorSddlForm("D:(A;;GA;;;WD)", AccessControlSections.Access);

            // Can only set integrity levels in the SACL for Vista and newer.
            Version vista = new Version(6, 0);
            if (vista <= Environment.OSVersion.Version)
            {
                security.SetSecurityDescriptorSddlForm("S:(ML;;NW;;;ME)", AccessControlSections.Audit);
            }
        }

        private sealed class Message : ObservableObject
        {
            private MessageType type;
            private string text;

            internal Message(string text)
                : this(text, MessageType.Information)
            {
            }

            internal Message(string text, MessageType type)
            {
                this.type = type;
                this.DateTime = DateTime.Now;
                this.text = text;
            }

            public MessageType Type
            {
                get;
                private set;
            }

            public DateTime DateTime
            {
                get;
                private set;
            }

            public string Text
            {
                get { return this.text; }
                set
                {
                    this.text = value;
                    this.OnPropertyChanged("Text");
                }
            }
        }

        private enum MessageType
        {
            Information,
            Warning,
            Error
        }
    }
}
