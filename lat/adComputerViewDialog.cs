// 
// lat - adComputerViewDialog.cs
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
	public class adComputerViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog adComputerDialog;
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

		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private bool _isEdit;
		
		private LdapEntry _le;
		private ArrayList _modList;
		private Hashtable _hi;

		private static string[] hostAttrs = { "cn", "description", "dNSHostName", 
						"operatingSystem", "operatingSystemVersion",
						"operatingSystemServicePack", "location", 
						"managedBy"};

		public adComputerViewDialog (lat.Connection conn) : base (conn)
		{
			Init ();

			adComputerDialog.Title = "LAT - Add Host";

			adComputerDialog.Run ();
			adComputerDialog.Destroy ();
		}

		public adComputerViewDialog (lat.Connection conn, LdapEntry le) : base (conn)
		{
			_le = le;
			_modList = new ArrayList ();

			_isEdit = true;

			Init ();

			_hi = getEntryInfo (hostAttrs, le);

			computerNameLabel.Text = (string) _hi["cn"];
		
			string cpName = (string) _hi["cn"];
			computerNameEntry.Text = cpName.ToUpper();

			adComputerDialog.Title = cpName + " Properties";

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
				LdapEntry leMan = conn.getEntry (manName);

				manOfficeLabel.Text = getAttribute (leMan, "physicalDeliveryOfficeName");
				manStreetTextView.Buffer.Text = getAttribute (leMan, "streetAddress");
				manCityLabel.Text = getAttribute (leMan, "l");
				manStateLabel.Text = getAttribute (leMan, "st");
				manCountryLabel.Text = getAttribute (leMan, "c");
				manTelephoneNumberLabel.Text = getAttribute (leMan, "telephoneNumber");
				manFaxNumberLabel.Text = getAttribute (leMan, "facsimileTelephoneNumber");
			}

			adComputerDialog.Run ();
			adComputerDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "adComputerDialog", null);
			ui.Autoconnect (this);

			_viewDialog = adComputerDialog;
		
			computerNameEntry.IsEditable = false;
			dnsNameEntry.IsEditable = false;

			osNameEntry.IsEditable = false;
			osVersionEntry.IsEditable = false;
			osServicePackEntry.IsEditable = false;
			
			manNameEntry.IsEditable = false;
			manStreetTextView.Editable = false;

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			adComputerDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);
		}

		private Hashtable getCurrentHostInfo ()
		{
			Hashtable retVal = new Hashtable ();

			retVal.Add ("description", descriptionEntry.Text);

			return retVal;
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			Hashtable chi = getCurrentHostInfo ();

			string[] missing = null;
			string[] objClass = {"top", "computer"};

			if (!checkReqAttrs (objClass, chi, out missing))
			{
				missingAlert (missing);
				return;
			}

			if (_isEdit)
			{
				_modList = getMods (hostAttrs, _hi, chi);

				Util.ModifyEntry (_conn, _viewDialog, _le.DN, _modList);
			}
			else
			{
				ArrayList attrList = getAttributes (objClass, hostAttrs, chi);

				SelectContainerDialog scd = 
					new SelectContainerDialog (_conn, adComputerDialog);

				scd.Title = "Save Computer";
				scd.Message = String.Format ("Where in the directory would\nyou like save the computer\n{0}?", (string)chi["cn"]);

				scd.Run ();

				if (scd.DN == "")
					return;

				string userDN = String.Format ("cn={0},{1}", (string)chi["cn"], scd.DN);

				Util.AddEntry (_conn, _viewDialog, userDN, attrList);
			}

			adComputerDialog.HideAll ();
		}
	}
}
