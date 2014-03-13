---
title: List of Tools
layout: documentation
after: files
---
# List of Tools

To view the usage information of the tools, run /? on the tool via the command line.

<table border="1">
  <tr>
    <td><b>Name</b></td>
    <td><b>Description</b></td>
  </tr>
  <tr>
    <td>
      <p>Candle</p>
    </td>
    <td>
      <p>Preprocesses and compiles WiX source files into object files (.wixobj). For more 
      information on compiling, see <a href="candle.html">Compiler</a>. For more information on 
      preprocessing, see <a href="preprocessor.html">Preprocessor</a>.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Light</p>
    </td>
    <td>
      <p>Links and binds one or more .wixobj files and creates a Windows Installer 
      database (.msi or .msm). When necessary, Light will also create cabinets and embed 
      streams into the Windows Installer database it creates.&nbsp;For more information 
      on linking, see <a href="light.html">Linker</a>.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Lit</p>
    </td>
    <td>
      <p>Combines multiple .wixobj files into libraries that can be consumed by Light. 
      For more information, see <a href="lit.html">Librarian</a>.</p>
    </td>
  </tr>
  <tr>
   <td>
      <p>Dark</p>
    </td>
    <td>
      <p>Converts a Windows Installer database into a set of WiX source files. This tool 
      is very useful for getting all your authoring into a WiX source file when you 
      have an existing Windows Installer database. However, you will then need to 
      tweak this file to accomodate different languages and breaking things into 
      fragments.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Heat</p>
    </td>
    <td>
      <p>Generates WiX authoring from various input formats. It is used for harvesting 
      files, Visual Studio projects and Internet Information Server web sites, 
      &quot;harvesting&quot; these files into components and generating Windows Installer XML 
      Source files (.wxs). Heat is good to use when you begin authoring your first 
      Windows Installer package for a product.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Insignia</p>
    </td>
    <td>
      <p>Inscribes an installer database with information about the digital certificates its external cabs are signed with. 
      For more information, see <a href="insignia.html">Insignia</a>.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Melt</p>
    </td>
    <td>
      <p>Converts an .msm into a component group in a WiX source file.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Torch</p>
    </td>
    <td>
      <p>Performs a diff to generate a transform (.wixmst or .mst) for XML outputs (.wixout or .wixpdb) or .msi files.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Smoke</p>
    </td>
    <td>
      <p>Runs validation checks on .msi or .msm files.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Pyro</p>
    </td>
    <td>
      <p>Takes an XML output patch file (.wixmsp) and one or more XML transform files (.wixmst) and produces an .msp file.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>WixCop</p>
    </td>
    <td>
      <p>Enforces standards on WiX source files. WixCop can also be used to assist in converting a set of WiX source files created using an older version of WiX to the latest version of WiX.
      For more information, see <a href="wixcop.html">WixCop</a>.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>WixUnit</p>
    </td>
    <td>
      <p>Runs validations on a set of XML files and the expected output file. Takes a set 
      of WiX source files and an expected MSI as the input and outputs Pass/Fail.</p>
    </td>
  </tr>
  <tr>
    <td>
      <p>Lux and Nit</p>
    </td>
    <td>
      <p>Author and run declarative unit tests for custom actions. For more information,
      see <a href="lux.html">Unit-testing custom actions with Lux</a>.</p>
    </td>
  </tr>
</table>

# Response files

All WiX command-line tools support **response files**, which are text files that contain command-line switches and arguments. Anything you can put on a WiX tool command line can instead go into a response file. Response files are useful when you have command lines that are too long for your command shell. For example, you might want to generate a response file that contains command-line switches and the files that you want to compile with candle.exe:

    -nologo -wx
    1.wxs
    2.wxs 
    3.wxs

  and issue a command like:

    candle @listOfFiles.txt

Specify a response file with the @ character, followed immediately by the pathname of the response file, with no whitespace in-between. Response files can appear at the beginning, in the middle, or at the end of command line arguments.
