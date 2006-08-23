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
		[Glade.Widget] Gtk.TreeView allUsersTreeview;
		[Glade.Widget] Gtk.TreeView currentMembersTreeview;

		ListStore allUserStore;
		ListStore currentMemberStore;

		LdapEntry currentEntry;
		List<string> currentMembers = new List<string> ();
		
		bool isEdit;

		public GroupsViewDialog (Connection connection, string newContainer) : base (connection, newContainer)
		{
			Init ();

			isEdit = false;

			populateUsers ();

			groupDialog.Title = "Add Group";
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

		public GroupsViewDialog (Connection connection, LdapEntry le) : base (connection, null)
		{
			currentEntry = le;
		
			isEdit = true;

			Init ();

			string groupName = conn.Data.GetAttributeValueFromEntry (currentEntry, "cn");

			groupDialog.Title = groupName + " Properties";
			groupNameEntry.Text = groupName;
			descriptionEntry.Text = conn.Data.GetAttributeValueFromEntry (currentEntry, "description");
			
			LdapAttribute attr = currentEntry.getAttribute ("member");
			if (attr != null) {
				foreach (string s in attr.StringValueArray) {
					LdapEntry userEntry = conn.Data.GetEntry (s);
					LdapAttribute userNameAttribute = userEntry.getAttribute ("name");
					
					currentMemberStore.AppendValues (userNameAttribute.StringValue);
					currentMembers.Add (userNameAttribute.StringValue);
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

		void populateUsers ()
		{
			LdapEntry[] _users = conn.Data.Search ("(&(objectclass=user)(objectcategory=Person))");

			foreach (LdapEntry le in _users) {
				LdapAttribute nameAttr = le.getAttribute ("name");
				if (nameAttr != null && !currentMembers.Contains (nameAttr.StringValue) )
					allUserStore.AppendValues (nameAttr.StringValue);
			}					
		}

		void Init ()
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
				
				currentMembers.Add (user);				
				currentMemberStore.AppendValues (user);
				
				allUserStore.Remove (ref iter);
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
		
				if (currentMembers.Contains (user))
					currentMembers.Remove (user);

				allUserStore.AppendValues (user);
			}
		}

		LdapEntry CreateEntry (string dn)
		{
			LdapAttributeSet aset = new LdapAttributeSet();
			aset.Add (new LdapAttribute ("objectClass", new string[] {"top", "group"}));
			aset.Add (new LdapAttribute ("cn", groupNameEntry.Text));
			aset.Add (new LdapAttribute ("description", descriptionEntry.Text));
			aset.Add (new LdapAttribute ("sAMAccountName", groupNameEntry.Text));
			aset.Add (new LdapAttribute ("groupType", "-2147483646"));
			
			if (currentMembers.Count >= 1)
				aset.Add (new LdapAttribute ("member", currentMembers.ToArray()));
										
			LdapEntry newEntry = new LdapEntry (dn, aset);
			return newEntry;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			LdapEntry entry = null;
			
			if (isEdit) {
				 entry = CreateEntry (currentEntry.DN);				 
				 
				 LdapEntryAnalyzer lea = new LdapEntryAnalyzer ();
				 lea.Run (currentEntry, entry);
				 
				 if (lea.Differences.Length == 0)
				 	return;
				 	
				 if (!Util.ModifyEntry (conn, entry.DN, lea.Differences))
				 	errorOccured = true;
				 	
			} else {
			
				string userDN = null;
				
				if (this.defaultNewContainer == string.Empty || this.defaultNewContainer == null) {
				
					SelectContainerDialog scd =	new SelectContainerDialog (conn, groupDialog);
					scd.Title = "Save Group";
					scd.Message = String.Format ("Where in the directory would\nyou like save the group\n{0}?", groupNameEntry.Text);
					scd.Run ();

					if (scd.DN == "")
						return;

					userDN = String.Format ("cn={0},{1}", groupNameEntry.Text, scd.DN);
				
				} else {
				
					userDN = String.Format ("cn={0},{1}", groupNameEntry.Text, this.defaultNewContainer);
				}
				
				entry = CreateEntry (userDN);

				string[] missing = LdapEntryAnalyzer.CheckRequiredAttributes (conn, entry);
				if (missing.Length != 0) {
					missingAlert (missing);
					missingValues = true;
					return;
				}

				if (!Util.AddEntry (conn, entry))
					errorOccured = true;
			}
		}
	}
}
