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
using System.Collections;

namespace lat
{
	public class ViewSelectedEventArgs : EventArgs
	{
		private string _name;

		public ViewSelectedEventArgs (string name)
		{
			_name = name;
		}

		public string Name
		{
			get { return _name; }
		}
	}

	public delegate void ViewSelectedHandler (object o, ViewSelectedEventArgs args);

	public class ViewsTreeView : Gtk.TreeView
	{
		private TreeStore viewsStore;
		private TreeIter viewRootIter;
		private TreeIter viewCustomIter;

		private Gtk.Window _parent;
		private LdapServer server;

		private Hashtable customIters = new Hashtable ();

		private static ViewFactory viewFactory;
		private lat.View _currentView = null;

		private ListStore _vs;
		private TreeView _vt;

		private enum TreeCols { Icon, Name };

		public event ViewSelectedHandler ViewSelected;

		public ViewsTreeView (LdapServer ldapServer, Gtk.Window parent,
				      ListStore valueStore, TreeView valueTreeView) : base ()
		{
			server = ldapServer;
			_parent = parent;

			_vs = valueStore;
			_vt = valueTreeView;

			viewFactory = new ViewFactory (_vs, _vt, _parent, server);

			viewsStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));

			this.Model = viewsStore;
			this.HeadersVisible = false;

			this.AppendColumn ("viewsIcon", new CellRendererPixbuf (), "pixbuf", 
					(int)TreeCols.Icon);

			this.AppendColumn ("viewsRoot", new CellRendererText (), "text", 
					(int)TreeCols.Name);

			Pixbuf groupIcon = Pixbuf.LoadFromResource ("users.png");
			Pixbuf usersIcon = Pixbuf.LoadFromResource ("stock_person.png");
			Pixbuf compIcon = Pixbuf.LoadFromResource ("x-directory-remote-workgroup.png");
			Pixbuf contactIcon = Pixbuf.LoadFromResource ("contact-new.png");
			Pixbuf customIcon = Pixbuf.LoadFromResource ("x-directory-normal.png");
			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");

			viewRootIter = viewsStore.AppendValues (dirIcon, server.Host);

			if (server.ServerType.ToLower() == "microsoft active directory")
			{
				viewsStore.AppendValues (viewRootIter, compIcon, "Computers");
				viewsStore.AppendValues (viewRootIter, contactIcon, "Contacts");
				viewsStore.AppendValues (viewRootIter, groupIcon, "Groups");
				viewsStore.AppendValues (viewRootIter, usersIcon, "Users");
			}
			else if (server.ServerType.ToLower() == "generic ldap server" ||
				 server.ServerType.ToLower() == "openldap")
			{
				viewsStore.AppendValues (viewRootIter, compIcon, "Computers");
				viewsStore.AppendValues (viewRootIter, contactIcon, "Contacts");
				viewsStore.AppendValues (viewRootIter, groupIcon, "Groups");
				viewsStore.AppendValues (viewRootIter, usersIcon, "Users");
			}

			viewCustomIter = viewsStore.AppendValues (viewRootIter, customIcon, 
				"Custom Views");

			CustomViewManager cvm = new CustomViewManager ();
			string[] views = cvm.getViewNames ();

			foreach (string v in views)
			{
				TreeIter citer;

				citer = viewsStore.AppendValues (viewCustomIter, customIcon, v);
				customIters.Add (v, citer);
			}

			customIters.Add ("root", viewCustomIter);

			viewFactory._viewStore = viewsStore;
			viewFactory._ti = customIters;

			this.RowActivated += new RowActivatedHandler (viewRowActivated);
			this.ExpandAll ();
			this.ShowAll ();
		}

		private void DispatchViewSelectedEvent (string name)
		{
			if (ViewSelected != null)
			{
				ViewSelected (this, new ViewSelectedEventArgs (name));
			}
		}

		private void viewRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (viewsStore.GetIter (out iter, path))
			{
				string name = null;
				name = (string) viewsStore.GetValue (iter, (int)TreeCols.Name);

				DispatchViewSelectedEvent (name);
			} 		
		}

		public lat.View CurrentView
		{
			get { return _currentView; }
			set { _currentView = value; }
		}

		public ViewFactory theViewFactory
		{
			get { return viewFactory; }
		}
	}
}
