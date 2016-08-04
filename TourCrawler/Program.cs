// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Program.cs" company="pzcast">
//   (C) 2015 pzcast. All rights reserved.
// </copyright>
// <summary>
//   The program.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace SimpleCrawler.Demo
{
    using SimpleCrawler;
    using System;
    using System.Collections;

    /// <summary>
    /// The program.
    /// </summary>
    internal class TourCrawler
    {
        #region Static Fields

        /// <summary>
        /// The settings.
        /// </summary>
        private static readonly CrawlSettings Settings = new CrawlSettings();

        /// <summary>
        /// The filter.
        /// 关于使用 Bloom 算法去除重复 URL： 
        /// </summary>
        private static BloomFilter<string> filter;

        #endregion

        #region Methods

        /// <summary>
        /// The main.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void Main(string[] args)
        {
            filter = new BloomFilter<string>(200000);

            /* 

            // 设置种子地址和爬取规则
            //Settings.SeedsAddress.Add(
            //    "http://www.ctszj.com.cn/ajax/soso_more_group.php?pageSize=99999&pageIndex=0"
            //    , new string[] { "route.php?gid=" }
            //    );
            Settings.SeedsAddress.Add(
                "http://www.ctszj.com.cn/domestic_tours.php"
                , new string[] { "route.php?gid=","page=" }
                );
                */
            Settings.SeedsAddress.Add(
                "http://www.cits.cn/"
                , new string[] { "^http://[a-z]+.cits.cn$" }
                );
            // 设置 URL 关键字


            // 设置爬取线程个数
            Settings.ThreadCount = 5;

            // 设置爬取深度
            Settings.Depth = 7;

            // 设置爬取时忽略的 Link，通过后缀名的方式，可以添加多个
            Settings.EscapeLinks.Add(".jpg");

            // 设置自动限速，1~5 秒随机间隔的自动限速
            Settings.AutoSpeedLimit = false;

            // 设置都是锁定域名,去除二级域名后，判断域名是否相等，相等则认为是同一个站点
            // 例如：mail.pzcast.com 和 www.pzcast.com
            Settings.LockHost = false;

            // 设置请求的 User-Agent HTTP 标头的值
            // settings.UserAgent 已提供默认值，如有特殊需求则自行设置

            // 设置请求页面的超时时间，默认值 15000 毫秒
            // settings.Timeout 按照自己的要求确定超时时间

            // 设置用于过滤的正则表达式
            // settings.RegularFilterExpressions.Add("");
            var master = new CrawlMaster(Settings);
            master.AddUrlEvent += MasterAddUrlEvent;
            master.DataReceivedEvent += MasterDataReceivedEvent;
            master.Crawl();

            Console.ReadKey();
        }
        static int addedLinksAmount;
        /// <summary>
        /// The master add url event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        /// <returns>
        /// The <see cref="bool"/>.
        /// </returns>
        private static bool MasterAddUrlEvent(AddUrlEventArgs args)
        {
            if (!filter.Contains(args.Url))
            {
                filter.Add(args.Url);
                Console.WriteLine(addedLinksAmount+ args.Url);
                addedLinksAmount++;
                return true;
            }

            return false; // 返回 false 代表：不添加到队列中
        }
        static int downloadedPageAmount;
        /// <summary>
        /// The master data received event.
        /// </summary>
        /// <param name="args">
        /// The args.
        /// </param>
        private static void MasterDataReceivedEvent(DataReceivedEventArgs args)
        {
            
            Console.WriteLine(downloadedPageAmount+ "下载完毕:" + args.Url);
            downloadedPageAmount++;
            // 在此处解析页面，可以用类似于 HtmlAgilityPack（页面解析组件）的东东、也可以用正则表达式、还可以自己进行字符串分析
        }

        #endregion
    }
}