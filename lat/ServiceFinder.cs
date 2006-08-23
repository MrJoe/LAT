// 
// lat - ServiceFinder.cs
// Author: Loren Bandiera
// Copyright 2005-2006 MMG Security, Inc.
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
		Connection conn;

		public ServiceEventArgs (Connection conn)
		{
			this.conn = conn;
		}

		public Connection FoundConnection
		{
			get { return conn; }
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
			client = new Client();			
		}

		public void Start ()
		{
			try {				
				sb = new ServiceBrowser (client, "_ldap._tcp");
				sb.ServiceAdded += OnServiceAdded;
				sb.ServiceRemoved += OnServiceRemoved;
			} catch (ClientException ce) {
				Log.Debug (ce);
			}
		}
		
		public void Stop ()
		{
			sb.Dispose ();
		}

	    void OnServiceResolved (object o, ServiceInfoArgs args) 
		{
			(o as ServiceResolver).Dispose ();
		
			ConnectionData cd = new ConnectionData ();
			cd.Name = String.Format ("{0} ({1})", args.Service.Name, args.Service.Address);
			cd.Host = args.Service.Address.ToString ();
			cd.Port = args.Service.Port;
			cd.UserName = "";
			cd.Pass = "";
			cd.SavePassword = false;
			cd.ServerType = Util.GetServerType ("Generic LDAP server");
			cd.Dynamic = true;

			Log.Debug ("Found LDAP service {0} on {1} port {2}", 
				args.Service.Name, args.Service.Address, args.Service.Port);

			if (args.Service.Port == 636)
				cd.Encryption = EncryptionType.SSL;
			
			Connection conn = new Connection (cd);
			
			if (Found != null)
                Found (this, new ServiceEventArgs (conn));
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
