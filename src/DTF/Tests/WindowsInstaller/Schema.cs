// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Deployment.Test
{
    using System;
    using System.IO;
    using System.Collections.Generic;
    using System.Text;
    using Microsoft.Deployment.WindowsInstaller;

    public static class Schema
    {
        public static IList<TableInfo> Tables
        {
            get
            {
                return new TableInfo[]
                {
                    Binary,
                    Component,
                    CustomAction,
                    Directory,
                    EmbeddedUI,
                    Feature,
                    FeatureComponents,
                    File,
                    InstallExecuteSequence,
                    Media,
                    Property,
                    Registry
                };
            }
        }

        #region Table data

        public static TableInfo Binary { get { return new TableInfo(
            "Binary",
            new ColumnInfo[]
            {
                new ColumnInfo("Name", typeof(String), 72, true),
                new ColumnInfo("Data", typeof(Stream),  0, true),
            },
            new string[] { "Name" });
        } }

        public static TableInfo Component { get { return new TableInfo(
            "Component",
            new ColumnInfo[]
            {
                new ColumnInfo("Component",   typeof(String),  72, true),
                new ColumnInfo("ComponentId", typeof(String),  38, false),
                new ColumnInfo("Directory_",  typeof(String),  72, true),
                new ColumnInfo("Attributes",  typeof(Int16),    2, true),
                new ColumnInfo("Condition",   typeof(String), 255, false),
                new ColumnInfo("KeyPath",     typeof(String),  72, false),
            },
            new string[] { "Component" });
        } }

        public static TableInfo CustomAction { get { return new TableInfo(
            "CustomAction",
            new ColumnInfo[]
            {
                new ColumnInfo("Action", typeof(String),  72, true),
                new ColumnInfo("Type",   typeof(Int16),    2, true),
                new ColumnInfo("Source", typeof(String),  64, false),
                new ColumnInfo("Target", typeof(String), 255, false),
            },
            new string[] { "Action" });
        } }

        public static TableInfo Directory { get { return new TableInfo(
            "Directory",
            new ColumnInfo[]
            {
                new ColumnInfo("Directory",        typeof(String),  72, true),
                new ColumnInfo("Directory_Parent", typeof(String),  72, false),
                new ColumnInfo("DefaultDir",       typeof(String), 255, true, false, true),
            },
            new string[] { "Directory" });
        } }

        public static TableInfo EmbeddedUI { get { return new TableInfo(
            "MsiEmbeddedUI",
            new ColumnInfo[]
            {
                new ColumnInfo("MsiEmbeddedUI", typeof(String), 72, true),
                new ColumnInfo("FileName",      typeof(String), 72, true),
                new ColumnInfo("Attributes",    typeof(Int16),   2, true),
                new ColumnInfo("MessageFilter", typeof(Int32),   4, false),
                new ColumnInfo("Data",          typeof(Stream),  0, true),
            },
            new string[] { "MsiEmbeddedUI" });
        } }

        public static TableInfo Feature { get { return new TableInfo(
            "Feature",
            new ColumnInfo[]
            {
                new ColumnInfo("Feature",        typeof(String),  38, true),
                new ColumnInfo("Feature_Parent", typeof(String),  38, false),
                new ColumnInfo("Title",          typeof(String),  64, false, false, true),
                new ColumnInfo("Description",    typeof(String),  64, false, false, true),
                new ColumnInfo("Display",        typeof(Int16),    2, false),
                new ColumnInfo("Level",          typeof(Int16),    2, true),
                new ColumnInfo("Directory_",     typeof(String),  72, false),
                new ColumnInfo("Attributes",     typeof(Int16),    2, true),
            },
            new string[] { "Feature" });
        } }

        public static TableInfo FeatureComponents { get { return new TableInfo(
            "FeatureComponents",
            new ColumnInfo[]
            {
                new ColumnInfo("Feature_",   typeof(String),  38, true),
                new ColumnInfo("Component_", typeof(String),  72, true),
            },
            new string[] { "Feature_", "Component_" });
        } }

        public static TableInfo File { get { return new TableInfo(
            "File",
            new ColumnInfo[]
            {
                new ColumnInfo("File",       typeof(String),  72, true),
                new ColumnInfo("Component_", typeof(String),  72, true),
                new ColumnInfo("FileName",   typeof(String), 255, true, false, true),
                new ColumnInfo("FileSize",   typeof(Int32),    4, true),
                new ColumnInfo("Version",    typeof(String),  72, false),
                new ColumnInfo("Language",   typeof(String),  20, false),
                new ColumnInfo("Attributes", typeof(Int16),    2, false),
                new ColumnInfo("Sequence",   typeof(Int16),    2, true),
            },
            new string[] { "File" });
        } }

        public static TableInfo InstallExecuteSequence { get { return new TableInfo(
            "InstallExecuteSequence",
            new ColumnInfo[]
            {
                new ColumnInfo("Action",    typeof(String),  72, true),
                new ColumnInfo("Condition", typeof(String), 255, false),
                new ColumnInfo("Sequence",  typeof(Int16),    2, true),
            },
            new string[] { "Action" });
        } }

        public static TableInfo Media { get { return new TableInfo(
            "Media",
            new ColumnInfo[]
            {
                new ColumnInfo("DiskId",       typeof(Int16),    2, true),
                new ColumnInfo("LastSequence", typeof(Int16),    2, true),
                new ColumnInfo("DiskPrompt",   typeof(String),  64, false, false, true),
                new ColumnInfo("Cabinet",      typeof(String), 255, false),
                new ColumnInfo("VolumeLabel",  typeof(String),  32, false),
                new ColumnInfo("Source",       typeof(String),  32, false),
            },
            new string[] { "DiskId" });
        } }

        public static TableInfo Property { get { return new TableInfo(
            "Property",
            new ColumnInfo[]
            {
                new ColumnInfo("Property", typeof(String),  72, true),
                new ColumnInfo("Value",    typeof(String), 255, true),
            },
            new string[] { "Property" });
        } }

        public static TableInfo Registry { get { return new TableInfo(
            "Registry",
            new ColumnInfo[]
            {
                new ColumnInfo("Registry",   typeof(String),  72, true),
                new ColumnInfo("Root",       typeof(Int16),    2, true),
                new ColumnInfo("Key",        typeof(String), 255, true, false, true),
                new ColumnInfo("Name",       typeof(String), 255, false, false, true),
                new ColumnInfo("Value",      typeof(String),   0, false, false, true),
                new ColumnInfo("Component_", typeof(String),  72, true),
            },
            new string[] { "Registry" });
        } }

        #endregion

    }

    public class Action
    {
        public readonly string Name;
        public readonly int Sequence;

        public Action(string name, int sequence)
        {
            this.Name = name;
            this.Sequence = sequence;
        }

    }

    public class Sequence
    {
        public static IList<Action> InstallExecute
        {
            get
            {
                return new Action[]
                {
                    new Action("CostInitialize",        800),
                    new Action("FileCost",              900),
                    new Action("CostFinalize",         1000),
                    new Action("InstallValidate",      1400),
                    new Action("InstallInitialize",    1500),
                    new Action("ProcessComponents",    1600),
                    new Action("UnpublishComponents",  1700),
                    new Action("UnpublishFeatures",    1800),
                    new Action("RemoveRegistryValues", 2600),
                    new Action("RemoveFiles",          3500),
                    new Action("RemoveFolders",        3600),
                    new Action("CreateFolders",        3700),
                    new Action("MoveFiles",            3800),
                    new Action("InstallFiles",         4000),
                    new Action("WriteRegistryValues",  5000),
                    new Action("RegisterProduct",      6100),
                    new Action("PublishComponents",    6200),
                    new Action("PublishFeatures",      6300),
                    new Action("PublishProduct",       6400),
                    new Action("InstallFinalize",      6600),
                };
            }
        }

    }
}
