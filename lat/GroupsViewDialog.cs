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
using GLib;
using Glade;
using System;
using System.Collections;
using Novell.Directory.Ldap;

namespace lat
{
	public class GroupsViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog groupDialog;
		[Glade.Widget] Gtk.Entry groupNameEntry;
		[Glade.Widget] Gtk.SpinButton groupIDSpinButton;
		[Glade.Widget] Gtk.TreeView allUsersTreeview;
		[Glade.Widget] Gtk.TreeView currentMembersTreeview;
		[Glade.Widget] Gtk.Button addButton;
		[Glade.Widget] Gtk.Button removeButton;
		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private ListStore allUserStore;
		private ListStore currentMemberStore;

		private Hashtable _gi;
		private Hashtable _currentMembers = new Hashtable ();

		private bool _isEdit;
		
		private LdapEntry _le;
		private ArrayList _modList;

		private static string[] groupAttrs = { "cn", "gidNumber" };

		public GroupsViewDialog (lat.Connection conn) : base (conn)
		{
			Init ();

			populateUsers ();

			groupDialog.Title = "Add Group";

			groupDialog.Run ();

			if (missingValues)
			{
				missingValues = false;
				groupDialog.Run ();				
			}
			else
			{
				groupDialog.Destroy ();
			}
		}

		public GroupsViewDialog (lat.Connection conn, LdapEntry le) : base (conn)
		{
			_le = le;
			_modList = new ArrayList ();

			Logger.Log.Debug ("GroupsViewDialog: _modList == {0}", _modList.Count);

			_isEdit = true;

			Init ();

			_gi = getEntryInfo (groupAttrs, le);

			string groupName = (string) _gi ["cn"];

			groupDialog.Title = groupName + " Properties";
			groupNameEntry.Text = groupName;
			groupIDSpinButton.Value = int.Parse ((string) _gi ["gidNumber"]);

			LdapAttribute attr;
			attr = le.getAttribute ("memberuid");

			if (attr != null)
			{
				string[] svalues = attr.StringValueArray;


				foreach (string s in svalues)
				{
					currentMemberStore.AppendValues (s);
					_currentMembers.Add (s, "memberuid");
				}

			}

			populateUsers ();

			groupDialog.Run ();

			if (missingValues)
			{
				missingValues = false;
				groupDialog.Run ();				
			}
			else
			{
				groupDialog.Destroy ();
			}
		}

		private void populateUsers ()
		{
			ArrayList _users = _conn.SearchByClass ("posixAccount");

			foreach (LdapEntry le in _users)
			{
				LdapAttribute nameAttr;

				nameAttr = le.getAttribute ("uid");

				if (nameAttr != null && !_currentMembers.ContainsKey (nameAttr.StringValue) )
				{
					allUserStore.AppendValues (nameAttr.StringValue);
				}
			}					
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "groupDialog", null);
			ui.Autoconnect (this);

			_viewDialog = groupDialog;

			TreeViewColumn col;

			allUserStore = new ListStore (typeof (string));
			allUsersTreeview.Model = allUserStore;

			col = allUsersTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			allUserStore.SetSortColumnId (0, SortType.Ascending);
			
			currentMemberStore = new ListStore (typeof (string));
			currentMembersTreeview.Model = currentMemberStore;

			col = currentMembersTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			currentMemberStore.SetSortColumnId (0, SortType.Ascending);

			addButton.Clicked += new EventHandler (OnAddClicked);
			removeButton.Clicked += new EventHandler (OnRemoveClicked);
			
			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			groupDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);

			groupDialog.Resize (350, 400);			
		}

		private void OnAddClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (allUsersTreeview.Selection.GetSelected (out model, out iter))
			{
				string user = (string) allUserStore.GetValue (iter, 0);
				
				currentMemberStore.AppendValues (user);
		
				if (!_currentMembers.ContainsKey (user))
					_currentMembers.Add (user, "memberuid");

				allUserStore.Remove (ref iter);

				Logger.Log.Debug ("Adding {0} to group", user);

				if (_isEdit)
				{
					LdapAttribute attr = new LdapAttribute ("memberuid", user);
					LdapModification lm = new LdapModification (LdapModification.ADD, attr);
					_modList.Add (lm);

					Logger.Log.Debug ("OnAddClicked: _modList == {0}", _modList.Count);
				}

			}
			else
			{
				Util.MessageBox (groupDialog, "No user selected to add.",
						 MessageType.Error);
			}
		}

		private void OnRemoveClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (currentMembersTreeview.Selection.GetSelected (out model, out iter))
			{
				string user = (string) currentMemberStore.GetValue (iter, 0);

				currentMemberStore.Remove (ref iter);
		
				if (_currentMembers.ContainsKey (user))
					_currentMembers.Remove (user);

				Logger.Log.Debug ("Removing user {0} from group", user);

				allUserStore.AppendValues (user);

				if (_isEdit)
				{
					LdapAttribute attr = new LdapAttribute ("memberuid", user);
					LdapModification lm = new LdapModification (LdapModification.DELETE, attr);
					_modList.Add (lm);
				}
			}
			else
			{
				Util.MessageBox (groupDialog, "No user selected to remove.",
						 MessageType.Error);
			}			
		}

		private Hashtable getCurrentGroupInfo ()
		{
			Hashtable retVal = new Hashtable ();

			retVal.Add ("cn", groupNameEntry.Text);
			retVal.Add ("gidNumber", groupIDSpinButton.Value.ToString());

			return retVal;
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			Hashtable cgi = getCurrentGroupInfo ();

			string[] objClass = { "top", "posixGroup" };
			string[] missing = null;

			if (!checkReqAttrs (objClass, cgi, out missing))
			{
				missingAlert (missing);
				missingValues = true;

				return;
			}


			if (_isEdit)
			{
				if (_modList.Count == 0)
				{
					_modList = getMods (groupAttrs, _gi, cgi);
				}
				else
				{
					ArrayList tmp = getMods (groupAttrs, _gi, cgi);
					foreach (LdapModification lm in tmp)
					{
						_modList.Add (lm);
					}
				}
	
				Util.ModifyEntry (_conn, _viewDialog, _le.DN, _modList);
			}
			else
			{
				ArrayList attrList = getAttributes (objClass, groupAttrs, cgi);

				foreach (string key in _currentMembers.Keys)
				{
					LdapAttribute attr = new LdapAttribute ("memberuid", key);
					attrList.Add (attr);
				}

				SelectContainerDialog scd = 
					new SelectContainerDialog ( _conn, groupDialog);

				scd.Title = "Save Group";
				scd.Message = String.Format ("Where in the directory would\nyou like save the group\n{0}?", (string)cgi["cn"]);

				scd.Run ();

				if (scd.DN == "")
					return;

				string userDN = String.Format ("cn={0},{1}", (string)cgi["cn"], scd.DN);

				Util.AddEntry (_conn, _viewDialog, userDN, attrList);
			}

			groupDialog.HideAll ();
		}
	}
}
