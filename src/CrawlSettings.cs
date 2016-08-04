// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrawlSettings.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The crawl settings.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// The crawl settings.
    /// </summary>
    [Serializable]
    public class CrawlSettings
    {
        #region Fields

        /// <summary>
        /// The depth.
        /// </summary>
        private byte depth = 3;

        /// <summary>
        /// The lock host.
        /// </summary>
        private bool lockHost = true;

        /// <summary>
        /// The thread count.
        /// </summary>
        private byte threadCount = 1;

        /// <summary>
        /// The timeout.
        /// </summary>
        private int timeout = 15000;

        /// <summary>
        /// The user agent.
        /// </summary>
        private string userAgent = 
            "Mozilla/5.0 (Windows NT 6.1; WOW64) AppleWebKit/537.11 (KHTML, like Gecko) Chrome/23.0.1271.97 Safari/537.11";

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CrawlSettings"/> class.
        /// </summary>
        public CrawlSettings()
        {
            this.AutoSpeedLimit = false;
            this.EscapeLinks = new List<string>();
            this.KeepCookie = true;
            this.HrefKeywords = new List<string>();
            this.LockHost = true;
            this.RegularFilterExpressions = new List<string>();
            this.SeedsAddress = new Dictionary<string, IList<string>>();
        }

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets or sets a value indicating whether auto speed limit.
        /// </summary>
        public bool AutoSpeedLimit { get; set; }

        /// <summary>
        /// Gets or sets the depth.
        /// </summary>
        public byte Depth
        {
            get
            {
                return this.depth;
            }

            set
            {
                this.depth = value;
            }
        }

        /// <summary>
        /// Gets the escape links.
        /// </summary>
        public List<string> EscapeLinks { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether keep cookie.
        /// </summary>
        public bool KeepCookie { get; set; }

        /// <summary>
        /// Gets the href keywords.
        /// </summary>
        public List<string> HrefKeywords { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether lock host.
        /// </summary>
        public bool LockHost
        {
            get
            {
                return this.lockHost;
            }

            set
            {
                this.lockHost = value;
            }
        }

        /// <summary>
        /// Gets the regular filter expressions.
        /// </summary>
        public List<string> RegularFilterExpressions { get; private set; }

        /// <summary>
        /// Gets  the seeds address.
        /// </summary>
        public Dictionary<string,IList<string>> SeedsAddress { get; private set; }

        /// <summary>
        /// Gets or sets the thread count.
        /// </summary>
        public byte ThreadCount
        {
            get
            {
                return this.threadCount;
            }

            set
            {
                this.threadCount = value;
            }
        }

        /// <summary>
        /// Gets or sets the timeout.
        /// </summary>
        public int Timeout
        {
            get
            {
                return this.timeout;
            }

            set
            {
                this.timeout = value;
            }
        }

        /// <summary>
        /// Gets or sets the user agent.
        /// </summary>
        public string UserAgent
        {
            get
            {
                return this.userAgent;
            }

            set
            {
                this.userAgent = value;
            }
        }

        #endregion
    }
}