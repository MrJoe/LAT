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
		private LdapServer	server;
		private Gtk.Window	parentWindow;
		private Menu 		popup;
		private TreeStore	viewsStore;
		private TreeIter	viewRootIter;
		private TreeIter	viewCustomIter;
		private Hashtable 	customIters; 

		private enum TreeCols { Icon, Name };

		public event ViewSelectedHandler ViewSelected;

		public ViewsTreeView (LdapServer ldapServer, Gtk.Window parent) : base ()
		{
			server = ldapServer;
			customIters = new Hashtable ();
		
			this.ButtonPressEvent += new ButtonPressEventHandler (OnRightClick);

			viewsStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));
			this.Model = viewsStore;
			this.HeadersVisible = false;

			this.AppendColumn ("viewsIcon", new CellRendererPixbuf (), "pixbuf", 
					(int)TreeCols.Icon);

			this.AppendColumn ("viewsRoot", new CellRendererText (), "text", 
					(int)TreeCols.Name);

			AddViews (server.ServerType);

			Pixbuf customIcon = Pixbuf.LoadFromResource ("x-directory-normal.png");
			Pixbuf genIcon = Pixbuf.LoadFromResource ("text-x-generic.png");

			viewCustomIter = viewsStore.AppendValues (viewRootIter, customIcon, 
				"Custom Views");

			string[] customViews = Global.viewManager.GetCustomViewNames ();

			foreach (string v in customViews)
			{
				TreeIter citer;

				citer = viewsStore.AppendValues (viewCustomIter, genIcon, v);
				customIters.Add (v, citer);
			}

			customIters.Add ("root", viewCustomIter);

			this.RowActivated += new RowActivatedHandler (viewRowActivated);
			this.ExpandAll ();
			this.ShowAll ();
		}

		private void AddViews (string serverType)
		{
			Gdk.Pixbuf dirIcon = Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			Pixbuf compIcon = Pixbuf.LoadFromResource ("x-directory-remote-workgroup.png");
			Pixbuf contactIcon = Pixbuf.LoadFromResource ("contact-new.png");
			Pixbuf groupIcon = Pixbuf.LoadFromResource ("users.png");
			Pixbuf usersIcon = Pixbuf.LoadFromResource ("stock_person.png");

			viewRootIter = viewsStore.AppendValues (dirIcon, server.Host);
			string prefix = "";

			switch (serverType.ToLower())
			{
				case "microsoft active directory":
					prefix = "ad";
					break;

				case "openldap":
					prefix = serverType.ToLower();
					break;
			}

			ViewData vd = (ViewData) Global.viewManager.Lookup (prefix + "Computers");
			viewsStore.AppendValues (viewRootIter, compIcon, vd.DisplayName);

			vd = (ViewData) Global.viewManager.Lookup (prefix + "Contacts");
			viewsStore.AppendValues (viewRootIter, contactIcon, vd.DisplayName);

			vd = (ViewData) Global.viewManager.Lookup (prefix + "Groups");
			viewsStore.AppendValues (viewRootIter, groupIcon, vd.DisplayName);
			
			vd = (ViewData) Global.viewManager.Lookup (prefix + "Users");
			viewsStore.AppendValues (viewRootIter, usersIcon, vd.DisplayName);
		}

		[ConnectBefore]
		public void OnRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
			{
				DoPopUp ();
			}
		}

		private void DoPopUp()
		{
			popup = new Menu();

			AccelGroup ag = new AccelGroup ();

			ImageMenuItem newItem = new ImageMenuItem (
				Stock.New, new Gtk.AccelGroup(IntPtr.Zero));

			newItem.Activated += new EventHandler (OnNewActivate);
			newItem.Show ();
			popup.Append (newItem);

			ImageMenuItem deleteItem = new ImageMenuItem (
				Stock.Delete, new Gtk.AccelGroup(IntPtr.Zero));

			deleteItem.Activated += new EventHandler (OnDeleteActivate);
			deleteItem.Show ();

			popup.Append (deleteItem);

			ImageMenuItem propItem = new ImageMenuItem (Stock.Properties, ag);
			propItem.Activated += new EventHandler (OnPropertiesActivate);
			propItem.Show ();

			popup.Append (propItem);

			popup.Popup(null, null, null, 3,
					Gtk.Global.CurrentEventTime);
		}

		private void OnNewActivate (object o, EventArgs args)
		{
			CustomViewDialog cvd = new CustomViewDialog (server);
			cvd.Run ();

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("text-x-generic.png");

			TreeIter newIter;
			newIter = viewsStore.AppendValues (viewCustomIter, pb, cvd.Name);

			customIters.Add (cvd.Name, newIter);
		}

		public string GetSelectedViewName ()
		{
			TreeModel model;
			TreeIter iter;
			string name;

			if (this.Selection.GetSelected (out model, out iter))
			{
				name = (string) viewsStore.GetValue (iter, (int)TreeCols.Name);
				return name;
			}

			return null;
		}

		private void OnDeleteActivate (object o, EventArgs args) 
		{
			string viewName = GetSelectedViewName ();
			
			string msg = String.Format (
				Mono.Unix.Catalog.GetString (
				"Are you sure you want to delete: {0}"), viewName);

			if (Util.AskYesNo (parentWindow, msg))
			{
				if (!customIters.Contains (viewName))
				{
					string errMsg = "Unable to delete standard view";

					Util.MessageBox (parentWindow, errMsg, MessageType.Error);
					return;
				}

				TreeIter iter = (TreeIter) customIters [viewName];
				viewsStore.Remove (ref iter);

				Global.viewManager.DeleteView (viewName);
			}
		}

		private void OnPropertiesActivate (object o, EventArgs args) 
		{
			string viewName = GetSelectedViewName ();

			if (!customIters.Contains (viewName))
			{
				string prefix = Util.GetServerPrefix (server);
				viewName = prefix + viewName;
			}

			new CustomViewDialog (server, viewName);
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

				if (name.Equals ("Custom Views"))
					return;

				DispatchViewSelectedEvent (name);
			} 		
		}
	}

	public struct ViewData
	{
		public string Name;
		public string DisplayName;
		public string Type;
		public string Filter;
		public string Base;
		public int PrimaryKey;
		public string[] Cols;
		public string[] ColNames;
	}

	public class ViewManager
	{
		private string configFileName;
		private Hashtable views;

		public ViewManager ()
		{
			views = new Hashtable ();
		
			string dir = Environment.GetEnvironmentVariable("HOME");
			string tmp = Path.Combine (dir, ".lat");
			
			configFileName = Path.Combine (tmp, "views.xml");

			DirectoryInfo di = new DirectoryInfo (tmp);
			if (!di.Exists)
			{
				di.Create ();
			}

			FileInfo fi = new FileInfo (configFileName);
			if (!fi.Exists)
			{
				SetDefaultViews ();
			}
			
			LoadViews ();		
		}

		private void SetDefaultViews ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.LoadXml (Defines.VIEWS_XML);
			doc.Save (configFileName);
		}

		private void ParseNode (XmlNode node)
		{
			ViewData vd = new ViewData ();
			vd.Name = node.Attributes["name"].Value;
			vd.DisplayName = node.Attributes["displayName"].Value;
			vd.Type = node.Attributes["type"].Value;

			ArrayList cols = new ArrayList ();
			ArrayList colNames = new ArrayList ();

			foreach (XmlNode n in node.ChildNodes)
			{
				if (n.Name.Equals ("columns"))
				{
					vd.PrimaryKey = int.Parse (n.Attributes["primaryKey"].Value);
					foreach (XmlNode c in n.ChildNodes)
					{
						cols.Add (c.Attributes["name"].Value);
						colNames.Add (c.InnerText);
					}
				}
				else
				{
					if (n.Name.Equals ("filter"))
						vd.Filter = XmlConvert.DecodeName(n.InnerText);
					else if (n.Name.Equals ("searchBase"))
						vd.Base = n.InnerText;
				}
			}

			vd.Cols = (string[]) cols.ToArray (typeof (string));
			vd.ColNames = (string[]) colNames.ToArray (typeof (string));

			views.Add (vd.Name, vd);
		}

		public void LoadViews ()
		{
			XmlDocument doc = new XmlDocument ();
			doc.Load (configFileName);

			XmlNodeList nodeList;
			XmlElement root = doc.DocumentElement;

			nodeList = root.SelectNodes("//view");

			foreach (XmlNode view in nodeList)
			{
				ParseNode (view);
			}
		}

		// FIXME: better to override []
		public ViewData Lookup (string viewName)
		{
			if (views.Contains (viewName))
				return (ViewData) views[viewName];
			
			return new ViewData ();
		}

		public string[] GetCustomViewNames ()
		{
			ArrayList retVal = new ArrayList ();

			foreach (string k in views.Keys)
			{
				ViewData vd = (ViewData) views[k];
				
				if (!vd.Type.Equals ("standard"))
				{
					retVal.Add (vd.Name);
				}
			}

			return (string[]) retVal.ToArray (typeof (string));
		}
		
		public void AddView (ViewData newView)
		{
			views.Add (newView.Name, newView);
		}
		
		public void UpdateView (ViewData newView)
		{
			views [newView.Name] = newView;
		}
		
		public void DeleteView (string name)
		{
			views.Remove (name);
		}

		public void ReloadViews ()
		{
			views.Clear ();
			LoadViews ();
		}

		public void SaveViews ()	
		{
			XmlDocument doc = new XmlDocument ();
			XmlElement viewsElement, newView;

			viewsElement = doc.CreateElement ("views");

			foreach (string name in views.Keys)
			{
				ViewData vd = (ViewData) views [name];
			
				newView = doc.CreateElement ("view");
				newView.SetAttribute ("displayName", vd.DisplayName);
				newView.SetAttribute ("name", vd.Name);
				newView.SetAttribute ("type", vd.Type);

				XmlElement filter = doc.CreateElement ("filter");
				filter.InnerText = vd.Filter;
				newView.AppendChild (filter);

				XmlElement searchBase = doc.CreateElement ("searchBase");
				searchBase.InnerText = vd.Base;
				newView.AppendChild (searchBase);

				XmlElement columns = doc.CreateElement ("columns");
				columns.SetAttribute ("primaryKey", vd.PrimaryKey.ToString());

				for (int i = 0; i < vd.Cols.Length; i++)
				{
					XmlElement col = doc.CreateElement ("column");
					col.SetAttribute ("name", vd.Cols [i]);
					col.InnerText = vd.ColNames [i];

					columns.AppendChild (col);
				}

				newView.AppendChild (columns);
				viewsElement.AppendChild (newView);
			}
			
			doc.AppendChild (viewsElement);
			doc.Save (configFileName);
		}
	}
}
