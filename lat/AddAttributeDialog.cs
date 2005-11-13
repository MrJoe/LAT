// 
// lat - AddAttributeDialog.cs
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
using GLib;
using Glade;
using System;
using System.Collections;
using Novell.Directory.Ldap;

namespace lat
{
	public class AddAttributeDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog addAttributeDialog;
		[Glade.Widget] Gtk.HBox attrNameHBox;
		[Glade.Widget] Gtk.HBox attrClassHBox;
		[Glade.Widget] Gtk.Entry attrValueEntry;
		[Glade.Widget] Gtk.Button cancelButton;
		[Glade.Widget] Gtk.Button okButton;

		private LdapServer server;

		private string _dn;
		private string _name = null;
		private string _value = null;

		private ComboBox attrClassComboBox;
		private static ComboBox attrNameComboBox;

		public AddAttributeDialog (LdapServer ldapServer, string dn)
		{
			server = ldapServer;
			_dn = dn;

			ui = new Glade.XML (null, "lat.glade", "addAttributeDialog", null);
			ui.Autoconnect (this);
			
			createCombos ();

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			addAttributeDialog.DeleteEvent += new DeleteEventHandler (OnDlgDelete);

			addAttributeDialog.Run ();
			addAttributeDialog.Destroy ();
		}

		private void createCombos ()
		{
			try
			{
				// class
				LdapEntry le = server.GetEntry (_dn);
				LdapAttribute la = le.getAttribute ("objectClass");

				attrClassComboBox = ComboBox.NewText ();

				foreach (string s in la.StringValueArray)
				{
					if (!s.Equals ("top"))
						attrClassComboBox.AppendText (s);
				}

				attrClassComboBox.Changed += new EventHandler (OnClassChanged);

				attrClassComboBox.Active = 0;
				attrClassComboBox.Show ();

				attrClassHBox.PackStart (attrClassComboBox, true, true, 5);

				// name
				attrNameComboBox = ComboBox.NewText ();
				attrNameComboBox.AppendText ("(none)");
				attrNameComboBox.Active = 0;
				attrNameComboBox.Show ();

				attrNameHBox.PackStart (attrNameComboBox, true, true, 5);
			}
			catch {}
		}

		private void OnClassChanged (object o, EventArgs args)
		{
			TreeIter iter;
				
			if (!attrClassComboBox.GetActiveIter (out iter))
				return;

			string objClass = (string) attrClassComboBox.Model.GetValue (iter, 0);

			string [] attrs = server.GetAllAttributes (objClass);

			if (attrNameComboBox == null)
			{
				// don't know why this happens

				return;
			}
			else
			{
// FIXME: causes list to go blank
//				attrNameComboBox.Clear ();

				foreach (string s in attrs)
				{				
					attrNameComboBox.AppendText (s);
				}
			}		
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			TreeIter iter;
				
			if (!attrNameComboBox.GetActiveIter (out iter))
				return;

			string attrName = (string) attrNameComboBox.Model.GetValue (iter, 0);

			if (attrName.Equals ("(none)"))
			{
				// add object class
				TreeIter cnIter;
					
				if (!attrClassComboBox.GetActiveIter (out cnIter))
					return;

				string objClass = (string) attrClassComboBox.Model.GetValue (cnIter, 0);

				_name = "objectClass";
				_value = objClass;
			}
			else
			{
				_name = attrName;
				_value = attrValueEntry.Text;
			}

			addAttributeDialog.HideAll ();
		}

		private void OnCancelClicked (object o, EventArgs args)
		{
			addAttributeDialog.HideAll ();
		}

		private void OnDlgDelete (object o, DeleteEventArgs args)
		{
			addAttributeDialog.HideAll ();
		}

		public string Name
		{
			get { return _name; }
		}

		public string Value
		{
			get { return _value; }
		}
	}
}
