// 
// lat - adUserViewDialog.cs
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
	public class adUserViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog adUserDialog;

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

		[Glade.Widget] Gtk.Entry accLoginNameEntry;

		[Glade.Widget] Gtk.Entry proPathEntry;
		[Glade.Widget] Gtk.Entry proLogonScriptEntry;
		[Glade.Widget] Gtk.Entry proLocalPathEntry;

		private bool _isEdit;
		
		private LdapEntry _le;
		private Dictionary<string,string> _ci;
		private List<LdapModification> _modList;

		// FIXME: "sAMAccountName"

		private static string[] contactAttrs = { "givenName", "sn", "initials", "cn",
					       "physicalDeliveryOfficeName", "description",
					       "mail", "postalAddress", "displayName",
					       "l", "st", "postalCode", "wWWHomePage", "co",
					       "telephoneNumber", "facsimileTelephoneNumber",
				               "pager", "mobile", "homePhone", "streetAddress",
						"company", "department", "ipPhone", "info",
						"title", "postOfficeBox", "homeDirectory",
						"profilePath", "scriptPath", "userPrincipalName" };

		public adUserViewDialog (LdapServer ldapServer, string newContainer) : base (ldapServer, newContainer)
		{
			Init ();

			adUserDialog.Title = "LAT - Add Contact";

			adUserDialog.Run ();

			while (missingValues || errorOccured) {

				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				adUserDialog.Run ();				
			}

			adUserDialog.Destroy ();
		}

		public adUserViewDialog (LdapServer ldapServer, LdapEntry le) : base (ldapServer, null)
		{
			_le = le;
			_modList = new List<LdapModification> ();

			_isEdit = true;

			Init ();

			server.GetAttributeValuesFromEntry (le, contactAttrs, out _ci);

			string displayName = _ci["displayName"];

			gnNameLabel.Text = displayName;
			gnFirstNameEntry.Text = _ci["givenName"];
			gnInitialsEntry.Text = _ci["initials"];
			gnLastNameEntry.Text = _ci["sn"];
			gnDisplayName.Text = displayName;
			gnDescriptionEntry.Text = _ci["description"];
			gnOfficeEntry.Text = _ci["physicalDeliveryOfficeName"];
			gnTelephoneNumberEntry.Text = _ci["telephoneNumber"];
			gnEmailEntry.Text = _ci["mail"];
			gnWebPageEntry.Text = _ci["wWWHomePage"];

			adStreetTextView.Buffer.Text = _ci["streetAddress"];
			adPOBoxEntry.Text = _ci["postOfficeBox"];
			adCityEntry.Text = _ci["l"];
			adStateEntry.Text = _ci["st"];
			adZipEntry.Text = _ci["postalCode"];
			adCountryEntry.Text = _ci["co"];

			tnHomeEntry.Text = _ci["homePhone"];
			tnPagerEntry.Text = _ci["pager"];
			tnMobileEntry.Text = _ci["mobile"];
			tnFaxEntry.Text = _ci["facsimileTelephoneNumber"];
			tnIPPhoneEntry.Text = _ci["ipPhone"];
			tnNotesTextView.Buffer.Text = _ci["info"];

			ozTitleEntry.Text = _ci["title"];
			ozDeptEntry.Text = _ci["department"];
			ozCompanyEntry.Text = _ci["company"];

			accLoginNameEntry.Text = _ci["userPrincipalName"];

			proPathEntry.Text = _ci["profilePath"];
			proLogonScriptEntry.Text = _ci["scriptPath"];
			proLocalPathEntry.Text = _ci["homeDirectory"];

			adUserDialog.Title = _ci["cn"] + " Properties";

			adUserDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				adUserDialog.Run ();				
			}

			adUserDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "adUserDialog", null);
			ui.Autoconnect (this);

			viewDialog = adUserDialog;
			
			adUserDialog.Icon = Global.latIcon;
		}

		public void OnNameChanged (object o, EventArgs args)
		{
			gnNameLabel.Text = gnDisplayName.Text;
		}

		Dictionary<string,string> getCurrentContactInfo ()
		{
			Dictionary<string,string> retVal = new Dictionary<string,string> ();

			retVal.Add ("givenName", gnFirstNameEntry.Text);
			retVal.Add ("initials", gnInitialsEntry.Text);
			retVal.Add ("sn", gnLastNameEntry.Text);
			retVal.Add ("displayName", gnDisplayName.Text);
			retVal.Add ("wWWHomePage", gnWebPageEntry.Text);
			retVal.Add ("physicalDeliveryOfficeName", gnOfficeEntry.Text);
			retVal.Add ("mail", gnEmailEntry.Text);
			retVal.Add ("description", gnDescriptionEntry.Text);
			retVal.Add ("streetAddress", adStreetTextView.Buffer.Text);
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
			retVal.Add ("info", tnNotesTextView.Buffer.Text);
			retVal.Add ("title", ozTitleEntry.Text);
			retVal.Add ("department", ozDeptEntry.Text);
			retVal.Add ("company", ozCompanyEntry.Text);

			retVal.Add ("userPrincipalName", accLoginNameEntry.Text);
			retVal.Add ("profilePath", proPathEntry.Text);
			retVal.Add ("scriptPath", proLogonScriptEntry.Text);
			retVal.Add ("homeDirectory", proLocalPathEntry.Text);

			return retVal;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Dictionary<string,string> cci = getCurrentContactInfo ();

			string[] objClass = {"top", "person", "organizationalPerson", "user" };
			string[] missing = null;

			if (!checkReqAttrs (objClass, cci, out missing)) {
				missingAlert (missing);
				missingValues = true;

				return;
			}

			if (_isEdit) {

				_modList = getMods (contactAttrs, _ci, cci);

				Util.ModifyEntry (server, viewDialog, _le.DN, _modList, true);

			} else {

				List<LdapAttribute> attrList = getAttributes (objClass, contactAttrs, cci);

				string fullName = String.Format ("{0} {1}", 
					(string)cci["givenName"], (string)cci["sn"] );

				cci["cn"] = fullName;

				LdapAttribute attr;

				attr = new LdapAttribute ("cn", fullName);
				attrList.Add (attr);

				SelectContainerDialog scd = 
					new SelectContainerDialog (server, adUserDialog);

				scd.Title = "Save Contact";
				scd.Message = String.Format ("Where in the directory would\nyou like save the contact\n{0}?", fullName);

				scd.Run ();

				if (scd.DN == "")
					return;

				string userDN = String.Format ("cn={0},{1}", fullName, scd.DN);

				if (!Util.AddEntry (server, viewDialog, userDN, attrList, true)) {
					errorOccured = true;
					return;
				}
			}

			adUserDialog.HideAll ();
		}
	}
}
