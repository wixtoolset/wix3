Dim msi : Set msi = CreateObject("WindowsInstaller.Installer")
Dim path : path = WScript.Arguments(0)
Dim xml : xml = msi.ExtractPatchXMLData(path)
WScript.Echo(xml)
