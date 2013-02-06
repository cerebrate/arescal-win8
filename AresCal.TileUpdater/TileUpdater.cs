using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.ApplicationModel.Background;
using Windows.UI.Notifications;

namespace ArkaneSystems.AresCal.TileUpdater
{
    public sealed class TileUpdater : IBackgroundTask
    {
        public void Run(IBackgroundTaskInstance taskInstance)
        {
            var deferral = taskInstance.GetDeferral();

            // Set up scheduled notifications.
            TileUpdateScheduler.CreateScheduledUpdates();
            
            deferral.Complete();
        }
    }
}
