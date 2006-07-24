// 
// lat - EditAdUserViewDialog.cs
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
	public class EditAdUserViewDialog : ViewDialog
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

		LdapEntry currentEntry;

		public EditAdUserViewDialog (LdapServer ldapServer, LdapEntry le) : base (ldapServer, null)
		{
			currentEntry = le;

			Init ();

			string displayName = server.GetAttributeValueFromEntry (currentEntry, "displayName");

			gnNameLabel.Text = displayName;
			gnFirstNameEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "givenName");
			gnInitialsEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "initials");
			gnLastNameEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "sn");
			gnDisplayName.Text = displayName;
			gnDescriptionEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "description");
			gnOfficeEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "physicalDeliveryOfficeName");
			gnTelephoneNumberEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "telephoneNumber");
			gnEmailEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "mail");
			gnWebPageEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "wWWHomePage");

			adStreetTextView.Buffer.Text = server.GetAttributeValueFromEntry (currentEntry, "streetAddress");
			adPOBoxEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "postOfficeBox");
			adCityEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "l");
			adStateEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "st");
			adZipEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "postalCode");
			adCountryEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "co");

			tnHomeEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "homePhone");
			tnPagerEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "pager");
			tnMobileEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "mobile");
			tnFaxEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "facsimileTelephoneNumber");
			tnIPPhoneEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "ipPhone");
			tnNotesTextView.Buffer.Text = server.GetAttributeValueFromEntry (currentEntry, "info");

			ozTitleEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "title");
			ozDeptEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "department");
			ozCompanyEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "company");

			accLoginNameEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "userPrincipalName");

			proPathEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "profilePath");
			proLogonScriptEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "scriptPath");
			proLocalPathEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "homeDirectory");

			adUserDialog.Title = server.GetAttributeValueFromEntry (currentEntry, "cn") + " Properties";

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

		void Init ()
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

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();
			aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "person", "organizationalPerson", "user"}));
			aset.Add (new LdapAttribute ("cn", gnDisplayName.Text));
			aset.Add (new LdapAttribute ("givenName", gnFirstNameEntry.Text));
			aset.Add (new LdapAttribute ("initials", gnInitialsEntry.Text));
			aset.Add (new LdapAttribute ("sn", gnLastNameEntry.Text));
			aset.Add (new LdapAttribute ("displayName", gnDisplayName.Text));
			aset.Add (new LdapAttribute ("wWWHomePage", gnWebPageEntry.Text));			
			aset.Add (new LdapAttribute ("physicalDeliveryOfficeName", gnOfficeEntry.Text));
			aset.Add (new LdapAttribute ("mail", gnEmailEntry.Text));
			aset.Add (new LdapAttribute ("description", gnDescriptionEntry.Text));
			aset.Add (new LdapAttribute ("streetAddress", adStreetTextView.Buffer.Text));
			aset.Add (new LdapAttribute ("l", adCityEntry.Text));
			aset.Add (new LdapAttribute ("st", adStateEntry.Text));
			aset.Add (new LdapAttribute ("postalCode", adZipEntry.Text));
			aset.Add (new LdapAttribute ("postOfficeBox", adPOBoxEntry.Text));
			aset.Add (new LdapAttribute ("co", adCountryEntry.Text));
			aset.Add (new LdapAttribute ("telephoneNumber", gnTelephoneNumberEntry.Text));
			aset.Add (new LdapAttribute ("facsimileTelephoneNumber", tnFaxEntry.Text));
			aset.Add (new LdapAttribute ("pager", tnPagerEntry.Text));
			aset.Add (new LdapAttribute ("mobile", tnMobileEntry.Text));
			aset.Add (new LdapAttribute ("homePhone", tnHomeEntry.Text));
			aset.Add (new LdapAttribute ("ipPhone", tnIPPhoneEntry.Text));
			aset.Add (new LdapAttribute ("info", tnNotesTextView.Buffer.Text));
			aset.Add (new LdapAttribute ("title", ozTitleEntry.Text));
			aset.Add (new LdapAttribute ("department", ozDeptEntry.Text));
			aset.Add (new LdapAttribute ("company", ozCompanyEntry.Text));
			aset.Add (new LdapAttribute ("userPrincipalName", accLoginNameEntry.Text));
			aset.Add (new LdapAttribute ("profilePath", proPathEntry.Text));
			aset.Add (new LdapAttribute ("scriptPath", proLogonScriptEntry.Text));
			aset.Add (new LdapAttribute ("homeDirectory", proLocalPathEntry.Text));
			
			LdapEntry newEntry = new LdapEntry (dn, aset);
			return newEntry;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			LdapEntry entry = null;
			entry = CreateEntry (currentEntry.DN);				 
				 
			LdapEntryAnalyzer lea = new LdapEntryAnalyzer ();
			lea.Run (currentEntry, entry);
				 
			if (lea.Differences.Length == 0)
				return;
				 	
			if (!Util.ModifyEntry (server, entry.DN, lea.Differences))
			 	errorOccured = true;
		}
	}
}
