// 
// lat - LdapEntryAnalyzer.cs
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
using Novell.Directory.Ldap;

namespace lat
{
	public class LdapEntryAnalyzer 
	{
		List<LdapModification> mods;
	
		public LdapEntryAnalyzer ()
		{
			mods = new List<LdapModification> ();
		}

		static bool IsAttributeEmpty (LdapAttribute attribute)
		{
			if (attribute == null)
				return true;
				
			if (attribute.size() == 0)
				return true;
				
			if (attribute.StringValue == null || attribute.StringValue == "")
				return true;
				
			return false;
		}

		public static string[] CheckRequiredAttributes (Connection conn, LdapEntry entry)
		{
			List<string> missingAttributes = new List<string> ();
		
			LdapAttribute objAttr = entry.getAttribute ("objectClass");
			if (objAttr == null)
				return null;
				
			foreach (string o in objAttr.StringValueArray) {
				if (o.Equals ("top"))
					continue;

				string[] reqs = conn.Data.GetRequiredAttrs (o);
				if (reqs == null)
					continue;

				foreach (string r in reqs) {
					if (r.Equals ("cn"))
						continue;
						
					if (IsAttributeEmpty (entry.getAttribute (r))) {
						missingAttributes.Add (r);
						continue;
					}
				}
			}
			
			return missingAttributes.ToArray();
		}

		public void Run (LdapEntry lhs, LdapEntry rhs)
		{
			Log.Debug ("Starting LdapEntryAnalyzer");
		
			if (lhs.CompareTo (rhs) != 0) {
				Log.Debug ("Entry DNs don't match\nlhs: {0}\nrhs: {1}", lhs.DN, rhs.DN);
				return;
			}
				
			LdapAttributeSet las = lhs.getAttributeSet ();
			foreach (LdapAttribute la in las) {			
				LdapAttribute rla = rhs.getAttribute (la.Name);
				if (rla == null){
				
					Log.Debug ("Delete attribute {0} from {1}", la.Name, lhs.DN);					
					LdapAttribute a = new LdapAttribute (la.Name);
					LdapModification m = new LdapModification (LdapModification.DELETE, a);
					mods.Add (m);
					
				} else {
				
					if (rla.StringValueArray.Length > 1) {
					
						Log.Debug ("Replacing attribute {0} with multiple values", la.Name); 
						LdapAttribute a = new LdapAttribute (la.Name, rla.StringValueArray);
						LdapModification m = new LdapModification (LdapModification.REPLACE, a);
						mods.Add (m);					
					
					} else if (la.StringValue != rla.StringValue) {
					
						LdapAttribute newattr;
						LdapModification lm;

						if (rla.StringValue == "" || rla.StringValue == null) {
							Log.Debug ("Delete attribute {0} from {1}", la.Name, lhs.DN);					
							newattr = new LdapAttribute (la.Name);
							lm = new LdapModification (LdapModification.DELETE, newattr);
						} else {
							Log.Debug ("Replace attribute {0} value from {1} to {2} ", la.Name, la.StringValue, rla.StringValue);
							newattr = new LdapAttribute (la.Name, rla.StringValue);
							lm = new LdapModification (LdapModification.REPLACE, newattr);
						}
						
						mods.Add (lm);
					}
				}
			}
			
			LdapAttributeSet rlas = rhs.getAttributeSet ();
			foreach (LdapAttribute la in rlas) {
				LdapAttribute lla = lhs.getAttribute (la.Name);
				if (lla == null && la.StringValue != string.Empty) {
					Log.Debug ("Add attribute {0} value [{1}] to {2}", la.Name, la.StringValue, lhs.DN);
					LdapAttribute a = new LdapAttribute (la.Name, la.StringValue);
					LdapModification m = new LdapModification (LdapModification.ADD, a);
					mods.Add (m);				
				}
			}
			
			Log.Debug ("End LdapEntryAnalyzer");
		}
		
		public LdapModification[] Differences
		{
			get { return mods.ToArray(); }
		}
	}
}