Steps to create an MSI that doesn't evoke the UAC prompts are listed on this blog http://blogs.msdn.com/astebner/archive/2007/11/18/6385121.aspx

Summary is as follows:
1). Ensure that no actions performed in your MSI require Elevation (all files and registry must be created in per-user location, no program files or windows folder or HKLM keys)
2). Set the ALLUSERS property to an empty string
3). Set bit 3 of the "Word Count summary property" to 8 to indicate that elevate privileges are not required