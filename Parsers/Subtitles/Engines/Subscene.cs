﻿namespace RoliSoft.TVShowTracker.Parsers.Subtitles.Engines
{
    using System;
    using System.Collections.Generic;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for scraping Subscene.
    /// </summary>
    [TestFixture]
    public class Subscene : SubtitleSearchEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "Subscene";
            }
        }

        /// <summary>
        /// Gets the URL of the site.
        /// </summary>
        /// <value>The site location.</value>
        public override string Site
        {
            get
            {
                return "http://subscene.com/";
            }
        }

        /// <summary>
        /// Gets the URL to the favicon of the site.
        /// </summary>
        /// <value>The icon location.</value>
        public override string Icon
        {
            get
            {
                return "http://subscene.com/favicon.png";
            }
        }

        /// <summary>
        /// Searches for subtitles on the service.
        /// </summary>
        /// <param name="query">The name of the release to search for.</param>
        /// <returns>List of found subtitles.</returns>
        public override IEnumerable<Subtitle> Search(string query)
        {
            var html = Utils.GetHTML(Site + "s.aspx?q=" + Uri.EscapeUriString(ShowNames.Tools.Normalize(query)));
            var subs = html.DocumentNode.SelectNodes("//a[@class='a1']");
            
            if (subs == null)
            {
                yield break;
            }

            foreach (var node in subs)
            {
                if (!ShowNames.Tools.IsMatch(query, node.GetTextValue("span[2]").Trim()))
                {
                    continue;
                }

                yield return new Subtitle
                   {
                       Site         = Name,
                       Release      = node.GetTextValue("span[2]").Trim(),
                       Language     = Addic7ed.ParseLanguage(node.GetTextValue("span[1]").Trim()),
                       URL          = Site.TrimEnd('/') + node.GetAttributeValue("href"),
                       IsLinkDirect = false
                   };
            }
        }
    }
}
