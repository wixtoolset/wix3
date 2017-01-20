---
title: Insignia Task
layout: documentation
---

# Insignia Task

The Insignia task wraps [insignia.exe](~/overview/insignia.html), the WiX inscribing/signing tool. It supports a variety of settings that are described in more detail below. To control these settings in your .wixproj file, you can create a PropertyGroup and specify the settings that you want to use for your process. You can refer to the [Candle Task](candle.html) for details about how to set up a PropertyGroup.

The following table describes the common WiX MSBuild parameters that are applicable to the <b>Insignia</b> task.

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

The following table describes the parameters that are specific to the <b>Insignia</b> task.

<table border="1" cellspacing="0" cellpadding="4">
  <tr>
    <td><b>Parameter</b></td>
    <td><b>Description</b></td>
  </tr>
  <tr>
    <td><b>BundleFile</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specify the bundle file to be used either to extract the engine from or to sign.</td>
  </tr>
  <tr>
    <td><b>OriginalBundleFile</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specify the original bundle file to be used for reattaching an engine bundle.</td>
  </tr>
  <tr>
    <td><b>DatabaseFile</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies the msi package to inscribe.</td>
  </tr>
  <tr>
    <td><b>OutputFile</b></td>
    <td>Optional <b>string</b> parameter.<br />
    <br />
    Specifies the output file in all cases. 
    In the case of singing a bundle, it specifies the signed bundle. 
    In the case of detaching the engine, it specifies the detached engine file.
    Lastly, when reattaching the engine, it specifies the new bundle with the reattached engine.
    </td>
  </tr>
  <tr>
    <td><b>NoLogo</b></td>
    <td>Optional <b>boolean</b> parameter.<br />
    <br />
    Skip printing insignia logo information.</td>
  </tr>
</table>
