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
using GLib;
using Glade;
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
		[Glade.Widget] Gtk.RadioButton encryptionRadioButton;
		[Glade.Widget] Gtk.RadioButton noEncryptionRadioButton;
			
		[Glade.Widget] Gtk.Button okButton;
		[Glade.Widget] Gtk.Button cancelButton;

		private bool _useSSL = false;
		private bool _isEdit = false;
		private ProfileManager _pm;
	
		private string _oldName = null;

		public ProfileDialog (ProfileManager pm)
		{
			Init (pm);

			portEntry.Text = "389";

			profileDialog.Run ();
		}
		
		public ProfileDialog (ProfileManager pm, ConnectionProfile cp)
		{
			Init (pm);
			
			_oldName = cp.Name;

			profileNameEntry.Text = cp.Name;
			hostEntry.Text = cp.Host;
			portEntry.Text = cp.Port.ToString();
			ldapBaseEntry.Text = cp.LdapRoot;
			
			userEntry.Text = cp.User;
			passEntry.Text = cp.Pass;

			if (cp.SSL)
			{
				encryptionRadioButton.Active = true;
			}
				
			_isEdit = true;

			profileDialog.Run ();
		}
			
		private void Init (ProfileManager pm)
		{
			_pm = pm;
		
			ui = new Glade.XML (null, "lat.glade", "profileDialog", null);
			ui.Autoconnect (this);		

			noEncryptionRadioButton.Toggled += new EventHandler (OnEncryptionToggled);
			noEncryptionRadioButton.Active = true;
			
			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);	
		}	

		private void OnEncryptionToggled (object obj, EventArgs args)
		{
			if (encryptionRadioButton.Active)
			{
				_useSSL = true;
			}
			else
			{
				_useSSL = false;
			}
		}
			
		private void OnOkClicked (object o, EventArgs args)
		{
			ConnectionProfile profile = new ConnectionProfile (
					profileNameEntry.Text,
					hostEntry.Text,
					int.Parse (portEntry.Text),
					ldapBaseEntry.Text,
					userEntry.Text,
					passEntry.Text,
					_useSSL);
					
			if (_isEdit)
			{
				if (!_oldName.Equals (profile.Name))
				{
					_pm.deleteProfile (_oldName);
					_pm.addProfile (profile);
				}
				else
				{
					_pm.updateProfile (profile);
				}
			}
			else
			{
				_pm.addProfile (profile);
			}
			
			_pm.saveProfiles ();
			
			profileDialog.HideAll ();
		}
		
		private void OnCancelClicked (object o, EventArgs args)
		{
			profileDialog.HideAll ();		
		}
	}
}
