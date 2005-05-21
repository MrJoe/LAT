// 
// lat - Main.cs
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
using System.Diagnostics;
using System.IO;

namespace lat 
{
	public class Logger
	{
		private static TextWriter writer = Console.Out;

		public static Logger Log 
		{
			get 
			{
				Logger log = new Logger ();
				return log;
			}
		}

		private void Write (string level, string message) 
		{
			if (writer != null) 
			{
				writer.WriteLine ("{0}: {1}", level, message);
				writer.Flush ();
			}
		}

		public void Debug (string message, params object[] args) 
		{
			if (Global.Debug) 
			{
				Write ("DEBUG", String.Format (message, args));
			}
		}
	}
}  
