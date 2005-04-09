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
		[Glade.Widget] Gtk.Label fullNameLabel;
		[Glade.Widget] Gtk.Label commentLabel;
		[Glade.Widget] Gtk.Entry firstNameEntry;
		[Glade.Widget] Gtk.Entry lastNameEntry;
		[Glade.Widget] Gtk.Entry officeEntry;
		[Glade.Widget] Gtk.Entry commentEntry;
		[Glade.Widget] Gtk.Entry addressEntry;
		[Glade.Widget] Gtk.Entry cityEntry;
		[Glade.Widget] Gtk.Entry stateEntry;
		[Glade.Widget] Gtk.Entry postalEntry;
		[Glade.Widget] Gtk.Entry emailEntry;
		[Glade.Widget] Gtk.Entry workNumberEntry;
		[Glade.Widget] Gtk.Entry faxNumberEntry;
		[Glade.Widget] Gtk.Entry pagerNumberEntry;
		[Glade.Widget] Gtk.Entry mobileNumberEntry;
		[Glade.Widget] Gtk.Entry homeNumberEntry;
		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private bool _isEdit;
		
		private LdapEntry _le;
		private Hashtable _ci;
		private ArrayList _modList;

		private static string[] contactAttrs = { "givenName", "sn",
					       "physicalDeliveryOfficeName", "description",
					       "mail", "postalAddress",
					       "l", "st", "postalCode",
					       "telephoneNumber", "facsimileTelephoneNumber",
				               "pager", "mobile", "homePhone" };

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

			firstNameEntry.Text = (string)_ci["givenName"];
			lastNameEntry.Text = (string)_ci["sn"];
			officeEntry.Text = (string)_ci["physicalDeliveryOfficeName"];
			commentEntry.Text = (string)_ci["description"];
			emailEntry.Text = (string)_ci["mail"];
			addressEntry.Text = (string)_ci["postalAddress"];
			cityEntry.Text = (string)_ci["l"];
			stateEntry.Text = (string)_ci["st"];
			postalEntry.Text = (string)_ci["postalCode"];
			workNumberEntry.Text = (string)_ci["telephoneNumber"];
			faxNumberEntry.Text = (string)_ci["facsimileTelephoneNumber"];
			pagerNumberEntry.Text = (string)_ci["pager"];
			mobileNumberEntry.Text = (string)_ci["mobile"];
			homeNumberEntry.Text = (string)_ci["homePhone"];

			string fullName = String.Format ("<span size=\"larger\" weight=\"bold\">{0} {1}</span>", firstNameEntry.Text, lastNameEntry.Text);

			fullNameLabel.Markup = fullName;
			fullNameLabel.UseMarkup = true;

			contactDialog.Title = "LAT - Edit Contact";

			contactDialog.Run ();
			contactDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "contactDialog", null);
			ui.Autoconnect (this);

			_viewDialog = contactDialog;
		
			firstNameEntry.Changed += new EventHandler (OnNameChanged);
			lastNameEntry.Changed += new EventHandler (OnNameChanged);
			commentEntry.Changed += new EventHandler (OnNameChanged);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			contactDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);
		}

		private void OnNameChanged (object o, EventArgs args)
		{
			fullNameLabel.Markup = String.Format ("<span size=\"larger\" weight=\"bold\">{0} {1}</span>", firstNameEntry.Text, lastNameEntry.Text);
			
			commentLabel.Text = commentEntry.Text;
			
		}

		private Hashtable getCurrentContactInfo ()
		{
			Hashtable retVal = new Hashtable ();

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
