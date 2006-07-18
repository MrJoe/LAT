// 
// lat - ConnectDialog.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
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
using System.Net.Sockets;
using Novell.Directory.Ldap;
using Gtk;

namespace lat 
{
	public class ConnectDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog connectionDialog;
		[Glade.Widget] Gtk.Entry hostEntry;
		[Glade.Widget] Gtk.Entry portEntry;
		[Glade.Widget] Gtk.Entry ldapBaseEntry;
		[Glade.Widget] Gtk.Entry userEntry;
		[Glade.Widget] Gtk.Entry passEntry;
		[Glade.Widget] Gtk.RadioButton tlsRadioButton;
		[Glade.Widget] Gtk.RadioButton sslRadioButton;
		[Glade.Widget] Gtk.RadioButton noEncryptionRadioButton;
		[Glade.Widget] Gtk.CheckButton saveProfileButton;
		[Glade.Widget] Gtk.Entry profileNameEntry;
		[Glade.Widget] Gtk.HBox stHBox;	
//		[Glade.Widget] Gtk.Notebook notebook1;
//		[Glade.Widget] TreeView profileListview;
		[Glade.Widget] Gtk.Image image5;
		
//		bool haveProfiles = false;
		EncryptionType encryption;

//		ListStore profileListStore;
		ComboBox serverTypeComboBox;

		public ConnectDialog ()
		{
			Global.Profiles = new ProfileManager ();
		
			ui = new Glade.XML (null, "lat.glade", "connectionDialog", null);
			ui.Autoconnect (this);

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server-48x48.png");
			image5.Pixbuf = pb;

			connectionDialog.Icon = Global.latIcon;
			connectionDialog.Resizable = false;

			portEntry.Text = "389";
			createCombo ();			

//			profileListStore = new ListStore (typeof (string));
//			profileListview.Model = profileListStore;
//			profileListStore.SetSortColumnId (0, SortType.Ascending);
//			
//			TreeViewColumn col;
//			col = profileListview.AppendColumn ("Name", new CellRendererText (), "text", 0);
//			col.SortColumnId = 0;
//
//			UpdateProfileList ();
//
//			if (haveProfiles) {
//
//				notebook1.CurrentPage = 1;
//				connectionDialog.Resizable = true;
//			}
			
			noEncryptionRadioButton.Active = true;
			
			connectionDialog.Run ();
			connectionDialog.Destroy ();
		}

		private void createCombo ()
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

//		private string GetSelectedProfileName ()
//		{
//			TreeIter iter;
//			TreeModel model;
//
//			if (profileListview.Selection.GetSelected (out model, out iter))  {
//
//				string name = (string) model.GetValue (iter, 0);
//				return name;
//			}
//
//			return null;
//		}
//
//		private ConnectionProfile GetSelectedProfile ()
//		{
//			ConnectionProfile cp = new ConnectionProfile();
//			string profileName = GetSelectedProfileName ();
//
//			if (profileName != null)
//				cp = Global.Profiles [profileName]; 
//	
//			return cp;
//		}

		public void OnPageSwitch (object o, SwitchPageArgs args)
		{
//			if (args.PageNum == 0)
//				connectionDialog.Resizable = false;
//			else if (args.PageNum == 1)
//				connectionDialog.Resizable = true;
		}

		public void OnRowDoubleClicked (object o, RowActivatedArgs args) 
		{
//			ProfileConnect ();
		}

//		void UpdateProfileList ()
//		{
//			string[] names = Global.Profiles.GetProfileNames ();
//			
//			if (names.Length > 1)
//				haveProfiles = true;
//
//			profileListStore.Clear ();
//			
//			foreach (string s in names) 
//				profileListStore.AppendValues (s);
//		}
//
		public void OnProfileAdd (object o, EventArgs args)
		{
//			new ProfileDialog ();
//			UpdateProfileList ();		
		}

		public void OnProfileEdit (object o, EventArgs args)
		{	
//			string profileName = GetSelectedProfileName ();
//
//			if (profileName != null) {
//
//				ConnectionProfile cp = Global.Profiles [profileName];
//			
//				new ProfileDialog (cp);
//
//				UpdateProfileList ();
//			}		
		}

		public void OnProfileRemove (object o, EventArgs args)
		{
//			string profileName = GetSelectedProfileName ();
//			string msg = null;
//			
//			if (profileName != null) {
//
//				msg = String.Format ("{0} {1}",
//					Mono.Unix.Catalog.GetString (
//					"Are you sure you want to delete the profile:"),
//					profileName);
//				
//				if (Util.AskYesNo (connectionDialog, msg)) {
//
//					Global.Profiles.Remove (profileName);
//					Global.Profiles.SaveProfiles ();
//					UpdateProfileList ();				
//				}
//			}
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

//		private void ProfileConnect ()
//		{
//			LdapServer server = null;
//			ConnectionProfile cp = GetSelectedProfile ();
//
//			if (cp.Host == null) {
//
//				string	msg = Mono.Unix.Catalog.GetString (
//					"No profile selected");
//
//				HIGMessageDialog dialog = new HIGMessageDialog (
//					connectionDialog,
//					0,
//					Gtk.MessageType.Error,
//					Gtk.ButtonsType.Ok,
//					"Profile error",
//					msg);
//
//				dialog.Run ();
//				dialog.Destroy ();
//
//				return;
//			}
//
//			if (cp.LdapRoot == "") {
//
//				server = new LdapServer (cp.Host, cp.Port, cp.ServerType);
//
//			} else {
//
//				server = new LdapServer (cp.Host, cp.Port, 
//						 cp.LdapRoot, 
//						 cp.ServerType);
//			}
//
//			server.ProfileName = cp.Name;			
//			encryption = cp.Encryption;
//
//			if (cp.DontSavePassword) {
//
//				LoginDialog ld = new LoginDialog (
//					Mono.Unix.Catalog.GetString ("Enter your password"), 
//					cp.User);
//
//				ld.Run ();
//
//				if (ld.UserPass != null)
//					DoConnect (server, ld.UserName, ld.UserPass);
//
//			} else {
//
//				DoConnect (server, cp.User, cp.Pass);
//			}
//		}

		public void OnConnectClicked (object o, EventArgs args) 
		{
			TreeIter iter;
				
			if (!serverTypeComboBox.GetActiveIter (out iter))
				return;

			string serverType = (string) serverTypeComboBox.Model.GetValue (iter, 0);

			ConnectionProfile cp = new ConnectionProfile ();
			cp.Host = hostEntry.Text;
			cp.Port = int.Parse (portEntry.Text);
			cp.User = userEntry.Text;
			cp.Pass = passEntry.Text;
			cp.LdapRoot = ldapBaseEntry.Text;
			cp.ServerType = serverType;			
			cp.DontSavePassword = false;
			cp.Encryption = encryption;

			if (saveProfileButton.Active) {
				cp.Name = profileNameEntry.Text;
				cp.Dynamic = false;
			} else {
				cp.Name = String.Format ("{0}:{1}", cp.Host, cp.Port);
				cp.Dynamic = true;
			}
			
			Global.Profiles [cp.Name] = cp;
		}
	}
}
