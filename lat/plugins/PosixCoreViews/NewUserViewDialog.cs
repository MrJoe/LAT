// 
// lat - NewUserViewDialog.cs
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
	public class NewUserViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newUserDialog;

		// General 
		[Glade.Widget] Gtk.Label usernameLabel;
		[Glade.Widget] Gtk.Label fullnameLabel;

		[Glade.Widget] Gtk.Entry usernameEntry;
		[Glade.Widget] Gtk.SpinButton uidSpinButton;
		[Glade.Widget] Gtk.Entry firstNameEntry;
		[Glade.Widget] Gtk.Entry initialsEntry;
		[Glade.Widget] Gtk.Entry lastNameEntry;
		[Glade.Widget] Gtk.Entry displayNameEntry;
		[Glade.Widget] Gtk.Entry homeDirEntry;
		[Glade.Widget] Gtk.Entry shellEntry;
		[Glade.Widget] Gtk.Entry passwordEntry;
		[Glade.Widget] Gtk.HBox comboHbox;
		[Glade.Widget] Gtk.CheckButton enableSambaButton;

		Dictionary<string,LdapEntry> _allGroups;
		Dictionary<string,string> _allGroupGids;
		Dictionary<string,string> _memberOfGroups;

		string smbSID = "";
		string smbLM = "";
		string smbNT = "";

		ComboBox primaryGroupComboBox;

		public NewUserViewDialog (Connection connection, string newContainer) : base (connection, newContainer)
		{
			Init ();		

			getGroups ();

			createCombo ();

			uidSpinButton.Value = conn.Data.GetNextUID ();
			enableSambaButton.Toggled += new EventHandler (OnSambaChanged);

			newUserDialog.Icon = Global.latIcon;
			newUserDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				newUserDialog.Run ();				
			}

			newUserDialog.Destroy ();
		}

		void OnSambaChanged (object o, EventArgs args)
		{
			if (enableSambaButton.Active) {
				smbSID = conn.Data.GetLocalSID ();

				if (smbSID == null) {
					Util.DisplaySambaSIDWarning (newUserDialog);
					enableSambaButton.Active = false;
					return;
				}
			}
		}
		
		void getGroups ()
		{
			LdapEntry[] grps = conn.Data.SearchByClass ("posixGroup");

			foreach (LdapEntry e in grps) {

				LdapAttribute nameAttr, gidAttr;
				nameAttr = e.getAttribute ("cn");
				gidAttr = e.getAttribute ("gidNumber");

				_allGroups.Add (nameAttr.StringValue, e);
				_allGroupGids.Add (gidAttr.StringValue, nameAttr.StringValue);
			}
				
		}

		void createCombo ()
		{
			if (primaryGroupComboBox != null) {
				primaryGroupComboBox.Changed -= OnPrimaryGroupChanged;
				primaryGroupComboBox.Destroy ();
				primaryGroupComboBox = null;
			}
			
			primaryGroupComboBox = ComboBox.NewText ();

			foreach (string key in _allGroups.Keys)
				primaryGroupComboBox.AppendText (key);

			primaryGroupComboBox.AppendText ("Create new group...");

			primaryGroupComboBox.Active = 0;
			primaryGroupComboBox.Changed += OnPrimaryGroupChanged;
			primaryGroupComboBox.Show ();

			comboHbox.Add (primaryGroupComboBox);
		}

		void OnPrimaryGroupChanged (object o, EventArgs args)
		{
			ComboBox combo = o as ComboBox;
			if (o == null)
				return;
				
			TreeIter iter;
			if (combo.GetActiveIter (out iter)) {
				string selection = (string) combo.Model.GetValue (iter, 0);				
				if (selection == "Create new group...") {
					new GroupsViewDialog (conn, "");
					
					_allGroups.Clear();
					_allGroupGids.Clear();
					getGroups ();
					
					createCombo ();
				}
			}
		}

		void Init ()
		{
			_memberOfGroups = new Dictionary<string,string> ();
			_allGroups = new Dictionary<string,LdapEntry> ();
			_allGroupGids = new Dictionary<string,string> ();

			ui = new Glade.XML (null, "dialogs.glade", "newUserDialog", null);
			ui.Autoconnect (this);

			viewDialog = newUserDialog;

			passwordEntry.Sensitive = false;

			displayNameEntry.FocusInEvent += new FocusInEventHandler (OnDisplayNameFocusIn);
		}

		public void OnNameChanged (object o, EventArgs args)
		{
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", usernameEntry.Text);

			fullnameLabel.Text = String.Format ("{0} {1}", firstNameEntry.Text, lastNameEntry.Text);
		}

		public void OnPasswordClicked (object o, EventArgs args)
		{
			PasswordDialog pd = new PasswordDialog ();

			if (!passwordEntry.Text.Equals ("") && pd.UnixPassword.Equals (""))
				return;

			passwordEntry.Text = pd.UnixPassword;
			smbLM = pd.LMPassword;
			smbNT = pd.NTPassword;
		}
		
		void OnDisplayNameFocusIn (object o, EventArgs args)
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

			if (homeDirEntry.Text.Equals("") && !usernameEntry.Text.Equals(""))
				homeDirEntry.Text = String.Format("/home/{0}", usernameEntry.Text);
		}
			
		void modifyGroup (LdapEntry groupEntry, LdapModification[] mods)
		{
			if (groupEntry == null)
				return;

			try {
				conn.Data.Modify (groupEntry.DN, mods);

			} catch (Exception e) {

				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to modify group ") + groupEntry.DN;

				errorMsg += "\nError: " + e.Message;

				HIGMessageDialog dialog = new HIGMessageDialog (
					newUserDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Modify error",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		void updateGroupMembership ()
		{
			LdapEntry groupEntry = null;
			LdapModification[] mods = new LdapModification [1];

			foreach (string key in _memberOfGroups.Keys) {

				LdapAttribute attr = new LdapAttribute ("memberUid", usernameEntry.Text);
				LdapModification lm = new LdapModification (LdapModification.ADD, attr);

				groupEntry = (LdapEntry) _allGroups[key];

				mods[0] = lm;
			}

			modifyGroup (groupEntry, mods);
		}

		string getGidNumber (string name)
		{
			if (name == null)
				return null;

			LdapEntry le = (LdapEntry) _allGroups [name];		
			LdapAttribute attr = le.getAttribute ("gidNumber");

			if (attr != null)
				return attr.StringValue;
			
			return null;
		}

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();	

			TreeIter iter;
				
			if (primaryGroupComboBox.GetActiveIter (out iter)) {
				string pg = (string) primaryGroupComboBox.Model.GetValue (iter, 0);
				aset.Add (new LdapAttribute ("gidNumber", getGidNumber(pg)));
			}
						
			aset.Add (new LdapAttribute ("givenName", firstNameEntry.Text));
			aset.Add (new LdapAttribute ("sn", lastNameEntry.Text));
			aset.Add (new LdapAttribute ("uid", usernameEntry.Text));
			aset.Add (new LdapAttribute ("uidNumber", uidSpinButton.Value.ToString()));
			aset.Add (new LdapAttribute ("userPassword", passwordEntry.Text));
			aset.Add (new LdapAttribute ("loginShell", shellEntry.Text));
			aset.Add (new LdapAttribute ("homeDirectory", homeDirEntry.Text));
			aset.Add (new LdapAttribute ("displayName", displayNameEntry.Text));
			aset.Add (new LdapAttribute ("cn", displayNameEntry.Text));
			aset.Add (new LdapAttribute ("gecos", displayNameEntry.Text));
			
			if (initialsEntry.Text != "")
				aset.Add (new LdapAttribute ("initials", initialsEntry.Text));

			if (enableSambaButton.Active) {

				aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "posixaccount", "shadowaccount","inetorgperson", "person", "sambaSAMAccount"}));
				
				int user_rid = Convert.ToInt32 (uidSpinButton.Value) * 2 + 1000;
				LdapAttribute[] tmp = Util.CreateSambaAttributes (user_rid, smbSID, smbLM, smbNT);
				foreach (LdapAttribute a in tmp)
					aset.Add (a);					

			} else {
			
				aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "posixaccount", "shadowaccount","inetorgperson", "person"}));
			}
					
			LdapEntry newEntry = new LdapEntry (dn, aset);
			return newEntry;
		}

		bool IsUserNameAvailable ()
		{
			if (!Util.CheckUserName (conn, usernameEntry.Text)) {
				string format = Mono.Unix.Catalog.GetString (
					"A user with the username '{0}' already exists!");

				string msg = String.Format (format, usernameEntry.Text);

				HIGMessageDialog dialog = new HIGMessageDialog (
					newUserDialog,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"User error",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				return false;
			}
			
			return true;
		}

		bool IsUIDAvailable ()
		{
			if (!Util.CheckUID (conn, Convert.ToInt32 (uidSpinButton.Value))) {
				string msg = Mono.Unix.Catalog.GetString (
					"The UID you have selected is already in use!");

				HIGMessageDialog dialog = new HIGMessageDialog (
					newUserDialog,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"User error",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				return false;
			}
			
			return true;
		}

		bool IsPasswordEmpty ()
		{
			if (passwordEntry.Text == "" || passwordEntry.Text == null) {
				string msg = Mono.Unix.Catalog.GetString (
					"You must set a password for the new user");

				HIGMessageDialog dialog = new HIGMessageDialog (
					newUserDialog,
					0,
					Gtk.MessageType.Warning,
					Gtk.ButtonsType.Ok,
					"User error",
					msg);

				dialog.Run ();
				dialog.Destroy ();

				return true;
			}
			
			return false;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			LdapEntry entry = null;
			string userDN = null;

			if (!IsUserNameAvailable() || !IsUIDAvailable() || IsPasswordEmpty()) {
				errorOccured = true;
				return;
			}
			
			if (this.defaultNewContainer == null) {
			
				SelectContainerDialog scd =	new SelectContainerDialog (conn, newUserDialog);
				scd.Title = "Save Group";
				scd.Message = String.Format ("Where in the directory would\nyou like save the user\n{0}", displayNameEntry.Text);
				scd.Run ();

				if (scd.DN == "")
					return;

				userDN = String.Format ("cn={0},{1}", displayNameEntry.Text, scd.DN);
			
			} else {
			
				userDN = String.Format ("cn={0},{1}", displayNameEntry.Text, this.defaultNewContainer);
			}
			
			entry = CreateEntry (userDN);

			string[] missing = LdapEntryAnalyzer.CheckRequiredAttributes (conn, entry);
			if (missing.Length != 0) {
				missingAlert (missing);
				missingValues = true;
				return;
			}

			updateGroupMembership ();

			if (!Util.AddEntry (conn, entry))
				errorOccured = true;			
		}
	}
}
