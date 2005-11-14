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
		private TreeStore browserStore;
		private TreeIter objIter;
		private TreeIter attrIter;

		private LdapServer server;
		private Gtk.Window _parent;

		private enum TreeCols { Icon, DN };

		public event schemaSelectedHandler schemaSelected;

		public SchemaTreeView (LdapServer ldapServer, Gtk.Window parent) : base ()
		{
			server = ldapServer;
			_parent = parent;

			browserStore = new TreeStore (typeof (Gdk.Pixbuf), typeof (string));

			this.Model = browserStore;
			this.HeadersVisible = false;

			this.RowActivated += new RowActivatedHandler (ldapRowActivated);

			this.AppendColumn ("icon", new CellRendererPixbuf (), "pixbuf", (int)TreeCols.Icon);
			this.AppendColumn ("ldapRoot", new CellRendererText (), "text", (int)TreeCols.DN);

			Gdk.Pixbuf pb = _parent.RenderIcon (Stock.Convert, IconSize.Menu, "");

			TreeIter iter;
			iter = browserStore.AppendValues (pb, server.Host);

			objIter = browserStore.AppendValues (iter, pb, "Object Classes");
			LdapEntry[] objEntries = server.GetObjectClasses ();

			ArrayList tmp = new ArrayList ();

			foreach (LdapEntry le in objEntries)
			{
				LdapAttribute la = le.getAttribute ("objectclasses");
						
				foreach (string s in la.StringValueArray)
				{
					SchemaParser sp = new SchemaParser (s);
					tmp.Add (sp.Names[0]);
				}
			}

			tmp.Sort ();

			foreach (string n in tmp)
			{
				browserStore.AppendValues (objIter, pb, n);
			}

			tmp.Clear ();

			attrIter = browserStore.AppendValues (iter, pb, "Attribute Types");
			LdapEntry[] attrEntries = server.GetAttributeTypes ();

			foreach (LdapEntry le in attrEntries)
			{
				LdapAttribute la = le.getAttribute ("attributetypes");
						
				foreach (string s in la.StringValueArray)
				{
					SchemaParser sp = new SchemaParser (s);
					tmp.Add (sp.Names[0]);
				}
			}

			tmp.Sort ();

			foreach (string n in tmp)
			{
				browserStore.AppendValues (attrIter, pb, n);
			}

			this.ShowAll ();
		}

		private void DispatchDNSelectedEvent (string name, string parent)
		{
			if (schemaSelected != null)
			{
				schemaSelected (this, new schemaSelectedEventArgs (name, parent));
			}
		}

		public string getSelectedDN ()
		{
			TreeModel ldapModel;
			TreeIter ldapIter;
			string dn;

			if (this.Selection.GetSelected (out ldapModel, out ldapIter))
			{
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
			{
				return ldapIter;
			}

			return ldapIter;
		}

		private void ldapRowActivated (object o, RowActivatedArgs args)
		{	
			TreePath path = args.Path;
			TreeIter iter;
			
			if (browserStore.GetIter (out iter, path))
			{
				string name = null;
				name = (string) browserStore.GetValue (iter, (int)TreeCols.DN);

				if (name.Equals (server.Host))
				{
					return;
				}

				TreeIter parent;
				browserStore.IterParent (out parent, iter);

				string parentName = null;
				parentName = (string) browserStore.GetValue (parent, (int)TreeCols.DN);

				DispatchDNSelectedEvent (name, parentName);
			} 		
		}
	}
}
