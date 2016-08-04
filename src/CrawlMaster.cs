// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CrawlMaster.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The crawl master.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.IO.Compression;
    using System.Linq;
    using System.Net;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading;
    
    /// <summary>
    /// The crawl master.
    /// </summary>
    public class CrawlMaster
    {
        #region Constants

        /// <summary>
        /// The web url regular expressions.
        /// </summary>
        private const string WebUrlRegularExpressions = @"^(http|https)://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?";

        #endregion

        #region Fields

        /// <summary>
        /// The cookie container.
        /// </summary>
        private readonly CookieContainer cookieContainer;

        /// <summary>
        /// The random.
        /// </summary>
        private readonly Random random;

        /// <summary>
        /// The thread status.
        /// </summary>
        private readonly bool[] threadStatus;

        /// <summary>
        /// The threads.
        /// </summary>
        private readonly Thread[] threads;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="CrawlMaster"/> class.
        /// </summary>
        /// <param name="settings">
        /// The settings.
        /// </param>
        public CrawlMaster(CrawlSettings settings)
        {
            this.cookieContainer = new CookieContainer();
            this.random = new Random();

            this.Settings = settings;
            this.threads = new Thread[settings.ThreadCount];
            this.threadStatus = new bool[settings.ThreadCount];
        }

        #endregion

        #region Public Events

        /// <summary>
        /// The add url event.
        /// </summary>
        public event AddUrlEventHandler AddUrlEvent;

        /// <summary>
        /// The crawl error event.
        /// </summary>
        public event CrawlErrorEventHandler CrawlErrorEvent;

        /// <summary>
        /// The data received event.
        /// </summary>
        public event DataReceivedEventHandler DataReceivedEvent;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the settings.
        /// </summary>
        public CrawlSettings Settings { get; private set; }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// The crawl.
        /// </summary>
        public void Crawl()
        {
            this.Initialize();

            for (int i = 0; i < this.threads.Length; i++)
            {
                this.threads[i].Start(i);
                this.threadStatus[i] = false;
            }
        }

        /// <summary>
        /// The stop.
        /// </summary>
        public void Stop()
        {
            foreach (Thread thread in this.threads)
            {
                thread.Abort();
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// The config request.
        /// </summary>
        /// <param name="request">
        /// The request.
        /// </param>
        private void ConfigRequest(HttpWebRequest request)
        {
            request.UserAgent = this.Settings.UserAgent;
            request.CookieContainer = this.cookieContainer;
            request.AllowAutoRedirect = true;
            request.MediaType = "text/html";
            request.Headers["Accept-Language"] = "zh-CN,zh;q=0.8";

            if (this.Settings.Timeout > 0)
            {
                request.Timeout = this.Settings.Timeout;
            }
        }

        /// <summary>
        /// The crawl process.
        /// </summary>
        /// <param name="threadIndex">
        /// The thread index.
        /// </param>
        private void CrawlProcess(object threadIndex)
        {
            var currentThreadIndex = (int)threadIndex;
            while (true)
            {
                // 根据队列中的 Url 数量和空闲线程的数量，判断线程是睡眠还是退出
                if (UrlQueue.Instance.Count == 0)
                {
                    this.threadStatus[currentThreadIndex] = true;
                    if (!this.threadStatus.Any(t => t == false))
                    {
                        break;
                    }

                    Thread.Sleep(2000);
                    continue;
                }

                this.threadStatus[currentThreadIndex] = false;

                if (UrlQueue.Instance.Count == 0)
                {
                    continue;
                }

                UrlInfo urlInfo = UrlQueue.Instance.DeQueue();

                HttpWebRequest request = null;
                HttpWebResponse response = null;

                try
                {
                    if (urlInfo == null)
                    {
                        continue;
                    }

                    // 1~5 秒随机间隔的自动限速
                    if (this.Settings.AutoSpeedLimit)
                    {
                        int span = this.random.Next(1000, 5000);
                        Thread.Sleep(span);
                    }

                    // 创建并配置Web请求
                    request = WebRequest.Create(urlInfo.UrlString) as HttpWebRequest;
                    this.ConfigRequest(request);

                    if (request != null)
                    {
                        response = request.GetResponse() as HttpWebResponse;
                    }

                    if (response != null)
                    {
                        this.PersistenceCookie(response);

                        Stream stream = null;

                        // 如果页面压缩，则解压数据流
                        if (response.ContentEncoding == "gzip")
                        {
                            Stream responseStream = response.GetResponseStream();
                            if (responseStream != null)
                            {
                                stream = new GZipStream(responseStream, CompressionMode.Decompress);
                            }
                        }
                        else
                        {
                            stream = response.GetResponseStream();
                        }

                        using (stream)
                        {
                            string html = this.ParseContent(stream, response.CharacterSet);

                            this.ParseLinks(urlInfo, html);

                            if (this.DataReceivedEvent != null)
                            {
                                this.DataReceivedEvent(
                                    new DataReceivedEventArgs
                                        {
                                            Url = urlInfo.UrlString, 
                                            Depth = urlInfo.Depth, 
                                            Html = html
                                        });
                            }

                            if (stream != null)
                            {
                                stream.Close();
                            }
                        }
                    }
                }
                catch (Exception exception)
                {
                    if (this.CrawlErrorEvent != null)
                    {
                        if (urlInfo != null)
                        {
                            this.CrawlErrorEvent(
                                new CrawlErrorEventArgs { Url = urlInfo.UrlString, Exception = exception });
                        }
                    }
                }
                finally
                {
                    if (request != null)
                    {
                        request.Abort();
                    }

                    if (response != null)
                    {
                        response.Close();
                    }
                }
            }
        }

        /// <summary>
        /// The initialize.
        /// </summary>
        private void Initialize()
        {
            if (this.Settings.SeedsAddress != null && this.Settings.SeedsAddress.Count > 0)
            {
                foreach (var seed in this.Settings.SeedsAddress)
                {
                    if (Regex.IsMatch(seed.Key, WebUrlRegularExpressions, RegexOptions.IgnoreCase))
                    {
                        UrlQueue.Instance.EnQueue(new UrlInfo(seed.Key,seed.Value) { Depth = 1 });
                    }
                }
            }

            for (int i = 0; i < this.Settings.ThreadCount; i++)
            {
                var threadStart = new ParameterizedThreadStart(this.CrawlProcess);

                this.threads[i] = new Thread(threadStart);
            }

            ServicePointManager.DefaultConnectionLimit = 256;
        }

        /// <summary>
        /// The is match regular.
        /// </summary>
        /// <param name="url">
        /// The url.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private bool IsMatchRegular(string url)
        {
            bool result = false;

            if (this.Settings.RegularFilterExpressions != null && this.Settings.RegularFilterExpressions.Count > 0)
            {
                if (
                    this.Settings.RegularFilterExpressions.Any(
                        pattern => Regex.IsMatch(url, pattern, RegexOptions.IgnoreCase)))
                {
                    result = true;
                }
            }
            else
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// The parse content.
        /// </summary>
        /// <param name="stream">
        /// The stream.
        /// </param>
        /// <param name="characterSet">
        /// The character set.
        /// </param>
        /// <returns>
        /// The <see cref="string"/>.
        /// </returns>
        private string ParseContent(Stream stream, string characterSet)
        {
            var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);

            byte[] buffer = memoryStream.ToArray();

            Encoding encode = Encoding.ASCII;
            string html = encode.GetString(buffer);

            string localCharacterSet = characterSet;

            Match match = Regex.Match(html, "<meta([^<]*)charset=([^<]*)\"", RegexOptions.IgnoreCase);
            if (match.Success)
            {
                localCharacterSet = match.Groups[2].Value;

                var stringBuilder = new StringBuilder();
                foreach (char item in localCharacterSet)
                {
                    if (item == ' ')
                    {
                        break;
                    }

                    if (item != '\"')
                    {
                        stringBuilder.Append(item);
                    }
                }

                localCharacterSet = stringBuilder.ToString();
            }

            if (string.IsNullOrEmpty(localCharacterSet))
            {
                localCharacterSet = characterSet;
            }

            if (!string.IsNullOrEmpty(localCharacterSet))
            {
                encode = Encoding.GetEncoding(localCharacterSet);
            }

            memoryStream.Close();

            return encode.GetString(buffer);
        }

        /// <summary>
        /// The parse links.
        /// </summary>
        /// <param name="urlInfo">
        /// The url info.
        /// </param>
        /// <param name="html">
        /// The html.
        /// </param>
        private void ParseLinks(UrlInfo urlInfo, string html)
        {
            if (this.Settings.Depth > 0 && urlInfo.Depth >= this.Settings.Depth)
            {
                return;
            }

            var urlDictionary = new Dictionary<string, string>();

            Match match = Regex.Match(html, "(?<=<a\\s+(?:[^>]*?\\s+)?href=\")([^ \"]*)(?=\")");
            while (match.Success)
            {
                // 以 href 作为 key
                string urlKey = match.Groups[1].Value;

                // 以 text 作为 value
                string urlValue = Regex.Replace(match.Groups[2].Value, "(?i)<.*?>", string.Empty);

                urlDictionary[urlKey] = urlValue;
                match = match.NextMatch();
            }

            foreach (var item in urlDictionary)
            {
                string href = item.Key;
                string text = item.Value;

                if (!string.IsNullOrEmpty(href))
                {
                    bool canBeAdd = true;

                    if (this.Settings.EscapeLinks != null && this.Settings.EscapeLinks.Count > 0)
                    {
                        if (this.Settings.EscapeLinks.Any(suffix => href.EndsWith(suffix, StringComparison.OrdinalIgnoreCase)))
                        {
                            canBeAdd = false;
                        }
                    }

                    if (urlInfo.FollowLinkRules != null && urlInfo.FollowLinkRules.Count>0)
                    {
                        if (!urlInfo.FollowLinkRules.Any(href.Contains))
                        {
                            canBeAdd = false;
                        }
                    }

                    if (canBeAdd)
                    {
                        string url = href.Replace("%3f", "?")
                            .Replace("%3d", "=")
                            .Replace("%2f", "/")
                            .Replace("&amp;", "&");

                        if (string.IsNullOrEmpty(url) || url.StartsWith("#")
                            || url.StartsWith("mailto:", StringComparison.OrdinalIgnoreCase)
                            || url.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                        {
                            continue;
                        }

                        var baseUri = new Uri(urlInfo.UrlString);
                        Uri currentUri = url.StartsWith("http", StringComparison.OrdinalIgnoreCase)
                                             ? new Uri(url)
                                             : new Uri(baseUri, url);

                        url = currentUri.AbsoluteUri;

                        if (this.Settings.LockHost)
                        {
                            // 去除二级域名后，判断域名是否相等，相等则认为是同一个站点
                            // 例如：mail.pzcast.com 和 www.pzcast.com
                            if (baseUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b)
                                != currentUri.Host.Split('.').Skip(1).Aggregate((a, b) => a + "." + b))
                            {
                                continue;
                            }
                        }

                        if (!this.IsMatchRegular(url))
                        {
                            continue;
                        }

                        var addUrlEventArgs = new AddUrlEventArgs { Title = text, Depth = urlInfo.Depth + 1, Url = url };
                        if (this.AddUrlEvent != null && !this.AddUrlEvent(addUrlEventArgs))
                        {
                            continue;
                        }

                        UrlQueue.Instance.EnQueue(new UrlInfo(url,urlInfo.FollowLinkRules) { Depth = urlInfo.Depth + 1 });
                    }
                }
            }
        }

        /// <summary>
        /// The persistence cookie.
        /// </summary>
        /// <param name="response">
        /// The response.
        /// </param>
        private void PersistenceCookie(HttpWebResponse response)
        {
            if (!this.Settings.KeepCookie)
            {
                return;
            }

            string cookies = response.Headers["Set-Cookie"];
            if (!string.IsNullOrEmpty(cookies))
            {
                var cookieUri =
                    new Uri(
                        string.Format(
                            "{0}://{1}:{2}/", 
                            response.ResponseUri.Scheme, 
                            response.ResponseUri.Host, 
                            response.ResponseUri.Port));

                this.cookieContainer.SetCookies(cookieUri, cookies);
            }
        }

        #endregion
    }
}