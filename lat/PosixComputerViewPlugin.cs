// 
// lat - PosixComputerViewPlugin.cs
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

	public class PosixComputerViewPlugin : ViewPlugin
	{
		public PosixComputerViewPlugin () : base ()
		{
		}
	
		public override void Init ()
		{
		}

		public override void OnAddEntry (LdapServer server)
		{
			new HostsViewDialog (server);
		}		

		public override void OnEditEntry (LdapServer server, LdapEntry le)
		{
			new HostsViewDialog (server, le);
		}
					
		public override void OnPopupShow (Menu popup)
		{
		}
					
		public override string[] Authors 
		{
			get {
				string[] cols = { "Loren Bandiera" };
				return cols;
			}
		}

		public override string[] ColumnAttributes 
		{
			get {
				string[] cols = { "cn", "ipHostNumber", "description" };
				return cols;
			}
		}

		public override string[] ColumnNames 
		{
			get {
				string[] cols = { "Hostname", "IP Address", "Description" };
				return cols;
			}
		}
		
		public override string Copyright 
		{ 
			get { return "MMG Security, Inc."; } 
		}
		
		public override string Description 
		{ 
			get { return "POSIX Computer View"; } 
		}

		public override string Filter 
		{ 
			get { return "(objectclass=ipHost)"; } 
		}
		
		public override string Name 
		{ 
			get { return "Computers"; } 
		}
		
		public override Gdk.Pixbuf Icon 
		{
			get { return Pixbuf.LoadFromResource ("x-directory-remote-workgroup.png"); }
		}
	}
}