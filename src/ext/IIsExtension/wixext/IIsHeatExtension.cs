// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections.Specialized;
    using Microsoft.Tools.WindowsInstallerXml.Tools;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// An IIS harvesting extension for the Windows Installer XML Toolset Harvester application.
    /// </summary>
    public sealed class IIsHeatExtension : HeatExtension
    {
        /// <summary>
        /// Gets the supported command line types for this extension.
        /// </summary>
        /// <value>The supported command line types for this extension.</value>
        public override HeatCommandLineOption[] CommandLineTypes
        {
            get
            {
                return new HeatCommandLineOption[]
                {
                    new HeatCommandLineOption("website", "harvest an IIS web site"),
                };
            }
        }

        /// <summary>
        /// Parse the command line options for this extension.
        /// </summary>
        /// <param name="type">The active harvester type.</param>
        /// <param name="args">The option arguments.</param>
        public override void ParseOptions(string type, string[] args)
        {
            bool active = false;
            HarvesterExtension harvesterExtension = null;
            IIsHarvesterMutator iisHarvesterMutator = new IIsHarvesterMutator();

            // select the harvester
            switch (type)
            {
                case "website":
                    harvesterExtension = new IIsWebSiteHarvester();
                    active = true;
                    break;
            }

            // set default settings
            iisHarvesterMutator.SetUniqueIdentifiers = true;

            // parse the options
            foreach (string arg in args)
            {
                if (null == arg || 0 == arg.Length) // skip blank arguments
                {
                    continue;
                }

                if ('-' == arg[0] || '/' == arg[0])
                {
                    string parameter = arg.Substring(1);

                    if ("suid" == parameter)
                    {
                        iisHarvesterMutator.SetUniqueIdentifiers = false;
                    }
                }
            }

            // set the appropriate harvester extension
            if (active)
            {
                this.Core.Harvester.Extension = harvesterExtension;
                this.Core.Mutator.AddExtension(iisHarvesterMutator);
                this.Core.Mutator.AddExtension(new IIsFinalizeHarvesterMutator());
                this.Core.Mutator.AddExtension(new UtilFinalizeHarvesterMutator());
            }
        }
    }
}
