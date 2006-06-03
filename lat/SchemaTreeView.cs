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
using System.Collections;
using System.IO;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class schemaSelectedEventArgs : EventArgs
	{
		private string _name;
		private string _parent;

		public schemaSelectedEventArgs (string name, string parent)
		{
			_name = name;
			_parent = parent;
		}

		public string Name
		{
			get { return _name; }
		}

		public string Parent
		{
			get { return _parent; }
		}
	}

	public delegate void schemaSelectedHandler (object o, schemaSelectedEventArgs args);

	public class SchemaTreeView : Gtk.TreeView
	{
		Gtk.Window parentWindow;
		
		TreeStore schemaStore;
		TreeIter objIter;
		TreeIter attrIter;
		TreeIter matIter;
		TreeIter synIter;

		LdapServer server;

		enum TreeCols { Icon, ObjectName };

		public event schemaSelectedHandler schemaSelected;

		public SchemaTreeView (LdapServer ldapServer, Gtk.Window parent) : base ()
		{
			server = ldapServer;
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

			TreeIter iter;
			iter = schemaStore.AppendValues (dirIcon, server.Host);

			objIter = schemaStore.AppendValues (iter, folderIcon, "Object Classes");
			schemaStore.AppendValues (objIter, null, "");
			
			attrIter = schemaStore.AppendValues (iter, folderIcon, "Attribute Types");
			schemaStore.AppendValues (attrIter, null, "");
			
			matIter = schemaStore.AppendValues (iter, folderIcon, "Matching Rules");
			schemaStore.AppendValues (matIter, null, "");
				
			synIter = schemaStore.AppendValues (iter, folderIcon, "LDAP Syntaxes");
			schemaStore.AppendValues (synIter, null, "");
				
			this.ShowAll ();
		}

		void DispatchDNSelectedEvent (string name, string parent)
		{
			if (schemaSelected != null)
				schemaSelected (this, new schemaSelectedEventArgs (name, parent));
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (schemaStore.GetIter (out iter, path)) {

				string name = null;
				name = (string) schemaStore.GetValue (iter, (int)TreeCols.ObjectName);

				if (name.Equals (server.Host))
					return;

				TreeIter parent;
				schemaStore.IterParent (out parent, iter);

				string parentName = null;
				parentName = (string) schemaStore.GetValue (parent, (int)TreeCols.ObjectName);

				DispatchDNSelectedEvent (name, parentName);
			} 		
		}
		
		TreeIter GetParent (string parentName)
		{
			switch (parentName) {
			
			case "Object Classes":
				return objIter;
				
			case "Attribute Types":
				return attrIter;
				
			case "Matching Rules":
				return matIter;
			
			case "LDAP Syntaxes":
				return synIter;
				
			default:
				throw new ArgumentOutOfRangeException (parentName);
			}		
		}

		string[] GetChildren (string parentName)
		{
			string[] childValues = null;
		
			switch (parentName) {
			
			case "Object Classes":
				childValues = server.GetObjectClasses ();
				break;
				
			case "Attribute Types":
				childValues = server.GetAttributeTypes ();
				break;
				
			case "Matching Rules":
				childValues = server.GetMatchingRules ();			
				break;
			
			case "LDAP Syntaxes":
				childValues = server.GetLDAPSyntaxes ();
				break;
				
			default:
				break;
			}
			
			return childValues;			
		}
		
		void OnRowExpanded (object o, RowExpandedArgs args)
		{
			string name = null;
			bool firstPass = false;
			
			name = (string) schemaStore.GetValue (args.Iter, (int)TreeCols.ObjectName);
			if (name == server.Host)
				return;

			TreeIter parent, child;
			schemaStore.IterParent (out parent, args.Iter);
			schemaStore.IterChildren (out child, args.Iter);

			string childName = (string)schemaStore.GetValue (child, (int)TreeCols.ObjectName);
			
			if (childName == "")
				firstPass = true;
			else
				return;

			try {

				Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("text-x-generic.png");
		 		
		 		string[] kids = GetChildren (name);
				foreach (string s in kids) {
								
					if (firstPass) {
						schemaStore.SetValue (child, (int)TreeCols.Icon, pb);
						schemaStore.SetValue (child, (int)TreeCols.ObjectName, s);
						
						firstPass = false;
						
					} else {
						schemaStore.AppendValues (GetParent(name), pb, s);
					}								
				}

			} catch {

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
