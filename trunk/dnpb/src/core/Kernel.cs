#region BSD License
/*
Copyright (c) 2004 Matthew Holmes (matthew@wildfiregames.com)

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

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Nodes;
using DNPreBuild.Core.Parse;
using DNPreBuild.Core.Util;

namespace DNPreBuild.Core 
{
    public sealed class Kernel
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

        private static string m_SchemaVersion = "1.2";
        private static string m_Schema = "dnpb-" + m_SchemaVersion + ".xsd";
        private static string m_SchemaURI = "http://dnpb.sourceforge.net/schemas/" + m_Schema;
        private Version m_Version = null;
        private string m_Revision = "";
        private CommandLine m_CommandLine = null;
        private Log m_Log = null;
        private CurrentDirStack m_CWDStack = null;
        private XmlSchemaCollection m_Schemas = null;
        
        private Hashtable m_Targets = null;
        private Hashtable m_Nodes = null;
        
        ArrayList m_Solutions = null;        
        string m_Target = null;
        string m_Clean = null;
        string m_CurrentFile = null;
        StringCollection m_Refs = null;

        #endregion

        #region Constructors

        private Kernel()
        {
        }

        #endregion

        #region Properties

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

        #region Private Methods

        private void LoadSchema()
        {
            Assembly assembly = this.GetType().Assembly;
            Stream stream = assembly.GetManifestResourceStream("DNPreBuild.data." + m_Schema);
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
            m_Log.Write(".NET Pre-Build v" + this.Version);
            m_Log.Write("Copyright (c) 2004 Matthew Holmes");
            m_Log.Write("See 'dnpb /usage' for help");
            m_Log.Write("");
        }

        private string WriteTempXml(string xml)
        {
            string tmpFile = Path.GetTempFileName();
            StreamWriter writer = new StreamWriter(tmpFile, false);
            writer.Write(xml);
            writer.Flush();
            writer.Close();

            return tmpFile;
        }

        private void ProcessFile(string file)
        {
            m_CWDStack.Push();
            
            string path = file;
            try
            {
                try
                {
                    path = Helper.ResolvePath(path);
                }
                catch(ArgumentException)
                {
                    m_Log.Write("Could not open .NET Pre-Build file: " + path);
                    m_CWDStack.Pop();
                    return;
                }

                m_CurrentFile = path;
                Helper.SetCurrentDir(Path.GetDirectoryName(path));
            
                XmlTextReader reader = new XmlTextReader(path);
                Preprocessor pre = new Preprocessor();
                string xml = pre.Process(reader);
                string tmpFile = WriteTempXml(xml);

                reader = new XmlTextReader(tmpFile);
                XmlValidatingReader valReader = new XmlValidatingReader(reader);
                valReader.Schemas.Add(m_Schemas);

                XmlDocument doc = new XmlDocument();
                doc.Load(valReader);

                if(doc.DocumentElement.NamespaceURI != m_SchemaURI)
                    throw new XmlException(
                        String.Format("Invalid schema namespace referenced {0}, expected {1}", 
                            doc.DocumentElement.NamespaceURI,
                            m_SchemaURI
                    ));

                string version = Helper.AttributeValue(doc.DocumentElement, "version", null);
                if(version != m_SchemaVersion)
                    throw new XmlException(String.Format("Invalid schema version referenced {0}, requires {1}", version, m_SchemaVersion));
            
                foreach(XmlNode node in doc.DocumentElement.ChildNodes)
                {
                    IDataNode dataNode = ParseNode(node, null);
                    if(dataNode is ProcessNode)
                    {
                        ProcessNode proc = (ProcessNode)dataNode;
                        if(proc.IsValid)
                            ProcessFile(proc.Path);
                    }
                    else if(dataNode is SolutionNode)
                        m_Solutions.Add(dataNode);
                }
            }
            catch(XmlSchemaException xse)
            {
                m_Log.Write("XML validation error at line {0} in {1}:\n\n{2}",
                    xse.LineNumber, path, xse.Message);
            }
            finally
            {
                m_CWDStack.Pop();
            }
        }

        #endregion

        #region Public Methods

        public Type GetNodeType(XmlNode node)
        {
            if(!m_Nodes.ContainsKey(node.Name))
                return null;

            NodeEntry ne = (NodeEntry)m_Nodes[node.Name];
            return ne.Type;
        }

        public IDataNode ParseNode(XmlNode node, IDataNode parent)
        {
            return ParseNode(node, parent, null);
        }

        public IDataNode ParseNode(XmlNode node, IDataNode parent, IDataNode preNode)
        {
            IDataNode dataNode = null;

            try
            {
                if(preNode == null)
                {
                    if(!m_Nodes.ContainsKey(node.Name))
                    {
                        //throw new XmlException("Unknown XML node: " + node.Name);
                        return null;
                    }

                    NodeEntry ne = (NodeEntry)m_Nodes[node.Name];
                    Type type = ne.Type;
                    DataNodeAttribute dna = ne.Attribute;

                    dataNode = (IDataNode)type.Assembly.CreateInstance(type.FullName);
                    if(dataNode == null)
                        throw new OutOfMemoryException("Could not create new parser instance: " + type.FullName);
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

        public void Initialize(LogTarget target, string[] args)
        {
            m_Targets = new Hashtable();
            CacheTargets(this.GetType().Assembly);
            m_Nodes = new Hashtable();
            CacheNodeTypes(this.GetType().Assembly);
            CacheVersion();

            m_CommandLine = new CommandLine(args);
            
            string logFile = "DNPreBuild.log";
            if(m_CommandLine.WasPassed("log"))
                logFile = m_CommandLine["log"];
            
            m_Log = new Log(target, logFile);
            LogBanner();

            m_CWDStack = new CurrentDirStack();

            m_Target = m_CommandLine["target"];
            m_Clean = m_CommandLine["clean"];

            LoadSchema();

            m_Solutions = new ArrayList();
            m_Refs = new StringCollection();
        }

        public void Process()
        {
            if(m_Target != null && m_Clean != null)
            {
                m_Log.Write(LogType.Error, "The options /target and /clean cannot be passed together");
                return;
            }
            else if(m_Target == null && m_Clean == null)
            {
                m_Log.Write(LogType.Error, "Must pass either /target or /clean to process a .NET pre-build file");
                return;
            }

            string file = "./prebuild.xml";
            if(m_CommandLine.WasPassed("file"))
                file = m_CommandLine["file"];

            ProcessFile(file);

            string target = (m_Target != null ? m_Target.ToLower() : m_Clean.ToLower());
            bool clean = (m_Target == null);
            if(clean && target == "all")
            {
                Console.Write("WARNING: This operation will clean ALL project files for all targets, are you sure? (y/n):");
                string ret = Console.ReadLine();
                ret = ret.Trim();
                if(ret == null || (ret.ToLower() != "y" && ret.ToLower() != "yes"))
                    return;
            }

            foreach(ITarget targ in m_Targets.Values)
            {
                // Because VS2003 and VS2002 use the same file extensions, we pass over
                // VS2003 when 'all' is selected, because VS2002 files can be converted
                // to VS2003 files by VS2003
                if(target == "all" && targ.Name == "vs2003")
                    continue;

                if(targ.Name.ToLower() == target || target == "all")
                {
                    if(clean)
                        targ.Clean(this);
                    else
                        targ.Write(this);
                }
            }

            m_Log.Flush();
        }

        #endregion        
    }
}