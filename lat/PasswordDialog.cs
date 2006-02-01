// 
// lat - PasswordDialog.cs
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

using System;
using System.Security.Cryptography;
using System.Text;

using Gtk;
using Mono.Unix;

namespace lat 
{
	public class PasswordDialog
	{
		[Glade.Widget] Gtk.Dialog passwordDialog;
		[Glade.Widget] Gtk.Entry passwordEntry;
		[Glade.Widget] Gtk.Entry reenterEntry;
		[Glade.Widget] Gtk.RadioButton cryptRadioButton;
		[Glade.Widget] Gtk.RadioButton md5RadioButton;
		[Glade.Widget] Gtk.RadioButton shaRadioButton;
		[Glade.Widget] Gtk.CheckButton useSaltCheckButton;

		Glade.XML ui;

		private string _unix;
		private string _lm;
		private string _nt;

		private bool passwordsDontMatch = false;
		private ResponseType response;

		public PasswordDialog ()
		{
			ui = new Glade.XML (null, "lat.glade", "passwordDialog", null);
			ui.Autoconnect (this);

			// Use SSHA by default
			shaRadioButton.Active = true;
			useSaltCheckButton.Active = true;

			response = (ResponseType) passwordDialog.Run ();

			while (passwordsDontMatch) {
				passwordsDontMatch = false;
				response = (ResponseType) passwordDialog.Run ();
			}

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

			switch (algorithm) {

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

			byte[] buffer = enc.GetBytes (_unix);

			if (salted) {

				byte[] saltedBuffer = addSalt (hashAlgorithm, buffer);

				encText = "{S" + algorithm + "}";
            
				retVal = encText + Convert.ToBase64String (saltedBuffer);

			} else {

				hash = hashAlgorithm.ComputeHash(buffer);

				retVal = encText + Convert.ToBase64String (hash);
			}

			return retVal;
		}

		public static string generateUnixCrypt (string passwd)
		{
			// from crypt(3) manpage:
			//
			// salt is a two-character string chosen from the set [a-zA-Z0-9./]. 
			// This string is used to perturb the algorithm in one of 4096 different ways.

			string strSalt = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789./";
			char[] chrSalt = strSalt.ToCharArray ();

			string retVal = null;
			int c1, c2;

			Random random = new Random();
			c1 = random.Next (0, chrSalt.Length);
			c2 = random.Next (0, chrSalt.Length);

			string tmpSalt = String.Format ("{0}{1}", chrSalt[c1], chrSalt[c2]);
			string crypt = Mono.Unix.Native.Syscall.crypt (passwd, tmpSalt);

			retVal = "{CRYPT}" + crypt;

			return retVal;
		}

		private void GeneratePassword ()
		{	 
			if (md5RadioButton.Active) {
				_unix = doEncryption (passwordEntry.Text, "MD5",
					useSaltCheckButton.Active);
			} else if (shaRadioButton.Active) {
				_unix = doEncryption (passwordEntry.Text,
					"SHA", useSaltCheckButton.Active);
			} else if (cryptRadioButton.Active) {
				_unix = generateUnixCrypt (passwordEntry.Text);
			}


			SMBPassword smbpass = new SMBPassword (passwordEntry.Text);
			_lm = smbpass.LM;
			_nt = smbpass.NT;
		}

		public void OnEncryptionChanged (object o, EventArgs args)
		{
			if (cryptRadioButton.Active)
				useSaltCheckButton.Sensitive = false;
			else
				useSaltCheckButton.Sensitive = true;

			GeneratePassword ();
		}

		public void OnOkClicked (object o, EventArgs args)
		{
			if (passwordEntry.Text != reenterEntry.Text) {
				string msg = Mono.Unix.Catalog.GetString ("Password don't match");

				HIGMessageDialog dialog = new HIGMessageDialog (
					passwordDialog,
					0,
					Gtk.MessageType.Error,
					Gtk.ButtonsType.Ok,
					"Password error",
					msg);

				dialog.Run ();
				dialog.Destroy ();
			
				passwordsDontMatch = true;

				return;
			}

			GeneratePassword ();

			passwordDialog.HideAll ();
		}

		public void OnCancelClicked (object o, EventArgs args)
		{
			passwordDialog.HideAll ();
		}

		public ResponseType UserResponse
		{
			get { return response; }
		}

		public string UnixPassword 
		{
			get { return _unix; }
		}

		public string LMPassword
		{
			get { return _lm; }
		}

		public string NTPassword
		{
			get { return _nt; }
		}
	}
}
