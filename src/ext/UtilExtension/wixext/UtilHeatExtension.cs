// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.IO;
    using System.Text;
    using Microsoft.Tools.WindowsInstallerXml.Tools;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    /// <summary>
    /// A utility heat extension for the Windows Installer XML Toolset Harvester application.
    /// </summary>
    public sealed class UtilHeatExtension : HeatExtension
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
                    new HeatCommandLineOption("dir", "harvest a directory"),
                    new HeatCommandLineOption("file", "harvest a file"),
                    new HeatCommandLineOption("payload", "harvest a bundle payload as RemotePayload"),
                    new HeatCommandLineOption("perf", "harvest performance counters"),
                    new HeatCommandLineOption("reg", "harvest a .reg file"),
                    new HeatCommandLineOption("-ag", "autogenerate component guids at compile time"),
                    new HeatCommandLineOption("-cg <ComponentGroupName>", "component group name (cannot contain spaces e.g -cg MyComponentGroup)"),
                    new HeatCommandLineOption("-dr <DirectoryName>", "directory reference to root directories (cannot contain spaces e.g. -dr MyAppDirRef)"),
                    new HeatCommandLineOption("-var <VariableName>", "substitute File/@Source=\"SourceDir\" with a preprocessor or a wix variable" + Environment.NewLine +
                                                      "(e.g. -var var.MySource will become File/@Source=\"$(var.MySource)\\myfile.txt\" and " + Environment.NewLine + 
                                                      "-var wix.MySource will become File/@Source=\"!(wix.MySource)\\myfile.txt\""),
                    new HeatCommandLineOption("-gg", "generate guids now"),
                    new HeatCommandLineOption("-g1", "generated guids are not in brackets"),
                    new HeatCommandLineOption("-ke", "keep empty directories"),
                    new HeatCommandLineOption("-scom", "suppress COM elements"),
                    new HeatCommandLineOption("-sfrag", "suppress fragments"),
                    new HeatCommandLineOption("-srd", "suppress harvesting the root directory as an element"),
                    new HeatCommandLineOption("-svb6", "suppress VB6 COM elements"),
                    new HeatCommandLineOption("-sreg", "suppress registry harvesting"),
                    new HeatCommandLineOption("-suid", "suppress unique identifiers for files, components, & directories"),
                    new HeatCommandLineOption("-t", "transform harvested output with XSL file"),
                    new HeatCommandLineOption("-template", "use template, one of: fragment,module,product"),
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
            bool suppressHarvestingRegistryValues = false;
            UtilFinalizeHarvesterMutator utilFinalizeHarvesterMutator = new UtilFinalizeHarvesterMutator();
            UtilMutator utilMutator = new UtilMutator();
            List<UtilTransformMutator> transformMutators = new List<UtilTransformMutator>();

            // select the harvester
            switch (type)
            {
                case "dir":
                    harvesterExtension = new DirectoryHarvester();
                    active = true;
                    break;
                case "file":
                    harvesterExtension = new FileHarvester();
                    active = true;
                    break;
                case "payload":
                    harvesterExtension = new PayloadHarvester();
                    active = true;
                    break;
                case "perf":
                    harvesterExtension = new PerformanceCategoryHarvester();
                    active = true;
                    break;
                case "reg":
                    harvesterExtension = new RegFileHarvester();
                    active = true;
                    break;
            }

            // set default settings
            utilMutator.CreateFragments = true;
            utilMutator.SetUniqueIdentifiers = true;

            // parse the options
            for (int i = 0; i < args.Length; i++)
            {
                string commandSwitch = args[i];

                if (null == commandSwitch || 0 == commandSwitch.Length) // skip blank arguments
                {
                    continue;
                }

                if ('-' == commandSwitch[0] || '/' == commandSwitch[0])
                {
                    string truncatedCommandSwitch = commandSwitch.Substring(1);

                    if ("ag" == truncatedCommandSwitch)
                    {
                        utilMutator.AutogenerateGuids = true;
                    }
                    else if ("cg" == truncatedCommandSwitch)
                    {
                        utilMutator.ComponentGroupName = this.GetArgumentParameter(args, i);

                        if (this.Core.EncounteredError)
                        {
                            return;
                        }
                    }
                    else if ("dr" == truncatedCommandSwitch)
                    {
                        string dr = this.GetArgumentParameter(args, i);

                        if (this.Core.EncounteredError)
                        {
                            return;
                        }

                        if (harvesterExtension is DirectoryHarvester)
                        {
                            ((DirectoryHarvester)harvesterExtension).RootedDirectoryRef = dr;
                        }
                        else if (harvesterExtension is FileHarvester)
                        {
                            ((FileHarvester)harvesterExtension).RootedDirectoryRef = dr;
                        }
                    }
                    else if ("gg" == truncatedCommandSwitch)
                    {
                        utilMutator.GenerateGuids = true;
                    }
                    else if ("g1" == truncatedCommandSwitch)
                    {
                        utilMutator.GuidFormat = "D";
                    }
                    else if ("ke" == truncatedCommandSwitch)
                    {
                        if (harvesterExtension is DirectoryHarvester)
                        {
                            ((DirectoryHarvester)harvesterExtension).KeepEmptyDirectories = true;
                        }
                        else if (active)
                        {
                            // TODO: error message - not applicable to file harvester
                        }
                    }
                    else if ("scom" == truncatedCommandSwitch)
                    {
                        if (active)
                        {
                            utilFinalizeHarvesterMutator.SuppressCOMElements = true;
                        }
                        else
                        {
                            // TODO: error message - not applicable
                        }
                    }
                    else if ("svb6" == truncatedCommandSwitch)
                    {
                        if (active)
                        {
                            utilFinalizeHarvesterMutator.SuppressVB6COMElements = true;
                        }
                        else
                        {
                            // TODO: error message - not applicable
                        }
                    }
                    else if ("sfrag" == truncatedCommandSwitch)
                    {
                        utilMutator.CreateFragments = false;
                    }
                    else if ("srd" == truncatedCommandSwitch)
                    {
                        if (harvesterExtension is DirectoryHarvester)
                        {
                            ((DirectoryHarvester)harvesterExtension).SuppressRootDirectory = true;
                        }
                        else if (harvesterExtension is FileHarvester)
                        {
                            ((FileHarvester)harvesterExtension).SuppressRootDirectory = true;
                        }
                    }
                    else if ("sreg" == truncatedCommandSwitch)
                    {
                        suppressHarvestingRegistryValues = true;
                    }
                    else if ("suid" == truncatedCommandSwitch)
                    {
                        utilMutator.SetUniqueIdentifiers = false;

                        if (harvesterExtension is DirectoryHarvester)
                        {
                            ((DirectoryHarvester)harvesterExtension).SetUniqueIdentifiers = false;
                        }
                        else if (harvesterExtension is FileHarvester)
                        {
                            ((FileHarvester)harvesterExtension).SetUniqueIdentifiers = false;
                        }
                    }
                    else if (truncatedCommandSwitch.StartsWith("t:", StringComparison.Ordinal) || "t" == truncatedCommandSwitch)
                    {
                        string xslFile;
                        if (truncatedCommandSwitch.StartsWith("t:", StringComparison.Ordinal))
                        {
                            this.Core.OnMessage(WixWarnings.DeprecatedCommandLineSwitch("t:", "t"));
                            xslFile = truncatedCommandSwitch.Substring(2);
                        }
                        else
                        {
                            xslFile = this.GetArgumentParameter(args, i, true);
                        }

                        if (0 <= xslFile.IndexOf('\"'))
                        {
                            this.Core.OnMessage(WixErrors.PathCannotContainQuote(xslFile));
                            return;
                        }

                        try
                        {
                            xslFile = Path.GetFullPath(xslFile);
                        }
                        catch (Exception e)
                        {
                            this.Core.OnMessage(WixErrors.InvalidCommandLineFileName(xslFile, e.Message));
                            return;
                        }

                        transformMutators.Add(new UtilTransformMutator(xslFile, transformMutators.Count));
                    }
                    else if (truncatedCommandSwitch.StartsWith("template:", StringComparison.Ordinal) || "template" == truncatedCommandSwitch)
                    {
                        string template;
                        if(truncatedCommandSwitch.StartsWith("template:", StringComparison.Ordinal))
                        {
                            this.Core.OnMessage(WixWarnings.DeprecatedCommandLineSwitch("template:", "template"));
                            template = truncatedCommandSwitch.Substring(9);
                        }
                        else
                        {
                            template = this.GetArgumentParameter(args, i);
                        }

                        switch (template)
                        {
                            case "fragment":
                                utilMutator.TemplateType = TemplateType.Fragment;
                                break;
                            case "module":
                                utilMutator.TemplateType = TemplateType.Module;
                                break;
                            case "product":
                                utilMutator.TemplateType = TemplateType.Product;
                                break;
                            default:
                                // TODO: error
                                break;
                        }
                    }
                    else if ("var" == truncatedCommandSwitch)
                    {
                        if (active)
                        {
                            utilFinalizeHarvesterMutator.PreprocessorVariable = this.GetArgumentParameter(args, i);

                            if (this.Core.EncounteredError)
                            {
                                return;
                            }
                        }
                    }
                }
            }

            // set the appropriate harvester extension
            if (active)
            {
                this.Core.Harvester.Extension = harvesterExtension;

                if (!suppressHarvestingRegistryValues)
                {
                    this.Core.Mutator.AddExtension(new UtilHarvesterMutator());
                }

                this.Core.Mutator.AddExtension(utilFinalizeHarvesterMutator);

                if (harvesterExtension is DirectoryHarvester)
                {
                    this.Core.Harvester.Core.RootDirectory = this.Core.Harvester.Core.ExtensionArgument;
                }
                else if (harvesterExtension is FileHarvester)
                {
                    if (((FileHarvester)harvesterExtension).SuppressRootDirectory)
                    {
                        this.Core.Harvester.Core.RootDirectory = Path.GetDirectoryName(Path.GetFullPath(this.Core.Harvester.Core.ExtensionArgument));
                    }
                    else
                    {
                        this.Core.Harvester.Core.RootDirectory = Path.GetDirectoryName(Path.GetDirectoryName(Path.GetFullPath(this.Core.Harvester.Core.ExtensionArgument)));

                        // GetDirectoryName() returns null for root paths such as "c:\", so make sure to support that as well
                        if (null == this.Core.Harvester.Core.RootDirectory)
                        {
                            this.Core.Harvester.Core.RootDirectory = Path.GetPathRoot(Path.GetDirectoryName(Path.GetFullPath(this.Core.Harvester.Core.ExtensionArgument)));
                        }
                    }
                }
            }

            // set the mutator
            this.Core.Mutator.AddExtension(utilMutator);

            // add the transforms
            foreach (UtilTransformMutator transformMutator in transformMutators)
            {
                this.Core.Mutator.AddExtension(transformMutator);
            }
        }

        private string GetArgumentParameter(string[] args, int index)
        {
            return this.GetArgumentParameter(args, index, false);
        }

        private string GetArgumentParameter(string[] args, int index, bool allowSpaces)
        {
            string truncatedCommandSwitch = args[index];
            string commandSwitchValue = args[index + 1];
            
            //increment the index to the switch value
            index++;

            if (CommandLine.IsValidArg(args, index) && !String.IsNullOrEmpty(commandSwitchValue.Trim()))
            {
                if (!allowSpaces && commandSwitchValue.Contains(" "))
                {
                    this.Core.OnMessage(UtilErrors.SpacesNotAllowedInArgumentValue(truncatedCommandSwitch, commandSwitchValue));
                }
                else
                {
                    return commandSwitchValue;
                }
            }
            else
            {
                this.Core.OnMessage(UtilErrors.ArgumentRequiresValue(truncatedCommandSwitch));
            }

            return null;
        }
    }
}
