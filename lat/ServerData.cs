// 
// lat - ServerData.cs
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
using System.Collections.Generic;
using Novell.Directory.Ldap;
using Novell.Directory.Ldap.Utilclass;

namespace lat
{
	public class ServerData
	{
		LdapServer server;
	
		public ServerData (LdapServer server)
		{
			this.server = server;
		}

		public void Add (LdapEntry entry)
		{
			server.Add (entry);
		}

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
			Add (newEntry);

			Log.Debug ("END Connection.Add ()");
		}

		/// <summary>Copy a directory entry
		/// </summary>
		/// <param name="oldDN">Distinguished name of the entry to copy</param>
		/// <param name="newRDN">New name for entry</param>
		/// <param name="parentDN">Parent name</param>
		public void Copy (string oldDN, string newRDN, string parentDN)
		{
			server.Copy (oldDN, newRDN, parentDN);
		}

		/// <summary>Deletes a directory entry
		/// </summary>
		/// <param name="dn">Distinguished name of the entry to delete</param>
		public void Delete (string dn)
		{
			server.Delete (dn);
		}
		
		/// <summary>Gets a list of all attributes for the given object class
		/// </summary>
		/// <param name="objClass">Name of object class</param>
		public string[] GetAllAttributes (string objClass)
		{
			try {

				LdapSchema schema = server.GetSchema ();
				LdapObjectClassSchema ocs = schema.getObjectClassSchema ( objClass );
				
				List<string> attrs = new List<string> ();
				
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
				Log.Debug (e);
				return null;
			}
		}
		
		/// <summary>Gets a list of required and optional attributes for
		/// the given object classes.
		/// </summary>
		/// <param name="objClass">List of object classes</param>
		/// <param name="required">Required attributes</param>
		/// <param name="optional">Optional attributes</param>
		public void GetAllAttributes (List<string> objClass, out string[] required, out string[] optional)
		{
			try {

				LdapSchema schema = server.GetSchema ();
				LdapObjectClassSchema ocs;
				
				List<string> r_attrs = new List<string> ();
				List<string> o_attrs = new List<string> ();
					
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

				Log.Debug (e);
			}
		}
		
		/// <summary>Gets a list of attribute types supported on the
		/// directory.
		/// </summary>
		/// <returns>An array of LdapEntry objects</returns>
		public string[] GetAttributeTypes ()
		{
			LdapEntry[] entries = server.Search (
									server.GetSchemaDN (), 
									LdapConnection.SCOPE_BASE, 
									server.DefaultSearchFilter, 
									new string[] { "attributetypes" });
			
			if (entries == null)
				return null;

			List<string> tmp = new List<string> ();				
			LdapAttribute la = entries[0].getAttribute ("attributetypes");		

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
			LdapEntry[] entries = server.Search (
									server.GetSchemaDN (), 
									LdapConnection.SCOPE_BASE, 
									server.DefaultSearchFilter, 
									new string[] { "attributetypes" });
			
			if (entries == null)
				return null;		

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
		
		/// <summary>Gets an entry in the directory.
		/// </summary>
		/// <param name="dn">The distinguished name of the entry</param>
		public LdapEntry GetEntry (string dn)
		{
			LdapEntry[] entry = server.Search (dn, LdapConnection.SCOPE_BASE, "objectclass=*", null);
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
			return server.Search (entryDN, LdapConnection.SCOPE_ONE, "objectclass=*", null);
		}
		
		/// <summary>Gets the schema information for a given ldap syntax
		/// </summary>
		/// <param name="attrType">LDAP syntax</param>
		/// <returns>schema information</returns>
		public SchemaParser GetLdapSyntax (string synName)
		{
			LdapEntry[] entries = server.Search (
									server.GetSchemaDN (), 
									LdapConnection.SCOPE_BASE, 
									"", 
									new string[] { "ldapSyntaxes" });
			
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
		
		/// <summary>Gets the servers LDAP syntaxes (if available).
		/// </summary>
		/// <returns>matching rules</returns>
		public string[] GetLdapSyntaxes ()
		{
			LdapEntry[] entries = server.Search (
									server.GetSchemaDN (), 
									LdapConnection.SCOPE_BASE, 
									"", 
									new string[] { "ldapSyntaxes" });
			
			if (entries == null)
				return null;
				
			List<string> tmp = new List<string> ();				
			LdapAttribute la = entries[0].getAttribute ("ldapSyntaxes");		

			foreach (string s in la.StringValueArray) {
				SchemaParser sp = new SchemaParser (s);
				tmp.Add (sp.Description);
			}
			
			tmp.Sort ();
			return tmp.ToArray ();				
		}
		
		/// <summary>Gets the local Samba SID (if available).
		/// </summary>
		/// <returns>sambaSID</returns>
		public string GetLocalSID ()
		{
			LdapEntry[] sid = server.Search (server.DirectoryRoot, LdapConnection.SCOPE_SUB, "objectclass=sambaDomain", null); 
			if (sid.Length > 0) {
				LdapAttribute a = sid[0].getAttribute ("sambaSID");
				return a.StringValue;
			}

			return null;			
		}
		
		/// <summary>Gets the schema information for a given matching rule
		/// </summary>
		/// <param name="attrType">Matching rule</param>
		/// <returns>schema information</returns>
		public SchemaParser GetMatchingRule (string matName)
		{
			LdapEntry[] entries = server.Search (
									server.GetSchemaDN (), 
									LdapConnection.SCOPE_BASE, 
									"", 
									new string[] { "matchingRules" });
			
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
		
		/// <summary>Gets the servers matching rules (if available).
		/// </summary>
		/// <returns>matching rules</returns>
		public string[] GetMatchingRules ()
		{
			LdapEntry[] entries = server.Search (
									server.GetSchemaDN (), 
									LdapConnection.SCOPE_BASE, 
									"", 
									new string[] { "matchingRules" });
			
			if (entries == null)
				return null;
				
			List<string> tmp = new List<string> ();
			LdapAttribute la = entries[0].getAttribute ("matchingRules");			

			foreach (string s in la.StringValueArray) {
				SchemaParser sp = new SchemaParser (s);
				tmp.Add (sp.Names[0]);
			}

			tmp.Sort ();				
			return tmp.ToArray ();
		}
		
		/// <summary>Gets the next available gidNumber
		/// </summary>
		/// <returns>The next group number</returns>
		public int GetNextGID ()
		{
			List<int> gids = new List<int> ();

			LdapEntry[] groups = server.Search (server.DirectoryRoot, LdapConnection.SCOPE_SUB, "gidNumber=*", null);
			foreach (LdapEntry entry in groups) {
				LdapAttribute a = entry.getAttribute ("gidNumber");
				gids.Add (int.Parse(a.StringValue));
			}

			gids.Sort ();
			if (gids.Count == 0)
				return 1000;
			else
				return (gids [gids.Count - 1]) + 1;
		}
		
		/// <summary>Gets the next available uidNumber
		/// </summary>
		/// <returns>The next user number</returns>
		public int GetNextUID ()
		{
			List<int> uids = new List<int> ();

			LdapEntry[] users = server.Search (server.DirectoryRoot, LdapConnection.SCOPE_SUB, "uidNumber=*", null);
			foreach (LdapEntry entry in users) {
				LdapAttribute a = entry.getAttribute ("uidNumber");
				uids.Add (int.Parse(a.StringValue));
			}

			uids.Sort ();
			if (uids.Count == 0)
				return 1000;
			else
				return (uids [uids.Count - 1]) + 1;
		}
		
		/// <summary>Gets the schema of a given object class.
		/// </summary>
		/// <param name="objClass">Name of object class</param>
		/// <returns>A SchemaParser object</returns>
		public SchemaParser GetObjectClassSchema (string objClass)
		{
			LdapEntry[] entries;
			
			entries = server.Search (
				server.GetSchemaDN (), 
				LdapConnection.SCOPE_BASE, 
				server.DefaultSearchFilter, 
				new string[] { "objectclasses" });
		
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
			if (objClass == null)
				return null;
			
			LdapSchema schema = server.GetSchema ();
			LdapObjectClassSchema ocs = schema.getObjectClassSchema ( objClass );

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
			if (objClasses == null)
				return null;

			List<string> retVal = new List<string> ();
			LdapSchema schema = server.GetSchema ();

			foreach (string oc in objClasses) {

				LdapObjectClassSchema ocs = schema.getObjectClassSchema ( oc );
				foreach (string c in ocs.RequiredAttributes)
					if (!retVal.Contains (c))
						retVal.Add (c);
			}

			return retVal.ToArray ();
		}
		
		/// <summary>Modifies the specified entry
		/// </summary>
		/// <param name="dn">Distinguished name of entry to modify</param>
		/// <param name="mods">Array of LdapModification objects</param>
		public void Modify (string dn, LdapModification[] mods)
		{
			server.Modify (dn, mods);
		}
		
		/// <summary>Moves the specified entry
		/// </summary>
		/// <param name="oldDN">Distinguished name of entry to move</param>
		/// <param name="newRDN">New name of entry</param>
		/// <param name="parentDN">Name of parent entry</param>
		public void Move (string oldDN, string newRDN, string parentDN)
		{
			server.Move (oldDN, newRDN, parentDN);
		}
		
		/// <summary>Renames the specified entry
		/// </summary>
		/// <param name="oldDN">Distinguished name of entry to rename</param>
		/// <param name="newDN">New to rename entry to</param>
		/// <param name="saveOld">Save old entry</param>
		public void Rename (string oldDN, string newDN, bool saveOld)
		{
			server.Rename (oldDN, newDN, saveOld);
		}
		
		/// <summary>Searches the directory
		/// </summary>
		/// <param name="searchFilter">filter to search for</param>
		/// <returns>List of entries matching filter</returns>
		public LdapEntry[] Search (string searchFilter)
		{
			return server.Search (server.DirectoryRoot, LdapConnection.SCOPE_SUB, searchFilter, null); 
		}
		
		/// <summary>Searches the directory
		/// </summary>
		/// <param name="searchBase">Where to start the search</param>
		/// <param name="searchFilter">Filter to search for</param>
		/// <returns>List of entries matching filter</returns>
		public LdapEntry[] Search (string searchBase, string searchFilter)
		{
			return server.Search (searchBase, LdapConnection.SCOPE_SUB, searchFilter, null); 
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
			return server.Search (searchBase, searchScope, searchFilter, searchAttrs);
		}
		
		/// <summary>Searches the directory for all entries of a given object
		/// class.
		/// </summary>
		/// <param name="objectClass">Name of objectclass</param>
		/// <returns>List of entries matching objectclass</returns>
		public LdapEntry[] SearchByClass (string objectClass)
		{
			return Search (server.DirectoryRoot, String.Format ("objectclass={0}", objectClass)); 
		}
		
		public string[] ObjectClasses
		{		
			get {
			
				LdapEntry[] le = server.Search (
					server.GetSchemaDN (), 
					LdapConnection.SCOPE_BASE, 
					server.DefaultSearchFilter, 
					new string[] { "objectclasses" });
			
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
		}
	}
}