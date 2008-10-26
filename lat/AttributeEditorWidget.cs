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
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using Gtk;
using GLib;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class AttributeEditorWidget : Gtk.VBox
	{
		ScrolledWindow sw;
		Button applyButton;
		TreeView tv;
		ListStore store;

		Connection conn;
		string currentDN;
		bool displayAll;
		
		List<string> allAttrs;
		NameValueCollection currentAttributes;

		public AttributeEditorWidget() : base ()
		{
			sw = new ScrolledWindow ();
			sw.HscrollbarPolicy = PolicyType.Automatic;
			sw.VscrollbarPolicy = PolicyType.Automatic;

			store = new ListStore (typeof (string), typeof(string));
			store.SetSortColumnId (0, SortType.Ascending);
			
			tv = new TreeView ();
			tv.Model = store;
			
			TreeViewColumn col;
			col = tv.AppendColumn ("Name", new CellRendererText (), "text", 0);
			col.SortColumnId = 0;

			CellRendererText cell = new CellRendererText ();
			cell.Editable = true;
			cell.Edited += new EditedHandler (OnAttributeEdit);
			
			tv.AppendColumn ("Value", cell, "text", 1);						
			
			tv.KeyPressEvent += new KeyPressEventHandler (OnKeyPress);
			tv.ButtonPressEvent += new ButtonPressEventHandler (OnRightClick);
			tv.RowActivated += new RowActivatedHandler (OnRowActivated);
			
			sw.AddWithViewport (tv);			
					
			HButtonBox hb = new HButtonBox ();			
			hb.Layout = ButtonBoxStyle.End;
			
			applyButton = new Button ();
			applyButton.Label = "Apply";
			applyButton.Image = new Gtk.Image (Stock.Apply, IconSize.Button);
			applyButton.Clicked += new EventHandler (OnApplyClicked);
			applyButton.Sensitive = false;
			
			hb.Add (applyButton);
			
			this.PackStart (sw, true, true, 0);
			this.PackStart (hb, false, false, 5);
		
			this.ShowAll ();
		}

		void OnApplyClicked (object o, EventArgs args)
		{
			List<LdapModification> modList = new List<LdapModification> ();
			NameValueCollection newAttributes = new NameValueCollection ();
			
			foreach (object[] row in this.store) {
			
				string newValue = row[1].ToString();
				
				if (newValue == "" || newValue == null)
					continue;
					
				newAttributes.Add (row[0].ToString(), newValue);
			}

			foreach (string key in newAttributes.AllKeys) {
			
				string[] newValues = newAttributes.GetValues(key);
				string[] oldValues = currentAttributes.GetValues (key);
				LdapAttribute la = new LdapAttribute (key, newValues);
				
				if (oldValues == null) {					
					LdapModification lm = new LdapModification (LdapModification.ADD, la);
					modList.Add (lm);
				} else {
										
					foreach (string nv in newValues) {
					
						bool foundMatch = false;
						foreach (string ov in oldValues)
							if (ov == nv)
								foundMatch = true;
								
						if (!foundMatch) {
							LdapModification lm = new LdapModification (LdapModification.REPLACE, la);
							modList.Add (lm);
						}
					}
				}				
			}

			foreach (string key in currentAttributes.AllKeys) {
				string[] newValues = newAttributes.GetValues (key);
								
				if (newValues == null) {
					string[] oldValues = currentAttributes.GetValues (key);
					LdapAttribute la = new LdapAttribute (key, oldValues);
					LdapModification lm = new LdapModification (LdapModification.DELETE, la);
					modList.Add (lm);
				} else {
					LdapAttribute la = new LdapAttribute (key, newValues);
					LdapModification lm = new LdapModification (LdapModification.REPLACE, la);
					modList.Add (lm);
				}
			}

			Util.ModifyEntry (conn, currentDN, modList.ToArray());
		}
	
		void OnAttributeEdit (object o, EditedArgs args)
		{
			TreeIter iter;

			if (!store.GetIterFromString (out iter, args.Path))
				return;

			string oldText = (string) store.GetValue (iter, 1);
			if (oldText == args.NewText)
				return;
				
			store.SetValue (iter, 1, args.NewText);
			applyButton.Sensitive = true;
		}
	
		public void Show (Connection connection, LdapEntry entry, bool showAll)
		{
			displayAll = showAll;
			conn = connection;
			currentDN = entry.DN;			
			currentAttributes = new NameValueCollection ();

			// FIXME: crashes after an apply if I don't re-create the store;	
			store = new ListStore (typeof (string), typeof(string));
			store.SetSortColumnId (0, SortType.Ascending);
			tv.Model = store;
			
//			store.Clear ();
		
			allAttrs = new List<string> ();
			LdapAttribute a = entry.getAttribute ("objectClass");
			
			for (int i = 0; i < a.StringValueArray.Length; i++) {
			
				string o = (string) a.StringValueArray[i];	
				store.AppendValues ("objectClass", o);
				currentAttributes.Add ("objectClass", o);
				
				string[] attrs = conn.Data.GetAllAttributes (o);
				if (attrs != null) {
					foreach (string at in attrs)
						if (!allAttrs.Contains (at))
							allAttrs.Add (at);
				} else {
					Log.Debug("Could not retrieve any attribute for objectClass " + o);
				}
			}
			
			LdapAttributeSet attributeSet = entry.getAttributeSet ();
			
			// Fedora Directory Server supports an Access Control Item (ACI)
			// but it is not listed as "allowed attribute" for any objectClass 
			// found in Fedora's LDAP schema.
			if (showAll && conn.Settings.ServerType == LdapServerType.FedoraDirectory) {
				LdapEntry[] acientries = conn.Data.Search(	currentDN, 
														LdapConnection.SCOPE_BASE,
						    							"objectclass=*", 
						    							new string[] {"aci"} );
						    							
				if (acientries.Length > 0) {
					LdapEntry acientry = acientries[0];
					LdapAttribute aciattr = acientry.getAttribute("aci");
					if (aciattr != null) 
						if (attributeSet.Add(aciattr) == false)
							Log.Debug ("Could not add ACI attribute.");
				}
			}
			
			foreach (LdapAttribute attr in attributeSet) {

				if (allAttrs.Contains (attr.Name))
					allAttrs.Remove (attr.Name);

				if (attr.Name.ToLower() == "objectclass")
					continue;

				try {
				
					foreach (string s in attr.StringValueArray) {
						store.AppendValues (attr.Name, s);
						currentAttributes.Add (attr.Name, s);
					}
					
				} catch (ArgumentOutOfRangeException e) {					
					// FIXME: this only happens with gmcs
					store.AppendValues (attr.Name, "");
					Log.Debug ("Show attribute arugment out of range: {0}", attr.Name);
					Log.Debug (e.Message);
				}
			}

			if (!showAll)
				return;

			foreach (string n in allAttrs)
				store.AppendValues (n, "");
		}
		
		void InsertAttribute ()
		{
			string attrName;
			TreeModel model;
			TreeIter iter;

			if (!tv.Selection.GetSelected (out model, out iter))
				return;
				
			attrName = (string) store.GetValue (iter, 0);
						
			if (attrName == null)
				return;
								
			SchemaParser sp = conn.Data.GetAttributeTypeSchema (attrName);
			
			if (!sp.Single) {
				TreeIter newRow = store.InsertAfter (iter);
				store.SetValue (newRow, 0, attrName);
				applyButton.Sensitive = true;
			} else {
			
				HIGMessageDialog dialog = new HIGMessageDialog (
					null,
					0,
					Gtk.MessageType.Info,
					Gtk.ButtonsType.Ok,
					"Unable to insert value",
					"Multiple values not supported by this attribute");

				dialog.Run ();
				dialog.Destroy ();
			}						
		}
		
		void DeleteAttribute ()
		{
			TreeModel model;
			TreeIter iter;

			if (!tv.Selection.GetSelected (out model, out iter))
				return;
				
			store.Remove (ref iter);
			applyButton.Sensitive = true;
		}
		
		void OnKeyPress (object o, KeyPressEventArgs args)
		{
			switch (args.Event.Key) {
			
			case Gdk.Key.Insert:
			case Gdk.Key.KP_Insert:
				InsertAttribute ();
				break;
				
			case Gdk.Key.Delete:
			case Gdk.Key.KP_Delete:
				DeleteAttribute ();
				break;
				
			default:
				break;
			}				
		}
		
		[ConnectBefore]
		void OnRightClick (object o, ButtonPressEventArgs args)
		{
			if (args.Event.Button == 3)
				DoPopUp ();
		}

		void RunViewerPlugin (AttributeViewPlugin avp, string attributeName)
		{
			LdapEntry le = conn.Data.GetEntry (currentDN);
			LdapAttribute la = le.getAttribute (attributeName);
			
			bool existing = false;
			if (la != null)
				existing = true; 

			LdapAttribute newla = new LdapAttribute (attributeName);
									
			switch (avp.DataType) {
			
			case ViewerDataType.Binary:
				if (existing)
					avp.OnActivate (attributeName, SupportClass.ToByteArray (la.ByteValue));
				else
					avp.OnActivate (attributeName, new byte[0]);
				break;

			case ViewerDataType.String:
				if (existing)
					avp.OnActivate (attributeName, la.StringValue);
				else
					avp.OnActivate (attributeName, "");
				break;				
			}
		
			if (avp.ByteValue != null)
				newla.addBase64Value (System.Convert.ToBase64String (avp.ByteValue, 0, avp.ByteValue.Length));
			else if (avp.StringValue != null)
				newla.addValue (avp.StringValue);
			else
				return;
			
			LdapModification lm;
			if (existing)
				lm = new LdapModification (LdapModification.REPLACE, newla);
			else
				lm = new LdapModification (LdapModification.ADD, newla);

			List<LdapModification> modList = new List<LdapModification> ();
			modList.Add (lm);			
			Util.ModifyEntry (conn, currentDN, modList.ToArray());

			this.Show (conn, conn.Data.GetEntry (currentDN), displayAll);
		}

		void OnRowActivated (object o, RowActivatedArgs args)
		{
			TreePath path = args.Path;
			TreeIter iter;
			
			if (store.GetIter (out iter, path)) {
			
				string name = null;
				name = (string) store.GetValue (iter, 0);
					
				foreach (AttributeViewPlugin avp in Global.Plugins.AttributeViewPlugins) {
					foreach (string an in avp.AttributeNames)
						if (an.ToLower() == name.ToLower())
							if (conn.AttributeViewers.Contains (avp.GetType().ToString()))
								RunViewerPlugin (avp, name);
				}
			} 		
		}

		void OnInsertActivate (object o, EventArgs args)
		{
			InsertAttribute ();
		}
		
		void OnDeleteActivate (object o, EventArgs args)
		{
			DeleteAttribute ();
		}
	
		public string GetAttributeName ()
		{
			TreeModel model;
			TreeIter iter;
			string name;

			if (tv.Selection.GetSelected (out model, out iter)) {
				name = (string) store.GetValue (iter, 0);
				return name;
			}

			return null;
		}
	
		byte[] ReadFileBytes (string fileName)
		{
			List<byte> fileBytes = new List<byte> ();
		
			try {
							
				FileStream fs = File.OpenRead (fileName);
				
				byte[] buf = new byte[4096];
				int ret = 0;
				
				do {
				
					ret = fs.Read (buf, 0, buf.Length);
					for (int i = 0; i < ret; i++)
						fileBytes.Add (buf[i]);
				
				} while (ret != 0);
				
				fs.Close ();				
			
			} catch (Exception e) {
				Log.Debug (e);
				
				HIGMessageDialog dialog = new HIGMessageDialog (
					null,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Add binary value error",
					e.Message);

				dialog.Run ();
				dialog.Destroy ();				
			}
			
			return fileBytes.ToArray();
		}
	
		void OnAddBinaryValueActivate (object o, EventArgs args)
		{
			FileChooserDialog fcd = new FileChooserDialog (
				Mono.Unix.Catalog.GetString ("Select file to add as binary attribute"),
				Gtk.Stock.Open, 
				null, 
				FileChooserAction.Open);

			fcd.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			fcd.AddButton (Gtk.Stock.Open, ResponseType.Ok);

			fcd.SelectMultiple = false;

			ResponseType response = (ResponseType) fcd.Run();
			if (response == ResponseType.Ok) {

				byte[] fileBytes = ReadFileBytes (fcd.Filename);
				if (fileBytes.Length == 0)
					return;
				
				string attributeName = GetAttributeName ();
				string attributeValue = Base64.encode (SupportClass.ToSByteArray (fileBytes));
				
				LdapEntry le = conn.Data.GetEntry (currentDN);
				LdapAttribute la = le.getAttribute (attributeName);
			
				bool existing = false;
				if (la != null)
					existing = true; 

				LdapAttribute newla = new LdapAttribute (attributeName);		
				newla.addBase64Value (attributeValue);
				
				LdapModification lm;
			
				if (existing)
					lm = new LdapModification (LdapModification.REPLACE, newla);
				else
					lm = new LdapModification (LdapModification.ADD, newla);

				List<LdapModification> modList = new List<LdapModification> ();
				modList.Add (lm);
				
				Util.ModifyEntry (conn, currentDN, modList.ToArray());
				
				this.Show (conn, conn.Data.GetEntry (currentDN), displayAll);
			}
			
			fcd.Destroy();
		}

		void WriteBytesToFile (string fileName, byte[] fileBytes)
		{
			try {
							
				FileStream fs = File.OpenWrite (fileName);
				fs.Write (fileBytes, 0, fileBytes.Length);			
				fs.Close ();				
			
			} catch (Exception e) {
				Log.Debug (e);
				
				HIGMessageDialog dialog = new HIGMessageDialog (
					null,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Save binary attribute error",
					e.Message);

				dialog.Run ();
				dialog.Destroy ();				
			}
		}

		void OnSaveBinaryValueActivate (object o, EventArgs args)
		{
			FileChooserDialog fcd = new FileChooserDialog (
				Mono.Unix.Catalog.GetString ("Save binary as"),
				Gtk.Stock.Save, 
				null, 
				FileChooserAction.Save);

			fcd.AddButton (Gtk.Stock.Cancel, ResponseType.Cancel);
			fcd.AddButton (Gtk.Stock.Save, ResponseType.Ok);

			fcd.SelectMultiple = false;

			ResponseType response = (ResponseType) fcd.Run();
			if (response == ResponseType.Ok)  {			
				string attributeName = GetAttributeName ();
				LdapEntry le = conn.Data.GetEntry (currentDN);
				LdapAttribute la = le.getAttribute (attributeName);
				
				WriteBytesToFile (fcd.Filename, SupportClass.ToByteArray (la.ByteValue)); 
			}
			
			fcd.Destroy ();
		}

		void OnAddObjectClassActivate (object o, EventArgs args)
		{
			AddObjectClassDialog dlg = new AddObjectClassDialog (conn);
				
			foreach (string s in dlg.ObjectClasses) {
				string[] req = conn.Data.GetRequiredAttrs (s);
				store.AppendValues ("objectClass", s);

				if (req == null)
					continue;
				
				foreach (string r in req) {
					if (allAttrs.Contains (r))
						allAttrs.Remove (r);						

					string m = currentAttributes[r];
					if (m == null) {
						store.AppendValues (r, "");
						currentAttributes.Add (r, "");
					}
				}
			}
		}

		void DoPopUp()
		{
			Menu popup = new Menu();

			ImageMenuItem addBinaryValueItem = new ImageMenuItem ("Add binary value...");
			addBinaryValueItem.Image = new Gtk.Image (Stock.Open, IconSize.Menu);
			addBinaryValueItem.Activated += new EventHandler (OnAddBinaryValueActivate);
			addBinaryValueItem.Show ();
			popup.Append (addBinaryValueItem);

			ImageMenuItem newObjectClassItem = new ImageMenuItem ("Add object class(es)...");
			newObjectClassItem.Image = new Gtk.Image (Stock.Add, IconSize.Menu);
			newObjectClassItem.Activated += new EventHandler (OnAddObjectClassActivate);
			newObjectClassItem.Show ();
			popup.Append (newObjectClassItem);

			ImageMenuItem deleteItem = new ImageMenuItem ("Delete attribute");
			deleteItem.Image = new Gtk.Image (Stock.Delete, IconSize.Menu);
			deleteItem.Activated += new EventHandler (OnDeleteActivate);
			deleteItem.Show ();
			popup.Append (deleteItem);

			ImageMenuItem newItem = new ImageMenuItem ("Insert attribute");
			newItem.Image = new Gtk.Image (Stock.New, IconSize.Menu);
			newItem.Activated += new EventHandler (OnInsertActivate);			
			newItem.Show ();
			popup.Append (newItem);

			ImageMenuItem saveBinaryValueItem = new ImageMenuItem ("Save binary value...");
			saveBinaryValueItem.Image = new Gtk.Image (Stock.Save, IconSize.Menu);
			saveBinaryValueItem.Activated += new EventHandler (OnSaveBinaryValueActivate);
			saveBinaryValueItem.Show ();
			popup.Append (saveBinaryValueItem);

			popup.Popup(null, null, null, 3, Gtk.Global.CurrentEventTime);
		}
	}
	
}
