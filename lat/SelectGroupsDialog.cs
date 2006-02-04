// 
// lat - SelectGroupsDialog.cs
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

namespace lat
{
	public class SelectGroupsDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog selectGroupsDialog;
		[Glade.Widget] Gtk.TreeView allGroupsTreeview;
		
		private ListStore store;
		private ArrayList groups;

		public SelectGroupsDialog (string[] allGroups)
		{
			ui = new Glade.XML (null, "lat.glade", "selectGroupsDialog", null);
			ui.Autoconnect (this);

			groups = new ArrayList ();

			TreeViewColumn col;

			store = new ListStore (typeof (string));
			allGroupsTreeview.Model = store;
			allGroupsTreeview.Selection.Mode = SelectionMode.Multiple;

			col = allGroupsTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;
	
			store.SetSortColumnId (0, SortType.Ascending);

			foreach (string s in allGroups)
				store.AppendValues (s);
			
			selectGroupsDialog.Icon = Global.latIcon;
			selectGroupsDialog.Resize (320, 200);
			selectGroupsDialog.Run ();
			selectGroupsDialog.Destroy ();
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			TreePath[] tp = allGroupsTreeview.Selection.GetSelectedRows (out model);

			foreach (TreePath t in tp) {

				store.GetIter (out iter, t);

				string name = (string) store.GetValue (iter, 0);
				groups.Add (name);
			}

			selectGroupsDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			selectGroupsDialog.HideAll ();
		}

		public string[] SelectedGroupNames 
		{
			get { return (string[]) groups.ToArray (typeof (string)); }
		}
	}
}
