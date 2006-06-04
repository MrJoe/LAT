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

		public ViewSelectedEventArgs (string name)
		{
			viewName = name;
		}

		public string Name
		{
			get { return viewName; }
		}
	}

	public delegate void ViewSelectedHandler (object o, ViewSelectedEventArgs args);

	public class ViewsTreeView : Gtk.TreeView
	{
		private LdapServer	server;
//		private Gtk.Window	parentWindow;
//		private Menu 		popup;
		private TreeStore	viewsStore;
		private TreeIter	viewRootIter;

		private enum TreeCols { Icon, Name };

		public event ViewSelectedHandler ViewSelected;

		public ViewsTreeView (LdapServer ldapServer, Gtk.Window parent) : base ()
		{
			server = ldapServer;
//			parentWindow = parent;
		
			viewsStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));
			this.Model = viewsStore;
			this.HeadersVisible = false;

			this.AppendColumn ("viewsIcon", new CellRendererPixbuf (), "pixbuf", 
					(int)TreeCols.Icon);

			this.AppendColumn ("viewsRoot", new CellRendererText (), "text", 
					(int)TreeCols.Name);

			AddViews ();

//			this.ButtonPressEvent += new ButtonPressEventHandler (OnRightClick);
			this.RowActivated += new RowActivatedHandler (ViewRowActivated);
			
			this.ExpandAll ();
			this.ShowAll ();
		}

		void AddViews ()
		{
			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			viewRootIter = viewsStore.AppendValues (dirIcon, server.Host);

			ConnectionProfile cp = null;
			if (server.ProfileName == null) 
				cp = new ConnectionProfile ();
			else
				cp = Global.profileManager [server.ProfileName];
			
			if (cp.ServerType == null)
				cp.ServerType = server.ServerTypeString;
			
			if (cp.ActiveServerViews == null) 
				cp.SetDefaultServerViews ();
				
			foreach (ViewPlugin vp in Global.pluginManager.ServerViewPlugins)
				if (cp.ActiveServerViews.Contains (vp.GetType().ToString()))
					viewsStore.AppendValues (viewRootIter, vp.Icon, vp.Name);
		}

		public void Refresh ()
		{
			viewsStore.Clear ();
			AddViews ();
			this.ExpandAll ();
		}

//		[ConnectBefore]
//		public void OnRightClick (object o, ButtonPressEventArgs args)
//		{
//			if (args.Event.Button == 3)
//				DoPopUp ();
//		}

//		private void DoPopUp()
//		{
//			popup = new Menu();
//
//			AccelGroup ag = new AccelGroup ();
//
//			ImageMenuItem newItem = new ImageMenuItem (
//				Stock.New, new Gtk.AccelGroup(IntPtr.Zero));
//
//			newItem.Activated += new EventHandler (OnNewActivate);
//			newItem.Show ();
//			popup.Append (newItem);
//
//			ImageMenuItem deleteItem = new ImageMenuItem (
//				Stock.Delete, new Gtk.AccelGroup(IntPtr.Zero));
//
//			deleteItem.Activated += new EventHandler (OnDeleteActivate);
//			deleteItem.Show ();
//
//			popup.Append (deleteItem);
//
//			string viewName = GetSelectedViewName ();
//
//			if (viewName.ToLower() != "custom views") {
//				ImageMenuItem propItem = new ImageMenuItem (Stock.Properties, ag);
//				propItem.Activated += new EventHandler (OnPropertiesActivate);
//				propItem.Show ();
//				popup.Append (propItem);
//			}
//
//			popup.Popup(null, null, null, 3,
//					Gtk.Global.CurrentEventTime);
//		}
//
//		private void OnNewActivate (object o, EventArgs args)
//		{
//			CustomViewDialog cvd = new CustomViewDialog (server);
//			cvd.Run ();
//
//			if (cvd.UserResponse == ResponseType.Cancel || cvd.Name == null)
//				return;
//
//			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("text-x-generic.png");
//
//			TreeIter newIter;
//			newIter = viewsStore.AppendValues (viewCustomIter, pb, cvd.Name);
//
//			customIters.Add (cvd.Name, newIter);
//		}

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

		private void DispatchViewSelectedEvent (string name)
		{
			if (ViewSelected != null)
				ViewSelected (this, new ViewSelectedEventArgs (name));
		}

		void ViewRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (viewsStore.GetIter (out iter, path)) {

				string name = null;
				name = (string) viewsStore.GetValue (iter, (int)TreeCols.Name);

				DispatchViewSelectedEvent (name);
			} 		
		}
	}
}
