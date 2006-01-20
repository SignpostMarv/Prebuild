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
using System.Collections;
using System.IO;
using System.Xml;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Utilities;

namespace DNPreBuild.Core.Nodes
{
	public enum ProjectType
	{
		Exe,
		WinExe,
		Library
	}

	public enum ClrRuntime
	{
		Microsoft,
		Mono
	}

	[DataNode("Project")]
	public class ProjectNode : DataNode
	{
		#region Fields

		private string m_Name = "unknown";
		private string m_Path = "";
		private string m_FullPath = "";
		private string m_AssemblyName;
		private string m_AppIcon = "";
		private string m_Language = "C#";
		private ProjectType m_Type = ProjectType.Exe;
		private ClrRuntime m_Runtime = ClrRuntime.Microsoft;
		private string m_StartupObject = "";
		private string m_RootNamespace;
		private string m_FilterGroups = "";
		private Guid m_Guid;

		private Hashtable m_Configurations;
		private ArrayList m_ReferencePaths;
		private ArrayList m_References;
		private FilesNode m_Files;

		#endregion

		#region Constructors

		public ProjectNode()
		{
			m_Configurations = new Hashtable();
			m_ReferencePaths = new ArrayList();
			m_References = new ArrayList();
		}

		#endregion

		#region Properties

		public string Name
		{
			get
			{
				return m_Name;
			}
		}

		public string Path
		{
			get
			{
				return m_Path;
			}
		}

		public string FilterGroups { get { return m_FilterGroups; } }

		public string FullPath
		{
			get
			{
				return m_FullPath;
			}
		}

		public string AssemblyName
		{
			get
			{
				return m_AssemblyName;
			}
		}

		public string AppIcon 
		{
			get 
			{
				return m_AppIcon;
			}
		}

		public string Language
		{
			get
			{
				return m_Language;
			}
		}

		public ProjectType Type
		{
			get
			{
				return m_Type;
			}
		}

		public ClrRuntime Runtime
		{
			get
			{
				return m_Runtime;
			}
		}

		public string StartupObject
		{
			get
			{
				return m_StartupObject;
			}
		}

		public string RootNamespace
		{
			get
			{
				return m_RootNamespace;
			}
		}

		public ICollection Configurations
		{
			get
			{
				return m_Configurations.Values;
			}
		}

		public Hashtable ConfigurationsTable
		{
			get
			{
				return m_Configurations;
			}
		}

		public ArrayList ReferencePaths
		{
			get
			{
				return m_ReferencePaths;
			}
		}

		public ArrayList References
		{
			get
			{
				return m_References;
			}
		}

		public FilesNode Files
		{
			get
			{
				return m_Files;
			}
		}

		public override IDataNode Parent
		{
			get
			{
				return base.Parent;
			}
			set
			{
				base.Parent = value;
				if(base.Parent is SolutionNode && m_Configurations.Count < 1)
				{
					SolutionNode parent = (SolutionNode)base.Parent;
					foreach(ConfigurationNode conf in parent.Configurations)
					{
						m_Configurations[conf.Name] = conf.Clone();
					}
				}
			}
		}

		public Guid Guid
		{
			get
			{
				return m_Guid;
			}
		}

		#endregion

		#region Private Methods

		private void HandleConfiguration(ConfigurationNode conf)
		{
			if(String.Compare(conf.Name, "all", true) == 0) //apply changes to all, this may not always be applied first,
				//so it *may* override changes to the same properties for configurations defines at the project level
			{
				foreach(ConfigurationNode confNode in this.m_Configurations.Values) 
				{
					conf.CopyTo(confNode);//update the config templates defines at the project level with the overrides
				}
			}
			if(m_Configurations.ContainsKey(conf.Name))
			{
				ConfigurationNode parentConf = (ConfigurationNode)m_Configurations[conf.Name];
				conf.CopyTo(parentConf);//update the config templates defines at the project level with the overrides
			} 
			else
			{
				m_Configurations[conf.Name] = conf;
			}
		}

		#endregion

		#region Public Methods

		public override void Parse(XmlNode node)
		{
			m_Name = Helper.AttributeValue(node, "name", m_Name);
			m_Path = Helper.AttributeValue(node, "path", m_Path);
			m_FilterGroups = Helper.AttributeValue(node, "filterGroups", m_FilterGroups);
			m_AppIcon = Helper.AttributeValue(node, "icon", m_AppIcon);
			m_AssemblyName = Helper.AttributeValue(node, "assemblyName", m_AssemblyName);
			m_Language = Helper.AttributeValue(node, "language", m_Language);
			m_Type = (ProjectType)Helper.EnumAttributeValue(node, "type", typeof(ProjectType), m_Type);
			m_Runtime = (ClrRuntime)Helper.EnumAttributeValue(node, "runtime", typeof(ClrRuntime), m_Runtime);
			m_StartupObject = Helper.AttributeValue(node, "startupObject", m_StartupObject);
			m_RootNamespace = Helper.AttributeValue(node, "rootNamespace", m_RootNamespace);
			m_Guid = Guid.NewGuid();
            
			if(m_AssemblyName == null || m_AssemblyName.Length < 1)
			{
				m_AssemblyName = m_Name;
			}

			if(m_RootNamespace == null || m_RootNamespace.Length < 1)
			{
				m_RootNamespace = m_Name;
			}

			m_FullPath = m_Path;
			try
			{
				m_FullPath = Helper.ResolvePath(m_FullPath);
			}
			catch
			{
				throw new WarningException("Could not resolve Solution path: {0}", m_Path);
			}

			Kernel.Instance.CurrentWorkingDirectory.Push();
			try
			{
				Helper.SetCurrentDir(m_FullPath);

				if( node == null )
				{
					throw new ArgumentNullException("node");
				}

				foreach(XmlNode child in node.ChildNodes)
				{
					IDataNode dataNode = Kernel.Instance.ParseNode(child, this);
					if(dataNode is ConfigurationNode)
					{
						HandleConfiguration((ConfigurationNode)dataNode);
					}
					else if(dataNode is ReferencePathNode)
					{
						m_ReferencePaths.Add(dataNode);
					}
					else if(dataNode is ReferenceNode)
					{
						m_References.Add(dataNode);
					}
					else if(dataNode is FilesNode)
					{
						m_Files = (FilesNode)dataNode;
					}
				}
			}
			finally
			{
				Kernel.Instance.CurrentWorkingDirectory.Pop();
			}
		}


		#endregion
	}
}
