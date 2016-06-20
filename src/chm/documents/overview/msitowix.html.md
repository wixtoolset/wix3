---
title: MSI Tables to WiX Schema
layout: documentation
after: lux
---

# MSI Tables to WiX Schema

In the WiX schema, its not always entirely obvious how the tables from the Windows Installer schema map to the WiX schema. Below are some helpful hints on how to figure out the relationships between the two schemas.

## DuplicateFile Table

This is authored using a [CopyFile](~/xsd/wix/copyfile.html) node nested under a File node. You only need to set the Id, DestinationFolder, and DestinationName attributes.

## LaunchCondition Table

This is authored using a [Condition](~/xsd/wix/condition.html) node authored under Fragment or Product. You only need to set the Message attribute.

##LockPermissions Table

This is authored using [Permission](~/xsd/wix/permission.html).

## MoveFile Table

This is authored using a [CopyFile](~/xsd/wix/copyfile.html) node nested under a Component node. You will need to set all attributes except Delete. Set Delete to &apos;yes&apos; in order to use the msidbMoveFileOptionsMove option.

## PublishComponent Table

The PublishComponent functionality is available in WiX by using a [Category](~/xsd/wix/category.html). Here is a small sample of what a PublishComponent record would look like in MSI, then in WiX notation.

<dl>
  <dt>MSI</dt>
  <dd>
    <table>
      <tr>
        <th>ComponentId</th>
        <th>Qualifier</th>
        <th>Component_</th>
        <th>AppData</th>
        <th>Feature_</th>
      </tr>
      <tr>
        <td>{11111111-2222-3333-4444-5555555555555}</td>
        <td>1033</td>
        <td>MyComponent</td>
        <td>Random Data</td>
        <td>MyFeature</td>
      </tr>
    </table>
  </dd>
</dl>

<dl>
  <dt>WiX</dt>
  <dd>
    <table class="command">
      <tr>
        <td>
          <pre>
&lt;Component Id='MyComponent' Guid='87654321-4321-4321-4321-110987654321'&gt;
     <b>&lt;Category Id='11111111-2222-3333-4444-5555555555555' AppData='Random Data' 
               Qualifier='1033'/&gt;</b>
&lt;/Component&gt;
.
.
.
&lt;Feature Id='MyFeature' Level='1'&gt;
     &lt;ComponentRef Id='MyComponent'/&gt;
&lt;/Feature&gt;
</pre>
        </td>
      </tr>
    </table>
  </dd>
</dl>

## RemoveIniFile

This is authored using [IniFile](~/xsd/wix/inifile.html). Just set the Action attribute to &apos;removeLine&apos; or &apos;removeTag&apos; as appropriate.

## RemoveRegistry Table

This is authored using [Registry](~/xsd/wix/registry.html). Simply set the Action attribute to &apos;remove&apos; or &apos;removeKey&apos; (as appropriate) in order to get an entry in the RemoveRegistry table.
