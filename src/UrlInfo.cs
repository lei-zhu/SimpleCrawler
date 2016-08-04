// --------------------------------------------------------------------------------------------------------------------
// <copyright file="UrlInfo.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The url info.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
using System.Collections.Generic;
namespace SimpleCrawler
{
    
    /// <summary>
    /// The url info.
    /// </summary>
    public class UrlInfo
    {
        #region Fields

        /// <summary>
        /// The url.
        /// </summary>
        private readonly string url;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="UrlInfo"/> class.
        /// </summary>
        /// <param name="urlString">
        /// The url string.
        /// </param>
        public UrlInfo(string urlString,IList<string> followLinkRules)
        {
            this.url = urlString;
            this.FollowLinkRules = followLinkRules;
            
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public int Depth { get; set; }

        /// <summary>
        /// Gets the url string.
        /// </summary>
        public string UrlString
        {
            get
            {
                return this.url;
            }
        }

        /// <summary>
        /// Gets or sets the status.
        /// </summary>
        public CrawlStatus Status { get; set; }
        public IList<string> FollowLinkRules { get; set; }
        #endregion
    }
}