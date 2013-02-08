#region header

// AresCal.TileUpdater - TileUpdateScheduler.cs
// 
// Alistair J. R. Young
// Arkane Systems
// 
// Copyright Arkane Systems 2012-2013.  All rights reserved.
// 
// Licensed and made available under MS-PL: http://opensource.org/licenses/ms-pl .
// 
// Created: 2013-02-06 1:43 PM

#endregion

using System;
using System.Collections.Generic;
using System.Linq;

using Windows.Data.Xml.Dom;
using Windows.UI.Notifications;

namespace ArkaneSystems.AresCal.TileUpdater
{
    public static class TileUpdateScheduler
    {
        public static void CreateScheduledUpdates()
        {
            // Get the tile updater.
            Windows.UI.Notifications.TileUpdater updater = TileUpdateManager.CreateTileUpdaterForApplication();

            // Get current planned updates.
            IReadOnlyList<ScheduledTileNotification> plannedUpdates = updater.GetScheduledTileNotifications();

            // Compute parameters for new updates to queue.
            // Starting either now or at end of planned updates --
            DateTime now = DateTime.Now;

            DateTime updateTime = now.AddMinutes(1);

            if (plannedUpdates.Count > 0)
                updateTime = plannedUpdates.Select(x => x.DeliveryTime.DateTime).Max();

            // -- and ending four hours hence (BT runs every three hours).
            DateTime planTill = now.AddHours(3);

            // Perform immediate update.
            XmlDocument immediateTile = MakeTile(now);

            updater.Update(new TileNotification(immediateTile) {ExpirationTime = now.AddMinutes(1)});

            for (DateTime startPlanning = updateTime;
                 startPlanning < planTill;
                 startPlanning = startPlanning.AddMinutes(1))
            {
                XmlDocument tile = MakeTile(startPlanning);

                var scheduled = new ScheduledTileNotification(tile, new DateTimeOffset(startPlanning))
                    {ExpirationTime = startPlanning.AddMinutes(1)};
                updater.AddToSchedule(scheduled);
            }
        }

        private static XmlDocument MakeTile(DateTime forTime)
        {
            var mdt = new MartianDateTime(forTime);

            XmlDocument tile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileSquareText04);
            tile.GetElementsByTagName("text")[0].InnerText = String.Format("{0} AMT", mdt.ShortTime);

            XmlDocument wideTile = TileUpdateManager.GetTemplateContent(TileTemplateType.TileWideText09);
            wideTile.GetElementsByTagName("text")[0].InnerText = String.Format("{0} AMT", mdt.ShortTime);
            wideTile.GetElementsByTagName("text")[1].InnerText = mdt.Date;

            // Mix the payloads.
            IXmlNode node = tile.ImportNode(wideTile.GetElementsByTagName("binding")[0], true);
            tile.GetElementsByTagName("visual")[0].AppendChild(node);

            return tile;
        }
    }
}
