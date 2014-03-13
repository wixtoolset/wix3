This msi contains a type 19 Custom Action called "failme" that will cause it to fail to install.
The "failme" custom action will not run (i.e. won't fail the install) if the property PASSME=1 is set.

msiexec.exe /i hello_world.msi             - Install will fail
msiexec.exe /i hello_world.msi PASSME=1    - Install will succeed
msiexec.exe /x hello_world.msi             - Uninstall will succeed (uninstall always works, the failme custom action is conditioned to never run during uninstall)

hello_world3.msi - This msi FailMe CA which requires PASSME=1 for successful install and uninstall

msiexec.exe /i hello_world3.msi             - Install will fail
msiexec.exe /i hello_world3.msi PASSME=1    - Install will succeed
msiexec.exe /x hello_world3.msi             - Uninstall will fail
msiexec.exe /x hello_world3.msi PASSME=1    - Uninstall will succeed