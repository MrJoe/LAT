// 
// lat - PasswordDialog.cs
// Author: Loren Bandiera
// Copyright 2005 MMG Security, Inc.
//
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
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
using System.Security.Cryptography;
using System.Text;

namespace lat 
{
	public class PasswordDialog
	{
		[Glade.Widget] Gtk.Dialog passwordDialog;
		[Glade.Widget] Gtk.Entry passwordEntry;
		[Glade.Widget] Gtk.Entry outputEntry;
		[Glade.Widget] Gtk.RadioButton md5RadioButton;
		[Glade.Widget] Gtk.RadioButton shaRadioButton;
		[Glade.Widget] Gtk.CheckButton useSaltCheckButton;
		[Glade.Widget] Gtk.Button okButton;
		[Glade.Widget] Gtk.Button cancelButton;

		Glade.XML ui;

		private string _password;

		public PasswordDialog ()
		{
			ui = new Glade.XML (null, "lat.glade", "passwordDialog", null);
			ui.Autoconnect (this);

			outputEntry.Sensitive = false;

			passwordEntry.Changed += new EventHandler (OnPasswordChanged);

			useSaltCheckButton.Toggled += new EventHandler (OnEncryptionChanged);
			md5RadioButton.Toggled += new EventHandler (OnEncryptionChanged);
			shaRadioButton.Toggled += new EventHandler (OnEncryptionChanged);

			okButton.Clicked += new EventHandler (OnOkClicked);
			cancelButton.Clicked += new EventHandler (OnCancelClicked);

			passwordDialog.Run ();
			passwordDialog.Destroy ();
		}	

		private byte[] getSalt ()
		{
			byte[] retVal = new byte [8];

			RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();

			rng.GetNonZeroBytes (retVal); 

			return retVal;
		}

		private byte[] addSalt (HashAlgorithm hashAlgorithm, byte[] buffer)
		{
			// get salt
			byte[] saltBytes = getSalt ();

			// Create buffer for password + salt
		        byte[] bufferWithSaltBytes = 
       	   		     new byte[buffer.Length + saltBytes.Length];

			// Insert password
			for (int i = 0; i < buffer.Length; i++)
				bufferWithSaltBytes[i] = buffer[i];

			// Insert salt
			for (int i = 0; i < saltBytes.Length; i++)
				bufferWithSaltBytes[buffer.Length + i] = saltBytes[i];

			// Encrypt
			byte[] hashBytes = hashAlgorithm.ComputeHash (bufferWithSaltBytes);

			// Create byte array for encrypted hash + salt
			byte[] hashWithSaltBytes = new byte[hashBytes.Length + 
				saltBytes.Length];

			// Insert hashed password
			for (int i = 0; i < hashBytes.Length; i++)
				hashWithSaltBytes[i] = hashBytes[i];

			// Insert salt
			for (int i = 0; i < saltBytes.Length; i++)
				hashWithSaltBytes[hashBytes.Length + i] = saltBytes[i];

			return hashWithSaltBytes;
		}

		private string doEncryption (string input, string algorithm, bool salted)
		{
			string retVal = "";

			if (input.Equals (""))
			{
				return retVal;
			}

			HashAlgorithm hashAlgorithm = null;
			byte[] hash = null;
			string encText = null;

			switch (algorithm)
			{
				case "MD5":
					hashAlgorithm = new MD5CryptoServiceProvider();
					encText = "{MD5}";
					break;

				case "SHA":
					hashAlgorithm = new SHA1Managed();
					encText = "{SHA}";
					break;
			}

			ASCIIEncoding enc = new ASCIIEncoding();

			byte[] buffer = enc.GetBytes (_password);

			if (salted)
			{
				byte[] saltedBuffer = addSalt (hashAlgorithm, buffer);

				encText = "{S" + algorithm + "}";
            
				retVal = encText + Convert.ToBase64String (saltedBuffer);
			}
			else
			{
				hash = hashAlgorithm.ComputeHash(buffer);

				retVal = encText + Convert.ToBase64String (hash);
			}

			return retVal;
		}

		private void updateOutput ()
		{
			_password = passwordEntry.Text;
			bool salt = false;

			if (useSaltCheckButton.Active)
				salt = true;
	
			if (md5RadioButton.Active)
			{
				outputEntry.Text = doEncryption (_password, "MD5", salt);
			}
			else if (shaRadioButton.Active)
			{
				outputEntry.Text = doEncryption (_password, "SHA", salt);
			}
		}

		private void OnEncryptionChanged (object o, EventArgs args)
		{
			updateOutput ();
		}

		private void OnPasswordChanged (object o, EventArgs args)
		{
			updateOutput ();
		}

		private void OnOkClicked (object o, EventArgs args)
		{
			passwordDialog.HideAll ();
		}

		private void OnCancelClicked (object o, EventArgs args)
		{
			passwordDialog.HideAll ();
		}

		private void OnDlgDelete (object o, DeleteEventArgs args)
		{
			passwordDialog.HideAll ();
		}

		public string Password 
		{
			get { return outputEntry.Text; }
		}
	}
}
