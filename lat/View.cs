// 
// lat - View.cs
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
using Gdk;
using GLib;
using System;
using System.Collections;
using System.Text.RegularExpressions;
using Novell.Directory.Ldap;

namespace lat
{
	public abstract class View
	{
		protected ListStore _store;
		protected TreeView _tv;
		protected Gtk.Window _parent;
		protected lat.Connection _conn;
		
		protected string _viewName = null;
		protected string _filter = null;

		protected int _lookupKeyCol;
		protected Hashtable _lookupTable;

		private static TargetEntry[] _sourceTable = new TargetEntry[]
		{
			new TargetEntry ("text/plain", 0, 1),
		};

		private static TargetEntry[] _targetsTable = new TargetEntry[]
		{
			new TargetEntry ("text/uri-list", 0, 0),
			new TargetEntry ("text/plain", 0, 1),
		};

		public View (lat.Connection conn, TreeView tv, Gtk.Window parent)
		{
			_conn = conn;
			_parent = parent;

			_tv = tv;
			_tv.Selection.Mode = SelectionMode.Multiple;
			_tv.ButtonPressEvent += new ButtonPressEventHandler (OnEntryRightClick);

			_lookupTable = new Hashtable ();

			Gtk.Drag.DestSet (_tv, DestDefaults.All, _targetsTable,
					Gdk.DragAction.Copy);

			Gtk.Drag.SourceSet (_tv, 
				Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask, 
				_sourceTable, Gdk.DragAction.Copy | DragAction.Move);

			_tv.DragBegin += new DragBeginHandler (OnDragBegin);
			_tv.DragDataGet += new DragDataGetHandler (OnDragDataGet);
			_tv.DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);

			_tv.RowActivated += new RowActivatedHandler (OnRowActivated);
		}

		public virtual void DoPopUp()
		{
/*
			Menu popup = new Menu();

			MenuItem mailItem = new MenuItem ("Send email");
			mailItem.Activated += new EventHandler (OnEmailActivate);
			mailItem.Show ();

			popup.Append (mailItem);

			MenuItem exportItem = new MenuItem ("Export");
			exportItem.Activated += new EventHandler (OnExportActivate);
			exportItem.Show ();

			popup.Append (exportItem);

			ImageMenuItem deleteItem = new ImageMenuItem (Stock.Delete, new Gtk.AccelGroup(IntPtr.Zero));
			deleteItem.Activated += new EventHandler (OnDeleteActivate);
			deleteItem.Show ();

			popup.Append (deleteItem);

			popup.Popup(null, null, null, IntPtr.Zero, 3,
					Gtk.Global.CurrentEventTime);
*/
		}

		[ConnectBefore]
		public void OnEntryRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
			{
				DoPopUp ();
			}
		}

		public void setupColumns (string[] cols)
		{
			CellRenderer crt = new CellRendererText ();

			for (int i = 0; i < cols.Length; i++)
			{
				TreeViewColumn col = new TreeViewColumn ();
				col.Title = cols[i];
				col.PackStart (crt, true);
				col.AddAttribute (crt, "text", i);
				col.SortColumnId = i;

				_tv.AppendColumn (col);
			}

			_tv.ShowAll ();
		}

		private static string[] getAttrValues (LdapEntry le, string[] attr)
		{
			if (le == null || attr == null)
				return null;

			ArrayList retVal = new ArrayList ();

			foreach (string n in attr)
			{
				LdapAttribute la = le.getAttribute (n);

				if (la != null)
					retVal.Add (la.StringValue);
				else
					retVal.Add ("");
			}

			return (string[]) retVal.ToArray (typeof (string));
		}

		private void doInsert (ArrayList objs, string[] attributes)
		{
			if (_store != null)
				_store.Clear ();

			foreach (LdapEntry le in objs)
			{
				string[] values = getAttrValues (le, attributes);
				_store.AppendValues (values);

				if (_lookupTable.ContainsKey (values [_lookupKeyCol]))
					_lookupTable.Remove (values [_lookupKeyCol]);

				_lookupTable.Add (values [_lookupKeyCol], le);
			}
		}

		public void insertData (string[] attributes)
		{
			ArrayList objs = _conn.SearchByClass (_filter);
			doInsert (objs, attributes);
		}

		public void insertData (string searchBase, string[] attributes)
		{
			ArrayList objs = _conn.Search (searchBase, _filter);
			doInsert (objs, attributes);
		}

		public LdapEntry lookupEntry (TreePath path)
		{
			TreeIter iter;
			
			if (_store.GetIter (out iter, path))
			{
				string key = null;
				key = (string) _store.GetValue (iter, _lookupKeyCol);
				
				LdapEntry le = (LdapEntry) _lookupTable [key];

				return le;
			} 

			return null;
		}

		public virtual void OnRowActivated (object o, RowActivatedArgs args)
		{	
			LdapEntry le = lookupEntry (args.Path);

			ViewDialogFactory.Create (_viewName, _conn, le);

			Populate ();
		}

		public virtual void OnDragBegin (object o, DragBeginArgs args)
		{
			// FIXME: change icon
			// FIXME: Drag.SetIconPixbuf (args.Context, <obj>, 0, 0);
		}

		public virtual void OnDragDataGet (object o, DragDataGetArgs args)
		{
			Gtk.TreeModel model;

			TreePath[] tp = _tv.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp)
			{
				LdapEntry le = lookupEntry (path);

				LDIF _ldif = new LDIF (le);

				Atom[] targets = args.Context.Targets;

				args.SelectionData.Set (targets[0], 8,
					System.Text.Encoding.UTF8.GetBytes (_ldif.Export()));
			}
		}

		public void removeDndHandlers ()
		{
			_tv.DragBegin -= new DragBeginHandler (OnDragBegin);
			_tv.DragDataGet -= new DragDataGetHandler (OnDragDataGet);
			_tv.DragDataReceived -= new DragDataReceivedHandler (OnDragDataReceived);
		}
		
		public void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			bool success = false;

			string data = System.Text.Encoding.UTF8.GetString (
					args.SelectionData.Data);

			switch (args.Info)
			{
				case 0:
				{
					string[] uri_list = Regex.Split (data, "\r\n");

					Util.ImportData (_conn, _parent, uri_list);
					
					success = true;
					break;
				}

				case 1:
				{
					Util.ImportData (_conn, _parent, data);

					success = true;
					break;
				}
			}

			Gtk.Drag.Finish (args.Context, success, false, args.Time);
		}

		public virtual void OnNewEntryActivate (object o, EventArgs args) 
		{
			ViewDialogFactory.Create (_viewName, _conn, null);

			Populate ();
		}

		public virtual void OnEditActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = _tv.Selection.GetSelectedRows (out model);

			foreach (TreePath path in tp)
			{
				LdapEntry le = lookupEntry (path);

				ViewDialogFactory.Create (_viewName, _conn, le);
				
				Populate ();
			}
		}

		private void deleteEntry (TreePath[] path)
		{
			if (!(path.Length > 1))
			{
				LdapEntry le = lookupEntry (path[0]);

				Util.DeleteEntry (_conn, _parent, le.DN);

				return;
			}

			ArrayList dnList = new ArrayList ();

			foreach (TreePath tp in path)
			{
				LdapEntry le = lookupEntry (tp);
				dnList.Add (le.DN);
			}

			string[] dns = (string[]) dnList.ToArray (typeof(string));

			Util.DeleteEntry (_conn, _parent, dns);
		}

		public virtual void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreePath[] tp = _tv.Selection.GetSelectedRows (out model);

			deleteEntry (tp);
			
			Populate ();
		}

		private string getSelectedAttribute (string attrName)
		{
			Gtk.TreeModel model;

			TreePath[] tp = this._tv.Selection.GetSelectedRows (out model);

			LdapEntry le = this.lookupEntry (tp[0]);

			if (le == null)
				return null;

			LdapAttribute la = le.getAttribute (attrName);

			return la.StringValue;
		}

		public virtual void OnEmailActivate (object o, EventArgs args) 
		{
			string url = getSelectedAttribute ("mail");

			if (url == null || url == "")
				return;

			Gnome.Url.Show ("mailto:" + url);
		}

		public virtual void OnWWWActivate (object o, EventArgs args) 
		{
			string url = getSelectedAttribute ("wWWHomePage");

			if (url == null || url == "")
				return;

			Gnome.Url.Show (url);
		}


		public virtual void OnRefreshActivate (object o, EventArgs args)
		{
			Populate ();
		}

		public void removeHandlers ()
		{
			_tv.RowActivated -= new RowActivatedHandler (OnRowActivated);
			_tv.ButtonPressEvent -= new ButtonPressEventHandler (OnEntryRightClick);
		}

		public abstract void Populate ();
	}
}
