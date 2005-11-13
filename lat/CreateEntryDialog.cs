// 
// lat - CreateEntryDialog.cs
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
	public class CreateEntryDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog createEntryDialog;
		[Glade.Widget] Gtk.Entry rdnEntry;
		[Glade.Widget] Gtk.Button browseButton;
		[Glade.Widget] Gtk.TreeView attrTreeView;

		private LdapServer server;
		private ListStore attrListStore;
		private ArrayList _objectClass;
		private ArrayList _attributes;
		private LdapAttribute objAttr = null;
		private Template t;
		private bool isTemplate = false;

		public CreateEntryDialog (LdapServer ldapServer, Template theTemplate)
		{
			server = ldapServer;
			t = theTemplate;
			isTemplate = true;

			Init ();

			foreach (string s in t.Classes)
			{
				attrListStore.AppendValues ("objectClass", s, "Optional");
				_objectClass.Add (s);
			}

			showAttributes ();

			createEntryDialog.Run ();
			createEntryDialog.Destroy ();
		}

		public CreateEntryDialog (LdapServer ldapServer, LdapEntry le)
		{
			server = ldapServer;

			Init ();

			LdapAttribute la = le.getAttribute ("objectClass");

			foreach (string s in la.StringValueArray)
			{
				attrListStore.AppendValues ("objectClass", s, "Optional");
				_objectClass.Add (s);
			}

			showAttributes ();

			createEntryDialog.Run ();
			createEntryDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "createEntryDialog", null);
			ui.Autoconnect (this);

			_objectClass = new ArrayList ();
			_attributes = new ArrayList ();
			
			setupTreeViews ();

			browseButton.Label = server.DirectoryRoot;

			createEntryDialog.Resize (320, 200);
		}

		private void showAttributes ()
		{
			string[] required, optional;			
			server.GetAllAttributes (_objectClass, out required, out optional);

			foreach (string s in required)
			{
				if (s == "objectClass")
					continue;

				if (isTemplate)
				{
					attrListStore.AppendValues (s, 
						t.GetAttributeDefaultValue (s),
						"Required");
				}
				else
				{
					attrListStore.AppendValues (s, 
						"",
						"Required");
				}
			}

			foreach (string s in optional)
			{
				if (isTemplate)
				{
					attrListStore.AppendValues (s, 
						t.GetAttributeDefaultValue (s),
						"Optional");
				}
				else
				{
					attrListStore.AppendValues (s, 
						"",
						"Optional");
				}
			}
		}

		private void setupTreeViews ()
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

		private void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!attrListStore.GetIterFromString (out iter, args.Path))
			{
				return;
			}

			attrListStore.SetValue (iter, 1, args.NewText);		
		}

		public void OnBrowseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = 
				new SelectContainerDialog (server, createEntryDialog);

			scd.Message = String.Format (
				Mono.Unix.Catalog.GetString (
				"Where in the directory would\nyou like to save the entry?"));

			scd.Title = Mono.Unix.Catalog.GetString ("Select entry base");
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (server.Host))
				browseButton.Label = scd.DN;
		}

		private bool attrForeachFunc (TreeModel model, TreePath path, TreeIter iter)
		{
			if (!attrListStore.IterIsValid (iter))
				return true;

			string _name = null;
			string _value = null;

			_name = (string) attrListStore.GetValue (iter, 0);
			_value = (string) attrListStore.GetValue (iter, 1);

			if (_name == null || _value == null || _value == "")
				return false;

			if (_name.ToLower() == "objectclass")
			{
				if (objAttr == null)
				{
					objAttr = new LdapAttribute (_name, _value);
				}
				else
				{
					objAttr.addValue (_value);
				}
			}
			else
			{
				LdapAttribute attr = new LdapAttribute (_name, _value);
				_attributes.Add (attr);
			}

			return false;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			string dn = String.Format ("{0},{1}", 
				rdnEntry.Text, browseButton.Label);

			attrListStore.Foreach (new TreeModelForeachFunc (attrForeachFunc));

			_attributes.Add (objAttr);

			Util.AddEntry (server, createEntryDialog, dn, _attributes, true);

			createEntryDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			createEntryDialog.HideAll ();
		}
	}
}
