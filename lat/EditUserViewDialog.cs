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
using System.Collections;
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

		private static string[] userAttrs = { "givenName", "sn", "initials", "cn",
			"uid", "uidNumber", "gidNumber", "userPassword", "mail", "loginShell", 
			"homeDirectory", "description", "physicalDeliveryOfficeName",
			"telephoneNumber", "postalAddress", "l", "st", "postalCode",
			"facsimileTelephoneNumber", "pager", "mobile", "homePhone", 
			"street", "title", "postOfficeBox" };

		private static string[] sambaAttrs = { "sambaProfilePath", "sambaHomePath",
			"sambaHomeDrive", "sambaLogonScript", "sambaKickoffTime", 
			"sambaPwdCanChange", "sambaPwdMustChange" };

		private bool _isSamba = false;
		private bool firstTimeSamba = false;
		private string _pass = "";
		private string _smbLM = "";
		private string _smbNT = "";
		private string _smbSID = "";
		private bool _passChanged = false;
		
		private LdapEntry _le;
		private Hashtable _ui;

		private ArrayList _modList;

		private Hashtable _allGroups;
		private Hashtable _allGroupGids;
		private Hashtable _modsGroup;
		private Hashtable _memberOfGroups;

		private ListStore _memberOfStore;

		public EditUserViewDialog (LdapServer ldapServer, LdapEntry le) : base (ldapServer, null)
		{
			_le = le;
			_modList = new ArrayList ();

			Init ();

			_isSamba = Util.CheckSamba (le);

			_ui = getUserInfo (le);

			getGroups (le);

			string userName = (string) _ui["cn"];

			editUserDialog.Title = userName + " Properties";

			// General
			usernameLabel.UseMarkup = true;
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", _ui["uid"]);

			fullnameLabel.Text = String.Format ("{0} {1}", _ui["givenName"], _ui["sn"]);

			firstNameEntry.Text = (string)_ui["givenName"];
			initialsEntry.Text = (string)_ui["initials"];
			lastNameEntry.Text = (string)_ui["sn"];
			descriptionEntry.Text = (string)_ui["description"];
			officeEntry.Text = (string)_ui["physicalDeliveryOfficeName"];
			mailEntry.Text = (string)_ui["mail"];
			phoneEntry.Text = (string)_ui["telephoneNumber"];

			// Account
			usernameEntry.Text = (string)_ui["uid"];
			uidSpinButton.Value = int.Parse ((string)_ui["uidNumber"]);
			shellEntry.Text = (string)_ui["loginShell"];
			homeDirEntry.Text = (string)_ui["homeDirectory"];

			if (_isSamba) {
				toggleSambaWidgets (true);
				smbEnableSambaButton.Hide ();

				smbLoginScriptEntry.Text = (string)_ui["sambaLogonScript"];
				smbProfilePathEntry.Text = (string)_ui["sambaProfilePath"];
				smbHomePathEntry.Text = (string)_ui["sambaHomePath"];
				smbHomeDriveEntry.Text = (string)_ui["sambaHomeDrive"];
				smbExpireEntry.Text = (string)_ui["sambaKickoffTime"];
				smbCanChangePwdEntry.Text = (string)_ui["sambaPwdCanChange"];
				smbMustChangePwdEntry.Text = (string)_ui["sambaPwdMustChange"];

			} else {

				smbEnableSambaButton.Toggled += new EventHandler (OnSambaChanged);
				toggleSambaWidgets (false);
			}

			// Groups
			string pgid = (string) _ui["gidNumber"];
			string pname = (string) _allGroupGids [pgid];		
			primaryGroupLabel.Text = pname;			

			// Address
			adStreetTextView.Buffer.Text = (string)_ui["street"];
			adPOBoxEntry.Text = (string)_ui["postOfficeBox"];
			adCityEntry.Text = (string)_ui["l"];
			adStateEntry.Text = (string)_ui["st"];
			adZipEntry.Text = (string)_ui["postalCode"];

			// Telephones
			tnHomeEntry.Text = (string)_ui["homePhone"];
			tnPagerEntry.Text = (string)_ui["pager"];
			tnMobileEntry.Text = (string)_ui["mobile"];
			tnFaxEntry.Text = (string)_ui["facsimileTelephoneNumber"];

			// Organization
			ozTitleEntry.Text = (string)_ui["title"];
			ozDeptEntry.Text = (string)_ui["departmentNumber"];
			ozCompanyEntry.Text = (string)_ui["o"];

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
	
		private void OnSambaChanged (object o, EventArgs args)
		{
			if (smbEnableSambaButton.Active) {

				_smbSID = server.GetLocalSID ();

				if (_smbSID == null) {
					Util.DisplaySambaSIDWarning (editUserDialog);
					smbEnableSambaButton.Active = false;
					return;
				}

				toggleSambaWidgets (true);
			} else {

				toggleSambaWidgets (false);
			}
		}

		private Hashtable getUserInfo (LdapEntry le)
		{
			Hashtable ui = new Hashtable ();

			foreach (string a in userAttrs) {
				LdapAttribute attr;
				attr = le.getAttribute (a);

				if (attr == null)
					ui.Add (a, "");
				else
					ui.Add (a, attr.StringValue);
			}

			if (_isSamba) {
				foreach (string a in sambaAttrs) {
					LdapAttribute attr;
					attr = le.getAttribute (a);

					if (attr == null)
						ui.Add (a, "");
					else
						ui.Add (a, attr.StringValue);
				}
			} else {
				firstTimeSamba = true;
			}

			return ui;
		}

		private bool checkMemberOf (string user, string[] members)
		{
			foreach (string s in members)
				if (s.Equals (user))
					return true;
	
			return false;			
		}

		private void getGroups (LdapEntry le)
		{
			LdapEntry[] grps = server.SearchByClass ("posixGroup");

			foreach (LdapEntry e in grps) {

				LdapAttribute nameAttr, gidAttr;
				nameAttr = e.getAttribute ("cn");
				gidAttr = e.getAttribute ("gidNumber");

				if (le != null) {

					LdapAttribute a;
					a  = e.getAttribute ("memberUid");
					
					if (a != null) {

						if (checkMemberOf ((string)_ui["uid"], a.StringValueArray)
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

		private void Init ()
		{
			_memberOfGroups = new Hashtable ();
			_allGroups = new Hashtable ();
			_allGroupGids = new Hashtable ();
			_modsGroup = new Hashtable ();

			ui = new Glade.XML (null, "lat.glade", "editUserDialog", null);
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

		private void toggleSambaWidgets (bool state)
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
			ArrayList tmp = new ArrayList ();
	
			foreach (string k in _allGroups.Keys) {
				if (k.Equals (primaryGroupLabel.Text) ||
				    _memberOfGroups.ContainsKey (k))
					continue;

				tmp.Add (k);
			}

			string[] allgroups = (string[]) tmp.ToArray (typeof(string));

			SelectGroupsDialog sgd = new SelectGroupsDialog (allgroups);

			foreach (string name in sgd.SelectedGroupNames) {

				_memberOfStore.AppendValues (name);
		
				if (!_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Add (name, "memberUid");

				LdapAttribute attr = new LdapAttribute ("memberUid", (string)_ui["uid"]);
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

				LdapAttribute attr = new LdapAttribute ("memberUid", (string)_ui["uid"]);
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

			_pass = pd.UnixPassword;
			_smbLM = pd.LMPassword;
			_smbNT = pd.NTPassword;

			_passChanged = true;
		}

		public void OnSetPrimaryGroupClicked (object o, EventArgs args)
		{
			ArrayList tmp = new ArrayList ();
	
			foreach (string k in _allGroups.Keys) {

				if (k.Equals (primaryGroupLabel.Text))
					continue;

				tmp.Add (k);
			}

			string[] allgroups = (string[]) tmp.ToArray (typeof(string));

			SelectGroupsDialog sgd = new SelectGroupsDialog (allgroups);

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

		private void modifyGroup (LdapEntry groupEntry, LdapModification[] mods)
		{
			if (groupEntry == null)
				return;

			try {
			
				server.Modify (groupEntry.DN, mods);

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

		private void updateGroupMembership ()
		{
			Logger.Log.Debug ("START updateGroupMembership ()");

			LdapEntry groupEntry = null;
			LdapModification[] mods = new LdapModification [_modsGroup.Count];

			int count = 0;

			foreach (string key in _modsGroup.Keys) {

				Logger.Log.Debug ("group: {0}", key);

				LdapModification lm = (LdapModification) _modsGroup[key];
				groupEntry = (LdapEntry) _allGroups [key];

				mods[count] = lm;

				count++;
			}	

			modifyGroup (groupEntry, mods);

			Logger.Log.Debug ("END updateGroupMembership ()");
		}

		private string getGidNumber (string name)
		{
			if (name == null)
				return null;

			LdapEntry le = (LdapEntry) _allGroups [name];		
			LdapAttribute attr = le.getAttribute ("gidNumber");

			if (attr != null)
				return attr.StringValue;
			
			return null;
		}

		private Hashtable getUpdatedUserInfo ()
		{
			Hashtable retVal = new Hashtable ();

			// General 
			retVal.Add ("givenName", firstNameEntry.Text);
			retVal.Add ("initials", initialsEntry.Text);
			retVal.Add ("sn", lastNameEntry.Text);
			retVal.Add ("description", descriptionEntry.Text);
			retVal.Add ("physicalDeliveryOfficeName", officeEntry.Text);
			retVal.Add ("mail", mailEntry.Text);
			retVal.Add ("telephoneNumber", phoneEntry.Text);

			// Account
			retVal.Add ("uid", usernameEntry.Text);
			retVal.Add ("uidNumber", uidSpinButton.Value.ToString());
			retVal.Add ("homeDirectory", homeDirEntry.Text);
			retVal.Add ("loginShell", shellEntry.Text);

			if (_passChanged)
				retVal.Add ("userPassword", _pass);

			if (_isSamba) {

				retVal.Add ("sambaProfilePath", smbProfilePathEntry.Text);
				retVal.Add ("sambaHomePath", smbHomePathEntry.Text);
				retVal.Add ("sambaHomeDrive", smbHomeDriveEntry.Text);
				retVal.Add ("sambaLogonScript", smbLoginScriptEntry.Text);

				if (smbExpireEntry.Text != "")
					retVal.Add ("sambaKickoffTime", smbExpireEntry.Text);

				if (smbCanChangePwdEntry.Text != "")
					retVal.Add ("sambaPwdCanChange", smbCanChangePwdEntry.Text);

				if (smbMustChangePwdEntry.Text != "")
					retVal.Add ("sambaPwdMustChange", smbMustChangePwdEntry.Text);
			}

			// Groups
			retVal.Add ("gidNumber", getGidNumber(primaryGroupLabel.Text));

			// Address
			retVal.Add ("street", adStreetTextView.Buffer.Text);
			retVal.Add ("l", adCityEntry.Text);
			retVal.Add ("st", adStateEntry.Text);
			retVal.Add ("postalCode", adZipEntry.Text);
			retVal.Add ("postOfficeBox", adPOBoxEntry.Text);

			// Telephones
			retVal.Add ("facsimileTelephoneNumber", tnFaxEntry.Text);
			retVal.Add ("pager", tnPagerEntry.Text);
			retVal.Add ("mobile", tnMobileEntry.Text);
			retVal.Add ("homePhone", tnHomeEntry.Text);
			retVal.Add ("ipPhone", tnIPPhoneEntry.Text);

			// Organization
			retVal.Add ("title", ozTitleEntry.Text);
			retVal.Add ("departmentNumber", ozDeptEntry.Text);
			retVal.Add ("o", ozCompanyEntry.Text);

			return retVal;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cui = getUpdatedUserInfo ();

			string[] objClass = {"posixaccount","inetorgperson", "person" };
			string[] missing = null;

			if (!checkReqAttrs (objClass, cui, out missing)) {
				missingAlert (missing);
				missingValues = true;

				return;
			}

			_modList = getMods (userAttrs, _ui, cui);

			if (smbEnableSambaButton.Active) {

				int user_rid = Convert.ToInt32 (uidSpinButton.Value) * 2 + 1000;

				ArrayList smbMods = Util.CreateSambaMods (
							user_rid, 
							_smbSID,
							_smbLM,
							_smbNT);

				foreach (LdapModification l in smbMods)
					_modList.Add (l);
			
			} else if (_isSamba) {

				ArrayList smbMods = getMods (sambaAttrs, _ui, cui);

				if (_passChanged) {

					LdapAttribute la; 
					LdapModification lm;

					la = new LdapAttribute ("sambaLMPassword", _smbLM);
					lm = new LdapModification (LdapModification.REPLACE, la);

					_modList.Add (lm);

					la = new LdapAttribute ("sambaNTPassword", _smbNT);
					lm = new LdapModification (LdapModification.REPLACE, la);

					_modList.Add (lm);
				}

				foreach (LdapModification l in smbMods)
					_modList.Add (l);
			}

			if (!Util.ModifyEntry (server, viewDialog, _le.DN, _modList, true)) {
				errorOccured = true;
				return;
			}

			editUserDialog.HideAll ();
		}
	}
}
