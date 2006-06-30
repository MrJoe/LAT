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
		private LdapServer server;

		public TemplatesDialog (LdapServer ldapServer)
		{
			server = ldapServer;

			ui = new Glade.XML (null, "lat.glade", "templatesDialog", null);
			ui.Autoconnect (this);

			setupTreeViews ();
			listTemplates ();

			templatesDialog.Icon = Global.latIcon;
			templatesDialog.Resize (320, 300);

			templatesDialog.Run ();
			templatesDialog.Destroy ();
		}

		private void listTemplates ()
		{
			_store.Clear ();

			string[] names = Global.Templates.GetTemplateNames ();
			foreach (string n in names)
				_store.AppendValues (n);
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
			TreePath path = args.Path;
			TreeIter iter;
			
			if (_store.GetIter (out iter, path)) {
				string name = null;
				name = (string) _store.GetValue (iter, 0);

				editTemplate (name);
			} 	
		}

		public void OnAddClicked (object o, EventArgs args)
		{
			TemplateEditorDialog ted = new TemplateEditorDialog (server);

			if (ted.UserTemplate == null)
				return;

			Global.Templates.Add (ted.UserTemplate);
			listTemplates ();
		}

		private void editTemplate (string name)
		{
			Template t = Global.Templates.Lookup (name);

			TemplateEditorDialog ted = new TemplateEditorDialog (server, t);

			if (ted.UserTemplate == null)
				return;

			Global.Templates.Update (ted.UserTemplate);
			listTemplates ();
		}

		public void OnEditClicked (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (templateTreeView.Selection.GetSelected (out model, out iter))  {
				string name = (string) model.GetValue (iter, 0);
				editTemplate (name);
			}
		}

		public void OnDeleteClicked (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (!templateTreeView.Selection.GetSelected (out model, out iter)) 
				return;

			string name = (string) model.GetValue (iter, 0);
			
			string tmp = String.Format (
				Mono.Unix.Catalog.GetString ("Are you sure you want to delete:"));

			string msg = String.Format ("{0}\n\n{1}", tmp, name);

			if (Util.AskYesNo (templatesDialog, msg)) {
				Global.Templates.Delete (name);
				listTemplates ();
			}
		}

		public void OnCloseClicked (object o, EventArgs args)
		{
			templatesDialog.HideAll ();
		}
	}
}
