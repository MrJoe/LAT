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
		[Glade.Widget] TreeView objTreeView; 
		[Glade.Widget] TreeView attrTreeView; 

		private ListStore objListStore;
		private ListStore attrListStore;

		private ArrayList _objectClass;
		private Template t = null;

		private LdapServer server;
		private bool _isEdit = false;

		public TemplateEditorDialog (LdapServer ldapServer)
		{
			server = ldapServer;
		
			Init ();

			templateEditorDialog.Run ();
			templateEditorDialog.Destroy ();
		}

		public TemplateEditorDialog (LdapServer ldapServer, Template theTemplate)
		{
			server = ldapServer;
			_isEdit = true;

			t = theTemplate;

			Init ();

			nameEntry.Text = t.Name;
			nameEntry.Sensitive = false;

			foreach (string s in t.Classes) {
				objListStore.AppendValues (s);
				_objectClass.Add (s);
			}

			ShowAttributes ();

			templateEditorDialog.Run ();
			templateEditorDialog.Destroy ();
		}

		private void Init ()
		{
			_objectClass = new ArrayList ();

			ui = new Glade.XML (null, "lat.glade", "templateEditorDialog", null);
			ui.Autoconnect (this);
			
			setupTreeViews ();
	
			templateEditorDialog.Icon = Global.latIcon;
			templateEditorDialog.Resize (640, 480);
		}

		private void setupTreeViews ()
		{
			// Object class
			objListStore = new ListStore (typeof (string));
			objTreeView.Model = objListStore;
			
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

			col = attrTreeView.AppendColumn ("Default Value", cell, "text", 2);
			col.SortColumnId = 2;

			attrListStore.SetSortColumnId (0, SortType.Ascending);
		}

		void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!attrListStore.GetIterFromString (out iter, args.Path))
				return;

			string oldText = (string) attrListStore.GetValue (iter, 2);

			if (oldText.Equals (args.NewText))
				return;
			
			attrListStore.SetValue (iter, 2, args.NewText);		
		}

		void ShowAttributes ()
		{
			attrListStore.Clear ();

			string[] required, optional;			
			server.GetAllAttributes (_objectClass, out required, out optional);

			foreach (string s in required) {

				if (_isEdit) {
					attrListStore.AppendValues (s, 
						"Required", 
						t.GetAttributeDefaultValue (s));
				} else {

					attrListStore.AppendValues (s, "Required", "");
				}
			}

			foreach (string s in optional) {

				if (_isEdit) {
					attrListStore.AppendValues (s, 
						"Required", 
						t.GetAttributeDefaultValue (s));

				} else {

					attrListStore.AppendValues (s, "Optional", "");
				}
			}
		}

		public void OnObjAddClicked (object o, EventArgs args)
		{
			AddObjectClassDialog dlg = new AddObjectClassDialog (server);				
			foreach (string s in dlg.ObjectClasses) {
				if (_objectClass.Contains (s))
					continue;
	
				_objectClass.Add (s);
				objListStore.AppendValues (s);			
			}

			ShowAttributes ();
		}

		public void OnObjRemoveClicked (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (objTreeView.Selection.GetSelected (out model, out iter))  {
				string objClass = (string) model.GetValue (iter, 0);
				_objectClass.Remove (objClass);

				objListStore.Remove (ref iter);

				ShowAttributes ();
			}
		}

		public void OnObjClearClicked (object o, EventArgs args)
		{
			objListStore.Clear ();
			_objectClass.Clear ();

			attrListStore.Clear ();
		}

		public void OnAttrClearClicked (object o, EventArgs args)
		{
			attrListStore.Clear ();
		}

		public void OnAttrRemoveClicked (object o, EventArgs args)
		{
			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (attrTreeView.Selection.GetSelected (out model, out iter)) 
				attrListStore.Remove (ref iter);
		}

		bool attrForeachFunc (TreeModel model, TreePath path, TreeIter iter)
		{
			if (!attrListStore.IterIsValid (iter))
				return true;

			string _name = null;
			string _value = null;

			_name = (string) attrListStore.GetValue (iter, 0);
			_value = (string) attrListStore.GetValue (iter, 2);

			if (_name == null || _value == null || _value == "")
				return false;

			t.AddAttribute (_name, _value);

			return false;
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			if (_isEdit) {
				t.Name = nameEntry.Text;
				t.ClearAttributes ();
			} else {

				t = new Template (nameEntry.Text);
			}

			t.AddClass (_objectClass);	

			attrListStore.Foreach (new TreeModelForeachFunc (attrForeachFunc));
		}

		public Template UserTemplate
		{
			get { return t; }
		}
	}
}
