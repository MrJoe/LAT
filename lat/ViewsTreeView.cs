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
		private Menu 		popup;
		private TreeStore	viewsStore;
		private TreeIter	viewRootIter;
		private TreeIter	viewCustomIter;

		private Hashtable customIters = new Hashtable ();

//		private ListStore _vs;
//		private TreeView _vt;

		private enum TreeCols { Icon, Name };

		public event ViewSelectedHandler ViewSelected;

		public ViewsTreeView (LdapServer ldapServer) : base ()
		{
			server = ldapServer;

//			_vs = valueStore;
//			_vt = valueTreeView;
			
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
			viewCustomIter = viewsStore.AppendValues (viewRootIter, customIcon, 
				"Custom Views");

//			CustomViewManager cvm = new CustomViewManager ();
//			string[] views = cvm.getViewNames ();

//			foreach (string v in views)
//			{
//				TreeIter citer;

//				citer = viewsStore.AppendValues (viewCustomIter, customIcon, v);
//				customIters.Add (v, citer);
//			}

			customIters.Add ("root", viewCustomIter);

//			viewFactory._viewStore = viewsStore;
//			viewFactory._ti = customIters;

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

			ImageMenuItem propItem = new ImageMenuItem (Stock.Properties, ag);
			propItem.Activated += new EventHandler (OnPropertiesActivate);
			propItem.Show ();

			popup.Append (propItem);

			popup.Popup(null, null, null, 3,
					Gtk.Global.CurrentEventTime);
		}

		private void OnPropertiesActivate (object o, EventArgs args) 
		{
			Console.WriteLine ("HERE");
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
	}

	public struct ViewData
	{
		public string Name;
		public string DisplayName;
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

		public ViewData Lookup (string viewName)
		{
			return (ViewData) views[viewName];
		}

/*		
		public string[] getViewNames ()
		{
			string[] retVal = new string [_views.Count];
			int count = 0;
			
			foreach (string s in _views.Keys)
			{
				CustomViewData cvd = (CustomViewData) _views[s];
				retVal [count] = cvd.Name;
				
				count++;
			}
			
			return retVal;
		}
		
		public CustomViewData Lookup (string name)
		{
			return (CustomViewData) _views[name];
		}
		
		public void addView (CustomViewData cvd)
		{
			_views.Add (cvd.Name, cvd);
		}
		
		public void updateView (CustomViewData cvd)
		{
			_views[cvd.Name] = cvd;
		}
		
		public void deleteView (string name)
		{
			_views.Remove (name);
		}

		public void reloadViews ()
		{
			_views.Clear ();
			loadViews ();
		}
		
		public void loadViews ()
		{
			try
			{
				XmlTextReader r = new XmlTextReader (_configFile);
				
				while (r.Read()) 
				{				
					if (r.Name == "view") 
					{								
						CustomViewData cvd = new CustomViewData (
							r.GetAttribute ("name"),
							r.GetAttribute ("filter"),
							r.GetAttribute ("base"),
							r.GetAttribute ("cols"));
				
						_views.Add (cvd.Name, cvd);				
					} 
			 	}
			 			
			 	r.Close ();
			}
			catch (Exception e)
			{
				Console.WriteLine (e.Message);
			}
		}
		
		public void saveViews ()	
		{	
			XmlTextWriter writer = new XmlTextWriter(_configFile,
						System.Text.Encoding.UTF8);
						
			writer.Formatting = Formatting.Indented;
			writer.WriteStartDocument(false);
			
			writer.WriteStartElement("views");
			
			foreach (string name in _views.Keys) 
			{		     			 	
				CustomViewData cvd = (CustomViewData) _views[name];
		 	
				writer.WriteStartElement("view", null);
			 		
				writer.WriteAttributeString ("name", cvd.Name);
				writer.WriteAttributeString ("filter", cvd.Filter);
				writer.WriteAttributeString ("base", cvd.Base);
				writer.WriteAttributeString ("cols", cvd.Cols);
				
	        		writer.WriteEndElement();
			}
	    		
			writer.WriteEndElement();
			
			writer.Flush();
			writer.Close();	
		}
*/
	}
}
