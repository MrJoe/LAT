// 
// lat - ContactsViewDialog.cs
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
	public class ContactsViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog contactDialog;

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

		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private bool _isEdit;
		
		private LdapEntry _le;
		private Hashtable _ci;
		private ArrayList _modList;

		private static string[] contactAttrs = { "givenName", "sn", "initials",
					       "physicalDeliveryOfficeName", "description",
					       "mail", "postalAddress", "displayName",
					       "l", "st", "postalCode", "wWWHomePage", "co",
					       "telephoneNumber", "facsimileTelephoneNumber",
				               "pager", "mobile", "homePhone", "streetAddress",
						"company", "department", "ipPhone", "info",
						"title", "postOfficeBox" };

		public ContactsViewDialog (lat.Connection conn) : base (conn)
		{
			Init ();

			contactDialog.Title = "LAT - Add Contact";

			contactDialog.Run ();
			contactDialog.Destroy ();
		}

		public ContactsViewDialog (lat.Connection conn, LdapEntry le) : base (conn)
		{
			_le = le;
			_modList = new ArrayList ();

			_isEdit = true;

			Init ();

			_ci = getEntryInfo (contactAttrs, le);

//			gnNameLabel;
			gnFirstNameEntry.Text = (string)_ci["givenName"];;
			gnInitialsEntry.Text = (string)_ci["initials"];;
			gnLastNameEntry.Text = (string)_ci["sn"];;
			gnDisplayName.Text = (string)_ci["displayName"];;
			gnDescriptionEntry.Text = (string)_ci["description"];
			gnOfficeEntry.Text = (string)_ci["physicalDeliveryOfficeName"];
			gnTelephoneNumberEntry.Text = (string)_ci["telephoneNumber"];
			gnEmailEntry.Text = (string)_ci["mail"];
			gnWebPageEntry.Text = (string)_ci["wWWHomePage"];

			adStreetTextView.Buffer.Text = (string)_ci["streetAddress"];;
			adPOBoxEntry.Text = (string)_ci["postOfficeBox"];
			adCityEntry.Text = (string)_ci["l"];
			adStateEntry.Text = (string)_ci["st"];
			adZipEntry.Text = (string)_ci["postalCode"];
			adCountryEntry.Text = (string)_ci["co"];

			tnHomeEntry.Text = (string)_ci["homePhone"];
			tnPagerEntry.Text = (string)_ci["pager"];
			tnMobileEntry.Text = (string)_ci["mobile"];
			tnFaxEntry.Text = (string)_ci["facsimileTelephoneNumber"];
			tnIPPhoneEntry.Text = (string)_ci["ipPhone"];
			tnNotesTextView.Buffer.Text = (string)_ci["info"];

			ozTitleEntry.Text = (string)_ci["title"];
			ozDeptEntry.Text = (string)_ci["department"];
			ozCompanyEntry.Text = (string)_ci["company"];

			contactDialog.Title = "LAT - Edit Contact";

			contactDialog.Run ();
			contactDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "contactDialog", null);
			ui.Autoconnect (this);

			_viewDialog = contactDialog;
		
//			firstNameEntry.Changed += new EventHandler (OnNameChanged);
//			lastNameEntry.Changed += new EventHandler (OnNameChanged);
//			commentEntry.Changed += new EventHandler (OnNameChanged);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			contactDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);
		}

		private void OnNameChanged (object o, EventArgs args)
		{
//			fullNameLabel.Markup = String.Format ("<span size=\"larger\" weight=\"bold\">{0} {1}</span>", firstNameEntry.Text, lastNameEntry.Text);
			
//			commentLabel.Text = commentEntry.Text;			
		}

		private Hashtable getCurrentContactInfo ()
		{
			Hashtable retVal = new Hashtable ();
/*
			retVal.Add ("givenName", firstNameEntry.Text);
			retVal.Add ("sn", lastNameEntry.Text);
			retVal.Add ("physicalDeliveryOfficeName", officeEntry.Text);
			retVal.Add ("mail", emailEntry.Text);
			retVal.Add ("description", commentEntry.Text);
			retVal.Add ("postalAddress", addressEntry.Text);
			retVal.Add ("l", cityEntry.Text);
			retVal.Add ("st", stateEntry.Text);
			retVal.Add ("postalCode", postalEntry.Text);
			retVal.Add ("telephoneNumber", workNumberEntry.Text);
			retVal.Add ("facsimileTelephoneNumber", faxNumberEntry.Text);
			retVal.Add ("pager", pagerNumberEntry.Text);
			retVal.Add ("mobile", mobileNumberEntry.Text);
			retVal.Add ("homePhone", homeNumberEntry.Text);
*/
			return retVal;
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cci = getCurrentContactInfo ();

			if (_isEdit)
			{
				_modList = getMods (contactAttrs, _ci, cci);

				Util.ModifyEntry (_conn, _viewDialog, _le.DN, _modList);
			}
			else
			{
				string[] objClass = {"top", "inetOrgPerson", "person"};

				ArrayList attrList = getAttributes (objClass, contactAttrs, cci);

				string fullName = String.Format ("{0} {1}", 
					(string)cci["givenName"], (string)cci["sn"] );

				cci["cn"] = fullName;

				string[] missing = null;

				if (!checkReqAttrs (objClass, cci, out missing))
				{
					attrList.Clear ();

					missingAlert (missing);
					return;
				}

				LdapAttribute attr;

				attr = new LdapAttribute ("cn", fullName);
				attrList.Add (attr);

				SelectContainerDialog scd = 
					new SelectContainerDialog (_conn, contactDialog);

				scd.Title = "Save Contact";
				scd.Message = String.Format ("Where in the directory would\nyou like save the contact\n{0}?", fullName);

				scd.Run ();

				if (scd.DN == "")
					return;

				string userDN = String.Format ("cn={0},{1}", fullName, scd.DN);

				Util.AddEntry (_conn, _viewDialog, userDN, attrList);
			}

			contactDialog.HideAll ();
		}
	}
}
