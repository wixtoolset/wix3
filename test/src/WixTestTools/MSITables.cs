//-------------------------------------------------------------------------------------------------
// <copyright file="MSITables.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//      Contains enumurations for MSI tables and thier columns
// </summary>
//-------------------------------------------------------------------------------------------------

namespace WixTest
{
    public enum MSITables
    {
        CustomAction,
        Certificate,
        FileShare,
        FileSharePermissions,
        EventManifest,
        Group,
        IIsFilter,
        IIsHttpHeader,
        IIsMimeMap,
        IIsProperty,
        IIsWebApplication,
        IIsWebDir,
        IIsWebError,
        IIsWebServiceExtension,
        IIsWebSite,
        IIsWebVirtualDir,
        NetFxNativeImage,
        PerformanceCategory,
        PerfmonManifest,
        Perfmon,
        SecureObjects,
        ServiceConfig,
        ServiceControl,
        SqlDatabase,
        SqlFileSpec,
        SqlString,
        SqlScript,
        TextStyle,
        UIText,
        User,
        UserGroup,
        WixInternetShortcut,
        WixCloseApplication,
        XmlConfig,
        XmlFile

    };

    public enum CertificateColumns
    {
        Certificate,
        Component_,
        Name,
        StoreLocation,
        StoreName,
        Attributes,
        Binary_,
        CertificatePath,
        PFXPassword
    };

    public enum CustomActionColumns
    {
        Action,
        Type,
        Source,
        Target
    };

    public enum EventManifestColumns
    {
        Component_,
        File
    };

    public enum FileShareColumns
    {
        FileShare,
        ShareName,
        Component_,
        Description,
        Directory_,
        User_,
        Permissions
    };

    public enum FileSharePermissionsColumns
    {
        FileShare_,
        User_,
        Permissions
    };

    public enum GroupColumns
    {
        Group,
        Component_,
        Name,
        Domain
    };

    public enum IISFilterColumns
    {
        Filter,
        Name,
        Component_,
        Path,
        Web_,
        Description,
        Flags,
        LoadOrder
    };

    public enum IIsHttpHeaderColumns
    {
        HttpHeader,
        ParentType,
        ParentValue,
        Name,
        Value,
        Attributes,
        Sequence
    };

    public enum IIsMimeMapColumns
    {
        MimeMap,
        ParentType,
        ParentValue,
        MimeType,
        Extension
    };

    public enum IIsPropertyColumns
    {
        Property,
        Component_,
        Attributes,
        Value,
    };

    public enum IIsWebApplicationColumns
    {
        Application,
        Name,
        Isolation,
        AllowSessions,
        SessionTimeout,
        Buffer,
        ParentPaths,
        DefaultScript,
        ScriptTimeout,
        ServerDebugging,
        ClientDebugging,
        AppPool_
    };

    public enum IISWebDirColumns
    {
        WebDir,
        Component_,
        Web_,
        Path,
        DirProperties_,
        Application_
    };

    public enum IIsWebErrorColumns
    { 
        ErrorCode,
        SubCode,
        ParentType,
        ParentValue,
        File,
        URL
    };

    public enum IIsWebServiceExtensionColumns
    {
        WebServiceExtension,
        Component_,
        File,
        Description,
        Group,
        Attributes
    };

    public enum IIsWebSiteColumns
    {
        Web,
        Component_,
        Description,
        ConnectionTimeout,
        Directory_,
        State,
        Attributes,
        KeyAddress_,
        DirProperties_,
        Application_,
        Sequence,
        Log_
    };

    public enum IIsWebVirtualDirColumns
    {
        VirtualDir,
        Component_,
        Web_,
        Alias,
        Directory_,
        DirProperties_,
        Application_
    };

    public enum PerformanceCategoryColumns
    {
        PerformanceCategory,
        Component_,
        Name,
        IniData,
        ConstantData
    };

    public enum NetFxNativeImageColumns
    {
        NetFxNativeImage,
        File_,
        Priority,
        Attributes,
        File_Application,
        Directory_ApplicationBase
    };

    public enum PerfmonManifestColumns
    {
        Component_,
        File,
        ResourceFileDirectory
    };

  
    public enum PerfmonColumns
    {
        Component_,
        File,
        Name
    };

    public enum SecureObjectsColumns
    {
        SecureObject,
        Table,
        Domain,
        User,
        Permission
    };

    public enum ServiceConfigColumns
    {
        ServiceName,
        Component_,
        NewService,
        FirstFailureActionType,
        SecondFailureActionType,
        ThirdFailureActionType,
        ResetPeriodInDays,
        RestartServiceDelayInSeconds,
        ProgramCommandLine,
        RebootMessage
    };

 

    public enum ServiceControlColumns
    {
        ServiceControl,
        Name,
        Event,
        Arguments,
        Wait,
        Component_

    };

    public enum SqlScriptColumns
    {
        Script,
        SqlDb_,
        Component_,
        ScriptBinary_,
        User_,
        Attributes,
        Sequence

    };


    public enum SqlDatabaseColumns
    {
        SqlDb,
        Server,
        Instance,
        Database,
        Component_,
        User_,
        FileSpec_,
        FileSpec_Log,
        Attributes
    };

    public enum SqlFileSpecColumns
    {
        FileSpec,
        Name,
        Filename,
        Size,
        MaxSize,
        GrowthSize
    };

    public enum SqlStringColumns
    {
        String,
        SqlDb_,
        Component_,
        SQL,
        User_,
        Attributes,
        Sequence
    };

    public enum UserColumns
    {
        User,
        Component_,
        Name,
        Domain,
        Password,
        Attributes
    };

    public enum UserGroupColumns
    {
        User_,
        Group_
    };

    public enum WixCloseApplicationColumns
    {
        WixCloseApplication,
        Target,
        Description,
        Condition,
        Attributes,
        Sequence,
        Property
    };

    public enum WixInternetShortcutColumns
    {
        WixInternetShortcut,
        Component_,
        Directory_,
        Name,
        Target,
        Attributes
    };

    public enum XmlConfigColumns
    {
        XmlConfig,
        File,
        ElementPath,
        VerifyPath,
        Name,
        Value,
        Flags,
        Component_,
        Sequence
    };

    public enum XmlFileColumns
    {
        XmlFile,
        File,
        ElementPath,
        Name,
        Value,
        Flags,
        Component_,
        Sequence
    };


    /// <summary>
    /// Data structure to encabsulate a generic MSI Table Row
    /// </summary>
    public class TableRow
    {
        private string key;
        private string value;
        private bool isString;

        /// <summary>
        /// Create a new new MSI table entry, the value will be treated as a string.
        /// </summary>
        /// <param name="newKey">Name of the column</param>
        /// <param name="newValue">Value</param>
        public TableRow(string newKey, string newValue)
            : this(newKey, newValue, true)
        {

        }

        /// <summary>
        /// Create a new new MSI table entry
        /// </summary>
        /// <param name="newKey">Name of the column</param>
        /// <param name="newValue">Value</param>
        /// <param name="isString">Is the value a string</param>
        public TableRow(string newKey, string newValue, bool isString)
        {
            this.key = newKey;
            this.value = newValue;
            this.isString = isString;
        }

        /// <summary>
        /// Name of the column
        /// </summary>
        public string Key
        {
            get
            {
                return this.key;
            }
        }

        /// <summary>
        /// Value 
        /// </summary>
        public string Value
        {
            get
            {
                return this.value;
            }
        }

        /// <summary>
        /// Is the value a string, and thus string comparison can be used
        /// </summary>
        public bool IsString
        {
            get
            {
                return this.isString;
            }
        }
    }

    /// <summary>
    /// Object that will contain Custom action table data.
    /// </summary>
    public class CustomActionTableData
    {
        private string action;
        private int type;
        private string source;
        private string target;

        public CustomActionTableData(string action, int type, string source, string target)
        {
            this.action = action;
            this.type = type;
            this.source = source;
            this.target = target;
        }

        public string Action
        {
            get { return this.action; }
        }

        public int Type
        {
            get { return this.type; }
        }

        public string Source
        {
            get { return this.source; }
        }

        public string Target
        {
            get { return this.target; }
        }
    }
}