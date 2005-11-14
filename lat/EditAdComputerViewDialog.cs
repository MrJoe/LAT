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
using System.Collections;
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

		private LdapEntry _le;
		private ArrayList _modList;
		private Hashtable _hi;

		private static string[] hostAttrs = { "cn", "description", "dNSHostName", 
						"operatingSystem", "operatingSystemVersion",
						"operatingSystemServicePack", "location", 
						"managedBy"};

		public EditAdComputerViewDialog (LdapServer ldapServer, LdapEntry le) : 
						 base (ldapServer)
		{
			_le = le;
			_modList = new ArrayList ();

			Init ();

			server.GetAttributeValuesFromEntry (le, hostAttrs, out _hi);

			computerNameLabel.Text = (string) _hi["cn"];
		
			string cpName = (string) _hi["cn"];
			computerNameEntry.Text = cpName.ToUpper();

			editAdComputerDialog.Title = cpName + " Properties";

			dnsNameEntry.Text = (string) _hi["dNSHostName"];
			descriptionEntry.Text = (string) _hi["description"];
			
			osNameEntry.Text = (string) _hi["operatingSystem"];
			osVersionEntry.Text = (string) _hi["operatingSystemVersion"];
			osServicePackEntry.Text = (string) _hi["operatingSystemServicePack"];

			locationEntry.Text = (string) _hi["location"];

			string manName = (string) _hi["managedBy"];
			manNameEntry.Text = manName;

			if (manName != "" || manName != null)
			{
				updateManagedBy (manName);
			}

			editAdComputerDialog.Run ();

			if (missingValues)
			{
				missingValues = false;
				editAdComputerDialog.Run ();				
			}
			else
			{
				editAdComputerDialog.Destroy ();
			}
		}

		private void updateManagedBy (string dn)
		{
			try
			{
				LdapEntry leMan = server.GetEntry (dn);

				manOfficeLabel.Text = server.GetAttributeValueFromEntry (
					leMan, "physicalDeliveryOfficeName");

				manStreetTextView.Buffer.Text = server.GetAttributeValueFromEntry 
					(leMan, "streetAddress");

				manCityLabel.Text = server.GetAttributeValueFromEntry (
					leMan, "l");

				manStateLabel.Text = server.GetAttributeValueFromEntry (
					leMan, "st");

				manCountryLabel.Text = server.GetAttributeValueFromEntry (
					leMan, "c");

				manTelephoneNumberLabel.Text = server.GetAttributeValueFromEntry 
					(leMan, "telephoneNumber");

				manFaxNumberLabel.Text = server.GetAttributeValueFromEntry (
					leMan, "facsimileTelephoneNumber");
			}
			catch 
			{
				manOfficeLabel.Text = "";
				manStreetTextView.Buffer.Text = "";
				manCityLabel.Text = "";
				manStateLabel.Text = "";
				manCountryLabel.Text = "";
				manTelephoneNumberLabel.Text = "";
				manFaxNumberLabel.Text = "";
			}
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "editAdComputerDialog", null);
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
		}

		private Hashtable getCurrentHostInfo ()
		{
			Hashtable retVal = new Hashtable ();

			retVal.Add ("description", descriptionEntry.Text);
			retVal.Add ("managedBy", manNameEntry.Text);

			return retVal;
		}

		public void OnManClearClicked (object o, EventArgs args)
		{
			manNameEntry.Text = "";
			updateManagedBy ("none");
		}

		public void OnManChangeClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = 
				new SelectContainerDialog (server, editAdComputerDialog);

			scd.Title = "Save Computer";
			scd.Message = Mono.Unix.Catalog.GetString (
					"Select a user who will manage ") + 
				(string) _hi["cn"];

			scd.Run ();

			if (scd.DN == "")
			{
				return;
			}
			else
			{
				manNameEntry.Text = scd.DN;
				updateManagedBy (scd.DN);
			}
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Hashtable chi = getCurrentHostInfo ();

			string[] missing = null;
			string[] objClass = {"top", "computer"};

			if (!checkReqAttrs (objClass, chi, out missing))
			{
				missingAlert (missing);
				missingValues = true;

				return;
			}

			_modList = getMods (hostAttrs, _hi, chi);

			Util.ModifyEntry (server, viewDialog, _le.DN, _modList, true);

			editAdComputerDialog.HideAll ();
		}
	}
}
