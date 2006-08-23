// 
// lat - NewEntryDialog.cs
// Author: Loren Bandiera
// Copyright 2005-2006 MMG Security, Inc.
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

namespace lat
{
	public class NewEntryDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog newEntryDialog;
		[Glade.Widget] Gtk.HBox comboHBox;
		[Glade.Widget] Gtk.RadioButton templateRadioButton;
		[Glade.Widget] Gtk.RadioButton entryRadioButton;

		Connection conn;
		ComboBox templateComboBox;
		string _dn;

		public NewEntryDialog (Connection connection, string dn)
		{
			conn = connection;
			_dn = dn;

			ui = new Glade.XML (null, "lat.glade", "newEntryDialog", null);
			ui.Autoconnect (this);
			
			entryRadioButton.Label += String.Format ("\n({0})", _dn);

			createCombos ();

			newEntryDialog.Icon = Global.latIcon;
			newEntryDialog.Run ();
			newEntryDialog.Destroy ();
		}

		private void createCombos ()
		{
			templateComboBox = ComboBox.NewText ();
	
			string[] templates = Global.Templates.GetTemplateNames ();

			foreach (string s in templates)
				templateComboBox.AppendText (s);

			templateComboBox.Active = 0;
			templateComboBox.Show ();

			comboHBox.PackStart (templateComboBox, true, true, 0);
		}


		public void OnOkClicked (object o, EventArgs args)
		{
			if (templateRadioButton.Active) {

				TreeIter iter;
						
				if (!templateComboBox.GetActiveIter (out iter))
					return;

				string name = (string) templateComboBox.Model.GetValue (iter, 0);

				Template t = Global.Templates.Lookup (name);

				new CreateEntryDialog (conn, t);

			} else {

				if (_dn == conn.Settings.Host)
					return;

				new CreateEntryDialog (conn, conn.Data.GetEntry (_dn));
			}

			newEntryDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			newEntryDialog.HideAll ();
		}
	}
}
