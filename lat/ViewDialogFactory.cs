// 
// lat - ViewDialogFactory.cs
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
using Novell.Directory.Ldap;

namespace lat
{
	public class ViewDialogFactory
	{
		public ViewDialogFactory ()
		{
		}

		public static void Create (string name, LdapServer server, LdapEntry le)
		{
			switch (name) {

			case "openldapUsers":
				if (le == null)
					new NewUserViewDialog (server);
				else
					new EditUserViewDialog (server, le);
				break;

			case "adUsers":
				if (le != null)
					new adUserViewDialog (server, le);
				else
					new NewAdUserViewDialog (server);

				break;

			case "openldapGroups":
				if (le == null)
					new GroupsViewDialog (server);
				else
					new GroupsViewDialog (server, le);
				break;

			case "adGroups":
				if (le == null)
					new NewAdGroupViewDialog (server);
//				else
//					new adGroupViewDialog (server, le);
				break;
	
			case "openldapComputers":
				if (le == null)
					new HostsViewDialog (server);
				else
					new HostsViewDialog (server, le);
	
				break;
	
			case "adComputers":
				if (le == null)
					new NewAdComputerViewDialog (server);
				else
					new EditAdComputerViewDialog (server, le);
				break;

			case "openldapContacts":
			case "adContacts":
				if (le == null)
					new NewContactsViewDialog (server);
				else
					new EditContactsViewDialog (server, le);
				break;

			default:
				if (le == null)
					new DynamicViewDialog (server);
				else
					new DynamicViewDialog (server, le);
				break;
			}
		}
	}
}
