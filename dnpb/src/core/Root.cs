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
using System.Diagnostics;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Xml;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Nodes;
using DNPreBuild.Core.Util;

namespace DNPreBuild.Core 
{
    public sealed class Root
    {
        #region Inner Classes

        private struct NodeEntry
        {
            public Type Type;
            public DataNodeAttribute Attribute;
        }

        #endregion

        #region Fields

        private static Root m_Instance = new Root();

        private Version m_Version = null;
        private CommandLine m_CommandLine = null;
        private Log m_Log = null;
        private CurrentDirStack m_CWDStack = null;
        
        private Hashtable m_Targets = null;
        private Hashtable m_Nodes = null;
        
        ArrayList m_Solutions = null;        
        string m_Target = null;
        string m_CurrentFile = null;

        #endregion

        #region Constructors

        private Root()
        {
        }

        #endregion

        #region Private Methods

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
                    continue;

                ITarget target = (ITarget)assm.CreateInstance(t.FullName);
                if(target == null)
                    throw new OutOfMemoryException("Could not create ITarget instance");

                m_Targets[ta.Name] = target;
            }
        }

        private void CacheNodeTypes(Assembly assm)
        {
            foreach(Type t in assm.GetTypes())
            {
                DataNodeAttribute dna = (DataNodeAttribute)Helper.CheckType(t, typeof(DataNodeAttribute), typeof(IDataNode));
                if(dna == null)
                    continue;

                NodeEntry ne = new NodeEntry();
                ne.Type = t;
                ne.Attribute = dna;
                m_Nodes[dna.Name] = ne;
            }
        }

        private void LogBanner()
        {
            m_Log.Write(".NET Pre-Build v" + m_Version.ToString());
            m_Log.Write("Copyright (c) 2004 Matthew Holmes");
            m_Log.Write("See 'dnpb /usage' for help");
            m_Log.Write("");
        }

        private void ProcessFile(string file)
        {
            if(!File.Exists(file))
                throw new IOException("Could not open .NET Pre-Build file: " + file);

            m_CWDStack.Push();
            
            string path = Path.GetFullPath(file);
            m_CurrentFile = path;
            Environment.CurrentDirectory = Path.GetDirectoryName(path);
            
            XmlDocument doc = new XmlDocument();
            doc.Load(path);
            
            foreach(XmlNode node in doc.DocumentElement.ChildNodes)
            {
                IDataNode dataNode = ParseNode(node, null, "DNPreBuild");
                if(dataNode is SolutionNode)
                    m_Solutions.Add(dataNode);
            }

            m_CWDStack.Pop();
        }

        #endregion

        #region Public Methods

        public IDataNode ParseNode(XmlNode node, IDataNode parent, string parentName)
        {
            IDataNode dataNode = null;

            try
            {
                if(!m_Nodes.ContainsKey(node.Name))
                {
                    //throw new XmlException("Unknown XML node: " + node.Name);
                    return null;
                }

                NodeEntry ne = (NodeEntry)m_Nodes[node.Name];
                Type type = ne.Type;
                DataNodeAttribute dna = ne.Attribute;
                
                if(!dna.ValidParents.Contains(parentName))
                    throw new XmlException("Invalid sub-node: " + node.Name);

                Trace.WriteLine(String.Format("Creating new parser: {0}", type.FullName));

                dataNode = (IDataNode)type.Assembly.CreateInstance(type.FullName);
                if(dataNode == null)
                    throw new OutOfMemoryException("Could not create new parser instance: " + type.FullName);

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

        public void Initialize(LogTarget target, string[] args)
        {
            CacheVersion();
            m_CommandLine = new CommandLine(args);
            
            string logFile = "DNPreBuild.log";
            if(m_CommandLine.WasPassed("log"))
                logFile = m_CommandLine["log"];

            m_CWDStack = new CurrentDirStack();

            m_Target = m_CommandLine["target"];
            
            m_Log = new Log(target, logFile);
            LogBanner();
            
            m_Targets = new Hashtable();
            CacheTargets(this.GetType().Assembly);

            m_Nodes = new Hashtable();
            CacheNodeTypes(this.GetType().Assembly);

            m_Solutions = new ArrayList();
        }

        public void Process()
        {
            string file = "./prebuild.xml";
            if(m_CommandLine.WasPassed("file"))
                file = m_CommandLine["file"];

            ProcessFile(file);

            if(m_Targets.ContainsKey(m_Target))
            {
                ITarget target = (ITarget)m_Targets[m_Target];
                target.Write(this);
            }

            m_Log.Flush();
        }

        #endregion

        #region Properties

        public static Root Instance
        {
            get
            {
                return m_Instance;
            }
        }

        public CommandLine CommandLine
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

        public CurrentDirStack CWDStack
        {
            get
            {
                return m_CWDStack;
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
    }
}