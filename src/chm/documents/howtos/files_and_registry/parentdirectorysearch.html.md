---
title: How To: Get the parent directory of a file search
layout: documentation
after: directorysearchref
---
# How To: Get the parent directory of a file search
You can set a property to the parent directory of a file.

## Step 1: Define the search root
In the following example, the path to [WindowsFolder]Microsoft.NET is defined as the root of the search. If you do not define a search root, Windows Installer will search all fixed drives up to the depth specified.

<pre><span style="COLOR: blue">&lt;</span><span style="COLOR: #a31515">Property </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">NGEN2DIR</span>"<span style="COLOR: blue">&gt;
    &lt;</span><span style="COLOR: #a31515">DirectorySearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Windows</span>" <span style="COLOR: red">Path</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">[WindowsFolder]</span>"<span style="COLOR: blue">&gt;
        &lt;</span><span style="COLOR: #a31515">DirectorySearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">MS.NET</span>" <span style="COLOR: red">Path</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Microsoft.NET</span>"<span style="COLOR: blue">&gt;
        &lt;/</span><span style="COLOR: #a31515">DirectorySearch</span><span style="COLOR: blue">&gt;
    &lt;/</span><span style="COLOR: #a31515">DirectorySearch</span><span style="COLOR: blue">&gt;
&lt;/</span><span style="COLOR: #a31515">Property</span><span style="COLOR: blue">&gt;</span></pre>

## Step 2: Define the parent directory to find
Under the search root, define the directory you want returned and set the DirectorySearch/@AssignToProperty attribute to &apos;yes&apos;. You must then define the file you want to find using a unique FileSearch/@Id attribute value.

<pre><span style="COLOR: blue">&lt;</span><span style="COLOR: #a31515">Property </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">NGEN2DIR</span>"<span style="COLOR: blue">&gt;
    &lt;</span><span style="COLOR: #a31515">DirectorySearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Windows</span>" <span style="COLOR: red">Path</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">[WindowsFolder]</span>"<span style="COLOR: blue">&gt;
        &lt;</span><span style="COLOR: #a31515">DirectorySearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">MS.NET</span>" <span style="COLOR: red">Path</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Microsoft.NET</span>"<span style="COLOR: blue">&gt;
            &lt;</span><span style="COLOR: #a31515">DirectorySearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Ngen2Dir</span>" <span style="COLOR: red">Depth</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">2</span>" <span style="COLOR: red">AssignToProperty</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">yes</span>"<span style="COLOR: blue">&gt;
                &lt;</span><span style="COLOR: #a31515">FileSearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Ngen_exe</span>" <span style="COLOR: red">Name</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">ngen.exe</span>" <span style="COLOR: red">MinVersion</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">2.0.0.0</span>" <span style="COLOR: blue">/&gt;
            &lt;/</span><span style="COLOR: #a31515">DirectorySearch</span><span style="COLOR: blue">&gt;
        &lt;/</span><span style="COLOR: #a31515">DirectorySearch</span><span style="COLOR: blue">&gt;
    &lt;/</span><span style="COLOR: #a31515">DirectorySearch</span><span style="COLOR: blue">&gt;
&lt;/</span><span style="COLOR: #a31515">Property</span><span style="COLOR: blue">&gt;</span></pre>

In this example, if ngen.exe is newer than version 2.0.0.0 and is found no more than two directories under [WindowsFolder]Microsoft.NET its parent directory is returned in the NGEN2DIR property.
