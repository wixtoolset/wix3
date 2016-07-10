// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Test.Frost.Core
{
    using System;
    using System.Collections.Generic;
    using System.Threading;
    using System.Xml;

    /// <summary>
    /// A variable store.
    /// </summary>
    public class Variables
    {
        private Dictionary<string, object> StorageValues;
        public Semaphore VariablesLock;

        public Variables()
        {
            StorageValues = new Dictionary<string, object>();
            VariablesLock = new Semaphore(1, 1);
        }

        /// <summary>
        /// Get a varible object with a given name.
        /// </summary>
        /// <param name="VariableName">Request the variable name.</param>
        /// <returns>An object representing the value of the variable name requested.</returns>
        public object this[string VariableName]
        {
            get
            {
                object RetVal;
                bool FoundVal = this.StorageValues.TryGetValue(VariableName, out RetVal);

                if (!FoundVal)
                {
                    throw new FrostNonExistentVariableException(VariableName);
                }

                return RetVal;
            }
            set
            {
                this.VariablesLock.WaitOne();
                try
                {
                    this.StorageValues[VariableName] = value;
                }
                catch (KeyNotFoundException)
                {
                    this.StorageValues.Add(VariableName, value);
                }
                this.VariablesLock.Release();
            }
        }

        public bool VariableExists(string VariableName)
        {
            bool RetVal = this.StorageValues.ContainsKey(VariableName);
            return RetVal;
        }

        public bool RemoveVariable(string VariableName)
        {
            this.VariablesLock.WaitOne();
            bool RetVal = StorageValues.Remove(VariableName);
            this.VariablesLock.Release();
            return RetVal;
        }

        public static object ValueParser(XmlNode VariableDescritorNode)
        {
            return ValueParser(VariableDescritorNode, false);
        }

        public static object ValueParser(XmlNode VariableDescriptorNode, bool UseParentName, object DefaultValue)
        {
            if (VariableDescriptorNode == null)
            {
                Frost.EngineLogger.WriteLog(LoggingLevel.TRACE, "Value parser returns default value");
                Frost.EngineLogger.WriteLog(LoggingLevel.INFO, "Default value: ", DefaultValue);
                return DefaultValue;
            }

            return ValueParser(VariableDescriptorNode, UseParentName);
        }

        public static object ValueParser(XmlNode VariableDescriptorNode, bool UseParentName)
        {
            if (VariableDescriptorNode == null)
            {
                throw new FrostConfigException("Trying to parse a value from a null XmlNode (node not found?)");
            }

            string VariableName;
            if (VariableDescriptorNode.Attributes["Name"] == null)
            {
                VariableName = VariableDescriptorNode.Name;
            }
            else
            {
                VariableName = VariableDescriptorNode.Attributes["Name"].Value;
            }

            if (UseParentName)
            {
                VariableName = String.Concat(VariableDescriptorNode.ParentNode.Name, VariableName); 
            }

            if (VariableDescriptorNode.Attributes["Type"] == null)
            {
                throw new FrostConfigException("Node for ", VariableName, " does not define a Type attribute");
            }

            string VariableType = VariableDescriptorNode.Attributes["Type"].Value;
            string VariableValue;
            XmlAttribute VariableValueAttribute = VariableDescriptorNode.Attributes["Value"];

            if (VariableValueAttribute != null)
            {
                VariableValue = VariableValueAttribute.Value;
            }
            else
            {
                VariableValue = VariableDescriptorNode.InnerText;
            }

            if (string.Equals(VariableType, "string", StringComparison.OrdinalIgnoreCase))
            {
                if (string.IsNullOrEmpty(VariableValue))
                {
                    XmlNode NullTokenGetter = VariableDescriptorNode.SelectSingleNode("Null");
                    if (NullTokenGetter != null && NullTokenGetter.NodeType == XmlNodeType.Element)
                    {
                        return null;
                    }
                    else
                    {
                        return string.Empty;
                    }
                }
                else
                {
                    return VariableValue;
                }
            }
            else
            {
                return ValueParser(VariableName, VariableType, VariableValue);
            }
        }

        public static object ValueParser(string VariableName, string VariableType, string VariableValue)
        {
            try
            {
                switch (VariableType.ToLower())
                {
                    case "int":
                    case "int32":
                        #region Int Parsing
                        return int.Parse(VariableValue);
                        #endregion

                    case "uint":
                    case "uint32":
                        #region uint parsing
                        return uint.Parse(VariableValue);
                        #endregion

                    case "long":
                    case "int64":
                        #region long parsing
                        return long.Parse(VariableValue);
                        #endregion

                    case "ulong":
                    case "uint64":
                        #region ulong parsing
                        return ulong.Parse(VariableValue);
                        #endregion

                    case "bool":
                        #region Bool parsing
                        if (VariableValue == null)
                            return false;
                        else
                        {
                            VariableValue = VariableValue.ToLower();
                            return VariableValue == "true";
                        }
                        #endregion

                    case "hresults":
                        #region hresults parsing
                        switch (VariableValue.ToLower())
                        {
                            case "ok":
                            case "s_ok":
                            case "hr_s_ok":
                                return HRESULTS.HR_S_OK;

                            case "false":
                            case "s_false":
                            case "hr_s_false":
                                return HRESULTS.HR_S_FALSE;

                            case "failure":
                            case "hr_failure":
                                return HRESULTS.HR_FAILURE;

                            default:
                                throw new FrostConfigException("Value for enum HRESULTS does not contain ", VariableValue);
                        }
                        #endregion

                    case "cur_package_state":
                        #region CUR_PACKAGE_STATE parsing
                        switch (VariableValue.ToLower())
                        {
                            case "absent":
                            case "cur_package_state_absent":
                                return CUR_PACKAGE_STATE.CUR_PACKAGE_STATE_ABSENT;

                            case "cached":
                            case "cur_package_state_cached":
                                return CUR_PACKAGE_STATE.CUR_PACKAGE_STATE_CACHED;

                            case "present":
                            case "cur_package_state_present":
                                return CUR_PACKAGE_STATE.CUR_PACKAGE_STATE_PRESENT;

                            case "unknown":
                            case "cur_package_state_unknown":
                                return CUR_PACKAGE_STATE.CUR_PACKAGE_STATE_UNKNOWN;

                            default:
                                throw new FrostConfigException("Value for enum CUR_PACKAGE_STATE does not contain ", VariableValue);
                        }
                        #endregion

                    case "pkg_request_state":
                        #region PKG_REQUEST_STATE parsing
                        switch (VariableValue.ToLower())
                        {
                            case "absent":
                            case "pkg_request_state_absent":
                                return PKG_REQUEST_STATE.PKG_REQUEST_STATE_ABSENT;

                            case "cache":
                            case "pkg_request_state_cache":
                                return PKG_REQUEST_STATE.PKG_REQUEST_STATE_CACHE;

                            case "none":
                            case "pkg_request_state_none":
                                return PKG_REQUEST_STATE.PKG_REQUEST_STATE_NONE;

                            case "present":
                            case "pkg_request_state_present":
                                return PKG_REQUEST_STATE.PKG_REQUEST_STATE_PRESENT;

                            case "repair":
                            case "pkg_request_state_repair":
                                return PKG_REQUEST_STATE.PKG_REQUEST_STATE_REPAIR;

                            default:
                                throw new FrostConfigException("Value for enum does not contain ", VariableValue);
                        }
                        #endregion

                    case "pkg_action_state":
                        #region PKG_ACTION_STATE parsing
                        switch (VariableValue.ToLower())
                        {
                            case "admin_install":
                            case "admin install":
                            case "pkg_action_state_admin_install":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_ADMIN_INSTALL;

                            case "install":
                            case "pkg_action_state_install":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_INSTALL;

                            case "maintenance":
                            case "pkg_action_state_maintenance":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_MAINTENANCE;

                            case "major_upgrade":
                            case "major upgrade":
                            case "pkg_action_state_major_upgrade":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_MAJOR_UPGRADE;

                            case "minor_upgrade":
                            case "minor upgrade":
                            case "pkg_action_state_minor_upgrade":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_MINOR_UPGRADE;

                            case "none":
                            case "pkg_action_state_none":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_NONE;

                            case "patch":
                            case "pkg_action_state_patch":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_PATCH;

                            case "recache":
                            case "pkg_action_state_recache":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_RECACHE;

                            case "uninstall":
                            case "pkg_action_state_uninstall":
                                return PKG_ACTION_STATE.PKG_ACTION_STATE_UNINSTALL;

                            default:
                                throw new FrostConfigException("Value for enum PKG_ACTION_STATE does not contain ", VariableValue);
                        }
                        #endregion

                    case "setup_action":
                        #region SETUP_ACTION parsing
                        switch (VariableValue.ToLower())
                        {
                            case "help":
                            case "setup_action_help":
                                return SETUP_ACTION.SETUP_ACTION_HELP;

                            case "install":
                            case "setup_action_install":
                                return SETUP_ACTION.SETUP_ACTION_INSTALL;

                            case "modify":
                            case "setup_action_modify":
                                return SETUP_ACTION.SETUP_ACTION_MODIFY;

                            case "repair":
                            case "setup_action_repair":
                                return SETUP_ACTION.SETUP_ACTION_REPAIR;

                            case "uninstall":
                            case "setup_action_uninstall":
                                return SETUP_ACTION.SETUP_ACTION_UNINSTALL;

                            case "unknown":
                            case "setup_action_unknown":
                                return SETUP_ACTION.SETUP_ACTION_UNKNOWN;

                            default:
                                throw new FrostConfigException("Value for enum SETUP_ACTION does not contain ", VariableValue);
                        }
                        #endregion

                    case "setup_display":
                        #region SETUP_DISPLAY parsing
                        switch (VariableValue.ToLower())
                        {
                            case "full":
                            case "setup_display_full":
                                return SETUP_DISPLAY.SETUP_DISPLAY_FULL;

                            case "none":
                            case "setup_display_none":
                                return SETUP_DISPLAY.SETUP_DISPLAY_NONE;

                            case "passive":
                            case "setup_display_passive":
                                return SETUP_DISPLAY.SETUP_DISPLAY_PASSIVE;

                            case "unknown":
                            case "setup_display_unknown":
                                return SETUP_DISPLAY.SETUP_DISPLAY_UNKNOWN;

                            default:
                                throw new FrostConfigException("Value for enum SETUP_DISPLAY does not contain ", VariableValue);
                        }
                        #endregion

                    case "setup_restart":
                        #region SETUP_RESTART parsing
                        switch (VariableValue.ToLower())
                        {
                            case "always":
                            case "setup_restart_always":
                                return SETUP_RESTART.SETUP_RESTART_ALWAYS;

                            case "automatic":
                            case "auto":
                            case "setup_restart_automatic":
                                return SETUP_RESTART.SETUP_RESTART_AUTOMATIC;

                            case "never":
                            case "setup_restart_never":
                                return SETUP_RESTART.SETUP_RESTART_NEVER;

                            case "prompt":
                            case "setup_restart_prompt":
                                return SETUP_RESTART.SETUP_RESTART_PROMPT;

                            case "unknown":
                            case "setup_restart_unknown":
                                return SETUP_RESTART.SETUP_RESTART_UNKNOWN;

                            default:
                                throw new FrostConfigException("Value for enum SETUP_RESTART does not contain ", VariableValue);
                        }
                        #endregion

                    case "setup_resume":
                        #region SETUP_RESUME parsing
                        switch (VariableValue.ToLower())
                        {
                            case "arp":
                                return SETUP_RESUME.SETUP_RESUME_ARP;

                            case "invalid":
                                return SETUP_RESUME.SETUP_RESUME_INVALID;

                            case "none":
                                return SETUP_RESUME.SETUP_RESUME_NONE;

                            case "reboot":
                                return SETUP_RESUME.SETUP_RESUME_REBOOT;

                            case "reboot_pending":
                            case "reboot pending":
                            case "pending":
                                return SETUP_RESUME.SETUP_RESUME_REBOOT_PENDING;

                            case "suspend":
                                return SETUP_RESUME.SETUP_RESUME_SUSPEND;

                            case "unexpected":
                                return SETUP_RESUME.SETUP_RESUME_UNEXPECTED;

                            default:
                                throw new FrostConfigException("Value for enum SETUP_RESUME does not contain ", VariableValue);
                        }
                        #endregion

                    case "string":
                    default:
                        return VariableValue;
                }
            }
            catch (ArgumentNullException)
            {
                throw new FrostConfigException("Trying to store null into ", VariableType, " value ", VariableName);
            }
            catch (FormatException)
            {
                throw new FrostConfigException("Unable to format ", VariableValue, " into a ", VariableType, " for ", VariableName);
            }
            catch (OverflowException)
            {
                throw new FrostConfigException(VariableName, "'s value ", VariableValue, " is too large for a ", VariableType);
            }

        }
    }
}
