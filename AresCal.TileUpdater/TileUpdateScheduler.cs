using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using Windows.Data.Xml.Dom;
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
            var immediateTile = MakeTile(now);

            updater.Update(new TileNotification(immediateTile) { ExpirationTime = now.AddMinutes(1) } );

            for (var startPlanning = updateTime; startPlanning < planTill; startPlanning = startPlanning.AddMinutes(1))
            {
                var tile = MakeTile(startPlanning);

                ScheduledTileNotification scheduled = new ScheduledTileNotification(tile, new DateTimeOffset(startPlanning))
                    {ExpirationTime = startPlanning.AddMinutes(1)};
                updater.AddToSchedule(scheduled);
            }
        }

        private static XmlDocument MakeTile (DateTime forTime)
        {
            MartianDateTime mdt = new MartianDateTime(forTime);

            var tile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareText04);
            tile.GetElementsByTagName("text")[0].InnerText = String.Format("{0} AMT", mdt.ShortTime);

            var wideTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWideText09);
            wideTile.GetElementsByTagName("text")[0].InnerText = String.Format("{0} AMT", mdt.ShortTime);
            wideTile.GetElementsByTagName("text")[1].InnerText = mdt.Date;

            // Mix the payloads.
            var node = tile.ImportNode(wideTile.GetElementsByTagName("binding")[0], true);
            tile.GetElementsByTagName("visual")[0].AppendChild(node);

            return tile;
        }
    }
}
