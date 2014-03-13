---
title: Creating patches
layout: documentation
after: /customactions/
---

# Creating patches

Patches are updates to a product or products. WiX supports two different ways of creating them:

* [Using Patch Creation Properties](patch_building.html) which requires that you have the Windows Installer 3.0 or newer SDK installed for full support of included examples.
* [Using Purely WiX](wix_patching.html) which uses functionality provided in WiX and does not require additional tools.

There are also [restrictions](patch_restrictions.html) on how patches are built in order to avoid problems when installing them.

## How Patches Work

Patches contain a collection of transforms - most often a pair of transforms for each target product. When a patch is applied, each installed target product is reinstalled individually with the corresponding patch transforms applied. These transforms contain the differences between that target product and the upgrade product that might contain new file versions and sizes, new registry keys, etc.

For more information about patching with Windows Installer, read <a href="http://msdn.microsoft.com/en-us/library/aa370579.aspx" target="_blank">Patching and Upgrades</a>.
