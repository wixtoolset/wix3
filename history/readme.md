The release build will move the contents of each file in this directory into History.md, based on when the file was added to the current branch.
This replaces the old way of everyone directly modifying History.md because that produced endless merge conflicts.

When submitting a pull request that includes something that WiX users would be interested to know about, create a text file in this directory.
The name of the file would ideally be the number of the issue from the issue tracker that is being addressed, but the only requirement is that the name is unique.
The build only uses the file name to go through the git history to find when it was created.
The contents of the file will be copied as is into History.md.
Here are a couple of examples:

    * RobMen: WIXBUG:4732 - fix documentation links to MsiServiceConfig and MsiServiceConfigFailureActions.

    * BobArnson: WIXFEAT:4719 - Implement ExePackage/CommandLine:
      * Add WixBundleExecutePackageAction variable: Set to the BOOTSTRAPPER_ACTION_STATE of the package as it's about to executed.
      * Add ExePackage/CommandLine to compiler and binder.
      * Update Burn to parse CommandLine table in manifest and apply it during ExePackage execution.

Each line should begin with an asterisk (`*`), then the contributer's name (normally abbreviated to 9 characters or less), then WIXBUG or WIXFEAT and the issue number, and then a brief description of what was changed.