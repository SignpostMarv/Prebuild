#region BSD License
/*
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
using System.Collections.Specialized;
using System.IO;
using System.Text.RegularExpressions;
using System.Xml;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Util;

namespace DNPreBuild.Core.Nodes
{
    [DataNode("Match", "Files")]
    public class MatchNode : DataNode
    {
        #region Fields

        private string[] m_Files = null;

        #endregion

        #region Constructors

        public MatchNode()
        {
            m_Files = new string[0];
        }

        #endregion

        #region Properties

        public string[] Files
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
            string path = Helper.AttributeValue(node, "path", null);
            string pattern = Helper.AttributeValue(node, "pattern", null);

            if(path == null)
                throw new WarningException("Match must have a 'path' attribute");

            if(pattern == null)
                throw new WarningException("Match must have a 'pattern' attribute");

            path = Helper.NormalizePath(path);
            if(!Directory.Exists(path))
                throw new WarningException("Match path does not exist: {0}", path);
            
            m_Files = Directory.GetFiles(path, pattern);
            if(m_Files.Length < 1)
                throw new WarningException("Math ({0}{1}) did not match any files", Helper.EndPath(path), pattern);
        }

        #endregion
	}
}
