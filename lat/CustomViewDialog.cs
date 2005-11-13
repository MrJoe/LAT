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
		[Glade.Widget] Button searchBaseButton;
		[Glade.Widget] TreeView allAttrTreeview;
		[Glade.Widget] TreeView viewAttrTreeview;

		Glade.XML ui;

		private ListStore allStore;
		private ListStore viewStore;
	
		private Hashtable _viewAttrs = new Hashtable ();
		private LdapServer server;
		private CustomViewManager _cvm;

		private bool _isEdit = false;
		private string _oldName;

		private string _name = null;

		private ResponseType _result;

		public CustomViewDialog (LdapServer ldapServer, CustomViewManager cvm)
		{
			server = ldapServer;
			_cvm = cvm;

			Init ();

			customViewDialog.Title = "LAT - New Custom View";

			searchBaseButton.Label = server.DirectoryRoot;
		}

		public CustomViewDialog (LdapServer ldapServer, CustomViewManager cvm, string name)
		{
			server = ldapServer;
			_cvm = cvm;

			_isEdit = true;
			_oldName = name;

			Init ();
			
			customViewDialog.Title = name + " Properties";

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
			_result = (ResponseType) customViewDialog.Run ();
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

			customViewDialog.Resize (350, 400);
		}

		public void OnSearchBaseClicked (object o, EventArgs args)
		{
			SelectContainerDialog scd = 
				new SelectContainerDialog (server, customViewDialog);

			scd.Message = String.Format ("Where in the directory would\nyou like to start the search?");
			scd.Title = "Select search base";
			scd.Run ();

			if (!scd.DN.Equals ("") && !scd.DN.Equals (server.Host))
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
			LdapEntry[] res;

			res = server.Search (
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

		public void OnTestClicked (object o, EventArgs args)
		{
			checkFilter ();
		}

		public void OnAddClicked (object o, EventArgs args)
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

		public void OnRemoveClicked (object o, EventArgs args)
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

		public void OnSaveClicked (object o, EventArgs args)
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

		public void OnCancelClicked (object o, EventArgs args)
		{
			customViewDialog.HideAll ();
		}

		public void OnSearchBuilderClicked (object o, EventArgs args)
		{
			SearchBuilderDialog sbd = new SearchBuilderDialog ();
			filterEntry.Text = sbd.UserFilter;
		}

		public string Name
		{
			get { return _name; }
		}	

		public ResponseType Result
		{
			get { return _result; }
		}
	}
}
