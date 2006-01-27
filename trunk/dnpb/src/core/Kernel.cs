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
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Xml;
using System.Xml.Schema;
using System.Text;

using Prebuild.Core.Attributes;
using Prebuild.Core.Interfaces;
using Prebuild.Core.Nodes;
using Prebuild.Core.Parse;
using Prebuild.Core.Utilities;

namespace Prebuild.Core 
{
	public class Kernel : IDisposable
	{
		#region Inner Classes

		private struct NodeEntry
		{
			public Type Type;
			public DataNodeAttribute Attribute;
		}

		#endregion

		#region Fields

		private static Kernel m_Instance = new Kernel();

		/// <summary>
		/// This must match the version of the schema that is embeeded
		/// </summary>
		private static string m_SchemaVersion = "1.6";
		private static string m_Schema = "dnpb-" + m_SchemaVersion + ".xsd";
		private static string m_SchemaURI = "http://dnpb.sourceforge.net/schemas/" + m_Schema;
		bool disposed;
		private Version m_Version;
		private string m_Revision = "";
		private CommandLineCollection m_CommandLine;
		private Log m_Log;
		private CurrentDirectory m_CurrentWorkingDirectory;
		private XmlSchemaCollection m_Schemas;
        
		private Hashtable m_Targets;
		private Hashtable m_Nodes;
        
		ArrayList m_Solutions;        
		string m_Target;
		string m_Clean;
		string[] m_RemoveDirectories;
		string m_CurrentFile;
		bool m_PauseAfterFinish;
		string[] m_ProjectGroups;
		StringCollection m_Refs;

		
		#endregion

		#region Constructors

		private Kernel()
		{
		}

		#endregion

		#region Properties

		public bool PauseAfterFinish 
		{
			get
			{ 
				return m_PauseAfterFinish; 
			} 
		}

		public static Kernel Instance
		{
			get
			{
				return m_Instance;
			}
		}

		public string Version
		{
			get
			{
				return String.Format("{0}.{1}.{2}{3}", m_Version.Major, m_Version.Minor, m_Version.Build, m_Revision);
			}
		}

		public CommandLineCollection CommandLine
		{
			get
			{
				return m_CommandLine;
			}
		}

		public Hashtable Targets
		{
			get
			{
				return m_Targets;
			}
		}

		public Log Log
		{
			get
			{
				return m_Log;
			}
		}

		public CurrentDirectory CurrentWorkingDirectory
		{
			get
			{
				return m_CurrentWorkingDirectory;
			}
		}

		public ArrayList Solutions
		{
			get
			{
				return m_Solutions;
			}
		}

		#endregion

		#region Private Methods

		private void RemoveDirectories(string rootDir, string[] dirNames) 
		{
			foreach(string dir in Directory.GetDirectories(rootDir)) 
			{
				string simpleName = Path.GetFileName(dir);

				if(Array.IndexOf(dirNames, simpleName) != -1) 
				{
					//delete if the name matches one of the directory names to delete
					string fullDirPath = Path.GetFullPath(dir);
					Directory.Delete(fullDirPath,true);
				} 
				else//not a match, so check children
				{
					RemoveDirectories(dir,dirNames);
					//recurse, checking children for them
				}
			}
		}

//		private void RemoveDirectoryMatches(string rootDir, string dirPattern) 
//		{
//			foreach(string dir in Directory.GetDirectories(rootDir)) 
//			{
//				foreach(string match in Directory.GetDirectories(dir)) 
//				{//delete all child directories that match
//					Directory.Delete(Path.GetFullPath(match),true);
//				}
//				//recure through the rest checking for nested matches to delete
//				RemoveDirectoryMatches(dir,dirPattern);
//			}
//		}

		private void LoadSchema()
		{
			Assembly assembly = this.GetType().Assembly;
			Stream stream = assembly.GetManifestResourceStream("Prebuild.data." + m_Schema);
			if(stream == null) 
			{
				//try without the default namespace prepending to it in case was compiled with SharpDevelop or MonoDevelop instead of Visual Studio .NET
				stream = assembly.GetManifestResourceStream(m_Schema);
				if(stream == null)
				{
					throw new System.Reflection.TargetException(string.Format("Could not find the scheme embedded resource file '{0}'.", m_Schema));
				}
			}
			XmlReader schema = new XmlTextReader(stream);
            
			m_Schemas = new XmlSchemaCollection();
			m_Schemas.Add(m_SchemaURI, schema);
		}

		private void CacheVersion() 
		{
			m_Version = Assembly.GetEntryAssembly().GetName().Version;
		}

		private void CacheTargets(Assembly assm)
		{
			foreach(Type t in assm.GetTypes())
			{
				TargetAttribute ta = (TargetAttribute)Helper.CheckType(t, typeof(TargetAttribute), typeof(ITarget));
				if(ta == null)
				{
					continue;
				}

				ITarget target = (ITarget)assm.CreateInstance(t.FullName);
				if(target == null)
				{
					throw new MissingMethodException("Could not create ITarget instance");
				}

				m_Targets[ta.Name] = target;
			}
		}

		private void CacheNodeTypes(Assembly assm)
		{
			foreach(Type t in assm.GetTypes())
			{
				DataNodeAttribute dna = (DataNodeAttribute)Helper.CheckType(t, typeof(DataNodeAttribute), typeof(IDataNode));
				if(dna == null)
				{
					continue;
				}

				NodeEntry ne = new NodeEntry();
				ne.Type = t;
				ne.Attribute = dna;
				m_Nodes[dna.Name] = ne;
			}
		}

		private void LogBanner()
		{
			m_Log.Write("Prebuild v" + this.Version);
			m_Log.Write("Copyright (c) Matthew Holmes, Dan Moorehead and David Hudson");
			m_Log.Write("See 'dnpb /usage' for help");
			m_Log.Write();
		}

		private void ProcessFile(string file)
		{
			m_CurrentWorkingDirectory.Push();
            
			string path = file;
			try
			{
				try
				{
					path = Helper.ResolvePath(path);
				}
				catch(ArgumentException)
				{
					m_Log.Write("Could not open Prebuild file: " + path);
					m_CurrentWorkingDirectory.Pop();
					return;
				}

				m_CurrentFile = path;
				Helper.SetCurrentDir(Path.GetDirectoryName(path));
            
				
				XmlTextReader reader = new XmlTextReader(path);
				
				Core.Parse.Preprocessor pre = new Core.Parse.Preprocessor();
				string xml = pre.Process(reader);//remove script and evaulate pre-proccessing to get schema-conforming XML
				
				
				XmlDocument doc = new XmlDocument();
				try
				{
					XmlValidatingReader validator = new XmlValidatingReader(new XmlTextReader(new StringReader(xml)));

					//validate while reading from string into XmlDocument DOM structure in memory
					foreach(XmlSchema schema in m_Schemas) 
					{
						validator.Schemas.Add(schema);
					}
					doc.Load(validator);
				} 
				catch(XmlException e) 
				{
					throw new XmlException(e.ToString());
				}

				//is there a purpose to writing it?  An syntax/schema problem would have been found during pre.Process() and reported with details
				if(m_CommandLine.WasPassed("ppo"))
				{
					string ppoFile = m_CommandLine["ppo"];
					if(ppoFile == null || ppoFile.Trim().Length < 1)
					{
						ppoFile = "preprocessed.xml";
					}

					StreamWriter writer = null;
					try
					{
						writer = new StreamWriter(ppoFile);
						writer.Write(xml);
					}
					catch(IOException ex)
					{
						Console.WriteLine("Could not write PPO file '{0}': {1}", ppoFile, ex.Message);
					}
					finally
					{
						if(writer != null)
						{
							writer.Close();
						}
					}
					return;
				}
				//start reading the xml config file
				XmlElement rootNode = doc.DocumentElement;
				//string suggestedVersion = Helper.AttributeValue(rootNode,"version","1.0");
				Helper.CheckForOSVariables = Helper.ParseBoolean(rootNode,"checkOsVars",false);

				foreach(XmlNode node in rootNode.ChildNodes)//solutions or if pre-proc instructions
				{
					IDataNode dataNode = ParseNode(node, null);
					if(dataNode is ProcessNode)
					{
						ProcessNode proc = (ProcessNode)dataNode;
						if(proc.IsValid)
						{
							ProcessFile(proc.Path);
						}
					}
					else if(dataNode is SolutionNode)
					{
						m_Solutions.Add(dataNode);
					}
				}
			}
			catch(XmlSchemaException xse)
			{
				m_Log.Write("XML validation error at line {0} in {1}:\n\n{2}",
					xse.LineNumber, path, xse.Message);
			}
			finally
			{
				m_CurrentWorkingDirectory.Pop();
			}
		}

		#endregion

		#region Public Methods

		public bool AllowProject(string projectGroupsFlags) 
		{
			if(m_ProjectGroups != null && m_ProjectGroups.Length > 0) 
			{
				if(projectGroupsFlags != null && projectGroupsFlags.Length == 0) 
				{
					foreach(string group in projectGroupsFlags.Split('|')) 
					{
						if(Array.IndexOf(m_ProjectGroups, group) != -1) //if included in the filter list
						{
							return true;
						}
					}
				}
				return false;//not included in the list or no groups specified for the project
			}
			return true;//no filter specified in the command line args
		}

		public Type GetNodeType(XmlNode node)
		{
			if( node == null )
			{
				throw new ArgumentNullException("node");
			}
			if(!m_Nodes.ContainsKey(node.Name))
			{
				return null;
			}

			NodeEntry ne = (NodeEntry)m_Nodes[node.Name];
			return ne.Type;
		}

		public IDataNode ParseNode(XmlNode node, IDataNode parent)
		{
			return ParseNode(node, parent, null);
		}

		//Create an instance of the data node type that is mapped to the name of the xml DOM node
		public IDataNode ParseNode(XmlNode node, IDataNode parent, IDataNode preNode)
		{
			IDataNode dataNode = null;

			try
			{
				if( node == null )
				{
					throw new ArgumentNullException("node");
				}
				if(preNode == null)
				{
					if(!m_Nodes.ContainsKey(node.Name))
					{
						//throw new XmlException("Unknown XML node: " + node.Name);
						return null;
					}

					NodeEntry ne = (NodeEntry)m_Nodes[node.Name];
					Type type = ne.Type;
					//DataNodeAttribute dna = ne.Attribute;

					dataNode = (IDataNode)type.Assembly.CreateInstance(type.FullName);
					if(dataNode == null)
					{
						throw new System.Reflection.TargetException("Could not create new parser instance: " + type.FullName);
					}
				}
				else
					dataNode = preNode;

				dataNode.Parent = parent;
				dataNode.Parse(node);
			}
			catch(WarningException wex)
			{
				m_Log.Write(LogType.Warning, wex.Message);
				return null;
			}
			catch(FatalException fex)
			{
				m_Log.WriteException(LogType.Error, fex);
				throw;
			}
			catch(Exception ex)
			{
				m_Log.WriteException(LogType.Error, ex);
				throw;
			}

			return dataNode;
		}

		public void Initialize(LogTargets target, string[] args)
		{
			m_Targets = new Hashtable();
			CacheTargets(this.GetType().Assembly);
			m_Nodes = new Hashtable();
			CacheNodeTypes(this.GetType().Assembly);
			CacheVersion();

			m_CommandLine = new CommandLineCollection(args);
            
			string logFile = null;
			if(m_CommandLine.WasPassed("log")) 
			{
				logFile = m_CommandLine["log"];

				if(logFile != null && logFile.Length == 0)
				{
					logFile = "Prebuild.log";
				}
			}
			else 
			{
				target = target & ~LogTargets.File;	//dont output to a file
			}
            
			m_Log = new Log(target, logFile);
			LogBanner();

			m_CurrentWorkingDirectory = new CurrentDirectory();

			m_Target = m_CommandLine["target"];
			m_Clean = m_CommandLine["clean"];
			string removeDirs = m_CommandLine["removedir"];
			if(removeDirs != null && removeDirs.Length == 0) 
			{
				m_RemoveDirectories = removeDirs.Split('|');
			}

			string flags = m_CommandLine["allowedgroups"];//allows filtering by specifying a pipe-delimited list of groups to include
			if(flags != null && flags.Length == 0)
			{
				m_ProjectGroups = flags.Split('|');
			}
			m_PauseAfterFinish = m_CommandLine.WasPassed("pause");

			LoadSchema();

			m_Solutions = new ArrayList();
			m_Refs = new StringCollection();
		}

		public void Process()
		{
			bool perfomedOtherTask = false;
			if(m_RemoveDirectories != null && m_RemoveDirectories.Length > 0) 
			{
				try
				{
					RemoveDirectories(".",m_RemoveDirectories);
				} 
				catch(IOException e) 
				{
					m_Log.Write("Failed to remove directories named {0}",m_RemoveDirectories);
					m_Log.WriteException(LogType.Error,e);
				}
				catch(UnauthorizedAccessException e) 
				{
					m_Log.Write("Failed to remove directories named {0}",m_RemoveDirectories);
					m_Log.WriteException(LogType.Error,e);
				}
				perfomedOtherTask = true;
			}

			if(m_Target != null && m_Clean != null)
			{
				m_Log.Write(LogType.Error, "The options /target and /clean cannot be passed together");
				return;
			}
			else if(m_Target == null && m_Clean == null)
			{
				if(perfomedOtherTask) //finished
				{
					return;
				}
				m_Log.Write(LogType.Error, "Must pass either /target or /clean to process a Prebuild file");
				return;
			}

			string file = "./prebuild.xml";
			if(m_CommandLine.WasPassed("file"))
			{
				file = m_CommandLine["file"];
			}

			ProcessFile(file);

			string target = (m_Target != null ? m_Target.ToLower() : m_Clean.ToLower());
			bool clean = (m_Target == null);
			if(clean && target != null && target.Length == 0)
			{
				target = "all";
			}
			if(clean && target == "all")//default to all if no target was specified for clean
			{
				Console.WriteLine("WARNING: This operation will clean ALL project files for all targets, are you sure? (y/n):");
				string ret = Console.ReadLine();
				if(ret == null)
				{
					return;
				}
				ret = ret.Trim().ToLower();
				if((ret.ToLower() != "y" && ret.ToLower() != "yes"))
				{
					return;
				}
			}
			
			if(target == "all")
			{
				target = "vs2002";//can be imported by all other tools
			}
			ITarget targ = (ITarget)m_Targets[target];
			
			if(clean)
			{
				targ.Clean(this);
			}
			else
			{
				targ.Write(this);
			}

			m_Log.Flush();
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
					if (this.m_Log != null)
					{
						this.m_Log.Close();
						this.m_Log = null;
					}
				}
			}
			this.disposed = true;
		}

		/// <summary>
		/// 
		/// </summary>
		~Kernel()
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