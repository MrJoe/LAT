// 
// lat - RenameEntryDialog.cs
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
using System;
using System.Collections;

namespace lat
{
	public class RenameEntryDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog renameEntryDialog;
		[Glade.Widget] Gtk.Entry oldNameEntry;
		[Glade.Widget] Gtk.Entry newNameEntry;
		[Glade.Widget] Gtk.CheckButton saveOldNameCheckButton;

		private LdapServer server;
		private string _selectedDN;

		public RenameEntryDialog (LdapServer ldapServer, string selectedDN)
		{
			server = ldapServer;
			_selectedDN = selectedDN;

			ui = new Glade.XML (null, "lat.glade", "renameEntryDialog", null);
			ui.Autoconnect (this);

			oldNameEntry.Text = _selectedDN;
			
			renameEntryDialog.Run ();
			renameEntryDialog.Destroy ();
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			string oldDN = oldNameEntry.Text;
			string newDN = newNameEntry.Text;
			bool saveOld = saveOldNameCheckButton.Active;
			
			try {

				server.Rename (oldDN, newDN, saveOld);

				string msg = String.Format (
					Mono.Unix.Catalog.GetString (
					"Entry {0} has been renamed to {1}."),
					oldDN, newDN);

				Util.MessageBox (renameEntryDialog, msg, MessageType.Info);

			} catch (Exception e) {

				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to rename entry ") + oldDN;

				errorMsg += "\nError: " + e.Message;

				Util.MessageBox (renameEntryDialog, errorMsg, MessageType.Error);
			}

			renameEntryDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			renameEntryDialog.HideAll ();
		}

		public void OnDlgDelete (object o, DeleteEventArgs args)
		{
			renameEntryDialog.HideAll ();
		}
	}
}
