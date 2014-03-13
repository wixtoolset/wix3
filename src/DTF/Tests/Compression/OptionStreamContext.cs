//-------------------------------------------------------------------------------------------------
// <copyright file="OptionStreamContext.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
//-------------------------------------------------------------------------------------------------

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
