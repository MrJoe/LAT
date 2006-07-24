// 
// lat - NewAdComputerViewDialog.cs
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
	public class NewAdComputerViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newAdComputerDialog;
//		[Glade.Widget] Gtk.Label computerNameLabel;
		[Glade.Widget] Gtk.Entry computerNameEntry;
		[Glade.Widget] Gtk.Entry dnsNameEntry;
		[Glade.Widget] Gtk.Image image182;

		static string[] hostAttrs = { "cn", "dNSHostName" };

		public NewAdComputerViewDialog (LdapServer ldapServer, string newContainer) : base (ldapServer, newContainer)
		{
			Init ();

			newAdComputerDialog.Icon = Global.latIcon;
			newAdComputerDialog.Title = "Add Computer";

			newAdComputerDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				newAdComputerDialog.Run ();				
			}

			newAdComputerDialog.Destroy ();
		}

		void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "newAdComputerDialog", null);
			ui.Autoconnect (this);

			viewDialog = newAdComputerDialog;

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server-48x48.png");
			image182.Pixbuf = pb;
		}

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();
			aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "computer", "organizationalPerson", "person", "user"}));
			aset.Add (new LdapAttribute ("cn", computerNameEntry.Text));
			aset.Add (new LdapAttribute ("dNSHostName", dnsNameEntry.Text));
			aset.Add (new LdapAttribute ("sAMAccountName", computerNameEntry.Text));
			aset.Add (new LdapAttribute ("userAccountControl", "4128"));
								
			LdapEntry newEntry = new LdapEntry (dn, aset);
			return newEntry;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			LdapEntry entry = null;
			string userDN = null;
			
			if (this.defaultNewContainer == string.Empty) {
			
				SelectContainerDialog scd =	new SelectContainerDialog (server, newAdComputerDialog);
				scd.Title = "Save Group";
				scd.Message = String.Format ("Where in the directory would\nyou like save the computer\n{0}?", computerNameEntry.Text);
				scd.Run ();

				if (scd.DN == "")
					return;

				userDN = String.Format ("cn={0},{1}", computerNameEntry.Text, scd.DN);
			
			} else {
			
				userDN = String.Format ("cn={0},{1}", computerNameEntry.Text, this.defaultNewContainer);
			}
			
			entry = CreateEntry (userDN);

			string[] missing = LdapEntryAnalyzer.CheckRequiredAttributes (server, entry);
			if (missing.Length != 0) {
				missingAlert (missing);
				missingValues = true;
				return;
			}

			if (!Util.AddEntry (server, entry))
				errorOccured = true;
		}
	}
}
