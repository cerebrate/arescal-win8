#region header

// AresCal.TileUpdater - TileUpdater.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2013.  All rights reserved.
// 
// Licensed and made available under MS-PL: http://opensource.org/licenses/ms-pl .
// 
// Created: 2013-02-04 2:27 PM

#endregion

using Windows.ApplicationModel.Background;

namespace ArkaneSystems.AresCal.TileUpdater
{
    public sealed class TileUpdater : IBackgroundTask
    {
        #region IBackgroundTask Members

        public void Run(IBackgroundTaskInstance taskInstance)
        {
            BackgroundTaskDeferral deferral = taskInstance.GetDeferral();

            // Set up scheduled notifications.
            TileUpdateScheduler.CreateScheduledUpdates();

            deferral.Complete();
        }

        #endregion
    }
}
