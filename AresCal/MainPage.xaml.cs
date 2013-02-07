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

using Windows.ApplicationModel.Background;
using Windows.Storage;
using Windows.UI.Xaml.Controls;
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

        public MainPage()
        {
            this.InitializeComponent();
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
        }

        /// <summary>
        ///     Preserves state associated with this page in case the application is suspended or the
        ///     page is discarded from the navigation cache.  Values must conform to the serialization
        ///     requirements of <see cref="SuspensionManager.SessionState" />.
        /// </summary>
        /// <param name="pageState">An empty dictionary to be populated with serializable state.</param>
        protected override void SaveState(Dictionary<String, Object> pageState)
        {}

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            TileUpdateScheduler.CreateScheduledUpdates();

            await this.CreateTileUpdaterTask();
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
    }
}
