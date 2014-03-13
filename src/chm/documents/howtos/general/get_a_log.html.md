---
title: How To: Get a Log of Your Installation for Debugging
layout: documentation
---
# How To: Get a Log of Your Installation for Debugging

When authoring installers it is often necessary to get a log of the installation for debugging purposes. This is particularly helpful when trying to debug file searches and launch conditions. To obtain a log of an installation use the <a href="http://support.microsoft.com/kb/227091" target="_blank">command line msiexec tool</a>:

    msiexec /i MyApplication.msi /l*v MyLogFile.txt

This will install your application and write a verbose log to MyLogFile.txt in the current directory.

If you need to get a log of your installer when it is launched from the Add/Remove Programs dialog you can <a href="http://support.microsoft.com/kb/223300" target="_blank">enable Windows Installer logging via the registry</a>.
