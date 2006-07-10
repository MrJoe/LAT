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
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;
using Gtk;
using GLib;
using Gdk;

namespace lat
{
	public class dnSelectedEventArgs : EventArgs
	{
		string _dn;
		string _server;
		bool _isHost;

		public dnSelectedEventArgs (string dn, bool isHost, string server)
		{
			_dn = dn;
			_server = server;
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

		public string Server
		{
			get { return _server; }
		}		
	}

	public delegate void dnSelectedHandler (object o, dnSelectedEventArgs args);

	public class LdapTreeView : Gtk.TreeView
	{
		TreeStore browserStore;
		TreeIter rootIter;

		Gtk.Window parent;

		bool _handlersSet = false;
		Gtk.ToolButton _newButton = null;
		Gtk.ToolButton _deleteButton = null;

		int browserSelectionMethod = 0;

		enum TreeCols { Icon, DN, RDN };

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

		public LdapTreeView (Gtk.Window parentWindow) : base ()
		{
			parent = parentWindow;

			browserStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string), typeof (string));

			this.Model = browserStore;
			this.HeadersVisible = false;

			this.RowActivated += new RowActivatedHandler (OnRowActivated);
			this.RowCollapsed += new RowCollapsedHandler (ldapRowCollapsed);
			this.RowExpanded += new RowExpandedHandler (ldapRowExpanded);
			this.Selection.Changed += OnSelectionChanged;

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

			rootIter = browserStore.AppendValues (dirIcon, "Servers", "Servers");

			foreach (string n in Global.Profiles.GetProfileNames()) {
				TreeIter iter = browserStore.AppendValues (rootIter, dirIcon, n, n);
				browserStore.AppendValues (iter, null, "", "");				
			}

			this.ButtonPressEvent += new ButtonPressEventHandler (OnBrowserRightClick);
			this.ShowAll ();
		}

		void DispatchDNSelectedEvent (string dn, bool host, string serverName)
		{
			if (dnSelected != null)
				dnSelected (this, new dnSelectedEventArgs (dn, host, serverName));
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

		public void GetSelectedDN (out string dn, out LdapServer server)
		{
			TreeModel model;
			TreeIter iter;

			if (this.Selection.GetSelected (out model, out iter)) {
				string name = (string) browserStore.GetValue (iter, (int)TreeCols.DN);
				string serverName =  FindServerName (iter, model);
				
				ConnectionProfile cp = Global.Profiles [serverName];			
				LdapServer foundServer = Global.Connections [cp];
				
				dn = name;
				server = foundServer;
				
				return;
			}
		
			dn = null;
			server  =null;
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

		public void Refresh ()
		{
			browserStore.Clear ();
			
			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			rootIter = browserStore.AppendValues (dirIcon, "Servers");
			TreePath path = browserStore.GetPath (rootIter);
			
			foreach (string n in Global.Profiles.GetProfileNames()) {
				TreeIter iter = browserStore.AppendValues (rootIter, dirIcon, n, n);
				browserStore.AppendValues (iter, null, "", "");
			}			

			this.ExpandRow (path, false);		
		}

		public void AddServer (ConnectionProfile cp)
		{
			Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			TreeIter iter = browserStore.AppendValues (rootIter, dirIcon, cp.Name, cp.Name);
			browserStore.AppendValues (iter, null, "");
		}
		
		public string GetActiveServerName ()
		{
			TreeModel model;
			TreeIter iter;

			if (this.Selection.GetSelected (out model, out iter))
				return FindServerName (iter, model);
			
			return null;
		}
		
		void OnSelectionChanged (object o, EventArgs args)
		{
			if (this.BrowserSelectionMethod == 2)
				return;

			Gtk.TreeIter iter;
			Gtk.TreeModel model;
			
			if (this.Selection.GetSelected (out model, out iter))  {
					
				string dn = (string) model.GetValue (iter, (int)TreeCols.DN);				
				string serverName = FindServerName (iter, model);
				
				if (dn.Equals (serverName)) {
					DispatchDNSelectedEvent (dn, true, serverName);
					return;
				}

				DispatchDNSelectedEvent (dn, false, serverName);
			}
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{
			if (this.BrowserSelectionMethod == 1)
				return;
		
			TreePath path = args.Path;
			TreeIter iter;
			
			if (browserStore.GetIter (out iter, path)) {

				string name = null;
				name = (string) browserStore.GetValue (iter, (int)TreeCols.DN);

				string serverName = FindServerName (iter, browserStore);
				
				if (name.Equals (serverName)) {
					DispatchDNSelectedEvent (name, true, serverName);
					return;
				}

				DispatchDNSelectedEvent (name, false, serverName);
			} 		
		}

		void ldapRowCollapsed (object o, RowCollapsedArgs args)
		{
			string name = (string) browserStore.GetValue (args.Iter, (int)TreeCols.DN);
			string serverName = FindServerName (args.Iter, browserStore);

			if (name == serverName) 
				return;

			Log.Debug ("collapsed row: {0}", name);

			TreeIter child;
			browserStore.IterChildren (out child, args.Iter);

//			string fcName = (string) browserStore.GetValue (child, (int)TreeCols.DN);				

			TreeIter lastChild = child;

			while (browserStore.IterNext (ref child)) {

				browserStore.Remove (ref lastChild);

//				string cn = (string) browserStore.GetValue (child, (int)TreeCols.DN);
				
				lastChild = child;
			}

			browserStore.Remove (ref lastChild);

			Gdk.Pixbuf pb = parent.RenderIcon (Stock.Open, IconSize.Menu, "");
			browserStore.AppendValues (args.Iter, pb, "");
		}

		string FindServerName (TreeIter iter, TreeModel model)
		{
			TreeIter parent;
			browserStore.IterParent (out parent, iter);
			
			if (!browserStore.IterIsValid (parent))
				return null;
			
			string parentName = (string)model.GetValue (parent, (int)TreeCols.DN);			
			if (parentName == "Servers")
				return (string)model.GetValue (iter, (int)TreeCols.DN);
			
			return FindServerName (parent, model);
		}

		void ldapRowExpanded (object o, RowExpandedArgs args)
		{		
			string name = null;
			name = (string) browserStore.GetValue (args.Iter, (int)TreeCols.DN);			
			if (name == "Servers")
				return;

			TreeIter child;
			browserStore.IterChildren (out child, args.Iter);
			
			string childName = (string)browserStore.GetValue (child, (int)TreeCols.DN);
			if (childName != "")
				return;			
					
			browserStore.Remove (ref child);
					
			Log.Debug ("Row expanded {0}", name);

			string serverName = FindServerName (args.Iter, browserStore);
			ConnectionProfile cp = Global.Profiles [serverName];			
			LdapServer server = Global.Connections [cp];		

			if (name == serverName) {			
			
				Pixbuf pb = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
				TreeIter i = browserStore.AppendValues (args.Iter, pb, server.DirectoryRoot, server.DirectoryRoot);
				browserStore.AppendValues (i, null, "", "");
				
			} else {

				AddEntry (name, server, args.Iter);
				
			}
			
			TreePath path = browserStore.GetPath (args.Iter);
			this.ExpandRow (path, false);				
		}

		void AddEntry (string name, LdapServer server, TreeIter iter)
		{		
			try {

				Pixbuf pb = Pixbuf.LoadFromResource ("x-directory-normal.png");
		 		LdapEntry[] ldapEntries = server.GetEntryChildren (name);

				foreach (LdapEntry le in ldapEntries) {

					Log.Debug ("\tchild: {0}", le.DN);
					DN dn = new DN (le.DN);
					RDN rdn = (RDN) dn.RDNs[0];

					TreeIter newChild;

					newChild = browserStore.AppendValues (iter, pb, le.DN, rdn.Value);
					browserStore.AppendValues (newChild, pb, "", "");
				}

			} catch {

				string	msg = Mono.Unix.Catalog.GetString (
					"Unable to read data from server");

				HIGMessageDialog dialog = new HIGMessageDialog (
					parent,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Network error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			}
		
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
			
			TreeModel model;
			TreeIter iter;

			if (this.Selection.GetSelected (out model, out iter)) {
			
				string serverName = FindServerName (iter, model);
				if (serverName == null)
					return;
					
				ConnectionProfile cp = Global.Profiles [serverName];			
				LdapServer server = Global.Connections [cp];
				
				new NewEntryDialog (server, dn);			
			}
		}

		private void OnRenameActivate (object o, EventArgs args) 
		{
			string dn = getSelectedDN ();
			TreeIter iter = getSelectedIter ();

			string serverName = FindServerName (iter, browserStore);
			if (serverName == null)
				return;
					
			ConnectionProfile cp = Global.Profiles [serverName];			
			LdapServer server = Global.Connections [cp];

			if (dn == server.Host)
				return;

			RenameEntryDialog red = new RenameEntryDialog (server, dn);

			TreeModel model;
			TreeIter iter2, parentIter;

			if (red.RenameHappened) {
				if (this.Selection.GetSelected (out model, out iter2)) {
					browserStore.IterParent (out parentIter, iter2);
					TreePath tp = browserStore.GetPath (parentIter);
					this.CollapseRow (tp);								
					this.ExpandRow (tp, false);
				}
			}	
		}

		private void OnExportActivate (object o, EventArgs args) 
		{
			string dn = getSelectedDN ();
			
			if (dn.Equals (null))
				return;

			TreeIter iter = getSelectedIter ();
			string serverName = FindServerName (iter, browserStore);
			if (serverName == null)
				return;
					
			ConnectionProfile cp = Global.Profiles [serverName];			
			LdapServer server = Global.Connections [cp];

			Util.ExportData (server, this.parent, dn);
		}

		public void OnDeleteActivate (object o, EventArgs args) 
		{
			TreeModel model;
			TreeIter iter;

			if (!this.Selection.GetSelected (out model, out iter))
				return;

			string dn = (string) browserStore.GetValue (iter, (int)TreeCols.DN);
			string serverName = FindServerName (iter, model);
			if (serverName == null)
				return;
					
			ConnectionProfile cp = Global.Profiles [serverName];			
			LdapServer server = Global.Connections [cp];

			if (dn == server.Host)
				return;

			try {
				if (Util.DeleteEntry (server, parent, dn))
					browserStore.Remove (ref iter);
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
//			Log.Debug ("BEGIN OnDragDataGet");
//
//			Gtk.TreeModel model;
//			Gtk.TreeIter iter;
//
//			if (!this.Selection.GetSelected (out model, out iter))
//				return;
//
//			string dn = (string) model.GetValue (iter, (int)TreeCols.DN);
//			string data = null;
//
//			Log.Debug ("Exporting entry: {0}", dn);
//
//			Util.ExportData (server, dn, out data);
//
//			Atom[] targets = args.Context.Targets;
//
//			args.SelectionData.Set (targets[0], 8,
//				System.Text.Encoding.UTF8.GetBytes (data));
//
//			Log.Debug ("END OnDragDataGet");
		}

		public void OnDragDataReceived (object o, DragDataReceivedArgs args)
		{
//			Log.Debug ("BEGIN OnDragDataReceived");
//
//			bool success = false;
//
//			string data = System.Text.Encoding.UTF8.GetString (
//					args.SelectionData.Data);
//
//			switch (args.Info) {
//				
//			case 0:
//			{
//				string[] uri_list = Regex.Split (data, "\r\n");
//			
//				Util.ImportData (server, parent, uri_list);
//			
//				success = true;
//				break;
//			}
//
//			case 1:
//				Util.ImportData (server, parent, data);
//				success = true;
//				break;
//
//			}
//
//			Log.Debug ("import success: {0}", success.ToString());
//
//			Gtk.Drag.Finish (args.Context, success, false, args.Time);
//
//			Log.Debug ("END OnDragDataReceived");
		}
		
		public int BrowserSelectionMethod
		{
			get { return browserSelectionMethod; }
			set { browserSelectionMethod = value; }
		}
	}
}
