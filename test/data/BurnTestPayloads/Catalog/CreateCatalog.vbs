Option Explicit

dim rootFolder
dim burnDataFolder
dim catalogFolder
dim cdfPath
dim catPath
dim cdfWriter
dim fso
dim WShell
dim fileCount
dim command
dim exitCode

' Get objects
set fso = WScript.CreateObject("Scripting.FileSystemObject")
set WShell = WScript.CreateObject("WScript.Shell")

' Set up paths
rootFolder = WShell.ExpandEnvironmentStrings("%WIX_ROOT%")
burnDataFolder = fso.BuildPath(rootFolder, "test")
burnDataFolder = fso.BuildPath(burnDataFolder, "Data")
burnDataFolder = fso.BuildPath(burnDataFolder, "BurnTestPayloads")
catalogFolder = fso.BuildPath(burnDataFolder, "Catalog")
cdfPath = fso.BuildPath(catalogFolder, "BurnTestCatalog.cdf")
catPath = fso.BuildPath(catalogFolder, "BurnTestCatalog.cat")

' Start CDF file
if (fso.FileExists(cdfPath)) then
    fso.DeleteFile cdfPath, true
end if
set cdfWriter = fso.CreateTextFile(cdfPath, true, false)
fileCount = 0

' Write CDF header
cdfWriter.WriteLine "[CatalogHeader]"
cdfWriter.WriteLine "Name=" + catPath
cdfWriter.WriteLine "PublicVersion=0x0000001"
cdfWriter.WriteLine "EncodingType=0x00010001"
cdfWriter.WriteLine "PageHashes=false"
cdfWriter.WriteLine "CATATTR1=0x00010001:OSAttr:2:6.2,2:6.1,2:6.0,2:5.2,2:5.1"
cdfWriter.WriteLine ""
cdfWriter.WriteLine "[CatalogFiles]"

' Get all of the MSI, MSP, and EXE files under
GetCatalogFiles burnDataFolder
cdfWriter.Close

' Call makecat.exe to make the catalog file
command = "makecat.exe -v -n " + cdfPath
WScript.Echo "Running: " + command
exitCode = WShell.Run(command, 10, true)
WScript.Echo "MakeCat.exe exit code: " + CStr(exitCode)
WScript.Quit(exitCode)

' Method that writes all file information to the CDF file
sub GetCatalogFiles(byval folder)
    dim currentFolder
    dim currentFile
    dim childFolder
    dim extension
    dim fileName

    ' Get each file
    set currentFolder = fso.GetFolder(folder)
    for each currentFile in currentFolder.Files
        extension = UCase(fso.GetExtensionName(currentFile))
        fileName = fso.GetFileName(currentFile)
        if (("MSI" = extension) or ("MSP" = extension) or ("EXE" = extension)) then
            cdfWriter.WriteLine "<hash>" + CStr(fileCount) + "=" + currentFile
            cdfWriter.WriteLine "<hash>" + CStr(fileCount) + "ATTR1=0x10010001:File:" + fileName
            fileCount = fileCount + 1
        end if
    next

    ' Go through each folder
    for each childFolder in currentFolder.SubFolders
        GetCatalogFiles(childFolder)
    next
end sub
