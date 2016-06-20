---
title: Light Task
layout: documentation
---
# Light Task

The Light task wraps [light.exe](~/overview/light.html), the WiX linker. It supports a variety of settings that are described in more detail below. To control these settings in your .wixproj file, you can create a PropertyGroup and specify the settings that you want to use for your build process. The following is a sample PropertyGroup that contains settings that will be used by the Light task:

<pre><font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">LinkerTreatWarningsAsErrors</font><font size="2" color="#0000FF">&gt;</font><font size="2">False</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">LinkerTreatWarningsAsErrors</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">LinkerVerboseOutput</font><font size="2" color="#0000FF">&gt;</font><font size="2">True</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">LinkerVerboseOutput</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">SuppressIces</font><font size="2" color="#0000FF">&gt;</font><font size="2">ICE18;ICE45;ICE82</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">SuppressIces</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">SuppressSpecificWarnings</font><font size="2" color="#0000FF">&gt;</font><font size="2">1111</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">SuppressSpecificWarnings</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">TreatSpecificWarningsAsErrors</font><font size="2" color="#0000FF">&gt;</font><font size="2">2222</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">TreatSpecificWarningsAsErrors</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">WixVariables</font><font size="2" color="#0000FF">&gt;</font><font size="2">Variable1=value1;Variable2=value2</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">WixVariables</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;</font></pre>

The following table describes the common WiX MSBuild parameters that are applicable to the <b>Light</b> task.

<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td><b>Parameter</b></td>
    <td><b>Description</b></td>
  </tr>
  <tr>
    <td><b>BindInputPaths</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies a binder path that should be used to locate all files. This is equivalent to the -b &lt;path&gt; switch.<br />Named BindPaths are created by prefixing the 2-or-more-character bucket name followed by an equal sign ("=") to the supplied path.</td>
  </tr>
  <tr>
    <td><b>BindFiles</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the tool should bind files into a .wixout file. This is only valid when the OutputAsXml parameter is also provided. This is equivalent to the -bf switch.</td>
  </tr>
  <tr>
    <td><b>Pedantic</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the tool should display pedantic messages. This is equivalent to the -pedantic switch.</td>
  </tr>
  <tr>
    <td><b>SuppressAllWarnings</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all warnings should be suppressed. This is equivalent to the -sw switch.</td>
  </tr>
  <tr>
    <td><b>SuppressIntermediateFileVersionMatching</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the tool should suppress intermediate file version mismatch checking. This is equivalent to the -sv switch.</td>
  </tr>
  <tr>
    <td><b>SuppressSchemaValidation</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that schema validation of documents should be suppressed. This is equivalent to the -ss switch.</td>
  </tr>
  <tr>
    <td><b>SuppressSpecificWarnings</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that certain warnings should be suppressed. This is equivalent to the -sw[N] switch.</td>
  </tr>
  <tr>
    <td><b>TreatSpecificWarningsAsErrors</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that certain warnings should be treated as errors. This is equivalent to the -wx[N] switch.</td>
  </tr>
  <tr>
    <td><b>TreatWarningsAsErrors</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all warnings should be treated as errors. This is equivalent to the -wx switch.</td>
  </tr>
  <tr>
    <td><b>VerboseOutput</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the tool should provide verbose output. This is equivalent to the -v switch.</td>
  </tr>
</table>

The following table describes the parameters that are specific to the <b>Light</b> task.

<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td><b>Parameter</b></td>
    <td><b>Description</b></td>
  </tr>
  <tr>
    <td><b>AllowIdenticalRows</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should allow identical rows. Identical rows will be treated as warnings. This is equivalent to the -ai switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>AllowDuplicateDirectoryIds</b></td>

    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should allow duplicate directory identifiers. This allows duplicate directories from different libraries to be merged into the product. This is equivalent to the -ad switch in light.exe.</td>
  </tr>

  <tr>
    <td><b>AllowUnresolvedReferences</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should allow unresolved references. This will not create valid output. This is equivalent to the -au switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>AdditionalCub</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies an additional .cub file that the linker should use when running ICE validation. This is equivalent to the -cub &lt;file.cub&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>BackwardsCompatibleGuidGeneration</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should use the backward compatible GUID generation algorithm. This is equivalent to the -bcgg switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>CabinetCachePath</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies a path that the linker should use to cache built cabinet files. This is equivalent to the -cc &lt;path&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>CabinetCreationThreadCount</b></td>
    <td>Optional <b>integer</b> parameter.<br />
    <br />
    Specifies that number of threads that the linker should use when building cabinet files. This is equivalent to the -ct &lt;N&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>Cultures</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies a semicolon or comma delimited list of localized string cultures to load from .wxl files and libraries.  Precedence of cultures is from left to right. This is equivalent to the -cultures:&lt;cultures&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>DefaultCompressionLevel</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies the compression level that the linker should use when building cabinet files. Valid values are low, medium, high, none and mszip. This is equivalent to the -dcl:&lt;level&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>DropUnrealTables</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should drop unreal tables from the output image. This is equivalent to the -dut switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>ExactAssemblyVersions</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should use exact assembly versions. This is equivalent to the -eav switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>Ices</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that the linker should run specific internal consistency evaluators (ICEs). This is equivalent to the -ice:&lt;ICE&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LeaveTemporaryFiles</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should not delete temporary files. This is equivalent to the -notidy switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerAdditionalOptions</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies additional command line parameters to append when calling light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerBindInputPaths</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies a binder path that the linker should use to locate all files. This is equivalent to the -b &lt;path&gt; switch in light.exe.<br />Named BindPaths are created by prefixing the 2-or-more-character bucket name followed by an equal sign ("=") to the supplied path.</td>
  </tr>
  <tr>
    <td><b>LinkerBindFiles</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should bind files into a .wixout file. This is only valid when the OutputAsXml parameter is also provided. This is equivalent to the -bf switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerPedantic</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should display pedantic messages. This is equivalent to the -pedantic switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerSuppressAllWarnings</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all linker warnings should be suppressed. This is equivalent to the -sw switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerSuppressIntermediateFileVersionMatching</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress intermediate file version mismatch checking. This is equivalent to the -sv switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerSuppressSchemaValidation</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress schema validation of documents. This is equivalent to the -ss switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerSuppressSpecificWarnings</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that certain linker warnings should be suppressed. This is equivalent to the -sw[N] switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerTreatSpecificWarningsAsErrors</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that certain linker warnings should be treated as errors. This is equivalent to the -wx[N] switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerTreatWarningsAsErrors</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all linker warnings should be treated as errors. This is equivalent to the -wx switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerVerboseOutput</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should provide verbose output. This is equivalent to the -v switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>OutputAsXml</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should output a .wixout file instead of a .msi file. This is equivalent to the -xo switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>PdbOutputFile</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that the linker should create the output .wixpdb file with the provided name. This is equivalent to the -pdbout &lt;output.wixpdb&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>ReuseCabinetCache</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should reuse cabinet files from the cabinet cache. This is equivalent to the -reusecab switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SetMsiAssemblyNameFileVersion</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should add a fileVersion entry to the MsiAssemblyName table for each assembly. This is equivalent to the -fv switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressAclReset</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress resetting ACLs. This is useful when laying out an image to a network share. This is equivalent to the -sacl switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressAssemblies</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should not get assembly name information for assemblies. This is equivalent to the -sa switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressDefaultAdminSequenceActions</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress default admin sequence actions. This is equivalent to the -sadmin switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressDefaultAdvSequenceActions</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress default advertised sequence actions. This is equivalent to the -sadv switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressDefaultUISequenceActions</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress default UI sequence actions. This is equivalent to the -ui switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressFileHashAndInfo</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress gathering file information (hash, version, language, etc). This is equivalent to the -sh switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressFiles</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress gathering all file data. This has the same effect as setting the SuppressAssemblies adn SuppressFileHashAndInfo parameters. This is equivalent to the -sf switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressIces</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that the linker should suppress running specific ICEs. This is equivalent to the -sice:&lt;ICE&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressLayout</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress layout creation. This is equivalent to the -sl switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressMsiAssemblyTableProcessing</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress processing the data in the MsiAssembly table. This is equivalent to the -sma switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressPatchSequenceData</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress patch sequence data in patch XML to decrease bundle size and increase patch applicability performance (patch packages themselves are not modified).</td>
  </tr>
  <tr>
    <td><b>SuppressPdbOutput</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress outputting .wixpdb files. This is equivalent to the -spdb switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressValidation</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress .msi and .msm validation. This is equivalent to the -sval switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressTagSectionIdAttributeOnTuples</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the linker should suppress adding the sectionId attribute on rows. This is equivalent to the -sts switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>UnreferencedSymbolsFile</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies an unreferenced symbols file that the linker should use. This is equivalent to the -usf &lt;output.xml&gt; switch in light.exe.</td>
  </tr>
  <tr>
    <td><b>WixVariables</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies a semicolon-delimited list of bind-time WiX variables. This is equivalent to the -d&lt;name&gt;[=&lt;value&gt;] switch in light.exe.</td>
  </tr>
</table>
