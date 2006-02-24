// 
// lat - EditContactsViewDialog.cs
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
	public class EditContactsViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog editContactDialog;

		[Glade.Widget] Gtk.Label gnNameLabel;
		[Glade.Widget] Gtk.Entry gnFirstNameEntry;
		[Glade.Widget] Gtk.Entry gnInitialsEntry;
		[Glade.Widget] Gtk.Entry gnLastNameEntry;
		[Glade.Widget] Gtk.Entry gnDisplayName;
		[Glade.Widget] Gtk.Entry gnDescriptionEntry;
		[Glade.Widget] Gtk.Entry gnOfficeEntry;
		[Glade.Widget] Gtk.Entry gnTelephoneNumberEntry;
		[Glade.Widget] Gtk.Entry gnEmailEntry;
		[Glade.Widget] Gtk.Entry gnWebPageEntry;

		[Glade.Widget] Gtk.TextView adStreetTextView;
		[Glade.Widget] Gtk.Entry adPOBoxEntry;
		[Glade.Widget] Gtk.Entry adCityEntry;
		[Glade.Widget] Gtk.Entry adStateEntry;
		[Glade.Widget] Gtk.Entry adZipEntry;
		[Glade.Widget] Gtk.Entry adCountryEntry;

		[Glade.Widget] Gtk.Entry tnHomeEntry;
		[Glade.Widget] Gtk.Entry tnPagerEntry;
		[Glade.Widget] Gtk.Entry tnMobileEntry;
		[Glade.Widget] Gtk.Entry tnFaxEntry;
		[Glade.Widget] Gtk.Entry tnIPPhoneEntry;
		[Glade.Widget] Gtk.TextView tnNotesTextView;

		[Glade.Widget] Gtk.Entry ozTitleEntry;
		[Glade.Widget] Gtk.Entry ozDeptEntry;
		[Glade.Widget] Gtk.Entry ozCompanyEntry;

		[Glade.Widget] Gtk.Image image180;

		private bool _isPosix;
		
		private LdapEntry _le;
		private Hashtable _ci;
		private ArrayList _modList;

		private static string[] posixContactAttrs = { "givenName", "sn", "initials", "cn",
					       "physicalDeliveryOfficeName", "description",
					       "mail", "postalAddress", "displayName",
					       "l", "st", "postalCode", 
					       "telephoneNumber", "facsimileTelephoneNumber",
				               "pager", "mobile", "homePhone", "street",
						"title", "postOfficeBox" };

		private static string[] adContactAttrs = { "givenName", "sn", "initials", "cn",
					       "physicalDeliveryOfficeName", "description",
					       "mail", "postalAddress", "displayName",
					       "l", "st", "postalCode", "wWWHomePage", "co",
					       "telephoneNumber", "facsimileTelephoneNumber",
				               "pager", "mobile", "homePhone", "streetAddress",
						"company", "department", "ipPhone", "info",
						"title", "postOfficeBox" };

		private static string[] contactAttrs;

		public EditContactsViewDialog (LdapServer ldapServer, LdapEntry le) : 
			base (ldapServer)
		{
			_le = le;
			_modList = new ArrayList ();

			Init ();

			if (!_isPosix)			
				contactAttrs = adContactAttrs;
			else
				contactAttrs = posixContactAttrs;

			server.GetAttributeValuesFromEntry (le, contactAttrs, out _ci);

			string displayName = (string)_ci["displayName"];

			gnNameLabel.Text = displayName;
			gnFirstNameEntry.Text = (string)_ci["givenName"];
			gnInitialsEntry.Text = (string)_ci["initials"];
			gnLastNameEntry.Text = (string)_ci["sn"];
			gnDisplayName.Text = displayName;
			gnDescriptionEntry.Text = (string)_ci["description"];
			gnOfficeEntry.Text = (string)_ci["physicalDeliveryOfficeName"];
			gnTelephoneNumberEntry.Text = (string)_ci["telephoneNumber"];
			gnEmailEntry.Text = (string)_ci["mail"];
			
			adPOBoxEntry.Text = (string)_ci["postOfficeBox"];
			adCityEntry.Text = (string)_ci["l"];
			adStateEntry.Text = (string)_ci["st"];
			adZipEntry.Text = (string)_ci["postalCode"];
			

			tnHomeEntry.Text = (string)_ci["homePhone"];
			tnPagerEntry.Text = (string)_ci["pager"];
			tnMobileEntry.Text = (string)_ci["mobile"];
			tnFaxEntry.Text = (string)_ci["facsimileTelephoneNumber"];
			
			ozTitleEntry.Text = (string)_ci["title"];

			string contactName = (string) _ci["cn"];
			editContactDialog.Title = contactName + " Properties";

			if (!_isPosix) {

				gnWebPageEntry.Text = (string)_ci["wWWHomePage"];

				adStreetTextView.Buffer.Text = (string)_ci["streetAddress"];
				adCountryEntry.Text = (string)_ci["co"];

				tnIPPhoneEntry.Text = (string)_ci["ipPhone"];
				tnNotesTextView.Buffer.Text = (string)_ci["info"];

				ozDeptEntry.Text = (string)_ci["department"];
				ozCompanyEntry.Text = (string)_ci["company"];

			} else {

				adStreetTextView.Buffer.Text = (string)_ci["street"];
			}

			editContactDialog.Icon = Global.latIcon;
			editContactDialog.Run ();

			while (missingValues) {

				missingValues = false;
				editContactDialog.Run ();
			}

			editContactDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "editContactDialog", null);
			ui.Autoconnect (this);

			viewDialog = editContactDialog;

			switch (server.ServerType) {

			case LdapServerType.ActiveDirectory:
				_isPosix = false;
				break;

			case LdapServerType.OpenLDAP:
			case LdapServerType.Generic:
			default:
				_isPosix = true;
				tnNotesTextView.Sensitive = false;
				ozDeptEntry.Sensitive = false;
				ozCompanyEntry.Sensitive = false;
				gnWebPageEntry.Sensitive = false;
				tnIPPhoneEntry.Sensitive = false;
				adCountryEntry.Sensitive = false;
				break;
			}

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("contact-new-48x48.png");
			image180.Pixbuf = pb;
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
			retVal.Add ("wWWHomePage", gnWebPageEntry.Text);
			retVal.Add ("physicalDeliveryOfficeName", gnOfficeEntry.Text);
			retVal.Add ("mail", gnEmailEntry.Text);
			retVal.Add ("description", gnDescriptionEntry.Text);
			retVal.Add ("street", adStreetTextView.Buffer.Text);			
			retVal.Add ("l", adCityEntry.Text);
			retVal.Add ("st", adStateEntry.Text);
			retVal.Add ("postalCode", adZipEntry.Text);
			retVal.Add ("postOfficeBox", adPOBoxEntry.Text);
			retVal.Add ("co", adCountryEntry.Text);
			retVal.Add ("telephoneNumber", gnTelephoneNumberEntry.Text);
			retVal.Add ("facsimileTelephoneNumber", tnFaxEntry.Text);
			retVal.Add ("pager", tnPagerEntry.Text);
			retVal.Add ("mobile", tnMobileEntry.Text);
			retVal.Add ("homePhone", tnHomeEntry.Text);
			retVal.Add ("ipPhone", tnIPPhoneEntry.Text);
			
			retVal.Add ("title", ozTitleEntry.Text);
			retVal.Add ("department", ozDeptEntry.Text);
			retVal.Add ("company", ozCompanyEntry.Text);

			if (!_isPosix) {
				retVal.Add ("streetAddress", adStreetTextView.Buffer.Text);
				retVal.Add ("info", tnNotesTextView.Buffer.Text);
			}

			return retVal;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cci = getCurrentContactInfo ();

			string[] objClass;
			string[] missing = null;

			if (!_isPosix) {
				objClass = new string[] {"top", "person", "organizationalPerson", "contact" };
				contactAttrs = adContactAttrs;		
			} else {

				contactAttrs = posixContactAttrs;
				objClass = new string[] {"top", "person", "inetOrgPerson" };
			}

			if (!checkReqAttrs (objClass, cci, out missing)) {
				missingAlert (missing);
				missingValues = true;

				return;
			}

			_modList = getMods (contactAttrs, _ci, cci);
			Util.ModifyEntry (server, viewDialog, _le.DN, _modList, true);

			editContactDialog.HideAll ();
		}
	}
}
