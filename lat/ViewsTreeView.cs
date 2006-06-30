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
using GLib;
using System;
using System.Collections;
using System.IO;
using System.Xml;

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
			
			foreach (string n in Global.Profiles.GetProfileNames()) {
				TreeIter iter = viewsStore.AppendValues (viewRootIter, dirIcon, n);
				viewsStore.AppendValues (iter, null, "");				
			}			

			this.RowExpanded += new RowExpandedHandler (OnRowExpanded);
			this.RowActivated += new RowActivatedHandler (OnRowActivated);

			this.ExpandRow (path, false);
			this.ShowAll ();		
		}

		public void AddServer (ConnectionProfile cp)
		{
			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			TreeIter iter = viewsStore.AppendValues (viewRootIter, dirIcon, cp.Name);
			AddViews (cp.Name, iter);
		}

		public void Refresh ()
		{
			viewsStore.Clear ();
			
			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			viewRootIter = viewsStore.AppendValues (dirIcon, "Servers");
			TreePath path = viewsStore.GetPath (viewRootIter);
			
			foreach (string n in Global.Profiles.GetProfileNames()) {
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
					
			Logger.Log.Debug ("view expanded {0}", name);
			
			AddViews (name, args.Iter);
			
			TreePath path = viewsStore.GetPath (args.Iter);
			this.ExpandRow (path, false);
		}
	
		void AddViews (string profileName, TreeIter profileIter)
		{
			ConnectionProfile cp = Global.Profiles [profileName];

			if (cp.ActiveServerViews == null) 
				cp.SetDefaultServerViews ();
				
			foreach (ViewPlugin vp in Global.Plugins.ServerViewPlugins)
				if (cp.ActiveServerViews.Contains (vp.GetType().ToString()))
					viewsStore.AppendValues (profileIter, vp.Icon, vp.Name);
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

				string name = null;
				name = (string) viewsStore.GetValue (iter, (int)TreeCols.Name);

				TreeIter parent;
				viewsStore.IterParent (out parent, iter);
				
				string connection = (string) viewsStore.GetValue (parent, (int)TreeCols.Name);

				DispatchViewSelectedEvent (name, connection);
			} 		
		}
	}
}
