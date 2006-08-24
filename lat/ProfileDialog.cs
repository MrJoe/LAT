// 
// lat - ProfileDialog.cs
// Author: Loren Bandiera
// Copyright 2005-2006 MMG Security, Inc.
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

using Gtk;
using System;

namespace lat
{

	public class ProfileDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog profileDialog;
		[Glade.Widget] Gtk.Entry profileNameEntry;
		[Glade.Widget] Gtk.Entry hostEntry;
		[Glade.Widget] Gtk.Entry portEntry;
		[Glade.Widget] Gtk.Entry ldapBaseEntry;
		[Glade.Widget] Gtk.Entry userEntry;
		[Glade.Widget] Gtk.Entry passEntry;
		[Glade.Widget] Gtk.CheckButton savePasswordButton;
		[Glade.Widget] Gtk.RadioButton tlsRadioButton;
		[Glade.Widget] Gtk.RadioButton sslRadioButton;
		[Glade.Widget] Gtk.RadioButton noEncryptionRadioButton;
		[Glade.Widget] Gtk.HBox stHBox;
		[Glade.Widget] Gtk.Image image7;
		
		Connection conn;
		ComboBox serverTypeComboBox;
		EncryptionType encryption = EncryptionType.None;
		bool isEdit = false;
		string oldName = null;	

		[Glade.Widget] TreeView pluginTreeView;
		[Glade.Widget] TreeView attrViewPluginTreeView;

		ListStore pluginStore;
		ListStore attrPluginStore;

		public ProfileDialog ()
		{
			conn = new Connection (new ConnectionData());
		
			Init ();

			portEntry.Text = "389";

			profileDialog.Run ();
			profileDialog.Destroy ();
		}
		
		public ProfileDialog (Connection conn)
		{
			this.conn = conn;
		
			Init ();
						
			oldName = conn.Settings.Name;

			profileNameEntry.Text = conn.Settings.Name;
			hostEntry.Text = conn.Settings.Host;
			portEntry.Text = conn.Settings.Port.ToString();
			ldapBaseEntry.Text = conn.Settings.DirectoryRoot;
			
			userEntry.Text = conn.Settings.UserName;

			if (conn.Settings.SavePassword)
				savePasswordButton.Active =  true;
			else
				passEntry.Text = conn.Settings.Pass;

			switch (conn.Settings.Encryption) {
	
			case EncryptionType.TLS:
				tlsRadioButton.Active = true;
				break;

			case EncryptionType.SSL:
				sslRadioButton.Active = true;
				break;

			case EncryptionType.None:
				noEncryptionRadioButton.Active = true;
				break;
			}
				
			comboSetActive (serverTypeComboBox, Util.GetServerType (conn.Settings.ServerType));

			isEdit = true;

			profileDialog.Run ();
			profileDialog.Destroy ();
		}
			
		void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "profileDialog", null);
			ui.Autoconnect (this);		

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server-48x48.png");
			image7.Pixbuf = pb;
			
			createCombo ();
			noEncryptionRadioButton.Active = true;

			// views
			pluginStore = new ListStore (typeof (bool), typeof (string));

			CellRendererToggle crt = new CellRendererToggle();
			crt.Activatable = true;
			crt.Toggled += OnClassToggled;

			pluginTreeView.AppendColumn ("Enabled", crt, "active", 0);
			pluginTreeView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			
			pluginTreeView.Model = pluginStore;

			foreach (ViewPlugin vp in Global.Plugins.ServerViewPlugins) {
				if (conn.ServerViews.Contains (vp.GetType().ToString()))
					pluginStore.AppendValues (true, vp.Name);
				else
					pluginStore.AppendValues (false, vp.Name);
			}

			attrPluginStore = new ListStore (typeof (bool), typeof (string));

			crt = new CellRendererToggle();
			crt.Activatable = true;
			crt.Toggled += OnAttributeViewerToggled;
			
			attrViewPluginTreeView.AppendColumn ("Enabled", crt, "active", 0);
			attrViewPluginTreeView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			
			attrViewPluginTreeView.Model = attrPluginStore;

			if (conn.AttributeViewers.Count == 0)
				conn.SetDefaultAttributeViewers ();
					
			foreach (AttributeViewPlugin avp in Global.Plugins.AttributeViewPlugins) {
				if (conn.AttributeViewers.Contains (avp.GetType().ToString()))
					attrPluginStore.AppendValues (true, avp.Name);
				else
					attrPluginStore.AppendValues (false, avp.Name);
			}

			profileDialog.Icon = Global.latIcon;
		}	

		static void comboSetActive (ComboBox cb, string name)
		{		
			if (name.Equals ("generic ldap server"))
				cb.Active = 2;
			else if (name.Equals ("openldap"))
				cb.Active = 0;
			else if (name.Equals ("microsoft active directory"))
				cb.Active = 1;
		}

		void createCombo ()
		{
			serverTypeComboBox = ComboBox.NewText ();
			serverTypeComboBox.AppendText ("OpenLDAP");
			serverTypeComboBox.AppendText ("Microsoft Active Directory");
			serverTypeComboBox.AppendText ("Fedora Directory Server");
			serverTypeComboBox.AppendText ("Generic LDAP server");

			serverTypeComboBox.Active = 0;
			serverTypeComboBox.Show ();

			stHBox.PackStart (serverTypeComboBox, true, true, 5);
		}

		public void OnAboutClicked (object o, EventArgs args)
		{	
			TreeModel model;
			TreeIter iter;

			if (pluginTreeView.Selection.GetSelected (out model, out iter)) {
							
				string name = (string) pluginStore.GetValue (iter, 1);
				ViewPlugin vp = Global.Plugins.GetViewPlugin (name, conn.Settings.Name); 
				
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
				
				ViewPlugin vp = Global.Plugins.GetViewPlugin (name, conn.Settings.Name); 
				
				if (!conn.ServerViews.Contains (vp.GetType().ToString()))
					conn.ServerViews.Add (vp.GetType().ToString());
				else
					conn.ServerViews.Remove (vp.GetType().ToString());
				
				Global.Connections [conn.Settings.Name] = conn;				
				pluginStore.SetValue(iter,0,!old);
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

		public void OnSavePasswordToggled (object obj, EventArgs args)
		{
			if (savePasswordButton.Active)
				passEntry.Sensitive = false;
			else
				passEntry.Sensitive = true;
		}

		public void OnEncryptionToggled (object obj, EventArgs args)
		{
			if (tlsRadioButton.Active) {
				portEntry.Text = "389";
				encryption = EncryptionType.TLS;
			} else if (sslRadioButton.Active) {
				portEntry.Text = "636";
				encryption = EncryptionType.SSL;
			} else {
				portEntry.Text = "389";
				encryption = EncryptionType.None;
			}
		}
		
		public void OnOkClicked (object o, EventArgs args)
		{
			TreeIter iter;
				
			if (!serverTypeComboBox.GetActiveIter (out iter))
				return;

			string st = (string) serverTypeComboBox.Model.GetValue (iter, 0);

			ConnectionData data = new ConnectionData ();
			data.Name = profileNameEntry.Text;
			data.Host = hostEntry.Text;
			data.Port = int.Parse (portEntry.Text);
			data.DirectoryRoot = ldapBaseEntry.Text;
			data.UserName = userEntry.Text;
			data.Encryption = encryption;
			data.SavePassword = savePasswordButton.Active;
			data.ServerType = Util.GetServerType (st);

			if (data.SavePassword)
				data.Pass = "";
			else
				data.Pass = passEntry.Text;

			if (isEdit) {

				Connection c = Global.Connections [data.Name];
				c.Settings = data;
				
				if (!oldName.Equals (data.Name)) {

					Global.Connections.Delete (oldName);
					Global.Connections[data.Name] = c;

				} else {

					Global.Connections[data.Name] = c;
				}

			} else {

				Connection c = new Connection (data);				
				Global.Connections[data.Name] = c;
			}
			
			Global.Connections.Save ();
		}
	}
}
