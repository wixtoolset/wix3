// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace WixTest.Burn.OM.WixAuthoringOM.Bundle.Chain
{
    using WixTest.Burn.OM.ElementAttribute;

    [BurnXmlElement("ExePackage")]
    public class ExePackageElement : Package
    {
        // Xml attributes

        private string m_PerMachine;
        [BurnXmlAttribute("PerMachine")]
        public string PerMachine
        {
            get { return m_PerMachine; }
            set { m_PerMachine = value; }
        }

        private string m_DetectCondition;
        [BurnXmlAttribute("DetectCondition")]
        public string DetectCondition
        {
            get { return m_DetectCondition; }
            set { m_DetectCondition = value; }
        }

        private string m_InstallCommand;
        [BurnXmlAttribute("InstallCommand")]
        public string InstallCommand
        {
            get { return m_InstallCommand; }
            set { m_InstallCommand = value; }
        }

        private string m_RepairCommand;
        [BurnXmlAttribute("RepairCommand")]
        public string RepairCommand
        {
            get { return m_RepairCommand; }
            set { m_RepairCommand = value; }
        }

        private string m_UninstallCommand;
        [BurnXmlAttribute("UninstallCommand")]
        public string UninstallCommand
        {
            get { return m_UninstallCommand; }
            set { m_UninstallCommand = value; }
        }
    }
}
