#region header

// AresCal - App.xaml.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2013.  All rights reserved.
// 
// Licensed and made available under MS-PL: http://opensource.org/licenses/ms-pl .
// 
// Created: 2013-02-04 1:59 PM

#endregion

using System;

using ArkaneSystems.AresCal.Common;

using Windows.ApplicationModel;
using Windows.ApplicationModel.Activation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

// The Blank Application template is documented at http://go.microsoft.com/fwlink/?LinkId=234227

namespace ArkaneSystems.AresCal
{
    /// <summary>
    ///     Provides application-specific behavior to supplement the default Application class.
    /// </summary>
    sealed partial class App : Application
    {
        private DispatcherTimer tickTimer;

        /// <summary>
        ///     Initializes the singleton application object.  This is the first line of authored code
        ///     executed, and as such is the logical equivalent of main() or WinMain().
        /// </summary>
        public App()
        {
            this.InitializeComponent();
            Suspending += this.OnSuspending;
            Resuming += this.OnResuming;
        }

        /// <summary>
        ///     Invoked when the application is launched normally by the end user.  Other entry points
        ///     will be used when the application is launched to open a specific file, to display
        ///     search results, and so forth.
        /// </summary>
        /// <param name="args">Details about the launch request and process.</param>
        protected override async void OnLaunched(LaunchActivatedEventArgs args)
        {
            var rootFrame = Window.Current.Content as Frame;

            // Do not repeat app initialization when the Window already has content,
            // just ensure that the window is active
            if (rootFrame == null)
            {
                // Create a Frame to act as the navigation context and navigate to the first page
                rootFrame = new Frame();

                if (args.PreviousExecutionState == ApplicationExecutionState.Terminated)
                {
                    await SuspensionManager.RestoreAsync();
                }

                // Place the frame in the current Window
                Window.Current.Content = rootFrame;
            }

            if (rootFrame.Content == null)
            {
                // When the navigation stack isn't restored navigate to the first page,
                // configuring the new page by passing required information as a navigation
                // parameter
                if (!rootFrame.Navigate(typeof (MainPage), args.Arguments))
                {
                    throw new Exception("Failed to create initial page");
                }
            }

            // Ensure the current window is active
            Window.Current.Activate();

            // Set up and start the tick timer.
            this.tickTimer = new DispatcherTimer();
            this.tickTimer.Interval = new TimeSpan(0, 0, 1);
            this.tickTimer.Tick += this.tickTimer_Tick;
            this.tickTimer.Start();

            // Force immediate tick.
            this.tickTimer_Tick(this, null);
        }

        private void tickTimer_Tick(object sender, object e)
        {
            // Pass the tick along to the main page, if present, to update itself.
            Frame frame;

            if ((frame = Window.Current.Content as Frame) != null)
            {
                MainPage mp;

                if ((mp = frame.Content as MainPage) != null)
                {
                    mp.DoTimeUpdate();
                }
            }
        }

        /// <summary>
        ///     Invoked when application execution is being suspended.  Application state is saved
        ///     without knowing whether the application will be terminated or resumed with the contents
        ///     of memory still intact.
        /// </summary>
        /// <param name="sender">The source of the suspend request.</param>
        /// <param name="e">Details about the suspend request.</param>
        private async void OnSuspending(object sender, SuspendingEventArgs e)
        {
            // Stop the tick timer.
            this.tickTimer.Stop();

            SuspendingDeferral deferral = e.SuspendingOperation.GetDeferral();

            // Save application state and stop any background activity
            await SuspensionManager.SaveAsync();

            deferral.Complete();
        }

        private void OnResuming(object sender, object e)
        {
            // Restart the tick timer.
            this.tickTimer.Start();
        }
    }
}
