// 
// lat - Preferences.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; Version 2 .
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program; if not, write to the Free Software
// Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
//
//
using System;

namespace lat
{
	// Taken from f-spot preferences
	public class Preferences
	{
		public const string MAIN_WINDOW_MAXIMIZED = "/apps/lat/ui/maximized";

		public const string MAIN_WINDOW_X = "/apps/lat/ui/main_window_x";
		public const string MAIN_WINDOW_Y = "/apps/lat/ui/main_window_y";
		public const string MAIN_WINDOW_WIDTH = "/apps/lat/ui/main_window_width";
		public const string MAIN_WINDOW_HEIGHT = "/apps/lat/ui/main_window_height";
		public const string MAIN_WINDOW_HPANED = "/apps/lat/ui/main_window_hpaned";

		static GConf.Client client;
		static GConf.NotifyEventHandler changed_handler;

		public static GConf.Client Client 
		{
			get {
				if (client == null) {
					client = new GConf.Client ();

					changed_handler = new GConf.NotifyEventHandler (OnSettingChanged);
					client.AddNotify ("/apps/lat", changed_handler);
				}
				return client;
			}
		}

		public static object GetDefault (string key)
		{
			switch (key) 
			{
				case MAIN_WINDOW_X:
				case MAIN_WINDOW_Y:
				case MAIN_WINDOW_HEIGHT:
				case MAIN_WINDOW_WIDTH:
				case MAIN_WINDOW_HPANED:
					return null;			
			}

			return null;
		}

		public static object Get (string key)
		{
			try {
				return Client.Get (key);
			} catch (GConf.NoSuchKeyException) {
				object default_val = GetDefault (key);

				if (default_val != null)
					Client.Set (key, default_val);

				return default_val;
			}
		}

		public static void Set (string key, object value)
		{
			Client.Set (key, value);
		}

		public static event GConf.NotifyEventHandler SettingChanged;

		static void OnSettingChanged (object sender, GConf.NotifyEventArgs args)
		{
			if (SettingChanged != null) {
				SettingChanged (sender, args);
			}
		}
	}
}
