// 
// lat - PosixUserViewPlugin.cs
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
using Gdk;
using Novell.Directory.Ldap;

namespace lat {

	public class PosixUserViewPlugin : ViewPlugin
	{
		public PosixUserViewPlugin () : base ()
		{
			config.ColumnAttributes =  new string[] { "uid", "cn" };
			config.ColumnNames = new string[] { "Username", "Real name" };
			config.Filter = "(&(objectclass=posixAccount)(objectclass=shadowAccount))";		
		}
	
		public override void Init ()
		{
		}

		public override void OnAddEntry (LdapServer server)
		{
			new NewUserViewDialog (server);
		}		

		public override void OnEditEntry (LdapServer server, LdapEntry le)
		{
			new EditUserViewDialog (server, le);
		}
					
		public override void OnPopupShow (Menu popup)
		{
			SeparatorMenuItem sm = new SeparatorMenuItem ();
			sm.Show ();
		
			popup.Append (sm);

			Gdk.Pixbuf pwdImage = Gdk.Pixbuf.LoadFromResource ("locked16x16.png");
			ImageMenuItem pwdItem = new ImageMenuItem ("Change password");
			pwdItem.Image = new Gtk.Image (pwdImage);
//			pwdItem.Activated += new EventHandler (OnPwdActivate);
			pwdItem.Show ();
			
			popup.Append (pwdItem);		
		}
					
//		void OnPwdActivate (object o, EventArgs args)
//		{
//			PasswordDialog pd = new PasswordDialog ();
//
//			if (pd.UnixPassword.Equals ("") || pd.UserResponse == ResponseType.Cancel)
//				return;
//
//			TreeModel model;
//			TreePath[] tp = tv.Selection.GetSelectedRows (out model);
//
//			foreach (TreePath path in tp) {
//				LdapEntry le = LookupEntry (path);
//				ChangePassword (le, pd);
//			}
//		}			
			
		public override string[] Authors 
		{
			get {
				string[] cols = { "Loren Bandiera" };
				return cols;
			}
		}
		
		public override string Copyright 
		{ 
			get { return "MMG Security, Inc."; } 
		}
		
		public override string Description 
		{ 
			get { return "POSIX User View"; } 
		}
		
		public override string Name 
		{ 
			get { return "Users"; } 
		}
		
		public override Gdk.Pixbuf Icon 
		{
			get { return Pixbuf.LoadFromResource ("stock_person.png"); }
		}
	}
}