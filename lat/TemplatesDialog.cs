// 
// lat - TemplatesDialog.cs
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

using System;
using System.Collections;

using Gtk;
using Glade;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class TemplatesDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog templatesDialog;
		[Glade.Widget] Gtk.TreeView templateTreeView;

		private ListStore _store;
		private Connection _conn;

		public TemplatesDialog (Connection conn)
		{
			_conn = conn;

			ui = new Glade.XML (null, "lat.glade", "templatesDialog", null);
			ui.Autoconnect (this);

			setupTreeViews ();
		
			templatesDialog.Resize (320, 300);

			templatesDialog.Run ();
			templatesDialog.Destroy ();
		}


		private void setupTreeViews ()
		{
			_store = new ListStore (typeof (string));
			templateTreeView.Model = _store;
			
			TreeViewColumn col;
			col = templateTreeView.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			_store.SetSortColumnId (0, SortType.Ascending);
		}

		public void OnRowActivated (object o, RowActivatedArgs args)
		{
		}

		public void OnAddClicked (object o, EventArgs args)
		{
			TemplateEditorDialog ted = new TemplateEditorDialog (_conn);
		}

		public void OnEditClicked (object o, EventArgs args)
		{
		}

		public void OnRemoveClicked (object o, EventArgs args)
		{
		}

		public void OnCloseClicked (object o, EventArgs args)
		{
			templatesDialog.HideAll ();
		}
	}
}
