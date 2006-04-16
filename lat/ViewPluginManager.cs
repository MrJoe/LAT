// 
// lat - ViewManager.cs
// Author: Loren Bandiera
// Copyright 2006 MMG Security, Inc.
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
using System.Collections;
using Gtk;
using Novell.Directory.Ldap;

namespace lat {

	public abstract class ViewPlugin
	{
		public ViewPlugin ()
		{
		}
	
		// Methods
		public abstract void Init ();
		public abstract void OnAddEntry (LdapServer server);
		public abstract void OnEditEntry (LdapServer server, LdapEntry le);
		public abstract void OnPopupShow (Menu popup);
			
		// Properties
		public abstract string[] Authors { get; }
		public abstract string[] ColumnAttributes { get; }
		public abstract string[] ColumnNames { get; }
		public abstract string Copyright { get; }
		public abstract string Description { get; }
		public abstract string Filter { get; }
		public abstract string Name { get; }
		public abstract Gdk.Pixbuf Icon { get; }		
	}
	
	public class ViewPluginManager
	{
		string pluginDirectory;
		ArrayList pluginList;
	
		public ViewPluginManager (string directory)
		{
			pluginDirectory = directory;
			pluginList = new ArrayList ();
		}

		public void LoadPlugins ()
		{
			pluginList.Add (new PosixUserViewPlugin ());
			pluginList.Add (new PosixGroupViewPlugin ());
			pluginList.Add (new PosixContactsViewPlugin ());
			pluginList.Add (new PosixComputerViewPlugin ());
			pluginList.Add (new ActiveDirectoryUserViewPlugin ());
			pluginList.Add (new ActiveDirectoryGroupViewPlugin ());
			pluginList.Add (new ActiveDirectoryContactsViewPlugin ());
			pluginList.Add (new ActiveDirectoryComputerViewPlugin ());
		}

		public ViewPlugin Find (string name)
		{
			foreach (ViewPlugin vp in pluginList)
				if (vp.Name == name)
						return vp;
						
			return null;
		}

		public ViewPlugin[] Plugins
		{
			get { return (ViewPlugin[]) pluginList.ToArray (typeof (ViewPlugin)); }
		}
		
		// Find plugins
		// Load plugins
		// Show views in viewTreeview
	}
}