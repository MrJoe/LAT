// 
// lat - ConnectionProfile.cs
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

namespace lat
{
	public struct ConnectionProfile 
	{
		public string Name;
		public string Host;
		public int Port;
		public string LdapRoot;
		public string User;
		public string Pass;
		public bool SSL;

		public ConnectionProfile (string name, string host, int port, string ldapRoot, string user, string pass, bool ssl)
		{
			Name = name;
			Host = host;
			Port = port;
			LdapRoot = ldapRoot;
			User = user;
			Pass = pass;
			SSL = ssl;
		}
	}
}
