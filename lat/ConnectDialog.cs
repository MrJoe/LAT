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
using System.Collections;
using System.Net.Sockets;
using Novell.Directory.Ldap;
using Gtk;

#if ENABLE_AVAHI
using Avahi;
#endif

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
		[Glade.Widget] Gtk.HBox stHBox;	
		[Glade.Widget] Gtk.Notebook notebook1;
		[Glade.Widget] TreeView profileListview;
		[Glade.Widget] Gtk.Image image5;
		
		bool haveProfiles = false;
		EncryptionType encryption;

		ListStore profileListStore;
		ComboBox serverTypeComboBox;

#if ENABLE_AVAHI
		Client client;
		ServiceBrowser sb;
#endif
		public ConnectDialog ()
		{
#if ENABLE_AVAHI
			client = new Client();		
			sb = new ServiceBrowser (client, "_ldap._tcp");
			
			sb.ServiceAdded += OnServiceAdded;
			sb.ServiceRemoved += OnServiceRemoved;
#endif		
			Global.profileManager = new ProfileManager ();
		
			ui = new Glade.XML (null, "lat.glade", "connectionDialog", null);
			ui.Autoconnect (this);

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server-48x48.png");
			image5.Pixbuf = pb;

			connectionDialog.Icon = Global.latIcon;
			connectionDialog.Resizable = false;

			portEntry.Text = "389";
			createCombo ();			

			profileListStore = new ListStore (typeof (string));
			profileListview.Model = profileListStore;
			profileListStore.SetSortColumnId (0, SortType.Ascending);
			
			TreeViewColumn col;
			col = profileListview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			updateProfileList ();

			if (haveProfiles) {

				notebook1.CurrentPage = 1;
				connectionDialog.Resizable = true;
			}
			
			noEncryptionRadioButton.Active = true;
		}

		private void createCombo ()
		{
			serverTypeComboBox = ComboBox.NewText ();
			serverTypeComboBox.AppendText ("OpenLDAP");
			serverTypeComboBox.AppendText ("Microsoft Active Directory");
			serverTypeComboBox.AppendText ("Generic LDAP server");

			serverTypeComboBox.Active = 0;
			serverTypeComboBox.Show ();

			stHBox.PackStart (serverTypeComboBox, true, true, 5);
		}

		private string GetSelectedProfileName ()
		{
			TreeIter iter;
			TreeModel model;

			if (profileListview.Selection.GetSelected (out model, out iter))  {

				string name = (string) model.GetValue (iter, 0);
				return name;
			}

			return null;
		}

		private ConnectionProfile GetSelectedProfile ()
		{
			ConnectionProfile cp = new ConnectionProfile();
			string profileName = GetSelectedProfileName ();

			if (profileName != null)
				cp = Global.profileManager [profileName]; 
	
			return cp;
		}

		public void OnPageSwitch (object o, SwitchPageArgs args)
		{
			if (args.PageNum == 0)
				connectionDialog.Resizable = false;
			else if (args.PageNum == 1)
				connectionDialog.Resizable = true;
		}

		public void OnRowDoubleClicked (object o, RowActivatedArgs args) 
		{
			ProfileConnect ();
		}

#if ENABLE_AVAHI

	    void OnServiceResolved (object o, ServiceInfoArgs args) 
		{
			(o as ServiceResolver).Dispose ();

			ConnectionProfile cp = new ConnectionProfile ();
			cp.Name = args.Service.Name;
			cp.Host = args.Service.Address.ToString ();
			cp.Port = args.Service.Port;
			cp.DontSavePassword = false;
			cp.ServerType = "Generic LDAP server";
			cp.Dynamic = true;

			Logger.Log.Debug ("Found LDAP service {0} on {1} port {2}", 
				args.Service.Name, args.Service.Address, args.Service.Port);

			if (args.Service.Port == 636)
				cp.Encryption = EncryptionType.SSL;
				
//			Global.profileManager [cp.Name] = cp;
//			updateProfileList ();
		}

		void OnServiceAdded (object o, ServiceInfoArgs args) 
		{
			ServiceResolver resolver = new ServiceResolver (client, args.Service);
			resolver.Found += OnServiceResolved;
		}

		void OnServiceRemoved (object o, ServiceInfoArgs args)
		{
			Global.profileManager.Remove (args.Service.Name);
//			updateProfileList ();
		}
		
#endif	

		void updateProfileList ()
		{
			string[] names = Global.profileManager.GetProfileNames ();
			
			if (names.Length > 1)
				haveProfiles = true;

			profileListStore.Clear ();
			
			foreach (string s in names) 
				profileListStore.AppendValues (s);
		}

		public void OnProfileAdd (object o, EventArgs args)
		{
			new ProfileDialog ();
			updateProfileList ();		
		}

		public void OnProfileEdit (object o, EventArgs args)
		{	
			string profileName = GetSelectedProfileName ();

			if (profileName != null) {

				ConnectionProfile cp = Global.profileManager [profileName];
			
				new ProfileDialog (cp);

				updateProfileList ();
			}		
		}

		public void OnProfileRemove (object o, EventArgs args)
		{
			string profileName = GetSelectedProfileName ();
			string msg = null;
			
			if (profileName != null) {

				msg = String.Format ("{0} {1}",
					Mono.Unix.Catalog.GetString (
					"Are you sure you want to delete the profile:"),
					profileName);
				
				if (Util.AskYesNo (connectionDialog, msg)) {

					Global.profileManager.Remove (profileName);
					Global.profileManager.SaveProfiles ();
					updateProfileList ();				
				}
			}
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

		private bool CheckConnection (LdapServer server, string userName)
		{
			string msg = null;

			if (server == null)
				return false;

			if (!server.Connected) {

				msg = String.Format (
					Mono.Unix.Catalog.GetString (
					"Unable to connect to: ldap://{0}:{1}"),
					server.Host, server.Port);
			}

			if (!server.Bound && msg == null && userName != "") {

				msg = String.Format (
					Mono.Unix.Catalog.GetString (
					"Unable to bind to: ldap://{0}:{1}"),
					server.Host, server.Port);
			}

			if (msg != null) {

				HIGMessageDialog dialog = new HIGMessageDialog (
					connectionDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Connection Error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			
				return false;
			}
		
			return true;
		}

		private void DoConnect (LdapServer server, string userName, string userPass)
		{
			try {
				server.Connect (encryption);
				server.Bind (userName, userPass);

			} catch (SocketException se) {

				Logger.Log.Debug ("Socket error: {0}", se.Message);

			} catch (LdapException le) {

				Logger.Log.Debug ("Ldap error: {0}", le.Message);

				HIGMessageDialog dialog = new HIGMessageDialog (
					connectionDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Connection error",
					le.Message);

				dialog.Run ();
				dialog.Destroy ();

				return;

			} catch (Exception e) {

				Logger.Log.Debug ("Unknown error: {0}", e.Message);

				HIGMessageDialog dialog = new HIGMessageDialog (
					connectionDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Unknown connection error",
					Mono.Unix.Catalog.GetString ("An unknown error occured: ") + e.Message);

				dialog.Run ();
				dialog.Destroy ();

				return;
			}

			if (CheckConnection (server, userName)) {

				connectionDialog.Destroy ();
				new latWindow (server);
			}
		}

		private void QuickConnect ()
		{
			LdapServer server = null;
			TreeIter iter;
				
			if (!serverTypeComboBox.GetActiveIter (out iter))
				return;

			string serverType = (string) serverTypeComboBox.Model.GetValue (iter, 0);

			if (ldapBaseEntry.Text != "") {

				server = new LdapServer (
					hostEntry.Text, 
					int.Parse (portEntry.Text), 
					ldapBaseEntry.Text,
					serverType);

			} else {

				server = new LdapServer (
					hostEntry.Text, 
					int.Parse (portEntry.Text), 
					serverType);
			}

			DoConnect (server, userEntry.Text, passEntry.Text);
		}

		private void ProfileConnect ()
		{
			LdapServer server = null;
			ConnectionProfile cp = GetSelectedProfile ();

			if (cp.Host == null) {

				string	msg = Mono.Unix.Catalog.GetString (
					"No profile selected");

				HIGMessageDialog dialog = new HIGMessageDialog (
					connectionDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Profile error",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				return;
			}

			if (cp.LdapRoot == "") {

				server = new LdapServer (cp.Host, cp.Port, cp.ServerType);

			} else {

				server = new LdapServer (cp.Host, cp.Port, 
						 cp.LdapRoot, 
						 cp.ServerType);
			}

			server.ProfileName = cp.Name;			
			encryption = cp.Encryption;

			if (cp.DontSavePassword) {

				LoginDialog ld = new LoginDialog (
					Mono.Unix.Catalog.GetString ("Enter your password"), 
					cp.User);

				ld.Run ();

				if (ld.UserPass != null)
					DoConnect (server, ld.UserName, ld.UserPass);

			} else {

				DoConnect (server, cp.User, cp.Pass);
			}
		}

		public void OnConnectClicked (object o, EventArgs args) 
		{
			if (notebook1.CurrentPage == 0)
				QuickConnect ();
			else if (notebook1.CurrentPage == 1)
				ProfileConnect ();
		}

		public void OnCloseClicked (object o, EventArgs args) 
		{
			exitApp ();
		}

		public void OnAppDelete (object o, DeleteEventArgs args) 
		{
			exitApp ();
		}

		private void exitApp ()
		{
			connectionDialog.Destroy ();
			Application.Quit ();
		}
	}
}
