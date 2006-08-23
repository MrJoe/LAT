// 
// lat - EditAdComputerViewDialog.cs
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
	public class EditAdComputerViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog editAdComputerDialog;
		[Glade.Widget] Gtk.Label computerNameLabel;
		[Glade.Widget] Gtk.Entry computerNameEntry;
		[Glade.Widget] Gtk.Entry dnsNameEntry;
		[Glade.Widget] Gtk.Entry descriptionEntry;

		[Glade.Widget] Gtk.Entry osNameEntry;
		[Glade.Widget] Gtk.Entry osVersionEntry;
		[Glade.Widget] Gtk.Entry osServicePackEntry;

		[Glade.Widget] Gtk.Entry locationEntry;

		[Glade.Widget] Gtk.Entry manNameEntry;
		[Glade.Widget] Gtk.Label manOfficeLabel;
		[Glade.Widget] Gtk.TextView manStreetTextView;
		[Glade.Widget] Gtk.Label manCityLabel;
		[Glade.Widget] Gtk.Label manStateLabel;
		[Glade.Widget] Gtk.Label manCountryLabel;
		[Glade.Widget] Gtk.Label manTelephoneNumberLabel;
		[Glade.Widget] Gtk.Label manFaxNumberLabel;
		[Glade.Widget] Gtk.Image image178;

		LdapEntry currentEntry;

		public EditAdComputerViewDialog (Connection connection, LdapEntry le) : base (connection, null)
		{
			currentEntry = le;

			Init ();

			computerNameLabel.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "cn");
		
			string cpName = (string) conn.Data.GetAttributeValueFromEntry (currentEntry, "cn");
			computerNameEntry.Text = cpName.ToUpper();

			editAdComputerDialog.Title = cpName + " Properties";

			dnsNameEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "dNSHostName");
			descriptionEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "description");
			
			osNameEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "operatingSystem");
			osVersionEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "operatingSystemVersion");
			osServicePackEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "operatingSystemServicePack");

			locationEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "location");

			string manName = conn.Data.GetAttributeValueFromEntry (currentEntry, "managedBy");
			manNameEntry.Text = manName;

			if (manName != "" || manName != null)
				updateManagedBy (manName);

			editAdComputerDialog.Icon = Global.latIcon;
			editAdComputerDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				editAdComputerDialog.Run ();				
			}

			editAdComputerDialog.Destroy ();
		}

		void updateManagedBy (string dn)
		{
			try {

				LdapEntry leMan = conn.Data.GetEntry (dn);

				manOfficeLabel.Text = conn.Data.GetAttributeValueFromEntry (
					leMan, "physicalDeliveryOfficeName");

				manStreetTextView.Buffer.Text = conn.Data.GetAttributeValueFromEntry 
					(leMan, "streetAddress");

				manCityLabel.Text = conn.Data.GetAttributeValueFromEntry (
					leMan, "l");

				manStateLabel.Text = conn.Data.GetAttributeValueFromEntry (
					leMan, "st");

				manCountryLabel.Text = conn.Data.GetAttributeValueFromEntry (
					leMan, "c");

				manTelephoneNumberLabel.Text = conn.Data.GetAttributeValueFromEntry 
					(leMan, "telephoneNumber");

				manFaxNumberLabel.Text = conn.Data.GetAttributeValueFromEntry (
					leMan, "facsimileTelephoneNumber");

			} catch {

				manOfficeLabel.Text = "";
				manStreetTextView.Buffer.Text = "";
				manCityLabel.Text = "";
				manStateLabel.Text = "";
				manCountryLabel.Text = "";
				manTelephoneNumberLabel.Text = "";
				manFaxNumberLabel.Text = "";
			}
		}

		void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "editAdComputerDialog", null);
			ui.Autoconnect (this);

			viewDialog = editAdComputerDialog;
		
			computerNameEntry.Sensitive = false;
//			computerNameEntry.IsEditable = false;

			dnsNameEntry.Sensitive = false;
//			dnsNameEntry.IsEditable = false;

			osNameEntry.Sensitive = false;
			osVersionEntry.Sensitive = false;
			osServicePackEntry.Sensitive = false;
			
			manNameEntry.Sensitive = false;
			manStreetTextView.Sensitive = false;

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-conn.Data-48x48.png");
			image178.Pixbuf = pb;
		}

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();
			aset.Add (new LdapAttribute ("objectClass", new string[] {"computer"}));
			aset.Add (new LdapAttribute ("cn", computerNameLabel.Text));
			aset.Add (new LdapAttribute ("description", descriptionEntry.Text));
			aset.Add (new LdapAttribute ("dNSHostName", dnsNameEntry.Text));
			aset.Add (new LdapAttribute ("operatingSystem", osNameEntry.Text));
			aset.Add (new LdapAttribute ("operatingSystemVersion", osVersionEntry.Text));
			aset.Add (new LdapAttribute ("operatingSystemServicePack", osServicePackEntry.Text));
			aset.Add (new LdapAttribute ("location", locationEntry.Text));
			aset.Add (new LdapAttribute ("managedBy", manNameEntry.Text));
								
			LdapEntry newEntry = new LdapEntry (dn, aset);
			return newEntry;
		}

		public void OnManClearClicked (object o, EventArgs args)
		{
			manNameEntry.Text = "";
			updateManagedBy ("none");
		}

		public void OnManChangeClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = new SelectContainerDialog (conn, editAdComputerDialog);
			scd.Title = "Save Computer";
			scd.Message = Mono.Unix.Catalog.GetString (
					"Select a user who will manage ") + 
					computerNameLabel.Text;

			scd.Run ();

			if (scd.DN == "") {
				return;
			} else {
				manNameEntry.Text = scd.DN;
				updateManagedBy (scd.DN);
			}
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			LdapEntry entry = null;			
			entry = CreateEntry (currentEntry.DN);				 
				 
			LdapEntryAnalyzer lea = new LdapEntryAnalyzer ();
			lea.Run (currentEntry, entry);
				 
			if (lea.Differences.Length == 0)
				return;
				 	
			if (!Util.ModifyEntry (conn, entry.DN, lea.Differences))
				errorOccured = true;
		}
	}
}
