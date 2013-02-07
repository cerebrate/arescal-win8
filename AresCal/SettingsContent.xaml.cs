using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

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
                tsFreezeDuringGap.IsOn = (bool)roamingSettings.Values["tsFreezeDuringGap"];
        }
        
        private void tsFreezeDuringGap_Toggled(object sender, RoutedEventArgs e)
        {
            // When setting is toggled, save it.
            ApplicationDataContainer roamingSettings = ApplicationData.Current.RoamingSettings;
            roamingSettings.Values["tsFreezeDuringGap"] = tsFreezeDuringGap.IsOn;
        }
    }
}
