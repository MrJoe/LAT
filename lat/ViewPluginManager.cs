// 
// lat - ViewManager.cs
// Author: Loren Bandiera
// Copyright 2006 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; Version 2 
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
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters;
using System.Runtime.Serialization.Formatters.Binary;
using Gtk;
using Novell.Directory.Ldap;

namespace lat {

	[Serializable]
	public struct ViewPluginConfig
	{
		public string[] ColumnNames;
		public string[] ColumnAttributes;
		public string DefaultNewContainer;
		public string Filter;
		public string SearchBase;
	}

	public abstract class ViewPlugin
	{
		protected ViewPluginConfig config;
		
		public ViewPlugin ()
		{
		}
	
		// Methods
		public void Deserialize (string stateFileName)
		{
			try {

				Stream stream = File.OpenRead (stateFileName);

				IFormatter formatter = new BinaryFormatter();
				this.config = (ViewPluginConfig) formatter.Deserialize (stream);
				stream.Close ();

			} catch (Exception e) {

				Logger.Log.Debug ("ViewPlugin.Deserialize: {0}", e.Message);
			}
		}
		
		public void Serialize (string stateFileName)
		{
			try {

				Stream stream = File.OpenWrite (stateFileName);
			
				IFormatter formatter = new BinaryFormatter ();
				formatter.Serialize (stream, this.config); 
				stream.Close ();

			} catch (Exception e) {

				Logger.Log.Debug ("ViewPlugin.Serialize: {0}", e.Message);
			}
		}
		
		public abstract void Init ();
		public abstract void OnAddEntry (LdapServer server);
		public abstract void OnEditEntry (LdapServer server, LdapEntry le);
		public abstract void OnPopupShow (Menu popup);
			
		// Properties		
		public string[] ColumnAttributes 
		{ 
			get { return config.ColumnAttributes; }
			set { config.ColumnAttributes = value; }
		}
		
		public string[] ColumnNames 
		{
			get { return config.ColumnNames; }
			set { config.ColumnNames = value; } 
		}
		
		public string DefaultNewContainer
		{
			get { return config.DefaultNewContainer; }
			set { config.DefaultNewContainer = value; }
		}
		
		public string Filter
		{
			get { return config.Filter; }
			set { config.Filter = value; }
		}

		public string SearchBase
		{
			get { return config.SearchBase; }
			set { config.SearchBase = value; }
		}
		
		public abstract string[] Authors { get; }		
		public abstract string Copyright { get; }
		public abstract string Description { get; }		
		public abstract string Name { get; }
		public abstract Gdk.Pixbuf Icon { get; }		
	}
	
	public class ViewPluginManager
	{
//		string pluginDirectory;
		string pluginStateDirectory;
		ArrayList pluginList;
	
		public ViewPluginManager (string directory)
		{
//			pluginDirectory = directory;			
			pluginList = new ArrayList ();
			
			string homeDir = Path.Combine (Environment.GetEnvironmentVariable("HOME"), ".lat");
			DirectoryInfo di = new DirectoryInfo (homeDir);
			if (!di.Exists)
				di.Create ();
				
			pluginStateDirectory = homeDir;
		}

		public ViewPlugin Find (string name)
		{
			foreach (ViewPlugin vp in pluginList)
				if (vp.Name == name)
						return vp;
						
			return null;
		}

		public void LoadPlugins ()
		{
//			pluginList.Add (new PosixUserViewPlugin ());
//			pluginList.Add (new PosixGroupViewPlugin ());
//			pluginList.Add (new PosixContactsViewPlugin ());
//			pluginList.Add (new PosixComputerViewPlugin ());
//			pluginList.Add (new ActiveDirectoryUserViewPlugin ());
//			pluginList.Add (new ActiveDirectoryGroupViewPlugin ());
//			pluginList.Add (new ActiveDirectoryContactsViewPlugin ());
//			pluginList.Add (new ActiveDirectoryComputerViewPlugin ());
			
			foreach (ViewPlugin vp in pluginList) {
				string fileName = vp.GetType() + ".state";
				vp.Deserialize (Path.Combine (pluginStateDirectory, fileName));
			}
		}

		public void SavePluginsState ()
		{
			foreach (ViewPlugin vp in pluginList) {
				string fileName = vp.GetType() + ".state";
				vp.Serialize (Path.Combine (pluginStateDirectory, fileName));
			}
		}

		public ViewPlugin[] Plugins
		{
			get { return (ViewPlugin[]) pluginList.ToArray (typeof (ViewPlugin)); }
		}		
	}
	
	public class PluginManagerDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog pluginManagerDialog;
		[Glade.Widget] Gtk.Entry colNamesEntry;
		[Glade.Widget] Gtk.Entry colAttrsEntry;
		[Glade.Widget] Gtk.Entry filterEntry;
		[Glade.Widget] Gtk.Button newContainerButton;
		[Glade.Widget] Gtk.Button searchBaseButton;
		[Glade.Widget] TreeView pluginTreeView; 

		ListStore pluginStore;
		LdapServer server;
		Gtk.Window parent;

		string lastSelected;
	
		public PluginManagerDialog (LdapServer ldapServer, Gtk.Window parentWindow)
		{
			server = ldapServer;
			parent = parentWindow;
			
			ui = new Glade.XML (null, "lat.glade", "pluginManagerDialog", null);
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
			
			pluginTreeView.Selection.Changed += OnSelectionChanged;
			
			pluginManagerDialog.Icon = Global.latIcon;
			pluginManagerDialog.Run ();
			pluginManagerDialog.Destroy ();
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

		void OnSelectionChanged (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (pluginTreeView.Selection.GetSelected (out model, out iter))  {
				
				if (lastSelected != null) {
					ViewPlugin p = Global.viewPluginManager.Find (lastSelected);
					
					p.Filter = filterEntry.Text;
					
					if (newContainerButton.Label != "")
						p.DefaultNewContainer = newContainerButton.Label;

					if (searchBaseButton.Label != "")
						p.SearchBase = searchBaseButton.Label;					
				}
			
				string name = (string) model.GetValue (iter, 1);
				lastSelected = name;
				
				ViewPlugin vp = Global.viewPluginManager.Find (name);
				if (vp != null) {
					colNamesEntry.Text = vp.ColumnNames.ToString ();
					colAttrsEntry.Text = vp.ColumnAttributes.ToString ();
					filterEntry.Text = vp.Filter;
					
					if (vp.DefaultNewContainer != null)
						newContainerButton.Label = vp.DefaultNewContainer;

					if (vp.SearchBase != null)
						searchBaseButton.Label = vp.SearchBase;					
				}
			}
		}

		public void OnFilterBuildClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			filterEntry.Text = sbd.UserFilter;
		}

		public void OnNewContainerClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = 
				new SelectContainerDialog (server, parent);

			scd.Message = String.Format (
				Mono.Unix.Catalog.GetString ("Select a container for new objects"));

			scd.Title = Mono.Unix.Catalog.GetString ("Select container");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (server.Host))
				newContainerButton.Label = scd.DN;
		}
		
		public void OnSearchBaseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = 
				new SelectContainerDialog (server, parent);

			scd.Message = String.Format (
				Mono.Unix.Catalog.GetString ("Select a search base"));

			scd.Title = Mono.Unix.Catalog.GetString ("Select container");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (server.Host))
				searchBaseButton.Label = scd.DN;
		}
		
		public void OnCloseClicked (object o, EventArgs args)
		{
			if (lastSelected != null) {
				ViewPlugin p = Global.viewPluginManager.Find (lastSelected);

				p.Filter = filterEntry.Text;
				
				if (newContainerButton.Label != "")
					p.DefaultNewContainer = newContainerButton.Label;

				if (searchBaseButton.Label != "")
					p.SearchBase = searchBaseButton.Label; 				
			}		
		}
	}
}
