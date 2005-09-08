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
using GLib;
using Glade;
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

		[Glade.Widget] Gtk.Entry usernameEntry;
		[Glade.Widget] Gtk.SpinButton uidSpinButton;
		[Glade.Widget] Gtk.Entry firstNameEntry;
		[Glade.Widget] Gtk.Entry lastNameEntry;
		[Glade.Widget] Gtk.Entry descriptionEntry;
		[Glade.Widget] Gtk.Entry officeEntry;

		[Glade.Widget] Gtk.Entry homeDirEntry;
		[Glade.Widget] Gtk.Entry shellEntry;
		[Glade.Widget] Gtk.Entry passwordEntry;
		[Glade.Widget] Gtk.Button passwordButton;
		[Glade.Widget] Gtk.HBox comboHbox;

		[Glade.Widget] Gtk.Entry mailEntry;
		[Glade.Widget] Gtk.Entry phoneEntry;

		// Groups
		[Glade.Widget] Gtk.Button addGroupButton;
		[Glade.Widget] Gtk.Button removeGroupButton;

		[Glade.Widget] Gtk.TreeView allGroupsTreeview;
		[Glade.Widget] Gtk.TreeView memberOfTreeview;

		// Samba
		[Glade.Widget] Gtk.CheckButton smbEnableSambaButton;
		[Glade.Widget] Gtk.Label smbNoteLabel;
		[Glade.Widget] Gtk.Entry smbLoginScriptEntry;
		[Glade.Widget] Gtk.Entry smbProfilePathEntry;
		[Glade.Widget] Gtk.Entry smbHomePathEntry;
		[Glade.Widget] Gtk.Entry smbHomeDriveEntry;

		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private static string[] userAttrs = { "givenName", "sn", "uid", "uidNumber", "gidNumber",
					      "userPassword", "mail", "loginShell", "cn",
					      "homeDirectory", "description",
				              "physicalDeliveryOfficeName",
					      "telephoneNumber"};

		private static string[] sambaAttrs = { "sambaProfilePath", "sambaHomePath", "sambaHomeDrive", "sambaLogonScript" };

		private bool _isSamba = false;
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

		private ListStore _allGroupStore;
		private ListStore _memberOfStore;

		private ComboBox primaryGroupComboBox;

		public EditUserViewDialog (lat.Connection conn, LdapEntry le) : base (conn)
		{
			_le = le;
			_modList = new ArrayList ();

			Init ();

			_isSamba = checkSamba (le);

			_ui = getUserInfo (le);

			getGroups (le);

			createCombo ();		

			string userName = (string) _ui["cn"];

			editUserDialog.Title = userName + " Properties";

			usernameLabel.UseMarkup = true;
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", _ui["uid"]);

			fullnameLabel.Text = String.Format ("{0} {1}", _ui["givenName"], _ui["sn"]);

			firstNameEntry.Text = (string)_ui["givenName"];
			lastNameEntry.Text = (string)_ui["sn"];
			usernameEntry.Text = (string)_ui["uid"];
			uidSpinButton.Value = int.Parse ((string)_ui["uidNumber"]);
			passwordEntry.Text = (string)_ui["userPassword"];
			mailEntry.Text = (string)_ui["mail"];
			shellEntry.Text = (string)_ui["loginShell"];
			homeDirEntry.Text = (string)_ui["homeDirectory"];
			descriptionEntry.Text = (string)_ui["description"];
			officeEntry.Text = (string)_ui["physicalDeliveryOfficeName"];
			phoneEntry.Text = (string)_ui["telephoneNumber"];

			if (_isSamba)
			{
				toggleSambaWidgets (true);
				smbEnableSambaButton.Hide ();

				smbLoginScriptEntry.Text = (string)_ui["sambaLogonScript"];
				smbProfilePathEntry.Text = (string)_ui["sambaProfilePath"];
				smbHomePathEntry.Text = (string)_ui["sambaHomePath"];
				smbHomeDriveEntry.Text = (string)_ui["sambaHomeDrive"];
			}
			else
			{
				smbEnableSambaButton.Toggled += new EventHandler (OnSambaChanged);
				toggleSambaWidgets (false);
			}

			editUserDialog.Run ();

			if (missingValues)
			{
				missingValues = false;
				editUserDialog.Run ();				
			}
			else
			{
				editUserDialog.Destroy ();
			}
		}
	
		private void OnSambaChanged (object o, EventArgs args)
		{
			if (smbEnableSambaButton.Active)
			{
				_smbSID = _conn.GetLocalSID ();

				toggleSambaWidgets (true);
				smbNoteLabel.Text = "Note: You must now reset your password.";
			}
			else
			{
				toggleSambaWidgets (false);
				smbNoteLabel.Markup = "";
			}
		}

		private Hashtable getUserInfo (LdapEntry le)
		{
			Hashtable ui = new Hashtable ();

			foreach (string a in userAttrs)
			{
				LdapAttribute attr;
				attr = le.getAttribute (a);

				if (attr == null)
				{
					ui.Add (a, "");
				}
				else
				{
					ui.Add (a, attr.StringValue);
				}
			}

			if (_isSamba)
			{
				foreach (string a in sambaAttrs)
				{
					LdapAttribute attr;
					attr = le.getAttribute (a);

					if (attr == null)
					{
						ui.Add (a, "");
					}
					else
					{
						ui.Add (a, attr.StringValue);
					}
				}
			}

			return ui;
		}

		private bool checkMemberOf (string user, string[] members)
		{
			foreach (string s in members)
			{
				if (s.Equals (user))
				{
					return true;
				}
			}
	
			return false;			
		}

		private void getGroups (LdapEntry le)
		{
			ArrayList grps = _conn.SearchByClass ("posixGroup");

			foreach (LdapEntry e in grps)
			{
				LdapAttribute nameAttr, gidAttr;
				nameAttr = e.getAttribute ("cn");
				gidAttr = e.getAttribute ("gidNumber");

				if (le != null)
				{
					LdapAttribute a;
					a  = e.getAttribute ("memberUid");
					
					if (a != null)
					{	
						if (checkMemberOf ((string)_ui["uid"], a.StringValueArray)
						   && !_memberOfGroups.ContainsKey (nameAttr.StringValue))
						{
							_memberOfGroups.Add (nameAttr.StringValue,"memeberUid");
							_memberOfStore.AppendValues (nameAttr.StringValue);
						}
					}

					if (!_memberOfGroups.ContainsKey (nameAttr.StringValue))
						_allGroupStore.AppendValues (nameAttr.StringValue);
				}
				else
				{
					_allGroupStore.AppendValues (nameAttr.StringValue);
				}

				if (!_allGroups.ContainsKey (nameAttr.StringValue))
					_allGroups.Add (nameAttr.StringValue, e);

				if (!_allGroupGids.ContainsKey (nameAttr.StringValue))
					_allGroupGids.Add (gidAttr.StringValue, nameAttr.StringValue);
			}
				
		}

		private void createCombo ()
		{
			primaryGroupComboBox = ComboBox.NewText ();

			string pgid = (string) _ui["gidNumber"];
			string name = (string) _allGroupGids [pgid];
			int index = 0;
			int pindex = 0;

			foreach (string key in _allGroups.Keys)
			{
				primaryGroupComboBox.AppendText (key);
			
				if (key.Equals (name))
					pindex = index;

				index++;
			}		

			primaryGroupComboBox.Active = pindex;
			primaryGroupComboBox.Show ();

			comboHbox.Add (primaryGroupComboBox);
		}

		private bool checkSamba (LdapEntry le)
		{
			bool retVal = false;
			
			LdapAttribute la = le.getAttribute ("objectClass");
			
			if (la == null)
				return retVal;

			foreach (string s in la.StringValueArray)
			{
				if (s.ToLower() == "sambasamaccount")
					retVal = true;
			}

			return retVal;
		}

		private void Init ()
		{
			_memberOfGroups = new Hashtable ();
			_allGroups = new Hashtable ();
			_allGroupGids = new Hashtable ();
			_modsGroup = new Hashtable ();

			ui = new Glade.XML (null, "lat.glade", "editUserDialog", null);
			ui.Autoconnect (this);

			_viewDialog = editUserDialog;

			TreeViewColumn col;

			_allGroupStore = new ListStore (typeof (string));
			allGroupsTreeview.Model = _allGroupStore;
			allGroupsTreeview.Selection.Mode = SelectionMode.Multiple;

			col = allGroupsTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			_allGroupStore.SetSortColumnId (0, SortType.Ascending);

			_memberOfStore = new ListStore (typeof (string));
			memberOfTreeview.Model = _memberOfStore;
			memberOfTreeview.Selection.Mode = SelectionMode.Multiple;

			col = memberOfTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			_memberOfStore.SetSortColumnId (0, SortType.Ascending);

			passwordEntry.Sensitive = false;

			usernameEntry.Changed += new EventHandler (OnNameChanged);
			firstNameEntry.Changed += new EventHandler (OnNameChanged);
			lastNameEntry.Changed += new EventHandler (OnNameChanged);

			passwordButton.Clicked += new EventHandler (OnPasswordClicked);

			addGroupButton.Clicked += new EventHandler (OnAddGroupClicked);
			removeGroupButton.Clicked += new EventHandler (OnRemoveGroupClicked);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			editUserDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);
		}

		private void toggleSambaWidgets (bool state)
		{
			smbLoginScriptEntry.Sensitive = state;
			smbProfilePathEntry.Sensitive = state;
			smbHomePathEntry.Sensitive = state;
			smbHomeDriveEntry.Sensitive = state;
		}

		private void OnAddGroupClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			TreePath[] tp = allGroupsTreeview.Selection.GetSelectedRows (out model);

			for (int i  = tp.Length; i > 0; i--)
			{
				_allGroupStore.GetIter (out iter, tp[(i - 1)]);

				string name = (string) _allGroupStore.GetValue (iter, 0);

				_memberOfStore.AppendValues (name);
		
				if (!_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Add (name, "memberUid");

				_allGroupStore.Remove (ref iter);

				LdapAttribute attr = new LdapAttribute ("memberUid", (string)_ui["uid"]);
				LdapModification lm = new LdapModification (LdapModification.ADD, attr);

				_modsGroup.Add (name, lm);

				updateGroupMembership ();

				_modsGroup.Clear ();
			}

		}

		private void OnRemoveGroupClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;
			
			TreePath[] tp = memberOfTreeview.Selection.GetSelectedRows (out model);

			for (int i  = tp.Length; i > 0; i--)
			{
				_memberOfStore.GetIter (out iter, tp[(i - 1)]);

				string name = (string) _memberOfStore.GetValue (iter, 0);

				_memberOfStore.Remove (ref iter);
		
				if (_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Remove (name);

				_allGroupStore.AppendValues (name);

				LdapAttribute attr = new LdapAttribute ("memberUid", (string)_ui["uid"]);
				LdapModification lm = new LdapModification (LdapModification.DELETE, attr);

				_modsGroup.Add (name, lm);
			
				updateGroupMembership ();

				_modsGroup.Clear ();
			}
		}

		private void OnNameChanged (object o, EventArgs args)
		{
			usernameLabel.Markup = 
				String.Format ("<span size=\"larger\"><b>{0}</b></span>", usernameEntry.Text);
			fullnameLabel.Text = String.Format ("{0} {1}", firstNameEntry.Text, lastNameEntry.Text);
			
		}

		private void OnPasswordClicked (object o, EventArgs args)
		{
			PasswordDialog pd = new PasswordDialog ();

			if (!passwordEntry.Text.Equals ("") && pd.UnixPassword.Equals (""))
				return;

			passwordEntry.Text = pd.UnixPassword;
			_smbLM = pd.LMPassword;
			_smbNT = pd.NTPassword;

			_passChanged = true;
		}

		private void modifyGroup (LdapEntry groupEntry, LdapModification[] mods)
		{
			if (groupEntry == null)
				return;

			try
			{
				_conn.Modify (groupEntry.DN, mods);
			}
			catch (Exception e)
			{
				string errorMsg =
					Mono.Unix.Catalog.GetString ("Unable to modify group ") + groupEntry.DN;

				errorMsg += "\nError: " + e.Message;

				Util.MessageBox (editUserDialog, errorMsg, MessageType.Error);
			}
		}

		private void updateGroupMembership ()
		{
			Logger.Log.Debug ("START updateGroupMembership ()");

			LdapEntry groupEntry = null;
			LdapModification[] mods = new LdapModification [_modsGroup.Count];

			int count = 0;

			foreach (string key in _modsGroup.Keys)
			{
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

			TreeIter iter;
				
			primaryGroupComboBox.GetActiveIter (out iter);

			string pg = (string) primaryGroupComboBox.Model.GetValue (iter, 0);

			retVal.Add ("givenName", firstNameEntry.Text);
			retVal.Add ("sn", lastNameEntry.Text);
			retVal.Add ("uid", usernameEntry.Text);
			retVal.Add ("uidNumber", uidSpinButton.Value.ToString());
			retVal.Add ("userPassword", passwordEntry.Text);
			retVal.Add ("gidNumber", getGidNumber(pg));
			retVal.Add ("mail", mailEntry.Text);
			retVal.Add ("loginShell", shellEntry.Text);
			retVal.Add ("homeDirectory", homeDirEntry.Text);
			retVal.Add ("description", descriptionEntry.Text);
			retVal.Add ("physicalDeliveryOfficeName", officeEntry.Text);
			retVal.Add ("telephoneNumber", phoneEntry.Text);

			if (_isSamba)
			{
				retVal.Add ("sambaProfilePath", smbProfilePathEntry.Text);
				retVal.Add ("sambaHomePath", smbHomePathEntry.Text);
				retVal.Add ("sambaHomeDrive", smbHomeDriveEntry.Text);
				retVal.Add ("sambaLogonScript", smbLoginScriptEntry.Text);
			}

			return retVal;
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cui = getUpdatedUserInfo ();

			string[] objClass = {"posixaccount","inetorgperson", "person" };
			string[] missing = null;

			if (!checkReqAttrs (objClass, cui, out missing))
			{
				missingAlert (missing);
				missingValues = true;

				return;
			}

			_modList = getMods (userAttrs, _ui, cui);

			if (smbEnableSambaButton.Active)
			{
				int user_rid = Convert.ToInt32 (uidSpinButton.Value) * 2 + 1000;

				ArrayList smbMods = Util.CreateSambaMods (
							user_rid, 
							_smbSID,
							_smbLM,
							_smbNT);

				foreach (LdapModification l in smbMods)
				{
					_modList.Add (l);
				}
			}
			else if (_isSamba)
			{
				ArrayList smbMods = getMods (sambaAttrs, _ui, cui);

				if (_passChanged)
				{
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
				{
					_modList.Add (l);
				}
			}

			Util.ModifyEntry (_conn, _viewDialog, _le.DN, _modList, true);

			editUserDialog.HideAll ();
		}
	}
}
