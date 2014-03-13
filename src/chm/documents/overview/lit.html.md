---
title: Library Tool (lit)
layout: documentation
after: light
---

# Library Tool (lit)

Lit is the WiX library creation tool. It can be used to combine multiple .wixobj files into libraries that can be consumed by [light](light.html).

## Usage Information

    lit.exe [-?] [-nologo] [-out libraryFile] objectFile [objectFile ...] [@responseFile]

Lit supports the following command line parameters:

<table border="1" cellspacing="0" cellpadding="2" class="style1">
  <tr>
    <td valign="top">
      <p><b>Switch</b></p>
    </td>
    <td valign="top">
      <p><b>Meaning</b></p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-b</p>
    </td>
    <td>
      <p>Specify a base path to locate all files; the default value is the current working directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-bf</p>
    </td>
    <td>
      <p>Bind files into the library file.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ext &lt;extension&gt;</p>
    </td>
    <td>
      <p>Specify an extension assembly.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-loc &lt;loc.wxl&gt;</p>
    </td>
    <td>
      <p>Provide a .wxl file to read localization strings from.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-nologo</p>
    </td>
    <td>
      <p>Skip printing Lit logo information.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-out</p>
    </td>
    <td>
      <p>Specify an output file; by default, Lit will write to the current working directory.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-pedantic</p>
    </td>
    <td>
      <p>Show pedantic messages.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-ss</p>
    </td>
    <td>
      <p>Suppress schema validation for documents; this switch provides a performance boost during linking.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sv</p>
    </td>
    <td>
      <p>Suppress intermediate file version mismatch checking. </p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-sw&lt;N&gt;</p>
    </td>
    <td>
      <p>Suppress warnings with specific message IDs. For example, -sw1011 -sw1012.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-swall</p>
    </td>
    <td>
      <p>Suppress all warnings (deprecated). </p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-v</p>
    </td>
    <td>
      <p>Generate verbose output</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-wx&lt;N&gt;</p>
    </td>
    <td>
      <p>Treat warnings as errors. For example, -wx1011 -wx1012.</p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-wxall</p>
    </td>
     <td>
      <p>Treat all warnings as errors (deprecated). </p>
    </td>
  </tr>
  <tr>
    <td valign="top">
      <p>-?</p>
    </td>
    <td>
      <p>Display Lit help information</p>
    </td>
  </tr>
</table>
