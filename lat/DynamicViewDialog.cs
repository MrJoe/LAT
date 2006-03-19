// 
// lat - DynamicViewDialog.cs
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
	public class DynamicViewDialog : ViewDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog dynamicDialog;
		[Glade.Widget] TreeView attrTreeview;

		private ListStore attrStore;
		private bool _isEdit;
		
		private LdapEntry _le;
		private ArrayList _modList;
//		private Hashtable _di;

		public DynamicViewDialog (LdapServer ldapServer) : base (ldapServer)
		{
			Init ();

			dynamicDialog.Icon = Global.latIcon;
			dynamicDialog.Title = "LAT - Add Entry";

			while (errorOccured) {
				errorOccured = false;
				dynamicDialog.Run ();				
			}

			dynamicDialog.Run ();
			dynamicDialog.Destroy ();
		}

		private void showEntryAttributes (LdapEntry entry)
		{
			attrStore.Clear ();
			_modList.Clear ();
		
			LdapAttributeSet attributeSet = entry.getAttributeSet ();
			
			foreach (LdapAttribute a in attributeSet) {
				string[] svalues;
				svalues = a.StringValueArray;
							
				foreach (string s in svalues)
					attrStore.AppendValues (a.Name, s);
			}		
		}

		public DynamicViewDialog (LdapServer ldapServer, LdapEntry le) : base (ldapServer)
		{
			_le = le;
			_modList = new ArrayList ();

			_isEdit = true;

			Init ();

			showEntryAttributes (le);

			LdapAttribute a = le.getAttribute ("cn");

			if (a != null)
				dynamicDialog.Title = a.StringValue + " Properties";

			dynamicDialog.Run ();
			dynamicDialog.Destroy ();
		}

		private void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "dynamicDialog", null);
			ui.Autoconnect (this);

			viewDialog = dynamicDialog;

			TreeViewColumn col;

			attrStore = new ListStore (typeof (string), typeof (string));
			attrTreeview.Model = attrStore;

			col = attrTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			CellRendererText cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnAttributeEdit);

			col = attrTreeview.AppendColumn ("Value", cell, "text", 1);

			attrStore.SetSortColumnId (0, SortType.Ascending);
		
			dynamicDialog.Resize (350, 400);
		}

		private void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!attrStore.GetIterFromString (out iter, args.Path))
				return;

			string oldText = (string) attrStore.GetValue (iter, 1);

			if (oldText.Equals (args.NewText))
				return;
			
			string _name = (string) attrStore.GetValue (iter, 0);

			attrStore.SetValue (iter, 1, args.NewText);

			LdapAttribute attribute = new LdapAttribute (_name, args.NewText);
			LdapModification lm = new LdapModification (LdapModification.REPLACE, attribute);

			_modList.Add (lm);
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			if (_isEdit) {
				if (!Util.ModifyEntry (server, dynamicDialog, _le.DN, _modList, true)) {
					errorOccured = true;
					return;
				}
			}

			dynamicDialog.HideAll ();
		}
	}
}
