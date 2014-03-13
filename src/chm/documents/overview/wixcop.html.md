---
title: WixCop
layout: documentation
after: insignia
---
# WixCop

WixCop serves two main purposes:

* To upgrade WiX authoring to the current schema
* To format WiX authoring according to a set of common formatting

WixCop&apos;s command-line syntax is:

    WixCop.exe [options] sourceFile [sourceFile ...]

WixCop takes any number of WiX source files as command-line arguments. Wildcards are permitted. WixCop supports response files containing options and source files, using @responseFile syntax.

WixCop returns the following exit codes:

* 0, when no errors are reported.
* 1, when a fatal error occurs.
* 2, when WixCop violations occur.

The following table describes the switches that WixCop supports.

<table cellspacing="0" cellpadding="4" border="1">
  <thead>
    <tr>
      <td><strong>WixCop switch</strong></td>
      <td><strong>Description</strong></td>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>-?</td>
      <td>Show help.</td>
    </tr>
    <tr>
      <td>-nologo</td>
      <td>Don't show the WixCop banner.</td>
    </tr>
    <tr>
      <td>-f</td>
      <td>Fix errors encountered in source files. This switch takes effect only for source files that are writable.</td>
    </tr>
    <tr>
      <td>-s</td>
      <td>Look for source files in subdirectories.</td>
    </tr>
    <tr>
      <td>-indent:<em>n</em></td>
      <td>Overrides the default number of spaces per indentation level (4) to the number <em>n</em> you specify.</td>
    </tr>
    <tr>
      <td>-set1<em>filename</em></td>
      <td>Loads a primary settings file (see below). Note that there are no characters separating <em>-set1</em> and the settings file name.</td>
    </tr>
    <tr>
      <td>-set2<em>filename</em></td>
      <td>Loads an alternate settings file that overrides some or all of the settings in the primary settings file. Note that there are no characters separating <em>-set2</em> and the settings file name.</td>
    </tr>
  </tbody>
</table>

### WixCop settings files

WixCop supports two settings files. Generally, the primary settings file is your &ldquo;global&rdquo; settings and the alternate settings file lets you override the global settings for a particular project.

Settings files are XML with the following structure:

    <Settings>
      <IgnoreErrors>
        <Test Id="testId" />
      </IgnoreErrors> 
      <ErrorsAsWarnings>
        <Test Id="testId" />
      </ErrorsAsWarnings> 
      <ExemptFiles>
        <File Name="foo.wxs" />
       </ExemptFiles>
     </Settings>

The IgnoreErrors element lists test IDs that should be ignored. The ErrorsAsWarnings element lists test IDs that should be demoted from errors to warnings. The ExemptFiles element lists files that should be skipped. The following table describes the tests that WixCop supports.

<table cellspacing="0" cellpadding="4" border="1">
  <thead>
    <tr>
      <td><strong>WixCop test ID</strong></td>
      <td><strong>Description</strong></td>
    </tr>
  </thead>
  <tbody>
    <tr>
      <td>Unknown</td>
      <td>Internal only: returned when a string cannot be converted to an InspectorTestType.</td>
    </tr>
    <tr>
      <td>InspectorTestTypeUnknown</td>
      <td>Internal only: displayed when a string cannot be converted to an InspectorTestType.</td>
    </tr>
    <tr>
      <td>XmlException</td>
      <td>Displayed when an XML loading exception has occurred.</td>
    </tr>
    <tr>
      <td>UnauthorizedAccessException</td>
      <td>Displayed when a file cannot be accessed; typically when trying to save back a fixed file.</td>
    </tr>
    <tr>
      <td>DeclarationEncodingWrong</td>
      <td>Displayed when the encoding attribute in the XML declaration is not 'UTF-8'.</td>
    </tr>
    <tr>
      <td>DeclarationMissing</td>
      <td>Displayed when the XML declaration is missing from the source file.</td>
    </tr>
    <tr>
      <td>WhitespacePrecedingCDATAWrong</td>
      <td>Displayed when the whitespace preceding a CDATA node is wrong.</td>
    </tr>
    <tr>
      <td>WhitespacePrecedingNodeWrong</td>
      <td>Displayed when the whitespace preceding a node is wrong.</td>
    </tr>
    <tr>
      <td>NotEmptyElement</td>
      <td>Displayed when an element is not empty as it should be.</td>
    </tr>
    <tr>
      <td>WhitespaceFollowingCDATAWrong</td>
      <td>Displayed when the whitespace following a CDATA node is wrong.</td>
    <tr>
      <td>WhitespacePrecedingEndElementWrong</td>
      <td>Displayed when the whitespace preceding an end element is wrong.</td>
    </tr>
    <tr>
      <td>XmlnsMissing</td>
      <td>Displayed when the xmlns attribute is missing from the document element.</td>
    </tr>
    <tr>
      <td>XmlnsValueWrong</td>
      <td>Displayed when the xmlns attribute on the document element is wrong.</td>
    </tr>
    <tr>
      <td>CategoryAppDataEmpty</td>
      <td>Displayed when a Category element has an empty AppData attribute.</td>
    </tr>
    <tr>
      <td>COMRegistrationTyper</td>
      <td>Displayed when a Registry element encounters an error while being converted to a strongly-typed WiX COM element.</td>
    </tr>
    <tr>
      <td>UpgradeVersionRemoveFeaturesEmpty</td>
      <td>Displayed when an UpgradeVersion element has an empty RemoveFeatures attribute.</td>
    </tr>
    <tr>
      <td>FeatureFollowParentDeprecated</td>
      <td>Displayed when a Feature element contains the deprecated FollowParent attribute.</td>
    </tr>
    <tr>
      <td>RadioButtonMissingValue</td>
      <td>Displayed when a RadioButton element is missing the Value attribute.</td>
    </tr>
    <tr>
      <td>TypeLibDescriptionEmpty</td>
      <td>Displayed when a TypeLib element contains a Description element with an empty string value.</td>
    </tr>
    <tr>
      <td>ClassRelativePathMustBeAdvertised</td>
      <td>Displayed when a RelativePath attribute occurs on an unadvertised Class element.</td>
    </tr>
    <tr>
      <td>ClassDescriptionEmpty</td>
      <td>Displayed when a Class element has an empty Description attribute.</td>
    </tr>
    <tr>
      <td>ServiceInstallLocalGroupEmpty</td>
      <td>Displayed when a ServiceInstall element has an empty LocalGroup attribute.</td>
    </tr>
    <tr>
      <td>ServiceInstallPasswordEmpty</td>
      <td>Displayed when a ServiceInstall element has an empty Password attribute.</td>
    </tr>
    <tr>
      <td>ShortcutWorkingDirectoryEmpty</td>
      <td>Displayed when a Shortcut element has an empty WorkingDirectory attribute.</td>
    </tr>
    <tr>
      <td>IniFileValueEmpty</td>
       <td>Displayed when a IniFile element has an empty Value attribute.</td>
    </tr>
    <tr>
      <td>FileSearchNamesCombined</td>
      <td>Displayed when a FileSearch element has a Name attribute that contains both the short and long versions of the file name.</td>
    </tr>
    <tr>
      <td>WebApplicationExtensionIdDeprecated</td>
      <td>Displayed when a WebApplicationExtension element has a deprecated Id attribute.</td>
    </tr>
    <tr>
      <td>WebApplicationExtensionIdEmpty</td>
      <td>Displayed when a WebApplicationExtension element has an empty Id attribute.</td>
    </tr>
    <tr>
      <td>PropertyValueEmpty</td>
      <td>Displayed when a Property element has an empty Value attribute.</td>
    </tr>
    <tr>
      <td>ControlCheckBoxValueEmpty</td>
      <td>Displayed when a Control element has an empty CheckBoxValue attribute.</td>
    </tr>
    <tr>
      <td>RadioGroupDeprecated</td>
      <td>Displayed when a deprecated RadioGroup element is found.</td>
    </tr>
    <tr>
      <td>ProgressTextTemplateEmpty</td>
      <td>Displayed when a Progress element has an empty TextTemplate attribute.</td>
    </tr>
    <tr>
      <td>RegistrySearchTypeRegistryDeprecated</td>
      <td>Displayed when a RegistrySearch element has a Type attribute set to 'registry'.</td>
    </tr>
    <tr>
      <td>WebFilterLoadOrderIncorrect</td>
      <td>Displayed when a WebFilter/@LoadOrder attribute has a value that is not more stongly typed.</td>
    </tr>
    <tr>
      <td>SrcIsDeprecated</td>
      <td>Displayed when an element contains a deprecated src attribute.</td>
    </tr>
    <tr>
      <td>RequireComponentGuid</td>
      <td>Displayed when a Component element is missing the required Guid attribute.</td>
    </tr>
    <tr>
      <td>LongNameDeprecated</td>
      <td>Displayed when a an element has a LongName attribute.</td>
    </tr>
    <tr>
      <td>RemoveFileNameRequired</td>
      <td>Displayed when a RemoveFile element has no Name or LongName attribute.</td>
    </tr>
    <tr>
      <td>DeprecatedLocalizationVariablePrefix</td>
      <td>Displayed when a localization variable begins with the deprecated '$' character.</td>
    </tr>
    <tr>
      <td>NamespaceChanged</td>
      <td>Displayed when the namespace of an element has changed.</td>
    </tr>
    <tr>
      <td>UpgradeVersionPropertyAttributeRequired</td>
      <td>Displayed when an UpgradeVersion element is missing the required Property attribute.</td>
    </tr>
    <tr>
      <td>UpgradePropertyChild</td>
      <td>Displayed when an Upgrade element contains a deprecated Property child element.</td>
    </tr>
    <tr>
      <td>RegistryElementDeprecated</td>
       <td>Displayed when a deprecated Registry element is found.</td>
    </tr>
    <tr>
      <td>PatchSequenceSupersedeTypeChanged</td>
      <td>Displayed when a PatchSequence/@Supersede attribute contains a deprecated integer value.</td>
    </tr>
    <tr>
      <td>PatchSequenceTargetDeprecated</td>
      <td>Displayed when a deprecated PatchSequence/@Target attribute is found.</td>
    </tr>
    <tr>
      <td>VerbTargetDeprecated</td>
      <td>Displayed when a deprecated Verb/@Target attribute is found.</td>
    </tr>
    <tr>
      <td>ProgIdIconFormatted</td>
      <td>Displayed when a ProgId/@Icon attribute value contains a formatted string.</td>
    </tr>
    <tr>
      <td>IgnoreModularizationDeprecated</td>
      <td>Displayed when a deprecated IgnoreModularization element is found.</td>
    </tr>
    <tr>
      <td>PackageCompressedIllegal</td>
      <td>Displayed when a Package/@Compressed attribute is found under a Module element.</td>
    </tr>
    <tr>
      <td>PackagePlatformsDeprecated</td>
      <td>Displayed when a Package/@Platforms attribute is found.</td>
    </tr>
    <tr>
      <td>ModuleGuidDeprecated</td>
      <td>Displayed when a deprecated Module/@Guid attribute is found.</td>
    </tr>
    <tr>
      <td>GuidWildcardDeprecated</td>
      <td>Displayed when a deprecated guid wildcard value is found.</td>
    </tr>
    <tr>
      <td>FragmentRefIllegal</td>
      <td>Displayed when a FragmentRef Element is found.</td>
    </tr>
    <tr>
      <td>FileRedundantNames</td>
      <td>Displayed when a File/@Name matches a File/@ShortName.</td>
    </tr>
  </tbody>
</table>
