// 
// lat - LdapTreeView.cs
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
using Glade;
using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Novell.Directory.Ldap;

namespace lat
{
	public class dnSelectedEventArgs : EventArgs
	{
		private string _dn;
		private bool _isHost;

		public dnSelectedEventArgs (string dn, bool isHost)
		{
			_dn = dn;
			_isHost = isHost;
		}

		public string DN
		{
			get { return _dn; }
		}

		public bool IsHost
		{
			get { return _isHost; }
		}
	}

	public delegate void dnSelectedHandler (object o, dnSelectedEventArgs args);

	public class LdapTreeView : Gtk.TreeView
	{
		private TreeStore browserStore;
		private TreeIter ldapRootIter;

		private lat.Connection _conn;
		private Gtk.Window _parent;

		private bool _handlersSet = false;
		private Gtk.ToolButton _newButton = null;
		private Gtk.ToolButton _deleteButton = null;

		private enum TreeCols { Icon, DN };

		public event dnSelectedHandler dnSelected;

		private static TargetEntry[] _sourceTable = new TargetEntry[]
		{
			new TargetEntry ("text/plain", 0, 1),
		};

		private static TargetEntry[] _targetsTable = new TargetEntry[]
		{
			new TargetEntry ("text/uri-list", 0, 0),
			new TargetEntry ("text/plain", 0, 1),
		};

		public LdapTreeView (lat.Connection conn, Gtk.Window parent) : base ()
		{
			_conn = conn;
			_parent = parent;

			browserStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));

			this.Model = browserStore;
			this.HeadersVisible = false;

			this.RowActivated += new RowActivatedHandler (ldapRowActivated);
			this.RowCollapsed += new RowCollapsedHandler (ldapRowCollapsed);
			this.RowExpanded += new RowExpandedHandler (ldapRowExpanded);

			Gtk.Drag.DestSet (this, DestDefaults.All, _targetsTable,
					Gdk.DragAction.Copy);
		
			Gtk.Drag.SourceSet (this, 
				Gdk.ModifierType.Button1Mask | Gdk.ModifierType.Button3Mask, 
				_sourceTable, Gdk.DragAction.Copy | DragAction.Move);

			this.DragBegin += new DragBeginHandler (OnDragBegin);
			this.DragDataGet += new DragDataGetHandler (OnDragDataGet);
			this.DragDataReceived += new DragDataReceivedHandler (OnDragDataReceived);

			this.AppendColumn ("icon", new CellRendererPixbuf (), "pixbuf", (int)TreeCols.Icon);
			this.AppendColumn ("ldapRoot", new CellRendererText (), "text", (int)TreeCols.DN);

			Gdk.Pixbuf pb = _parent.RenderIcon (Stock.Convert, IconSize.Menu, "");

			TreeIter iter;
			iter = browserStore.AppendValues (pb, _conn.Host);

			ldapRootIter = browserStore.AppendValues (iter, pb, _conn.LdapRoot);
			browserStore.AppendValues (ldapRootIter, null, "");

			this.ButtonPressEvent += new ButtonPressEventHandler (OnBrowserRightClick);

			this.ShowAll ();
		}

		private void DispatchDNSelectedEvent (string dn, bool host)
		{
			if (dnSelected != null)
			{
				dnSelected (this, new dnSelectedEventArgs (dn, host));
			}
		}

		public string getSelectedDN ()
		{
			TreeModel ldapModel;
			TreeIter ldapIter;
			string dn;

			if (this.Selection.GetSelected (out ldapModel, out ldapIter))
			{
				dn = (string) browserStore.GetValue (ldapIter, (int)TreeCols.DN);
				return dn;
			}

			return null;
		}

		public TreeIter getSelectedIter ()
		{
			TreeModel ldapModel;
			TreeIter ldapIter;

			if (this.Selection.GetSelected (out ldapModel, out ldapIter))
			{
				return ldapIter;
			}

			return ldapIter;
		}

		public void RemoveRow (TreeIter iter)
		{
			browserStore.Remove (ref iter);
		}

		private void ldapRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (browserStore.GetIter (out iter, path))
			{
				string name = null;
				name = (string) browserStore.GetValue (iter, (int)TreeCols.DN);

				if (name.Equals (_conn.Host))
				{
					DispatchDNSelectedEvent (_conn.Host, true);
					return;
				}

				DispatchDNSelectedEvent (name, false);
			} 		
		}

		private void ldapRowCollapsed (object o, RowCollapsedArgs args)
		{
			Logger.Log.Debug ("BEGIN ldapRowCollapsed");

			string name = (string) browserStore.GetValue (
					args.Iter, (int)TreeCols.DN);

			if (name == _conn.Host)
			{
				Logger.Log.Debug ("END ldapRowCollapsed");
				return;
			}

			Logger.Log.Debug ("collapsed row: {0}", name);

			TreeIter child;

			browserStore.IterChildren (out child, args.Iter);

			string fcName = (string) browserStore.GetValue (
					child, (int)TreeCols.DN);
				
			Logger.Log.Debug ("\tchild: {0}", fcName);


			TreeIter lastChild = child;

			while (browserStore.IterNext (ref child))
			{
				browserStore.Remove (ref lastChild);

				string cn = (string) browserStore.GetValue (
					child, (int)TreeCols.DN);
				
				Logger.Log.Debug ("\tchild: {0}", cn);

				lastChild = child;
			}

			browserStore.Remove (ref lastChild);

			Gdk.Pixbuf pb = _parent.RenderIcon (Stock.Open, IconSize.Menu, "");
			browserStore.AppendValues (args.Iter, pb, "");

			Logger.Log.Debug ("END ldapRowCollapsed");
		}

		private void ldapRowExpanded (object o, RowExpandedArgs args)
		{
			Logger.Log.Debug ("BEGIN ldapRowExpanded");

			string name = null;
			bool firstPass = false;

			Gdk.Pixbuf pb = _parent.RenderIcon (Stock.Open, IconSize.Menu, "");

			name = (string) browserStore.GetValue (args.Iter, (int)TreeCols.DN);

			if (name == _conn.Host)
			{
				Logger.Log.Debug ("END ldapRowExpanded");
				return;
			}

			TreeIter parent, child;
			browserStore.IterParent (out parent, args.Iter);
			browserStore.IterChildren (out child, args.Iter);

			string childName = (string)browserStore.GetValue (child, (int)TreeCols.DN);
			
			if (childName == "")
				firstPass = true;

	 		ArrayList ldapEntries = _conn.getChildren (name);

			if (ldapEntries.Count == 0)
			{
				browserStore.Remove (ref child);
			}

			Logger.Log.Debug ("expanded row: {0}", name);

			foreach (LdapEntry le in ldapEntries)
			{
				Logger.Log.Debug ("\tchild: {0}", le.DN);

				TreeIter _newChild;

				if (firstPass)
				{
					browserStore.SetValue (child, (int)TreeCols.Icon, pb);
					browserStore.SetValue (child, (int)TreeCols.DN, le.DN);

					browserStore.AppendValues (child, pb, "");
				
					firstPass = false;
				}
				else
				{
					_newChild = browserStore.AppendValues (args.Iter, pb, le.DN);
					browserStore.AppendValues (_newChild, pb, "");
				}
			}

			Logger.Log.Debug ("END ldapRowExpanded");
		}

		public void removeToolbarHandlers ()
		{
			if (_handlersSet)
			{
				_newButton.Clicked -= new EventHandler (OnNewEntryActivate);
				_deleteButton.Clicked -= new EventHandler (OnDeleteActivate);

				_handlersSet = false;
			}
		}

		public void setToolbarHandlers (Gtk.ToolButton newButton, Gtk.ToolButton deleteButton)
		{
			_newButton = newButton;
			_deleteButton = deleteButton;
			
			_newButton.Clicked += new EventHandler (OnNewEntryActivate);
			_deleteButton.Clicked += new EventHandler (OnDeleteActivate);

			_handlersSet = true;
		}

		public void OnNewEntryActivate (object o, EventArgs args) 
		{
			new AddEntryDialog (_conn);		
		}

		private void OnRenameActivate (object o, EventArgs args) 
		{
			string dn = getSelectedDN ();

			new RenameEntryDialog (_conn, dn);		
		}

		private void OnExportActivate (object o, EventArgs args) 
		{
			string dn = getSelectedDN ();

			if (dn.Equals (null))
				return;

			Util.ExportData (_conn, this._parent, dn);
		}

		public void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return;

			string dn = (string) browserStore.GetValue (iter, (int)TreeCols.DN);

			try
			{
				browserStore.Remove (ref iter);
				Util.DeleteEntry (_conn, _parent, dn);
			}
			catch {}
		}

		private void DoPopUp()
		{
			Menu popup = new Menu();

			ImageMenuItem newItem = new ImageMenuItem (Stock.New, new Gtk.AccelGroup(IntPtr.Zero));
			newItem.Activated += new EventHandler (OnNewEntryActivate);
			newItem.Show ();

			popup.Append (newItem);

			MenuItem renameItem = new MenuItem ("Rename");
			renameItem.Activated += new EventHandler (OnRenameActivate);
			renameItem.Show ();

			popup.Append (renameItem);

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
		}

		[ConnectBefore]
		private void OnBrowserRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
			{
				DoPopUp ();
			}
		}

		public void OnDragBegin (object o, DragBeginArgs args)
		{
			// FIXME: change icon
			// FIXME: Drag.SetIconPixbuf (args.Context, <obj>, 0, 0);
		}

		public void OnDragDataGet (object o, DragDataGetArgs args)
		{
			Logger.Log.Debug ("BEGIN OnDragDataGet");

			Gtk.TreeModel model;
			Gtk.TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return;

			string dn = (string) model.GetValue (iter, (int)TreeCols.DN);
			string data = null;

			Logger.Log.Debug ("Exporting entry: {0}", dn);

			Util.ExportData (_conn, dn, out data);

			Atom[] targets = args.Context.Targets;

			args.SelectionData.Set (targets[0], 8,
				System.Text.Encoding.UTF8.GetBytes (data));

			Logger.Log.Debug ("END OnDragDataGet");
		}

		public void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
			Logger.Log.Debug ("BEGIN OnDragDataReceived");

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

			Logger.Log.Debug ("import success: {0}", success.ToString());

			Gtk.Drag.Finish (args.Context, success, false, args.Time);

			Logger.Log.Debug ("END OnDragDataReceived");
		}
	}
}
