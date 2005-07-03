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
using GLib;
using Glade;
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
		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private Connection _conn;
		private string _selectedDN;

		public RenameEntryDialog (Connection conn, string selectedDN)
		{
			_conn = conn;
			_selectedDN = selectedDN;

			ui = new Glade.XML (null, "lat.glade", "renameEntryDialog", null);
			ui.Autoconnect (this);

			oldNameEntry.Text = _selectedDN;
			
			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			renameEntryDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);

			renameEntryDialog.Run ();
			renameEntryDialog.Destroy ();
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			string oldDN = oldNameEntry.Text;
			string newDN = newNameEntry.Text;
			bool saveOld = saveOldNameCheckButton.Active;
			
			try
			{
				_conn.Rename (oldDN, newDN, saveOld);

				string msg = String.Format (
					Mono.Unix.Catalog.GetString ("Entry {0} has been renamed to {1}."),
					oldDN, newDN);

				Util.MessageBox (renameEntryDialog, msg, MessageType.Info);
			}
			catch (Exception e)
			{
				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to rename entry ") + oldDN;

				errorMsg += "\nError: " + e.Message;

				Util.MessageBox (renameEntryDialog, errorMsg, MessageType.Error);
			}

			renameEntryDialog.HideAll ();
		}

		private void OnCancelClicked (object o, EventArgs args)
		{
			renameEntryDialog.HideAll ();
		}

		private void OnDlgDelete (object o, DeleteEventArgs args)
		{
			renameEntryDialog.HideAll ();
		}
	}
}
