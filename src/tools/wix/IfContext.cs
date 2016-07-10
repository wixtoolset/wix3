// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml
{
    using System;

    /// <summary>
    /// Current state of the if context.
    /// </summary>
    public enum IfState
    {
        /// <summary>Context currently in unknown state.</summary>
        Unknown,

        /// <summary>Context currently inside if statement.</summary>
        If,

        /// <summary>Context currently inside elseif statement..</summary>
        ElseIf,

        /// <summary>Conext currently inside else statement.</summary>
        Else,
    }

    /// <summary>
    /// Context for an if statement in the preprocessor.
    /// </summary>
    public sealed class IfContext
    {
        private bool active;
        private bool keep;
        private bool everKept;
        private IfState state;

        /// <summary>
        /// Creates a default if context object, which are used for if's within an inactive preprocessor block
        /// </summary>
        public IfContext()
        {
            this.active = false;
            this.keep = false;
            this.everKept = true;
            this.state = IfState.If;
        }

        /// <summary>
        /// Creates an if context object.
        /// </summary>
        /// <param name="active">Flag if context is currently active.</param>
        /// <param name="keep">Flag if context is currently true.</param>
        /// <param name="state">State of context to start in.</param>
        public IfContext(bool active, bool keep, IfState state)
        {
            this.active = active;
            this.keep = keep;
            this.everKept = keep;
            this.state = state;
        }

        /// <summary>
        /// Gets and sets if this if context is currently active.
        /// </summary>
        /// <value>true if context is active.</value>
        public bool Active
        {
            get { return this.active; }
            set { this.active = value; }
        }

        /// <summary>
        /// Gets and sets if context is current true.
        /// </summary>
        /// <value>true if context is currently true.</value>
        public bool IsTrue
        {
            get
            {
                return this.keep;
            }

            set
            {
                this.keep = value;
                if (this.keep)
                {
                    this.everKept = true;
                }
            }
        }

        /// <summary>
        /// Gets if the context was ever true.
        /// </summary>
        /// <value>True if context was ever true.</value>
        public bool WasEverTrue
        {
            get { return this.everKept; }
        }

        /// <summary>
        /// Gets the current state of the if context.
        /// </summary>
        /// <value>Current state of context.</value>
        public IfState IfState
        {
            get { return this.state; }
            set { this.state = value; }
        }
    }
}
