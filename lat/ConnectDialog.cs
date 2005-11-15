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
		[Glade.Widget] Gtk.RadioButton encryptionRadioButton;
		[Glade.Widget] Gtk.RadioButton noEncryptionRadioButton;
		[Glade.Widget] Gtk.HBox stHBox;	
		[Glade.Widget] Gtk.Notebook notebook1;
		[Glade.Widget] TreeView profileListview; 
		
		private bool haveProfiles = false;
		private bool useSSL = false;

		private ProfileManager profileManager;
		private ListStore profileListStore;

		private ComboBox serverTypeComboBox;

		public ConnectDialog ()
		{
			profileManager = new ProfileManager ();
		
			ui = new Glade.XML (null, "lat.glade", "connectionDialog", null);
			ui.Autoconnect (this);

			connectionDialog.Resizable = false;

			portEntry.Text = "389";
			createCombo ();			

			profileListStore = new ListStore (typeof (string));
			profileListview.Model = profileListStore;
			
			TreeViewColumn col;
			col = profileListview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			updateProfileList ();

			if (haveProfiles)
			{
				notebook1.CurrentPage = 1;
				connectionDialog.Resizable = true;
			}
			
			noEncryptionRadioButton.Active = true;		
		}

		private void createCombo ()
		{
			serverTypeComboBox = ComboBox.NewText ();
			serverTypeComboBox.AppendText ("Generic LDAP Server");
			serverTypeComboBox.AppendText ("OpenLDAP");
			serverTypeComboBox.AppendText ("Microsoft Active Directory");

			serverTypeComboBox.Active = 0;
			serverTypeComboBox.Show ();

			stHBox.PackStart (serverTypeComboBox, true, true, 5);
		}

		internal string GetSelectedProfileName ()
		{
			TreeIter iter;
			TreeModel model;

			if (profileListview.Selection.GetSelected (out model, out iter)) 
			{
				string name = (string) model.GetValue (iter, 0);
				return name;
			}

			return null;
		}

		internal ConnectionProfile GetSelectedProfile ()
		{
			ConnectionProfile cp = new ConnectionProfile();
			string profileName = GetSelectedProfileName ();

			if (profileName != null)
				cp = profileManager.Lookup (profileName); 
	
			return cp;
		}

		public void OnPageSwitch (object o, SwitchPageArgs args)
		{
			if (args.PageNum == 0)
			{
				connectionDialog.Resizable = false;
			}
			else if (args.PageNum == 1)
			{
				connectionDialog.Resizable = true;
			}
		}

		public void OnRowDoubleClicked (object o, RowActivatedArgs args) 
		{
			ProfileConnect ();
		}

		private void updateProfileList ()
		{
			string[] names = profileManager.getProfileNames ();
			
			if (names.Length > 1)
			{
				haveProfiles = true;
			}

			profileListStore.Clear ();
			
			foreach (string s in names)
			{
				profileListStore.AppendValues (s);
			}
		}

		public void OnProfileAdd (object o, EventArgs args)
		{
			new ProfileDialog (profileManager);
			updateProfileList ();		
		}

		public void OnProfileEdit (object o, EventArgs args)
		{	
			string profileName = GetSelectedProfileName ();

			if (profileName != null)
			{
				ConnectionProfile cp = profileManager.Lookup (profileName);
			
				new ProfileDialog (profileManager, cp);

				updateProfileList ();
			}		
		}

		public void OnProfileRemove (object o, EventArgs args)
		{
			string profileName = GetSelectedProfileName ();
			string msg = null;
			
			if (profileName != null)
			{
				msg = String.Format ("{0} {1}",
					Mono.Unix.Catalog.GetString (
					"Are you sure you want to delete the profile:"),
					profileName);
				
				if (Util.AskYesNo (connectionDialog, msg))
				{
					profileManager.deleteProfile (profileName);
					profileManager.saveProfiles ();
					updateProfileList ();				
				}
			}
		}

		public void OnEncryptionToggled (object obj, EventArgs args)
		{
			if (encryptionRadioButton.Active)
			{
				useSSL = true;
				portEntry.Text = "636";
			}
			else
			{
				useSSL = false;
				portEntry.Text = "389";
			}
		}

		internal bool CheckConnection (LdapServer server, string userName)
		{
			string msg = null;

			if (server == null)
				return false;

			if (!server.Connected)
			{
				msg = String.Format (
					Mono.Unix.Catalog.GetString (
					"Unable to connect to: ldap://{0}:{1}"),
					server.Host, server.Port);
			}

			if (!server.Bound && msg == null && userName != "")
			{
				msg = String.Format (
					Mono.Unix.Catalog.GetString (
					"Unable to bind to: ldap://{0}:{1}"),
					server.Host, server.Port);
			}

			if (msg != null)
			{
				Util.MessageBox (connectionDialog, msg, 
						 MessageType.Error);
			
				return false;
			}
		
			return true;
		}

		internal void DoConnect (LdapServer server, string userName, string userPass)
		{
			try
			{
				server.Connect (useSSL);
				server.Bind (userName, userPass);
			}
			catch (SocketException se)
			{
				Logger.Log.Debug ("Socket error: {0}", se.Message);
			}

			if (CheckConnection (server, userName))
			{
				connectionDialog.Destroy ();
				new latWindow (server);
			}
		}

		internal void QuickConnect ()
		{
			LdapServer server = null;
			TreeIter iter;
				
			if (!serverTypeComboBox.GetActiveIter (out iter))
				return;

			string serverType = (string) 
				serverTypeComboBox.Model.GetValue (iter, 0);

			if (ldapBaseEntry.Text != "")
			{
				server = new LdapServer (
					hostEntry.Text, 
					int.Parse (portEntry.Text), 
					ldapBaseEntry.Text,
					serverType);
			}
			else
			{
				server = new LdapServer (
					hostEntry.Text, 
					int.Parse (portEntry.Text), 
					serverType);
			}

			DoConnect (server, userEntry.Text, passEntry.Text);
		}

		internal void ProfileConnect ()
		{
			LdapServer server = null;
			ConnectionProfile cp = GetSelectedProfile ();

			if (cp.Host == null)
			{
				string	msg = Mono.Unix.Catalog.GetString (
					"No profile selected");

				Util.MessageBox (connectionDialog, 
					msg,
					MessageType.Error);

				return;
			}

			if (cp.LdapRoot == "")
			{
				server = new LdapServer (cp.Host, cp.Port, 
						 cp.ServerType);
			}
			else
			{
				server = new LdapServer (cp.Host, cp.Port, 
						 cp.LdapRoot, 
						 cp.ServerType);
			}

			useSSL = cp.SSL;

			DoConnect (server, cp.User, cp.Pass);
		}

		public void OnConnectClicked (object o, EventArgs args) 
		{
			if (notebook1.CurrentPage == 0)
			{
				QuickConnect ();
			}
			else if (notebook1.CurrentPage == 1)
			{
				ProfileConnect ();
			}
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
