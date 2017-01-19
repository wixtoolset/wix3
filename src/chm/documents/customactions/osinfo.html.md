---
title: OSInfo custom actions
layout: documentation
after: using_standard_customactions
---
# OSInfo custom actions

The WixQueryOsInfo, WixQueryOsDirs, and WixQueryOsDriverInfo custom actions in wixca (part of WixUtilExtension) set properties over and above the MSI set for OS product/suite detection and standard directories. The WixQueryOsWellKnownSID custom action sets properties for the localized names of some built in Windows users and groups.

To use these custom actions you simply need to add a [&lt;PropertyRef&gt;](~/xsd/wix/propertyref.html) to the property you want to use and then include WixUtilExtensions when linking. For example:

    <PropertyRef Id="WIX_SUITE_SINGLEUSERTS" />
    <PropertyRef Id="WIX_DIR_COMMON_DOCUMENTS" />
    <PropertyRef Id="WIX_ACCOUNT_LOCALSERVICE" />

WixUtilExtension will automatically schedule the custom actions as needed after the AppSearch standard action. For additional information about standard directory tokens in Windows and which ones are supported directly by Windows Installer, see the following topics in the MSDN documentation:

* <a href="http://msdn.microsoft.com/library/bb762494.aspx" target="_blank">Constant special item ID list (CSIDL) values</a>
* <a href="http://msdn.microsoft.com/library/aa372057.aspx" target="_blank">Windows Installer system folder values</a>

## WixQueryOsInfo Properties

<table cellspacing="0" cellpadding="4" class="style1">
<tr>
<td valign="top">
<p>WIX_SUITE_BACKOFFICE</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_BACKOFFICE flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_BLADE</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_BLADE flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_COMMUNICATIONS</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_COMMUNICATIONS flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_COMPUTE_SERVER</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_COMPUTE_SERVER flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_DATACENTER</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_DATACENTER flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_EMBEDDEDNT</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_EMBEDDEDNT flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_EMBEDDED_RESTRICTED</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_EMBEDDED_RESTRICTED flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_ENTERPRISE</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_ENTERPRISE flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_MEDIACENTER</p>
</td>

<td>
<p>Equivalent to the GetSystemMetrics SM_SERVERR2 flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_PERSONAL</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_PERSONAL flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_SECURITY_APPLIANCE</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_SECURITY_APPLIANCE flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_SERVERR2</p>
</td>

<td>
<p>Equivalent to the GetSystemMetrics SM_SERVERR2 flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_SINGLEUSERTS</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_SINGLEUSERTS flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_SMALLBUSINESS</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_SMALLBUSINESS flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_SMALLBUSINESS_RESTRICTED</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_SMALLBUSINESS_RESTRICTED&nbsp; flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_STARTER</p>
</td>

<td>
<p>Equivalent to the GetSystemMetrics SM_STARTER flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_STORAGE_SERVER</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_STORAGE_SERVER flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_TABLETPC</p>
</td>

<td>
<p>Equivalent to the GetSystemMetrics SM_TABLETPC flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_TERMINAL</p>
</td>

<td>
<p>Equivalent to the OSVERSIONINFOEX VER_SUITE_TERMINAL flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_SUITE_WH_SERVER</p>
</td>

<td>
<p>Windows Home Server. Equivalent to the OSVERSIONINFOEX VER_SUITE_WH_SERVER flag.</p>
</td>
</tr>
</table>

<h2>WixQueryOsDirs Properties</h2>

<table>
<tr>
<td valign="top">
<p>WIX_DIR_ADMINTOOLS</p>
</td>

<td>
<p>Per-user administrative tools directory. Equivalent to the SHGetFolderPath CSIDL_ADMINTOOLS flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_ALTSTARTUP</p>
</td>

<td>
<p>Per-user nonlocalized Startup program group. Equivalent to the SHGetFolderPath CSIDL_ALTSTARTUP flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_CDBURN_AREA</p>
</td>

<td>
<p>Per-user CD burning staging directory. Equivalent to the SHGetFolderPath CSIDL_CDBURN_AREA flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_COMMON_ADMINTOOLS</p>
</td>

<td>
<p>All-users administrative tools directory. Equivalent to the SHGetFolderPath CSIDL_COMMON_ADMINTOOLS flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_COMMON_ALTSTARTUP</p>
</td>

<td>
<p>All-users nonlocalized Startup program group. Equivalent to the SHGetFolderPath CSIDL_COMMON_ALTSTARTUP flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_COMMON_DOCUMENTS</p>
</td>

<td>
<p>All-users documents directory. Equivalent to the SHGetFolderPath CSIDL_COMMON_DOCUMENTS flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_COMMON_FAVORITES</p>
</td>

<td>
<p>All-users favorite items directory. Equivalent to the SHGetFolderPath CSIDL_COMMON_FAVORITES flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_COMMON_MUSIC</p>
</td>

<td>
<p>All-users music files directory. Equivalent to the SHGetFolderPath CSIDL_COMMON_MUSIC flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_COMMON_PICTURES</p>
</td>

<td>
<p>All-users picture files directory. Equivalent to the SHGetFolderPath CSIDL_COMMON_PICTURES flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_COMMON_VIDEO</p>
</td>

<td>
<p>All-users video files directory. Equivalent to the SHGetFolderPath CSIDL_COMMON_VIDEO flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_COOKIES</p>
</td>

<td>
<p>Per-user Internet Explorer cookies directory. Equivalent to the SHGetFolderPath CSIDL_COOKIES flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_DESKTOP</p>
</td>

<td>
<p>Per-user desktop directory. Equivalent to the SHGetFolderPath CSIDL_DESKTOP flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_HISTORY</p>
</td>

<td>
<p>Per-user Internet Explorer history directory. Equivalent to the SHGetFolderPath CSIDL_HISTORY flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_INTERNET_CACHE</p>
</td>

<td>
<p>Per-user Internet Explorer cache directory. Equivalent to the SHGetFolderPath CSIDL_INTERNET_CACHE flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_MYMUSIC</p>
</td>

<td>
<p>Per-user music files directory. Equivalent to the SHGetFolderPath CSIDL_MYMUSIC flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_MYPICTURES</p>
</td>

<td>
<p>Per-user picture files directory. Equivalent to the SHGetFolderPath CSIDL_MYPICTURES flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_MYVIDEO</p>
</td>

<td>
<p>Per-user video files directory. Equivalent to the SHGetFolderPath CSIDL_MYVIDEO flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_NETHOOD</p>
</td>

<td>
<p>Per-user My Network Places link object directory. Equivalent to the SHGetFolderPath CSIDL_NETHOOD flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_PERSONAL</p>
</td>

<td>
<p>Per-user documents directory. Equivalent to the SHGetFolderPath CSIDL_PERSONAL flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_PRINTHOOD</p>
</td>

<td>
<p>Per-user Printers link object directory. Equivalent to the SHGetFolderPath CSIDL_PRINTHOOD flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_PROFILE</p>
</td>

<td>
<p>Per-user profile directory. Equivalent to the SHGetFolderPath CSIDL_PROFILE flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_RECENT</p>
</td>

<td>
<p>Per-user most recently used documents shortcut directory. Equivalent to the SHGetFolderPath CSIDL_RECENT flag.</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_DIR_RESOURCES</p>
</td>

<td>
<p>All-users resource data directory. Equivalent to the SHGetFolderPath CSIDL_RESOURCES flag.</p>
</td>
</tr>
</table>

<h2>WixQueryOsWellKnownSID properties</h2>

<table>
<tr>
<td valign="top">
<p>WIX_ACCOUNT_LOCALSYSTEM</p>
</td>

<td>
<p>Localized qualified name of the Local System account (WinLocalSystemSid).</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_ACCOUNT_LOCALSERVICE</p>
</td>

<td>
<p>Localized qualified name of the Local Service account (WinLocalServiceSid).</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_ACCOUNT_NETWORKSERVICE</p>
</td>

<td>
<p>Localized qualified name of the Network Service account (WinNetworkServiceSid).</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_ACCOUNT_ADMINISTRATORS</p>
</td>

<td>
<p>Localized qualified name of the Administrators group (WinBuiltinAdministratorsSid).</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_ACCOUNT_USERS</p>
</td>

<td>
<p>Localized qualified name of the Users group (WinBuiltinUsersSid).</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_ACCOUNT_GUESTS</p>
</td>

<td>
<p>Localized qualified name of the Users group (WinBuiltinGuestsSid).</p>
</td>
</tr>

<tr>
<td valign="top">
<p>WIX_ACCOUNT_PERFLOGUSERS, WIX_ACCOUNT_PERFLOGUSERS_NODOMAIN</p>
</td>

<td>
<p>Localized qualified name of the Performance Log Users group (WinBuiltinPerfLoggingUsersSid).</p>
</td>
</tr>
</table>

<h2>WixQueryOsDriverInfo properties</h2>

<table>
<tr>
<td valign="top">
<p>WIX_WDDM_DRIVER_PRESENT</p>
</td>

<td>
<p>Set to 1 if the video card driver on the target machine is a WDDM driver. This property is only set on machines running Windows Vista or higher.</p>
</td>
</tr>

<tr>
<td valign="top">WIX_DWM_COMPOSITION_ENABLED</td>

<td>Set to 1 if the target machine has composition enabled. This property is only set on machines running Windows Vista or higher.</td>
</tr>
</table>
