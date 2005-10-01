// 
// lat - TemplateEditorDialog.cs
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
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class TemplateEditorDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog templateEditorDialog;
		[Glade.Widget] Gtk.Entry nameEntry;
		[Glade.Widget] Gtk.HBox attrClassHBox;
		[Glade.Widget] TreeView objTreeView; 
		[Glade.Widget] TreeView attrTreeView; 

		private ListStore objListStore;
		private ListStore attrListStore;

		private ArrayList _objectClass;
		private ArrayList _attributes;

		private Connection _conn;
		private ComboBox attrClassComboBox;

		public TemplateEditorDialog (Connection conn)
		{
			_conn = conn;

			ui = new Glade.XML (null, "lat.glade", "templateEditorDialog", null);
			ui.Autoconnect (this);
			
			createCombos ();
			setupTreeViews ();
	
			templateEditorDialog.Resize (640, 480);

			templateEditorDialog.Run ();
			templateEditorDialog.Destroy ();
		}

		private void setupTreeViews ()
		{
			// Object class
			objListStore = new ListStore (typeof (string), typeof (string), typeof (string));
			objTreeView.Model = attrListStore;
			
			TreeViewColumn col;
			col = objTreeView.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			objListStore.SetSortColumnId (0, SortType.Ascending);

			// Attributes
			attrListStore = new ListStore (typeof (string), typeof (string), typeof (string));
			attrTreeView.Model = attrListStore;
			
			col = attrTreeView.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			col = attrTreeView.AppendColumn ("Type", new CellRendererText (), "text", 1);
			col.SortColumnId = 1;

			CellRendererText cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnAttributeEdit);

			col = attrTreeView.AppendColumn ("Value", cell, "text", 2);
			col.SortColumnId = 2;

			attrListStore.SetSortColumnId (0, SortType.Ascending);
		}

		private void createCombos ()
		{
			// class
			attrClassComboBox = ComboBox.NewText ();
			
			ArrayList ocs = _conn.getObjClasses ();			
			ArrayList tmp = new ArrayList ();

			foreach (LdapEntry le in ocs)
			{
				LdapAttribute la = le.getAttribute ("objectclasses");
						
				foreach (string s in la.StringValueArray)
				{
					SchemaParser sp = new SchemaParser (s);
					tmp.Add (sp.Names[0]);
				}
			}

			tmp.Sort ();

			foreach (string n in tmp)
			{
				attrClassComboBox.AppendText (n);
			}

			attrClassComboBox.Active = 0;
			attrClassComboBox.Show ();

			attrClassHBox.PackStart (attrClassComboBox, true, true, 5);
		}

		private void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!attrListStore.GetIterFromString (out iter, args.Path))
			{
				return;
			}

			string oldText = (string) attrListStore.GetValue (iter, 2);

			if (oldText.Equals (args.NewText))
			{
				// no modification
				return;
			}
			
			attrListStore.SetValue (iter, 2, args.NewText);		
		}

		public void OnAddClicked (object o, EventArgs args)
		{
			TreeIter iter;
				
			if (!attrClassComboBox.GetActiveIter (out iter))
				return;

			attrListStore.Clear ();

			string objClass = (string) attrClassComboBox.Model.GetValue (iter, 0);
			_objectClass.Add (objClass);

			string[] required, optional;			

			_conn.getAllAttrs (_objectClass, out required, out optional);

			foreach (string s in required)
			{
				attrListStore.AppendValues (s, "Required", "");
			}

			foreach (string s in optional)
			{
				attrListStore.AppendValues (s, "Optional", "");
			}
		}

		public void OnObjRemoveClicked (object o, EventArgs args)
		{
		}

		public void OnObjClearClicked (object o, EventArgs args)
		{
		}

		public void OnAttrClearClicked (object o, EventArgs args)
		{
			attrListStore.Clear ();
			_objectClass.Clear ();
		}

		public void OnAttrRemoveClicked (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (attrTreeView.Selection.GetSelected (out model, out iter)) 
			{
				attrListStore.Remove (ref iter);
			}
		}

		private bool attrForeachFunc (TreeModel model, TreePath path, TreeIter iter)
		{
			if (!attrListStore.IterIsValid (iter))
				return true;

			string _name = null;
			string _value = null;

			_name = (string) attrListStore.GetValue (iter, 0);
			_value = (string) attrListStore.GetValue (iter, 2);

			if (_name == null || _value == null || _value == "")
				return false;

			LdapAttribute attr = new LdapAttribute (_name, _value);

			_attributes.Add (attr);

			return false;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
/*
			_dn = dnEntry.Text;

			LdapAttribute a = new LdapAttribute ("objectClass", "top");

			foreach (string s in _objectClass)
			{
				a.addValue (s);
			}
			
			_attributes.Add (a);

			attrListStore.Foreach (new TreeModelForeachFunc (attrForeachFunc));

			Util.AddEntry (_conn, addEntryDialog, _dn, _attributes, true);
*/
			templateEditorDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			templateEditorDialog.HideAll ();
		}
	}
}
