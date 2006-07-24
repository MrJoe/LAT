// 
// lat - NewAdGroupViewDialog.cs
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
	public class NewAdGroupViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newAdGroupDialog;
		[Glade.Widget] Gtk.Entry groupNameEntry;
		[Glade.Widget] Gtk.Entry descriptionEntry;

		public NewAdGroupViewDialog (LdapServer ldapServer, string newContainer) : base (ldapServer, newContainer)
		{
			Init ();

			newAdGroupDialog.Icon = Global.latIcon;
			newAdGroupDialog.Title = "Add Group";

			newAdGroupDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				newAdGroupDialog.Run ();				
			}

			newAdGroupDialog.Destroy ();
		}

		void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "newAdGroupDialog", null);
			ui.Autoconnect (this);

			viewDialog = newAdGroupDialog;
		}

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();
			aset.Add (new LdapAttribute ("objectClass", new string[] {"group"}));
			aset.Add (new LdapAttribute ("cn", groupNameEntry.Text));
			aset.Add (new LdapAttribute ("description", descriptionEntry.Text));
			aset.Add (new LdapAttribute ("sAMAccountName", groupNameEntry.Text));
			aset.Add (new LdapAttribute ("groupType", "2"));
								
			LdapEntry newEntry = new LdapEntry (dn, aset);
			return newEntry;
		}

		bool IsGroupGood ()
		{
			if (groupNameEntry.Text == "" || descriptionEntry.Text == "") {

				string msg = Mono.Unix.Catalog.GetString (
					"You must provide a group name and description");

				HIGMessageDialog dialog = new HIGMessageDialog (
					newAdGroupDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Creation error",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				return false;
			}
			
			return true;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			if (!IsGroupGood()) {
				missingValues = true;
				return;
			}
		
			LdapEntry entry = null;
			string userDN = null;
			
			if (this.defaultNewContainer == string.Empty || this.defaultNewContainer == null) {
			
				SelectContainerDialog scd =	new SelectContainerDialog (server, newAdGroupDialog);
				scd.Title = "Save Group";
				scd.Message = String.Format ("Where in the directory would\nyou like save the group\n{0}?", groupNameEntry.Text);
				scd.Run ();

				if (scd.DN == "")
					return;

				userDN = String.Format ("cn={0},{1}", groupNameEntry.Text, scd.DN);
			
			} else {
			
				userDN = String.Format ("cn={0},{1}", groupNameEntry.Text, this.defaultNewContainer);
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
