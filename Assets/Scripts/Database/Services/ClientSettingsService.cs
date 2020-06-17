/**
 * Copyright (c) 2020 LG Electronics, Inc.
 *
 * This software contains code licensed as described in LICENSE.
 *
 */

using PetaPoco;
using System;
using System.Collections.Generic;

namespace Simulator.Database.Services
{
    public class ClientSettingsService : IClientSettingsService
    {
        public ClientSettings GetOrMake()
        {
            using (var db = DatabaseManager.Open())
            {
                ClientSettings settings = db.FirstOrDefault<ClientSettings>("");
                if (settings == null)
                {
                    settings = new ClientSettings();
                    settings.simid = Guid.NewGuid().ToString();
                    settings.onlineStatus = true;
                    db.Insert(settings);
                }

                return settings;
            }
        }

        public void SetSimID(string simid)
        {
            using (var db = DatabaseManager.Open())
            {
                ClientSettings settings = db.FirstOrDefault<ClientSettings>("");
                if (settings == null)
                {
                    settings = new ClientSettings();
                    settings.simid = simid;
                    settings.onlineStatus = true;
                    db.Insert(settings);
                }
                else
                {
                    settings.simid = simid;
                    db.Update(settings);
                }
            }
        }

        public void UpdateOnlineStatus(bool online)
        {
            using (var db = DatabaseManager.Open())
            {
                ClientSettings settings = db.FirstOrDefault<ClientSettings>("");
                settings.onlineStatus = online;
                db.Update(settings);
            }
        }
    }
}
