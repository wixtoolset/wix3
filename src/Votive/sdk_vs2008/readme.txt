This directory is a copy of parts of the Visual Studio 2008 SDK.  This SDK can be downloaded from http://www.microsoft.com/downloads/details.aspx?familyid=30402623-93ca-479a-867c-04dc45164f5b.

From: C:\Program Files\Visual Studio 2008 SDK\VisualStudioIntegration\Common\Source\CSharp\Project
  To: .\SDK_VS2008\Common\Source\CSharp\Project

From: C:\Program Files\MSBuild\Microsoft\VisualStudio\v9.0\VSSDK
  To: .\SDK_VS2008\Tools\Build

The following changes have been made to the SDK source code:

SDK\Common\Source\CSharp\Project\ProjectNode.cs
-----------------------------------------------
* Change the following method from private to public:

	public MSBuild.BuildProperty GetMsBuildProperty(string propertyName, bool resetCache)

SDK\Common\Source\CSharp\Project\ProjectBase.files
--------------------------------------------------
* Remove $(ProjectBasePath) from the <DependentUpon> elements for each of the following Compile and EmbeddedResource elements:

    <Compile Include="$(ProjectBasePath)\DontShowAgainDialog.Designer.cs">
      <DependentUpon>DontShowAgainDialog.cs</DependentUpon>
      <Link>ProjectBase\DontShowAgainDialog.Designer.cs</Link>
      <Visible>true</Visible>
    </Compile>
    <Compile Include="$(ProjectBasePath)\SecurityWarningDialog.Designer.cs">
      <DependentUpon>SecurityWarningDialog.cs</DependentUpon>
      <Link>ProjectBase\SecurityWarningDialog.Designer.cs</Link>
      <Visible>true</Visible>
    </Compile>

    <EmbeddedResource Include="$(ProjectBasePath)\SecurityWarningDialog.resx">
      <Link>ProjectBase\SecurityWarningDialog.resx</Link>
      <Visible>true</Visible>
      <SubType>Designer</SubType>
      <DependentUpon>$(ProjectBasePath)\SecurityWarningDialog.cs</DependentUpon>
    </EmbeddedResource>
    <EmbeddedResource Include="$(ProjectBasePath)\DontShowAgainDialog.resx">
      <Link>ProjectBase\DontShowAgainDialog.resx</Link>
      <Visible>true</Visible>
      <SubType>Designer</SubType>
      <DependentUpon>$(ProjectBasePath)\DontShowAgainDialog.cs</DependentUpon>
    </EmbeddedResource>

* Add HintPath values to each referenced assembly to facilitate builds on systems that do not have VS 2008 installed.

* Remove reference to System.Core.dll

SDK\Common\Source\CSharp\Project\ProjectConfig.cs
SDK\Common\Source\CSharp\Project\ProjectNode.cs
---------------------------------------
* Added a new MSBuild.Project parameter to GetConfigurationProperty, GetMsBuildProperty and 
  SetConfigurationProperty to allow passing a different bulid project. This is used to work with 
  properties in the .user file. Added the PerUserFileExtension constant string ".user".

SDK\Common\Source\CSharp\Project\NodeProperties.cs
--------------------------------------------------
* Change the CopyToLocal property of the ReferenceNodeProperties class to virtual.
* Add automation-only URL property to FileNodeProperties class.

SDK\Common\Source\CSharp\Project\HierarchyNode.cs
-----------------------------------------------------------------------------
* Changed the ExcludeNodeFromScc property of HierarchyNode class to virtual.
* Modified public virtual void Remove(bool removeFromStorage)
  Added condtion 'if (this.parentNode != null)' around parentNode.onChildRemoved(parentNode, args)
  and OnInvalidateItems(this.parentNode) method calls. This is required when partially added (no parent)
  node needs to be removed (when user tries to add an invalid reference).
* Changed GetGuidProperty(int, out Guid) method to return S_OK even for empty type guids.

SDK\Common\Source\CSharp\Project\ProjectNode.cs
-----------------------------------------------------------------------------
* Added a protected method to ProjectNode class.
  Method Signature: protected virtual VSADDRESULT IncludeExistingNonMemberNode(HierarchyNode node)
* Changed the AddItemWithSpecific method of ProjectNode class to add a call to IncludeExistingNonMemberNode method in case of a non-member item.

SDK\Common\Source\CSharp\Project\ReferenceNode.cs
-----------------------------------------------------------------------------
* Added ShowReferenceErrorMessage() message, refactored from derived class, ProjectReferenceNode.cs, ShowReferenceErrorMessage().
  Method Signature: protected void ShowReferenceErrorMessage(string message)
* Modified CanAddReference() by setting errorHandler to ShowReferenceAlreadyExistMessage when IsAlreadyAdded() is true.

SDK\Common\Source\CSharp\Project\ProjectReferenceNode.cs
-----------------------------------------------------------------------------
* Refactored string display code from ShowCircularReferenceErrorMessage() into ShowReferenceErrorMessage().
* Used ShowReferenceErrorMessage() in ShowCircularReferenceErrorMessage() to display the message.
* Modified ReferencedProjectOutputPath property to fall-back to checking config-independent OutputPath property.

SDK\Common\Source\CSharp\Project\ReferenceContainerNode.cs
-----------------------------------------------------------------------------
* Modified public ReferenceNode AddReferenceFromSelectorData(VSCOMPONENTSELECTORDATA selectorData)
  Added 'node.Remove(true);' when the reference node was not added completely. Without this call, the node 
  doesn't get added to the solution hierarchy but gets added to the project file.

SDK\Common\Source\CSharp\Project\ProjectNode.cs
-----------------------------------------------------------------------------
* Made OnAfterProjectOpened protected virtual

SDK\Common\Source\CSharp\Project\FileNode.cs
SDK\Common\Source\CSharp\Project\FolderNode.cs
SDK\Common\Source\CSharp\Project\HierarchyNode.cs
SDK\Common\Source\CSharp\Project\NodeProperties.cs
SDK\Common\Source\CSharp\Project\ProjectFileConstants.cs
SDK\Common\Source\CSharp\Project\ProjectNode.cs
SDK\Common\Source\CSharp\Project\Automation\OANavigableProjectItems.cs
SDK\Common\Source\CSharp\Project\Automation\OAProjectItems.cs
SDK\Common\Source\CSharp\Project\Microsoft.VisualStudio.Package.Project.cs
SDK\Common\Source\CSharp\Project\Microsoft.VisualStudio.Package.Project.resx
-----------------------------------------------------------------------------
* Added support for linked files in the project. See various small code changes in above files related to:
   - "Link" metadata item on project build items
   - VSADDITEMOP_LINKTOFILE
   - OVERLAYICON_SHORTCUT
   - VSADDITEM_ProjectHandlesLinks

SDK\Common\Source\CSharp\Project\ProjectNode.cs
-----------------------------------------------------------------------------
* Added a GetMsBuildProperty method which allows specifying a configuration instead of using the currently selected one.
* Implemented IVsDeferredSaveProject

SDK\Common\Source\CSharp\Project\ConfigProvider.cs
SDK\Common\Source\CSharp\Project\OutputGroup.cs
SDK\Common\Source\CSharp\Project\ProjectConfig.cs
SDK\Common\Source\CSharp\Project\ProjectNode.cs
SDK\Common\Source\CSharp\Project\Utilities.cs
-----------------------------------------------------------------------------
* Added support for configuration platforms and made project configuration management extensible in subclasses.

SDK\Common\Source\CSharp\Project\ProjectReferenceNode.cs
-----------------------------------------------------------------------------
* Added FindProject() method to handle references to projects in solution folders.

SDK\Common\Source\CSharp\Project\ConfigProvider.cs
SDK\Common\Source\CSharp\Project\ProjectConfig.cs
-----------------------------------------------------------------------------
* Refactored IVsDebuggableProjectCfg into DebuggableProjectConfig subclass.

SDK\Common\Source\CSharp\Project\ProjectNode.cs
SDK\Common\Source\CSharp\Project\ProjectFileConstants.cs
-----------------------------------------------------------------------------
* Modified ProcessFiles() so it skips items that have <Visible>false</Visible> as metadata
* Added the ProjectFileAttributeValue.Visible constant

SDK\Common\Source\CSharp\Project\FileNode.cs
-----------------------------------------------------------------------------
* Added a check to RenameFileNode to prevent writing empty <Link> tags to the project file

SDK\Common\Source\CSharp\Project\ProjectNode.cs
-----------------------------------------------------------------------------
* Fixed IVsDeferredSaveProject implementation

SDK\Common\Source\CSharp\Project\ProjectNode.cs
-----------------------------------------------------------------------------
* Fixed relative Wix reference for deferred-save projects
