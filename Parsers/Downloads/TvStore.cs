﻿namespace RoliSoft.TVShowTracker.Parsers.Downloads
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Text.RegularExpressions;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Bson;

    /// <summary>
    /// Provides support for scraping tvstore.me.
    /// </summary>
    public class TvStore : DownloadSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "TvStore";
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
                return true;
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
        /// Gets or sets the show IDs on the site.
        /// </summary>
        /// <value>The show IDs.</value>
        public Dictionary<int, string> ShowIDs { get; set; }

        /// <summary>
        /// Searches for download links on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found download links.</returns>
        public override List<Link> Search(string query)
        {
            var gyors = Utils.GetURL("http://tvstore.me/torrent/br_process.php?gyors=" + Convert.ToBase64String(Encoding.UTF8.GetBytes(ShowNames.ReplaceEpisode(query, 1))).Replace('=', '_') + "&now=" + Utils.DateTimeToUnix(DateTime.Now), cookies: Cookies);
            var arr   = gyors.Split('\\');

            if (arr[0] == "0")
            {
                return null;
            }

            var list = new List<Link>();
            var idx  = 3;

            for (;idx <= (arr.Length - 10);)
            {
                var link = new Link { Site = Name };
                var name = GetShowForID(int.Parse(arr[idx].Trim()));

                idx++;

                link.URL = "http://tvstore.me/torrent/download.php?id=" + arr[idx].Trim();

                idx++;

                link.Release = name + " " + arr[idx].Trim();

                idx++;

                var quality   = arr[idx].Trim();
                link.Quality  = ParseQuality(quality);
                link.Release += " " + quality.Replace("[", string.Empty).Replace("]", string.Empty).Replace(" - ", " ");

                idx += 7;

                link.Size = Utils.GetFileSize(long.Parse(arr[idx].Trim()));

                idx += 18;

                list.Add(link);
            }

            return list;
        }

        /// <summary>
        /// Parses the quality of the file.
        /// </summary>
        /// <param name="release">The release name.</param>
        /// <returns>Extracted quality or Unknown.</returns>
        public static Link.Qualities ParseQuality(string release)
        {
            var q = Regex.Match(release, @"\[(?:(?:PROPER|REPACK)(?:\s\-)?)?\s*(.*?)\s\-").Groups[1].Value;

            if (IsMatch("Blu-ray-1080p", q))
            {
                return Link.Qualities.BluRay_1080;
            }
            if (IsMatch("HDTV-1080(p|i)", q))
            {
                return Link.Qualities.HDTV_1080;
            }
            if (IsMatch("Web-Dl-720p", q))
            {
                return Link.Qualities.WebDL_720p;
            }
            if (IsMatch("Blu-ray-720p", q))
            {
                return Link.Qualities.BluRay_720p;
            }
            if (IsMatch("HDTV-720p", q))
            {
                return Link.Qualities.HDTV_720p;
            }
            if (IsMatch("HR-HDTV", q))
            {
                return Link.Qualities.HR_x264;
            }
            if (IsMatch("TvRip", q))
            {
                return Link.Qualities.TVRip;
            }
            if (IsMatch("(PDTV|DVDSRC|Rip$)", q))
            {
                return Link.Qualities.HDTV_XviD;
            }

            return Link.Qualities.Unknown;
        }

        /// <summary>
        /// Determines whether the specified pattern is a match.
        /// </summary>
        /// <param name="pattern">The pattern.</param>
        /// <param name="input">The input.</param>
        /// <returns>
        /// 	<c>true</c> if the specified pattern is a match; otherwise, <c>false</c>.
        /// </returns>
        public static bool IsMatch(string pattern, string input)
        {
            return Regex.IsMatch(input, pattern.Replace("-", @"(\-|\s)?"), RegexOptions.IgnoreCase);
        }

        /// <summary>
        /// Gets the IDs from the browse page.
        /// </summary>
        public void GetIDs()
        {
            var browse  = Utils.GetURL("http://tvstore.me/torrent/browse.php", cookies: Cookies);
            var matches = Regex.Matches(browse, @"catse\[(?<id>\d+)\]\s*=\s*'(?<name>[^']+)';");

            ShowIDs = matches.Cast<Match>()
                     .ToDictionary(match => int.Parse(match.Groups["id"].Value),
                                   match => match.Groups["name"].Value);

            using (var file = File.Create(Path.Combine(Path.GetTempPath(), "TvStore-IDs")))
            using (var bson = new BsonWriter(file))
            {
                var js = new JsonSerializer();
                js.Serialize(bson, ShowIDs);
                file.Close();
            }
        }

        /// <summary>
        /// Gets the show name for an ID.
        /// </summary>
        /// <param name="id">The ID.</param>
        /// <returns>Corresponding show name.</returns>
        public string GetShowForID(int id)
        {
            if (ShowIDs == null)
            {
                var fn = Path.Combine(Path.GetTempPath(), "TvStore-IDs");

                if (File.Exists(fn))
                {
                    using (var file = File.OpenRead(fn))
                    using (var bson = new BsonReader(file))
                    {
                        var js = new JsonSerializer();
                        ShowIDs = js.Deserialize<Dictionary<int, string>>(bson);
                        file.Close();
                    }
                }
                else
                {
                    GetIDs();
                }
            }

            if (ShowIDs.ContainsKey(id))
            {
                return ShowIDs[id];
            }
            else
            {
                // try to refresh
                GetIDs();

                if (ShowIDs.ContainsKey(id))
                {
                    return ShowIDs[id];
                }
                else
                {
                    return "ID-" + id;
                }
            }
        }
    }
}