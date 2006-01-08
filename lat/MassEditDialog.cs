// 
// lat - MassEditDialog.cs
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
using Novell.Directory.Ldap;

namespace lat
{
	public class MassEditDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog massEditDialog;
		[Glade.Widget] Gtk.Entry searchEntry;
		[Glade.Widget] Gtk.Entry nameEntry;
		[Glade.Widget] Gtk.Entry valueEntry;
		[Glade.Widget] Gtk.HBox actionHBox;
		[Glade.Widget] TreeView modListView; 

		private ListStore modListStore;
		private ArrayList _modList;
		private LdapServer server;

		private ComboBox actionComboBox;

		public MassEditDialog (LdapServer ldapServer)
		{
			_modList = new ArrayList ();
			server = ldapServer;

			ui = new Glade.XML (null, "lat.glade", "massEditDialog", null);
			ui.Autoconnect (this);
			
			createCombos ();

			modListStore = new ListStore (typeof (string), typeof (string), typeof (string));
			modListView.Model = modListStore;
			
			TreeViewColumn col;
			col = modListView.AppendColumn ("Action", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			col = modListView.AppendColumn ("Name", new CellRendererText (), "text", 1);
			col.SortColumnId = 1;

			col = modListView.AppendColumn ("Value", new CellRendererText (), "text", 2);
			col.SortColumnId = 2;

			modListStore.SetSortColumnId (0, SortType.Ascending);

			massEditDialog.Resize (300, 450);

			massEditDialog.Run ();
			massEditDialog.Destroy ();
		}

		private void createCombos ()
		{
			// class
			actionComboBox = ComboBox.NewText ();
			actionComboBox.AppendText ("Add");
			actionComboBox.AppendText ("Delete");
			actionComboBox.AppendText ("Replace");

			actionComboBox.Active = 0;
			actionComboBox.Show ();

			actionHBox.PackStart (actionComboBox, true, true, 5);
		}

		public void OnSearchClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			searchEntry.Text = sbd.UserFilter;
		}

		public void OnAddClicked (object o, EventArgs args)
		{
			TreeIter iter;
				
			if (!actionComboBox.GetActiveIter (out iter))
				return;

			string action = (string) actionComboBox.Model.GetValue (iter, 0);

			modListStore.AppendValues (action, nameEntry.Text, valueEntry.Text);
		}

		public void OnClearClicked (object o, EventArgs args)
		{
			modListStore.Clear ();
			_modList.Clear ();
		}

		public void OnRemoveClicked (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (modListView.Selection.GetSelected (out model, out iter)) 
				modListStore.Remove (ref iter);
		}

		private bool attrForeachFunc (TreeModel model, TreePath path, TreeIter iter)
		{
			if (!modListStore.IterIsValid (iter))
				return true;

			string _name = null;
			string _value = null;
			string _action = null;

			_action = (string) modListStore.GetValue (iter, 0);
			_name = (string) modListStore.GetValue (iter, 1);
			_value = (string) modListStore.GetValue (iter, 2);

			LdapAttribute a = new LdapAttribute (_name, _value);
			LdapModification m;

			switch (_action) {

			case "Add":
				m = new LdapModification (LdapModification.ADD, a);
				break;

			case "Delete":
				m = new LdapModification (LdapModification.DELETE, a);
				break;

			case "Replace":
				m = new LdapModification (LdapModification.REPLACE, a);
				break;

			default:
				return true;
			}

			_modList.Add (m);

			return false;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			LdapEntry[] sr = server.Search (server.DirectoryRoot, searchEntry.Text);

			modListStore.Foreach (new TreeModelForeachFunc (attrForeachFunc));

			foreach (LdapEntry e in sr) {
				ArrayList tmp = (ArrayList) _modList.Clone ();
				Util.ModifyEntry (server, massEditDialog, e.DN, tmp, false);
			}

			massEditDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			massEditDialog.HideAll ();
		}

		public void OnDlgDelete (object o, DeleteEventArgs args)
		{
			massEditDialog.HideAll ();
		}
	}
}
