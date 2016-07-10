// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Test.Frost.Core
{
    using System;
    using System.Xml;

    /// <summary>
    /// A Burn Package state.
    /// </summary>
    public class Package
    {
        public string PackageID;
        public CUR_PACKAGE_STATE CurrentState;
        public PKG_REQUEST_STATE RequestedState;
        private XmlNode ExecuteStateDescriptor;
        public PKG_ACTION_STATE ExecuteState;
        private XmlNode RollbackStateDescriptor;
        public PKG_ACTION_STATE RollbackState;

        /// <summary>
        /// Initialize the object.
        /// </summary>
        /// <param name="PackageDescriptor">The <see cref="XmlNode"/> in the manifest which describes the package.</param>
        public Package(XmlNode PackageDescriptor)
        {
            if (PackageDescriptor.Attributes["ID"] == null)
            {
                throw new FrostConfigException("Package descriptor node must define an ID attribute");
            }

            PackageID = PackageDescriptor.Attributes["ID"].Value;

            if (string.IsNullOrEmpty(PackageID))
            {
                throw new FrostConfigException("Package ID must be defined, cant be null/emtpy");
            }

            XmlNode NodeChecker = PackageDescriptor.SelectSingleNode("CurrentState");
            if(NodeChecker == null)
            {
                throw new FrostConfigException("Package ", PackageID, " does not define a CurrentState node");
            }
            CurrentState = (CUR_PACKAGE_STATE)Variables.ValueParser(NodeChecker);

            ExecuteStateDescriptor = PackageDescriptor.SelectSingleNode("ExecuteState");
            RollbackStateDescriptor = PackageDescriptor.SelectSingleNode("RollbackState");
            RequestedState = PKG_REQUEST_STATE.PKG_REQUEST_STATE_NONE;
            ExecuteState = PKG_ACTION_STATE.PKG_ACTION_STATE_NONE;
            RollbackState = PKG_ACTION_STATE.PKG_ACTION_STATE_NONE;
        }

        public void DefineExecuteAndRollbackActions()
        {
            Frost.EngineLogger.WriteLog(LoggingLevel.TRACE, "Defining ExecuteState and RollbackState for ", this.PackageID);

            string RequestStateKey = "";
            switch (this.RequestedState)
            {
                case PKG_REQUEST_STATE.PKG_REQUEST_STATE_ABSENT:
                    RequestStateKey = "absent";
                    break;
                case PKG_REQUEST_STATE.PKG_REQUEST_STATE_CACHE:
                    RequestStateKey = "cache";
                    break;
                case PKG_REQUEST_STATE.PKG_REQUEST_STATE_NONE:
                    RequestStateKey = "none";
                    break;
                case PKG_REQUEST_STATE.PKG_REQUEST_STATE_PRESENT:
                    RequestStateKey = "present";
                    break;
                case PKG_REQUEST_STATE.PKG_REQUEST_STATE_REPAIR:
                    RequestStateKey = "repair";
                    break;
            }

            Frost.EngineLogger.WriteLog(LoggingLevel.INFO, "RequestedState: ", RequestStateKey);

            this.ExecuteState = GetActionValue(this.ExecuteStateDescriptor, RequestStateKey);
            this.RollbackState = GetActionValue(this.RollbackStateDescriptor, RequestStateKey);
        }

        public override string ToString()
        {
            string RetVal = String.Empty;

            RetVal = String.Concat("Package ID: ", this.PackageID, Environment.NewLine,
                                   "Current State: ", this.CurrentState, Environment.NewLine,
                                   ActionValueDescriptorsToString(this.ExecuteStateDescriptor, "Execute State Descriptor"),
                                   ActionValueDescriptorsToString(this.RollbackStateDescriptor, "Rollback State Descriptor"));

            return RetVal;
        }

        private string ActionValueDescriptorsToString(XmlNode TheNode, string SectionName)
        {
            string RetVal = String.Concat(SectionName, Environment.NewLine);

            if (TheNode.SelectSingleNode("*[@Request]") == null)
            {
                RetVal = String.Concat(RetVal, "\t[Request Type = ALL] ", TheNode.InnerText, Environment.NewLine);
            }
            else
            {
                foreach (XmlNode ThisDescriptor in TheNode.ChildNodes)
                {
                    RetVal = String.Concat(RetVal, "\t[Request Type = ", ThisDescriptor.Attributes["Request"].Value, "] ", ThisDescriptor.InnerText, Environment.NewLine);
                }
            }

            return RetVal;
        }

        private PKG_ACTION_STATE GetActionValue(XmlNode ParentNode, string RequestState)
        {
            string TargetValue = "";

            Frost.EngineLogger.WriteLog(LoggingLevel.TRACE, "Setting Action State from ", ParentNode.Name);

            if (ParentNode.SelectNodes("*[@Request]").Count != 0)
            {
                Frost.EngineLogger.WriteLog(LoggingLevel.TRACE, "Retrieving value for a particular Requested State");
                Frost.EngineLogger.WriteLog(LoggingLevel.INFO, "Requested State: ", RequestedState);
                XmlNode XmlTargetNode = ParentNode.SelectSingleNode(String.Concat("*[@Request='", RequestState, "']"));

                if (XmlTargetNode == null)
                {
                    throw new FrostConfigException("Package ", this.PackageID, " does not define a ", RequestState, " package Action value node for ", ParentNode.Name);
                }

                TargetValue = XmlTargetNode.InnerText;
            }
            else
            {
                Frost.EngineLogger.WriteLog(LoggingLevel.TRACE, "Retrieving generic (for all Requested states) value");
                TargetValue = ParentNode.InnerText;
            }

            if (String.IsNullOrEmpty(TargetValue))
            {
                throw new FrostConfigException("Package ", this.PackageID, " defines package Action ", ParentNode.Name, " for requested state ", RequestedState, " as an empty string");
            }

            Frost.EngineLogger.WriteLog(LoggingLevel.INFO, "Action value found: ", TargetValue);
            return (PKG_ACTION_STATE)Variables.ValueParser(ParentNode.Name, "PKG_ACTION_STATE", TargetValue);
        }
    }
}
