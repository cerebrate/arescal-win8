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
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
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

        private int cachedDayOfWeek = 0;

        private DataTransferManager dataTransferManager;

        public MainPage()
        {
            this.InitializeComponent();

            SettingsPane.GetForCurrentView().CommandsRequested += MainPage_CommandsRequested;
        }

        private void MainPage_CommandsRequested(SettingsPane sender, SettingsPaneCommandsRequestedEventArgs args)
        {
            ResourceLoader rl = new ResourceLoader();

            SettingsCommand sc = new SettingsCommand("options", rl.GetString("OptionsCommandText"), (x) =>
            {
                // Create a new instance of the flyout.
                SettingsFlyout settings = new SettingsFlyout();

                // Change header and content background colors away from defaults.
                settings.HeaderBrush = new SolidColorBrush(Colors.DarkGray);
                settings.HeaderText = rl.GetString("OptionsCommandText");

                // Provide logo.
                BitmapImage bmp = new BitmapImage(new Uri("ms-appx:///Assets/AresCal-SmallLogo.png"));
                settings.SmallLogoImageSource = bmp;

                // Set content for the flyout.
                settings.Content = new SettingsContent();

                // Handle the ad control.
                settings.Closed += (o, o1) =>
                    {
                        this.adControl.Visibility = Visibility.Visible;
                    };

                this.adControl.Visibility = Visibility.Collapsed;

                // Open it.
                settings.IsOpen = true;
            });

            SettingsCommand pc = new SettingsCommand("privacy", rl.GetString("PrivacyCommandText"), (x) =>
            {
                // Create a new instance of the flyout.
                SettingsFlyout settings = new SettingsFlyout();

                // Change header and content background colors away from defaults.
                settings.HeaderBrush = new SolidColorBrush(Colors.DarkGray);
                settings.HeaderText = rl.GetString("PrivacyCommandText");

                // Provide logo.
                BitmapImage bmp = new BitmapImage(new Uri("ms-appx:///Assets/AresCal-SmallLogo.png"));
                settings.SmallLogoImageSource = bmp;

                // Set content for the flyout.
                settings.Content = new PrivacyContent();

                // Handle the ad control.
                settings.Closed += (o, o1) =>
                {
                    this.adControl.Visibility = Visibility.Visible;
                };

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
                TsToggle.IsChecked = (bool)roamingSettings.Values["tsEnabled"];

            if (roamingSettings.Values.ContainsKey("epEnabled"))
                EpToggle.IsChecked = (bool)roamingSettings.Values["epEnabled"];

            // Restore values contained in session state.
            if (pageState != null && pageState.ContainsKey("txtSolDate"))
            {
                // If it contains one, it contains all.
                SolDate.Text = pageState["txtSolDate"].ToString();
                Date.Text = pageState["txtDate"].ToString();
                Time.Text = pageState["txtTime"].ToString();
                TsDate.Text = pageState["txtTranshuman"].ToString();
                EpDate.Text = pageState["txtEclipsePhase"].ToString();
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
            pageState["txtSolDate"] = SolDate.Text;
            pageState["txtDate"] = Date.Text;
            pageState["txtTime"] = Time.Text;
            pageState["txtTranshuman"] = TsDate.Text;
            pageState["txtEclipsePhase"] = EpDate.Text;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            TileUpdateScheduler.CreateScheduledUpdates();

            await this.CreateTileUpdaterTask();

            // Register this as a share source.
            this.dataTransferManager = DataTransferManager.GetForCurrentView();
            this.dataTransferManager.DataRequested += new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.OnDataRequested);
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            base.OnNavigatedFrom(e);

            // Unregister this as a share source.
            this.dataTransferManager.DataRequested -= new TypedEventHandler<DataTransferManager, DataRequestedEventArgs>(this.OnDataRequested);
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

        private void TsToggle_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["tsEnabled"] = TsToggle.IsChecked;
        }

        private void EpToggle_Click(object sender, Windows.UI.Xaml.RoutedEventArgs e)
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["epEnabled"] = EpToggle.IsChecked;
        }

        public void DoTimeUpdate()
        {
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            // Get current martian datetime.
            MartianDateTime mdt = new MartianDateTime();

            SolDate.Text = String.Format("{0:f6}", mdt.MartianSolDate);
            Time.Text = mdt.Time;
            Date.Text = mdt.Date;

            if ((roamingSettings.Values.ContainsKey("epEnabled")) &&
                ((bool)roamingSettings.Values["epEnabled"] == true))
                EpDate.Text = mdt.EclipsePhaseDate;

            if ((roamingSettings.Values.ContainsKey("tsEnabled")) &&
                ((bool)roamingSettings.Values["tsEnabled"] == true))
                TsDate.Text = mdt.TranshumanSpaceDate;

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

            ContentGrid.Background = new ImageBrush() { ImageSource = backgroundImage, Stretch = Stretch.UniformToFill, Opacity = 0.8};
        }

        private void OnVisualStateChanging(object sender, Windows.UI.Xaml.VisualStateChangedEventArgs e)
        {
            // Update the advertisement block to match the layout.
            switch (e.NewState.Name)
            {
                case "FullScreenLandscape":
                case "Filled":
                    adControl.AdUnitId = "10058667";
                    adControl.Refresh();
                    break;

                case "Snapped":
                    adControl.AdUnitId = "10058780";
                    adControl.Refresh();
                    break;

                case "FullScreenPortrait":
                    adControl.AdUnitId = "10058781";
                    adControl.Refresh();
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

            MartianDateTime mdt = new MartianDateTime();
            string share = String.Format("{0} AMT, {1}", mdt.Time, mdt.Date);

            requestData.Properties.Title = share;
            requestData.Properties.ApplicationName = "AresCal";
            // requestData.Properties.ApplicationListingUri
            requestData.SetText(share);   
        }
    }
}
