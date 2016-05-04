// Copyright (c) .NET Foundation and contributors. All rights reserved. Licensed under the Microsoft Reciprocal License. See LICENSE.TXT file in the project root for full license information.

namespace Microsoft.Tools.WindowsInstallerXml.Extensions
{
    using System;
    using System.Globalization;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Security.Cryptography;
    using System.Xml;

    /// <summary>
    /// Builder class to create feeds
    /// </summary>
    public sealed class FeedBuilder
    {
        private FabricatorCore core;

        private string appDetails;
        private Guid appId;
        private string appName;
        private Version appVersion;
        private string description;
        private Guid feedId;
        private string generator;
        private string packagePath;
        private Uri packageUrl;
        private string title;
        private int timeToLive;
        private Uri url;

        private XmlDocument previousFeed;
        private ApplicationFeedItem previousAppItem;

        /// <summary>
        /// Creates a new FeedBuilder object.
        /// </summary>
        /// <param name="core">Core build object for message handling.</param>
        public FeedBuilder(FabricatorCore core)
        {
            this.core = core;
            this.feedId = Guid.Empty;
            this.appId = Guid.Empty;
        }

        /// <summary>
        /// Gets and sets the details of the application in the feed.
        /// </summary>
        /// <value>Applicaiton details.</value>
        public string ApplicationDetails
        {
            get { return this.appDetails; }
            set { this.appDetails = value; }
        }

        /// <summary>
        /// Gets and sets the id used for the application in the feed.
        /// </summary>
        /// <value>Application identifier.</value>
        public Guid ApplicationId
        {
            get { return this.appId; }
            set { this.appId = value; }
        }

        /// <summary>
        /// Gets and sets the name of the application in the feed.
        /// </summary>
        /// <value>Application name.</value>
        public string ApplicationName
        {
            get { return this.appName; }
            set { this.appName = value; }
        }

        /// <summary>
        /// Gets and sets the version of the application in the feed.
        /// </summary>
        /// <value>Application version.</value>
        public Version ApplicationVersion
        {
            get { return this.appVersion; }
            set { this.appVersion = value; }
        }

        /// <summary>
        /// Gets and sets the description of the feed.
        /// </summary>
        /// <value>Feed description.</value>
        public string Description
        {
            get { return this.description; }
            set { this.description = value; }
        }

        /// <summary>
        /// Gets and sets the generator field in the application feed.
        /// </summary>
        /// <value>Feed generator.</value>
        public string Generator
        {
            get { return this.generator; }
            set { this.generator = value; }
        }

        /// <summary>
        /// Gets and sets the id used for the entire feed.
        /// </summary>
        /// <value>Feed identifier.</value>
        public Guid Id
        {
            get { return this.feedId; }
            set { this.feedId = value; }
        }

        /// <summary>
        /// Gets and sets the path to the package for this feed.
        /// </summary>
        /// <value>Path to enclosure.</value>
        public string PackagePath
        {
            get { return this.packagePath; }
            set { this.packagePath = value; }
        }

        /// <summary>
        /// Gets and sets the update URL of the package in this feed.
        /// </summary>
        /// <value>Enclosure url.</value>
        public Uri PackageUrl
        {
            get { return this.packageUrl; }
            set { this.packageUrl = value; }
        }

        /// <summary>
        /// Gets and sets the time to live of the feed.
        /// </summary>
        /// <value>Feed time to live.</value>
        public int TimeToLive
        {
            get { return this.timeToLive; }
            set { this.timeToLive = value; }
        }

        /// <summary>
        /// Gets and sets the title of the feed.
        /// </summary>
        /// <value>Feed title.</value>
        public string Title
        {
            get { return this.title; }
            set { this.title = value; }
        }

        /// <summary>
        /// Gets and sets the URL of the feed.
        /// </summary>
        /// <value>Feed url.</value>
        public Uri Url
        {
            get { return this.url; }
            set { this.url = value; }
        }

        /// <summary>
        /// Creates application feed.
        /// </summary>
        /// <param name="outputFile">Path to build feed file to.</param>
        /// <returns>True if build succeeded or false if something went wrong during build.</returns>
        public bool Build(string outputFile)
        {
            XmlDocument feed = this.CreateFeed();
            if (null == feed)
            {
                return false;
            }

            // If there is a previous feed, copy all of the items and add them to the end
            // of the list of items.
            if (this.previousFeed != null)
            {
                XmlNodeList previousItems = this.previousFeed.SelectNodes("rss/channel/item");
                XmlNode newChannel = feed.SelectSingleNode("rss/channel");
                foreach (XmlNode previousItem in previousItems)
                {
                    XmlNode importedItem = feed.ImportNode(previousItem, true);
                    newChannel.AppendChild(importedItem);
                }
            }

            feed.Save(outputFile);
            return true;
        }

        /// <summary>
        /// Gets the path to the previous application feed item.
        /// </summary>
        /// <returns>Path to the previous application feed item or null if there is no previous application.</returns>
        /// <remarks>Calling this method may cause the prevous application to be downloaded.</remarks>
        public string GetPreviousAppItemPath()
        {
            if (this.previousAppItem == null)
            {
                return null;
            }

            if (this.previousAppItem.RequiresDownload)
            {
                FileInfo downloadedFile = new FileInfo(this.previousAppItem.LocalPath);
                if (!downloadedFile.Exists || downloadedFile.Length == 0)
                {
                    this.DownloadUrl(this.previousAppItem.Url, this.previousAppItem.LocalPath);
                }
            }

            return this.previousAppItem.LocalPath;
        }

        /// <summary>
        /// Opens a previous application feed and populates default information from it.
        /// </summary>
        /// <param name="feedUrl">Url to feed.</param>
        public void OpenPrevious(Uri feedUrl)
        {
            string localFeedPath = null;
            bool download = false;
            if (feedUrl.Scheme == Uri.UriSchemeHttp || feedUrl.Scheme == Uri.UriSchemeHttps)
            {
                localFeedPath = Path.GetTempFileName();
                download = true;
            }
            else if (feedUrl.Scheme == Uri.UriSchemeFile)
            {
                localFeedPath = feedUrl.LocalPath;
            }
            else
            {
                throw new ArgumentException("Only http:, https:, and file: protocols are supported.", "feedUrl");
            }

            if (download)
            {
                this.DownloadUrl(feedUrl, localFeedPath);
            }

            this.previousFeed = new XmlDocument();
            this.previousAppItem = null;

            XmlNamespaceManager namespaces = new XmlNamespaceManager(this.previousFeed.NameTable);
            namespaces.AddNamespace("as", "http://appsyndication.org/schemas/appsyn");

            this.previousFeed.Load(localFeedPath);

            // Get the application id, if there isn't one this isn't a valid application feed.
            XmlNode node = this.previousFeed.SelectSingleNode("rss/channel/as:application", namespaces);
            if (node == null)
            {
                throw new ApplicationException("Did not open a valid Application Feed");
            }

            this.feedId = new Guid(node.InnerText);

            // Get the title and description from the previous feed, if present.
            node = this.previousFeed.SelectSingleNode("rss/channel/title");
            if (node != null)
            {
                this.title = node.InnerText;
            }

            node = this.previousFeed.SelectSingleNode("rss/channel/description");
            if (node != null)
            {
                this.description = node.InnerText;
            }

            // Find the previous app.
            XmlNodeList items = this.previousFeed.SelectNodes("rss/channel/item");
            foreach (XmlNode item in items)
            {
                // Get the application's version from the feed.  If there is
                // no version, then this isn't an application feed.
                node = item.SelectSingleNode("as:version", namespaces);
                if (node == null)
                {
                    continue;
                }

                // If this item's application version isn't higher, skip proccessing it.
                Version appVersion = new Version(node.InnerText);
                if (this.previousAppItem != null && appVersion < this.previousAppItem.Version)
                {
                    continue;
                }

                // Get the URL to the application.  If there is no URL, then this
                // isn't a valid application feed.
                node = item.SelectSingleNode("enclosure/@url");
                if (node == null)
                {
                    continue;
                }

                this.previousAppItem = new ApplicationFeedItem();
                this.previousAppItem.Version = appVersion;
                try
                {
                    this.previousAppItem.Url = new Uri(node.Value);
                }
                catch (UriFormatException)
                {
                    this.previousAppItem.Url = new Uri(feedUrl, node.Value);
                }

                // Get the optional application id, if there is one.
                node = item.SelectSingleNode("as:application", namespaces);
                if (node != null)
                {
                    this.previousAppItem.Id = node.InnerText;
                }
            }
        }

        /// <summary>
        /// Downloads a file to the target path.
        /// </summary>
        /// <param name="sourceUrl">Url to download.</param>
        /// <param name="targetPath">Path to download URL to.</param>
        private void DownloadUrl(Uri sourceUrl, string targetPath)
        {
            HttpWebRequest request = WebRequest.CreateDefault(sourceUrl) as HttpWebRequest;
            if (request == null)
            {
                throw new ArgumentException("Only web URLs can be downloaded.", "sourceName");
            }

            // Set some reasonable limits on resources used by this request
            request.MaximumAutomaticRedirections = 50;
            request.MaximumResponseHeadersLength = 4;
            request.Credentials = CredentialCache.DefaultCredentials; // set credentials to use for this request.

            HttpWebResponse response = null;
            try
            {
                response = request.GetResponse() as HttpWebResponse;
                if (response == null)
                {
                    throw new ApplicationException("Failed to get response from server.");
                }

                // Get the stream associated with the response.
                using (Stream receiveStream = response.GetResponseStream())
                {
                    using (FileStream output = new FileStream(targetPath, FileMode.Create, FileAccess.Write))
                    {
                        int read = 0;
                        int totalRead = 0;
                        byte[] buffer = new byte[1024 * 64];

                        do
                        {
                            read = receiveStream.Read(buffer, 0, buffer.Length);
                            output.Write(buffer, 0, read);
                            totalRead += read;
                        }
                        while (read > 0);
                    }
                }
            }
            finally
            {
                if (response != null)
                {
                    response.Close();
                }
            }
        }

        /// <summary>
        /// Creates a new feed with the new app feed item.
        /// </summary>
        /// <returns>XmlDocument holding the new feed.</returns>
        private XmlDocument CreateFeed()
        {
            DateTime moment = DateTime.UtcNow;
            string createdTime = String.Format("{0:ddd, d MMM yyyy hh:mm:ss} GMT", moment);
            string feedId = this.feedId.ToString();
            string itemId = this.appId.ToString();
            string itemVersion = this.appVersion.ToString();
            string relativePackageUrl = this.url.MakeRelative(this.packageUrl);

            XmlDocument feed = new XmlDocument();

            using (StringWriter sw = new StringWriter())
            {
                XmlTextWriter writer = null;
                try
                {
                    FileInfo packageFileInfo = new FileInfo(this.packagePath);

                    writer = new XmlTextWriter(sw);
                    writer.WriteStartElement("rss"); // <rss>
                    writer.WriteAttributeString("version", "2.0");
                    writer.WriteAttributeString("xmlns", "as", null, "http://appsyndication.org/schemas/appsyn");
                    writer.WriteStartElement("channel"); // <channel>
                    if (null != this.title)
                    {
                        writer.WriteElementString("title", this.title);
                    }
                    if (null != this.generator)
                    {
                        writer.WriteElementString("generator", this.generator);
                    }
                    writer.WriteElementString("lastBuildDate", createdTime);
                    if (0 < this.timeToLive)
                    {
                        writer.WriteElementString("ttl", this.timeToLive.ToString(CultureInfo.InvariantCulture));
                    }
                    writer.WriteStartElement("application", "http://appsyndication.org/schemas/appsyn"); // <as:application>
                    writer.WriteAttributeString("type", "application/vnd.ms-msi");
                    writer.WriteString(feedId);
                    writer.WriteEndElement(); // </as:application>

                    writer.WriteStartElement("item"); // <item>
                    writer.WriteStartElement("guid"); // <guid>
                    writer.WriteAttributeString("isPermaLink", "false");
                    writer.WriteString(String.Format("urn:msi:{0}/{1}", feedId, itemVersion));
                    writer.WriteEndElement(); // </guid>
                    writer.WriteElementString("pubDate", createdTime);
                    writer.WriteElementString("title", String.Concat(this.appName, " v", itemVersion));
                    if (null != this.description)
                    {
                        writer.WriteElementString("description", this.description);
                    }

                    writer.WriteStartElement("application", "http://appsyndication.org/schemas/appsyn"); // <as:application>
                    writer.WriteAttributeString("type", "application/vnd.ms-msi");
                    writer.WriteString(itemId);
                    writer.WriteEndElement(); // </as:application>
                    writer.WriteElementString("version", "http://appsyndication.org/schemas/appsyn", itemVersion);

                    writer.WriteStartElement("enclosure"); // <enclosure>
                    writer.WriteAttributeString("url", relativePackageUrl);
                    writer.WriteAttributeString("length", packageFileInfo.Length.ToString());
                    writer.WriteAttributeString("type", "application/octet-stream");
                    writer.WriteEndElement(); // </enclosure>

                    writer.WriteStartElement("digest", "http://appsyndication.org/schemas/appsyn"); // <as:digest>
                    writer.WriteAttributeString("algorithm", "sha256");
                    writer.WriteString(this.CalculateSHA256Digest(this.packagePath));
                    writer.WriteEndElement(); // </as:digest>

                    writer.WriteEndElement(); // </item>

                    writer.WriteEndElement(); // </channel>
                    writer.WriteEndElement(); // </rss>
                }
                finally
                {
                    if (writer != null)
                    {
                        writer.Close();
                    }
                }

                feed.LoadXml(sw.ToString());
            }

            return feed;
        }

        /// <summary>
        /// Calculates the sha256 digest hash of a provided file.
        /// </summary>
        /// <param name="filePath">Path to file to calculate digest for.</param>
        /// <returns>Digest converted into hexidecimal string.</returns>
        private string CalculateSHA256Digest(string filePath)
        {
            // Generate the hash.
            SHA256 sha256 = new SHA256Managed();
            byte[] hash;

            using (FileStream fs = new FileStream(filePath, FileMode.Open, FileAccess.Read))
            {
                hash = sha256.ComputeHash(fs);
            }

            // Convert hash into string digest.
            StringBuilder digest = new StringBuilder(36);
            for (int i = 0; i < hash.Length; ++i)
            {
                digest.Append(hash[i].ToString("X2", CultureInfo.InvariantCulture.NumberFormat));
            }

            return digest.ToString();
        }

        /// <summary>
        /// Private class that stores information about an application feed item.
        /// </summary>
        private class ApplicationFeedItem
        {
            private string id;
            private string localPath;
            private bool requiresDownload;
            private Uri url;
            private Version version;

            /// <summary>
            /// Gets and sets the id for the feed item.
            /// </summary>
            public string Id
            {
                get { return this.id; }
                set { this.id = value; }
            }

            /// <summary>
            /// Gets the path to the feed item's enclosure.
            /// </summary>
            /// <remarks>The feed item may first need to be downloaded.</remarks>
            public string LocalPath
            {
                get { return this.localPath; }
            }

            /// <summary>
            /// Gets whether this feed item's enclosure needs to be 
            /// download from the server before processing.
            /// </summary>
            public bool RequiresDownload
            {
                get { return this.requiresDownload; }
            }

            /// <summary>
            /// Gets and sets the URL for the feed item.
            /// </summary>
            public Uri Url
            {
                get
                {
                    return this.url;
                }
                set
                {
                    if (this.url != value)
                    {
                        this.url = value;
                        if (this.url.Scheme == Uri.UriSchemeHttp || this.url.Scheme == Uri.UriSchemeHttps)
                        {
                            string extension = Path.GetExtension(this.url.AbsolutePath);
                            if (extension != String.Empty)
                            {
                                string tempFile = Path.GetTempFileName();
                                this.localPath = Path.ChangeExtension(tempFile, extension);
                                File.Move(tempFile, this.localPath);
                            }
                            else
                            {
                                this.localPath = Path.GetTempFileName();
                            }
                            this.requiresDownload = true;
                        }
                        else if (this.url.Scheme == Uri.UriSchemeFile)
                        {
                            this.localPath = this.url.LocalPath;
                            this.requiresDownload = false;
                        }
                        else
                        {
                            throw new ArgumentException("Only http:, https:, and file: protocols are supported.", "Url");
                        }
                    }
                }
            }

            /// <summary>
            /// Gets and sets the version of the feed item.
            /// </summary>
            public Version Version
            {
                get { return this.version; }
                set { this.version = value; }
            }
        }
    }
}
