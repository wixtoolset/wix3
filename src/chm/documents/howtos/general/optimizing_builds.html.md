---
title: How To: Optimize build speed
layout: documentation
---

# How To: Optimize build speed

WiX provides two ways of speeding up the creation of cabinets for compressing files:

* Multithreaded cabinet creation.
* Cabinet reuse.

## Multithreaded cabinet creation

Light uses multiple threads to build multiple cabinets in a single package. Unfortunately, because the CAB API itself isn&apos;t multithreaded, a single cabinet is built with one thread. Light uses multiple threads when there are multiple cabinets, so each cabinet is built on one thread.

By default, Light uses the number of processors/cores in the system as the number of threads to use when creating cabinets. You can override the default using Light&apos;s -ct switch or the CabinetCreationThreadCount property in a .wixproj project.

You can use multiple cabinets both externally and embedded in the .msi package (using the [Media/@EmbedCab](~/xsd/wix/media.html) attribute).

## Cabinet reuse

If you build setups with files that don&apos;t change often, you can generate cabinets for those files once, then reuse them without spending the CPU time to re-build and re-compress them.

There are two Light.exe switches involved in cabinet reuse:

<dl>
  <dt><dfn>-cc (CabinetCachePath property in .wixproj projects)</dfn><dd>The value is the path to use to both write new cabinets and, when -reusecab/ReuseCabinetCache is specified, look for cached cabinets.</dd></dt>
  <dt><dfn>-reusecab (ReuseCabinetCache property in .wixproj projects)</dfn><dd>When -cc/CabinetCachePath is also specified, WiX reuses cabinets that don&apos;t need to be rebuilt.</dd></dt>
</dl>

WiX automatically validates that a cached cabinet is still valid by ensuring that:

* The number of files in the cached cabinet matches the number of files being built.
* The names of the files are all identical.
* The order of files is identical.
* The timestamps for all files all identical.
