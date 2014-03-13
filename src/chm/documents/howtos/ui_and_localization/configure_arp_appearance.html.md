---
title: How To: Set Your Installer's Icon in Add/Remove Programs
layout: documentation
---
# How To: Set Your Installer's Icon in Add/Remove Programs

Windows Installer supports a standard property, [ARPPRODUCTICON][1], that controls the icon displayed in Add/Remove Programs for your application. To set this property you first need to include the icon in your installer using the [Icon](~/xsd/wix/icon.html) element, then set the property using the [Property](~/xsd/wix/property.html) element.

    <Icon Id="icon.ico" SourceFile="MySourceFiles\icon.ico"/>
    <Property Id="ARPPRODUCTICON" Value="icon.ico" />

These two elements can be placed anywhere in your WiX project under the Product element. The Icon element specifies the location of the icon on your source machine, and gives it a unique id for use later in the WiX project. The Property element sets the ARPPRODUCTION property to the id of the icon to use.

[1]: http://msdn.microsoft.com/library/aa367593.aspx
