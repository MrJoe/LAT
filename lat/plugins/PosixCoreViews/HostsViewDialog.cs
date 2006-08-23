// 
// lat - HostsViewDialog.cs
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
	public class HostsViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog hostDialog;
		[Glade.Widget] Gtk.Entry hostNameEntry;
		[Glade.Widget] Gtk.Entry ipEntry;
		[Glade.Widget] Gtk.Entry descriptionEntry;
		[Glade.Widget] Gtk.Image image31;

		LdapEntry currentEntry;
		bool isEdit;

		public HostsViewDialog (Connection connection, string newContainer) : base (connection, newContainer)
		{
			Init ();

			hostDialog.Icon = Global.latIcon;
			hostDialog.Title = "Add Computer";

			hostDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				hostDialog.Run ();				
			}

			hostDialog.Destroy ();
		}

		public HostsViewDialog (Connection connection, LdapEntry le) : base (connection, null)
		{
			isEdit = true;
			currentEntry = le;
			
			Init ();

			string hostName = conn.Data.GetAttributeValueFromEntry (currentEntry, "cn"); 
			hostDialog.Title = hostName + " Properties";
			hostNameEntry.Text = hostName;
			
			ipEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "ipHostNumber");
			descriptionEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "description");

			hostDialog.Run ();
			hostDialog.Destroy ();
		}

		void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "hostDialog", null);
			ui.Autoconnect (this);

			viewDialog = hostDialog;

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server-48x48.png");
			image31.Pixbuf = pb;
		}

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();
			aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "ipHost", "device"}));
			aset.Add (new LdapAttribute ("cn", hostNameEntry.Text));
			aset.Add (new LdapAttribute ("ipHostNumber", ipEntry.Text));
			aset.Add (new LdapAttribute ("description", descriptionEntry.Text));
			
			LdapEntry newEntry = new LdapEntry (dn, aset);
			return newEntry;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			LdapEntry entry = null;
			
			if (isEdit) {
				 entry = CreateEntry (currentEntry.DN);
				 
				 LdapEntryAnalyzer lea = new LdapEntryAnalyzer ();
				 lea.Run (currentEntry, entry);
				 
				 if (lea.Differences.Length == 0)
				 	return;
				 	
				 if (!Util.ModifyEntry (conn, entry.DN, lea.Differences))
				 	errorOccured = true;
				 	
			} else {
			
				SelectContainerDialog scd = new SelectContainerDialog (conn, hostDialog);
				scd.Title = "Save Host";
				scd.Message = String.Format ("Where in the directory would\nyou like save the host\n{0}?", hostNameEntry.Text);
				scd.Run ();

				if (scd.DN == "")
					return;

				string userDN = String.Format ("cn={0},{1}", hostNameEntry.Text, scd.DN);
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
}
