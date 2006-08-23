// 
// lat - EditUserViewDialog.cs
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
using Novell.Directory.Ldap;

namespace lat
{
	public class EditUserViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog editUserDialog;

		// General 
		[Glade.Widget] Gtk.Label usernameLabel;
		[Glade.Widget] Gtk.Label fullnameLabel;

		[Glade.Widget] Gtk.Entry firstNameEntry;
		[Glade.Widget] Gtk.Entry initialsEntry;
		[Glade.Widget] Gtk.Entry lastNameEntry;
		[Glade.Widget] Gtk.Entry descriptionEntry;
		[Glade.Widget] Gtk.Entry officeEntry;

		[Glade.Widget] Gtk.Entry mailEntry;
		[Glade.Widget] Gtk.Entry phoneEntry;

		// Account
		[Glade.Widget] Gtk.Entry usernameEntry;
		[Glade.Widget] Gtk.SpinButton uidSpinButton;
		[Glade.Widget] Gtk.Entry homeDirEntry;
		[Glade.Widget] Gtk.Entry shellEntry;

		[Glade.Widget] Gtk.CheckButton smbEnableSambaButton;
		[Glade.Widget] Gtk.Entry smbLoginScriptEntry;
		[Glade.Widget] Gtk.Entry smbProfilePathEntry;
		[Glade.Widget] Gtk.Entry smbHomePathEntry;
		[Glade.Widget] Gtk.Entry smbHomeDriveEntry;
		[Glade.Widget] Gtk.Entry smbExpireEntry;
		[Glade.Widget] Gtk.Entry smbCanChangePwdEntry;
		[Glade.Widget] Gtk.Entry smbMustChangePwdEntry;
		[Glade.Widget] Gtk.Button smbSetExpireButton;
		[Glade.Widget] Gtk.Button smbSetCanButton;
		[Glade.Widget] Gtk.Button smbSetMustButton;

		// Groups
		[Glade.Widget] Gtk.Label primaryGroupLabel;
		[Glade.Widget] Gtk.TreeView memberOfTreeview;

		// Address
		[Glade.Widget] Gtk.TextView adStreetTextView;
		[Glade.Widget] Gtk.Entry adPOBoxEntry;
		[Glade.Widget] Gtk.Entry adCityEntry;
		[Glade.Widget] Gtk.Entry adStateEntry;
		[Glade.Widget] Gtk.Entry adZipEntry;

		// Telephones
		[Glade.Widget] Gtk.Entry tnHomeEntry;
		[Glade.Widget] Gtk.Entry tnPagerEntry;
		[Glade.Widget] Gtk.Entry tnMobileEntry;
		[Glade.Widget] Gtk.Entry tnFaxEntry;
		[Glade.Widget] Gtk.Entry tnIPPhoneEntry;

		// Organization
		[Glade.Widget] Gtk.Entry ozTitleEntry;
		[Glade.Widget] Gtk.Entry ozDeptEntry;
		[Glade.Widget] Gtk.Entry ozCompanyEntry;

		bool isSamba = false;
		bool firstTimeSamba = false;
		string pass = "";
		string smbLM = "";
		string smbNT = "";
		string smbSID = "";
		bool passChanged = false;
		
		LdapEntry currentEntry;

		Dictionary<string,LdapEntry> _allGroups;
		Dictionary<string,string> _allGroupGids;
		Dictionary<string,LdapModification> _modsGroup;
		Dictionary<string,string> _memberOfGroups;

		ListStore _memberOfStore;

		public EditUserViewDialog (Connection conn, LdapEntry le) : base (conn, null)
		{
			currentEntry = le;

			Init ();

			isSamba = Util.CheckSamba (currentEntry);
			if (!isSamba)
				firstTimeSamba = true;

			getGroups (currentEntry);

			string userName = conn.Data.GetAttributeValueFromEntry (currentEntry, "cn");
			editUserDialog.Title = userName + " Properties";

			// General
			usernameLabel.UseMarkup = true;
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", conn.Data.GetAttributeValueFromEntry (currentEntry, "uid"));

			fullnameLabel.Text = String.Format ("{0} {1}", 
				conn.Data.GetAttributeValueFromEntry (currentEntry, "givenName"),
				conn.Data.GetAttributeValueFromEntry (currentEntry, "sn"));

			firstNameEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "givenName");
			initialsEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "initials");
			lastNameEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "sn");
			descriptionEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "description");
			officeEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "physicalDeliveryOfficeName");
			mailEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "mail");
			phoneEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "telephoneNumber");

			// Account
			usernameEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "uid");
			uidSpinButton.Value = int.Parse (conn.Data.GetAttributeValueFromEntry (currentEntry, "uidNumber"));
			shellEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "loginShell");;
			homeDirEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "homeDirectory");

			if (isSamba) {
				toggleSambaWidgets (true);
				smbEnableSambaButton.Hide ();

				smbLoginScriptEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "sambaLogonScript");
				smbProfilePathEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "sambaProfilePath");
				smbHomePathEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "sambaHomePath");
				smbHomeDriveEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "sambaHomeDrive");
				smbExpireEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "sambaKickoffTime");
				smbCanChangePwdEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "sambaPwdCanChange");
				smbMustChangePwdEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "sambaPwdMustChange");

			} else {

				smbEnableSambaButton.Toggled += new EventHandler (OnSambaChanged);
				toggleSambaWidgets (false);
			}

			// Groups
			string pgid = conn.Data.GetAttributeValueFromEntry (currentEntry, "gidNumber");
			string pname = _allGroupGids [pgid];		
			primaryGroupLabel.Text = pname;			

			// Address
			adStreetTextView.Buffer.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "street");
			adPOBoxEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "postOfficeBox");
			adCityEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "l");
			adStateEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "st");
			adZipEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "postalCode");

			// Telephones
			tnHomeEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "homePhone");
			tnPagerEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "pager");
			tnMobileEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "mobile");
			tnFaxEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "facsimileTelephoneNumber");

			// Organization
			ozTitleEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "title");
			ozDeptEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "departmentNumber");
			ozCompanyEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "o");

			editUserDialog.Icon = Global.latIcon;
			editUserDialog.Run ();

			while (missingValues || errorOccured) {

				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				editUserDialog.Run ();				
			}

			editUserDialog.Destroy ();
		}
	
		void OnSambaChanged (object o, EventArgs args)
		{
			if (smbEnableSambaButton.Active) {

				smbSID = conn.Data.GetLocalSID ();

				if (smbSID == null) {
					Util.DisplaySambaSIDWarning (editUserDialog);
					smbEnableSambaButton.Active = false;
					return;
				}

				toggleSambaWidgets (true);
			} else {

				toggleSambaWidgets (false);
			}
		}

		bool checkMemberOf (string user, string[] members)
		{
			foreach (string s in members)
				if (s.Equals (user))
					return true;
	
			return false;			
		}

		void getGroups (LdapEntry le)
		{
			LdapEntry[] grps = conn.Data.SearchByClass ("posixGroup");

			foreach (LdapEntry e in grps) {

				LdapAttribute nameAttr, gidAttr;
				nameAttr = e.getAttribute ("cn");
				gidAttr = e.getAttribute ("gidNumber");

				if (le != null) {

					LdapAttribute a;
					a  = e.getAttribute ("memberUid");
					
					if (a != null) {

						if (checkMemberOf (conn.Data.GetAttributeValueFromEntry (currentEntry, "uid"), a.StringValueArray)
						   && !_memberOfGroups.ContainsKey (nameAttr.StringValue)) {

							_memberOfGroups.Add (nameAttr.StringValue,"memeberUid");
							_memberOfStore.AppendValues (nameAttr.StringValue);
						}
					}
				}

				if (!_allGroups.ContainsKey (nameAttr.StringValue))
					_allGroups.Add (nameAttr.StringValue, e);

				if (!_allGroupGids.ContainsKey (nameAttr.StringValue))
					_allGroupGids.Add (gidAttr.StringValue, nameAttr.StringValue);
			}
				
		}

		void Init ()
		{
			_memberOfGroups = new Dictionary<string,string> ();
			_allGroups = new Dictionary<string,LdapEntry> ();
			_allGroupGids = new Dictionary<string,string> ();
			_modsGroup = new Dictionary<string,LdapModification> ();

			ui = new Glade.XML (null, "dialogs.glade", "editUserDialog", null);
			ui.Autoconnect (this);

			viewDialog = editUserDialog;

			TreeViewColumn col;

			_memberOfStore = new ListStore (typeof (string));
			memberOfTreeview.Model = _memberOfStore;
			memberOfTreeview.Selection.Mode = SelectionMode.Multiple;

			col = memberOfTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			_memberOfStore.SetSortColumnId (0, SortType.Ascending);
		}

		void toggleSambaWidgets (bool state)
		{
			if (state && firstTimeSamba) {
				string msg = Mono.Unix.Catalog.GetString (
					"You must reset the password for this account in order to set a samba password.");

				HIGMessageDialog dialog = new HIGMessageDialog (
						editUserDialog,
						0,
						Gtk.MessageType.Info,
						Gtk.ButtonsType.Ok,
						"Setting a samba password",
						msg);

				dialog.Run ();
				dialog.Destroy ();
			}

			smbLoginScriptEntry.Sensitive = state;
			smbProfilePathEntry.Sensitive = state;
			smbHomePathEntry.Sensitive = state;
			smbHomeDriveEntry.Sensitive = state;
			smbExpireEntry.Sensitive = state;
			smbCanChangePwdEntry.Sensitive = state;
			smbMustChangePwdEntry.Sensitive = state;
			smbSetExpireButton.Sensitive = state;
			smbSetCanButton.Sensitive = state;
			smbSetMustButton.Sensitive = state;
		}

		public void OnAddGroupClicked (object o, EventArgs args)
		{
			List<string> tmp = new List<string> ();
	
			foreach (KeyValuePair<string, LdapEntry> kvp in _allGroups) {
				if (kvp.Key == primaryGroupLabel.Text || _memberOfGroups.ContainsKey (kvp.Key))
					continue;

				tmp.Add (kvp.Key);
			}

			SelectGroupsDialog sgd = new SelectGroupsDialog (tmp.ToArray ());

			foreach (string name in sgd.SelectedGroupNames) {

				_memberOfStore.AppendValues (name);
		
				if (!_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Add (name, "memberUid");

				LdapAttribute attr = new LdapAttribute ("memberUid", conn.Data.GetAttributeValueFromEntry (currentEntry, "uid"));
				LdapModification lm = new LdapModification (LdapModification.ADD, attr);

				_modsGroup.Add (name, lm);

				updateGroupMembership ();

				_modsGroup.Clear ();
			}
		}

		public void OnRemoveGroupClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;
			
			TreePath[] tp = memberOfTreeview.Selection.GetSelectedRows (out model);

			for (int i  = tp.Length; i > 0; i--) {

				_memberOfStore.GetIter (out iter, tp[(i - 1)]);

				string name = (string) _memberOfStore.GetValue (iter, 0);

				_memberOfStore.Remove (ref iter);
		
				if (_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Remove (name);

				LdapAttribute attr = new LdapAttribute ("memberUid", conn.Data.GetAttributeValueFromEntry (currentEntry, "uid"));
				LdapModification lm = new LdapModification (LdapModification.DELETE, attr);

				_modsGroup.Add (name, lm);
			
				updateGroupMembership ();

				_modsGroup.Clear ();
			}
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

			if (pd.UnixPassword.Equals (""))
				return;

			pass = pd.UnixPassword;
			smbLM = pd.LMPassword;
			smbNT = pd.NTPassword;

			passChanged = true;
		}

		public void OnSetPrimaryGroupClicked (object o, EventArgs args)
		{
			List<string> tmp = new List<string> ();
	
			foreach (KeyValuePair<string, LdapEntry> kvp in _allGroups) {

				if (kvp.Key == primaryGroupLabel.Text)
					continue;

				tmp.Add (kvp.Key);
			}

			SelectGroupsDialog sgd = new SelectGroupsDialog (tmp.ToArray());

			if (sgd.SelectedGroupNames.Length > 0)
				primaryGroupLabel.Text = sgd.SelectedGroupNames[0];
		}

		public void OnSetExpireClicked (object o, EventArgs args)
		{
			TimeDateDialog td = new TimeDateDialog ();

			smbExpireEntry.Text = td.UnixTime.ToString ();
		}

		public void OnSetCanClicked (object o, EventArgs args)
		{
			TimeDateDialog td = new TimeDateDialog ();

			smbCanChangePwdEntry.Text = td.UnixTime.ToString ();
		}

		public void OnSetMustClicked (object o, EventArgs args)
		{
			TimeDateDialog td = new TimeDateDialog ();

			smbMustChangePwdEntry.Text = td.UnixTime.ToString ();
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
					editUserDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Error",
					errorMsg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}

		void updateGroupMembership ()
		{
			Log.Debug ("START updateGroupMembership ()");

			LdapEntry groupEntry = null;
			LdapModification[] mods = new LdapModification [_modsGroup.Count];

			int count = 0;

			foreach (string key in _modsGroup.Keys) {

				Log.Debug ("group: {0}", key);

				LdapModification lm = (LdapModification) _modsGroup[key];
				groupEntry = (LdapEntry) _allGroups [key];

				mods[count] = lm;

				count++;
			}	

			modifyGroup (groupEntry, mods);

			Log.Debug ("END updateGroupMembership ()");
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
			
			// General
			aset.Add (new LdapAttribute ("cn", fullnameLabel.Text));
			aset.Add (new LdapAttribute ("displayName", fullnameLabel.Text));
			aset.Add (new LdapAttribute ("gecos", fullnameLabel.Text));
			aset.Add (new LdapAttribute ("givenName", firstNameEntry.Text));
			aset.Add (new LdapAttribute ("initials", initialsEntry.Text));
			aset.Add (new LdapAttribute ("sn", lastNameEntry.Text));
			aset.Add (new LdapAttribute ("description", descriptionEntry.Text));
			aset.Add (new LdapAttribute ("physicalDeliveryOfficeName", officeEntry.Text));
			aset.Add (new LdapAttribute ("mail", mailEntry.Text));
			aset.Add (new LdapAttribute ("telephoneNumber", phoneEntry.Text));

			// Account
			aset.Add (new LdapAttribute ("uid", usernameEntry.Text));
			aset.Add (new LdapAttribute ("uidNumber", uidSpinButton.Value.ToString()));
			aset.Add (new LdapAttribute ("homeDirectory", homeDirEntry.Text));
			aset.Add (new LdapAttribute ("loginShell", shellEntry.Text));

			if (passChanged)
				aset.Add (new LdapAttribute ("userPassword", pass));
			else {
				aset.Add (new LdapAttribute ("userPassword", conn.Data.GetAttributeValueFromEntry (currentEntry, "userPassword")));
			}

			if (smbEnableSambaButton.Active || isSamba) {

				aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "posixaccount", "shadowaccount","inetorgperson", "person", "sambaSAMAccount"}));
				
				int user_rid = Convert.ToInt32 (uidSpinButton.Value) * 2 + 1000;
				LdapAttribute[] tmp = Util.CreateSambaAttributes (user_rid, smbSID, smbLM, smbNT);
				foreach (LdapAttribute a in tmp)
					aset.Add (a);
			
				aset.Add (new LdapAttribute ("sambaProfilePath", smbProfilePathEntry.Text));
				aset.Add (new LdapAttribute ("sambaHomePath", smbHomePathEntry.Text));
				aset.Add (new LdapAttribute ("sambaHomeDrive", smbHomeDriveEntry.Text));
				aset.Add (new LdapAttribute ("sambaLogonScript", smbLoginScriptEntry.Text));
				
				if (smbExpireEntry.Text != "")
					aset.Add (new LdapAttribute ("sambaKickoffTime", smbExpireEntry.Text));

				if (smbCanChangePwdEntry.Text != "")
					aset.Add (new LdapAttribute ("sambaPwdCanChange", smbCanChangePwdEntry.Text));

				if (smbMustChangePwdEntry.Text != "")
					aset.Add (new LdapAttribute ("sambaPwdMustChange", smbMustChangePwdEntry.Text));
					
			} else {
			
				aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "posixaccount", "shadowaccount","inetorgperson", "person"}));
			}
								
			// Groups
			aset.Add (new LdapAttribute ("gidNumber", getGidNumber(primaryGroupLabel.Text)));

			// Address
			aset.Add (new LdapAttribute ("street", adStreetTextView.Buffer.Text));
			aset.Add (new LdapAttribute ("l", adCityEntry.Text));
			aset.Add (new LdapAttribute ("st", adStateEntry.Text));
			aset.Add (new LdapAttribute ("postalCode", adZipEntry.Text));
			aset.Add (new LdapAttribute ("postOfficeBox", adPOBoxEntry.Text));

			// Telephones
			aset.Add (new LdapAttribute ("facsimileTelephoneNumber", tnFaxEntry.Text));
			aset.Add (new LdapAttribute ("pager", tnPagerEntry.Text));
			aset.Add (new LdapAttribute ("mobile", tnMobileEntry.Text));
			aset.Add (new LdapAttribute ("homePhone", tnHomeEntry.Text));
			aset.Add (new LdapAttribute ("ipPhone", tnIPPhoneEntry.Text));			

			// Organization
			aset.Add (new LdapAttribute ("title", ozTitleEntry.Text));
			aset.Add (new LdapAttribute ("departmentNumber", ozDeptEntry.Text));
			aset.Add (new LdapAttribute ("o", ozCompanyEntry.Text));				
					
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
				 	
			 if (!Util.ModifyEntry (conn, entry.DN, lea.Differences))
			 	errorOccured = true;
		}
	}
}
