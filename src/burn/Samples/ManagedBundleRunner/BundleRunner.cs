// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Wix.Samples
{
    using System;
    using System.Diagnostics;
    using System.IO.Pipes;
    using System.Text;
    using System.Threading;

    /// <summary>
    /// Runs a bundle with provided command-line.
    /// </summary>
    public class BundleRunner
    {
        /// <summary>
        /// Creates a runner for the provided bundle.
        /// </summary>
        /// <param name="bundle">Path to the bundle to run.</param>
        public BundleRunner(string bundle)
        {
            this.Path = bundle;
        }

        /// <summary>
        /// Fired when the bundle encounters an error.
        /// </summary>
        public event EventHandler<BundleErrorEventArgs> Error;

        /// <summary>
        /// Fired when the bundle progress is udpated.
        /// </summary>
        public event EventHandler<BundleProgressEventArgs> Progress;

        /// <summary>
        /// Gets the path to the bundle to run.
        /// </summary>
        public string Path { get; private set; }

        /// <summary>
        /// Runs the bundle with the provided command-line.
        /// </summary>
        /// <param name="commandLine">Optional command-line to pass to the bundle.</param>
        /// <returns>Exit code from the bundle.</returns>
        public int Run(string commandLine = null)
        {
            WaitHandle[] waits = new WaitHandle[] { new ManualResetEvent(false), new ManualResetEvent(false) };
            int returnCode = 0;
            int pid = Process.GetCurrentProcess().Id;
            string pipeName = String.Concat("bpe_", pid);
            string pipeSecret = Guid.NewGuid().ToString("N");

            using (NamedPipeServerStream pipe = new NamedPipeServerStream(pipeName, PipeDirection.InOut, 1))
            {
                using (Process bundleProcess = new Process())
                {
                    bundleProcess.StartInfo.FileName = this.Path;
                    bundleProcess.StartInfo.Arguments = String.Format("{0} -burn.embedded {1} {2} {3}", commandLine ?? String.Empty, pipeName, pipeSecret, pid);
                    bundleProcess.StartInfo.UseShellExecute = false;
                    bundleProcess.StartInfo.CreateNoWindow = true;
                    bundleProcess.Start();

                    Connect(pipe, pipeSecret, pid, bundleProcess.Id);

                    PumpMessages(pipe);

                    bundleProcess.WaitForExit();
                    returnCode = bundleProcess.ExitCode;
                }
            }

            return returnCode;
        }

        /// <summary>
        /// Called when bundle encounters an error.
        /// </summary>
        /// <param name="e">Additional arguments for this event.</param>
        protected virtual void OnError(BundleErrorEventArgs e)
        {
            EventHandler<BundleErrorEventArgs> handler = this.Error;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        /// <summary>
        /// Called when bundle progress is updated.
        /// </summary>
        /// <param name="e">Additional arguments for this event.</param>
        protected virtual void OnProgress(BundleProgressEventArgs e)
        {
            EventHandler<BundleProgressEventArgs> handler = this.Progress;
            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void Connect(NamedPipeServerStream pipe, string pipeSecret, int pid, int childPid)
        {
            pipe.WaitForConnection();

            WriteSecretToPipe(pipe, pipeSecret);

            WriteNumberToPipe(pipe, (uint)pid);

            uint ack = ReadNumberFromPipe(pipe);
            // This is not true when bundle is run under a debugger
            //if (ack != childPid)
            //{
            //    throw new ApplicationException("Incorrect child process.");
            //}
        }

        private void PumpMessages(NamedPipeServerStream pipe)
        {
            uint messageId;
            while (TryReadNumberFromPipe(pipe, out messageId))
            {
                uint messageSize = ReadNumberFromPipe(pipe);

                BundleResult result = BundleResult.None;
                switch (messageId)
                {
                    case 1: //error
                        result = ProcessErrorMessage(pipe);
                        break;

                    case 2: // progress
                        result = ProcessProgressMessage(pipe);
                        break;

                    default: // unknown message, do nothing.
                        break;
                }

                CompleteMessage(pipe, result);
            }
        }

        private BundleResult ProcessErrorMessage(NamedPipeServerStream pipe)
        {
            BundleErrorEventArgs e = new BundleErrorEventArgs();
            e.Code = (int)ReadNumberFromPipe(pipe);
            e.Message = ReadStringFromPipe(pipe);
            e.UIHint = (int)ReadNumberFromPipe(pipe);

            this.OnError(e);

            return e.Result;
        }

        private BundleResult ProcessProgressMessage(NamedPipeServerStream pipe)
        {
            ReadNumberFromPipe(pipe); // eat the first progress number because it is always zero.

            BundleProgressEventArgs e = new BundleProgressEventArgs();
            e.Progress = (int)ReadNumberFromPipe(pipe);

            this.OnProgress(e);

            return e.Result;
        }

        private void CompleteMessage(NamedPipeServerStream pipe, BundleResult result)
        {
            uint complete = 0xF0000002;
            WriteNumberToPipe(pipe, complete);
            WriteNumberToPipe(pipe, 4); // size of message data
            WriteNumberToPipe(pipe, (uint)result);
        }

        private uint ReadNumberFromPipe(NamedPipeServerStream pipe)
        {
            byte[] buffer = new byte[4];
            pipe.Read(buffer, 0, buffer.Length);
            return BitConverter.ToUInt32(buffer, 0);
        }

        private string ReadStringFromPipe(NamedPipeServerStream pipe)
        {
            uint length = ReadNumberFromPipe(pipe);

            byte[] buffer = new byte[length * 2];
            pipe.Read(buffer, 0, buffer.Length);

            return Encoding.Unicode.GetString(buffer);
        }

        private bool TryReadNumberFromPipe(NamedPipeServerStream pipe, out uint value)
        {
            value = ReadNumberFromPipe(pipe); // reading will not block and return zero if pipe is not connected.
            return pipe.IsConnected;
        }

        private void WriteNumberToPipe(NamedPipeServerStream pipe, uint value)
        {
            byte[] buffer = BitConverter.GetBytes(value);
            pipe.Write(buffer, 0, buffer.Length);
        }

        private void WriteSecretToPipe(NamedPipeServerStream pipe, string secret)
        {
            byte[] buffer = Encoding.Unicode.GetBytes(secret);

            WriteNumberToPipe(pipe, (uint)buffer.Length);
            pipe.Write(buffer, 0, buffer.Length);
        }
    }
}
