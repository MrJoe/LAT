// 
// lat - LoginDialog.cs
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
using Novell.Directory.Ldap;

namespace lat 
{
	public class LoginDialog
	{
		[Glade.Widget] Gtk.Dialog loginDialog;
		[Glade.Widget] Gtk.Label msgLabel;
		[Glade.Widget] Gtk.Entry userEntry;
		[Glade.Widget] Gtk.Entry passEntry;
		[Glade.Widget] Gtk.CheckButton useSSLCheckButton;
		[Glade.Widget] Gtk.Image image455;

		Glade.XML ui;

		Connection conn;
		bool isRelogin = false;
		string userName;
		string userPass;

		public LoginDialog (string msg, string user)
		{
			Init ();

			useSSLCheckButton.HideAll ();

			msgLabel.Text = msg;
			userEntry.Text = user;
		}

		public LoginDialog (Connection connection, string msg)
		{
			Init ();

			conn = connection;
			msgLabel.Text = msg;
			isRelogin = true;
		}

		public void Run ()
		{
			loginDialog.Run ();
			loginDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "loginDialog", null);
			ui.Autoconnect (this);

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("locked-48x48.png");
			image455.Pixbuf = pb;

			loginDialog.Icon = Global.latIcon;
		}

		private void Relogin ()
		{
			try {
				if (useSSLCheckButton.Active)
					conn.Settings.Encryption = EncryptionType.SSL;

				if (conn.Settings.Encryption == EncryptionType.TLS)
					conn.StartTLS ();

				conn.Bind (userEntry.Text, passEntry.Text);

			} catch (Exception e) {

				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to re-login");

				errorMsg += "\nError: " + e.Message;

				HIGMessageDialog dialog = new HIGMessageDialog (
					loginDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Login error",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		public void OnOkClicked (object o, EventArgs args)
		{	
			if (isRelogin) {
				Relogin ();
			} else {
				userName = userEntry.Text;
				userPass = passEntry.Text;
			}

			loginDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			loginDialog.HideAll ();
		}

		public string UserName 
		{
			get { return userName; }
		}

		public string UserPass
		{
			get { return userPass; }
		}
	}
}
