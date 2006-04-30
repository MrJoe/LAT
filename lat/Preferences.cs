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
using Gtk;

namespace lat
{
	public class Preferences
	{
		public const string MAIN_WINDOW_MAXIMIZED = "/apps/lat/ui/maximized";

		public const string MAIN_WINDOW_X = "/apps/lat/ui/main_window_x";
		public const string MAIN_WINDOW_Y = "/apps/lat/ui/main_window_y";
		public const string MAIN_WINDOW_WIDTH = "/apps/lat/ui/main_window_width";
		public const string MAIN_WINDOW_HEIGHT = "/apps/lat/ui/main_window_height";
		public const string MAIN_WINDOW_HPANED = "/apps/lat/ui/main_window_hpaned";

		public const string BROWSER_SELECTION = "/apps/lat/ui/browser_selection";
		public const string DISPLAY_VERBOSE_MESSAGES = "/apps/lat/ui/display_verbose_messages";

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
					
				case BROWSER_SELECTION:
					return 2;
					
				case DISPLAY_VERBOSE_MESSAGES:
					return true;
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
	
	public class PreferencesDialog
	{
		Glade.XML ui;
		
		[Glade.Widget] Gtk.Dialog preferencesDialog;
		[Glade.Widget] RadioButton browserSingleClickButton;
		[Glade.Widget] RadioButton browserDoubleClickButton;
		[Glade.Widget] CheckButton verboseMessagesButton;
		[Glade.Widget] TreeView pluginTreeView; 

		ListStore pluginStore;
		LdapServer server;
			
		public PreferencesDialog (LdapServer ldapServer)
		{
			server = ldapServer;
		
			ui = new Glade.XML (null, "lat.glade", "preferencesDialog", null);
			ui.Autoconnect (this);

			pluginStore = new ListStore (typeof (bool), typeof (string));

			CellRendererToggle crt = new CellRendererToggle();
			crt.Activatable = true;
			crt.Toggled += OnClassToggled;

			pluginTreeView.AppendColumn ("Enabled", crt, "active", 0);
			pluginTreeView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			
			pluginTreeView.Model = pluginStore;
			
			foreach (ViewPlugin vp in Global.viewPluginManager.Plugins) {
				pluginStore.AppendValues (true, vp.Name);
			}
					
			LoadPreference (Preferences.BROWSER_SELECTION);
			LoadPreference (Preferences.DISPLAY_VERBOSE_MESSAGES);
					
			preferencesDialog.Icon = Global.latIcon;
			preferencesDialog.Resize (300, 400);
			preferencesDialog.Run ();
			preferencesDialog.Destroy ();
		}

		void LoadPreference (String key)
		{
			object val = Preferences.Get (key);

			if (val == null)
				return;
			
			switch (key) {
				
			case Preferences.BROWSER_SELECTION:
				int b = (int) val;
				if (b == 1)
					browserSingleClickButton.Active = true;
				else if (b == 2)
					browserDoubleClickButton.Active = true;
					
				break;
				
			case Preferences.DISPLAY_VERBOSE_MESSAGES:
				verboseMessagesButton.Active = (bool) val;
				break;
			}
		}
				
		public void OnDoubleClickToggled (object o, EventArgs args)
		{
			if (browserSingleClickButton.Active)
				Preferences.Set (Preferences.BROWSER_SELECTION, 1);
			else
				Preferences.Set (Preferences.BROWSER_SELECTION, 2);
		}

					
		public void OnVerboseToggled (object o, EventArgs args)
		{
			Preferences.Set (Preferences.DISPLAY_VERBOSE_MESSAGES, verboseMessagesButton.Active);
		}
		
		public void OnAboutClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (pluginTreeView.Selection.GetSelected (out model, out iter)) {
							
				string name = (string) pluginStore.GetValue (iter, 1);
				ViewPlugin vp = Global.viewPluginManager.Find (name);
				
				if (vp != null) {
					Gtk.AboutDialog ab = new Gtk.AboutDialog ();
					ab.Authors = vp.Authors;
					ab.Comments = vp.Description;
					ab.Copyright = vp.Copyright;
					ab.Name = vp.Name;
					ab.Version = vp.Version;
					ab.Icon = vp.Icon;

					ab.Run ();
					ab.Destroy ();
				}
			}
		}
		
		public void OnConfigureClicked (object o, EventArgs args)
		{
		}
		
		void OnClassToggled (object o, ToggledArgs args)
		{
			TreeIter iter;

			if (pluginStore.GetIter (out iter, new TreePath(args.Path))) {
				bool old = (bool) pluginStore.GetValue (iter,0);
//				string name = (string) pluginStore.GetValue (iter, 1);

//				if (!old)
//					objectClasses.Add (name);
//				else
//					objectClasses.Remove (name);

				pluginStore.SetValue(iter,0,!old);
			}
		}
	}
}
