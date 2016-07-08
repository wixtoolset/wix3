---
title: Candle Task
layout: documentation
---

# Candle Task

The Candle task wraps [candle.exe](~/overview/candle.html), the WiX compiler. It supports a variety of settings that are described in more detail below. To control these settings in your .wixproj file, you can create a PropertyGroup and specify the settings that you want to use for your build process. The following is a sample PropertyGroup that contains settings that will be used by the Candle task:

<pre><font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">CompilerTreatWarningsAsErrors</font><font size="2" color="#0000FF">&gt;</font><font size="2">False</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">CompilerTreatWarningsAsErrors</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">CompilerVerboseOutput</font><font size="2" color="#0000FF">&gt;</font><font size="2">True</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">CompilerVerboseOutput</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">DefineConstants</font><font size="2" color="#0000FF">&gt;</font><font size="2">Variable1=value1;Variable2=value2</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">DefineConstants</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">InstallerPlatform</font><font size="2" color="#0000FF">&gt;</font><font size="2">x86</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">InstallerPlatform</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">SuppressSpecificWarnings</font><font size="2" color="#0000FF">&gt;</font><font size="2">1111</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">SuppressSpecificWarnings</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">TreatSpecificWarningsAsErrors</font><font size="2" color="#0000FF">&gt;</font><font size="2">2222</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">TreatSpecificWarningsAsErrors</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;</font></pre>

The following table describes the common WiX MSBuild parameters that are applicable to the <b>Candle</b> task.

<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td><b>Parameter</b></td>
    <td><b>Description</b></td>
  </tr>
  <tr>
    <td><b>SuppressAllWarnings</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all warnings should be suppressed. This is equivalent to the -sw switch.</td>
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
    <td></td>
  </tr>
  <tr>
    <td><b>VerboseOutput</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the tool should provide verbose output. This is equivalent to the -v switch.</td>
  </tr>
</table>

The following table describes the parameters that are specific to the <b>Candle</b> task.

<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td><b>Parameter</b></td>
    <td><b>Description</b></td>
  </tr>
  <tr>
    <td><b>CompilerAdditionalOptions</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies additional command line parameters to append when calling candle.exe.</td>
  </tr>
  <tr>
    <td><b>CompilerSuppressAllWarnings</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all compiler warnings should be suppressed. This is equivalent to the -sw switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>CompilerSuppressSchemaValidation</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the compiler should suppress schema validation of documents. This is equivalent to the -ss switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>CompilerSuppressSpecificWarnings</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that certain compiler warnings should be suppressed. This is equivalent to the -sw[N] switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>CompilerTreatSpecificWarningsAsErrors</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that certain compiler warnings should be treated as errors. This is equivalent to the -wx[N] switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>CompilerTreatWarningsAsErrors</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all compiler warnings should be treated as errors. This is equivalent to the -wx switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>CompilerVerboseOutput</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the compiler should provide verbose output. This is equivalent to the -v switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>DefineConstants</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies a semicolon-delimited list of preprocessor variables. This is equivalent to the -d&lt;name&gt;[=&lt;value&gt;] switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>SuppressFilesVitalByDefault</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the compiler should suppress marking files as vital by default. This is equivalent to the -sfdvital switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>PreprocessToStdOut</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the compiler should output preprocessing information to stdout. This is equivalent to the -p switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>PreprocessToFile</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that the compiler should output preprocessing information to a file. This is equivalent to the -p&lt;file&gt; switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>IncludeSearchPaths</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies directories to add to the compiler include search path. This is equivalent to the -I&lt;dir&gt; switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>InstallerPlatform</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies the processor architecture for the package. Valid values are x86, x64, and ia64. (Deprecated values include intel for x86 and intel64 for ia64.) This is equivalent to the -arch switch in candle.exe.<br />
    <br />
    Sets the sys.BUILDARCH preprocessor variable and, when the value is x64 or ia64, defaults the Win64 attribute to "yes" on all Package, Component, CustomAction, and RegistrySearch elements in the source file.</td>
  </tr>
  <tr>
    <td><b>OnlyValidateDocuments</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the compiler should only validate documents. This is equivalent to the -zs switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>Pedantic</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the compiler should display pedantic messages. This is equivalent to the -pedantic switch in candle.exe.</td>
  </tr>
  <tr>
    <td><b>ShowSourceTrace</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the compiler should show source trace information for errors, warnings and verbose messages. This is equivalent to the -trace switch in candle.exe.</td>
  </tr>
</table>
