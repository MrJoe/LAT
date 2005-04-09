// 
// lat - CustomViewDialog.cs
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
	public class CustomViewDialog
	{
		[Glade.Widget] Gtk.Dialog customViewDialog;
		[Glade.Widget] Gtk.Entry nameEntry;
		[Glade.Widget] Gtk.Entry filterEntry;
		[Glade.Widget] Button searchBuilderButton;
		[Glade.Widget] Button searchBaseButton;
		[Glade.Widget] Button testButton;
		[Glade.Widget] TreeView allAttrTreeview;
		[Glade.Widget] TreeView viewAttrTreeview;
		[Glade.Widget] Button addButton;
		[Glade.Widget] Button removeButton;
		[Glade.Widget] Button saveButton;
		[Glade.Widget] Button cancelButton;

		Glade.XML ui;

		private ListStore allStore;
		private ListStore viewStore;
	
		private Hashtable _viewAttrs = new Hashtable ();
		private lat.Connection _conn;
		private CustomViewManager _cvm;

		private bool _isEdit = false;
		private string _oldName;

		private string _name = null;

		public CustomViewDialog (lat.Connection conn, CustomViewManager cvm)
		{
			_conn = conn;
			_cvm = cvm;

			Init ();

			customViewDialog.Title = "LAT - New Custom View";

			searchBaseButton.Label = _conn.LdapRoot;
		}

		public CustomViewDialog (lat.Connection conn, CustomViewManager cvm, string name)
		{
			_conn = conn;
			_cvm = cvm;

			_isEdit = true;
			_oldName = name;

			Init ();
			
			customViewDialog.Title = "LAT - Edit Custom View";

			CustomViewData cvd = cvm.Lookup (name);

			nameEntry.Text = cvd.Name;
			filterEntry.Text = cvd.Filter;
			searchBaseButton.Label = cvd.Base;

			char[] delimStr = { ',' };
			string[] cols = cvd.Cols.Split (delimStr);

			foreach (string c in cols)
			{
				_viewAttrs.Add (c, c);
				viewStore.AppendValues (c);
			}

			checkFilter ();
		}

		public void Run ()
		{
			customViewDialog.Run ();
			customViewDialog.Destroy ();
		}

		private void Init ()
		{		
			ui = new Glade.XML (null, "lat.glade", "customViewDialog", null);
			ui.Autoconnect (this);

			TreeViewColumn col;

			allStore = new ListStore (typeof (string));
			allAttrTreeview.Model = allStore;

			col = allAttrTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			allStore.SetSortColumnId (0, SortType.Ascending);
		
			viewStore = new ListStore (typeof (string));
			viewAttrTreeview.Model = viewStore;

			col = viewAttrTreeview.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			viewStore.SetSortColumnId (0, SortType.Ascending);

			testButton.Clicked += new EventHandler (OnTestClicked);
			addButton.Clicked += new EventHandler (OnAddClicked);
			removeButton.Clicked += new EventHandler (OnRemoveClicked);
			saveButton.Clicked += new EventHandler (OnSaveClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);
			searchBaseButton.Clicked += new EventHandler (OnSearchBaseClicked);
			searchBuilderButton.Clicked += new EventHandler (OnSearchBuilderClicked);

			customViewDialog.Resize (350, 400);
		}

		private void OnSearchBaseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = 
				new SelectContainerDialog (_conn, customViewDialog);

			scd.Message = String.Format ("Where in the directory would\nyou like to start the search?");
			scd.Title = "Select search base";
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (_conn.Host))
				searchBaseButton.Label = scd.DN;
		}

		private void fillAttrs (LdapEntry le)
		{
			allStore.Clear ();

			LdapAttributeSet attributeSet = le.getAttributeSet ();

			foreach (LdapAttribute attr in attributeSet)
			{
				if (!_viewAttrs.ContainsKey (attr.Name))
					allStore.AppendValues (attr.Name);
			}
		}

		private void checkFilter ()
		{
			ArrayList res;

			res = _conn.Search (
				searchBaseButton.Label, filterEntry.Text);

			if (res != null)
			{
				LdapEntry le = (LdapEntry) res[0];
				fillAttrs (le);
			}
			else
			{
				Util.MessageBox (customViewDialog, 
						"Invalid search filter.", 
						MessageType.Error);
			}
		}

		private void OnTestClicked (object o, EventArgs args)
		{
			checkFilter ();
		}

		private void OnAddClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (allAttrTreeview.Selection.GetSelected (out model, out iter))
			{
				string name = (string) allStore.GetValue (iter, 0);
				
				viewStore.AppendValues (name);
		
				if (!_viewAttrs.ContainsKey (name))
					_viewAttrs.Add (name, name);

				allStore.Remove (ref iter);
			}
			else
			{
				Util.MessageBox (customViewDialog, 
						"No attribute selected to add.",
						 MessageType.Error);
			}
		}

		private void OnRemoveClicked (object o, EventArgs args)
		{
			TreeModel model;
			TreeIter iter;

			if (viewAttrTreeview.Selection.GetSelected (out model, out iter))
			{
				string name = (string) viewStore.GetValue (iter, 0);

				viewStore.Remove (ref iter);
		
				if (_viewAttrs.ContainsKey (name))
					_viewAttrs.Remove (name);

				allStore.AppendValues (name);
			}
			else
			{
				Util.MessageBox (customViewDialog, 
						"No attribute selected to remove.",
						 MessageType.Error);
			}			
		}

		private void OnSaveClicked (object o, EventArgs args)
		{
			string cols = "";

			foreach (string name in _viewAttrs.Keys)
			{
				cols += String.Format ("{0},", 
						(string)_viewAttrs[name]);
			}

			cols = cols.Remove ((cols.Length - 1), 1);

			CustomViewData cvd = new CustomViewData (
				nameEntry.Text,
				filterEntry.Text,
				searchBaseButton.Label,
				cols);

			if (_isEdit)
			{
				if (!_oldName.Equals (cvd.Name))
				{
					_cvm.deleteView (_oldName);
					_cvm.addView (cvd);
				}
				else
				{
					_cvm.updateView (cvd);
				}
			}
			else
			{
				_cvm.addView (cvd);
			}

			_cvm.saveViews ();

			_name = cvd.Name;

			customViewDialog.HideAll ();
		}

		private void OnCancelClicked (object o, EventArgs args)
		{
			customViewDialog.HideAll ();
		}

		private void OnSearchBuilderClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			filterEntry.Text = sbd.UserFilter;
		}

		public string Name
		{
			get { return _name; }
		}	
	}
}
