//---------------------------------------------------------------------
// <copyright file="InstallProgressCounter.cs" company="Outercurve Foundation">
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
    using Microsoft.Deployment.WindowsInstaller;

    /// <summary>
    /// Tracks MSI progress messages and converts them to usable progress.
    /// </summary>
    public class InstallProgressCounter
    {
        private int total;
        private int completed;
        private int step;
        private bool moveForward;
        private bool enableActionData;
        private int progressPhase;
        private double scriptPhaseWeight;

        public InstallProgressCounter() : this(0.3)
        {
        }

        public InstallProgressCounter(double scriptPhaseWeight)
        {
            if (!(0 <= scriptPhaseWeight && scriptPhaseWeight <= 1))
            {
                throw new ArgumentOutOfRangeException("scriptPhaseWeight");
            }

            this.scriptPhaseWeight = scriptPhaseWeight;
        }

        /// <summary>
        /// Gets a number between 0 and 1 that indicates the overall installation progress.
        /// </summary>
        public double Progress { get; private set; }

        public void ProcessMessage(InstallMessage messageType, Record messageRecord)
        {
            // This MSI progress-handling code was mostly borrowed from burn and translated from C++ to C#.

            switch (messageType)
            {
                case InstallMessage.ActionStart:
                    if (this.enableActionData)
                    {
                        this.enableActionData = false;
                    }
                    break;

                case InstallMessage.ActionData:
                    if (this.enableActionData)
                    {
                        if (this.moveForward)
                        {
                            this.completed += this.step;
                        }
                        else
                        {
                            this.completed -= this.step;
                        }

                        this.UpdateProgress();
                    }
                    break;

                case InstallMessage.Progress:
                    this.ProcessProgressMessage(messageRecord);
                    break;
            }
        }

        private void ProcessProgressMessage(Record progressRecord)
        {
            // This MSI progress-handling code was mostly borrowed from burn and translated from C++ to C#.

            if (progressRecord == null || progressRecord.FieldCount == 0)
            {
                return;
            }

            int fieldCount = progressRecord.FieldCount;
            int progressType = progressRecord.GetInteger(1);
            string progressTypeString = String.Empty;
            switch (progressType)
            {
                case 0: // Master progress reset
                    if (fieldCount < 4)
                    {
                        return;
                    }

                    this.progressPhase++;

                    this.total = progressRecord.GetInteger(2);
                    if (this.progressPhase == 1)
                    {
                        // HACK!!! this is a hack courtesy of the Windows Installer team. It seems the script planning phase
                        // is always off by "about 50".  So we'll toss an extra 50 ticks on so that the standard progress
                        // doesn't go over 100%.  If there are any custom actions, they may blow the total so we'll call this
                        // "close" and deal with the rest.
                        this.total += 50;
                    }

                    this.moveForward = (progressRecord.GetInteger(3) == 0);
                    this.completed = (this.moveForward ? 0 : this.total); // if forward start at 0, if backwards start at max
                    this.enableActionData = false;

                    this.UpdateProgress();
                    break;

                case 1: // Action info
                    if (fieldCount < 3)
                    {
                        return;
                    }

                    if (progressRecord.GetInteger(3) == 0)
                    {
                        this.enableActionData = false;
                    }
                    else
                    {
                        this.enableActionData = true;
                        this.step = progressRecord.GetInteger(2);
                    }
                    break;

                case 2: // Progress report
                    if (fieldCount < 2 || this.total == 0 || this.progressPhase == 0)
                    {
                        return;
                    }

                    if (this.moveForward)
                    {
                        this.completed += progressRecord.GetInteger(2);
                    }
                    else
                    {
                        this.completed -= progressRecord.GetInteger(2);
                    }

                    this.UpdateProgress();
                    break;

                case 3: // Progress total addition
                    this.total += progressRecord.GetInteger(2);
                    break;
            }
        }

        private void UpdateProgress()
        {
            if (this.progressPhase < 1 || this.total == 0)
            {
                this.Progress = 0;
            }
            else if (this.progressPhase == 1)
            {
                this.Progress = this.scriptPhaseWeight * Math.Min(this.completed, this.total) / this.total;
            }
            else if (this.progressPhase == 2)
            {
                this.Progress = this.scriptPhaseWeight +
                    (1 - this.scriptPhaseWeight) * Math.Min(this.completed, this.total) / this.total;
            }
            else
            {
                this.Progress = 1;
            }
        }
    }
}
