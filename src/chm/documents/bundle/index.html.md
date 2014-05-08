---
title: Building Installation Package Bundles
layout: documentation
after: /wixui/
---
# Building Installation Package Bundles

In this section, we will cover the basics of creating a simple setup that produces a bundle using the WiX toolset.

A bundle is a collection of installation packages that are chained together in a single user experience. Bundles are often used to install prerequisites, such as the .NET Framework or Visual C++ runtime, before an application&apos;s .MSI file. Bundles also allow very large applications or suites of applications to be broken into smaller, logical installation packages while still presenting a single product to the end-user.

To create a seamless setup experience across multiple installation packages, the WiX toolset provides an engine (often referred to as a bootstrapper or chainer) named Burn. The Burn engine is an executable that hosts a DLL called the &quot;bootstrapper application&quot;. The bootstrapper application DLL is responsible for displaying UI to the end-user and directs the Burn engine when to carry out download, install, repair and uninstall actions. Most developers will not need to interact directly with the Burn engine because the WiX toolset provides a standard bootstrapper application and the language necessary to create bundles.

Creating bundles with the WiX toolset is directly analogous to creating Windows Installer packages (.MSI files) using the language and standard UI extension provided by the WiX toolset.

This section will give you an overview of the WiX bundle language and how to use it to create a bundle.

*  [Create the Skeleton Bundle Authoring](authoring_bundle_skeleton.html)
*  [Author the Bootstrapper Application for a Bundle](authoring_bundle_application.html)
*  [Author a Bundle Package Manifest](authoring_bundle_package_manifest.html)
*  [Burn Built-In Variables](bundle_built_in_variables.html)
*  [Define Searches Using Variables](bundle_define_searches.html)
*  [Chain Packages into a Bundle](bundle_author_chain.html)
*  [Working with WiX Standard Bootstrapper Application](wixstdba/index.html)
*  [Building a Bootstrapper Application](ba/index.html)
