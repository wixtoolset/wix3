---
title: Lit Task
layout: documentation
---
# Lit Task

The Lit task wraps [lit.exe](~/overview/lit.html), the WiX library creation tool. It supports a variety of settings that are described in more detail below. To control these settings in your .wixproj file, you can create a PropertyGroup and specify the settings that you want to use for your build process. The following is a sample PropertyGroup that contains settings that will be used by the Lit task:

<pre><font size="2" color="#0000FF">&lt;</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">LibTreatWarningsAsErrors</font><font size="2" color="#0000FF">&gt;</font><font size="2">False</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">LibTreatWarningsAsErrors</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">LibVerboseOutput</font><font size="2" color="#0000FF">&gt;</font><font size="2">True</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">LibVerboseOutput</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">SuppressSpecificWarnings</font><font size="2" color="#0000FF">&gt;</font><font size="2">1111</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">SuppressSpecificWarnings</font><font size="2" color="#0000FF">&gt;
    &lt;</font><font size="2" color="#A31515">TreatSpecificWarningsAsErrors</font><font size="2" color="#0000FF">&gt;</font><font size="2">2222</font><font size="2" color="#0000FF">&lt;/</font><font size="2" color="#A31515">TreatSpecificWarningsAsErrors</font><font size="2" color="#0000FF">&gt;
&lt;/</font><font size="2" color="#A31515">PropertyGroup</font><font size="2" color="#0000FF">&gt;</font></pre>

The following table describes the common WiX MSBuild parameters that are applicable to the <b>Lit</b> task.

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

The following table describes the parameters that are specific to the <b>Lit</b> task.

<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td><b>Parameter</b></td>
    <td><b>Description</b></td>
  </tr>
  <tr>
    <td><b>LibAdditionalOptions</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies additional command line parameters to append when calling lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibBindFiles</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the library creation tool should bind files into a .wixout file. This is only valid when the OutputAsXml parameter is also provided. This is equivalent to the -bf switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibPedantic</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the library creation tool should display pedantic messages. This is equivalent to the -pedantic switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibSuppressAllWarnings</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all library creation tool warnings should be suppressed. This is equivalent to the -sw switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibSuppressIntermediateFileVersionMatching</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the library creation tool should suppress intermediate file version mismatch checking. This is equivalent to the -sv switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibSuppressSchemaValidation</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the library creation tool should suppress schema validation of documents. This is equivalent to the -ss switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibSuppressSpecificWarnings</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that certain library creation tool warnings should be suppressed. This is equivalent to the -sw[N] switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibTreatSpecificWarningsAsErrors</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies that certain library creation tool warnings should be treated as errors. This is equivalent to the -wx[N] switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibTreatWarningsAsErrors</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that all library creation tool warnings should be treated as errors. This is equivalent to the -wx switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LibVerboseOutput</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Specifies that the library creation tool should provide verbose output. This is equivalent to the -v switch in lit.exe.</td>
  </tr>
  <tr>
    <td><b>LinkerBindInputPaths</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies a binder path that the library creation tool should use to locate all files. This is equivalent to the -b &lt;path&gt; switch in lit.exe.<br />Named BindPaths are created by prefixing the 2-or-more-character bucket name followed by an equal sign ("=") to the supplied path.</td>
  </tr>
</table>
