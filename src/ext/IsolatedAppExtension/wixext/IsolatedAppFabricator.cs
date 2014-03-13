//-------------------------------------------------------------------------------------------------
// <copyright file="IsolatedAppFabricator.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// An IsolatedApp fabricator extension for the Windows Installer XML Toolset ClickThrough application.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.IO;
    using System.Reflection;
    using System.Xml;
    using System.Windows.Forms;

    using Wix = Microsoft.Tools.WindowsInstallerXml.Serialize;

    using Microsoft.Tools.WindowsInstallerXml.Extensions.IsolatedApp;
    using IA = Microsoft.Tools.WindowsInstallerXml.Extensions.Serialize.IsolatedApp;

    /// <summary>
    /// An IsolatedApp fabricator extension for the Windows Installer XML Toolset ClickThrough application.
    /// </summary>
    public sealed class IsolatedAppFabricator : Fabricator
    {
        // application values
        private Guid appId;
        private Version appVersion;
        private string description;
        private string details;
        private string entryPoint;
        private FileVersionInfo entryPointVersionInfo;
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
        /// Creates a new isolated application builder
        /// </summary>
        public IsolatedAppFabricator()
        {
            this.appId = Guid.Empty;
            this.packageId = Guid.Empty;
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
        /// <value>Application title.</value>
        public override string Title
        {
            get { return "ClickThrough for Isolated Applications"; }
        }

        /// <summary>
        /// Gets the namespace of the extension.
        /// </summary>
        /// <value>Extension namespace.</value>
        public override string Namespace
        {
            get { return "http://wix.sourceforge.net/schemas/clickthrough/isolatedapp/2006"; }
        }

        /// <summary>
        /// Gets or sets the path to the root of the application.
        /// </summary>
        /// <remarks>Changing the Source value will reset the EntryPoint value as well.</remarks>
        /// <value>Source.</value>
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
        /// <returns>True if fabrication was successful, false if any failure occurs.</returns>
        public override bool Fabricate(string outputFeed)
        {
            this.VerifyRequiredInformation();

            try
            {
                this.Core = new FabricatorCore(this.MessageHandler);

                // Calculate the paths required to build the MSI.
                string localSetupFeed = outputFeed;
                Uri urlSetupFeed = new Uri(this.UpdateUrl, Path.GetFileName(localSetupFeed));
                string outputMsi = Path.GetTempFileName();

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

                // Create the MSI builder and open the previous package if there was one.
                IsolatedMsiBuilder msiBuilder = new IsolatedMsiBuilder(this.Core);

                if (previousPackagePath != null)
                {
                    msiBuilder.OpenPrevious(previousPackagePath);
                }

                // Build the MSI.
                msiBuilder.Description = this.Description;
                msiBuilder.EntryFileRelativePath = this.EntryPoint;
                msiBuilder.Manufacturer = this.Manufacturer;
                msiBuilder.Name = this.Name;
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
                feedBuilder.Generator = "WiX Toolset's ClickThrough for Isolated Applications";
                feedBuilder.Id = msiBuilder.UpgradeCode;
                feedBuilder.PackagePath = localSetupExe;
                feedBuilder.PackageUrl = urlSetupExe;
                feedBuilder.TimeToLive = this.updateRate;
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
        /// Loads the fabricator data from disk.
        /// </summary>
        /// <param name="path">Path to load the facbricator information from.</param>
        public override void Open(string path)
        {
            Assembly[] assemblies = new Assembly[] { Assembly.GetExecutingAssembly() };
            Wix.CodeDomReader reader = new Wix.CodeDomReader(assemblies);

            IA.IsolatedApp isolatedApp = reader.Load(path) as IA.IsolatedApp;
            if (null == isolatedApp)
            {
                throw new ApplicationException("Failed to load isolated app data file.");
            }

            this.description = null;
            this.packageId = Guid.Empty;
            this.manufacturer = null;
            this.appId = Guid.Empty;
            this.appVersion = null;
            this.updateUrl = null;
            this.details = null;
            this.name = null;
            this.source = null;
            this.entryPoint = null;
            this.iconPath = null;
            this.previousFeedUrl = null;

            foreach (Wix.ISchemaElement child in isolatedApp.Children)
            {
                if (child is IA.Package)
                {
                    foreach (Wix.ISchemaElement grandchild in ((IA.Package)child).Children)
                    {
                        if (grandchild is IA.Description)
                        {
                            this.description = ((IA.Description)grandchild).Content;
                        }
                        else if (grandchild is IA.Feed)
                        {
                            if (((IA.Feed)grandchild).Content != null)
                            {
                                this.updateUrl = new Uri(((IA.Feed)grandchild).Content);
                            }
                        }
                        else if (grandchild is IA.UpdateRate)
                        {
                            if (((IA.UpdateRate)grandchild).Content != 0)
                            {
                                this.updateRate = Convert.ToInt32(((IA.UpdateRate)grandchild).Content);
                            }
                        }
                        else if (grandchild is IA.Icon)
                        {
                        }
                        else if (grandchild is IA.Id)
                        {
                            if (((IA.Id)grandchild).Content != null)
                            {
                                this.packageId = new Guid(((IA.Id)grandchild).Content);
                            }
                        }
                        else if (grandchild is IA.Manufacturer)
                        {
                            this.manufacturer = ((IA.Manufacturer)grandchild).Content;
                        }
                        else if (grandchild is IA.Version)
                        {
                            if (((IA.Version)grandchild).Content != null)
                            {
                                this.appVersion = new Version(((IA.Version)grandchild).Content);
                            }
                        }
                    }
                }
                else if (child is IA.Application)
                {
                    foreach (Wix.ISchemaElement grandchild in ((IA.Application)child).Children)
                    {
                        if (grandchild is IA.Details)
                        {
                            this.details = ((IA.Details)grandchild).Content;
                        }
                        else if (grandchild is IA.EntryPoint)
                        {
                            this.entryPoint = ((IA.EntryPoint)grandchild).Content;
                        }
                        else if (grandchild is IA.Icon)
                        {
                            this.iconPath = ((IA.Icon)grandchild).Content;
                        }
                        else if (grandchild is IA.Id)
                        {
                            this.appId = new Guid(((IA.Id)grandchild).Content);
                        }
                        else if (grandchild is IA.Name)
                        {
                            this.name = ((IA.Name)grandchild).Content;
                        }
                        else if (grandchild is IA.Source)
                        {
                            if (((IA.Source)grandchild).Content != null)
                            {
                                string expandedSource = System.Environment.ExpandEnvironmentVariables(((IA.Source)grandchild).Content);
                                this.source = Path.GetFullPath(expandedSource);
                            }
                        }
                    }
                }
                else if (child is IA.PreviousFeed)
                {
                    if (((IA.PreviousFeed)child).Content != null)
                    {
                        string expandedUrl = System.Environment.ExpandEnvironmentVariables(((IA.PreviousFeed)child).Content);
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
        /// Saves the package builder data to disk.
        /// </summary>
        /// <param name="path">Path to save the fabricator information.</param>
        public override void Save(string path)
        {
            IA.IsolatedApp isolatedApp = new IA.IsolatedApp();

            // Serialize out the package information.
            IA.Package package = new IA.Package();
            isolatedApp.AddChild(package);

            if (this.description != null && this.description.Length > 0)
            {
                IA.Description description = new IA.Description();
                description.Content = this.description;
                package.AddChild(description);
            }

            if (this.updateUrl != null)
            {
                IA.Feed feed = new IA.Feed();
                feed.Content = this.updateUrl.ToString();
                package.AddChild(feed);
            }

            if (this.updateRate > 0)
            {
                IA.UpdateRate updateRate = new IA.UpdateRate();
                updateRate.Content = this.updateRate;
                package.AddChild(updateRate);
            }

            if (this.packageId != Guid.Empty)
            {
                IA.Id id = new IA.Id();
                id.Content = this.packageId.ToString();
                package.AddChild(id);
            }

            if (this.manufacturer != null && this.manufacturer.Length > 0)
            {
                IA.Manufacturer manufacturer = new IA.Manufacturer();
                manufacturer.Content = this.manufacturer;
                package.AddChild(manufacturer);
            }

            if (this.appVersion != null)
            {
                IA.Version version = new IA.Version();
                version.Content = this.appVersion.ToString();
                package.AddChild(version);
            }

            // Serialize out the application information.
            IA.Application application = new IA.Application();
            isolatedApp.AddChild(application);

            if (this.details != null && this.details.Length > 0)
            {
                IA.Details details = new IA.Details();
                details.Content = this.details;
                application.AddChild(details);
            }

            if (this.entryPoint != null && this.entryPoint.Length > 0)
            {
                IA.EntryPoint entryPoint = new IA.EntryPoint();
                entryPoint.Content = this.entryPoint;
                application.AddChild(entryPoint);
            }

            if (this.iconPath != null && this.iconPath.Length > 0)
            {
                IA.Icon icon = new IA.Icon();
                icon.Content = this.iconPath;
                application.AddChild(icon);
            }

            if (this.appId != Guid.Empty)
            {
                IA.Id id = new IA.Id();
                id.Content = this.appId.ToString();
                application.AddChild(id);
            }

            if (this.name != null && this.name.Length > 0)
            {
                IA.Name name = new IA.Name();
                name.Content = this.name;
                application.AddChild(name);
            }

            if (this.source != null && this.source.Length > 0)
            {
                IA.Source source = new IA.Source();
                source.Content = this.source;
                application.AddChild(source);
            }

            // Serialize out the previous package path if there is one.
            if (this.previousFeedUrl != null)
            {
                IA.PreviousFeed previousFeed = new IA.PreviousFeed();
                previousFeed.Content = this.previousFeedUrl.AbsoluteUri;
                isolatedApp.AddChild(previousFeed);
            }

            // Serialize the data to disk.
            using (StreamWriter sw = new StreamWriter(path))
            {
                XmlTextWriter writer = null;
                try
                {
                    writer = new XmlTextWriter(sw);
                    writer.Formatting = Formatting.Indented;
                    writer.Indentation = 4;

                    isolatedApp.OutputXml(writer);
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

            IsolatedMsiBuilder msiBuilder = new IsolatedMsiBuilder(this.Core);
            msiBuilder.Source = this.source;

            return msiBuilder.GetRootDirectory(true);
        }

        /// <summary>
        /// Ensures all of the required properties were populated before 
        /// trying to execute any operations on the package builder.
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
            else if (null == this.Source)
            {
                throw new InvalidOperationException("ApplicationRoot must be specified before saving the package builder.");
            }
        }
    }
}
