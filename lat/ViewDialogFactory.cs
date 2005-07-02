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

		public static void Create (string name, lat.Connection cn, LdapEntry le)
		{
			switch (name)
			{
				case "Users":
				{
					if (cn.ServerType.ToLower() == "microsoft active directory")
					{
						if (le == null)
							new adUserViewDialog (cn);
						else
							new adUserViewDialog (cn, le);
					}
					else
					{
						if (le == null)
							new UsersViewDialog (cn);
						else
							new UsersViewDialog (cn, le);
					}

					break;
				}

				case "Groups":
					if (le == null)
						new GroupsViewDialog (cn);
					else
						new GroupsViewDialog (cn, le);

					break;
				
				case "Hosts":
				{
					if (cn.ServerType.ToLower() == "microsoft active directory")
					{
						if (le == null)
							new adComputerViewDialog (cn);
						else
							new adComputerViewDialog (cn, le);
					}
					else
					{
						if (le == null)
							new HostsViewDialog (cn);
						else
							new HostsViewDialog (cn, le);
					}				
					break;
				}

				case "Contacts":
					if (le == null)
						new ContactsViewDialog (cn);
					else
						new ContactsViewDialog (cn, le);

					break;

				default:
					if (le == null)
						new DynamicViewDialog (cn);
					else
						new DynamicViewDialog (cn, le);

					break;
			}
		}
	}
}
