// 
// lat - LoginDialog.cs
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
using Novell.Directory.Ldap;

namespace lat 
{
	public class LoginDialog
	{
		[Glade.Widget] Gtk.Dialog loginDialog;
		[Glade.Widget] Gtk.Entry userEntry;
		[Glade.Widget] Gtk.Entry passEntry;
		[Glade.Widget] Button okButton;
		[Glade.Widget] Button cancelButton;

		Glade.XML ui;

		private lat.Connection _conn;

		public LoginDialog (lat.Connection conn)
		{
			_conn = conn;

			ui = new Glade.XML (null, "lat.glade", "loginDialog", null);
			ui.Autoconnect (this);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			loginDialog.Run ();
			loginDialog.Destroy ();
		}

		private void OnOkClicked (object o, EventArgs args)
		{	
			if (!_conn.Bind (userEntry.Text, passEntry.Text))
			{
				Util.MessageBox (loginDialog,
					Mono.Unix.Catalog.GetString ("Unable to re-login"),
					MessageType.Error);
			}

			loginDialog.HideAll ();
		}

		private void OnCancelClicked (object o, EventArgs args)
		{
			loginDialog.HideAll ();
		}
	}
}
