﻿namespace RoliSoft.TVShowTracker.Parsers.OnlineVideos.Engines
{
    using System;
    using System.Text.RegularExpressions;

    /// <summary>
    /// Provides support for searching videos on SideReel.
    /// </summary>
    public class SideReel : OnlineVideoSearchEngine
    {
        /// <summary>
        /// Searches for videos on SideReel.
        /// </summary>
        /// <param name="name">The name of the show.</param>
        /// <param name="episode">The episode number.</param>
        /// <param name="extra">This field is not used here.</param>
        /// <exception cref="OnlineVideoNotFoundException">No video was found.</exception>
        public override string Search(string name, string episode, object extra = null)
        {
            var g = WebSearch.Google("intitle:\"{0}\" intitle:\"online links for\" site:sidereel.com".FormatWith(name));

            if (string.IsNullOrWhiteSpace(g))
            {
                g = WebSearch.Google("intitle:{0} intitle:\"online links for\" site:sidereel.com".FormatWith(name));
            }

            g = g.Replace("'", "%27");

            var urln = Regex.Match(g, @"sidereel\.com/([^/]+)", RegexOptions.IgnoreCase);

            if (!string.IsNullOrWhiteSpace(g) && urln.Success)
            {
                return g;
            }
            else
            {
                throw new OnlineVideoNotFoundException("No videos could be found on SideReel using Google." + Environment.NewLine + "You can try to use SideReel's internal search engine.", "Open SideReel search page", "http://www.sidereel.com/_television/search?q=" + Uri.EscapeUriString(ShowNames.Tools.Normalize(name)));
            }
        }
    }
}
