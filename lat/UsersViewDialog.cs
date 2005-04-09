// 
// lat - UsersViewDialog.cs
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
	public class UsersViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog userDialog;

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

		[Glade.Widget] Gtk.Button addGroupButton;
		[Glade.Widget] Gtk.Button removeGroupButton;

		[Glade.Widget] Gtk.TreeView allGroupsTreeview;
		[Glade.Widget] Gtk.TreeView memberOfTreeview;

		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private static string[] userAttrs = { "givenName", "sn", "uid", "uidNumber", "gidNumber",
					      "userPassword", "mail", "loginShell",
					      "homeDirectory", "description",
				              "physicalDeliveryOfficeName",
					      "telephoneNumber"};

		private bool _isEdit = false;
		
		private LdapEntry _le;
		private Hashtable _ui;

		private ArrayList _modList;

		private Hashtable _allGroups;
		private Hashtable _allGroupGids;
		private Hashtable _modsGroup;
		private Hashtable _memberOfGroups;

		private ListStore _allGroupStore;
		private ListStore _memberOfStore;

		private Combo primaryGroupComboBox;

		public UsersViewDialog (lat.Connection conn) : base (conn)
		{
			Init ();		

			getGroups (null);

			createCombo ();

			userDialog.Title = "LAT - Add User";

			userDialog.Run ();
			userDialog.Destroy ();
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
						if (checkMemberOf ((string)_ui["uid"], a.StringValueArray))
						{
							_memberOfGroups.Add 	(nameAttr.StringValue,"memeberUid");
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

				_allGroups.Add (nameAttr.StringValue, e);
				_allGroupGids.Add (gidAttr.StringValue, nameAttr.StringValue);
			}
				
		}

		private void createCombo ()
		{
			ArrayList list = new ArrayList ();
			foreach (string key in _allGroups.Keys)
			{
				list.Add (key);
			}

			primaryGroupComboBox = new Combo ();
			primaryGroupComboBox.PopdownStrings = (string[])list.ToArray (typeof(string));
			primaryGroupComboBox.DisableActivate ();
			primaryGroupComboBox.Entry.IsEditable = false;
			primaryGroupComboBox.Show ();
			comboHbox.Add (primaryGroupComboBox);
		}

		public UsersViewDialog (lat.Connection conn, LdapEntry le) : base (conn)
		{
			_le = le;
			_modList = new ArrayList ();

			_isEdit = true;

			Init ();

			_ui = getUserInfo (le);

			getGroups (le);

			createCombo ();			

			userDialog.Title = "LAT - Edit User";

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

			userDialog.Run ();
			userDialog.Destroy ();
		}

		private void Init ()
		{
			_memberOfGroups = new Hashtable ();
			_allGroups = new Hashtable ();
			_allGroupGids = new Hashtable ();
			_modsGroup = new Hashtable ();

			ui = new Glade.XML (null, "lat.glade", "userDialog", null);
			ui.Autoconnect (this);

			_viewDialog = userDialog;

			TreeViewColumn col;

			_allGroupStore = new ListStore (typeof (string));
			allGroupsTreeview.Model = _allGroupStore;

			col = allGroupsTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			_allGroupStore.SetSortColumnId (0, SortType.Ascending);

			_memberOfStore = new ListStore (typeof (string));
			memberOfTreeview.Model = _memberOfStore;

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

			userDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);
		}

		private void OnAddGroupClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (allGroupsTreeview.Selection.GetSelected (out model, out iter))
			{
				string name = (string) _allGroupStore.GetValue (iter, 0);
				
				_memberOfStore.AppendValues (name);
		
				if (!_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Add (name, "memberUid");

				_allGroupStore.Remove (ref iter);

				if (_isEdit)
				{
					LdapAttribute attr = new LdapAttribute ("memberUid", (string)_ui["uid"]);
					LdapModification lm = new LdapModification (LdapModification.ADD, attr);

					if (!_modsGroup.ContainsKey (name))
						_modsGroup.Add (name, lm);
					else
					{
						_modsGroup.Remove (name);
					}
				}		

			}
			else
			{
				Util.MessageBox (userDialog, "No group selected to add.",
						 MessageType.Error);
			}
		}

		private void OnRemoveGroupClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (memberOfTreeview.Selection.GetSelected (out model, out iter))
			{
				string name = (string) _memberOfStore.GetValue (iter, 0);

				_memberOfStore.Remove (ref iter);
		
				if (_memberOfGroups.ContainsKey (name))
					_memberOfGroups.Remove (name);

				_allGroupStore.AppendValues (name);

				if (_isEdit)
				{
					LdapAttribute attr = new LdapAttribute ("memberUid", (string)_ui["uid"]);
					LdapModification lm = new LdapModification (LdapModification.DELETE, attr);

					if (!_modsGroup.ContainsKey (name))
						_modsGroup.Add (name, lm);
					else
					{
						_modsGroup.Remove (name);
					}
				}
			}
			else
			{
				Util.MessageBox (userDialog, "No group selected to remove.",
						 MessageType.Error);
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

			if (!passwordEntry.Text.Equals ("") && pd.Password.Equals (""))
				return;

			passwordEntry.Text = pd.Password;
		}

		private void modifyGroup (LdapEntry groupEntry, LdapModification[] mods)
		{
			if (groupEntry == null)
				return;

			if (!_conn.Modify (groupEntry.DN, mods))
			{
				string msg = String.Format (
					"Unable to modify group {0}", groupEntry.DN);

				Util.MessageBox (userDialog, msg, MessageType.Error);
			}
		}

		private void updateGroupMembership ()
		{
			LdapEntry groupEntry = null;
			LdapModification[] mods = new LdapModification [1];

			if (_isEdit)
			{
				foreach (string key in _modsGroup.Keys)
				{
					LdapModification lm = (LdapModification) _modsGroup[key];
					groupEntry = (LdapEntry) _allGroups [key];

					mods[0] = lm;
				}
			}
			else
			{
				foreach (string key in _memberOfGroups.Keys)
				{
					LdapAttribute attr = new LdapAttribute ("memberUid", usernameEntry.Text);
					LdapModification lm = new LdapModification (LdapModification.ADD, attr);

					groupEntry = (LdapEntry) _allGroups[key];

					mods[0] = lm;
				}
			}

			modifyGroup (groupEntry, mods);
		}

		private string getGidName (int gid)
		{
			return (string) _allGroupGids [gid.ToString()];
		}

		private string getGidNumber (string name)
		{
			LdapEntry le = (LdapEntry) _allGroups [name];
			LdapAttribute attr = le.getAttribute ("gidNumber");

			if (attr != null)
				return attr.StringValue;
			
			return null;
		}

		private Hashtable getUpdatedUserInfo ()
		{
			Hashtable retVal = new Hashtable ();

			retVal.Add ("givenName", firstNameEntry.Text);
			retVal.Add ("sn", lastNameEntry.Text);
			retVal.Add ("uid", usernameEntry.Text);
			retVal.Add ("uidNumber", uidSpinButton.Value.ToString());
			retVal.Add ("userPassword", passwordEntry.Text);
			retVal.Add ("gidNumber", getGidNumber(primaryGroupComboBox.Entry.Text));
			retVal.Add ("mail", mailEntry.Text);
			retVal.Add ("loginShell", shellEntry.Text);
			retVal.Add ("homeDirectory", homeDirEntry.Text);
			retVal.Add ("description", descriptionEntry.Text);
			retVal.Add ("physicalDeliveryOfficeName", officeEntry.Text);
			retVal.Add ("telephoneNumber", phoneEntry.Text);

			return retVal;
		}
	
		private void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cui = getUpdatedUserInfo ();

			if (_isEdit)
			{
				_modList = getMods (userAttrs, _ui, cui);

				updateGroupMembership ();

				Util.ModifyEntry (_conn, _viewDialog, _le.DN, _modList);
			}
			else
			{
				string[] objClass = {"posixaccount","inetorgperson", "person" };

				ArrayList attrList = getAttributes (objClass, userAttrs, cui);

				string fullName = String.Format ("{0} {1}", 
					(string)cui["givenName"], (string)cui["sn"] );

				cui["cn"] = fullName;
				cui["gecos"] = fullName;

				string[] missing = null;

				if (!checkReqAttrs (objClass, cui, out missing))
				{
					attrList.Clear ();

					missingAlert (missing);
					return;
				}

				LdapAttribute attr;

				attr = new LdapAttribute ("cn", fullName);
				attrList.Add (attr);

				attr = new LdapAttribute ("gecos", fullName);
				attrList.Add (attr);

				SelectContainerDialog scd = 
					new SelectContainerDialog (_conn, userDialog);

				scd.Title = "Save User";
				scd.Message = String.Format ("Where in the directory would\nyou like save the user\n{0}?", fullName);

				scd.Run ();

				if (scd.DN == "")
					return;

				string userDN = String.Format ("cn={0},{1}", fullName, scd.DN);

				updateGroupMembership ();

				Util.AddEntry (_conn, _viewDialog, userDN, attrList);
			}

			userDialog.HideAll ();
		}
	}
}
