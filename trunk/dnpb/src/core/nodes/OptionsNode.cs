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
using System.Collections;
using System.Collections.Specialized;
using System.Reflection;
using System.Xml;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Util;

namespace DNPreBuild.Core.Nodes
{
    [DataNode("Options", "Solution", "Configuration")]
    public class OptionsNode : DataNode
    {
        #region Fields

        private static Hashtable m_OptionFields = null;

        [OptionNode("CompilerDefines")]
        private string m_CompilerDefines = "";
        
        [OptionNode("OptimizeCode")]
        private bool m_OptimizeCode = false;
        
        [OptionNode("CheckUnderflowOverflow")]
        private bool m_CheckUnderflowOverflow = false;
        
        [OptionNode("AllowUnsafe")]
        private bool m_AllowUnsafe = false;
        
        [OptionNode("WarningLevel")]
        private int m_WarningLevel = 4;
        
        [OptionNode("WarningsAsErrors")]
        private bool m_WarningsAsErrors = false;
        
        [OptionNode("OutputPath")]
        private string m_OutputPath = "bin/";
        
        [OptionNode("XmlDocFile")]
        private string m_XmlDocFile = "";
        
        [OptionNode("DebugInformation")]
        private bool m_DebugInformation = false;
        
        [OptionNode("RegisterCOMInterop")]
        private bool m_RegisterCOMInterop = false;
        
        [OptionNode("IncrementalBuild")]
        private bool m_IncrementalBuild = false;
        
        [OptionNode("BaseAddress")]
        private string m_BaseAddress = "285212672";
        
        [OptionNode("FileAlignment")]
        private int m_FileAlignment = 4096;
        
        [OptionNode("NoStdLib")]
        private bool m_NoStdLib = false;

        private StringCollection m_ReferencePaths = null;
        private StringCollection m_FieldsChanged = null;

        #endregion

        #region Constructors

        static OptionsNode()
        {
            Type t = typeof(OptionsNode);
            
            m_OptionFields = new Hashtable();
            foreach(FieldInfo f in t.GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                object[] attrs = f.GetCustomAttributes(typeof(OptionNodeAttribute), false);
                if(attrs == null || attrs.Length < 1)
                    continue;

                OptionNodeAttribute ona = (OptionNodeAttribute)attrs[0];
                m_OptionFields[ona.NodeName] = f;
            }
        }

        public OptionsNode()
        {
            m_FieldsChanged = new StringCollection();
            m_ReferencePaths = new StringCollection();
        }

        #endregion

        #region Properties

        public object this[string idx]
        {
            get
            {
                if(!m_OptionFields.ContainsKey(idx))
                    return null;

                FieldInfo f = (FieldInfo)m_OptionFields[idx];
                return f.GetValue(this);
            }
        }

        public StringCollection ReferencePaths
        {
            get
            {
                return m_ReferencePaths;
            }
        }

        #endregion

        #region Private Methods

        private void FlagChanged(string name)
        {
            if(!m_FieldsChanged.Contains(name))
                m_FieldsChanged.Add(name);
        }

        private void SetOption(string nodeName, string val)
        {
            if(nodeName == "ReferencePath")
                m_ReferencePaths.Add(val);
            else
            {
                lock(m_OptionFields)
                {
                    if(!m_OptionFields.ContainsKey(nodeName))
                        return;

                    FieldInfo f = (FieldInfo)m_OptionFields[nodeName];
                    f.SetValue(this, Helper.TranslateValue(f.FieldType, val));
                    FlagChanged(f.Name);
                }
            }
        }

        #endregion

        #region Public Methods

        public override void Parse(XmlNode node)
        {
            foreach(XmlNode child in node.ChildNodes)
                SetOption(child.Name, child.InnerText);
        }

        public void CopyTo(ref OptionsNode opt)
        {
            if(opt == null)
                return;

            foreach(FieldInfo f in m_OptionFields.Values)
            {
                if(m_FieldsChanged.Contains(f.Name))
                    f.SetValue(opt, f.GetValue(this));
            }
        }

        #endregion        
    }
}
