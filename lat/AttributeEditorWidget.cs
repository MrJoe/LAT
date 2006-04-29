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
using System.Collections;
using Gtk;
using Novell.Directory.Ldap;

namespace lat
{	
	public class AttributeEditorWidget : Gtk.VBox
	{
		ScrolledWindow sw;
		Table table;
	
		public AttributeEditorWidget() : base ()
		{
			sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Automatic;
			sw.VscrollbarPolicy = PolicyType.Automatic;

			table = new Table (0, 0, false);
			sw.AddWithViewport (table);
			sw.Show ();
			
			Button button = new Button ("Apply");
			button.Show ();
			
			this.PackStart (sw, true, true, 0);
//			this.PackStart (button, false, false, 0);
		
			this.ShowAll ();
		}

		void AddAttributeItem (string attrName, string attrValue)
		{
			uint rowNum = table.NRows + 1;
			uint topAttachNum = rowNum - 1;
		
			table.Resize (rowNum + 1, 5);
			table.ColumnSpacing = 10;
			table.RowSpacing = 5;
			
			Label label = new Label (attrName);
			table.Attach (label, 0, 1, topAttachNum, rowNum, AttachOptions.Fill, AttachOptions.Expand, 0, 0); 

			Gtk.Image image = new Gtk.Image (Stock.Info, IconSize.Button);
			table.Attach (image, 1, 2, topAttachNum, rowNum, AttachOptions.Fill, AttachOptions.Expand, 0, 0);
			
			Gtk.Button button = new Gtk.Button ();
			button.Image = new Gtk.Image (Stock.Add, IconSize.Button);
			table.Attach (button, 2, 3, topAttachNum, rowNum, AttachOptions.Fill, AttachOptions.Expand, 0, 0);
			
			Gtk.Entry attrEntry = new Gtk.Entry ();
			attrEntry.Text = attrValue;
			attrEntry.Show ();
			table.Attach (attrEntry, 3, 4, topAttachNum, rowNum, AttachOptions.Fill, AttachOptions.Expand, 0, 0);

			Gtk.Button button2 = new Gtk.Button ();
			button2.Image = new Gtk.Image (Stock.Remove, IconSize.Button);
			table.Attach (button2, 4, 5, topAttachNum, rowNum, AttachOptions.Fill, AttachOptions.Expand, 0, 0);

			table.ShowAll ();			
		}
		
		public void Show (LdapServer server, LdapEntry entry)
		{
			if (table != null) {
				table.Destroy ();
				table = new Table (1, 5, false);
				sw.AddWithViewport (table);
			}
		
			ArrayList allAttrs = new ArrayList ();
		
			LdapAttribute a = entry.getAttribute ("objectClass");

			foreach (string o in a.StringValueArray) {

				AddAttributeItem ("objectClass", o);

				string[] attrs = server.GetAllAttributes (o);
				
				foreach (string at in attrs)
					if (!allAttrs.Contains (at))
						allAttrs.Add (at);
			}

			LdapAttributeSet attributeSet = entry.getAttributeSet ();

			foreach (LdapAttribute attr in attributeSet) {

				if (allAttrs.Contains (attr.Name))
					allAttrs.Remove (attr.Name);

				if (attr.Name.ToLower() == "objectclass")
					continue;

				foreach (string s in attr.StringValueArray)
					AddAttributeItem (attr.Name, s);
			}
			
//			if (!showAllAttributes.Active)
//				return;

//			foreach (string n in allAttrs)
//				valuesStore.AppendValues (n, "");		
		}		
	}
	
}
