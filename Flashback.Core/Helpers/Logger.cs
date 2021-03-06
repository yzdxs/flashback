using System;
using System.Reflection;
using System.Diagnostics;
using System.IO;

namespace Flashback.Core
{
	/// <summary>
	/// Provides basic console and text file logging. Requires the #LOGGING compilation symbol.
	/// </summary>
	public class Logger
	{
		static Logger()
		{
#if LOGGING
			try	
			{
				if (File.Exists(Settings.LogFile))
					File.Delete(Settings.LogFile);
			}
			catch {}
#endif
		}
		
		public static void Info(string format, params object[] args)
		{
			WriteLine(LoggingLevel.Info,format,args);
		}

		public static void Warn(string format, params object[] args)
		{
			WriteLine(LoggingLevel.Warn,format,args);
		}

		public static void Fatal(string format, params object[] args)
		{
			WriteLine(LoggingLevel.Fatal,format,args);
		}

		/// <summary>
		/// Writes the args to the default logging output using the format provided.
		/// </summary>
		public static void WriteLine(LoggingLevel level,string format, params object[] args)
		{
#if LOGGING
			var name = new StackFrame(2,false).GetMethod().Name;

			string prefix = string.Format("[{0} - {1}] ",level,name);
			string message = string.Format(prefix + format, args);

			Console.WriteLine(message);

			WriteToFile(message);
#endif
		}

		private static void WriteToFile(string message)
		{
			try
			{
				File.AppendAllText(Settings.LogFile, string.Format("[{0}] {1}\n", DateTime.UtcNow.ToString(), message));
			}
			catch (IOException)
			{
			}
		}
	}

	public enum LoggingLevel
	{
		Info,
		Warn,
		Fatal
	}
}
