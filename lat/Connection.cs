// 
// lat - Connection.cs
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
using System.Collections;
using Novell.Directory.Ldap;

namespace lat
{
	public class Connection
	{
		private string _host;
		private int _port;
		private string _ldapRoot = null;
		private string _user;
		private string _pass;
//		private bool _ssl;
		private LdapConnection _conn;

		public Connection (string host, int port, string user, string pass, string baseDN, bool ssl)
		{
			_host = host;
			_port = port;
			_user = user;
			_pass = pass;
			_ldapRoot = baseDN;
//			_ssl = ssl;

			try 
			{
				_conn = new LdapConnection ();

	//			_conn.SecureSocketLayer = _ssl;	
				_conn.Connect (_host, _port);
				_conn.Bind (_user, _pass);
			}
			catch
			{
			}
		}

		public LdapEntry getEntry (string dn)
		{
			if (!_conn.Connected)
				return null;

			LdapSearchQueue queue = _conn.Search (dn,
						LdapConnection.SCOPE_BASE,
						"objectclass=*",
						null,
						false,
						(LdapSearchQueue) null,
						(LdapSearchConstraints) null );

			LdapMessage msg;

			while ((msg = queue.getResponse ()) != null)
			{		
				if (msg is LdapSearchResult)
				{			
					LdapEntry entry = ((LdapSearchResult) msg).Entry;
					return entry;
				}
			}		
			
			return null;
		}

		public ArrayList getChildren (string searchBase)
		{
			if (!_conn.Connected)
				return null;

			ArrayList retVal = new ArrayList ();
			
			LdapSearchQueue queue = _conn.Search (searchBase,
						LdapConnection.SCOPE_ONE,
						"objectclass=*",
						null,
						false,
						(LdapSearchQueue) null,
						(LdapSearchConstraints) null );

			LdapMessage msg;

			while ((msg = queue.getResponse ()) != null)
			{
			
				if (msg is LdapSearchResult)
				{
				
					LdapEntry entry = ((LdapSearchResult) msg).Entry;
					retVal.Add (entry);
				}
			}
			
			return retVal;
		}

		public ArrayList Search (string searchBase, string filter)
		{
			if (!_conn.Connected)
				return null;

			try 
			{
				ArrayList retVal = new ArrayList ();

				LdapSearchQueue queue = _conn.Search (searchBase,
						LdapConnection.SCOPE_SUB,
						filter,
						null,
						false,
						(LdapSearchQueue) null,
						(LdapSearchConstraints) null );

				LdapMessage msg;

				while ((msg = queue.getResponse ()) != null)
				{
			
					if (msg is LdapSearchResult)
					{
						LdapEntry entry = ((LdapSearchResult) msg).Entry;
						retVal.Add (entry);
					}
				}

				return retVal;
			}
			catch 
			{
				return null;
			}
		}

		public ArrayList SearchByClass (string objectClass)
		{
			return Search (_ldapRoot, String.Format ("objectclass={0}", objectClass));
		}

		public bool Add (string dn, ArrayList attributes)
		{
			if (!_conn.Connected)
				return false;

			LdapAttributeSet attributeSet = new LdapAttributeSet();

			foreach (LdapAttribute attr in attributes)
			{
				attributeSet.Add (attr);
			}

			LdapEntry newEntry = new LdapEntry( dn, attributeSet );

			try 
			{
				_conn.Add (newEntry);
				return true;
			} 
			catch 
			{
				return false;
			}		
		}
		
		public bool Delete (string dn)
		{
			if (!_conn.Connected)
				return false;

			try 
			{
				_conn.Delete (dn);
				return true;
			} 
			catch 
			{
				return false;
			}
		}

		public bool Move (string oldDN, string newRDN, string parentDN)
		{
			if (!_conn.Connected)
				return false;

			try
			{
				_conn.Rename (oldDN, newRDN, parentDN, true);
				return true;
			}
			catch (LdapException e)
			{
Console.WriteLine ("Move error: {0} - RC: {1}", e.LdapErrorMessage, e.ResultCode);
				return false;
			}
		}

		public bool Rename (string oldDN, string newDN, bool saveOld)
		{
			if (!_conn.Connected)
				return false;

			try
			{
				_conn.Rename (oldDN, newDN, saveOld);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public bool Modify (string dn, LdapModification[] mods)
		{
			if (!_conn.Connected)
				return false;

			try 
			{
				_conn.Modify (dn, mods);
				return true;
			} 
			catch 
			{
				return false;
			}		
		}

		public string[] getRequiredAttrs (string[] objClasses)
		{
			if (!_conn.Connected || objClasses == null)
				return null;

			ArrayList retVal = new ArrayList ();
			Hashtable retHash = new Hashtable ();

			LdapSchema schema;
			schema = _conn.FetchSchema ( _conn.GetSchemaDN() );

			foreach (string oc in objClasses)
			{
				LdapObjectClassSchema ocs;

				ocs = schema.getObjectClassSchema ( oc );

				foreach (string c in ocs.RequiredAttributes)
				{
					if (!retHash.ContainsKey (c))
						retHash.Add (c, c);
				}
			}

			foreach (string key in retHash.Keys)
			{
				retVal.Add (key);
			}

			return (string[]) retVal.ToArray (typeof (string));
		}

		public string[] getRequiredAttrs (string objClass)
		{
			if (!_conn.Connected || objClass == null)
				return null;
			
			LdapSchema schema;
			LdapObjectClassSchema ocs;

			schema = _conn.FetchSchema ( _conn.GetSchemaDN() );
			ocs = schema.getObjectClassSchema ( objClass );

			if (ocs != null)
			{
				return ocs.RequiredAttributes;
			}

			return null;
		}

		public void Disconnect ()
		{
			_conn.Disconnect ();
		}

		public bool Bind (string user, string pass)
		{
			try 
			{
				_conn.Bind (user, pass);
				return true;
			}
			catch
			{
				return false;
			}
		}

		public string Host
		{
			get { return _host; }
		}
		
		public int Port
		{
			get { return _port; }
		}

		public string LdapRoot
		{
			get { return _ldapRoot; }
		}

		public string User
		{
			get { return _user; }
		}

		public string AuthDN
		{
			get { return _conn.AuthenticationDN; }
		}

		public bool IsBound
		{
			get { return _conn.Bound; }
		}

		public bool IsConnected
		{
			get { return _conn.Connected; }
		}

		public bool UseSSL
		{
			get { return false; }
	//		get { return _conn.SecureSocketLayer; }
		}

		public int Protocol
		{
			get { return _conn.ProtocolVersion; }
		}	
	}
}
