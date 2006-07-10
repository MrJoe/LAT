// 
// lat - NewAdUserViewDialog.cs
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
using System.Security.Cryptography;
using System.Text;
using Mono.Security.Protocol.Ntlm;
using Novell.Directory.Ldap;

namespace lat
{
	public class NewAdUserViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newAdUserDialog;

		[Glade.Widget] Gtk.Label usernameLabel;
		[Glade.Widget] Gtk.Label fullnameLabel;

		[Glade.Widget] Gtk.Entry upnEntry;
		[Glade.Widget] Gtk.Entry usernameEntry;
		[Glade.Widget] Gtk.Entry firstNameEntry;
		[Glade.Widget] Gtk.Entry initialsEntry;
		[Glade.Widget] Gtk.Entry lastNameEntry;
		[Glade.Widget] Gtk.Entry displayNameEntry;
		[Glade.Widget] Gtk.Entry passwordEntry;
		[Glade.Widget] Gtk.HBox comboHbox;
		[Glade.Widget] Gtk.CheckButton mustChangePwdCheckButton;
		[Glade.Widget] Gtk.CheckButton cantChangePwdCheckButton;
		[Glade.Widget] Gtk.CheckButton pwdNeverExpiresCheckButton;
		[Glade.Widget] Gtk.CheckButton accountDisabledCheckButton;

		static string[] userAttrs = { "givenName", "sn", "sAMAccountName",
			"userAccountControl", "initials", "loginShell", "cn", "geos",
			"displayName", "userPrincipalName" };

		// ACCOUNT_DISABLE|NORMAL_ACCOUNT|DONT_EXPIRE_PASSWORD
		private int userAC = 66050;

		string[] groupList;
		ComboBox primaryGroupComboBox;

		public NewAdUserViewDialog (LdapServer ldapServer, string newContainer) : base (ldapServer, newContainer)
		{
			Init ();		

			groupList = GetGroups ();

			createCombo ();

			newAdUserDialog.Icon = Global.latIcon;
			newAdUserDialog.Run ();

			while (missingValues || errorOccured) {

				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				newAdUserDialog.Run ();				
			}

			newAdUserDialog.Destroy ();
		}

		public void OnMustChangePwdToggled (object o, EventArgs args)
		{
			cantChangePwdCheckButton.Active = false;
		}

		public void OnCantChangePwdToggled (object o, EventArgs args)
		{
			mustChangePwdCheckButton.Active = false;
		}

		public void OnPwdNeverExpiresToggled (object o, EventArgs args)
		{
			mustChangePwdCheckButton.Active = false;			
		}

		public void OnAccountDisabledToggled (object o, EventArgs args)
		{
		}

		private string[] GetGroups ()
		{
			LdapEntry[] grps = server.SearchByClass ("group");
			List<string> glist = new List<string> ();
	
			foreach (LdapEntry e in grps) {

				LdapAttribute nameAttr;
				nameAttr = e.getAttribute ("cn");

				glist.Add (nameAttr.StringValue);
			}

			return glist.ToArray ();
		}

		private void createCombo ()
		{
			primaryGroupComboBox = ComboBox.NewText ();

			foreach (string n in groupList)
				primaryGroupComboBox.AppendText (n);

			primaryGroupComboBox.Active = 0;
			primaryGroupComboBox.Show ();

			// FIXME: primary group
			primaryGroupComboBox.Sensitive = false;

			comboHbox.Add (primaryGroupComboBox);
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "newAdUserDialog", null);
			ui.Autoconnect (this);

			viewDialog = newAdUserDialog;

			// FIXME: need SSL to set the password
			passwordEntry.Sensitive = false;
			mustChangePwdCheckButton.Sensitive = false;
			cantChangePwdCheckButton.Sensitive = false;
			pwdNeverExpiresCheckButton.Sensitive = false;
			accountDisabledCheckButton.Sensitive = false;

			displayNameEntry.FocusInEvent += new FocusInEventHandler (OnDisplayNameFocusIn);
		}

		public void OnNameChanged (object o, EventArgs args)
		{
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", usernameEntry.Text);

			fullnameLabel.Text = String.Format ("{0} {1}", firstNameEntry.Text, lastNameEntry.Text);
		}
	
		private void OnDisplayNameFocusIn (object o, EventArgs args)
		{
			string suid = Util.SuggestUserName (
					firstNameEntry.Text, 
					lastNameEntry.Text);

			usernameEntry.Text = suid;

			if (displayNameEntry.Text != "")
				return;

			if (initialsEntry.Text.Equals("")) {

				displayNameEntry.Text = String.Format ("{0} {1}", 
					firstNameEntry.Text, 
					lastNameEntry.Text);

			} else {

				String format = "";
				if (initialsEntry.Text.EndsWith("."))
					format = "{0} {1} {2}";
				else
					format = "{0} {1}. {2}";

				displayNameEntry.Text = String.Format (format, 
					firstNameEntry.Text, 
					initialsEntry.Text, 
					lastNameEntry.Text);
			}
		}
			
		Dictionary<string,string> getUpdatedUserInfo ()
		{
			Dictionary<string,string> retVal = new Dictionary<string,string> ();
//			TreeIter iter;
//			string pg = "";
				
//			if (primaryGroupComboBox.GetActiveIter (out iter))
//			{
//				pg = (string) primaryGroupComboBox.Model.GetValue (iter, 0);
//			}

			retVal.Add ("givenName", firstNameEntry.Text);
			retVal.Add ("sn", lastNameEntry.Text);
			retVal.Add ("userPrincipalName", upnEntry.Text);
			retVal.Add ("sAMAccountName", usernameEntry.Text);
			retVal.Add ("userAccountControl", userAC.ToString());
			retVal.Add ("displayName", displayNameEntry.Text);
			retVal.Add ("initials", initialsEntry.Text);

			return retVal;
		}
	
		public void OnOkClicked (object o, EventArgs args)
		{
			Dictionary<string,string> cui = getUpdatedUserInfo ();

			string[] objClass = { "top", "person", 
				"organizationalPerson","user" };

			string[] missing = null;

			if (!checkReqAttrs (objClass, cui, out missing)) {

				missingAlert (missing);
				missingValues = true;

				return;
			}

			string fullName = cui["displayName"];

			cui["cn"] = fullName;
			cui["gecos"] = fullName;
			cui.Remove ("displayName");

			List<LdapAttribute> attrList = getAttributes (objClass, userAttrs, cui);

			string userDN = null;			
			if (this.defaultNewContainer == null) {
			
				SelectContainerDialog scd = new SelectContainerDialog (server, newAdUserDialog);
				scd.Title = "Save User";
				scd.Message = String.Format (
					"Where in the directory would\nyou like save the user\n{0}?",
					fullName);

				scd.Run ();

				if (scd.DN == "")
					return;

				userDN = String.Format ("cn={0},{1}", fullName, scd.DN);
				
			} else {
			
				userDN = String.Format ("cn={0},{1}", fullName, this.defaultNewContainer);
			}

			if (!Util.AddEntry (server, viewDialog, userDN, attrList, true)) {
				errorOccured = true;
				return;
			}

			newAdUserDialog.HideAll ();
		}
	}
}
