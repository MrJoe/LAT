// 
// lat - ProfileDialog.cs
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

namespace lat
{

	public class ProfileDialog
	{
		Glade.XML ui;

		[Glade.Widget] Gtk.Dialog profileDialog;
		[Glade.Widget] Gtk.Entry profileNameEntry;
		[Glade.Widget] Gtk.Entry hostEntry;
		[Glade.Widget] Gtk.Entry portEntry;
		[Glade.Widget] Gtk.Entry ldapBaseEntry;
		[Glade.Widget] Gtk.Entry userEntry;
		[Glade.Widget] Gtk.Entry passEntry;
		[Glade.Widget] Gtk.CheckButton savePasswordButton;
		[Glade.Widget] Gtk.RadioButton tlsRadioButton;
		[Glade.Widget] Gtk.RadioButton sslRadioButton;
		[Glade.Widget] Gtk.RadioButton noEncryptionRadioButton;
		[Glade.Widget] Gtk.HBox stHBox;
		[Glade.Widget] Gtk.Image image7;
		
		ComboBox serverTypeComboBox;
		EncryptionType encryption = EncryptionType.None;
		bool isEdit = false;
		string oldName = null;	

		public ProfileDialog ()
		{
			Init ();

			portEntry.Text = "389";

			profileDialog.Run ();
			profileDialog.Destroy ();
		}
		
		public ProfileDialog (ConnectionProfile cp)
		{
			Init ();
			
			oldName = cp.Name;

			profileNameEntry.Text = cp.Name;
			hostEntry.Text = cp.Host;
			portEntry.Text = cp.Port.ToString();
			ldapBaseEntry.Text = cp.LdapRoot;
			
			userEntry.Text = cp.User;

			if (cp.DontSavePassword)
				savePasswordButton.Active =  true;
			else
				passEntry.Text = cp.Pass;

			switch (cp.Encryption) {
	
			case EncryptionType.TLS:
				tlsRadioButton.Active = true;
				break;

			case EncryptionType.SSL:
				sslRadioButton.Active = true;
				break;

			case EncryptionType.None:
				noEncryptionRadioButton.Active = true;
				break;
			}
				
			comboSetActive (serverTypeComboBox, cp.ServerType.ToLower());

			isEdit = true;

			profileDialog.Run ();
			profileDialog.Destroy ();
		}
			
		void Init ()
		{
			ui = new Glade.XML (null, "lat.glade", "profileDialog", null);
			ui.Autoconnect (this);		

			Gdk.Pixbuf pb = Gdk.Pixbuf.LoadFromResource ("x-directory-remote-server-48x48.png");
			image7.Pixbuf = pb;
			
			createCombo ();

			noEncryptionRadioButton.Active = true;

			profileDialog.Icon = Global.latIcon;
		}	

		static void comboSetActive (ComboBox cb, string name)
		{		
			if (name.Equals ("generic ldap server"))
				cb.Active = 2;
			else if (name.Equals ("openldap"))
				cb.Active = 0;
			else if (name.Equals ("microsoft active directory"))
				cb.Active = 1;
		}

		void createCombo ()
		{
			serverTypeComboBox = ComboBox.NewText ();
			serverTypeComboBox.AppendText ("OpenLDAP");
			serverTypeComboBox.AppendText ("Microsoft Active Directory");
			serverTypeComboBox.AppendText ("Generic LDAP server");

			serverTypeComboBox.Active = 0;
			serverTypeComboBox.Show ();

			stHBox.PackStart (serverTypeComboBox, true, true, 5);
		}

		public void OnSavePasswordToggled (object obj, EventArgs args)
		{
			if (savePasswordButton.Active)
				passEntry.Sensitive = false;
			else
				passEntry.Sensitive = true;
		}

		public void OnEncryptionToggled (object obj, EventArgs args)
		{
			if (tlsRadioButton.Active) {
				portEntry.Text = "389";
				encryption = EncryptionType.TLS;
			} else if (sslRadioButton.Active) {
				portEntry.Text = "636";
				encryption = EncryptionType.SSL;
			} else {
				portEntry.Text = "389";
				encryption = EncryptionType.None;
			}
		}
		
		public void OnOkClicked (object o, EventArgs args)
		{
			TreeIter iter;
				
			if (!serverTypeComboBox.GetActiveIter (out iter))
				return;

			string st = (string) serverTypeComboBox.Model.GetValue (iter, 0);

			ConnectionProfile profile = new ConnectionProfile ();
			profile.Name = profileNameEntry.Text;
			profile.Host = hostEntry.Text;
			profile.Port = int.Parse (portEntry.Text);
			profile.LdapRoot = ldapBaseEntry.Text;
			profile.User = userEntry.Text;
			profile.Encryption = encryption;
			profile.DontSavePassword = savePasswordButton.Active;
			profile.ServerType = st;

			if (profile.DontSavePassword)
				profile.Pass = "";
			else
				profile.Pass = passEntry.Text;

			if (isEdit) {

				if (!oldName.Equals (profile.Name)) {

					Global.Profiles.Remove (oldName);
					Global.Profiles[profile.Name] = profile;

				} else {

					Global.Profiles[profile.Name] = profile;
				}

			} else {

				Global.Profiles[profile.Name] = profile;
			}
			
			Global.Profiles.SaveProfiles ();
		}
	}
}
