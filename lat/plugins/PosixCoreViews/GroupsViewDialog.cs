// 
// lat - GroupsViewDialog.cs
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
	public class GroupsViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog groupDialog;
		[Glade.Widget] Gtk.Entry groupNameEntry;
		[Glade.Widget] Gtk.Entry descriptionEntry;
		[Glade.Widget] Gtk.SpinButton groupIDSpinButton;
		[Glade.Widget] Gtk.TreeView allUsersTreeview;
		[Glade.Widget] Gtk.TreeView currentMembersTreeview;
		[Glade.Widget] Gtk.CheckButton enableSambaButton;

		ListStore allUserStore;
		ListStore currentMemberStore;

		Dictionary<string,string> _gi;
		Dictionary<string,string> _currentMembers = new Dictionary<string,string> ();

		bool _isEdit;
		
		LdapEntry _le;
		List<LdapModification> _modList;

		static string[] groupAttrs = { "cn", "gidNumber", "description" };

		bool _isSamba = false;
		string _smbSID = "";

		public GroupsViewDialog (LdapServer ldapServer, string newContainer) : base (ldapServer, newContainer)
		{
			Init ();

			populateUsers ();

			groupDialog.Title = "Add Group";
			groupIDSpinButton.Value = server.GetNextGID ();

			groupDialog.Icon = Global.latIcon;
			groupDialog.Run ();

			while (missingValues || errorOccured) {
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				groupDialog.Run ();				
			}

			groupDialog.Destroy ();
		}

		public GroupsViewDialog (LdapServer ldapServer, LdapEntry le) : base (ldapServer, null)
		{
			_le = le;
			_modList = new List<LdapModification> ();

			_isSamba = checkSamba (le);

			Log.Debug ("GroupsViewDialog: _modList == {0}", _modList.Count);

			_isEdit = true;

			Init ();

			server.GetAttributeValuesFromEntry (le, groupAttrs, out _gi);

			string groupName = (string) _gi ["cn"];

			groupDialog.Title = groupName + " Properties";
			groupNameEntry.Text = groupName;
			descriptionEntry.Text = (string) _gi ["description"];
			groupIDSpinButton.Value = int.Parse ((string) _gi ["gidNumber"]);

			LdapAttribute attr;
			attr = le.getAttribute ("memberuid");

			if (attr != null) {

				string[] svalues = attr.StringValueArray;

				foreach (string s in svalues) {
					currentMemberStore.AppendValues (s);
					_currentMembers.Add (s, "memberuid");
				}

			}

			populateUsers ();

			groupDialog.Run ();

			while (missingValues || errorOccured){
				if (missingValues)
					missingValues = false;
				else if (errorOccured)
					errorOccured = false;

				groupDialog.Run ();
			}

			groupDialog.Destroy ();
		}

		private void OnSambaChanged (object o, EventArgs args)
		{
			if (enableSambaButton.Active) {
				_smbSID = server.GetLocalSID ();

				if (_smbSID == null) {
					Util.DisplaySambaSIDWarning (groupDialog);
					enableSambaButton.Active = false;
					return;
				}
			}
		}

		private void populateUsers ()
		{
			LdapEntry[] _users = server.SearchByClass ("posixAccount");

			foreach (LdapEntry le in _users) {
				LdapAttribute nameAttr;

				nameAttr = le.getAttribute ("uid");

				if (nameAttr != null && !_currentMembers.ContainsKey (nameAttr.StringValue) )
					allUserStore.AppendValues (nameAttr.StringValue);
			}					
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "dialogs.glade", "groupDialog", null);
			ui.Autoconnect (this);

			viewDialog = groupDialog;

			TreeViewColumn col;

			allUserStore = new ListStore (typeof (string));
			allUsersTreeview.Model = allUserStore;
			allUsersTreeview.Selection.Mode = SelectionMode.Multiple;

			col = allUsersTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			allUserStore.SetSortColumnId (0, SortType.Ascending);
			
			currentMemberStore = new ListStore (typeof (string));
			currentMembersTreeview.Model = currentMemberStore;
			currentMembersTreeview.Selection.Mode = SelectionMode.Multiple;

			col = currentMembersTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			currentMemberStore.SetSortColumnId (0, SortType.Ascending);

			if (_isSamba)
				enableSambaButton.Hide ();
			else
				enableSambaButton.Toggled += new EventHandler (OnSambaChanged);

			groupDialog.Resize (350, 400);			
		}

		public void OnAddClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			TreePath[] tp = allUsersTreeview.Selection.GetSelectedRows (out model);

			for (int i  = tp.Length; i > 0; i--) {

				allUserStore.GetIter (out iter, tp[(i - 1)]);

				string user = (string) allUserStore.GetValue (iter, 0);
				
				currentMemberStore.AppendValues (user);
		
				if (!_currentMembers.ContainsKey (user))
					_currentMembers.Add (user, "memberuid");

				allUserStore.Remove (ref iter);

				Log.Debug ("Adding {0} to group", user);

				if (_isEdit) {

					LdapAttribute attr = new LdapAttribute ("memberuid", user);
					LdapModification lm = new LdapModification (LdapModification.ADD, attr);
					_modList.Add (lm);

					Log.Debug ("OnAddClicked: _modList == {0}", _modList.Count);
				}

			}
		}

		public void OnRemoveClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			TreePath[] tp = currentMembersTreeview.Selection.GetSelectedRows (out model);

			for (int i  = tp.Length; i > 0; i--) {

				currentMemberStore.GetIter (out iter, tp[(i - 1)]);

				string user = (string) currentMemberStore.GetValue (iter, 0);

				currentMemberStore.Remove (ref iter);
		
				if (_currentMembers.ContainsKey (user))
					_currentMembers.Remove (user);

				Log.Debug ("Removing user {0} from group", user);

				allUserStore.AppendValues (user);

				if (_isEdit) {

					LdapAttribute attr = new LdapAttribute ("memberuid", user);
					LdapModification lm = new LdapModification (LdapModification.DELETE, attr);
					_modList.Add (lm);
				}
			}
		}

		private bool checkSamba (LdapEntry le)
		{
			bool retVal = false;
			
			LdapAttribute la = le.getAttribute ("objectClass");
			
			if (la == null)
				return retVal;

			foreach (string s in la.StringValueArray)
				if (s.ToLower() == "sambagroupmapping")
					retVal = true;

			return retVal;
		}

		Dictionary<string,string> getCurrentGroupInfo ()
		{
			Dictionary<string,string> retVal = new Dictionary<string,string> ();

			retVal.Add ("cn", groupNameEntry.Text);
			retVal.Add ("description", descriptionEntry.Text);
			retVal.Add ("gidNumber", groupIDSpinButton.Value.ToString());

			return retVal;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			Dictionary<string,string> cgi = getCurrentGroupInfo ();

			string[] objClass = { "top", "posixGroup" };
			string[] missing = null;

			if (!checkReqAttrs (objClass, cgi, out missing)) {
				missingAlert (missing);
				missingValues = true;

				return;
			}


			if (_isEdit) {

				if (_modList.Count == 0)
				{
					_modList = getMods (groupAttrs, _gi, cgi);

				} else {

					List<LdapModification> tmp = getMods (groupAttrs, _gi, cgi);
					foreach (LdapModification lm in tmp)
						_modList.Add (lm);
				}

				if (enableSambaButton.Active) {

					LdapAttribute a = new LdapAttribute ("objectclass", "sambaGroupMapping");
					LdapModification lm = new LdapModification (LdapModification.ADD, a);

					_modList.Add (lm);

					a = new LdapAttribute ("sambaGroupType", "2");
					lm = new LdapModification (LdapModification.ADD, a);
					_modList.Add (lm);

					int grid = Convert.ToInt32 (groupIDSpinButton.Value) * 2 + 1001;

					a = new LdapAttribute ("sambaSID", String.Format ("{0}-{1}", _smbSID, grid));
					lm = new LdapModification (LdapModification.ADD, a);
					_modList.Add (lm);
				}
	
				if (!Util.ModifyEntry (server, viewDialog, _le.DN, _modList, true)) {
					errorOccured = true;	
					return;
				}

			} else {

				List<LdapAttribute> attrList = getAttributes (objClass, groupAttrs, cgi);

				if (enableSambaButton.Active) {

					LdapAttribute a = (LdapAttribute) attrList[0];
					a.addValue ("sambaGroupMapping");

					a = new LdapAttribute ("sambaGroupType", "2");
					attrList.Add (a);

					int grid = Convert.ToInt32 (groupIDSpinButton.Value) * 2 + 1001;

					a = new LdapAttribute ("sambaSID", String.Format ("{0}-{1}", _smbSID, grid));
					attrList.Add (a);
				}

				foreach (string key in _currentMembers.Keys) {
					LdapAttribute attr = new LdapAttribute ("memberuid", key);
					attrList.Add (attr);
				}

				string userDN = null;				
				if (this.defaultNewContainer == null) {
					SelectContainerDialog scd =	new SelectContainerDialog (server, groupDialog);

					scd.Title = "Save Group";
					scd.Message = String.Format (
						"Where in the directory would\nyou like save the group\n{0}?", (string)cgi["cn"]);

					scd.Run ();

					if (scd.DN == "")
						return;

					userDN = String.Format ("cn={0},{1}", (string)cgi["cn"], scd.DN);
				
				} else {
				
					userDN = String.Format ("cn={0},{1}", (string)cgi["cn"], this.defaultNewContainer);
				}

				if (!Util.AddEntry (server, viewDialog, userDN, attrList, true)) {
					errorOccured = true;
					return;
				}
			}

			groupDialog.HideAll ();
		}
	}
}
