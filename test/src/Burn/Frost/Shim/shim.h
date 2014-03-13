//-------------------------------------------------------------------------------------------------
// <copyright file="shim.h" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
//    Precompiled header for Frost shim
// </summary>
//-------------------------------------------------------------------------------------------------

#pragma once

namespace Microsoft
{
namespace Tools
{
namespace WindowsInstallerXml
{
namespace Test
{
namespace Frost
{

    public enum class CommandID // FROM WinUser.h
    {
        ID_OK = IDOK,
        ID_CANCEL = IDCANCEL,
        ID_ABORT = IDABORT,
        ID_RETRY = IDRETRY,
        ID_IGNORE = IDIGNORE,
        ID_YES = IDYES,
        ID_NO = IDNO
    };

    public enum class HRESULTS
    {
        HR_FAILURE = -1,
        HR_S_OK = 0,
        HR_S_FALSE = 1,
    };

    //From IBurnCore.h
    public enum class PKG_ACTION_STATE
    {
        PKG_ACTION_STATE_NONE = ACTION_STATE_NONE,
        PKG_ACTION_STATE_UNINSTALL = ACTION_STATE_UNINSTALL,
        PKG_ACTION_STATE_INSTALL = ACTION_STATE_INSTALL,
        PKG_ACTION_STATE_ADMIN_INSTALL = ACTION_STATE_ADMIN_INSTALL,
        PKG_ACTION_STATE_MAINTENANCE = ACTION_STATE_MAINTENANCE,
        PKG_ACTION_STATE_RECACHE = ACTION_STATE_RECACHE,
        PKG_ACTION_STATE_MINOR_UPGRADE = ACTION_STATE_MINOR_UPGRADE,
        PKG_ACTION_STATE_MAJOR_UPGRADE = ACTION_STATE_MAJOR_UPGRADE,
        PKG_ACTION_STATE_PATCH = ACTION_STATE_PATCH,
    };
    public enum class CUR_PACKAGE_STATE
    {
        CUR_PACKAGE_STATE_UNKNOWN = PACKAGE_STATE_UNKNOWN,
        CUR_PACKAGE_STATE_ABSENT = PACKAGE_STATE_ABSENT,
        CUR_PACKAGE_STATE_CACHED = PACKAGE_STATE_CACHED,
        CUR_PACKAGE_STATE_PRESENT = PACKAGE_STATE_PRESENT,
    };
    public enum class PKG_REQUEST_STATE
    {
        PKG_REQUEST_STATE_NONE = REQUEST_STATE_NONE,
        PKG_REQUEST_STATE_ABSENT = REQUEST_STATE_ABSENT,
        PKG_REQUEST_STATE_CACHE = REQUEST_STATE_CACHE,
        PKG_REQUEST_STATE_PRESENT = REQUEST_STATE_PRESENT,
        PKG_REQUEST_STATE_REPAIR = REQUEST_STATE_REPAIR,
    };

    public enum class ENGINE_LOG_LEVEL
    {
        ENGINE_LOG_LEVEL_NONE     = BURN_LOG_LEVEL_NONE,      // turns off report (only valid for XXXSetLevel())
        ENGINE_LOG_LEVEL_STANDARD = BURN_LOG_LEVEL_STANDARD,  // written if reporting is on
        ENGINE_LOG_LEVEL_VERBOSE  = BURN_LOG_LEVEL_VERBOSE,   // written only if verbose reporting is on
        ENGINE_LOG_LEVEL_DEBUG    = BURN_LOG_LEVEL_DEBUG,     // reporting useful when debugging code
        ENGINE_LOG_LEVEL_ERROR    = BURN_LOG_LEVEL_ERROR,     // always gets reported, but can never be specified
    };

    //From IBurnUserExperience.h
    public enum class SETUP_ACTION // BURN_ACTION
    {
        SETUP_ACTION_UNKNOWN   = BURN_ACTION_UNKNOWN,
        SETUP_ACTION_HELP      = BURN_ACTION_HELP,
        SETUP_ACTION_UNINSTALL = BURN_ACTION_UNINSTALL,
        SETUP_ACTION_INSTALL   = BURN_ACTION_INSTALL,
        SETUP_ACTION_MODIFY    = BURN_ACTION_MODIFY,
        SETUP_ACTION_REPAIR    = BURN_ACTION_REPAIR,
    };
    public enum class SETUP_DISPLAY // BURN_DISPLAY
    {
        SETUP_DISPLAY_UNKNOWN = BURN_DISPLAY_UNKNOWN,
        SETUP_DISPLAY_NONE = BURN_DISPLAY_NONE,
        SETUP_DISPLAY_PASSIVE = BURN_DISPLAY_PASSIVE,
        SETUP_DISPLAY_FULL = BURN_DISPLAY_FULL,
    };
    public enum class SETUP_RESTART // BURN_RESTART
    {
        SETUP_RESTART_UNKNOWN = BURN_RESTART_UNKNOWN,
        SETUP_RESTART_NEVER = BURN_RESTART_NEVER,
        SETUP_RESTART_PROMPT = BURN_RESTART_PROMPT,
        SETUP_RESTART_AUTOMATIC = BURN_RESTART_AUTOMATIC,
        SETUP_RESTART_ALWAYS = BURN_RESTART_ALWAYS,
    };

    public enum class SETUP_RESUME //BURN_RESUME
    {
        SETUP_RESUME_NONE = BURN_RESUME_TYPE_NONE,
        SETUP_RESUME_INVALID = BURN_RESUME_TYPE_INVALID,
        SETUP_RESUME_UNEXPECTED = BURN_RESUME_TYPE_UNEXPECTED,
        SETUP_RESUME_REBOOT_PENDING = BURN_RESUME_TYPE_REBOOT_PENDING,
        SETUP_RESUME_REBOOT = BURN_RESUME_TYPE_REBOOT,
        SETUP_RESUME_SUSPEND = BURN_RESUME_TYPE_SUSPEND,
        SETUP_RESUME_ARP = BURN_RESUME_TYPE_ARP,
    };

    public value struct SETUP_COMMAND
    {
        SETUP_ACTION action;
        SETUP_DISPLAY display;
        SETUP_RESTART restart;
    };
}
}
}
}
}
