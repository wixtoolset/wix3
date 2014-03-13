---
title: Create the Skeleton Bundle Authoring
layout: documentation
---
# Create the Skeleton Bundle Authoring

The root element of a bundle is [&lt;Bundle&gt;](~/xsd/wix/bundle.html). The [&lt;Bundle&gt;](~/xsd/wix/bundle.html) element is a child directly under the [&lt;Wix&gt;](~/xsd/wix/wix.html) element. Here&apos;s an example of a blank bundle:

    <?xml version="1.0"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
      <Bundle>
        <!-- Contents of the bundle goes here -->
      </Bundle>
    </Wix>

In this example, we will be adding the following elements to the [&lt;Bundle&gt;](~/xsd/wix/bundle.html) element:

* [&lt;BootstrapperApplicationRef&gt;](~/xsd/wix/bootstrapperapplicationref.html)
* [&lt;Chain&gt;](~/xsd/wix/chain.html)
* [&lt;Variable&gt;](~/xsd/wix/variable.html)

As a start, let&apos;s add the two most common elements inside a [&lt;Bundle&gt;](~/xsd/wix/bundle.html) :

    <?xml version="1.0"?>
    <Wix xmlns="http://schemas.microsoft.com/wix/2006/wi">
      <Bundle>
        <BootstrapperApplicationRef />
        <Chain>
        </Chain>
      </Bundle>
    </Wix>

The [&lt;BootstrapperApplicationRef&gt;](~/xsd/wix/bootstrapperapplicationref.html) element will eventually point to the standard BootstrapperApplication provided by the WiX toolset and the [&lt;Chain&gt;](~/xsd/wix/chain.html) element will eventually contain the ordered list of packages that make up the bundle.

Now you have the skeleton authoring of a Bundle, let&apos;s move on to adding information for the [&lt;BootstrapperApplicationRef&gt;](~/xsd/wix/bootstrapperapplicationref.html) element. See [Author the BootstrapperApplication for a Bundle](authoring_bundle_application.html).
