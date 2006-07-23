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
using System.Collections.Generic;
using Syscert = System.Security.Cryptography.X509Certificates;
using Mono.Security.X509;
using Mono.Security.Cryptography;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat {

	public enum LdapServerType { ActiveDirectory, OpenLDAP, FedoraDirectory, Generic, Unknown };

	public enum EncryptionType : byte { None, SSL, TLS };

	public struct ActiveDirectoryInfo
	{
		public string DnsHostName;
		public string DomainControllerFunctionality;
		public string ForestFunctionality;
		public string DomainFunctionality;
		public bool IsGlobalCatalogReady;
		public bool IsSynchronized;
	}

	/// <summary>The main class that encapsulates the connection
	/// to a directory server through the Ldap protocol.
	/// </summary>
	public class LdapServer
	{
		string host;
		int	port;
		string rootDN;
		string schemaDN;
		string defaultSearchFilter;
		string sType;
		string profileName;
		EncryptionType encryption;
		ActiveDirectoryInfo	adInfo;
		LdapServerType ldapServerType;
		LdapConnection conn;

		public LdapServer (string hostName, int hostPort, string serverType)
		{
			host = hostName;
			port = hostPort;
			sType = serverType;
			rootDN = null;
			encryption = EncryptionType.None;

			adInfo = new ActiveDirectoryInfo ();

			SetServerType ();
		}

		public LdapServer (string hostName, int hostPort, string dirRoot, 
				   string serverType)
		{
			host = hostName;
			port = hostPort;
			rootDN = dirRoot;
			sType = serverType;
			adInfo = new ActiveDirectoryInfo ();

			SetServerType ();
		}

		#region methods

		/// <summary>Adds an entry to the directory
		/// 
		/// </summary>
		/// <param name="dn">The distinguished name of the new entry.</param>
		/// <param name="attributes">An arraylist of string attributes for the 
		/// new ldap entry.</param>
		public void Add (string dn, List<LdapAttribute> attributes)
		{
			Log.Debug ("START Connection.Add ()");
			Log.Debug ("dn: {0}", dn);

			LdapAttributeSet attributeSet = new LdapAttributeSet();

			foreach (LdapAttribute attr in attributes) {

				foreach (string v in attr.StringValueArray)
					Log.Debug ("{0}:{1}", attr.Name, v);
				
				attributeSet.Add (attr);
			}

			LdapEntry newEntry = new LdapEntry( dn, attributeSet );

			conn.Add (newEntry);

			Log.Debug ("END Connection.Add ()");
		}

		public void Add (LdapEntry entry)
		{
			Log.Debug ("Adding entry {0}", entry.DN);
			conn.Add (entry);
		}

		/// <summary>Binds to the directory server with the given user
		/// name and password.
		/// </summary>
		/// <param name="userName">Username</param>
		/// <param name="userPass">Password</param> 
		public void Bind (string userName, string userPass)
		{
			conn.Bind (userName, userPass);

			Log.Debug ("Bound to directory as: {0}", userName);
		}

		/// <summary>Connects to the directory server.
		/// </summary>
		/// <param name="encryptionType">Type of encryption to use for session</param>
		public void Connect (EncryptionType encryptionType)
		{
			encryption = encryptionType;

			conn = new LdapConnection ();
	
			if (encryption == EncryptionType.SSL)
				conn.SecureSocketLayer = true;

			conn.UserDefinedServerCertValidationDelegate += new 
				CertificateValidationCallback(SSLHandler);

			conn.Connect (host, port);

			if (encryption == EncryptionType.TLS) {
				conn.startTLS ();
			}
			
			if (schemaDN == null)
				schemaDN = "cn=subschema";

			if (rootDN == null)
				QueryRootDSE ();

			Log.Debug ("Connected to '{0}' on port {1}", host, port);
			Log.Debug ("Base: {0}", rootDN);
			Log.Debug ("Using encryption type: {0}", encryptionType.ToString());
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

			Log.Debug ("Disconnected from '{0}'", host);
		}

		/// <summary>Gets a list of required and optional attributes for
		/// the given object classes.
		/// </summary>
		/// <param name="objClass">List of object classes</param>
		/// <param name="required">Required attributes</param>
		/// <param name="optional">Optional attributes</param>
		public void GetAllAttributes (List<string> objClass, 
					 out string[] required, out string[] optional)
		{
			try {

				LdapSchema schema;
				LdapObjectClassSchema ocs;
				
				List<string> r_attrs = new List<string> ();
				List<string> o_attrs = new List<string> ();

				schema = conn.FetchSchema ( conn.GetSchemaDN() );
						
				foreach (string c in objClass) {

					ocs = schema.getObjectClassSchema ( c );

					if (ocs.RequiredAttributes != null) {

						foreach (string r in ocs.RequiredAttributes)
							if (!r_attrs.Contains (r))
								r_attrs.Add (r);
					}

					if (ocs.OptionalAttributes != null) {
						foreach (string o in ocs.OptionalAttributes)
							if (!o_attrs.Contains (o))
								o_attrs.Add (o);
					}
				}

				required = r_attrs.ToArray ();
				optional = o_attrs.ToArray ();

			} catch (Exception e) {

				required = null;
				optional = null;

				Log.Debug ("getAllAttrs: {0}", e.Message);
			}
		}

		/// <summary>Gets a list of all attributes for the given object class
		/// </summary>
		/// <param name="objClass">Name of object class</param>
		public string[] GetAllAttributes (string objClass)
		{
			try {

				LdapSchema schema;
				LdapObjectClassSchema ocs;
				
				List<string> attrs = new List<string> ();
				
				schema = conn.FetchSchema ( conn.GetSchemaDN() );	
				
				ocs = schema.getObjectClassSchema ( objClass );

				if (ocs.RequiredAttributes != null) {
					foreach (string r in ocs.RequiredAttributes)
						if (!attrs.Contains (r))
							attrs.Add (r);
				}

				if (ocs.OptionalAttributes != null) {
					foreach (string o in ocs.OptionalAttributes)
						if (!attrs.Contains (o))
							attrs.Add (o);
				}

				attrs.Sort ();

				return attrs.ToArray ();

			} catch (Exception e) {
				Log.Debug("LdapServer.GetAllAttributes (" + objClass + "): \n" + e.Message);
				return null;
			}
		}

		/// <summary>Gets a list of attribute types supported on the
		/// directory.
		/// </summary>
		/// <returns>An array of LdapEntry objects</returns>
		public string[] GetAttributeTypes ()
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "attributetypes" };

			LdapEntry[] le = this.Search (schemaDN, LdapConnection.SCOPE_BASE, defaultSearchFilter, attrs);
			if (le == null)
				return null;

			List<string> tmp = new List<string> ();				
			LdapAttribute la = le[0].getAttribute ("attributetypes");		

			foreach (string s in la.StringValueArray) {
				SchemaParser sp = new SchemaParser (s);
				tmp.Add (sp.Names[0]);
			}

			tmp.Sort ();				
			return tmp.ToArray ();
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

			LdapEntry[] entries = Search (schemaDN, 
				LdapConnection.SCOPE_BASE,
	 			defaultSearchFilter, attrs);

			foreach (LdapEntry entry in entries) {

				LdapAttribute la = entry.getAttribute ("attributetypes");

				foreach (string s in la.StringValueArray) {

					SchemaParser sp = new SchemaParser (s);

					foreach (string a in sp.Names)
						if (attrType.Equals (a))
							return sp;
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
				return la.StringValue;

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

			List<string> retVal = new List<string> ();

			foreach (string n in attrs) {

				LdapAttribute la = le.getAttribute (n);

				if (la != null)
					retVal.Add (la.StringValue);
				else
					retVal.Add ("");
			}

			return retVal.ToArray ();
		}

		/// <summary>Gets the value of the given attribute for the given
		/// entry.
		/// </summary>
		/// <param name="le">LdapEntry</param>
		/// <param name="attrs">List of attributes to lookup</param>
		/// <param name="entryInfo">Dictionary<string,string> to populate values with</returns>
		public void GetAttributeValuesFromEntry (LdapEntry le, string[] attrs, 
							 out Dictionary<string,string> entryInfo)
		{
			if (le == null || attrs == null)
				throw new ArgumentNullException ();

			entryInfo = new Dictionary<string,string> ();

			foreach (string n in attrs) {

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
				return entry[0];
		
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
			LdapEntry[] sid = Search (rootDN, LdapConnection.SCOPE_SUB,
						    "objectclass=sambaDomain", null);

			if (sid.Length > 0) {
				LdapAttribute a = sid[0].getAttribute ("sambaSID");
				return a.StringValue;
			}

			return null;			
		}

		/// <summary>Gets the servers LDAP syntaxes (if available).
		/// </summary>
		/// <returns>matching rules</returns>
		public string[] GetLDAPSyntaxes ()
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "ldapSyntaxes" };

			LdapEntry[] le = this.Search (schemaDN, LdapConnection.SCOPE_BASE, "", attrs);
			if (le == null)
				return null;
				
			List<string> tmp = new List<string> ();				
			LdapAttribute la = le[0].getAttribute ("ldapSyntaxes");		

			foreach (string s in la.StringValueArray) {
				SchemaParser sp = new SchemaParser (s);
				tmp.Add (sp.Description);
			}
			
			tmp.Sort ();
			return tmp.ToArray ();				
		}

		/// <summary>Gets the schema information for a given ldap syntax
		/// </summary>
		/// <param name="attrType">LDAP syntax</param>
		/// <returns>schema information</returns>
		public SchemaParser GetLdapSyntax (string synName)
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "ldapSyntaxes" };

			LdapEntry[] entries = Search (schemaDN, LdapConnection.SCOPE_BASE, "", attrs);
			if (entries == null)
				return null;
			
			LdapAttribute la = entries[0].getAttribute ("ldapSyntaxes");
			foreach (string s in la.StringValueArray) {
				SchemaParser sp = new SchemaParser (s);
				if (synName.Equals (sp.Description))
						return sp;
			}
			
			return null;
		}

		/// <summary>Gets the servers matching rules (if available).
		/// </summary>
		/// <returns>matching rules</returns>
		public string[] GetMatchingRules ()
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "matchingRules" };

			LdapEntry[] le = this.Search (schemaDN, LdapConnection.SCOPE_BASE, "", attrs);
			if (le == null)
				return null;
				
			List<string> tmp = new List<string> ();
			LdapAttribute la = le[0].getAttribute ("matchingRules");			

			foreach (string s in la.StringValueArray) {
				SchemaParser sp = new SchemaParser (s);
				tmp.Add (sp.Names[0]);
			}

			tmp.Sort ();				
			return tmp.ToArray ();			
		}

		/// <summary>Gets the schema information for a given matching rule
		/// </summary>
		/// <param name="attrType">Matching rule</param>
		/// <returns>schema information</returns>
		public SchemaParser GetMatchingRule (string matName)
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "matchingRules" };

			LdapEntry[] entries = Search (schemaDN, LdapConnection.SCOPE_BASE, "", attrs);
			if (entries == null)
				return null;
			
			LdapAttribute la = entries[0].getAttribute ("matchingRules");
			foreach (string s in la.StringValueArray) {
				SchemaParser sp = new SchemaParser (s);

				foreach (string a in sp.Names)
					if (matName.Equals (a))
							return sp;
			}
			
			return null;
		}

		/// <summary>Gets the next available gidNumber
		/// </summary>
		/// <returns>The next group number</returns>
		public int GetNextGID ()
		{
			List<int> gids = new List<int> ();

			LdapEntry[] groups = Search (rootDN, LdapConnection.SCOPE_SUB,
						    "gidNumber=*", null);

			foreach (LdapEntry entry in groups) {
				LdapAttribute a = entry.getAttribute ("gidNumber");
				gids.Add (int.Parse(a.StringValue));
			}

			gids.Sort ();
			if (gids.Count == 0)
				return 1000;
			else
				return (int) (gids [gids.Count - 1]) + 1;
		}

		/// <summary>Gets the next available uidNumber
		/// </summary>
		/// <returns>The next user number</returns>
		public int GetNextUID ()
		{
			List<int> uids = new List<int> ();

			LdapEntry[] users = Search (rootDN, LdapConnection.SCOPE_SUB,
						    "uidNumber=*", null);

			foreach (LdapEntry entry in users) {
				LdapAttribute a = entry.getAttribute ("uidNumber");
				uids.Add (int.Parse(a.StringValue));
			}

			uids.Sort ();
			if (uids.Count == 0)
				return 1000;
			else
				return (int) (uids [uids.Count - 1]) + 1;
		}

		/// <summary>Gets a list of object classes supported on the directory.
		/// </summary>
		/// <returns>A list of object class entries</returns>
		public string[] GetObjectClasses ()
		{
			if (!conn.Connected)
				return null;

			string[] attrs = new string[] { "objectclasses" };

			LdapEntry[] le = this.Search (schemaDN, LdapConnection.SCOPE_BASE, defaultSearchFilter, attrs);
			if (le == null)
				return null;

			List<string> tmp = new List<string> ();
			LdapAttribute la = le[0].getAttribute ("objectclasses");			

			foreach (string s in la.StringValueArray) {
				SchemaParser sp = new SchemaParser (s);
				tmp.Add (sp.Names[0]);
			}

			tmp.Sort ();				
			return tmp.ToArray ();			
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

			LdapEntry[] entries = Search (schemaDN, 
				LdapConnection.SCOPE_BASE,
	 			defaultSearchFilter, attrs);

			foreach (LdapEntry entry in entries) {			
				LdapAttribute la = entry.getAttribute ("objectclasses");

				foreach (string s in la.StringValueArray) {
					SchemaParser sp = new SchemaParser (s);

					foreach (string a in sp.Names)
						if (objClass.Equals (a))
							return sp;
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
				return ocs.RequiredAttributes;

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

			List<string> retVal = new List<string> ();
			Dictionary<string,string> retHash = new Dictionary<string,string> ();

			LdapSchema schema;
			schema = conn.FetchSchema ( conn.GetSchemaDN() );

			foreach (string oc in objClasses) {

				LdapObjectClassSchema ocs;

				ocs = schema.getObjectClassSchema ( oc );

				foreach (string c in ocs.RequiredAttributes)
					if (!retHash.ContainsKey (c))
						retHash.Add (c, c);
			}

			foreach (KeyValuePair<string, string> kvp in retHash)
				retVal.Add (kvp.Key);

			return retVal.ToArray ();
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

			try {

				List<LdapEntry> retVal = new List<LdapEntry> ();

				LdapSearchQueue queue = conn.Search (searchBase,
						searchScope,
						searchFilter,
						searchAttrs,
						false,
						(LdapSearchQueue) null,
						(LdapSearchConstraints) null);

				LdapMessage msg;

				while ((msg = queue.getResponse ()) != null) {

					if (msg is LdapSearchResult) {
						LdapEntry entry = ((LdapSearchResult) msg).Entry;
						retVal.Add (entry);
					}
				}

				return retVal.ToArray ();

			} catch (Exception e) {

				Log.Debug ("LdapServer.Search error: {0}", e.Message);
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

		/// <summary>Tries to upgrade to an encrypted connection</summary>
		public void StartTLS ()
		{
			conn.startTLS ();
		}

		#endregion

		#region private_methods

		private void SetActiveDirectoryInfo (LdapEntry dse)
		{
			LdapAttribute a = dse.getAttribute ("dnsHostName");
			adInfo.DnsHostName = a.StringValue;

			LdapAttribute b = dse.getAttribute ("domainControllerFunctionality");

			if (b.StringValue == "0")
				adInfo.DomainControllerFunctionality =
					"Windows 2000 Mode";
			else if (b.StringValue == "2")
				adInfo.DomainControllerFunctionality = 
					"Windows Server 2003 Mode";
			else
				adInfo.DomainControllerFunctionality = "";

			LdapAttribute c = dse.getAttribute ("forestFunctionality");

			if (c.StringValue == "0")
				adInfo.ForestFunctionality = "Windows 2000 Forest Mode";
			else if (c.StringValue == "1")
				adInfo.ForestFunctionality = 
					"Windows Server 2003 Interim Forest Mode";
			else if (c.StringValue == "2")
				adInfo.ForestFunctionality = "Windows Server 2003 Forest Mode";
			else
				adInfo.ForestFunctionality = "";

			LdapAttribute d = dse.getAttribute ("domainFunctionality");

			if (d.StringValue == "0")
				adInfo.DomainFunctionality = "Windows 2000 Domain Mode";
			else if (d.StringValue == "1")
				adInfo.DomainFunctionality = 
					"Windows Server 2003 Interim Domain Mode";
			else if (d.StringValue == "2")
				adInfo.DomainFunctionality = "Windows Server 2003 Domain Mode";
			else
				adInfo.DomainFunctionality = "";

			LdapAttribute e = dse.getAttribute ("isGlobalCatalogReady");
			adInfo.IsGlobalCatalogReady = bool.Parse (e.StringValue);

			LdapAttribute f = dse.getAttribute ("isSynchronized");
			adInfo.IsSynchronized = bool.Parse (f.StringValue);
		}

		private void QueryRootDSE ()
		{
			LdapEntry[] dse;

			if (ldapServerType == LdapServerType.ActiveDirectory) {
				dse = Search ("", LdapConnection.SCOPE_BASE, 
					       "", null);

			} else {

				string[] attrs = new string[] { 
					"namingContexts",
					"subschemaSubentry" 
				};

				dse = Search ("", LdapConnection.SCOPE_BASE, 
					       "objectclass=*", attrs);
			}

			if (dse.Length > 0) {

				LdapAttribute a = dse[0].getAttribute ("namingContexts");
				rootDN = a.StringValue;

				LdapAttribute b = dse[0].getAttribute ("subschemaSubentry");
				schemaDN = b.StringValue;

				if (ldapServerType == LdapServerType.ActiveDirectory)
					SetActiveDirectoryInfo (dse[0]);

			} else {

				Log.Debug ("Unable to find directory namingContexts");
			}
		}

		private void SetServerType ()
		{
			switch (sType.ToLower()) {

			case "microsoft active directory":
				ldapServerType = LdapServerType.ActiveDirectory;
				defaultSearchFilter = "";
				break;

			case "openldap":
				ldapServerType = LdapServerType.OpenLDAP;
				defaultSearchFilter = "(objectClass=*)";
				break;
			
			case "fedora directory server":
				ldapServerType = LdapServerType.FedoraDirectory;
				defaultSearchFilter = "(objectClass=*)";
				break;

			case "generic":
				ldapServerType = LdapServerType.Generic;
				defaultSearchFilter = "(objectClass=*)";
				break;

			default:
				ldapServerType = LdapServerType.Unknown;
				defaultSearchFilter = "";
				break;
			}
		}

		static bool SSLHandler (Syscert.X509Certificate certificate, int[] certificateErrors)
		{
			bool retVal = true;
			X509Certificate x509 = null;

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

			Log.Debug ("Certificate info:\n{0}", msg);
			Log.Debug ("Certificate errors:\n{0}", certificateErrors.Length);

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
			set { port = value; }
		}

		public string ProfileName
		{
			get { return profileName; }
			set { profileName = value; }
		}

		public int Protocol
		{
			get { return conn.ProtocolVersion; }
		}

		public LdapServerType ServerType
		{
			get { return ldapServerType; }
		}

		public string ServerTypeString
		{
			get {
				switch (ldapServerType) {

				case LdapServerType.ActiveDirectory:
					return "Microsoft Active Directory";
					
				case LdapServerType.OpenLDAP:
					return "OpenLDAP";
					
				case LdapServerType.FedoraDirectory:
					return "Fedora Directory Server";
					
				case LdapServerType.Generic:
					return "Generic LDAP server";
					
				default:
					return "Generic LDAP server";
				}				
			}
		}

		public bool UseSSL
		{
			get { return conn.SecureSocketLayer; }
			set { conn.SecureSocketLayer = value; }
		}

		public EncryptionType Encryption
		{
			get { return encryption; }
			set { encryption = value; }
		}

		public ActiveDirectoryInfo ADInfo
		{
			get { return adInfo; }
		}

		#endregion
	}
}
