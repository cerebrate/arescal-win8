#region header

// AresCal - SettingsContent.xaml.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2013.  All rights reserved.
// 
// Licensed and made available under MS-PL: http://opensource.org/licenses/ms-pl .
// 
// Created: 2013-02-07 11:34 AM

#endregion

using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236

namespace ArkaneSystems.AresCal
{
    public sealed partial class SettingsContent : UserControl
    {
        public SettingsContent()
        {
            this.InitializeComponent();
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Restore value contained in app data.
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;

            if (roamingSettings.Values.ContainsKey("tsFreezeDuringGap"))
                this.tsFreezeDuringGap.IsOn = (bool) roamingSettings.Values["tsFreezeDuringGap"];
        }

        private void tsFreezeDuringGap_Toggled(object sender, RoutedEventArgs e)
        {
            // When setting is toggled, save it.
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["tsFreezeDuringGap"] = this.tsFreezeDuringGap.IsOn;
        }
    }
}
