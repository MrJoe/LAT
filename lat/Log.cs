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
using System.Text;

namespace lat 
{
	public enum LogLevel { 
		Error, 
		Warn,
		Info,
		Debug,
		None
	}

	public static class Log
	{
		static TextWriter textWriter;
		static LogLevel currentLevel = LogLevel.Info;

		public static void Initialize (LogLevel logLevel)
		{
			Log.currentLevel = logLevel;
			textWriter = Console.Out;
		}

		static void WriteLine (LogLevel level, string format, object[] args, Exception ex)
		{
			if (textWriter == null)
				return;

			if (currentLevel < level)
				return;

			string exceptionText = null;
			if (ex != null)
				exceptionText = ex.ToString();

			StringBuilder prefix = new StringBuilder ();
			prefix.AppendFormat ("{0} [{1:00000}] ", Defines.PACKAGE, Process.GetCurrentProcess ().Id); 

			switch (level) {

			case LogLevel.Error:
				prefix.Append ("ERROR ");
				break;

			case LogLevel.Warn:
				prefix.Append ("WARN ");
				break;

			case LogLevel.Info:
				prefix.Append ("INFO ");
				break;

			case LogLevel.Debug:
				prefix.Append ("DEBUG ");
				break;

			default:
				break;
			}

			StringBuilder msg = new StringBuilder ();
			msg.Append (prefix.ToString());
			
			if (format != null)
				msg.AppendFormat (format, args);

			if (exceptionText != null) {
				msg.Append ("\n");
				msg.Append (exceptionText);
			}
			
			textWriter.WriteLine (msg.ToString());
		}

		public static void Error (string message, params object[] args)
		{
			WriteLine (LogLevel.Error, message, args, null);
		}

		public static void Error (Exception e)
		{
			WriteLine (LogLevel.Error, null, null, e);
		}

		public static void Warn (string message, params object[] args)
		{
			WriteLine (LogLevel.Warn, message, args, null);
		}

		public static void Warn (Exception e)
		{
			WriteLine (LogLevel.Warn, null, null, e);
		}

		public static void Info (string message, params object[] args)
		{
			WriteLine (LogLevel.Info, message, args, null);
		}

		public static void Debug (string message, params object[] args)
		{
			WriteLine (LogLevel.Debug, message, args, null);
		}

		public static void Debug (Exception e)
		{
			WriteLine (LogLevel.Debug, null, null, e);
		}
	}
}  
