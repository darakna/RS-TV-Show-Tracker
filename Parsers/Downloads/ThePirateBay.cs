﻿namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides support for scraping The Pirate Bay.
    /// </summary>
    public class ThePirateBay : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "The Pirate Bay";
            }
        }

        /// <summary>
        /// Gets a value indicating whether the site requires cookies to authenticate.
        /// </summary>
        /// <value><c>true</c> if requires cookies; otherwise, <c>false</c>.</value>
        public override bool RequiresCookies
        {
            get
            {
                return false;
            }
        }

        /// <summary>
        /// Gets the type of the link.
        /// </summary>
        /// <value>The type of the link.</value>
        public override Types Type
        {
            get
            {
                return Types.Torrent;
            }
        }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override List<Link> Search(string query)
        {
            var html  = Utils.GetHTML("http://thepiratebay.org/search/" + Uri.EscapeUriString(query) + "/0/7/0");
            var links = html.DocumentNode.SelectNodes("//table/tr/td[2]/div/a");

            if (links == null)
            {
                return null;
            }

            return links.Select(node => new Link
                   {
                       Site    = Name,
                       Release = node.InnerText,
                       URL     = node.SelectSingleNode("../../a[1]").GetAttributeValue("href", string.Empty),
                       Size    = Regex.Match(node.SelectSingleNode("../../font").InnerText, "Size (.*?),").Groups[1].Value.Replace("&nbsp;", " ").Replace("i", string.Empty),
                       Quality = ParseQuality(node.InnerText.Replace(' ', '.')),
                       Type    = Types.Torrent
                   }).ToList();
        }

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Link.Qualities ParseQuality(string release)
        {
            if (IsMatch(release, @"\.1080(i|p)\.", @"\.WEB[_\-]?DL\."))
            {
                return Link.Qualities.WebDL_1080;
            }
            if (IsMatch(release, @"\.1080(i|p)\.", @"\.BluRay\."))
            {
                return Link.Qualities.BluRay_1080;
            }
            if (IsMatch(release, @"\.1080(i|p)\.", @"\.HDTV\."))
            {
                return Link.Qualities.HDTV_1080;
            }
            if (IsMatch(release, @"\.720p\.", @"\.WEB[_\-]?DL\."))
            {
                return Link.Qualities.WebDL_720p;
            }
            if (IsMatch(release, @"\.720p\.", @"\.BluRay\."))
            {
                return Link.Qualities.BluRay_720p;
            }
            if (IsMatch(release, @"\.720p\.", @"\.HDTV\."))
            {
                return Link.Qualities.HDTV_720p;
            }
            if (IsMatch(release, @"\.((HR|HiRes|High[_\-]?Resolution)\.|x264\-|H264)"))
            {
                return Link.Qualities.HR_x264;
            }
            if (IsMatch(release, @"\.(HDTV|PDTV|DVBRip|DVDRip)\."))
            {
                return Link.Qualities.HDTV_XviD;
            }
            if (IsMatch(release, @"\.TVRip\."))
            {
                return Link.Qualities.TVRip;
            }
            return Link.Qualities.Unknown;
        }

        /// <summary>
        /// Determines whether the specified input is matches all the specified regexes.
        /// </summary>
        /// <param name="input">The input.</param>
        /// <param name="regexes">The regexes.</param>
        /// <returns>
        /// 	<c>true</c> if the specified input matches all the specified regexes; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string input, params string[] regexes)
        {
            return regexes.All(regex => Regex.IsMatch(input, regex, RegexOptions.IgnoreCase));
        }
    }
}