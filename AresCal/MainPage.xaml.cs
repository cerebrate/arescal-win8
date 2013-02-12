#region header

// AresCal - MainPage.xaml.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2013.  All rights reserved.
// 
// Licensed and made available under MS-PL: http://opensource.org/licenses/ms-pl .
// 
// Created: 2013-02-04 2:19 PM

#endregion

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

using ArkaneSystems.AresCal.Common;
using ArkaneSystems.AresCal.TileUpdater;

using Callisto.Controls;

using Windows.ApplicationModel.Background;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.Resources;
using Windows.Foundation;
using Windows.Storage;
using Windows.UI;
using Windows.UI.ApplicationSettings;
using Windows.UI.Popups;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Media.Imaging;
using Windows.UI.Xaml.Navigation;

// The Basic Page item template is documented at http://go.microsoft.com/fwlink/?LinkId=234237

namespace ArkaneSystems.AresCal
{
    /// <summary>
    ///     A basic page that provides characteristics common to most applications.
    /// </summary>
    public sealed partial class MainPage : LayoutAwarePage
    {
        private const string TASK_NAME_USERPRESENT = "TileUpdaterUser";
        private const string TASK_NAME_TIMER = "TileUpdaterTime";
        private const string TASK_ENTRY = "ArkaneSystems.AresCal.TileUpdater.TileUpdater";

        private int cachedDayOfWeek;

        private DataTransferManager dataTransferManager;

        public MainPage()
        {
            this.InitializeComponent();

            SettingsPane.GetForCurrentView().CommandsRequested += this.MainPage_CommandsRequested;
        }

        private void MainPage_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            var rl = new ResourceLoader();

            var sc = new SettingsCommand("options",
                                         rl.GetString("OptionsCommandText"),
                                         (x) =>
                                             {
                                                 // Create a new instance of the flyout.
                                                 var settings = new SettingsFlyout();

                                                 // Change header and content background colors away from defaults.
                                                 settings.HeaderBrush = new SolidColorBrush(Colors.DarkGray);
                                                 settings.HeaderText = rl.GetString("OptionsCommandText");

                                                 // Provide logo.
                                                 var bmp =
                                                     new BitmapImage(new Uri("ms-appx:///Assets/AresCal-SmallLogo.png"));
                                                 settings.SmallLogoImageSource = bmp;

                                                 // Set content for the flyout.
                                                 settings.Content = new SettingsContent();

                                                 // Handle the ad control.
                                                 settings.Closed +=
                                                     (o, o1) => { this.adControl.Visibility = Visibility.Visible; };

                                                 this.adControl.Visibility = Visibility.Collapsed;

                                                 // Open it.
                                                 settings.IsOpen = true;
                                             });

            var pc = new SettingsCommand("privacy",
                                         rl.GetString("PrivacyCommandText"),
                                         (x) =>
                                             {
                                                 // Create a new instance of the flyout.
                                                 var settings = new SettingsFlyout();

                                                 // Change header and content background colors away from defaults.
                                                 settings.HeaderBrush = new SolidColorBrush(Colors.DarkGray);
                                                 settings.HeaderText = rl.GetString("PrivacyCommandText");

                                                 // Provide logo.
                                                 var bmp =
                                                     new BitmapImage(new Uri("ms-appx:///Assets/AresCal-SmallLogo.png"));
                                                 settings.SmallLogoImageSource = bmp;

                                                 // Set content for the flyout.
                                                 settings.Content = new PrivacyContent();

                                                 // Handle the ad control.
                                                 settings.Closed +=
                                                     (o, o1) => { this.adControl.Visibility = Visibility.Visible; };

                                                 this.adControl.Visibility = Visibility.Collapsed;

                                                 // Open it.
                                                 settings.IsOpen = true;
                                             });

            args.Request.ApplicationCommands.Add(sc);
            args.Request.ApplicationCommands.Add(pc);
        }

        /// <summary>
        ///     Populates the page with content passed during navigation.  Any saved state is also
        ///     provided when recreating a page from a prior session.
        /// </summary>
        /// <param name="navigationParameter">
        ///     The parameter value passed to
        ///     <see cref="Frame.Navigate(Type, Object)" /> when this page was initially requested.
        /// </param>
        /// <param name="pageState">
        ///     A dictionary of state preserved by this page during an earlier
        ///     session.  This will be null the first time a page is visited.
        /// </param>
        protected override void LoadState(Object navigationParameter, Dictionary<String, Object> pageState)
        {
            // Restore values contained in app data.
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            if (roamingSettings.Values.ContainsKey("tsEnabled"))
                this.TsToggle.IsChecked = (bool) roamingSettings.Values["tsEnabled"];

            if (roamingSettings.Values.ContainsKey("epEnabled"))
                this.EpToggle.IsChecked = (bool) roamingSettings.Values["epEnabled"];

            // Restore values contained in session state.
            if (pageState != null && pageState.ContainsKey("txtSolDate"))
            {
                // If it contains one, it contains all.
                this.SolDate.Text = pageState["txtSolDate"].ToString();
                this.Date.Text = pageState["txtDate"].ToString();
                this.Time.Text = pageState["txtTime"].ToString();
                this.TsDate.Text = pageState["txtTranshuman"].ToString();
                this.EpDate.Text = pageState["txtEclipsePhase"].ToString();
            }
        }

        /// <summary>
        ///     Preserves state associated with this page in case the application is suspended or the
        ///     page is discarded from the navigation cache.  Values must conform to the serialization
        ///     requirements of <see cref="SuspensionManager.SessionState" />.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {
            // Save the current UI text.
            pageState["txtSolDate"] = this.SolDate.Text;
            pageState["txtDate"] = this.Date.Text;
            pageState["txtTime"] = this.Time.Text;
            pageState["txtTranshuman"] = this.TsDate.Text;
            pageState["txtEclipsePhase"] = this.EpDate.Text;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            TileUpdateScheduler.CreateScheduledUpdates();

            await this.CreateTileUpdaterTask();

            // Register this as a share source.
            this.dataTransferManager = DataTransferManager.GetForCurrentView();
            this.dataTransferManager.DataRequested += this.OnDataRequested;
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Unregister this as a share source.
            this.dataTransferManager.DataRequested -= this.OnDataRequested;
        }

        private async Task CreateTileUpdaterTask()
        {
            BackgroundAccessStatus result = await BackgroundExecutionManager.RequestAccessAsync();
            if (result == BackgroundAccessStatus.AllowedMayUseActiveRealTimeConnectivity ||
                result == BackgroundAccessStatus.AllowedWithAlwaysOnRealTimeConnectivity)
            {
                // Remove tasks if present.
                foreach (KeyValuePair<Guid, IBackgroundTaskRegistration> task in BackgroundTaskRegistration.AllTasks)
                    if ((task.Value.Name == TASK_NAME_TIMER) ||
                        (task.Value.Name == TASK_NAME_USERPRESENT))
                        task.Value.Unregister(false);

                // Set up the background timer task.
                var builderTimer = new BackgroundTaskBuilder();
                builderTimer.Name = TASK_NAME_TIMER;
                builderTimer.TaskEntryPoint = TASK_ENTRY;
                builderTimer.SetTrigger(new TimeTrigger(180, false));
                builderTimer.Register();

                // Set up the user present task.
                var builderUser = new BackgroundTaskBuilder();
                builderUser.Name = TASK_NAME_USERPRESENT;
                builderUser.TaskEntryPoint = TASK_ENTRY;
                builderUser.SetTrigger(new SystemTrigger(SystemTriggerType.UserPresent, false));
                builderUser.Register();
            }
        }

        private void TsToggle_Click(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["tsEnabled"] = this.TsToggle.IsChecked;
        }

        private void EpToggle_Click(object sender, RoutedEventArgs e)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["epEnabled"] = this.EpToggle.IsChecked;
        }

        public void DoTimeUpdate()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            // Get current martian datetime.
            var mdt = new MartianDateTime();

            this.SolDate.Text = String.Format("{0:f6}", mdt.MartianSolDate);
            this.Time.Text = mdt.Time;
            this.Date.Text = mdt.Date;

            if ((roamingSettings.Values.ContainsKey("epEnabled")) &&
                (bool) roamingSettings.Values["epEnabled"])
                this.EpDate.Text = mdt.EclipsePhaseDate;

            if ((roamingSettings.Values.ContainsKey("tsEnabled")) &&
                (bool) roamingSettings.Values["tsEnabled"])
                this.TsDate.Text = mdt.TranshumanSpaceDate;

            // If necessary, update background.
            if (mdt.DayOfWeek != this.cachedDayOfWeek)
            {
                this.cachedDayOfWeek = mdt.DayOfWeek;
                this.DoBackgroundUpdate(mdt.DayOfWeek);
            }
        }

        public void DoBackgroundUpdate(int selection)
        {
            BitmapImage backgroundImage;

            switch (selection)
            {
                case 1:
                    backgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Mars/mars_1.jpg"));
                    break;

                case 2:
                    backgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Mars/mars_2.jpg"));
                    break;

                case 3:
                    backgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Mars/mars_3.jpg"));
                    break;

                case 4:
                    backgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Mars/mars_4.jpg"));
                    break;

                case 5:
                    backgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Mars/mars_5.jpg"));
                    break;

                case 6:
                    backgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Mars/mars_6.jpg"));
                    break;

                case 7:
                    backgroundImage = new BitmapImage(new Uri("ms-appx:///Assets/Mars/mars_7.jpg"));
                    break;

                default:
                    throw new IndexOutOfRangeException();
            }

            this.ContentGrid.Background = new ImageBrush
                {
                    ImageSource = backgroundImage,
                    Stretch = Stretch.UniformToFill,
                    Opacity = 0.8
                };
        }

        private void OnVisualStateChanging(object sender, VisualStateChangedEventArgs e)
        {
            // Update the advertisement block to match the layout.
            switch (e.NewState.Name)
            {
                case "FullScreenLandscape":
                case "Filled":
                    this.adControl.AdUnitId = "10058667";
                    this.adControl.Refresh();
                    break;

                case "Snapped":
                    this.adControl.AdUnitId = "10058780";
                    this.adControl.Refresh();
                    break;

                case "FullScreenPortrait":
                    this.adControl.AdUnitId = "10058781";
                    this.adControl.Refresh();
                    break;
            }
        }

        private void AppBar_Opened(object sender, object e)
        {
            this.adControl.Visibility = Visibility.Collapsed;
        }

        private void AppBar_Closed(object sender, object e)
        {
            this.adControl.Visibility = Visibility.Visible;
        }

        // Enable sharing of data.
        private void OnDataRequested(DataTransferManager sender, DataRequestedEventArgs args)
        {
            DataPackage requestData = args.Request.Data;

            var mdt = new MartianDateTime();
            string share = String.Format("{0} AMT, {1}", mdt.Time, mdt.Date);

            requestData.Properties.Title = share;
            requestData.Properties.ApplicationName = "AresCal";
            // requestData.Properties.ApplicationListingUri
            requestData.SetText(share);
        }

        private Rect GetRect(object sender)
        {
            FrameworkElement element = sender as FrameworkElement;
            GeneralTransform elementTransform = element.TransformToVisual(null);
            Point point = elementTransform.TransformPoint(new Point());
            return new Rect(point, new Size(element.ActualWidth, element.ActualHeight));
        }

        private async void DoCopyTextBlock(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;

            var rl = new ResourceLoader();

            PopupMenu p = new PopupMenu();
            p.Commands.Add(new UICommand(rl.GetString("CopyCommandText"), (command) =>
                {
                    // Copy the sending text block to the clipboard.
                    TextBlock tb = sender as TextBlock;
                    string text = tb.Text;

                    DataPackage dp = new DataPackage();
                    dp.SetText(text);
                    Clipboard.SetContent(dp);
                }));
            await p.ShowForSelectionAsync(GetRect(sender), Placement.Above);
        }

        private async void DoCopyTime(object sender, RightTappedRoutedEventArgs e)
        {
            e.Handled = true;

            var rl = new ResourceLoader();

            PopupMenu p = new PopupMenu();
            p.Commands.Add(new UICommand(rl.GetString("CopyCommandText"), (command) =>
                {
                    // Copy the current time to the clipboard.
                    string text = String.Format("{0} AMT", new MartianDateTime().Time);

                    DataPackage dp = new DataPackage();
                    dp.SetText(text);
                    Clipboard.SetContent(dp);
                }));
            await p.ShowForSelectionAsync(GetRect(sender), Placement.Above);
        }
    }
}
