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
using Gtk;

namespace lat {

	public abstract class ViewPlugin
	{
		public ViewPlugin ()
		{
		}
	
		// Methods
		protected abstract void Init ();
		protected abstract void OnAddEntry ();
		protected abstract void OnEditEntry ();
		protected abstract void OnDeleteEntry ();
			
		// Properties
		public abstract string[] Authors { get; }
		public abstract string Copyright { get; }
		public abstract string Description { get; }
		public abstract string Name { get; }
		public abstract Gdk.Pixbuf Icon { get; }		
	}
	
	public class ViewPluginManager
	{
		string pluginDirectory;
	
		public ViewPluginManager (string directory)
		{
			pluginDirectory = directory;
		}
	}
}