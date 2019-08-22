// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

using System;
using System.Collections.Generic;
using Microsoft.Deployment.Compression;

namespace Microsoft.Deployment.Test
{
    public class OptionStreamContext : ArchiveFileStreamContext
    {
        private PackOptionHandler packOptionHandler;

        public OptionStreamContext(IList<string> archiveFiles, string directory, IDictionary<string, string> files)
            : base(archiveFiles, directory, files)
        {
        }

        public delegate object PackOptionHandler(string optionName, object[] parameters);

        public PackOptionHandler OptionHandler
        {
            get
            {
                return this.packOptionHandler;
            }
            set
            {
                this.packOptionHandler = value;
            }
        }

        public override object GetOption(string optionName, object[] parameters)
        {
            if (this.OptionHandler == null)
            {
                return null;
            }

            return this.OptionHandler(optionName, parameters);
        }
    }
}
