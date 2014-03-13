---
title: ShellExecute CustomAction
layout: documentation
after: using_standard_customactions
---

# ShellExecute CustomAction

The WixShellExec custom action in wixca (part of WixUtilExtension) lets you open document or URL targets via the Windows shell. A common use is to launch readme files or URLs using their registered default applications based on their extension. Note that WixShellExecute can only be used as an immediate custom action as it launches an application without waiting for it to close. WixShellExec reads its target from the WixShellExecTarget property, formats it, and then calls ShellExecute with the formatted value. It uses the default verb, which is usually &quot;open.&quot; For more information, see <a href="http://msdn.microsoft.com/library/bb762153.aspx" target="_blank">ShellExecute Function</a>.

For a step-by-step example of how to use the ShellExecute custom action to launch a program at the end of install see the [How To: Run the Installed Application After Setup](~/howtos/ui_and_localization/run_program_after_install.html) topic.
