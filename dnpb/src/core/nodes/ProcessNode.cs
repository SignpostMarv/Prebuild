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
using System.Collections.Specialized;
using System.Xml;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Util;

namespace DNPreBuild.Core.Nodes
{
    [DataNode("Process")]
	public class ProcessNode : DataNode
	{
        #region Fields

        private string m_Path = null;
        private bool m_IsValid = true;

        #endregion

        #region Properties

        public string Path
        {
            get
            {
                return m_Path;
            }
        }

        public bool IsValid
        {
            get
            {
                return m_IsValid;
            }
        }

        #endregion

        #region Public Methods

        public override void Parse(XmlNode node)
        {
            m_Path = node.InnerText;
            if(m_Path == null)
                m_Path = "";

            try
            {
                m_Path = Helper.ResolvePath(m_Path);
            }
            catch(ArgumentException)
            {
                Kernel.Instance.Log.Write(LogType.Warning, "Could not find prebuild file for processing: {0}", m_Path);
                m_IsValid = false;
            }
        }

        #endregion
	}
}
