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

		private LdapServer server;
		private ListStore allStore;
		private ListStore viewStore;
	
		private Hashtable _viewAttrs = new Hashtable ();

		private bool _isEdit = false;
		private string _oldName;
		private string _name = null;

		private ResponseType response;

		public CustomViewDialog (LdapServer ldapServer)
		{
			server = ldapServer;

			Init ();

			customViewDialog.Title = "LAT - New custom view";

			searchBaseButton.Label = server.DirectoryRoot;
		}

		public CustomViewDialog (LdapServer ldapServer, string name)
		{
			server = ldapServer;

			_isEdit = true;
			_oldName = name;

			Init ();
			
			customViewDialog.Title = name + " Properties";

			ViewData vd = Global.viewManager.Lookup (name);

			nameEntry.Text = vd.Name;
			filterEntry.Text = vd.Filter;

			if (vd.Base == "")
				searchBaseButton.Label = ldapServer.DirectoryRoot;
			else
				searchBaseButton.Label = vd.Base;

			foreach (string c in vd.Cols)
			{
				_viewAttrs.Add (c, c);
				viewStore.AppendValues (c);
			}

			checkFilter ();
		}

		public void Run ()
		{
			response = (ResponseType) customViewDialog.Run ();
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
				foreach (LdapEntry le in res)
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
			if (_viewAttrs.Keys.Count == 0)
			{
				Util.MessageBox (customViewDialog, 
				  "You must select what attributes will be displayed in the view",
				   MessageType.Error);
			}

			ArrayList tmp = new ArrayList ();

			foreach (string name in _viewAttrs.Keys)
			{
				tmp.Add (name);
			}

			ViewData vd = new ViewData ();
			vd.Name = nameEntry.Text;
			vd.DisplayName = nameEntry.Text;
			vd.PrimaryKey = -1;
			vd.Type = "custom";
			vd.Filter = filterEntry.Text;
			vd.Base = searchBaseButton.Label;
			vd.Cols = (string[])tmp.ToArray (typeof(string));
			vd.ColNames = (string[])tmp.ToArray (typeof(string));

			if (_isEdit)
			{
				if (!_oldName.Equals (vd.Name))
				{
					Global.viewManager.DeleteView (_oldName);
					Global.viewManager.AddView (vd);
				}
				else
				{
					Global.viewManager.UpdateView (vd);
				}
			}
			else
			{
				Global.viewManager.AddView (vd);
			}

			Global.viewManager.SaveViews ();

			_name = vd.Name;

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

		public ResponseType UserResponse
		{
			get { return response; }
		}
	}
}
