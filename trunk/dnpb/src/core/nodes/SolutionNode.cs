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
using System.Diagnostics;
using System.IO;
using System.Xml;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Utilities;

namespace DNPreBuild.Core.Nodes
{
	[DataNode("Solution")]
	public class SolutionNode : DataNode
	{
		#region Fields
        
		private string m_Name = "unknown";
		private string m_Path = "";
		private string m_FullPath = "";
		private string m_ActiveConfig = "Debug";
        
		private OptionsNode m_Options = null;
		private FilesNode m_Files = null;
		private Hashtable m_Configurations = null;
		private Hashtable m_Projects = null;

		#endregion

		#region Constructors

		public SolutionNode()
		{
			m_Configurations = new Hashtable();
			m_Projects = new Hashtable();
		}

		#endregion

		#region Properties

		public string ActiveConfig { get { return m_ActiveConfig; } set { m_ActiveConfig = value; } }

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

		public string FullPath
		{
			get
			{
				return m_FullPath;
			}
		}

		public OptionsNode Options
		{
			get
			{
				return m_Options;
			}
		}

		public FilesNode Files
		{
			get
			{
				return m_Files;
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
        
		public ICollection Projects
		{
			get
			{
				return m_Projects.Values;
			}
		}

		public Hashtable ProjectsTable
		{
			get
			{
				return m_Projects;
			}
		}

		#endregion

		#region Public Methods

		public override void Parse(XmlNode node)
		{
			m_Name = Helper.AttributeValue(node, "name", m_Name);
			m_ActiveConfig = Helper.AttributeValue(node, "activeConfig", m_ActiveConfig);
			m_Path = Helper.AttributeValue(node, "path", m_Path);

			m_FullPath = m_Path;
			try
			{
				m_FullPath = Helper.ResolvePath(m_FullPath);
			}
			catch
			{
				throw new WarningException("Could not resolve solution path: {0}", m_Path);
			}

			Kernel.Instance.CWDStack.Push();
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
					if(dataNode is OptionsNode)
					{
						m_Options = (OptionsNode)dataNode;
					}
					else if(dataNode is FilesNode)
					{
						m_Files = (FilesNode)dataNode;
					}
					else if(dataNode is ConfigurationNode)
					{
						m_Configurations[((ConfigurationNode)dataNode).Name] = dataNode;
					}
					else if(dataNode is ProjectNode)
					{
						m_Projects[((ProjectNode)dataNode).Name] = dataNode;
					}
				}
			}
			finally
			{
				Kernel.Instance.CWDStack.Pop();
			}
		}

		#endregion
	}
}
