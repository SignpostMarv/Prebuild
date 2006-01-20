#region BSD License
/*
Copyright (c) 2004-2005 Matthew Holmes (matthew@wildfiregames.com), Dan Moorehead (dan05a@gmail.com)

Redistribution and use in source and binary forms, with or without modification, are permitted
provided that the following conditions are met:

* Redistributions of source code must retain the above copyright notice, this list of conditions 
  and the following disclaimer. 
* Redistributions in binary form must reproduce the above copyright notice, this list of conditions 
  and the following disclaimer in the documentation and/or other materials provided with the 
  distribution. 
* The name of the author may not be used to endorse or promote products derived from this software 
  without specific prior written permission. 

THIS SOFTWARE IS PROVIDED BY THE AUTHOR ``AS IS'' AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, 
BUT NOT LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE 
ARE DISCLAIMED. IN NO EVENT SHALL THE AUTHOR BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS
OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY
OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING
IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
*/
#endregion

#region CVS Information
/*
 * $Source$
 * $Author$
 * $Date$
 * $Revision$
 */
#endregion

using System;
using System.IO;

namespace DNPreBuild.Core.Utilities
{
	public enum LogType
	{
		None,
		Info,
		Warning,
		Error
	}

	[Flags]
	public enum LogTargets
	{
		None = 0,
		Null = 1,
		File = 2,
		Console = 4
	}

	/// <summary>
	/// Summary description for Log.
	/// </summary>
	public class Log : IDisposable
	{
		#region Fields

		private StreamWriter m_Writer;
		private LogTargets m_Target = LogTargets.Null;
		bool disposed;

		#endregion

		#region Constructors

		public Log(LogTargets target, string fileName)
		{
			m_Target = target;
            
			if((m_Target & LogTargets.File) != 0)
			{
				m_Writer = new StreamWriter(fileName, false);
			}
		}

		#endregion

		#region Public Methods

		public void Write() 
		{
			Write(string.Empty);
		}

		public void Write(string msg) 
		{
			if((m_Target & LogTargets.Null) != 0)
			{
				return;
			}

			if((m_Target & LogTargets.Console) != 0)
			{
				Console.WriteLine(msg);
			}
			if((m_Target & LogTargets.File) != 0 && m_Writer != null)
			{
				m_Writer.WriteLine(msg);
			}
		}

		public void Write(string format, params object[] args)
		{
			Write(string.Format(format,args));
		}

		public void Write(LogType type, string format, params object[] args)
		{
			if((m_Target & LogTargets.Null) != 0)
			{
				return;
			}

			string str = "";
			switch(type) 
			{
				case LogType.Info:
					str = "[I] "; 
					break;
				case LogType.Warning:
					str = "[!] "; 
					break;
				case LogType.Error:
					str = "[X] "; 
					break;
			}

			Write(str + format,args);
		}

		public void WriteException(LogType type, Exception ex)
		{
			if(ex != null)
			{
				Write(type, ex.Message);
				//#if DEBUG
				m_Writer.WriteLine("Exception @{0} stack trace [[", ex.TargetSite.Name);
				m_Writer.WriteLine(ex.StackTrace);
				m_Writer.WriteLine("]]");
				//#endif
			}
		}

		public void Flush()
		{
			if(m_Writer != null)
			{
				m_Writer.Flush();
			}
		}

		#endregion

		#region IDisposable Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		/// <summary>
		/// Dispose objects
		/// </summary>
		/// <param name="disposing">
		/// If true, it will dispose close the handle
		/// </param>
		/// <remarks>
		/// Will dispose managed and unmanaged resources.
		/// </remarks>
		protected virtual void Dispose(bool disposing)
		{
			if (!this.disposed)
			{
				if (disposing)
				{
					if (m_Writer != null)
					{
						m_Writer.Close();
						m_Writer = null;
					}
				}
			}
			this.disposed = true;
		}

		/// <summary>
		/// 
		/// </summary>
		~Log()
		{
			this.Dispose(false);
		}
		
		/// <summary>
		/// Closes and destroys this object
		/// </summary>
		/// <remarks>
		/// Same as Dispose(true)
		/// </remarks>
		public void Close() 
		{
			Dispose();
		}

		#endregion
	}
}
