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
using System;
using System.Collections;
using Novell.Directory.Ldap;

namespace lat 
{
	public class LoginDialog
	{
		[Glade.Widget] Gtk.Dialog loginDialog;
		[Glade.Widget] Gtk.Label msgLabel;
		[Glade.Widget] Gtk.Entry userEntry;
		[Glade.Widget] Gtk.Entry passEntry;

		Glade.XML ui;

		private LdapServer server;

		public LoginDialog (LdapServer ldapServer, string msg)
		{
			server = ldapServer;

			ui = new Glade.XML (null, "lat.glade", "loginDialog", null);
			ui.Autoconnect (this);

			msgLabel.Text = msg;

			loginDialog.Run ();
			loginDialog.Destroy ();
		}

		public void OnOkClicked (object o, EventArgs args)
		{	
			try
			{
				server.Bind (userEntry.Text, passEntry.Text);
			}
			catch (Exception e)
			{
				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to re-login");

				errorMsg += "\nError: " + e.Message;

				Util.MessageBox (loginDialog, errorMsg,	MessageType.Error);
			}

			loginDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			loginDialog.HideAll ();
		}
	}
}
