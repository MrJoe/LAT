// 
// lat - LdapServer.cs
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
using Syscert = System.Security.Cryptography.X509Certificates;
using Mono.Security.X509;
using Mono.Security.Cryptography;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat {

	public sealed class LdapServer
	{
		private string 		host;
		private int		port;
		private string		rootDN;
		private string		sType;
		private LdapConnection	conn;

		public LdapServer (string hostName, int hostPort, string dirRoot, 
				   string serverType)
		{
			host = hostName;
			port = hostPort;
			rootDN = dirRoot;
			sType = serverType;
		}

		#region methods

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

			conn.Add (newEntry);

			Logger.Log.Debug ("END Connection.Add ()");
		}

		public void Bind (string userName, string userPass)
		{
			conn.Bind (userName, userPass);
		}

		public void Connect (bool useSSL)
		{
			conn = new LdapConnection ();
			conn.SecureSocketLayer = useSSL;
			conn.UserDefinedServerCertValidationDelegate += new 
				CertificateValidationCallback(SSLHandler);

			conn.Connect (host, port);

			Logger.Log.Debug ("Connected to '{0}' on port {1}", host, port);
			Logger.Log.Debug ("Using SSL: {0}", useSSL);
		}

		public void Copy (string oldDN, string newRDN, string parentDN)
		{
			conn.Rename (oldDN, newRDN, parentDN, false);
		}

		public void Delete (string dn)
		{
			conn.Delete (dn);
		}

		public void Disconnect ()
		{
			conn.Disconnect ();
			conn = null;

			Logger.Log.Debug ("Disconnected from '{0}'", host);
		}

		public void GetAllAttributes (ArrayList objClass, 
					 out string[] required, out string[] optional)
		{
			try
			{
				LdapSchema schema;
				LdapObjectClassSchema ocs;
				
				ArrayList r_attrs = new ArrayList ();
				ArrayList o_attrs = new ArrayList ();

				schema = conn.FetchSchema ( conn.GetSchemaDN() );
		
				foreach (string c in objClass)
				{
					ocs = schema.getObjectClassSchema ( c );

					if (ocs.RequiredAttributes != null)
					{
						foreach (string r in ocs.RequiredAttributes)
						{
							if (!r_attrs.Contains (r))
								r_attrs.Add (r);
						}
					}

					if (ocs.OptionalAttributes != null)
					{
						foreach (string o in ocs.OptionalAttributes)
						{
							if (!o_attrs.Contains (o))
								o_attrs.Add (o);
						}
					}


				}

				required = (string[]) r_attrs.ToArray (typeof (string));
				optional = (string[]) o_attrs.ToArray (typeof (string));
			}
			catch (Exception e)
			{
				required = null;
				optional = null;

				Logger.Log.Debug ("getAllAttrs: {0}", e.Message);
			}
		}

		public string[] GetAllAttributes (string objClass)
		{
			try
			{
				LdapSchema schema;
				LdapObjectClassSchema ocs;
				
				ArrayList attrs = new ArrayList ();

				schema = conn.FetchSchema ( conn.GetSchemaDN() );	
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

		public LdapEntry[] GetAttributeTypes ()
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "attributetypes" };

			return Search ("cn=subschema", 
				LdapConnection.SCOPE_BASE,
	 			"objectclass=*", attrs);
		}

		public SchemaParser GetAttributeTypeSchema (string attrType)
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "attributetypes" };

			LdapEntry[] entries = Search ("cn=subschema", 
				LdapConnection.SCOPE_BASE,
	 			"objectclass=*", attrs);

			foreach (LdapEntry entry in entries)
			{			
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
			
			return null;
		}

		public string GetAttributeValueFromEntry (LdapEntry le, string attr)
		{
			LdapAttribute la = le.getAttribute (attr);

			if (la != null)
			{
				return la.StringValue;
			}

			return "";
		}

		public string[] GetAttributeValuesFromEntry (LdapEntry le, string[] attrs)
		{
			if (le == null || attrs == null)
				throw new ArgumentNullException ();

			ArrayList retVal = new ArrayList ();

			foreach (string n in attrs)
			{
				LdapAttribute la = le.getAttribute (n);

				if (la != null)
					retVal.Add (la.StringValue);
				else
					retVal.Add ("");
			}

			return (string[]) retVal.ToArray (typeof (string));
		}

		public void GetAttributeValuesFromEntry (LdapEntry le, string[] attrs, 
							 out Hashtable entryInfo)
		{
			if (le == null || attrs == null)
				throw new ArgumentNullException ();

			entryInfo = new Hashtable ();

			foreach (string n in attrs)
			{
				LdapAttribute la = le.getAttribute (n);

				if (la != null)
					entryInfo.Add (n, la.StringValue);
				else
					entryInfo.Add (n, "");
			}
		}

		public LdapEntry GetEntry (string dn)
		{
			if (!conn.Connected)
				return null;

			LdapEntry[] entry = Search (dn, LdapConnection.SCOPE_BASE,
						    "objectclass=*", null);

			if (entry.Length > 0)
			{
				return entry[0];
			}
		
			return null;
		}

		public LdapEntry[] GetEntryChildren (string entryDN)
		{
			if (!conn.Connected)
				return null;

			return Search (entryDN, LdapConnection.SCOPE_ONE,
					    "objectclass=*", null);
		}

		public string GetLocalSID ()
		{
			LdapEntry[] sid = Search (rootDN, LdapConnection.SCOPE_ONE,
						    "objectclass=sambaDomain", null);

			if (sid.Length > 0)
			{
				LdapAttribute a = sid[0].getAttribute ("sambaSID");
				return a.StringValue;
			}

			return null;			
		}

		public int GetNextGID ()
		{
			ArrayList gids = new ArrayList ();

			LdapEntry[] groups = Search (rootDN, LdapConnection.SCOPE_SUB,
						    "gidNumber=*", null);

			foreach (LdapEntry entry in groups)
			{			
				LdapAttribute a = entry.getAttribute ("gidNumber");
				gids.Add (int.Parse(a.StringValue));
			}

			gids.Sort ();
			if (gids.Count == 0)
			{
				return 1000;
			}
			else
			{
				return (int) (gids [gids.Count - 1]) + 1;
			}			
		}

		public int GetNextUID ()
		{
			ArrayList uids = new ArrayList ();

			LdapEntry[] users = Search (rootDN, LdapConnection.SCOPE_SUB,
						    "uidNumber=*", null);

			foreach (LdapEntry entry in users)
			{			
				LdapAttribute a = entry.getAttribute ("uidNumber");
				uids.Add (int.Parse(a.StringValue));
			}

			uids.Sort ();
			if (uids.Count == 0)
			{
				return 1000;
			}
			else
			{
				return (int) (uids [uids.Count - 1]) + 1;
			}
		}

		public LdapEntry[] GetObjectClasses ()
		{
			string[] attrs = new string[] { "objectclasses" };

			return Search ("cn=subschema", LdapConnection.SCOPE_BASE, 
				       "objectclass=*", attrs);
		}

		public SchemaParser GetObjectClassSchema (string objClass)
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "objectclasses" };

			LdapEntry[] entries = Search ("cn=subschema", 
				LdapConnection.SCOPE_BASE,
	 			"objectclass=*", attrs);

			foreach (LdapEntry entry in entries)
			{			
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
			
			return null;
		}

		public string[] GetRequiredAttrs (string objClass)
		{
			if (!conn.Connected || objClass == null)
				return null;
			
			LdapSchema schema;
			LdapObjectClassSchema ocs;

			schema = conn.FetchSchema ( conn.GetSchemaDN() );
			ocs = schema.getObjectClassSchema ( objClass );

			if (ocs != null)
			{
				return ocs.RequiredAttributes;
			}

			return null;
		}

		public string[] GetRequiredAttrs (string[] objClasses)
		{
			if (!conn.Connected || objClasses == null)
				return null;

			ArrayList retVal = new ArrayList ();
			Hashtable retHash = new Hashtable ();

			LdapSchema schema;
			schema = conn.FetchSchema ( conn.GetSchemaDN() );

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

		public void Modify (string dn, LdapModification[] mods)
		{
			conn.Modify (dn, mods);
		}

		public void Move (string oldDN, string newRDN, string parentDN)
		{
			conn.Rename (oldDN, newRDN, parentDN, true);
		}

		public void Rename (string oldDN, string newDN, bool saveOld)
		{
			conn.Rename (oldDN, newDN, saveOld);
		}

		public LdapEntry[] Search (string searchFilter)
		{
			return Search (rootDN, LdapConnection.SCOPE_SUB, searchFilter, null);
		}

		public LdapEntry[] Search (string searchBase, string searchFilter)
		{
			return Search (searchBase, LdapConnection.SCOPE_SUB, 
				       searchFilter, null);	
		}

		public LdapEntry[] Search (string searchBase, int searchScope, 
					   string searchFilter, string[] searchAttrs)
		{	
			if (!conn.Connected)
				return null;

			try
			{
				ArrayList retVal = new ArrayList ();

				LdapSearchQueue queue = conn.Search (searchBase,
						searchScope,
						searchFilter,
						searchAttrs,
						false,
						(LdapSearchQueue) null,
						(LdapSearchConstraints) null);

				LdapMessage msg;

				while ((msg = queue.getResponse ()) != null)
				{
					if (msg is LdapSearchResult)
					{
						LdapEntry entry = ((LdapSearchResult) msg).Entry;
						retVal.Add (entry);
					}
				}

				return (LdapEntry[]) retVal.ToArray (typeof (LdapEntry));
			}
			catch (Exception e)
			{
				Logger.Log.Debug ("LdapSearch.Search error: {0}", e.Message);
				return null;
			}
		}

		public LdapEntry[] SearchByClass (string objectClass)
		{
			return Search (rootDN, String.Format ("objectclass={0}", objectClass));
		}

		#endregion

		#region internal_methods

		internal static bool SSLHandler (Syscert.X509Certificate certificate,
						int[] certificateErrors)
		{
			X509Store store = null;
			X509Stores stores = X509StoreManager.CurrentUser;
			store = stores.TrustedRoot;
			bool retVal = true;

			//Import the details of the certificate from the server.
			X509Certificate x509 = null;
			X509CertificateCollection coll = new X509CertificateCollection ();
			byte[] data = certificate.GetRawCertData();
			if (data != null)			
				x509 = new X509Certificate (data);

			string msg = String.Format (" {0}X.509 v{1} Certificate", 
				(x509.IsSelfSigned ? "Self-signed " : String.Empty), 
				x509.Version);

			msg += "\nSerial Number: " + CryptoConvert.ToHex (x509.SerialNumber);
			msg += "\nIssuer Name:   " + x509.IssuerName;
			msg += "\nSubject Name:  " + x509.SubjectName;
			msg += "\nValid From:    " + x509.ValidFrom;
			msg += "\nValid Until:   " + x509.ValidUntil;
			msg += "\nUnique Hash:   " + CryptoConvert.ToHex (x509.Hash);

			CertificateDialog cd = new CertificateDialog (msg);

			Logger.Log.Debug ("CertificateDialog.UserResponse: {0}", cd.UserResponse);

			if (cd.UserResponse == CertDialogResponse.Import) 
			{
				if (x509 != null)
					coll.Add (x509);

				store.Import (x509);

				Logger.Log.Debug ("Certificate successfully imported.");
			} 
			else if (cd.UserResponse == CertDialogResponse.Cancel)
			{
				retVal = false;
			}

			return retVal;
		}

		#endregion

		#region properties

		public string AuthDN
		{
			get { return conn.AuthenticationDN; }
		}

		public bool Bound
		{
			get { return conn.Bound; }
		}

		public bool Connected
		{
			get { return conn.Connected; }
		}

		public string DirectoryRoot
		{
			get { return rootDN; }
		}

		public string Host
		{
			get { return host; }
		}

		public int Port
		{
			get { return port; }
		}

		public int Protocol
		{
			get { return conn.ProtocolVersion; }
		}

		public string ServerType
		{
			get { return sType; }
		}

		public bool UseSSL
		{
			get { return conn.SecureSocketLayer; }
		}

		#endregion
	}
}
