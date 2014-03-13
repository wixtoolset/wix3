---
title: Insignia
layout: documentation
after: heat
---

# Insignia

Insignia is a tool used for inscribing an MSI with the digital signatures that its external CABs are signed with.

To sign your external cabs with Insignia, first build your MSI normally,
and sign your cabs manually. Then call Insignia with the path to your MSI -
Insignia will update your MSI with the digital signature information of its
associated external cabs. The file will be updated in-place. Then sign your MSI.
This will allow windows installer to verify, at install-time, that the external
cabs haven&apos;t changed since you built them. For example:

    insignia -im setup.msi

If you use MSBuild, an easier method for doing this exists. In your .wixproj file,
set the &quot;SignOutput&quot; property to &quot;true&quot;. Then override the &quot;SignCabs&quot; target,
using the &quot;SignCabs&quot; property as a list of cabs to sign, to sign the external cabs.
Here&apos;s an example signing those cabs using signtool.exe:

      <Target Name="SignCabs">
        <Exec Command="Signtool.exe sign /a &quot;%(SignCabs.FullPath)&quot;" />
      </Target>

Finally, override the &quot;SignMsi&quot; target. Here&apos;s a similar example, also using signtool.exe.

      <Target Name="SignMsi">
        <Exec Command="signtool.exe sign /a &quot;%(SignMsi.FullPath)&quot;" />
      </Target>

This will cause the build process, after linking the MSI, to sign any external cabs, inscribe your MSI
with the digital signatures of those cabs, and then sign the MSI, all at the appropriate times during the build process.

Insignia can also be used to detach and re-attach the burn engine from a bundle, so that
it can be signed. For example:

    insignia -ib bundle.exe -o engine.exe
    ... sign engine.exe
    insignia -ab engine.exe bundle.exe -o bundle.exe
    ... sign bundle.exe

Again, there is an easier method with MSBuild. Set the &quot;SignOutput&quot; 
property to &quot;true&quot;, then override the &quot;SignBundleEngine&quot; and 
&quot;SignBundle&quot; targets. For example:

      <Target Name="SignBundleEngine">
        <Exec Command="Signtool.exe sign /a &quot;@(SignBundleEngine)&quot;" />
      </Target>
      <Target Name="SignBundle">
        <Exec Command="Signtool.exe sign /a &quot;@(SignBundle)&quot;" />
      </Target>
  
<!-- TODO: mention the SignContainers target -->
