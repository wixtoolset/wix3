//-------------------------------------------------------------------------------------------------
// <copyright file="NewsItem.cs" company="Outercurve Foundation">
//   Copyright (c) 2004, Outercurve Foundation.
//   This software is released under Microsoft Reciprocal License (MS-RL).
//   The license and further copyright text can be found in the file
//   LICENSE.TXT at the root directory of the distribution.
// </copyright>
// 
// <summary>
// The model for an individual news item.
// </summary>
//-------------------------------------------------------------------------------------------------

namespace Microsoft.Tools.WindowsInstallerXml.UX
{
    using System;

    /// <summary>
    /// The model for an individual news item.
    /// </summary>
    public class NewsItem
    {
        public string Author { get; set; }
        public string Title { get; set; }
        public string Url { get; set; }
        public string Snippet { get; set; }
        public DateTime Updated { get; set; }
    }
}
