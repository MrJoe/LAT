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
		
		[Glade.Widget] Gtk.Notebook notebook1;

		[Glade.Widget] TreeView profileListview; 
		[Glade.Widget] Gtk.Button profileAddButton;
		[Glade.Widget] Gtk.Button profileEditButton;
		[Glade.Widget] Gtk.Button profileRemoveButton;
		
		[Glade.Widget] Gtk.Button connectButton;
		[Glade.Widget] Gtk.Button closeButton;

		private bool useSSL = false;

		private ProfileManager profileManager;
		private ListStore profileListStore;

		public lat.Connection UserConnection;

		public ConnectDialog ()
		{
			profileManager = new ProfileManager ();
		
			ui = new Glade.XML (null, "lat.glade", "connectionDialog", null);
			ui.Autoconnect (this);

			portEntry.Text = "389";
			
			profileListStore = new ListStore (typeof (string));
			profileListview.Model = profileListStore;
			
			TreeViewColumn col;
			col = profileListview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			updateProfileList ();
			
			profileListview.RowActivated += new RowActivatedHandler (OnRowDoubleClicked);

			profileAddButton.Clicked += new EventHandler (OnProfileAdd);
			profileEditButton.Clicked += new EventHandler (OnProfileEdit);
			profileRemoveButton.Clicked += new EventHandler (OnProfileRemove);

			encryptionRadioButton.Toggled += new EventHandler (OnEncryptionToggled);
			noEncryptionRadioButton.Active = true;
		
			encryptionRadioButton.Sensitive = false;
			noEncryptionRadioButton.Sensitive = false;

			connectButton.Clicked += new EventHandler (OnConnectClicked);
			closeButton.Clicked += new EventHandler (OnCloseClicked);

			connectionDialog.DeleteEvent += new DeleteEventHandler (OnAppDelete);
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
					cp.SSL);

				return conn;
			}

			return null;	
		}

		private void OnRowDoubleClicked (object o, RowActivatedArgs args) 
		{
			Connection conn = getSelectedProfile ();
				
			if (doConnect (conn))
			{
				connectionDialog.Destroy ();
				new latWindow (conn);
			}
		}

		private void updateProfileList ()
		{
			string[] names = profileManager.getProfileNames ();
			
			profileListStore.Clear ();
			
			foreach (string s in names)
			{
				profileListStore.AppendValues (s);
			}
		}

		private void OnProfileAdd (object o, EventArgs args)
		{
			new ProfileDialog (profileManager);
			updateProfileList ();		
		}

		private void OnProfileEdit (object o, EventArgs args)
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

		private void OnProfileRemove (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			string msg = null;
			
			if (profileListview.Selection.GetSelected (out model, out iter)) 
			{
				object ob = model.GetValue (iter, 0);
				msg = String.Format ( 
					Mono.Posix.Catalog.GetString ("Are you sure you want to delete the profile: {0}"),
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
					Mono.Posix.Catalog.GetString ("Unable to connect to: ldap://{0}:{1}"),
					conn.Host, conn.Port);
			}

			if (!conn.IsBound && msg == null && conn.User != "")
			{
				msg = String.Format (
					Mono.Posix.Catalog.GetString ("Unable to bind to: ldap://{0}:{1}"),
					conn.Host, conn.Port);
			}

			if (msg != null)
			{
				MessageDialog md = new MessageDialog (connectionDialog, 
						DialogFlags.DestroyWithParent,
						MessageType.Error, 
						ButtonsType.Close, 
						msg);

				md.Run ();
				md.Destroy();

				md = null;
				conn = null;
				
				return false;
			}
		
			return true;
		}

		private void OnEncryptionToggled (object obj, EventArgs args)
		{
			if (encryptionRadioButton.Active)
			{
				useSSL = true;
			}
			else
			{
				useSSL = false;
			}
		}

		private void OnConnectClicked (object o, EventArgs args) 
		{
			Connection conn = null;

			if (notebook1.CurrentPage == 0)
			{
				conn = new Connection (hostEntry.Text, 
					int.Parse (portEntry.Text),
					userEntry.Text,
					passEntry.Text,
					ldapBaseEntry.Text,
					useSSL);
			}
			else if (notebook1.CurrentPage == 1)
			{
				// Profile
				conn = getSelectedProfile ();			
			}

			if (doConnect(conn))
			{
				connectionDialog.Destroy ();
				new latWindow (conn);
			}
		}

		private void OnCloseClicked (object o, EventArgs args) 
		{
			exitApp ();
		}

		private void OnAppDelete (object o, DeleteEventArgs args) 
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
