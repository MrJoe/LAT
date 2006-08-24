// 
// lat - SchemaTreeView.cs
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
using System;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class schemaSelectedEventArgs : EventArgs
	{
		string _name;
		string _parent;
		string _server;

		public schemaSelectedEventArgs (string name, string parent, string server)
		{
			_name = name;
			_parent = parent;
			_server = server;
		}

		public string Name
		{
			get { return _name; }
		}

		public string Parent
		{
			get { return _parent; }
		}

		public string Server
		{
			get { return _server; }
		}		
	}

	public delegate void schemaSelectedHandler (object o, schemaSelectedEventArgs args);

	public class SchemaTreeView : Gtk.TreeView
	{
		Gtk.Window parentWindow;
		
		TreeStore schemaStore;
		TreeIter rootIter;

		enum TreeCols { Icon, ObjectName };

		public event schemaSelectedHandler schemaSelected;

		public SchemaTreeView (Gtk.Window parent) : base ()
		{
			parentWindow = parent;
			
			schemaStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));

			this.Model = schemaStore;
			this.HeadersVisible = false;

			this.RowActivated += new RowActivatedHandler (OnRowActivated);
			this.RowExpanded += new RowExpandedHandler (OnRowExpanded);
			
			this.AppendColumn ("icon", new CellRendererPixbuf (), "pixbuf", (int)TreeCols.Icon);
			this.AppendColumn ("ldapRoot", new CellRendererText (), "text", (int)TreeCols.ObjectName);

			Gdk.Pixbuf dirIcon = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			Gdk.Pixbuf folderIcon = Gdk.Pixbuf.LoadFromResource ("x-directory-normal.png");

			rootIter = schemaStore.AppendValues (dirIcon, "Servers");

			foreach (string n in Global.Connections.ConnectionNames) {
				TreeIter iter = schemaStore.AppendValues (rootIter, dirIcon, n);

				TreeIter objIter;
				TreeIter attrIter;
				TreeIter matIter;
				TreeIter synIter;

				objIter = schemaStore.AppendValues (iter, folderIcon, "Object Classes");
				schemaStore.AppendValues (objIter, null, "");
				
				attrIter = schemaStore.AppendValues (iter, folderIcon, "Attribute Types");
				schemaStore.AppendValues (attrIter, null, "");
				
				matIter = schemaStore.AppendValues (iter, folderIcon, "Matching Rules");
				schemaStore.AppendValues (matIter, null, "");
					
				synIter = schemaStore.AppendValues (iter, folderIcon, "LDAP Syntaxes");
				schemaStore.AppendValues (synIter, null, "");				
			}
				
			this.ShowAll ();
		}

		public void Refresh ()
		{
			schemaStore.Clear ();
			
			Gdk.Pixbuf dirIcon = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			Gdk.Pixbuf folderIcon = Gdk.Pixbuf.LoadFromResource ("x-directory-normal.png");
			
			rootIter = schemaStore.AppendValues (dirIcon, "Servers");
			TreePath path = schemaStore.GetPath (rootIter);
			
			foreach (string n in Global.Connections.ConnectionNames) {
				TreeIter iter = schemaStore.AppendValues (rootIter, dirIcon, n);

				TreeIter objIter;
				TreeIter attrIter;
				TreeIter matIter;
				TreeIter synIter;

				objIter = schemaStore.AppendValues (iter, folderIcon, "Object Classes");
				schemaStore.AppendValues (objIter, null, "");
				
				attrIter = schemaStore.AppendValues (iter, folderIcon, "Attribute Types");
				schemaStore.AppendValues (attrIter, null, "");
				
				matIter = schemaStore.AppendValues (iter, folderIcon, "Matching Rules");
				schemaStore.AppendValues (matIter, null, "");
					
				synIter = schemaStore.AppendValues (iter, folderIcon, "LDAP Syntaxes");
				schemaStore.AppendValues (synIter, null, "");				
			}			

			this.ExpandRow (path, false);		
		}

		public void AddConnection (string name)
		{
			Gdk.Pixbuf dirIcon = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server.png");
			TreeIter iter = schemaStore.AppendValues (rootIter, dirIcon, name);
			schemaStore.AppendValues (iter, null, "");
		}

		void DispatchDNSelectedEvent (string name, string parent, string server)
		{
			if (schemaSelected != null)
				schemaSelected (this, new schemaSelectedEventArgs (name, parent, server));
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (schemaStore.GetIter (out iter, path)) {

				string name = null;
				name = (string) schemaStore.GetValue (iter, (int)TreeCols.ObjectName);

				TreeIter parent;
				schemaStore.IterParent (out parent, iter);
				
				string parentName = (string) schemaStore.GetValue (parent, (int)TreeCols.ObjectName);
				string serverName = FindServerName (iter, schemaStore);
				
				if (name.Equals (serverName)) {
					DispatchDNSelectedEvent (name, parentName, serverName);
					return;
				}

				DispatchDNSelectedEvent (name, parentName, serverName);
			} 		
		}
		
		string[] GetChildren (string parentName, Connection conn)
		{
			string[] childValues = null;
		
			switch (parentName) {
			
			case "Object Classes":
				childValues = conn.Data.ObjectClasses;
				break;
				
			case "Attribute Types":
				childValues = conn.Data.GetAttributeTypes ();
				break;
				
			case "Matching Rules":
				childValues = conn.Data.GetMatchingRules ();			
				break;
			
			case "LDAP Syntaxes":
				childValues = conn.Data.GetLdapSyntaxes ();
				break;
				
			default:
				break;
			}
			
			return childValues;			
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
			schemaStore.IterParent (out parent, iter);
			
			if (!schemaStore.IterIsValid (parent))
				return null;
			
			string parentName = (string)model.GetValue (parent, (int)TreeCols.ObjectName);			
			if (parentName == "Servers")
				return (string)model.GetValue (iter, (int)TreeCols.ObjectName);
			
			return FindServerName (parent, model);
		}
		
		void OnRowExpanded (object o, RowExpandedArgs args)
		{
			string name = null;
			name = (string) schemaStore.GetValue (args.Iter, (int)TreeCols.ObjectName);			
			if (name == "Servers")
				return;

			TreeIter child;
			schemaStore.IterChildren (out child, args.Iter);
			
			string childName = (string) schemaStore.GetValue (child, (int)TreeCols.ObjectName);
			if (childName != "")
				return;
					
			schemaStore.Remove (ref child);
					
			Log.Debug ("Row expanded {0}", name);
			
			string serverName = FindServerName (args.Iter, schemaStore);
			Connection conn = Global.Connections [serverName];

			try {

				Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("text-x-generic.png");
		 				 		
		 		string[] kids = GetChildren (name, conn);
				foreach (string s in kids) { 
					schemaStore.AppendValues (args.Iter, pb, s);
				}
				
				TreePath path = schemaStore.GetPath (args.Iter);
				this.ExpandRow (path, false);				

			} catch (Exception e) {

				Log.Debug (e.ToString());

				string	msg = Mono.Unix.Catalog.GetString (
					"Unable to read schema information from server");

				HIGMessageDialog dialog = new HIGMessageDialog (
					parentWindow,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Server error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			}
		}
	}
}
