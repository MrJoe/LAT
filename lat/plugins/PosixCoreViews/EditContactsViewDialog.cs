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
using System.Collections.Generic;
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

		bool isPosix;		
		LdapEntry currentEntry;

		public EditContactsViewDialog (LdapServer ldapServer, LdapEntry le) : base (ldapServer, null) 
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
			
			adPOBoxEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "postOfficeBox");
			adCityEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "l");
			adStateEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "st");
			adZipEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "postalCode");
			

			tnHomeEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "homePhone");
			tnPagerEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "pager");
			tnMobileEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "mobile");
			tnFaxEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "facsimileTelephoneNumber");
			
			ozTitleEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "title");

			string contactName = server.GetAttributeValueFromEntry (currentEntry, "cn");
			editContactDialog.Title = contactName + " Properties";

			if (!isPosix) {

				gnWebPageEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "wWWHomePage");

				adStreetTextView.Buffer.Text = server.GetAttributeValueFromEntry (currentEntry, "streetAddress");
				adCountryEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "co");

				tnIPPhoneEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "ipPhone");
				tnNotesTextView.Buffer.Text = server.GetAttributeValueFromEntry (currentEntry, "info");

				ozDeptEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "department");
				ozCompanyEntry.Text = server.GetAttributeValueFromEntry (currentEntry, "company");

			} else {

				server.GetAttributeValueFromEntry (currentEntry, "street");
			}

			editContactDialog.Icon = Global.latIcon;
			editContactDialog.Run ();

			while (missingValues || errorOccured) {

				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				editContactDialog.Run ();
			}

			editContactDialog.Destroy ();
		}

		void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "editContactDialog", null);
			ui.Autoconnect (this);

			viewDialog = editContactDialog;

			switch (server.ServerType) {

			case LdapServerType.ActiveDirectory:
				isPosix = false;
				break;

			case LdapServerType.OpenLDAP:
			case LdapServerType.FedoraDirectory:
			case LdapServerType.Generic:
			default:
				isPosix = true;
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

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();
			aset.Add (new LdapAttribute ("givenName", gnFirstNameEntry.Text));
			aset.Add (new LdapAttribute ("initials", gnInitialsEntry.Text));
			aset.Add (new LdapAttribute ("sn", gnLastNameEntry.Text));
			aset.Add (new LdapAttribute ("displayName", gnDisplayName.Text));
			aset.Add (new LdapAttribute ("cn", gnDisplayName.Text));
			aset.Add (new LdapAttribute ("wWWHomePage", gnWebPageEntry.Text));
			aset.Add (new LdapAttribute ("physicalDeliveryOfficeName", gnOfficeEntry.Text));
			aset.Add (new LdapAttribute ("mail", gnEmailEntry.Text));
			aset.Add (new LdapAttribute ("description", gnDescriptionEntry.Text));
			aset.Add (new LdapAttribute ("street", adStreetTextView.Buffer.Text));
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
			aset.Add (new LdapAttribute ("title", ozTitleEntry.Text));
			aset.Add (new LdapAttribute ("department", ozDeptEntry.Text));
			aset.Add (new LdapAttribute ("company", ozCompanyEntry.Text));
						
			if (!isPosix) {
				aset.Add (new LdapAttribute ("streetAddress", adStreetTextView.Buffer.Text));
				aset.Add (new LdapAttribute ("info", tnNotesTextView.Buffer.Text));
				aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "person", "organizationalPerson", "contact" }));
			} else {
				aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "person", "inetOrgPerson" }));
			}
					
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
