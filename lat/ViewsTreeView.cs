// 
// lat - ViewsTreeView.cs
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
using System;

namespace lat
{
	public class ViewSelectedEventArgs : EventArgs
	{
		string viewName;
		string connectionName;

		public ViewSelectedEventArgs (string name, string connection)
		{
			viewName = name;
			connectionName = connection;
		}

		public string Name
		{
			get { return viewName; }
		}
		
		public string ConnectionName
		{
			get { return connectionName; }
		}		
	}

	public delegate void ViewSelectedHandler (object o, ViewSelectedEventArgs args);

	public class ViewsTreeView : Gtk.TreeView
	{
		TreeStore	viewsStore;
		TreeIter	viewRootIter;

		enum TreeCols { Icon, Name };

		public event ViewSelectedHandler ViewSelected;

		public ViewsTreeView () : base ()
		{
			viewsStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));
			this.Model = viewsStore;
			this.HeadersVisible = false;

			this.AppendColumn ("viewsIcon", new CellRendererPixbuf (), "pixbuf", (int)TreeCols.Icon);
			this.AppendColumn ("viewsRoot", new CellRendererText (), "text", (int)TreeCols.Name);

			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			
			viewRootIter = viewsStore.AppendValues (dirIcon, "Servers");
			TreePath path = viewsStore.GetPath (viewRootIter);
			
			foreach (string n in Global.Connections.ConnectionNames) {
				TreeIter iter = viewsStore.AppendValues (viewRootIter, dirIcon, n);
				viewsStore.AppendValues (iter, null, "");				
			}			

			this.RowExpanded += new RowExpandedHandler (OnRowExpanded);
			this.RowActivated += new RowActivatedHandler (OnRowActivated);

			this.ExpandRow (path, false);
			this.ShowAll ();		
		}

		public void AddConnection (Connection conn)
		{
			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			TreeIter iter = viewsStore.AppendValues (viewRootIter, dirIcon, conn.Settings.Name);
			AddViews (conn, iter);
		}

		public void Refresh ()
		{
			viewsStore.Clear ();
			
			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			viewRootIter = viewsStore.AppendValues (dirIcon, "Servers");
			TreePath path = viewsStore.GetPath (viewRootIter);
			
			foreach (string n in Global.Connections.ConnectionNames) {
				TreeIter iter = viewsStore.AppendValues (viewRootIter, dirIcon, n);
				viewsStore.AppendValues (iter, null, "");
			}			

			this.ExpandRow (path, false);		
		}

		void OnRowExpanded (object o, RowExpandedArgs args)
		{
			string name = (string) viewsStore.GetValue (args.Iter, 1);
			if (name == "Servers")
				return;

			TreeIter child;
			viewsStore.IterChildren (out child, args.Iter);
			
			string childName = (string)viewsStore.GetValue (child, 1);
			if (childName != "")
				return;

			viewsStore.Remove (ref child);
					
			Log.Debug ("View expanded {0}", name);
			
			Connection conn = Global.Connections [name];
			AddViews (conn, args.Iter);
			
			TreePath path = viewsStore.GetPath (args.Iter);
			this.ExpandRow (path, false);
		}
	
		void AddViews (Connection conn, TreeIter profileIter)
		{
			if (conn == null) {
				Log.Error ("Unable to add views to ViewsTreeView connection is null");
				return;
			}

			if (conn.ServerViews.Count == 0)
				conn.SetDefaultServerViews ();
			
			foreach (ViewPlugin vp in Global.Plugins.ServerViewPlugins) {	
				if (conn.ServerViews.Contains (vp.GetType().ToString()))
					viewsStore.AppendValues (profileIter, vp.Icon, vp.Name);
			}			
		}

		public string GetSelectedViewName ()
		{
			TreeModel model;
			TreeIter iter;
			string name;

			if (this.Selection.GetSelected (out model, out iter)) {
				name = (string) viewsStore.GetValue (iter, (int)TreeCols.Name);
				return name;
			}

			return null;
		}

		public string GetActiveServerName ()
		{
			TreeModel model;
			TreeIter iter;

			if (this.Selection.GetSelected (out model, out iter))
				return FindServerName (iter, model);
			
			return null;
		}

		string FindServerName (TreeIter iter, TreeModel model)
		{
			TreeIter parent;
			viewsStore.IterParent (out parent, iter);
			
			if (!viewsStore.IterIsValid (parent))
				return null;
			
			string parentName = (string)model.GetValue (parent, (int)TreeCols.Name);			
			if (parentName == "Servers")
				return (string)model.GetValue (iter, (int)TreeCols.Name);
			
			return FindServerName (parent, model);
		}		

		void DispatchViewSelectedEvent (string name, string connection)
		{
			if (ViewSelected != null)
				ViewSelected (this, new ViewSelectedEventArgs (name, connection));
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (viewsStore.GetIter (out iter, path)) {

				string name = (string) viewsStore.GetValue (iter, (int)TreeCols.Name);
				if (name == "Servers")
					return;

				TreeIter parent;
				viewsStore.IterParent (out parent, iter);
				
				string connection = (string) viewsStore.GetValue (parent, (int)TreeCols.Name);
				if (connection == "Servers")
					connection = name;
				
				DispatchViewSelectedEvent (name, connection);
			} 		
		}
	}
}
