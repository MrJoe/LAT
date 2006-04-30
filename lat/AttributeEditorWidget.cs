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
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class AttributeItem : Gtk.HBox
	{
		Label nameLabel;
		Entry valueEntry;
		bool removed;
		bool added;
		bool modified;
	
		public AttributeItem (string attrName, string attrValue, bool withAddButton, bool required) : base ()
		{
			removed = false;
			added = false;
			modified = false;
		
			this.BorderWidth = 6;
			this.Spacing = 6;
			
			VBox v = new VBox ();
			v.Spacing = 6;
			this.PackStart (v, true, true, 0);
			
			nameLabel = new Label (attrName);
			nameLabel.Xalign = 0;
			v.PackStart (nameLabel, true, false, 0);
			
			if (required) {
				v = new VBox ();
				v.Spacing = 6;
				this.PackStart (v, false, false, 0);
				
				Gtk.Image image = new Gtk.Image (Stock.Info, IconSize.Button);
				v.PackStart (image, true, false, 0);
			}
			
			if (withAddButton) {
				v = new VBox ();
				v.Spacing = 6;
				this.PackStart (v, false, false, 0);
				
				Gtk.Button button = new Gtk.Button ();
				button.Image = new Gtk.Image (Stock.Add, IconSize.Button);
				button.Clicked += new EventHandler (OnAddClicked);
				v.PackStart (button, true, false, 0);
			}

			v = new VBox ();
			v.Spacing = 6;
			this.PackStart (v, true, true, 0);			
			
			valueEntry = new Gtk.Entry ();
			valueEntry.Text = attrValue;
			valueEntry.Changed += new EventHandler (OnEntryChanged);
			valueEntry.Show ();
			v.PackStart (valueEntry, true, true, 0);

			v = new VBox ();
			v.Spacing = 6;
			this.PackStart (v, false, false, 0);

			Gtk.Button button2 = new Gtk.Button ();
			button2.Image = new Gtk.Image (Stock.Remove, IconSize.Button);
			button2.Clicked += new EventHandler (OnRemoveClicked);
			v.PackStart (button2, true, false, 0);			
			
			this.ShowAll ();
		}
		
		void OnEntryChanged (object o, EventArgs args)
		{
			modified = true;
		}
		
		void OnAddClicked (object o, EventArgs args)
		{
//			added = true;
		}

		void OnRemoveClicked (object o, EventArgs args)
		{
			removed = true;
			this.Destroy ();
		}
		
		public string AttributeName
		{
			get { return nameLabel.Text; }
		}
		
		public string AttributeValue
		{
			get { return valueEntry.Text; }
		}
		
		public bool WasAdded
		{
			get { return added; }
		}
		
		public bool WasRemoved
		{
			get { return removed; }
		}
		
		public bool WasModified
		{
			get { return modified; }
		}
	}
	
	public class AttributeEditorWidget : Gtk.VBox
	{
		ScrolledWindow sw;
		VBox mainVBox;
		ArrayList attributeItems;
		LdapServer currentServer;
		string currentDN;
	
		public AttributeEditorWidget() : base ()
		{
			attributeItems = new ArrayList ();
		
			sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Automatic;
			sw.VscrollbarPolicy = PolicyType.Automatic;

			mainVBox = new VBox ();
			sw.AddWithViewport (mainVBox);
			sw.Show ();
			
			HButtonBox hb = new HButtonBox ();			
			hb.Layout = ButtonBoxStyle.End;
			
			Button button = new Button ();
			button.Label = "Apply";
			button.Image = new Gtk.Image (Stock.Apply, IconSize.Button);
			button.Clicked += new EventHandler (OnApplyClicked);
			
			hb.Add (button);

			
			this.PackStart (sw, true, true, 0);
			this.PackStart (hb, false, false, 5);
		
			this.ShowAll ();
		}
	
		void OnApplyClicked (object o, EventArgs args)
		{
			ArrayList modList = new ArrayList ();
		
			foreach (AttributeItem ai in attributeItems) {
				LdapAttribute attribute = new LdapAttribute (ai.AttributeName, ai.AttributeValue);
			
				if (ai.WasRemoved) {
					LdapModification lm = new LdapModification (LdapModification.DELETE, attribute);
					modList.Add (lm);
				}
				
				if (ai.WasAdded) {				
					LdapModification lm = new LdapModification (LdapModification.ADD, attribute);
					modList.Add (lm);				
				}
				
				if (ai.WasModified) {
					LdapModification lm = new LdapModification (LdapModification.REPLACE, attribute);
					modList.Add (lm);
				}
			}
			
			Util.ModifyEntry (currentServer, null, currentDN, modList, Global.VerboseMessages);
		}
	
		public void Show (LdapServer server, LdapEntry entry, bool showAll)
		{
			if (mainVBox != null) {
				mainVBox.Destroy ();
				mainVBox = new VBox ();
				sw.AddWithViewport (mainVBox);
			}
			
			currentServer = server;
			currentDN = entry.DN;
		
			ArrayList allAttrs = new ArrayList ();
		
			LdapAttribute a = entry.getAttribute ("objectClass");

			for (int i = 0; i < a.StringValueArray.Length; i++) {
			
				string o = (string) a.StringValueArray[i];
				AttributeItem ai;

				if (i == 0)			
					ai = new AttributeItem ("objectClass", o, true, true);
				else 
					ai = new AttributeItem ("objectClass", o, false, false);
				
				mainVBox.PackStart (ai, true, true, 5);				

				attributeItems.Add (ai);
				
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

				SchemaParser sp = server.GetAttributeTypeSchema (attr.Name);

				foreach (string s in attr.StringValueArray) {
					AttributeItem ai = new AttributeItem (attr.Name, s, !sp.Single, false);
					mainVBox.PackStart (ai, true, true, 5);
					attributeItems.Add (ai);
				}
			}
			
			mainVBox.ShowAll ();
			
			if (!showAll)
				return;

			foreach (string n in allAttrs) {
					AttributeItem ai = new AttributeItem (n, "", false, false);
					mainVBox.PackStart (ai, true, true, 5);
					attributeItems.Add (ai);
			}					
		}		
	}
	
}
