// 
// lat - NewContactsViewDialog.cs
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
using System.Collections.Generic;
using Novell.Directory.Ldap;

namespace lat
{
	public class NewContactsViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newContactDialog;

		[Glade.Widget] Gtk.Label gnNameLabel;
		[Glade.Widget] Gtk.Entry gnFirstNameEntry;
		[Glade.Widget] Gtk.Entry gnInitialsEntry;
		[Glade.Widget] Gtk.Entry gnLastNameEntry;
		[Glade.Widget] Gtk.Entry gnDisplayName;
		[Glade.Widget] Gtk.Image image181;

		public NewContactsViewDialog (Connection connection, string newContainer) : base (connection, newContainer)
		{
			Init ();

			newContactDialog.Icon = Global.latIcon;
			newContactDialog.Title = "New Contact";

			newContactDialog.Run ();

			while (missingValues || errorOccured) {

				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				newContactDialog.Run ();				
			}

			newContactDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "newContactDialog", null);
			ui.Autoconnect (this);

			viewDialog = newContactDialog;

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("contact-new-48x48.png");
			image181.Pixbuf = pb;
		}

		public void OnNameChanged (object o, EventArgs args)
		{
			gnNameLabel.Text = gnDisplayName.Text;
		}

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();
			aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "person", "inetOrgPerson" }));
			aset.Add (new LdapAttribute ("givenName", gnFirstNameEntry.Text));
			aset.Add (new LdapAttribute ("initials", gnInitialsEntry.Text));
			aset.Add (new LdapAttribute ("sn", gnLastNameEntry.Text));
			aset.Add (new LdapAttribute ("displayName", gnDisplayName.Text));
			aset.Add (new LdapAttribute ("cn", gnDisplayName.Text));
					
			LdapEntry newEntry = new LdapEntry (dn, aset);
			return newEntry;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			LdapEntry entry = null;
			
			string userDN = null;
			
			if (this.defaultNewContainer == null) {
			
				SelectContainerDialog scd =	new SelectContainerDialog (conn, newContactDialog);
				scd.Title = "Save Group";
				scd.Message = String.Format ("Where in the directory would\nyou like save the contact\n{0}?", gnDisplayName.Text);
				scd.Run ();

				if (scd.DN == "")
					return;

				userDN = String.Format ("cn={0},{1}", gnDisplayName.Text, scd.DN);
			
			} else {
			
				userDN = String.Format ("cn={0},{1}", gnDisplayName.Text, this.defaultNewContainer);
			}
			
			entry = CreateEntry (userDN);

			string[] missing = LdapEntryAnalyzer.CheckRequiredAttributes (conn, entry);
			if (missing.Length != 0) {
				missingAlert (missing);
				missingValues = true;
				return;
			}

			if (!Util.AddEntry (conn, entry))
				errorOccured = true;
		}
	}
}
