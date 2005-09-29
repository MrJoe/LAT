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

using Gtk;
using GLib;
using Glade;
using System;
using System.Collections;

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

		public lat.Connection UserConnection;

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
			
			// FIXME: SSL support
			encryptionRadioButton.Sensitive = false;
			noEncryptionRadioButton.Sensitive = false;
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

		private Connection getSelectedProfile ()
		{
			TreeIter iter;
			TreeModel model;

			if (profileListview.Selection.GetSelected (out model, out iter)) 
			{
				object ob = model.GetValue (iter, 0);

				ConnectionProfile cp = profileManager.Lookup (ob.ToString()); 

				Connection conn = new Connection (cp.Host, 
					cp.Port,
					cp.User,
					cp.Pass,
					cp.LdapRoot,
					cp.SSL,
					cp.ServerType);

				return conn;
			}

			return null;	
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
			Connection conn = getSelectedProfile ();

			Logger.Log.Debug ("Loaded profile for: {0}.", conn.Host);
			Logger.Log.Debug ("Using SSL: {0}.", conn.UseSSL);


			if (conn.UseSSL)
			{
				string url = String.Format ("ldaps://{0}:{1}",
					conn.Host, conn.Port);

				if (!CertificateManager.Ssl (url, connectionDialog))
					return;
			}
			
			conn.Bind ();
	
			if (doConnect (conn))
			{
				connectionDialog.Destroy ();
				new latWindow (conn);
			}
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
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (profileListview.Selection.GetSelected (out model, out iter)) 
			{
				object ob = model.GetValue (iter, 0);
				
				ConnectionProfile cp = profileManager.Lookup (ob.ToString());
				
				new ProfileDialog (profileManager, cp);

				updateProfileList ();
			}
		
		}

		public void OnProfileRemove (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			string msg = null;
			
			if (profileListview.Selection.GetSelected (out model, out iter)) 
			{
				object ob = model.GetValue (iter, 0);
				msg = String.Format ( 
					Mono.Unix.Catalog.GetString ("Are you sure you want to delete the profile: {0}"),
					ob.ToString ());
				
				MessageDialog md = new MessageDialog (connectionDialog, 
						DialogFlags.DestroyWithParent,
						MessageType.Question, 
						ButtonsType.YesNo, 
						msg);
	     
				ResponseType result = (ResponseType)md.Run ();

				if (result == ResponseType.Yes)
				{
					profileManager.deleteProfile (ob.ToString());
					profileManager.saveProfiles ();
					updateProfileList ();				
				}
				
				md.Destroy();																			
			}
				
		}

		private bool doConnect (Connection conn)
		{
			string msg = null;

			if (conn == null)
				return false;

			if (!conn.IsConnected)
			{
				msg = String.Format (
					Mono.Unix.Catalog.GetString ("Unable to connect to: ldap://{0}:{1}"),
					conn.Host, conn.Port);
			}

			if (!conn.IsBound && msg == null && conn.User != "")
			{
				msg = String.Format (
					Mono.Unix.Catalog.GetString ("Unable to bind to: ldap://{0}:{1}"),
					conn.Host, conn.Port);
			}

			if (msg != null)
			{
				Util.MessageBox (connectionDialog, msg, MessageType.Error);

				conn = null;
				
				return false;
			}
		
			return true;
		}

/* FIXME: re-enable SSL support when Mono gets updated Novell.Directory.Ldap.dll */
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

		public void OnConnectClicked (object o, EventArgs args) 
		{
			Connection conn = null;

			if (notebook1.CurrentPage == 0)
			{
				TreeIter iter;
				
				if (!serverTypeComboBox.GetActiveIter (out iter))
					return;

				string serverType = (string) serverTypeComboBox.Model.GetValue (iter, 0);

				conn = new Connection (hostEntry.Text, 
					int.Parse (portEntry.Text),
					userEntry.Text,
					passEntry.Text,
					ldapBaseEntry.Text,
					useSSL,
					serverType);

				if (useSSL)
				{
					string url = String.Format ("ldaps://{0}:{1}",
						hostEntry.Text, portEntry.Text);

					if (!CertificateManager.Ssl (url, connectionDialog))
					{
					}
				}
			}
			else if (notebook1.CurrentPage == 1)
			{
				// Profile
				conn = getSelectedProfile ();

				if (conn == null)
				{
					string	msg = Mono.Unix.Catalog.GetString (
						"No profile selected");

					Util.MessageBox (connectionDialog, msg, MessageType.Error);

					return;
				}

				Logger.Log.Debug ("Loaded profile for: {0}.", conn.Host);
				Logger.Log.Debug ("Using SSL: {0}.", conn.UseSSL);

				if (conn.UseSSL)
				{
					string url = String.Format ("ldaps://{0}:{1}",
						conn.Host, conn.Port);

					if (!CertificateManager.Ssl (url, connectionDialog))
						return;
				}
			}

			conn.Bind ();

			if (doConnect(conn))
			{
				connectionDialog.Destroy ();
				new latWindow (conn);
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
