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

		public LdapServer (ConnectionData connectionData)
		{
			conn = new LdapConnection ();
			
			host = connectionData.Host;
			port = connectionData.Port;
			sType = Util.GetServerType (connectionData.ServerType);
			encryption = connectionData.Encryption;
			
			if (connectionData.DirectoryRoot != "")
				rootDN = connectionData.DirectoryRoot;
				
			profileName = connectionData.Name;
				
			// FIXME: clean up this code
			adInfo = new ActiveDirectoryInfo ();
			SetServerType ();		
		}

		public LdapServer (string hostName, int hostPort, string serverType)
		{
			conn = new LdapConnection ();
			
			host = hostName;
			port = hostPort;
			sType = serverType;
			rootDN = null;
			encryption = EncryptionType.None;

			adInfo = new ActiveDirectoryInfo ();

			SetServerType ();
		}

		public LdapServer (string hostName, int hostPort, string dirRoot, string serverType)
		{
			conn = new LdapConnection ();
			
			host = hostName;
			port = hostPort;
			rootDN = dirRoot;
			sType = serverType;
			adInfo = new ActiveDirectoryInfo ();

			SetServerType ();
		}

		#region methods

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

		public LdapSchema GetSchema ()
		{
			if (!conn.Connected)
				return null;
				
			return conn.FetchSchema (conn.GetSchemaDN());
		}

		public string GetSchemaDN ()
		{
			if (!conn.Connected)
				return null;
				
			return conn.GetSchemaDN();
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
		/// <param name="searchBase">Where to start the search</param>
		/// <param name="searchScope">Scope of search</param>
		/// <param name="searchFilter">Filter to search for</param>
		/// <param name="searchAttrs">Attributes to search for</param>
		/// <returns>List of entries matching filter</returns>
		public LdapEntry[] Search (string searchBase, int searchScope, string searchFilter, string[] searchAttrs)
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

				Log.Debug (e);
				return null;
			}
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

		public string DefaultSearchFilter
		{
			get { return defaultSearchFilter; }
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
			get { return Util.GetServerType (ldapServerType); }
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
