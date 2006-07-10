// 
// lat - LDIF.cs
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
using System.IO;
using Novell.Directory.Ldap;

namespace lat
{
	public class LDIF
	{
		private LdapEntry _le = null;
		private LdapServer server = null;
		private int _numEntries = 0;

		public LDIF (LdapServer ldapServer)
		{
			server = ldapServer;
		}

		public LDIF (LdapEntry le)
		{
			this._le = le;
		}

		public string Export ()
		{
			string retVal = null;

			if (_le == null)
				return retVal;

			retVal = String.Format ("dn: {0}\n", _le.DN);

			LdapAttributeSet las = _le.getAttributeSet ();

			foreach (LdapAttribute attr in las) {

				// FIXME: handle binary value exports
				try {			
					foreach (string v in attr.StringValueArray) {
						string tmp = v.Replace ("\n", "\n ");
						retVal += String.Format ("{0}: {1}\n", attr.Name, tmp.Trim());
					}
				} catch {}
			}

			return retVal;
		}

		public void createEntry (Dictionary<string,LdapAttribute> ldap_info)
		{
			LdapAttribute dnAttr = ldap_info["dn"];

			string dn = dnAttr.StringValue.Trim();

			List<LdapAttribute> attrList = new List<LdapAttribute> ();

			foreach (KeyValuePair<string, LdapAttribute> kvp in ldap_info) {
				if (!kvp.Value.Name.Equals ("dn"))
					attrList.Add (kvp.Value);
			}

			try {

				server.Add (dn, attrList);
				_numEntries++;

			} catch {}
		}

		void ldifParse (Dictionary<string,LdapAttribute> ldap_info, string buf)
		{
			char[] delim = {':'};
			string[] pairs = buf.Split (delim, 2);

			if (!ldap_info.ContainsKey (pairs[0])) {
				LdapAttribute attr = new LdapAttribute (pairs[0], pairs[1].Trim());
				ldap_info.Add (pairs[0], attr);
			} else {
				LdapAttribute attr = ldap_info[pairs[0]];
				List<string> newValues = new List<string> ();

				newValues.Add (pairs[1].Trim());

				foreach (string v in attr.StringValueArray)
					newValues.Add (v);

				LdapAttribute newAttr = new LdapAttribute (pairs[0], newValues.ToArray ());

				ldap_info.Remove (pairs[0]);
				ldap_info.Add (pairs[0], newAttr);
			}
		}

		void readEntry (string dn, TextReader tr)
		{
			string line = null;
			Dictionary<string,LdapAttribute> ldapInfo = new Dictionary<string,LdapAttribute> ();

			ldifParse (ldapInfo, dn);

			try {
				while ((line = tr.ReadLine()) != null) {
					if (line.Equals (""))
						break;

					ldifParse (ldapInfo, line);
				}

				createEntry (ldapInfo);

			} catch {}
		}

		public int Import (Uri uri)
		{
			string line = null;

			try {
				StreamReader sr = new StreamReader (uri.LocalPath);

				while ((line = sr.ReadLine()) != null) {
					if (line.StartsWith ("dn:"))
						readEntry (line, sr);
				}

				return _numEntries;

			} catch {

				return 0;
			}
 		}

		public int Import (string textBuffer)
		{
			string line = null;

			try {
				StringReader sr = new StringReader (textBuffer);

				while ((line = sr.ReadLine()) != null)  {
					if (line.StartsWith ("dn:"))
						readEntry (line, sr);
				}

				return _numEntries;

			} catch	{
				return 0;
			}			
		}
	}
}
