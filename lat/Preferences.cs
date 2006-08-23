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
using System.Collections.Generic;
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
		[Glade.Widget] TreeView attrViewPluginTreeView;

		ListStore pluginStore;
		ListStore attrPluginStore;
		Connection conn;
			
		public PreferencesDialog (Connection connection)
		{
			conn = connection;
		
			ui = new Glade.XML (null, "lat.glade", "preferencesDialog", null);
			ui.Autoconnect (this);

			pluginStore = new ListStore (typeof (bool), typeof (string));

			CellRendererToggle crt = new CellRendererToggle();
			crt.Activatable = true;
			crt.Toggled += OnClassToggled;

			pluginTreeView.AppendColumn ("Enabled", crt, "active", 0);
			pluginTreeView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			
			pluginTreeView.Model = pluginStore;

			if (conn.Settings.Name != null) {
				foreach (ViewPlugin vp in Global.Plugins.ServerViewPlugins) {
					if (conn.ServerViews.Contains (vp.GetType().ToString()))
						pluginStore.AppendValues (true, vp.Name);
					else
						pluginStore.AppendValues (false, vp.Name);
				}
			}

			attrPluginStore = new ListStore (typeof (bool), typeof (string));

			crt = new CellRendererToggle();
			crt.Activatable = true;
			crt.Toggled += OnAttributeViewerToggled;
			
			attrViewPluginTreeView.AppendColumn ("Enabled", crt, "active", 0);
			attrViewPluginTreeView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			
			attrViewPluginTreeView.Model = attrPluginStore;

			if (conn.Settings.Name != null) {	
			
				if (conn.AttributeViewers.Count == 0)
					conn.SetDefaultAttributeViewers ();
					
				foreach (AttributeViewPlugin avp in Global.Plugins.AttributeViewPlugins) {
					if (conn.AttributeViewers.Contains (avp.GetType().ToString()))
						attrPluginStore.AppendValues (true, avp.Name);
					else
						attrPluginStore.AppendValues (false, avp.Name);
				}
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
		
		public void OnAttrAboutClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (attrViewPluginTreeView.Selection.GetSelected (out model, out iter)) {
							
				string name = (string) attrPluginStore.GetValue (iter, 1);
				AttributeViewPlugin vp = Global.Plugins.FindAttributeView (name);
				
				if (vp != null) {
					Gtk.AboutDialog ab = new Gtk.AboutDialog ();
					ab.Authors = vp.Authors;
					ab.Comments = vp.Description;
					ab.Copyright = vp.Copyright;
					ab.Name = vp.Name;
					ab.Version = vp.Version;

					ab.Run ();
					ab.Destroy ();
				}
			}		
		}		
		
		public void OnAboutClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (pluginTreeView.Selection.GetSelected (out model, out iter)) {
							
				string name = (string) pluginStore.GetValue (iter, 1);
				ViewPlugin vp = Global.Plugins.FindServerView (name);
				
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
			TreeModel model;
			TreeIter iter;
		
			if (pluginTreeView.Selection.GetSelected (out model, out iter)) {	
				string name = (string) pluginStore.GetValue (iter, 1);		
				new PluginConfigureDialog (conn, name);
			}
		}

		void OnAttributeViewerToggled (object o, ToggledArgs args)
		{			
			TreeIter iter;

			if (attrPluginStore.GetIter (out iter, new TreePath(args.Path))) {
			
				bool old = (bool) attrPluginStore.GetValue (iter,0);
				
				string name = (string) attrPluginStore.GetValue (iter, 1);				
				AttributeViewPlugin vp = Global.Plugins.FindAttributeView (name);
				
				if (!conn.AttributeViewers.Contains (vp.GetType().ToString()))
					conn.AttributeViewers.Add (vp.GetType().ToString());
				else
					conn.AttributeViewers.Remove (vp.GetType().ToString());
				
				Global.Connections [conn.Settings.Name] = conn;
				
				attrPluginStore.SetValue(iter,0,!old);
			}
		}
		
		void OnClassToggled (object o, ToggledArgs args)
		{			
			TreeIter iter;

			if (pluginStore.GetIter (out iter, new TreePath(args.Path))) {
			
				bool old = (bool) pluginStore.GetValue (iter,0);
				string name = (string) pluginStore.GetValue (iter, 1);
				
				ViewPlugin vp = Global.Plugins.FindServerView (name);
				
				if (!conn.ServerViews.Contains (vp.GetType().ToString()))
					conn.ServerViews.Add (vp.GetType().ToString());
				else
					conn.ServerViews.Remove (vp.GetType().ToString());
				
				Global.Connections [conn.Settings.Name] = conn;				
				pluginStore.SetValue(iter,0,!old);
			}
		}
	}
	
	public class PluginConfigureDialog
	{
		Glade.XML ui;
		
		[Glade.Widget] Gtk.Dialog pluginConfigureDialog;
		[Glade.Widget] Gtk.Entry filterEntry;
		[Glade.Widget] Gtk.Button newContainerButton;
		[Glade.Widget] Gtk.Button searchBaseButton;
		[Glade.Widget] TreeView columnsTreeView; 

		ListStore columnStore;
		Connection conn;
		ViewPlugin vp;
		List<string> colNames;
		List<string> colAttrs;
			
		public PluginConfigureDialog (Connection connection, string pluginName)
		{
			conn = connection;
			colNames = new List<string> ();
			colAttrs = new List<string> ();
		
			ui = new Glade.XML (null, "lat.glade", "pluginConfigureDialog", null);
			ui.Autoconnect (this);

			columnStore = new ListStore (typeof (string), typeof (string));
			
			CellRendererText cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnNameEdit);			
			columnsTreeView.AppendColumn ("Name", cell, "text", 0);
			
			cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnAttributeEdit);			
			columnsTreeView.AppendColumn ("Attribute", cell, "text", 1);
			
			columnsTreeView.Model = columnStore;
			
			vp = Global.Plugins.FindServerView (pluginName);
			if (vp != null) {			
				for (int i = 0; i < vp.ColumnNames.Length; i++) {  
					columnStore.AppendValues (vp.ColumnNames[i], vp.ColumnAttributes[i]);
					colNames.Add (vp.ColumnNames[i]);
					colAttrs.Add (vp.ColumnAttributes[i]);
				}
					
				filterEntry.Text = vp.Filter;
					
				if (vp.DefaultNewContainer != "")
					newContainerButton.Label = vp.DefaultNewContainer;

				if (vp.SearchBase != "")
					searchBaseButton.Label = vp.SearchBase;					
			}
										
			pluginConfigureDialog.Icon = Global.latIcon;
			pluginConfigureDialog.Resize (300, 400);
			pluginConfigureDialog.Run ();
			pluginConfigureDialog.Destroy ();
		}

		void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!columnStore.GetIterFromString (out iter, args.Path))
				return;
				
			string oldAttr = (string) columnStore.GetValue (iter, 1);		
			colAttrs.Remove (oldAttr);
			colAttrs.Add (args.NewText);
			
			columnStore.SetValue (iter, 1, args.NewText);
		}
		
		void OnNameEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!columnStore.GetIterFromString (out iter, args.Path))
				return;
				
			string oldName = (string) columnStore.GetValue (iter, 0);		
			colNames.Remove (oldName);	
			colNames.Add (args.NewText);
			
			columnStore.SetValue (iter, 0, args.NewText);
		}
		
		public void OnOkClicked (object o, EventArgs args)
		{
			vp.ColumnNames = colNames.ToArray ();
			vp.ColumnAttributes = colAttrs.ToArray ();
		
			vp.Filter = filterEntry.Text;
					
			if (newContainerButton.Label != "")
				vp.DefaultNewContainer = newContainerButton.Label;

			if (searchBaseButton.Label != "")
				vp.SearchBase = searchBaseButton.Label;		
		}
		
		public void OnAddClicked (object o, EventArgs args)
		{
			columnStore.AppendValues ("Untitiled", "Unknown");
		}
		
		public void OnRemoveClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (columnsTreeView.Selection.GetSelected (out model, out iter)) {
				string name = (string) columnStore.GetValue (iter, 0);
				string attr = (string) columnStore.GetValue (iter, 1);
				colNames.Remove (name);
				colAttrs.Remove (attr);
				columnStore.Remove (ref iter);
			}
		}
		
		public void OnFilterBuildClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			filterEntry.Text = sbd.UserFilter;
		}

		public void OnNewContainerClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = new SelectContainerDialog (conn, null);
			scd.Message = String.Format (Mono.Unix.Catalog.GetString ("Select a container for new objects"));
			scd.Title = Mono.Unix.Catalog.GetString ("Select container");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (conn.Settings.Host))
				newContainerButton.Label = scd.DN;
		}
		
		public void OnSearchBaseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = new SelectContainerDialog (conn, null);
			scd.Message = String.Format (Mono.Unix.Catalog.GetString ("Select a search base"));
			scd.Title = Mono.Unix.Catalog.GetString ("Select container");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (conn.Settings.Host))
				searchBaseButton.Label = scd.DN;
		}
	}
}
