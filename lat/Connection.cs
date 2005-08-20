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
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class Connection
	{
		private string _host;
		private int _port;
		private string _ldapRoot = null;
		private string _user;
		private string _pass;
		private bool _ssl;
		private string _serverType = null;
		private LdapConnection _conn;
		private string _name;

		public Connection (string host, int port, string user, string pass, string baseDN, bool ssl, string serverType, string name)
		{
			_host = host;
			_port = port;
			_user = user;
			_pass = pass;
			_ldapRoot = baseDN;
			_ssl = ssl;
			_serverType = serverType;
			_name = name;
		}

		public void Bind ()
		{
			try 
			{
				_conn = new LdapConnection ();

				_conn.SecureSocketLayer = _ssl;	
				_conn.Connect (_host, _port);
				_conn.Bind (_user, _pass);
			}
			catch
			{
				Logger.Log.Debug ("Bind failed");
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

					if (entry != null)
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

		public ArrayList Search (string filter)
		{
			return Search (_ldapRoot, filter);
		}

		public ArrayList SearchByClass (string objectClass)
		{
			return Search (_ldapRoot, String.Format ("objectclass={0}", objectClass));
		}

		public void Add (string dn, ArrayList attributes)
		{
			Logger.Log.Debug ("START Connection.Add ()");
			Logger.Log.Debug ("dn: {0}", dn);

			LdapAttributeSet attributeSet = new LdapAttributeSet();

			foreach (LdapAttribute attr in attributes)
			{
				foreach (string v in attr.StringValueArray)
				{
					Logger.Log.Debug ("{0}:{1}", attr.Name, v);
				}
				
				attributeSet.Add (attr);
			}

			LdapEntry newEntry = new LdapEntry( dn, attributeSet );

			try 
			{
				_conn.Add (newEntry);

				Logger.Log.Debug ("END Connection.Add ()");
			} 
			catch 
			{
				throw;
			}		
		}
		
		public void Delete (string dn)
		{
			try 
			{
				_conn.Delete (dn);
			} 
			catch 
			{
				throw;
			}
		}

		public void Copy (string oldDN, string newRDN, string parentDN)
		{
			try
			{
				_conn.Rename (oldDN, newRDN, parentDN, false);
			}
			catch
			{
				throw;
			}
		}

		public void Move (string oldDN, string newRDN, string parentDN)
		{
			try
			{
				_conn.Rename (oldDN, newRDN, parentDN, true);
			}
			catch
			{
				throw;
			}
		}

		public void Rename (string oldDN, string newDN, bool saveOld)
		{
			try
			{
				_conn.Rename (oldDN, newDN, saveOld);
			}
			catch
			{
				throw;
			}
		}

		public void Modify (string dn, LdapModification[] mods)
		{
			try 
			{
				_conn.Modify (dn, mods);
			} 
			catch 
			{
				throw;
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

		public string[] getAllAttrs (string objClass)
		{
			try
			{
				LdapSchema schema;
				LdapObjectClassSchema ocs;
				
				ArrayList attrs = new ArrayList ();

				schema = _conn.FetchSchema ( _conn.GetSchemaDN() );	
				ocs = schema.getObjectClassSchema ( objClass );

				if (ocs.RequiredAttributes != null)
				{
					foreach (string r in ocs.RequiredAttributes)
					{
						if (!attrs.Contains (r))
							attrs.Add (r);
					}
				}

				if (ocs.OptionalAttributes != null)
				{
					foreach (string o in ocs.OptionalAttributes)
					{
						if (!attrs.Contains (o))
							attrs.Add (o);
					}
				}

				attrs.Sort ();

				return (string[]) attrs.ToArray (typeof (string));
			}
			catch
			{
				return null;
			}
		}

		public SchemaParser getAttrTypeSchema (string attrType)
		{
			if (!_conn.Connected)
				return null;

			string[] attrs = new string[] { "attributetypes" };

			LdapSearchQueue queue = _conn.Search ("cn=subschema",
						LdapConnection.SCOPE_BASE,
						"objectclass=*",
						attrs,
						false,
						(LdapSearchQueue) null,
						(LdapSearchConstraints) null );

			LdapMessage msg;

			while ((msg = queue.getResponse ()) != null)
			{		
				if (msg is LdapSearchResult)
				{			
					LdapEntry entry = ((LdapSearchResult) msg).Entry;
					LdapAttribute la = entry.getAttribute ("attributetypes");

					foreach (string s in la.StringValueArray)
					{
						SchemaParser sp = new SchemaParser (s);

						foreach (string a in sp.Names)
						{
							if (attrType.Equals (a))
								return sp;
						}
					}
				}
			}		
			
			return null;
		}

		public SchemaParser getObjClassSchema (string objClass)
		{
			if (!_conn.Connected)
				return null;

			string[] attrs = new string[] { "objectclasses" };

			LdapSearchQueue queue = _conn.Search ("cn=subschema",
						LdapConnection.SCOPE_BASE,
						"objectclass=*",
						attrs,
						false,
						(LdapSearchQueue) null,
						(LdapSearchConstraints) null );

			LdapMessage msg;

			while ((msg = queue.getResponse ()) != null)
			{		
				if (msg is LdapSearchResult)
				{			
					LdapEntry entry = ((LdapSearchResult) msg).Entry;
					LdapAttribute la = entry.getAttribute ("objectclasses");

					foreach (string s in la.StringValueArray)
					{
						SchemaParser sp = new SchemaParser (s);

						foreach (string a in sp.Names)
						{
							if (objClass.Equals (a))
								return sp;
						}
					}
				}
			}		
			
			return null;
		}

		public ArrayList getAttrTypes ()
		{
			try
			{
				ArrayList retVal = new ArrayList ();
				string[] attrs = new string[] { "attributetypes" };

				LdapSearchQueue queue = _conn.Search ("cn=subschema",
						LdapConnection.SCOPE_BASE,
						"objectclass=*",
						attrs,
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

		public ArrayList getObjClasses ()
		{
			try
			{
				ArrayList retVal = new ArrayList ();
				string[] attrs = new string[] { "objectclasses" };

				LdapSearchQueue queue = _conn.Search ("cn=subschema",
						LdapConnection.SCOPE_BASE,
						"objectclass=*",
						attrs,
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

		public void Bind (string user, string pass)
		{
			try 
			{
				_conn.Bind (user, pass);
			}
			catch
			{
				throw;
			}
		}

		public LdapSchema Schema
		{
			get { return _conn.FetchSchema ( _conn.GetSchemaDN() ); }
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

		public string ServerType
		{
			get { return _serverType; }
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
			get { return _ssl; }
		}

		public int Protocol
		{
			get { return _conn.ProtocolVersion; }
		}	

		public string Name
		{
			get { return _name; }
		}
	}
}
