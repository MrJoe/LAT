// 
// lat - ViewsTreeView.cs
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
using Avahi;

namespace lat
{
	public class ServiceEventArgs : EventArgs
	{
		ConnectionProfile cp;

		public ServiceEventArgs (ConnectionProfile connectionProfile)
		{
			cp = connectionProfile;
		}

		public ConnectionProfile Profile
		{
			get { return cp; }
		}
	}

	public delegate void ServiceEventHandler (object o, ServiceEventArgs args);

	public class ServiceFinder
	{
		Client client;
		ServiceBrowser sb;

        public event ServiceEventHandler Found;

		public ServiceFinder ()
		{
		}

		public void Run ()
		{
			client = new Client();
			sb = new ServiceBrowser (client, "_ldap._tcp");
			sb.ServiceAdded += OnServiceAdded;
			sb.ServiceRemoved += OnServiceRemoved;		
		}

	    void OnServiceResolved (object o, ServiceInfoArgs args) 
		{
			ConnectionProfile cp = new ConnectionProfile ();
			cp.Name = String.Format ("{0} ({1})", args.Service.Name, args.Service.Address);
			cp.Host = args.Service.Address.ToString ();
			cp.Port = args.Service.Port;
			cp.User = "";
			cp.Pass = "";
			cp.DontSavePassword = false;
			cp.ServerType = "Generic LDAP server";
			cp.Dynamic = true;

			Logger.Log.Debug ("Found LDAP service {0} on {1} port {2}", 
				args.Service.Name, args.Service.Address, args.Service.Port);

			if (args.Service.Port == 636)
				cp.Encryption = EncryptionType.SSL;
			
			if (Found != null)
                Found (this, new ServiceEventArgs (cp));
		}

		void OnServiceAdded (object o, ServiceInfoArgs args) 
		{
			ServiceResolver resolver = new ServiceResolver (client, args.Service);
			resolver.Found += OnServiceResolved;
        }

		void OnServiceRemoved (object o, ServiceInfoArgs args)
		{
		}
	}
}