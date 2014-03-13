---
title: How To: Reference another DirectorySearch element
layout: documentation
after: ngen_managed_assemblies
---
# How To: Reference another DirectorySearch element
There may be times when you need to locate different files or subdirectories under the same directory, and assign each to a separate property. Since you cannot define the same DirectorySearch element more than once, you must use a DirectorySearchRef element. 
To reference another DirectorySearch element, you must specify the same Id, 
Parent Id, and Path attribute values or you will get unresolved symbol errors 
when linking with light.exe.

## Step 1: Define a DirectorySearch element
You first need to define the parent DirectorySearch element. This is expected to 
contain the different files or subdirectories you will assign to separate 
properties.

<pre><span style="COLOR: blue">&lt;</span><span style="COLOR: #a31515">Property </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">SHDOCVW</span>"<span style="COLOR: blue">&gt;
    &lt;</span><span style="COLOR: #a31515">DirectorySearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">WinDir</span>" <span style="COLOR: red">Path</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">[WindowsFolder]</span>"<span style="COLOR: blue">&gt;
        &lt;</span><span style="COLOR: #a31515">DirectorySearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Media</span>" <span style="COLOR: red">Path</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Media</span>"<span style="COLOR: blue">&gt;
            &lt;</span><span style="COLOR: #a31515">FileSearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Chimes</span>" <span style="COLOR: red">Name</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">chimes.wav</span>" <span style="COLOR: blue">/&gt;
        &lt;/</span><span style="COLOR: #a31515">DirectorySearch</span><span style="COLOR: blue">&gt;
    &lt;/</span><span style="COLOR: #a31515">DirectorySearch</span><span style="COLOR: blue">&gt;
&lt;/</span><span style="COLOR: #a31515">Property</span><span style="COLOR: blue">&gt;</span></pre>

This will search for the file &quot;chimes.wav&quot; under the Media directory in Windows. 
If the file is found, the full path will be assigned to the public property 
&quot;SHDOCVW&quot;.

## Step 2: Define a DirectorySearchRef element
To search for another file in the Media directory, you need to reference all the 
same Id, Parent Id, and Path attributes. Because the Media DirectorySearch 
element is nested under the WinDir DirectorySearch element, its Parent attribute is automatically assigned the parent DirectorySearch element&apos;s Id attribute value; thus, that is what you must specify for the DirectorySearchRef element&apos;s Parent attribute value.

<pre><span style="COLOR: blue">&lt;</span><span style="COLOR: #a31515">Property </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">USER32</span>"<span style="COLOR: blue">&gt;
    &lt;</span><span style="COLOR: #a31515">DirectorySearchRef </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Media</span>" <span style="COLOR: red">Parent</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">WinDir</span>" <span style="COLOR: red">Path</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Media</span>"<span style="COLOR: blue">&gt;
        &lt;</span><span style="COLOR: #a31515">FileSearch </span><span style="COLOR: red">Id</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">Chord</span>" <span style="COLOR: red">Name</span><span style="COLOR: blue">=</span>"<span style="COLOR: blue">chord.wav</span>" <span style="COLOR: blue">/&gt;
    &lt;/</span><span style="COLOR: #a31515">DirectorySearchRef</span><span style="COLOR: blue">&gt;
&lt;/</span><span style="COLOR: #a31515">Property</span><span style="COLOR: blue">&gt;</span></pre>

If you wanted to refer to another DirectorySearch element that used the Id Media
but was under a different parent path, you would have to define a new
DirectorySearch element under a different parent than in step 1.
