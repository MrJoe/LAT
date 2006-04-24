// 
// lat - AttributeEditorWidget.cs
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

namespace lat
{
	
	public class AttributeEditorWidget : Gtk.VBox
	{
		TreeView tv;
		ListStore store;
		
		public AttributeEditorWidget() : base ()
		{
			ScrolledWindow sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Automatic;
			sw.VscrollbarPolicy = PolicyType.Automatic;
			
			tv = new TreeView ();
			tv.Show ();
			
			store = new ListStore (typeof (string), typeof(string));
			tv.Model = store;
			
			tv.AppendColumn ("Name", new CellRendererText (), "text", 0);
			tv.AppendColumn ("Value", new CellRendererText (), "text", 1);
			
			sw.AddWithViewport (tv);
			sw.Show ();
			
			Button button = new Button ("Apply");
			button.Show ();
			
			this.PackStart (sw, true, true, 0);
			this.PackStart (button, false, false, 0);
		
			this.ShowAll ();
		}
	}
	
}
