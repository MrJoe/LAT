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
using System.Collections;
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

		private bool _isPosix;

		private static string[] contactAttrs = { "givenName", "sn", "initials", "cn", "displayName" };

		public NewContactsViewDialog (LdapServer ldapServer) : base (ldapServer)
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
			ui = new Glade.XML (null, "lat.glade", "newContactDialog", null);
			ui.Autoconnect (this);

			viewDialog = newContactDialog;

			switch (server.ServerType) {

			case LdapServerType.ActiveDirectory:
				_isPosix = false;
				break;

			case LdapServerType.OpenLDAP:
			case LdapServerType.Generic:
			default:
				_isPosix = true;
				break;
			}

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("contact-new-48x48.png");
			image181.Pixbuf = pb;
		}

		public void OnNameChanged (object o, EventArgs args)
		{
			gnNameLabel.Text = gnDisplayName.Text;
		}

		private Hashtable getCurrentContactInfo ()
		{
			Hashtable retVal = new Hashtable ();

			retVal.Add ("givenName", gnFirstNameEntry.Text);
			retVal.Add ("initials", gnInitialsEntry.Text);
			retVal.Add ("sn", gnLastNameEntry.Text);
			retVal.Add ("displayName", gnDisplayName.Text);

			return retVal;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cci = getCurrentContactInfo ();

			string[] objClass;
			string[] missing = null;

			if (!_isPosix)
				objClass = new string[] {"top", "person", "organizationalPerson", "contact" };
			else
				objClass = new string[] {"top", "person", "inetOrgPerson" };

			if (!checkReqAttrs (objClass, cci, out missing)) {
				missingAlert (missing);
				missingValues = true;

				return;
			}

			ArrayList attrList = getAttributes (objClass, contactAttrs, cci);

			string fullName = (string)cci["displayName"];
			cci["cn"] = fullName;

			LdapAttribute attr;

			attr = new LdapAttribute ("cn", fullName);
			attrList.Add (attr);

			SelectContainerDialog scd = 
				new SelectContainerDialog (server, newContactDialog);

			scd.Title = "Save Contact";
			scd.Message = String.Format (
				"Where in the directory would\nyou like save the contact\n{0}?",
				fullName);

			scd.Run ();

			if (scd.DN == "")
				return;

			string userDN = String.Format ("cn={0},{1}", fullName, scd.DN);

			if (!Util.AddEntry (server, viewDialog, userDN, attrList, true)) {
				errorOccured = true;
				return;
			}

			newContactDialog.HideAll ();
		}
	}
}
