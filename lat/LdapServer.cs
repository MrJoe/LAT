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

	/// <summary>The main class that encapsulates the connection
	/// to a directory server through the Ldap protocol.
	/// </summary>
	public class LdapServer
	{
		private string 		host;
		private int		port;
		private string		rootDN;
		private bool		findRootDN;
		private string		sType;
		private LdapConnection	conn;

		public LdapServer (string hostName, int hostPort, string serverType)
		{
			host = hostName;
			port = hostPort;
			sType = serverType;
			findRootDN = true;
		}

		public LdapServer (string hostName, int hostPort, string dirRoot, 
				   string serverType)
		{
			host = hostName;
			port = hostPort;
			rootDN = dirRoot;
			sType = serverType;
		}

		#region methods

		/// <summary>Adds an entry to the directory
		/// 
		/// </summary>
		/// <param name="dn">The distinguished name of the new entry.</param>
		/// <param name="attributes">An arraylist of string attributes for the 
		/// new ldap entry.</param>
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

		/// <summary>Binds to the directory server with the given user
		/// name and password.
		/// </summary>
		/// <param name="userName">Username</param>
		/// <param name="userPass">Password</param> 
		public void Bind (string userName, string userPass)
		{
			conn.Bind (userName, userPass);
		}

		/// <summary>Connects to the directory server.
		/// </summary>
		/// <param name="useSSL">Use SSL/TLS to encrypt session</param>
		public void Connect (bool useSSL)
		{
			conn = new LdapConnection ();
			conn.SecureSocketLayer = useSSL;
			conn.UserDefinedServerCertValidationDelegate += new 
				CertificateValidationCallback(SSLHandler);

			conn.Connect (host, port);

			if (findRootDN)
				rootDN = GetRootDN ();

			Logger.Log.Debug ("Connected to '{0}' on port {1}", host, port);
			Logger.Log.Debug ("Base: {0}", rootDN);
			Logger.Log.Debug ("Using SSL: {0}", useSSL);
		}

		/// <summary>Copy a directory entry
		/// </summary>
		/// <param name="oldDN">Distinguished name of the entry to copy</param>
		/// <param name="newRDN">New name for entry</param>
		/// <param name="parentDN">Parent name</param>
		public void Copy (string oldDN, string newRDN, string parentDN)
		{
			conn.Rename (oldDN, newRDN, parentDN, false);
		}

		/// <summary>Deletes a directory entry
		/// </summary>
		/// <param name="dn">Distinguished name of the entry to delete</param>
		public void Delete (string dn)
		{
			conn.Delete (dn);
		}

		/// <summary>Disconnects from a directory server
		/// </summary>
		public void Disconnect ()
		{
			conn.Disconnect ();
			conn = null;

			Logger.Log.Debug ("Disconnected from '{0}'", host);
		}

		/// <summary>Gets a list of required and optional attributes for
		/// the given object classes.
		/// </summary>
		/// <param name="objClass">List of object classes</param>
		/// <param name="required">Required attributes</param>
		/// <param name="optional">Optional attributes</param>
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

		/// <summary>Gets a list of all attributes for the given object class
		/// </summary>
		/// <param name="objClass">Name of object class</param>
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

		/// <summary>Gets a list of attribute types supported on the
		/// directory.
		/// </summary>
		/// <returns>An array of LdapEntry objects</returns>
		public LdapEntry[] GetAttributeTypes ()
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "attributetypes" };

			return Search ("cn=subschema", 
				LdapConnection.SCOPE_BASE,
	 			"objectclass=*", attrs);
		}

		/// <summary>Gets the schema for a given attribute type
		/// </summary>
		/// <param name="attrType">Attribute type</param>
		/// <returns>A SchemaParser object</returns>
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

		/// <summary>Gets the value of an attribute for the given
		/// entry.
		/// </summary>
		/// <param name="le">LdapEntry</param>
		/// <param name="attr">Attribute to lookup type</param>
		/// <returns>The value of the attribute (or an empty string if there is
		/// no value).</returns>
		public string GetAttributeValueFromEntry (LdapEntry le, string attr)
		{
			LdapAttribute la = le.getAttribute (attr);

			if (la != null)
			{
				return la.StringValue;
			}

			return "";
		}

		/// <summary>Gets the value of the given attribute for the given
		/// entry.
		/// </summary>
		/// <param name="le">LdapEntry</param>
		/// <param name="attrs">List of attributes to lookup</param>
		/// <returns>A list of attribute values</returns>
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

		/// <summary>Gets the value of the given attribute for the given
		/// entry.
		/// </summary>
		/// <param name="le">LdapEntry</param>
		/// <param name="attrs">List of attributes to lookup</param>
		/// <param name="entryInfo">Hashtable to populate values with</returns>
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

		/// <summary>Gets an entry in the directory.
		/// </summary>
		/// <param name="dn">The distinguished name of the entry</param>
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

		/// <summary>Gets the children of a given entry.
		/// </summary>
		/// <param name="entryDN">Distiguished name of entry</param>
		/// <returns>A list of children (if any)</returns>
		public LdapEntry[] GetEntryChildren (string entryDN)
		{
			if (!conn.Connected)
				return null;

			return Search (entryDN, LdapConnection.SCOPE_ONE,
					    "objectclass=*", null);
		}

		/// <summary>Gets the local Samba SID (if available).
		/// </summary>
		/// <returns>sambaSID</returns>
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

		/// <summary>Gets the next available gidNumber
		/// </summary>
		/// <returns>The next group number</returns>
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

		/// <summary>Gets the next available uidNumber
		/// </summary>
		/// <returns>The next user number</returns>
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

		/// <summary>Gets a list of object classes supported on the directory.
		/// </summary>
		/// <returns>A list of object class entries</returns>
		public LdapEntry[] GetObjectClasses ()
		{
			string[] attrs = new string[] { "objectclasses" };

			return Search ("cn=subschema", LdapConnection.SCOPE_BASE, 
				       "objectclass=*", attrs);
		}

		/// <summary>Gets the schema of a given object class.
		/// </summary>
		/// <param name="objClass">Name of object class</param>
		/// <returns>A SchemaParser object</returns>
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

		/// <summary>Gets a list of requried attributes for a given object class.
		/// </summary>
		/// <param name="objClass">Name of object class</param>
		/// <returns>An array of required attribute names</returns>
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

		/// <summary>Gets a list of requried attributes for a list of given
		/// object classes.
		/// </summary>
		/// <param name="objClasses">Array of objectclass names</param>
		/// <returns>An array of required attribute names</returns>
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

		/// <summary>Modifies the specified entry
		/// </summary>
		/// <param name="dn">Distinguished name of entry to modify</param>
		/// <param name="mods">Array of LdapModification objects</param>
		public void Modify (string dn, LdapModification[] mods)
		{
			conn.Modify (dn, mods);
		}

		/// <summary>Moves the specified entry
		/// </summary>
		/// <param name="oldDN">Distinguished name of entry to move</param>
		/// <param name="newRDN">New name of entry</param>
		/// <param name="parentDN">Name of parent entry</param>
		public void Move (string oldDN, string newRDN, string parentDN)
		{
			conn.Rename (oldDN, newRDN, parentDN, true);
		}

		/// <summary>Renames the specified entry
		/// </summary>
		/// <param name="oldDN">Distinguished name of entry to rename</param>
		/// <param name="newDN">New to rename entry to</param>
		/// <param name="saveOld">Save old entry</param>
		public void Rename (string oldDN, string newDN, bool saveOld)
		{
			conn.Rename (oldDN, newDN, saveOld);
		}

		/// <summary>Searches the directory
		/// </summary>
		/// <param name="searchFilter">filter to search for</param>
		/// <returns>List of entries matching filter</returns>
		public LdapEntry[] Search (string searchFilter)
		{
			return Search (rootDN, LdapConnection.SCOPE_SUB, searchFilter, null);
		}

		/// <summary>Searches the directory
		/// </summary>
		/// <param name="searchBase">Where to start the search</param>
		/// <param name="searchFilter">Filter to search for</param>
		/// <returns>List of entries matching filter</returns>
		public LdapEntry[] Search (string searchBase, string searchFilter)
		{
			return Search (searchBase, LdapConnection.SCOPE_SUB, 
				       searchFilter, null);	
		}

		/// <summary>Searches the directory
		/// </summary>
		/// <param name="searchBase">Where to start the search</param>
		/// <param name="searchScope">Scope of search</param>
		/// <param name="searchFilter">Filter to search for</param>
		/// <param name="searchAttrs">Attributes to search for</param>
		/// <returns>List of entries matching filter</returns>
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

		/// <summary>Searches the directory for all entries of a given object
		/// class.
		/// </summary>
		/// <param name="objectClass">Name of objectclass</param>
		/// <returns>List of entries matching objectclass</returns>
		public LdapEntry[] SearchByClass (string objectClass)
		{
			return Search (rootDN, String.Format ("objectclass={0}", objectClass));
		}

		#endregion

		#region private_methods

		private string GetRootDN ()
		{
			string[] attrs = new string[] { "namingContexts" };

			LdapEntry[] dse = Search ("", LdapConnection.SCOPE_BASE, 
				       "objectclass=*", attrs);

			if (dse.Length > 0)
			{
				LdapAttribute a = dse[0].getAttribute ("namingContexts");
				return a.StringValue;
			}

			Logger.Log.Debug ("Unable to find directory namingContexts");

			return null;
		}

		private static bool SSLHandler (Syscert.X509Certificate certificate,
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
			set { conn.SecureSocketLayer = value; }
		}

		#endregion
	}
}
