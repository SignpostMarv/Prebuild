#region BSD License
/*
Copyright (c) 2004 Matthew Holmes (kerion@houston.rr.com)

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

using System;
using System.Collections;
using System.IO;
using System.Xml;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Util;

namespace DNPreBuild.Core.Nodes
{
    public enum ProjectType
    {
        Exe,
        WinExe,
        Library
    }

    [DataNode("Project")]
	public class ProjectNode : DataNode
	{
        #region Fields

        private string m_Name = "unknown";
        private string m_Path = "";
        private string m_Language = "C#";
        private ProjectType m_Type = ProjectType.Exe;
        private string m_StartupObject = "";

        private ArrayList m_References = null;
        private FilesNode m_Files = null;

        #endregion

        #region Constructors

        public ProjectNode()
        {
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

        public string StartupObject
        {
            get
            {
                return m_StartupObject;
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

        #endregion

        #region Public Methods

        public override void Parse(XmlNode node)
        {
            m_Name = Helper.AttributeValue(node, "name", m_Name);
            m_Path = Helper.AttributeValue(node, "path", m_Path);
            m_Language = Helper.AttributeValue(node, "language", m_Language);
            m_Type = (ProjectType)Helper.EnumAttributeValue(node, "type", typeof(ProjectType), m_Type);
            m_StartupObject = Helper.AttributeValue(node, "startupObject", m_StartupObject);

            string tmpPath = m_Path;
            try
            {
                tmpPath = Helper.ResolvePath(tmpPath);
            }
            catch
            {
                throw new WarningException("Could not resolve Solution path: {0}", m_Path);
            }

            Kernel.Instance.CWDStack.Push();
            Environment.CurrentDirectory = tmpPath;

            foreach(XmlNode child in node.ChildNodes)
            {
                IDataNode dataNode = Kernel.Instance.ParseNode(child, this, "Project");
                if(dataNode is ReferenceNode)
                    m_References.Add(dataNode);
                else if(dataNode is FilesNode)
                    m_Files = (FilesNode)dataNode;
            }

            Kernel.Instance.CWDStack.Push();
        }


        #endregion
	}
}
