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
using System.IO;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Nodes;
using DNPreBuild.Core.Util;

namespace DNPreBuild.Core.Targets
{
    public enum VSVersion
    {
        VS70,
        VS71
    }

    [Target("vs2003")]
	public class VS2003Target : ITarget
	{
        #region Inner Classes

        protected struct ToolInfo
        {
            public string Name;
            public string GUID;
            public string FileExtension;
            public string XMLTag;

            public ToolInfo(string name, string guid, string fileExt, string xml)
            {
                Name = name;
                GUID = guid;
                FileExtension = fileExt;
                XMLTag = xml;
            }
        }

        #endregion

        #region Fields

        protected string m_SolutionVersion = "8.00";
        protected string m_ProductVersion = "7.10.3077";
        protected string m_SchemaVersion = "2.0";
        protected string m_VersionName = "2003";
        protected VSVersion m_Version = VSVersion.VS71;
        
        private Root m_Root = null;
        private Hashtable m_ProjectUUIDs = null;
        private Hashtable m_Tools = null;

        #endregion

        #region Constructors

        public VS2003Target()
        {
            m_ProjectUUIDs = new Hashtable();
            m_Tools = new Hashtable();

            m_Tools["C#"] = new ToolInfo("C#", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "csproj", "CSHARP");
            m_Tools["VB.NET"] = new ToolInfo("VB.NET", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", "vbproj", "VisualBasic");
        }

        #endregion

        #region Private Methods

        private void WriteProject(SolutionNode solution, ProjectNode project)
        {
            if(!m_Tools.ContainsKey(project.Language))
                throw new Exception("Unknown .NET language: " + project.Language);

            ToolInfo toolInfo = (ToolInfo)m_Tools[project.Language];
            string projectFile = Path.GetFullPath(Helper.MakeFilePath(project.Path, project.Name, toolInfo.FileExtension));
            StreamWriter ps = new StreamWriter(projectFile);

            using(ps)
            {
                ps.WriteLine("<VisualStudioProject>");
                ps.WriteLine("\t<{0}", toolInfo.XMLTag);
                ps.WriteLine("\t\tProjectType = \"Local\"");
                ps.WriteLine("\t\tProductVersion = \"{0}\"", m_ProductVersion);
                ps.WriteLine("\t\tSchemaVersion = \"{0}\"", m_SchemaVersion);
                ps.WriteLine("\t\tProjectGuid = \"{{{0}}}\"", ((Guid)m_ProjectUUIDs[project]).ToString().ToUpper());
                ps.WriteLine("\t>");

                ps.WriteLine("\t\t<Build>");
                
                ps.WriteLine("\t\t\t<Settings");
                ps.WriteLine("\t\t\t\tApplicationIcon = \"\"");
                ps.WriteLine("\t\t\t\tAssemblyKeyContainerName = \"\"");
                ps.WriteLine("\t\t\t\tAssemblyName = \"{0}\"", project.Name);
                ps.WriteLine("\t\t\t\tAssemblyOriginatorKeyFile = \"\"");
                ps.WriteLine("\t\t\t\tDefaultClientScript = \"JScript\"");
                ps.WriteLine("\t\t\t\tDefaultHTMLPageLayout = \"Grid\"");
                ps.WriteLine("\t\t\t\tDefaultTargetSchema = \"IE50\"");
                ps.WriteLine("\t\t\t\tDelaySign = \"false\"");

                if(m_Version == VSVersion.VS70)
                    ps.WriteLine("\t\t\t\tNoStandardLibraries = \"false\"");

                ps.WriteLine("\t\t\t\tOutputType = \"{0}\"", project.Type.ToString());
                ps.WriteLine("\t\t\t\tRootNamespace = \"{0}\"", project.Name);
                ps.WriteLine("\t\t\t\tStartupObject = \"{0}\"", project.StartupObject);
                ps.WriteLine("\t\t\t>");

                foreach(ConfigurationNode conf in solution.Configurations)
                {
                    ps.WriteLine("\t\t\t\t<Config");
                    ps.WriteLine("\t\t\t\t\tName = \"{0}\"", conf.Name);
                    ps.WriteLine("\t\t\t\t\tAllowUnsafeBlocks = \"{0}\"", conf.Options["AllowUnsafe"]);
                    ps.WriteLine("\t\t\t\t\tBaseAddress = \"{0}\"", conf.Options["BaseAddress"]);
                    ps.WriteLine("\t\t\t\t\tCheckForOverflowUnderflow = \"{0}\"", conf.Options["CheckUnderflowOverflow"]);
                    ps.WriteLine("\t\t\t\t\tConfigurationOverrideFile = \"\"");
                    ps.WriteLine("\t\t\t\t\tDefineConstants = \"{0}\"", conf.Options["CompilerDefines"]);
                    ps.WriteLine("\t\t\t\t\tDocumentationFile = \"{0}\"", conf.Options["XmlDocFile"]);
                    ps.WriteLine("\t\t\t\t\tDebugSymbols = \"{0}\"", conf.Options["DebugInformation"]);
                    ps.WriteLine("\t\t\t\t\tFileAlignment = \"{0}\"", conf.Options["FileAlignment"]);
                    ps.WriteLine("\t\t\t\t\tIncrementalBuild = \"{0}\"", conf.Options["IncrementalBuild"]);
                    
                    if(m_Version == VSVersion.VS71)
                    {
                        ps.WriteLine("\t\t\t\t\tNoStdLib = \"{0}\"", conf.Options["NoStdLib"]);
                        ps.WriteLine("\t\t\t\t\tNoWarn = \"{0}\"", conf.Options["SupressWarnings"]);
                    }

                    ps.WriteLine("\t\t\t\t\tOptimize = \"{0}\"", conf.Options["OptimizeCode"]);                    
                    ps.WriteLine("\t\t\t\t\tOutputPath = \"{0}\"", 
                        Helper.EndPath(Helper.NormalizePath(conf.Options["OutputPath"].ToString())) + Helper.EndPath(conf.Name));
                    ps.WriteLine("\t\t\t\t\tRegisterForComInterop = \"{0}\"", conf.Options["RegisterCOMInterop"]);
                    ps.WriteLine("\t\t\t\t\tRemoveIntegerChecks = \"{0}\"", conf.Options["RemoveIntegerChecks"]);
                    ps.WriteLine("\t\t\t\t\tTreatWarningsAsErrors = \"{0}\"", conf.Options["WarningsAsErrors"]);
                    ps.WriteLine("\t\t\t\t\tWarningLevel = \"{0}\"", conf.Options["WarningLevel"]);
                    ps.WriteLine("\t\t\t\t/>");
                }

                ps.WriteLine("\t\t\t</Settings>");

                ps.WriteLine("\t\t\t<References>");
                foreach(ReferenceNode refr in project.References)
                {
                    ps.WriteLine("\t\t\t\t<Reference");
                    ps.WriteLine("\t\t\t\t\tName = \"{0}\"", refr.Name);
                    
                    if(refr.Assembly != null)
                        ps.WriteLine("\t\t\t\t\tAssemblyName = \"{0}\"", refr.Assembly);

                    if(refr.Path != null)
                        ps.WriteLine("\t\t\t\t\tHintPath = \"{0}\"", refr.Path);

                    if(refr.LocalCopy)
                        ps.WriteLine("\t\t\t\t\tPrivate = \"{0}\"", refr.LocalCopy);
                    
                    ps.WriteLine("\t\t\t\t/>");
                }
                ps.WriteLine("\t\t\t</References>");

                ps.WriteLine("\t\t</Build>");
                ps.WriteLine("\t\t<Files>");
                
                ps.WriteLine("\t\t\t<Include>");
                foreach(string file in project.Files)
                {
                    ps.WriteLine("\t\t\t\t<File");
                    ps.WriteLine("\t\t\t\t\tRelPath = \"{0}\"", file.Replace(".\\", ""));
                    ps.WriteLine("\t\t\t\t\tSubType = \"Code\"");
                    ps.WriteLine("\t\t\t\t\tBuildAction = \"Compile\"");
                    ps.WriteLine("\t\t\t\t/>");
                }
                ps.WriteLine("\t\t\t</Include>");
                
                ps.WriteLine("\t\t</Files>");
                ps.WriteLine("\t</{0}>", toolInfo.XMLTag);
                ps.WriteLine("</VisualStudioProject>");
            }
        }

        private void WriteSolution(SolutionNode solution)
        {
            m_Root.Log.Write("Creating Visual Studio {0} solution and project files", m_VersionName);

            foreach(ProjectNode project in solution.Projects)
            {
                m_ProjectUUIDs[project] = Guid.NewGuid();
                m_Root.Log.Write("...Creating project: {0}", project.Name);
                WriteProject(solution, project);
            }
            
            m_Root.Log.Write("");

            string solutionFile = Path.GetFullPath(Helper.MakeFilePath(solution.Path, solution.Name, "sln"));
            StreamWriter ss = new StreamWriter(solutionFile);
            
            using(ss)
            {
                ss.WriteLine("Microsoft Visual Studio Solution File, Format Version {0}", m_SolutionVersion);
                foreach(ProjectNode project in solution.Projects)
                {
                    if(!m_Tools.ContainsKey(project.Language))
                        throw new Exception("Unknown .NET language: " + project.Language);

                    ToolInfo toolInfo = (ToolInfo)m_Tools[project.Language];
                
                    ss.WriteLine("Project(\"{0}\") = \"{1}\", \"{2}\", \"{{{3}}}\"",
                        toolInfo.GUID, project.Name, Helper.MakeFilePath(project.Path, project.Name,
                        toolInfo.FileExtension), ((Guid)m_ProjectUUIDs[project]).ToString().ToUpper());

                    ss.WriteLine("\tProjectSection(ProjectDependencies) = postProject");
                    ss.WriteLine("\tEndProjectSection");

                    ss.WriteLine("EndProject");
                }

                ss.WriteLine("Global");

                ss.WriteLine("\tGlobalSection(SolutionConfiguration) = preSolution");
                for(int i = 0; i < solution.Configurations.Count; i++)
                {
                    ConfigurationNode conf = (ConfigurationNode)solution.Configurations[i];
                    ss.WriteLine("\t\tConfigName.{0} = {1}", i, conf.Name);
                }
                ss.WriteLine("\tEndGlobalSection");

                ss.WriteLine("\tGlobalSection(ProjectDependencies) = postSolution");
                foreach(ProjectNode project in solution.Projects)
                {
                    for(int i = 0; i < project.References.Count; i++)
                    {
                        ReferenceNode refr = (ReferenceNode)project.References[i];
                        if(solution.ProjectsTable.ContainsKey(refr.Name))
                            ss.WriteLine("\t\t{0}.{1} = {2}", 
                                m_ProjectUUIDs[project],i, m_ProjectUUIDs[solution.ProjectsTable[refr.Name]]
                            );
                    }
                }
                ss.WriteLine("\tEndGlobalSection");

                ss.WriteLine("\tGlobalSection(ProjectConfiguration) = postSolution");
                foreach(ProjectNode project in solution.Projects)
                {
                    foreach(ConfigurationNode conf in solution.Configurations)
                    {
                        ss.WriteLine("\t\t({{{0}}}).{1}.ActiveCfg = {1}|.NET",
                            ((Guid)m_ProjectUUIDs[project]).ToString().ToUpper(),
                            conf.Name);

                        ss.WriteLine("\t\t({{{0}}}).{1}.Build.0 = {1}|.NET",
                            ((Guid)m_ProjectUUIDs[project]).ToString().ToUpper(),
                            conf.Name);
                    }
                }
                ss.WriteLine("\tEndGlobalSection");

                if(solution.Files != null)
                {
                    ss.WriteLine("\tGlobalSection(SolutionItems) = postSolution");
                    foreach(string file in solution.Files)
                        ss.WriteLine("\t\t{0} = {0}", file);
                    ss.WriteLine("\tEndGlobalSection");
                }

                ss.WriteLine("EndGlobal");
            }
        }

        #endregion

        #region ITarget Members

        public virtual void Write(Root root)
        {
            m_Root = root;
            foreach(SolutionNode sol in root.Solutions)
                WriteSolution(sol);
        }

        public string Name
        {
            get
            {
                return "vs2003";
            }
        }

        #endregion
    }
}
