﻿using System.IO;
using Intersect.Config;
using IntersectClientExtras.Database;
using Microsoft.Win32;

namespace Intersect_Client.Classes.Bridges_and_Interfaces.SFML.Database
{
    public class MonoDatabase : GameDatabase
    {
        public override void SavePreference(string key, string value)
        {
            RegistryKey regkey = Registry.CurrentUser.OpenSubKey("Software", true);

            regkey.CreateSubKey("IntersectClient");
            regkey = regkey.OpenSubKey("IntersectClient", true);
            regkey.CreateSubKey(ClientOptions.ServerHost + ":" + ClientOptions.ServerPort);
            regkey = regkey.OpenSubKey(ClientOptions.ServerHost + ":" + ClientOptions.ServerPort, true);
            regkey.SetValue(key, value);
        }

        public override string LoadPreference(string key)
        {
            RegistryKey regkey = Registry.CurrentUser.OpenSubKey("Software", false);
            regkey = regkey.OpenSubKey("IntersectClient", false);
            if (regkey == null)
            {
                return "";
            }
            regkey = regkey.OpenSubKey(ClientOptions.ServerHost + ":" + ClientOptions.ServerPort);
            if (regkey == null)
            {
                return "";
            }
            string value = (string) regkey.GetValue(key);
            if (string.IsNullOrEmpty(value))
            {
                return "";
            }
            return value;
        }

        public override bool LoadConfig()
        {
            if (!File.Exists(Path.Combine("resources", "config.json")))
            {
                ClientOptions.Load(null);
                File.WriteAllText(Path.Combine("resources", "config.json"), ClientOptions.GetJson());
                return true;
            }
            else
            {
                string jsonData = File.ReadAllText(Path.Combine("resources", "config.json"));
                ClientOptions.Load(jsonData);
                return true;
            }
        }
    }
}