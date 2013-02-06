using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.UI.Notifications;

namespace ArkaneSystems.AresCal.TileUpdater
{
    public static class TileUpdateScheduler
    {
        public static void CreateScheduledUpdates()
        {
            // Get the tile updater.
            var updater = TileUpdateManager.CreateTileUpdaterForApplication();

            // Get current planned updates.
            var plannedUpdates = updater.GetScheduledTileNotifications();

            // Compute parameters for new updates to queue.
            // Starting either now or at end of planned updates --
            DateTime now = DateTime.Now;

            DateTime updateTime = now.AddMinutes(1);

            if (plannedUpdates.Count > 0)
                updateTime = plannedUpdates.Select(x => x.DeliveryTime.DateTime).Max();

            // -- and ending four hours hence (BT runs every three hours).
            DateTime planTill = now.AddHours(3);

            // Perform immediate update.
            var immediateTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareText04);
            immediateTile.GetElementsByTagName("text")[0].InnerText = now.ToString(@"hh:mm A\MT");

            updater.Update(new TileNotification(immediateTile) { ExpirationTime = now.AddMinutes(1) } );

            for (var startPlanning = updateTime; startPlanning < planTill; startPlanning = startPlanning.AddMinutes(1))
            {
                var tile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareText04);
                tile.GetElementsByTagName("text")[0].InnerText = startPlanning.ToString(@"hh:mm A\MT");

                ScheduledTileNotification scheduled = new ScheduledTileNotification(tile, new DateTimeOffset(startPlanning))
                    {ExpirationTime = startPlanning.AddMinutes(1)};
                updater.AddToSchedule(scheduled);
            }
        }
    }
}
