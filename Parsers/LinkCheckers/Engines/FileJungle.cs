﻿namespace RoliSoft.TVShowTracker.Parsers.LinkCheckers.Engines
{
    using System;

    using NUnit.Framework;

    /// <summary>
    /// Provides support for checking FileJungle links.
    /// </summary>
    [TestFixture]
    public class FileJungle : LinkCheckerEngine
    {
        /// <summary>
        /// Gets the name of the site.
        /// </summary>
        /// <value>The name.</value>
        public override string Name
        {
            get
            {
                return "FileJungle";
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
                return "http://www.filejungle.com/";
            }
        }

        /// <summary>
        /// Gets the name of the plugin's developer.
        /// </summary>
        /// <value>The name of the plugin's developer.</value>
        public override string Developer
        {
            get
            {
                return "RoliSoft";
            }
        }

        /// <summary>
        /// Gets the version number of the plugin.
        /// </summary>
        /// <value>The version number of the plugin.</value>
        public override Version Version
        {
            get
            {
                return Utils.DateTimeToVersion("2011-10-01 8:41 AM");
            }
        }

        /// <summary>
        /// Checks the availability of the link on the service.
        /// </summary>
        /// <param name="url">The link to check.</param>
        /// <returns>
        ///   <c>true</c> if the link is available; otherwise, <c>false</c>.
        /// </returns>
        public override bool Check(string url)
        {
            var html = Utils.GetHTML(Site + "check_links.php", "urls=" + Utils.EncodeURL(url));
            var node = html.DocumentNode.SelectSingleNode("//div[contains(@class, 'linkChecker')]//span[@class='icon approved']");

            return node != null;
        }

        /// <summary>
        /// Determines whether this instance can check the availability of the link on the specified service.
        /// </summary>
        /// <param name="url">The link to check.</param>
        /// <returns>
        ///   <c>true</c> if this instance can check the specified service; otherwise, <c>false</c>.
        /// </returns>
        public override bool CanCheck(string url)
        {
            return new Uri(url).Host.EndsWith("filejungle.com");
        }

        /// <summary>
        /// Tests the link checker.
        /// </summary>
        [Test]
        public override void Test()
        {
            var s1 = Check("http://www.filejungle.com/f/qdX7qN/rs tv show tracker unit test file.txt");
            Assert.IsTrue(s1);

            var s2 = Check("http://www.filejungle.com/f/xxX7qN/rs tv show tracker unit test file.txt");
            Assert.IsFalse(s2);
        }
    }
}
