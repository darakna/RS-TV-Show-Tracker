﻿namespace RoliSoft.TVShowTracker
{
    using System;
    using System.Collections.Generic;
    using System.Collections.ObjectModel;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Runtime.InteropServices;
    using System.Windows;
    using System.Windows.Controls;
    using System.Windows.Interop;
    using System.Windows.Media.Animation;
    using System.Windows.Media.Imaging;

    using Microsoft.Win32;

    using RoliSoft.TVShowTracker.Helpers;
    using RoliSoft.TVShowTracker.Parsers.Downloads;

    using Image = System.Windows.Controls.Image;

    /// <summary>
    /// Interaction logic for DownloadLinksPage.xaml
    /// </summary>
    public partial class DownloadLinksPage : UserControl
    {
        /// <summary>
        /// Gets or sets the name of the specified torrent downloader.
        /// </summary>
        /// <value>The default torrent.</value>
        public string DefaultTorrent { get; set; }

        /// <summary>
        /// Gets or sets the download links list view item collection.
        /// </summary>
        /// <value>The download links list view item collection.</value>
        public ObservableCollection<LinkItem> DownloadLinksListViewItemCollection { get; set; }

        /// <summary>
        /// Extended class of the original Link class to handle the context menu items.
        /// </summary>
        public class LinkItem : DownloadSearchEngine.Link
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="LinkItem"/> class.
            /// </summary>
            /// <param name="link">The link.</param>
            public LinkItem(DownloadSearchEngine.Link link)
            {
                // unfortunately .NET doesn't support upcasting, so we need to do it the hard way

                Site         = link.Site;
                Release      = link.Release;
                Quality      = link.Quality;
                Size         = link.Size;
                Type         = link.Type;
                URL          = link.URL;
                IsLinkDirect = link.IsLinkDirect;
            }

            /// <summary>
            /// Gets the image of the link type.
            /// </summary>
            /// <value>The type's image.</value>
            public string TypeImage
            {
                get
                {
                    switch(Type)
                    {
                        case DownloadSearchEngine.Types.Torrent:
                            return "/RSTVShowTracker;component/Images/torrent.png";

                        case DownloadSearchEngine.Types.Usenet:
                            return "/RSTVShowTracker;component/Images/usenet.png";

                        default:
                            return "/RSTVShowTracker;component/Images/filehoster.png";
                    }
                }
            }

            /// <summary>
            /// Gets a value whether to show the "Show open page" context menu item.
            /// </summary>
            /// <value>Visible or Collapsed.</value>
            public string ShowOpenPage
            {
                get
                {
                    return Type == DownloadSearchEngine.Types.Http || !IsLinkDirect ? "Visible" : "Collapsed";
                }
            }

            /// <summary>
            /// Gets a value whether to show the "Download file" context menu item.
            /// </summary>
            /// <value>Visible or Collapsed.</value>
            public string ShowDownloadFile
            {
                get
                {
                    return Type != DownloadSearchEngine.Types.Http && IsLinkDirect ? "Visible" : "Collapsed";
                }
            }

            /// <summary>
            /// Gets a value whether to show the "Send to associated application" context menu item.
            /// </summary>
            /// <value>Visible or Collapsed.</value>
            public string ShowSendToAssociated
            {
                get
                {
                    return Type != DownloadSearchEngine.Types.Http && IsLinkDirect ? "Visible" : "Collapsed";
                }
            }

            /// <summary>
            /// Gets a value whether to show the "Send to [torrent application]" context menu item.
            /// </summary>
            /// <value>Visible or Collapsed.</value>
            public string ShowSendToTorrent
            {
                get
                {
                    return Type == DownloadSearchEngine.Types.Torrent && IsLinkDirect && !string.IsNullOrWhiteSpace(MainWindow.Active.activeDownloadLinksPage.DefaultTorrent) ? "Visible" : "Collapsed";
                }
            }
        }

        /// <summary>
        /// Gets or sets the active search.
        /// </summary>
        /// <value>The active search.</value>
        public DownloadSearch ActiveSearch { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="SubtitlesPage"/> class.
        /// </summary>
        public DownloadLinksPage()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Sets the status message.
        /// </summary>
        /// <param name="message">The message.</param>
        /// <param name="activity">if set to <c>true</c> an animating spinner will be displayed.</param>
        public void SetStatus(string message, bool activity = false)
        {
            Dispatcher.Invoke((Func<bool>)delegate
            {
                statusLabel.Content = message;

                if (activity)
                {
                    statusLabel.Padding = new Thickness(24, 0, 24, 0);
                    statusThrobber.Visibility = Visibility.Visible;
                    ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Begin();
                }
                else
                {
                    ((Storyboard)statusThrobber.FindResource("statusThrobberSpinner")).Stop();
                    statusThrobber.Visibility = Visibility.Hidden;
                    statusLabel.Padding = new Thickness(7, 0, 7, 0);
                }
                return true;
            });
        }

        [DllImport("shell32.dll", EntryPoint = "ExtractIconEx")]
        private static extern int ExtractIconExA(string lpszFile, int nIconIndex, ref IntPtr phiconLarge, ref IntPtr phiconSmall, int nIcons);

        [DllImport("user32.dll")]
        private static extern int DestroyIcon(IntPtr hIcon);

        /// <summary>
        /// Handles the Loaded event of the UserControl control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void UserControlLoaded(object sender, RoutedEventArgs e)
        {
            if (DownloadLinksListViewItemCollection == null)
            {
                DownloadLinksListViewItemCollection = new ObservableCollection<LinkItem>();
                listView.ItemsSource                = DownloadLinksListViewItemCollection;
            }

            var cm  = listView.ContextMenu;
            var tdl = Database.XmlSetting("Torrent Downloader");

            if (!string.IsNullOrWhiteSpace(tdl))
            {
                DefaultTorrent = FileVersionInfo.GetVersionInfo(tdl).ProductName;
                ((MenuItem)cm.Items[3]).Header = "Send to " + DefaultTorrent;

                try
                {
                    var largeIcon = IntPtr.Zero;
                    var smallIcon = IntPtr.Zero;

                    ExtractIconExA(tdl, 0, ref largeIcon, ref smallIcon, 1);
                    DestroyIcon(largeIcon);

                    if (smallIcon != IntPtr.Zero)
                    {
                        ((MenuItem)cm.Items[3]).Icon = new Image
                            {
                                Source = Imaging.CreateBitmapSourceFromHBitmap(Icon.FromHandle(smallIcon).ToBitmap().GetHbitmap(), IntPtr.Zero, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions())
                            };
                    }
                }
                catch { }
            }
            else
            {
                cm.Items.RemoveAt(3);
            }
        }

        /// <summary>
        /// Initiates a search on this usercontrol.
        /// </summary>
        /// <param name="query">The query.</param>
        public void Search(string query)
        {
            Dispatcher.Invoke((Func<bool>)delegate
            {
                // cancel if one is running
                if (searchButton.Content.ToString() == "Cancel")
                {
                    ActiveSearch.CancelAsync();
                    DownloadSearchDone();
                }

                textBox.Text = query;
                SearchButtonClick(null, null);

                return true;
            });
        }

        /// <summary>
        /// Handles the Click event of the searchButton control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SearchButtonClick(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(textBox.Text)) return;

            if (searchButton.Content.ToString() == "Cancel")
            {
                ActiveSearch.CancelAsync();
                DownloadSearchDone();
                return;
            }

            DownloadLinksListViewItemCollection.Clear();

            textBox.IsEnabled    = false;
            searchButton.Content = "Cancel";

            ActiveSearch                                = new DownloadSearch();
            ActiveSearch.DownloadSearchDone            += DownloadSearchDone;
            ActiveSearch.DownloadSearchProgressChanged += DownloadSearchProgressChanged;
            
            SetStatus("Searching for download links on " + (string.Join(", ", ActiveSearch.SearchEngines.Select(engine => engine.Name).ToArray())) + "...", true);

            ActiveSearch.SearchAsync(textBox.Text);
        }

        /// <summary>
        /// Called when a download link search progress has changed.
        /// </summary>
        private void DownloadSearchProgressChanged(List<DownloadSearchEngine.Link> links, double percentage, List<string> remaining)
        {
            SetStatus("Searching for download links on " + (string.Join(", ", remaining)) + "...", true);

            Dispatcher.Invoke((Func<bool>)delegate
                {
                    if (links != null)
                    {
                        DownloadLinksListViewItemCollection.AddRange(links.Select(link => new LinkItem(link)));
                    }

                    return true;
                });
        }

        /// <summary>
        /// Called when a download link search is done on all engines.
        /// </summary>
        private void DownloadSearchDone()
        {
            Dispatcher.Invoke((Func<bool>)delegate
                {
                    textBox.IsEnabled    = true;
                    searchButton.Content = "Search";

                    if (DownloadLinksListViewItemCollection.Count != 0)
                    {
                        SetStatus("Found " + Utils.FormatNumber(DownloadLinksListViewItemCollection.Count, "download link") + "!");
                    }
                    else
                    {
                        SetStatus("Couldn't find any download links.");
                    }

                    return true;
                });
        }

        #region Open page
        /// <summary>
        /// Handles the Click event of the OpenPage control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void OpenPageClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            Utils.Run(((LinkItem)listView.SelectedValue).URL);
        }
        #endregion

        #region Download file
        /// <summary>
        /// Handles the Click event of the DownloadFile control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void DownloadFileClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var link = (LinkItem)listView.SelectedValue;

            var uri = new Uri(link.URL);
            SetStatus("Sending request to " + uri.DnsSafeHost.Replace("www.", string.Empty) + "...", true);

            var wc  = new WebClientExt();
            var tmp = Utils.GetRandomFileName("torrent");

            var cookies = Database.XmlSetting(link.Site + " Cookies");
            if (!string.IsNullOrWhiteSpace(cookies))
            {
                wc.Headers[HttpRequestHeader.Cookie] = cookies;
            }

            wc.Headers[HttpRequestHeader.Referer] = "http://" + uri.DnsSafeHost + "/";
            wc.DownloadFileCompleted             += WebClientDownloadFileCompleted;
            wc.DownloadProgressChanged           += (s, a) => SetStatus("Downloading file... (" + a.ProgressPercentage + "%)", true);

            wc.DownloadFileAsync(uri, tmp, new[]
                {
                    // temporary file name
                    tmp,
                    // action to do when finished
                    sender is string ? sender as string : "DownloadFile"
                });
        }
        #endregion

        #region Send to associated
        /// <summary>
        /// Handles the Click event of the SendToAssociated control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SendToAssociatedClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            DownloadFileClick("SendToAssociated", e);
        }
        #endregion

        #region Send to [torrent application]
        /// <summary>
        /// Handles the Click event of the SendToTorrent control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.RoutedEventArgs"/> instance containing the event data.</param>
        private void SendToTorrentClick(object sender, RoutedEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            DownloadFileClick("SendToTorrent", e);
        }
        #endregion

        /// <summary>
        /// Handles the DownloadFileCompleted event of the wc control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.ComponentModel.AsyncCompletedEventArgs"/> instance containing the event data.</param>
        private void WebClientDownloadFileCompleted(object sender, System.ComponentModel.AsyncCompletedEventArgs e)
        {
            var web   = sender as WebClientExt;
            var token = e.UserState as string[];
            var file  = web.FileName;

            switch (token[1])
            {
                case "DownloadFile":
                    var sfd = new SaveFileDialog
                        {
                            CheckPathExists = true,
                            FileName = file
                        };

                    if (sfd.ShowDialog().Value)
                    {
                        if (File.Exists(sfd.FileName))
                        {
                            File.Delete(sfd.FileName);
                        }

                        File.Move(token[0], sfd.FileName);
                    }
                    else
                    {
                        File.Delete(token[0]);
                    }

                    SetStatus("File downloaded successfully.");
                    break;

                case "SendToAssociated":
                    Utils.Run(token[0]);

                    SetStatus("File sent to associated application successfully.");
                    break;

                case "SendToTorrent":
                    Utils.Run(Database.XmlSetting("Torrent Downloader"), token[0]);

                    SetStatus("File sent to " + DefaultTorrent + " successfully.");
                    break;
            }
        }

        /// <summary>
        /// Handles the MouseDoubleClick event of the listView control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Input.MouseButtonEventArgs"/> instance containing the event data.</param>
        private void ListViewMouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (listView.SelectedIndex == -1) return;

            var link = (LinkItem)listView.SelectedValue;

            if (link.ShowOpenPage == "Visible")
            {
                OpenPageClick(sender, e);
            }
            else if(link.ShowSendToTorrent == "Visible")
            {
                SendToTorrentClick(sender, e);
            }
            else if (link.ShowSendToAssociated == "Visible")
            {
                SendToAssociatedClick(sender, e);
            }
            else
            {
                DownloadFileClick(sender, e);
            }
        }
    }
}