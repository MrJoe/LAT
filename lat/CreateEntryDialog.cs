// 
// lat - CreateEntryDialog.cs
// Author: Loren Bandiera
// Copyright 2005-2006 MMG Security, Inc.
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
using System.Collections.Generic;
using Novell.Directory.Ldap;
using Gtk;

namespace lat
{
	public class CreateEntryDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog createEntryDialog;
		[Glade.Widget] Gtk.Entry rdnEntry;
		[Glade.Widget] Gtk.Button browseButton;
		[Glade.Widget] Gtk.TreeView attrTreeView;

		Connection conn;
		ListStore attrListStore;
		List<string> _objectClass;
		LdapAttribute objAttr = null;
		Template t;
		bool isTemplate = false;
		bool errorOccured = false;

		public CreateEntryDialog (Connection connection, Template theTemplate)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");
			if (theTemplate == null)
				throw new ArgumentNullException("theTemplate");
			
			conn = connection;
			t = theTemplate;
			isTemplate = true;

			Init ();

			foreach (string s in t.Classes) {
				attrListStore.AppendValues ("objectClass", s, "Optional");
				_objectClass.Add (s);
			}

			showAttributes ();

			createEntryDialog.Icon = Global.latIcon;
			
			createEntryDialog.Run ();
		
				createEntryDialog.Run ();

			createEntryDialog.Destroy ();
		}

		public CreateEntryDialog (Connection connection, LdapEntry le)
		{
			if (connection == null)
				throw new ArgumentNullException("connection");
			if (le == null)
				throw new ArgumentNullException("le");
			
			conn = connection;

			Init ();

			LdapAttribute la = le.getAttribute ("objectClass");

			foreach (string s in la.StringValueArray) {
				attrListStore.AppendValues ("objectClass", s, "Optional");
				_objectClass.Add (s);
			}

			showAttributes ();

			createEntryDialog.Run ();
			while (errorOccured)
				createEntryDialog.Run ();

			createEntryDialog.Destroy ();
		}

		void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "createEntryDialog", null);
			ui.Autoconnect (this);

			_objectClass = new List<string> ();
			
			setupTreeViews ();

			browseButton.Label = conn.DirectoryRoot;

			createEntryDialog.Resize (320, 200);
		}

		void insertValues (string[] values, string valueType)
		{
			foreach (string s in values) {
				if (s == "objectClass")
					continue;

				if (isTemplate)
					attrListStore.AppendValues (s, t.GetAttributeDefaultValue (s), valueType);
				else
					attrListStore.AppendValues (s, "", valueType);
			}
		}

		void showAttributes ()
		{
			string[] required, optional;
			conn.Data.GetAllAttributes (_objectClass, out required, out optional);

			insertValues (required, "Required");
			insertValues (optional, "Optional");
		}

		void setupTreeViews ()
		{
			// Attributes
			attrListStore = new ListStore (typeof (string), typeof (string), typeof (string));
			attrTreeView.Model = attrListStore;
			
			TreeViewColumn col;
			col = attrTreeView.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			CellRendererText cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnAttributeEdit);

			col = attrTreeView.AppendColumn ("Value", cell, "text", 1);
			col.SortColumnId = 1;

			col = attrTreeView.AppendColumn ("Type", new CellRendererText (), "text", 2);
			col.SortColumnId = 2;

			attrListStore.SetSortColumnId (0, SortType.Ascending);
		}

		void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!attrListStore.GetIterFromString (out iter, args.Path))
				return;

			attrListStore.SetValue (iter, 1, args.NewText);		
		}

		public void OnBrowseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = new SelectContainerDialog (conn, createEntryDialog);

			scd.Message = String.Format (
				Mono.Unix.Catalog.GetString (
				"Where in the directory would\nyou like to save the entry?"));

			scd.Title = Mono.Unix.Catalog.GetString ("Select entry base");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (conn.Settings.Host))
				browseButton.Label = scd.DN;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			string dn = String.Format ("{0},{1}", rdnEntry.Text, browseButton.Label);
			LdapAttributeSet lset = new LdapAttributeSet ();
			
			foreach (object[] row in attrListStore) {
			
				string n = (string) row[0];
				string v = (string) row[1];

				if (n == null || v == null || v == "")
					continue;

				if (n.ToLower() == "objectclass") {
					if (objAttr == null)
						objAttr = new LdapAttribute (n, v);
					else
						objAttr.addValue (v);

				} else {

					LdapAttribute attr = new LdapAttribute (n, v);
					lset.Add (attr);
				}				
			}
			
			lset.Add (objAttr);
			
			LdapEntry entry = new LdapEntry (dn, lset);
			if (!Util.AddEntry (conn, entry))
				errorOccured = true;
			else
				errorOccured = false;
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			errorOccured = false;
			createEntryDialog.HideAll ();
		}
	}
}
