//-------------------------------------------------------------------------------------------------
// <copyright file="OfficeAddinFabricator.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// An Office Addin fabricator extension for the Windows Installer XML Toolset ClickThrough application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Collections;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Windows.Forms;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    using Microsoft.Tools.WindowsInstallerXml.Extensions.OfficeAddin;
    using OA = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.OfficeAddin;

    /// <summary>
    /// An Office Addin fabricator extension for the Windows Installer XML Toolset ClickThrough application.
    /// </summary>
    public sealed class OfficeAddinFabricator : Fabricator
    {
        /// <summary>
        /// String version of SupportedOfficeApplications enum.
        /// </summary>
        static internal readonly string[] OfficeApplicationStrings =
        {
            "Excel",
            "Outlook",
            "PowerPoint",
            "Word"
        };

        // application values
        private Guid appId;
        private Version appVersion;
        private string description;
        private string details;
        private string entryPoint;
        private FileVersionInfo entryPointVersionInfo;
        private ArrayList extendedOfficeApplications;
        private string iconPath;
        private string manufacturer;
        private string name;
        private Guid packageId;
        private Uri previousFeedUrl;
        private string source;
        private Uri updateUrl;
        private int updateRate;

        private string saveWxsPath;

        /// <summary>
        /// Creates a new office addin builder
        /// </summary>
        public OfficeAddinFabricator()
        {
            this.appId = Guid.Empty;
            this.packageId = Guid.Empty;
            this.extendedOfficeApplications = new ArrayList();
        }

        /// <summary>
        /// Event fired any time a change is made to the fabricator's properties.
        /// </summary>
        public event PropertyChangedEventHandler Changed;

        /// <summary>
        /// Event fired any time the fabricator's is opened.
        /// </summary>
        public event EventHandler Opened;

        /// <summary>
        /// Enumeration of Office Applications supported by this fabricator.
        /// </summary>
        public enum OfficeApplications
        {
            /// <summary>Excel 2003</summary>
            Excel2003,

            /// <summary>Outlook 2003</summary>
            Outlook2003,

            /// <summary>PowerPoint 2003</summary>
            PowerPoint2003,

            /// <summary>Word 2003</summary>
            Word2003,

            /// <summary>Excel 2007</summary>
            Excel2007,

            /// <summary>Outlook 2007</summary>
            Outlook2007,

            /// <summary>PowerPoint 2007</summary>
            PowerPoint2007,

            /// <summary>Word 2007</summary>
            Word2007
        }

        /// <summary>
        /// Gets or sets the application id for the package.
        /// </summary>
        /// <value>Application identifier.</value>
        public Guid ApplicationId
        {
            get { return this.appId; }
            set { this.appId = value; }
        }

        /// <summary>
        /// Gets or sets the version of the package.
        /// </summary>
        /// <value>Application version.</value>
        public Version ApplicationVersion
        {
            get
            {
                if (this.appVersion == null && this.entryPointVersionInfo != null)
                {
                    if (this.entryPointVersionInfo.FileVersion != null)
                    {
                        return new Version(this.entryPointVersionInfo.ProductVersion);
                    }
                }

                return this.appVersion;
            }

            set
            {
                if (this.appVersion != value)
                {
                    this.appVersion = value;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("ApplicationVersion"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the description of the isolated application package.
        /// </summary>
        /// <value>Application description.</value>
        public string Description
        {
            get
            {
                if (this.description == null && this.entryPointVersionInfo != null)
                {
                    return this.entryPointVersionInfo.FileDescription;
                }

                return this.description;
            }

            set
            {
                if (value == String.Empty)
                {
                    value = null;
                }

                if (this.description != value)
                {
                    this.description = value;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("Description"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the entry point of the package.
        /// </summary>
        /// <value>Application entry point.</value>
        public string EntryPoint
        {
            get
            {
                return this.entryPoint;
            }

            set
            {
                if (value == String.Empty)
                {
                    value = null;
                }

                if (this.entryPoint != value)
                {
                    this.entryPoint = value;
                    if (this.entryPoint == null)
                    {
                        this.entryPointVersionInfo = null;
                    }
                    else
                    {
                        string fullPath = Path.Combine(this.source, this.entryPoint);
                        this.entryPointVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);
                    }

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("EntryPoint"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets the office applications supported by the addin.
        /// </summary>
        /// <value>Supported Office applications.</value>
        public OfficeApplications[] SupportedOfficeApplications
        {
            get
            {
                OfficeApplications[] supportedApps = new OfficeApplications[this.extendedOfficeApplications.Count];
                this.extendedOfficeApplications.CopyTo(supportedApps);
                return supportedApps;
            }
        }

        /// <summary>
        /// Gets or sets the name of the manufacturer.
        /// </summary>
        /// <value>Application manufacturer.</value>
        public string Manufacturer
        {
            get
            {
                if (this.manufacturer == null && this.entryPointVersionInfo != null)
                {
                    return this.entryPointVersionInfo.CompanyName;
                }

                return this.manufacturer;
            }

            set
            {
                if (value == String.Empty)
                {
                    value = null;
                }

                if (this.manufacturer != value)
                {
                    this.manufacturer = value;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("Manufacturer"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the name of the application.
        /// </summary>
        /// <value>Application name.</value>
        public string Name
        {
            get
            {
                if (this.name == null && this.entryPointVersionInfo != null)
                {
                    return this.entryPointVersionInfo.ProductName;
                }

                return this.name;
            }

            set
            {
                if (value == String.Empty)
                {
                    value = null;
                }

                if (this.name != value)
                {
                    this.name = value;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("Name"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the id for the package.
        /// </summary>
        /// <value>Package identifier.</value>
        public Guid PackageId
        {
            get { return this.packageId; }
            set { this.packageId = value; }
        }

        /// <summary>
        /// Gets and sets the previous feed for the package.
        /// </summary>
        /// <value>Previous feed URL.</value>
        public Uri PreviousFeedUrl
        {
            get
            {
                return this.previousFeedUrl;
            }

            set
            {
                if (this.previousFeedUrl != value)
                {
                    this.previousFeedUrl = value;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("PreviousFeedUrl"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets and sets the path to save the generated source file.
        /// </summary>
        /// <value>Path to save the generated source file.</value>
        public string SaveSourceFile
        {
            get { return this.saveWxsPath; }
            set { this.saveWxsPath = value; }
        }

        /// <summary>
        /// Gets the title of the extension.
        /// </summary>
        /// <value>Title of fabricator.</value>
        public override string Title
        {
            get { return "ClickThrough for Office Addins"; }
        }

        /// <summary>
        /// Gets the namespace of the extension.
        /// </summary>
        /// <value>Namespace of extension.</value>
        public override string Namespace
        {
            get { return "http://wix.sourceforge.net/schemas/clickthrough/officeaddin/2006"; }
        }

        /// <summary>
        /// Gets or sets the path to the root of the application.
        /// </summary>
        /// <remarks>Changing the Source value will reset the EntryPoint value as well.</remarks>
        /// <value>Source for fabricator.</value>
        public string Source
        {
            get
            {
                return this.source;
            }

            set
            {
                if (value == String.Empty)
                {
                    value = null;
                }

                if (this.source != value)
                {
                    this.EntryPoint = null; // clear out the entry point first

                    this.source = value;
                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("Source"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the RSS update feed for the package.
        /// </summary>
        /// <value>Update URL.</value>
        public Uri UpdateUrl
        {
            get
            {
                return this.updateUrl;
            }

            set
            {
                if (this.updateUrl != value)
                {
                    this.updateUrl = value;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("UpdateUrl"));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets the rate the feed should be checked by the client.
        /// </summary>
        /// <value>Update rate.</value>
        public int UpdateRate
        {
            get
            {
                return this.updateRate;
            }

            set
            {
                if (this.updateRate != value)
                {
                    this.updateRate = value;

                    if (this.Changed != null)
                    {
                        this.Changed(this, new PropertyChangedEventArgs("UpdateRate"));
                    }
                }
            }
        }

        /// <summary>
        /// Builds the setup package and feed.
        /// </summary>
        /// <param name="outputFeed">Path to the file where feed will be generated.</param>
        /// <returns>True if fabrication succeeded, false if anything went wrong.</returns>
        public override bool Fabricate(string outputFeed)
        {
            this.VerifyRequiredInformation();

            try
            {
                this.Core = new FabricatorCore(this.MessageHandler);

                // Calculate the paths required to build the MSI.
                string localSetupFeed = outputFeed;
                Uri urlSetupFeed = new Uri(this.updateUrl, Path.GetFileName(localSetupFeed));
                string shimPath = Path.GetTempFileName();
                string outputMsi = Path.GetTempFileName();
                Guid addinId = this.ApplicationId == Guid.Empty ? Guid.NewGuid() : this.ApplicationId;

                string previousPackagePath = null;

                // Create the feed builder and open the previous feed if it was provided.
                FeedBuilder feedBuilder = new FeedBuilder(this.Core);

                if (this.previousFeedUrl != null)
                {
                    feedBuilder.OpenPrevious(this.previousFeedUrl);
                    previousPackagePath = feedBuilder.GetPreviousAppItemPath();

                    if (String.Compare(Path.GetExtension(previousPackagePath), ".exe", true) == 0)
                    {
                        previousPackagePath = SetupExeBuilder.GetEmbeddedPackage(previousPackagePath);
                    }
                }

                // Create the shim builder.
                OfficeShimBuilder shimBuilder = new OfficeShimBuilder(this.Core);
                shimBuilder.AddinPath = Path.Combine(this.Source, this.EntryPoint);
                shimBuilder.AddinId = addinId.ToString("B");
                shimBuilder.Build(shimPath);

                // Create the MSI builder and open the previous package if there was one.
                OfficeAddinMsiBuilder msiBuilder = new OfficeAddinMsiBuilder(this.Core);

                if (previousPackagePath != null)
                {
                    msiBuilder.OpenPrevious(previousPackagePath);
                }

                // Build the MSI.
                msiBuilder.Description = this.description;
                msiBuilder.ExtendedOfficeApplications.AddRange(this.extendedOfficeApplications);
                msiBuilder.EntryFileRelativePath = this.entryPoint;
                msiBuilder.Manufacturer = this.Manufacturer;
                msiBuilder.Name = this.Name;
                msiBuilder.ProductCode = addinId;
                msiBuilder.ShimPath = shimPath;
                msiBuilder.ShimClsid = shimBuilder.AddinClsid;
                msiBuilder.ShimProgid = shimBuilder.AddinProgId;
                msiBuilder.Source = this.Source;
                msiBuilder.UpdateUrl = urlSetupFeed;
                msiBuilder.UpgradeCode = this.ApplicationId;
                msiBuilder.Version = this.ApplicationVersion;
                if (!msiBuilder.Build(outputMsi, this.saveWxsPath))
                {
                    return false;
                }

                // Calculate the paths required to build the setup.exe.
                string relativeSetupExe = Path.Combine(msiBuilder.Version.ToString(), "setup.exe");
                string localSetupExe = Path.Combine(Path.GetDirectoryName(outputFeed), relativeSetupExe);

                // Build the setup.exe.
                SetupExeBuilder setupExeBuilder = new SetupExeBuilder(this.Core);
                setupExeBuilder.MsiPath = outputMsi;
                if (!setupExeBuilder.Build(localSetupExe))
                {
                    return false;
                }

                // Calculate the paths required for the app feed.
                Uri urlSetupExe = new Uri(this.updateUrl, relativeSetupExe);

                // Build the application feed.  Note some values come from the MSI build because
                // they were updated there.
                feedBuilder.ApplicationId = msiBuilder.ProductCode;
                feedBuilder.ApplicationName = this.Name;
                feedBuilder.ApplicationVersion = msiBuilder.Version;
                feedBuilder.Description = this.Description;
                feedBuilder.Generator = "WiX Toolset's ClickThrough for Office Addins";
                feedBuilder.Id = msiBuilder.UpgradeCode;
                feedBuilder.PackagePath = localSetupExe;
                feedBuilder.PackageUrl = urlSetupExe;
                feedBuilder.TimeToLive = this.UpdateRate;
                feedBuilder.Title = String.Concat(this.Manufacturer, "'s ", this.Name);
                feedBuilder.Url = this.UpdateUrl;
                return feedBuilder.Build(localSetupFeed);
            }
            finally
            {
                this.Core = null;
            }
        }

        /// <summary>
        /// Loads the package builder data from disk.
        /// </summary>
        /// <param name="path">Path to load the builder information from.</param>
        public override void Open(string path)
        {
            Assembly[] assemblies = new Assembly[] { Assembly.GetExecutingAssembly() };
            Wix.CodeDomReader reader = new Wix.CodeDomReader(assemblies);

            OA.OfficeAddin officeAddin = reader.Load(path) as OA.OfficeAddin;
            if (null == officeAddin)
            {
                throw new ApplicationException("Failed to load office addin data file.");
            }

            foreach (Wix.ISchemaElement child in officeAddin.Children)
            {
                if (child is OA.Package)
                {
                    foreach (Wix.ISchemaElement grandchild in ((OA.Package)child).Children)
                    {
                        if (grandchild is OA.Description)
                        {
                            this.description = ((OA.Description)grandchild).Content;
                        }
                        else if (grandchild is OA.Feed)
                        {
                            if (((OA.Feed)grandchild).Content != null)
                            {
                                this.updateUrl = new Uri(((OA.Feed)grandchild).Content);
                            }
                        }
                        else if (grandchild is OA.UpdateRate)
                        {
                            if (((OA.UpdateRate)grandchild).Content != 0)
                            {
                                this.updateRate = Convert.ToInt32(((OA.UpdateRate)grandchild).Content);
                            }
                        }
                        else if (grandchild is OA.Icon)
                        {
                        }
                        else if (grandchild is OA.Id)
                        {
                            if (((OA.Id)grandchild).Content != null)
                            {
                                this.packageId = new Guid(((OA.Id)grandchild).Content);
                            }
                        }
                        else if (grandchild is OA.Manufacturer)
                        {
                            this.manufacturer = ((OA.Manufacturer)grandchild).Content;
                        }
                        else if (grandchild is OA.Version)
                        {
                            if (((OA.Version)grandchild).Content != null)
                            {
                                this.appVersion = new Version(((OA.Version)grandchild).Content);
                            }
                        }
                    }
                }
                else if (child is OA.Application)
                {
                    foreach (Wix.ISchemaElement grandchild in ((OA.Application)child).Children)
                    {
                        if (grandchild is OA.Details)
                        {
                            this.details = ((OA.Details)grandchild).Content;
                        }
                        else if (grandchild is OA.EntryPoint)
                        {
                            this.entryPoint = ((OA.EntryPoint)grandchild).Content;
                        }
                        else if (grandchild is OA.ExtendsApplication)
                        {
                            OfficeApplications extendedApp;
                            switch (((OA.ExtendsApplication)grandchild).Content)
                            {
                                case OA.SupportedOfficeApplications.Excel2003:
                                    extendedApp = OfficeApplications.Excel2003;
                                    break;
                                case OA.SupportedOfficeApplications.Outlook2003:
                                    extendedApp = OfficeApplications.Outlook2003;
                                    break;
                                case OA.SupportedOfficeApplications.PowerPoint2003:
                                    extendedApp = OfficeApplications.PowerPoint2003;
                                    break;
                                case OA.SupportedOfficeApplications.Word2003:
                                    extendedApp = OfficeApplications.Word2003;
                                    break;
                                default:
                                    throw new ArgumentException("Unexpected application in data file.", "ExtendsApplication");
                            }

                            if (!this.extendedOfficeApplications.Contains(extendedApp))
                            {
                                this.extendedOfficeApplications.Add(extendedApp);
                            }
                        }
                        else if (grandchild is OA.Icon)
                        {
                            this.iconPath = ((OA.Icon)grandchild).Content;
                        }
                        else if (grandchild is OA.Id)
                        {
                            this.appId = new Guid(((OA.Id)grandchild).Content);
                        }
                        else if (grandchild is OA.Name)
                        {
                            this.name = ((OA.Name)grandchild).Content;
                        }
                        else if (grandchild is OA.Source)
                        {
                            if (((OA.Source)grandchild).Content != null)
                            {
                                string expandedSource = System.Environment.ExpandEnvironmentVariables(((OA.Source)grandchild).Content);
                                this.source = Path.GetFullPath(expandedSource);
                            }
                        }
                    }
                }
                else if (child is OA.PreviousFeed)
                {
                    if (((OA.PreviousFeed)child).Content != null)
                    {
                        string expandedUrl = System.Environment.ExpandEnvironmentVariables(((OA.PreviousFeed)child).Content);
                        this.previousFeedUrl = new Uri(expandedUrl);
                    }
                }
            }

            if (this.entryPoint != null)
            {
                if (this.source != null)
                {
                    string fullPath = Path.Combine(this.source, this.entryPoint);
                    this.entryPointVersionInfo = FileVersionInfo.GetVersionInfo(fullPath);
                }
            }

            if (this.Opened != null)
            {
                this.Opened(this, new EventArgs());
            }
        }

        /// <summary>
        /// Adds a supported Office application for the Addin.
        /// </summary>
        /// <param name="officeApp">Office application to note support for.</param>
        public void AddExtendedOfficeApplication(OfficeApplications officeApp)
        {
            if (!this.extendedOfficeApplications.Contains(officeApp))
            {
                this.extendedOfficeApplications.Add(officeApp);

                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("SupportedOfficeApplications"));
                }
            }
        }

        /// <summary>
        /// Removes a supported Office application for the Addin.
        /// </summary>
        /// <param name="officeApp">Office application to remove support for.</param>
        public void RemoveExtendedOfficeApplication(OfficeApplications officeApp)
        {
            if (this.extendedOfficeApplications.Contains(officeApp))
            {
                this.extendedOfficeApplications.Remove(officeApp);

                if (this.Changed != null)
                {
                    this.Changed(this, new PropertyChangedEventArgs("SupportedOfficeApplications"));
                }
            }
        }

        /// <summary>
        /// Saves the package builder data to disk.
        /// </summary>
        /// <param name="filePath">Path to save the output builder to.</param>
        public override void Save(string filePath)
        {
            OA.OfficeAddin officeAddin = new OA.OfficeAddin();

            // Serialize out the package information.
            OA.Package package = new OA.Package();
            officeAddin.AddChild(package);

            if (this.description != null && this.description.Length > 0)
            {
                OA.Description description = new OA.Description();
                description.Content = this.description;
                package.AddChild(description);
            }

            if (this.updateUrl != null)
            {
                OA.Feed feed = new OA.Feed();
                feed.Content = this.updateUrl.ToString();
                package.AddChild(feed);
            }

            if (this.updateRate > 0)
            {
                OA.UpdateRate updateRate = new OA.UpdateRate();
                updateRate.Content = this.updateRate;
                package.AddChild(updateRate);
            }

            if (this.packageId != Guid.Empty)
            {
                OA.Id id = new OA.Id();
                id.Content = this.packageId.ToString();
                package.AddChild(id);
            }

            if (this.manufacturer != null && this.manufacturer.Length > 0)
            {
                OA.Manufacturer manufacturer = new OA.Manufacturer();
                manufacturer.Content = this.manufacturer;
                package.AddChild(manufacturer);
            }

            if (this.appVersion != null)
            {
                OA.Version version = new OA.Version();
                version.Content = this.appVersion.ToString();
                package.AddChild(version);
            }

            // Serialize out the application information.
            OA.Application application = new OA.Application();
            officeAddin.AddChild(application);

            if (this.details != null && this.details.Length > 0)
            {
                OA.Details details = new OA.Details();
                details.Content = this.details;
                application.AddChild(details);
            }

            if (this.entryPoint != null && this.entryPoint.Length > 0)
            {
                OA.EntryPoint entryPoint = new OA.EntryPoint();
                entryPoint.Content = this.entryPoint;
                application.AddChild(entryPoint);
            }

            foreach (OfficeApplications extendedApp in this.extendedOfficeApplications)
            {
                OA.ExtendsApplication extendsApplication = new OA.ExtendsApplication();
                switch (extendedApp)
                {
                    case OfficeApplications.Excel2003:
                        extendsApplication.Content = OA.SupportedOfficeApplications.Excel2003;
                        break;
                    case OfficeApplications.Outlook2003:
                        extendsApplication.Content = OA.SupportedOfficeApplications.Outlook2003;
                        break;
                    case OfficeApplications.PowerPoint2003:
                        extendsApplication.Content = OA.SupportedOfficeApplications.PowerPoint2003;
                        break;
                    case OfficeApplications.Word2003:
                        extendsApplication.Content = OA.SupportedOfficeApplications.Word2003;
                        break;
                }

                application.AddChild(extendsApplication);
            }

            if (this.iconPath != null && this.iconPath.Length > 0)
            {
                OA.Icon icon = new OA.Icon();
                icon.Content = this.iconPath;
                application.AddChild(icon);
            }

            if (this.appId != Guid.Empty)
            {
                OA.Id id = new OA.Id();
                id.Content = this.appId.ToString();
                application.AddChild(id);
            }

            if (this.name != null && this.name.Length > 0)
            {
                OA.Name name = new OA.Name();
                name.Content = this.name;
                application.AddChild(name);
            }

            if (this.source != null && this.source.Length > 0)
            {
                OA.Source source = new OA.Source();
                source.Content = this.source;
                application.AddChild(source);
            }

            // Serialize out the previous package path if there is one.
            if (this.previousFeedUrl != null)
            {
                OA.PreviousFeed previousFeed = new OA.PreviousFeed();
                previousFeed.Content = this.previousFeedUrl.AbsoluteUri;
                officeAddin.AddChild(previousFeed);
            }

            // Serialize the data to disk.
            using (StreamWriter sw = new StreamWriter(filePath))
            {
                XmlTextWriter writer = null;
                try
                {
                    writer = new XmlTextWriter(sw);
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;

                    officeAddin.OutputXml(writer);
                }
                finally
                {
                    if (writer != null)
                    {
                        writer.Close();
                    }
                }
            }
        }

        /// <summary>
        /// Gets the root directory for this application.
        /// </summary>
        /// <returns>WiX Directory element containing the root directory and all files.</returns>
        internal Wix.Directory GetApplicationRootDirectory()
        {
            if (null == this.source)
            {
                throw new ArgumentNullException("Source");
            }

            OfficeAddinMsiBuilder msiBuilder = new OfficeAddinMsiBuilder(this.Core);
            msiBuilder.Source = this.source;

            return msiBuilder.GetRootDirectory(true);
        }

        /// <summary>
        /// Ensures all of the required properties were populated before 
        /// trying to execute any operations on the fabricator.
        /// </summary>
        private void VerifyRequiredInformation()
        {
            if (null == this.UpdateUrl)
            {
                throw new InvalidOperationException("UpdateUrl must be specified before saving the package builder.");
            }
            else if (null == this.Manufacturer)
            {
                throw new InvalidOperationException("ManufacturerName must be specified before saving the package builder.");
            }
            else if (null == this.Name)
            {
                throw new InvalidOperationException("ApplicationName must be specified before saving the package builder.");
            }
            else if (null == this.EntryPoint)
            {
                throw new InvalidOperationException("EntryPoint must be specified before saving the package builder.");
            }
            else if (0 == this.extendedOfficeApplications.Count)
            {
                throw new InvalidOperationException("At least one Office application must be extended before fabrication can continue.");
            }
            else if (null == this.Source)
            {
                throw new InvalidOperationException("ApplicationRoot must be specified before saving the package builder.");
            }
        }
    }
}
