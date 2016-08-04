using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Domain
{

    /// <summary>
    /// 种子地址
    /// </summary>
    public class CrawlerSeed
    {
        public string SiteName { get; set; }
        public string SiteUrl { get; set; }
        /// <summary>
        /// 跟踪链接规则
        /// </summary>
        public IList<string> FollowLinkRules { get; set; }
        
    }
}
