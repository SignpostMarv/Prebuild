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
using System.Xml;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Util;

namespace DNPreBuild.Core.Nodes
{
	[DataNode("Reference")]
    public class ReferenceNode : DataNode
	{
        #region Fields

        private string m_Name = "unknown";
        private string m_Path = null;
        private string m_Assembly = null;
        private bool m_LocalCopy = false;
        private string m_Version = null;

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

        public string Assembly
        {
            get
            {
                return m_Assembly;
            }
        }

        public bool LocalCopy
        {
            get
            {
                return m_LocalCopy;
            }
        }

        public string Version
        {
            get
            {
                return m_Version;
            }
        }

        #endregion

        #region Public Methods

        public override void Parse(XmlNode node)
        {
            m_Name = Helper.AttributeValue(node, "name", m_Name);
            m_Path = Helper.AttributeValue(node, "path", m_Path);
            m_Assembly = Helper.AttributeValue(node, "assembly", m_Assembly);
            m_LocalCopy = (bool)Helper.TranslateValue(typeof(bool),
                Helper.AttributeValue(node, "localCopy", "false"));
            m_Version = Helper.AttributeValue(node, "version", m_Version);
        }

        #endregion
	}
}
