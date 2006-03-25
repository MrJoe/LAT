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

using System;
using System.Collections;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;
using Gtk;
using GLib;
using Gdk;

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

	public class AddAttributeEventArgs : EventArgs
	{
		private string _name;
		private string _value;

		public AddAttributeEventArgs (string attrName, string attrValue)
		{
			_name = attrName;
			_value = attrValue;
		}

		public string Name
		{
			get { return _name; }
		}

		public string Value
		{
			get { return _value; }
		}
	}

	public delegate void AttributeAddedHandler (object o, AddAttributeEventArgs args);

	public delegate void dnSelectedHandler (object o, dnSelectedEventArgs args);

	public class LdapTreeView : Gtk.TreeView
	{
		private TreeStore browserStore;
		private TreeIter ldapRootIter;

		private LdapServer server;
		private Gtk.Window _parent;

		private bool _handlersSet = false;
		private Gtk.ToolButton _newButton = null;
		private Gtk.ToolButton _deleteButton = null;

		private enum TreeCols { Icon, DN, RDN };

		public event AttributeAddedHandler AttributeAdded;
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

		public LdapTreeView (LdapServer ldapServer, Gtk.Window parent) : base ()
		{
			server = ldapServer;
			_parent = parent;

			browserStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string));

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

			TreeViewColumn col;

			this.AppendColumn ("icon", new CellRendererPixbuf (), "pixbuf", (int)TreeCols.Icon);

			col = this.AppendColumn ("DN", new CellRendererText (), "text", (int)TreeCols.DN);
			col.Visible = false;

			this.AppendColumn ("RDN", new CellRendererText (), "text", (int)TreeCols.RDN);

			Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");

			TreeIter iter;
			iter = browserStore.AppendValues (dirIcon, server.Host, server.Host);

			ldapRootIter = browserStore.AppendValues (iter, dirIcon,
				server.DirectoryRoot, server.DirectoryRoot);

			browserStore.AppendValues (ldapRootIter, null, "", "");

			this.ButtonPressEvent += new ButtonPressEventHandler (OnBrowserRightClick);

			this.ShowAll ();
		}

		private void DispatchDNSelectedEvent (string dn, bool host)
		{
			if (dnSelected != null)
				dnSelected (this, new dnSelectedEventArgs (dn, host));
		}

		private void DispatchAddAttributeEvent (string attrName, string attrValue)
		{
			if (AttributeAdded != null && attrName != null)
				AttributeAdded (this, new AddAttributeEventArgs (attrName, attrValue));
		}

		public string getSelectedDN ()
		{
			TreeModel ldapModel;
			TreeIter ldapIter;
			string dn;

			if (this.Selection.GetSelected (out ldapModel, out ldapIter)) {
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
				return ldapIter;

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
			
			if (browserStore.GetIter (out iter, path)) {

				string name = null;
				name = (string) browserStore.GetValue (iter, (int)TreeCols.DN);

				if (name.Equals (server.Host)) {
					DispatchDNSelectedEvent (server.Host, true);
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

			if (name == server.Host) {
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

			while (browserStore.IterNext (ref child)) {

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

			Pixbuf pb = Pixbuf.LoadFromResource ("x-directory-normal.png");

			name = (string) browserStore.GetValue (args.Iter, (int)TreeCols.DN);

			if (name == server.Host) {
				Logger.Log.Debug ("END ldapRowExpanded");
				return;
			}

			TreeIter parent, child;
			browserStore.IterParent (out parent, args.Iter);
			browserStore.IterChildren (out child, args.Iter);

			string childName = (string)browserStore.GetValue (child, (int)TreeCols.DN);
			
			if (childName == "")
				firstPass = true;

			try {

		 		LdapEntry[] ldapEntries = server.GetEntryChildren (name);

				if (ldapEntries.Length == 0)
					browserStore.Remove (ref child);

				Logger.Log.Debug ("expanded row: {0}", name);

				foreach (LdapEntry le in ldapEntries) {

					Logger.Log.Debug ("\tchild: {0}", le.DN);
					DN dn = new DN (le.DN);
					RDN rdn = (RDN) dn.RDNs[0];

					TreeIter _newChild;

					if (firstPass) {

						browserStore.SetValue (child, (int)TreeCols.Icon, pb);
						browserStore.SetValue (child, (int)TreeCols.DN, le.DN);
						browserStore.SetValue (child, (int)TreeCols.RDN, rdn.Value);

						browserStore.AppendValues (child, pb, "");
					
						firstPass = false;

					} else {

						_newChild = browserStore.AppendValues (args.Iter, pb, le.DN, rdn.Value);
						browserStore.AppendValues (_newChild, pb, "", "");
					}
				}

			} catch {

				string	msg = Mono.Unix.Catalog.GetString (
					"Unable to read data from server");

				HIGMessageDialog dialog = new HIGMessageDialog (
					_parent,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Network error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			}

			Logger.Log.Debug ("END ldapRowExpanded");
		}

		public void removeToolbarHandlers ()
		{
			if (_handlersSet) {
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
			string dn = getSelectedDN ();

			new NewEntryDialog (server, dn);
		}

		private void OnRenameActivate (object o, EventArgs args) 
		{
			string dn = getSelectedDN ();

			if (dn == server.Host)
				return;

			new RenameEntryDialog (server, dn);		
		}

		private void OnExportActivate (object o, EventArgs args) 
		{
			string dn = getSelectedDN ();

			if (dn.Equals (null))
				return;

			Util.ExportData (server, this._parent, dn);
		}

		public void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return;

			string dn = (string) browserStore.GetValue (iter, (int)TreeCols.DN);

			if (dn == server.Host)
				return;

			try {
				if (Util.DeleteEntry (server, _parent, dn))
					browserStore.Remove (ref iter);
			}
			catch {}
		}

		public void OnAddObjActivate (object o, EventArgs args)
		{
			string dn = getSelectedDN ();

			if (dn == server.Host)
				return;

			DispatchDNSelectedEvent (dn, false);

			AddObjectClassDialog dlg = new AddObjectClassDialog (server);

			foreach (string s in dlg.ObjectClasses)	
				DispatchAddAttributeEvent ("objectClass", s);
		}

		public void OnAddAttrActivate (object o, EventArgs args)
		{
			string dn = getSelectedDN ();

			if (dn == server.Host)
				return;

			DispatchDNSelectedEvent (dn, false);

			AddAttributeDialog aad = new AddAttributeDialog (server, dn);
	
			Logger.Log.Debug ("LdapTreeView.OnAddAttr: name: {0} - value: {1}", 
				aad.Name, aad.Value);

			DispatchAddAttributeEvent (aad.Name, aad.Value);
		}

		private void DoPopUp()
		{
			Menu popup = new Menu();

			ImageMenuItem newItem = new ImageMenuItem (Stock.New, new Gtk.AccelGroup(IntPtr.Zero));
			newItem.Activated += new EventHandler (OnNewEntryActivate);
			newItem.Show ();
			popup.Append (newItem);

			MenuItem addAttrItem = new MenuItem ("Add Attribute...");
			addAttrItem.Activated += new EventHandler (OnAddAttrActivate);
			addAttrItem.Show ();

			popup.Append (addAttrItem);

			MenuItem addObjItem = new MenuItem ("Add Object Class...");
			addObjItem.Activated += new EventHandler (OnAddObjActivate);
			addObjItem.Show ();

			popup.Append (addObjItem);

			MenuItem renameItem = new MenuItem ("Rename...");
			renameItem.Activated += new EventHandler (OnRenameActivate);
			renameItem.Show ();

			popup.Append (renameItem);

			MenuItem exportItem = new MenuItem ("Export...");
			exportItem.Activated += new EventHandler (OnExportActivate);
			exportItem.Show ();

			popup.Append (exportItem);

			ImageMenuItem deleteItem = new ImageMenuItem (Stock.Delete, new Gtk.AccelGroup(IntPtr.Zero));
			deleteItem.Activated += new EventHandler (OnDeleteActivate);
			deleteItem.Show ();

			popup.Append (deleteItem);

			popup.Popup(null, null, null, 3,
					Gtk.Global.CurrentEventTime);
		}

		[ConnectBefore]
		private void OnBrowserRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				DoPopUp ();
		}

		public void OnDragBegin (object o, DragBeginArgs args)
		{
			Gdk.Pixbuf pb = Pixbuf.LoadFromResource ("text-x-generic.png");
			Gtk.Drag.SetIconPixbuf (args.Context, pb, 0, 0);
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

			Util.ExportData (server, dn, out data);

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

			switch (args.Info) {
				
			case 0:
			{
				string[] uri_list = Regex.Split (data, "\r\n");
			
				Util.ImportData (server, _parent, uri_list);
			
				success = true;
				break;
			}

			case 1:
				Util.ImportData (server, _parent, data);
				success = true;
				break;

			}

			Logger.Log.Debug ("import success: {0}", success.ToString());

			Gtk.Drag.Finish (args.Context, success, false, args.Time);

			Logger.Log.Debug ("END OnDragDataReceived");
		}
	}
}
