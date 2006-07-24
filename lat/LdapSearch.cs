//
// lat - LdapSearch.cs
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

public class LdapSearch
{
	// FIXME: look into using RfcFilter() class for this
	private string _filter = "";

	public LdapSearch ()
	{
	}

	public void addCondition (string attr, string op, string val)
	{
		switch (op) {

		case "begins with":
			_filter += String.Format ("({0}={1}*)", attr, val);
			break;

		case "ends with":
			_filter += String.Format ("({0}=*{1})", attr, val);
			break;

		case "equals":
			_filter += String.Format ("({0}={1})", attr, val);
			break;
		
		case "contains":
			_filter += String.Format ("({0}=*{1}*)", attr, val);
			break;

		case "is present":
			_filter += String.Format ("({0}=*)", attr);
			break;

		default:
			break;
		}
	}

	public void addBool (string opBool)
	{
		switch (opBool) {

		case "AND":
			_filter = String.Format ("(&{0}", _filter);
			break;

		case "OR":
			_filter = String.Format ("(|{0}", _filter);
			break;

		default:
			break;
		}
	}

	public void endBool ()
	{
		if (_filter.StartsWith("(&") || _filter.StartsWith("(|"))
			_filter += ")";
	}

	public string Filter
	{
		get { return _filter; }
	}
}
