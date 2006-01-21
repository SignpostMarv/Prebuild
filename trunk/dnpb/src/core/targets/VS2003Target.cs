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
using System.Collections.Specialized;
using System.IO;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Nodes;
using DNPreBuild.Core.Utilities;

namespace DNPreBuild.Core.Targets
{
	public enum VSVersion
	{
		VS70,
		VS71,
		VS80
	}

	[Target("vs2003")]
	public class VS2003Target : ITarget
	{

		#region Fields

		string solutionVersion = "8.00";
		string productVersion = "7.10.3077";
		string schemaVersion = "2.0";
		string versionName = "2003";
		VSVersion version = VSVersion.VS71;

		Hashtable m_Tools;
		Kernel m_Kernel;

		protected string SolutionVersion
		{
			get
			{
				return this.solutionVersion;
			}
			set
			{
				this.solutionVersion = value;
			}
		}
		protected string ProductVersion
		{
			get
			{
				return this.productVersion;
			}
			set
			{
				this.productVersion = value;
			}
		}
		protected string SchemaVersion
		{
			get
			{
				return this.schemaVersion;
			}
			set
			{
				this.schemaVersion = value;
			}
		}
		protected string VersionName
		{
			get
			{
				return this.versionName;
			}
			set
			{
				this.versionName = value;
			}
		}
		protected VSVersion Version
		{
			get
			{
				return this.version;
			}
			set
			{
				this.version = value;
			}
		}

		#endregion

		#region Constructors

		public VS2003Target()
		{
			m_Tools = new Hashtable();

			m_Tools["C#"] = new ToolInfo("C#", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "csproj", "CSHARP");
			m_Tools["VB.NET"] = new ToolInfo("VB.NET", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", "vbproj", "VisualBasic");
		}

		#endregion

		#region Private Methods

		private string MakeRefPath(ProjectNode project)
		{
			string ret = "";
			foreach(ReferencePathNode node in project.ReferencePaths)
			{
				try
				{
					string fullPath = Helper.ResolvePath(node.Path);
					if(ret.Length < 1)
					{
						ret = fullPath;
					}
					else
					{
						ret += ";" + fullPath;
					}
				}
				catch(ArgumentException)
				{
					m_Kernel.Log.Write(LogType.Warning, "Could not resolve reference path: {0}", node.Path);
				}
			}

			return ret;
		}

		private void WriteProject(SolutionNode solution, ProjectNode project)
		{
			if(!m_Tools.ContainsKey(project.Language))
			{
				throw new UnknownLanguageException("Unknown .NET language: " + project.Language);
			}

			ToolInfo toolInfo = (ToolInfo)m_Tools[project.Language];
			string projectFile = Helper.MakeFilePath(project.FullPath, project.Name, toolInfo.FileExtension);
			StreamWriter ps = new StreamWriter(projectFile);

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(projectFile));

			using(ps)
			{
				ps.WriteLine("<VisualStudioProject>");
				ps.WriteLine("\t<{0}", toolInfo.XmlTag);
				ps.WriteLine("\t\tProjectType = \"Local\"");
				ps.WriteLine("\t\tProductVersion = \"{0}\"", this.ProductVersion);
				ps.WriteLine("\t\tSchemaVersion = \"{0}\"", this.SchemaVersion);
				ps.WriteLine("\t\tProjectGuid = \"{{{0}}}\"", project.Guid.ToString().ToUpper());
				ps.WriteLine("\t>");

				ps.WriteLine("\t\t<Build>");
                
				ps.WriteLine("\t\t\t<Settings");
				ps.WriteLine("\t\t\t\tApplicationIcon = \"{0}\"",project.AppIcon);
				ps.WriteLine("\t\t\t\tAssemblyKeyContainerName = \"\"");
				ps.WriteLine("\t\t\t\tAssemblyName = \"{0}\"", project.AssemblyName);
				ps.WriteLine("\t\t\t\tAssemblyOriginatorKeyFile = \"\"");
				ps.WriteLine("\t\t\t\tDefaultClientScript = \"JScript\"");
				ps.WriteLine("\t\t\t\tDefaultHTMLPageLayout = \"Grid\"");
				ps.WriteLine("\t\t\t\tDefaultTargetSchema = \"IE50\"");
				ps.WriteLine("\t\t\t\tDelaySign = \"false\"");

				if(this.Version == VSVersion.VS70)
					ps.WriteLine("\t\t\t\tNoStandardLibraries = \"false\"");

				ps.WriteLine("\t\t\t\tOutputType = \"{0}\"", project.Type.ToString());
				ps.WriteLine("\t\t\t\tRootNamespace = \"{0}\"", project.RootNamespace);
				ps.WriteLine("\t\t\t\tStartupObject = \"{0}\"", project.StartupObject);
				ps.WriteLine("\t\t\t>");

				foreach(ConfigurationNode conf in project.Configurations)
				{
					ps.WriteLine("\t\t\t\t<Config");
					ps.WriteLine("\t\t\t\t\tName = \"{0}\"", conf.Name);
					ps.WriteLine("\t\t\t\t\tAllowUnsafeBlocks = \"{0}\"", conf.Options["AllowUnsafe"]);
					ps.WriteLine("\t\t\t\t\tBaseAddress = \"{0}\"", conf.Options["BaseAddress"]);
					ps.WriteLine("\t\t\t\t\tCheckForOverflowUnderflow = \"{0}\"", conf.Options["CheckUnderflowOverflow"]);
					ps.WriteLine("\t\t\t\t\tConfigurationOverrideFile = \"\"");
					ps.WriteLine("\t\t\t\t\tDefineConstants = \"{0}\"", conf.Options["CompilerDefines"]);
					ps.WriteLine("\t\t\t\t\tDocumentationFile = \"{0}\"", GetXmlDocFile(project, conf));//default to the assembly name
					ps.WriteLine("\t\t\t\t\tDebugSymbols = \"{0}\"", conf.Options["DebugInformation"]);
					ps.WriteLine("\t\t\t\t\tFileAlignment = \"{0}\"", conf.Options["FileAlignment"]);
					ps.WriteLine("\t\t\t\t\tIncrementalBuild = \"{0}\"", conf.Options["IncrementalBuild"]);
                    
					if(this.Version == VSVersion.VS71)
					{
						ps.WriteLine("\t\t\t\t\tNoStdLib = \"{0}\"", conf.Options["NoStdLib"]);
						ps.WriteLine("\t\t\t\t\tNoWarn = \"{0}\"", conf.Options["SupressWarnings"]);
					}

					ps.WriteLine("\t\t\t\t\tOptimize = \"{0}\"", conf.Options["OptimizeCode"]);                    
					ps.WriteLine("\t\t\t\t\tOutputPath = \"{0}\"", 
						Helper.EndPath(Helper.NormalizePath(conf.Options["OutputPath"].ToString())));
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

					if(solution.ProjectsTable.ContainsKey(refr.Name))
					{
						ProjectNode refProject = (ProjectNode)solution.ProjectsTable[refr.Name];
						ps.WriteLine("\t\t\t\t\tProject = \"{{{0}}}\"", refProject.Guid.ToString().ToUpper());
						ps.WriteLine("\t\t\t\t\tPackage = \"{0}\"", toolInfo.Guid.ToString().ToUpper());
					}
					else
					{
						if(refr.Path != null)
						{
							ps.WriteLine("\t\t\t\t\tHintPath = \"{0}\"", Helper.MakeFilePath(refr.Path, refr.Name, "dll"));
						}

					}
                    
					if(refr.LocalCopySpecified)
					{
						ps.WriteLine("\t\t\t\t\tPrivate = \"{0}\"",refr.LocalCopy);
					}

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
//					string delimiter = ".";
//					string [] split = file.Split(delimiter.ToCharArray());
////					foreach (string s in split) 
////					{
////						
////						Console.WriteLine("-{0}-", s);
////					}
//					Console.WriteLine(file.ToString());
//					Console.WriteLine("Split: " + split[split.Length-2]+".resx");
//					split[split.Length-1] = ".resx";
					
					//if (File.Exists(split[split.Length-2]+".resx"))
//					if (File.Exists(split.ToString()))
//					{
//						ps.WriteLine("\t\t\t\t\tSubType = \"Form\"");
//					}
//					else
//					{
						ps.WriteLine("\t\t\t\t\tSubType = \"Code\"");
					//}
					ps.WriteLine("\t\t\t\t\tBuildAction = \"{0}\"", project.Files.GetBuildAction(file));
					ps.WriteLine("\t\t\t\t/>");
				}
				ps.WriteLine("\t\t\t</Include>");
                
				ps.WriteLine("\t\t</Files>");
				ps.WriteLine("\t</{0}>", toolInfo.XmlTag);
				ps.WriteLine("</VisualStudioProject>");
			}

			ps = new StreamWriter(projectFile + ".user");
			using(ps)
			{
				ps.WriteLine("<VisualStudioProject>");
				ps.WriteLine("\t<{0}>", toolInfo.XmlTag);
				ps.WriteLine("\t\t<Build>");

				ps.WriteLine("\t\t\t<Settings ReferencePath=\"{0}\">", MakeRefPath(project));
				foreach(ConfigurationNode conf in project.Configurations)
				{
					ps.WriteLine("\t\t\t\t<Config");
					ps.WriteLine("\t\t\t\t\tName = \"{0}\"", conf.Name);
					ps.WriteLine("\t\t\t\t/>");
				}
				ps.WriteLine("\t\t\t</Settings>");

				ps.WriteLine("\t\t</Build>");
				ps.WriteLine("\t</{0}>", toolInfo.XmlTag);
				ps.WriteLine("</VisualStudioProject>");
			}

			m_Kernel.CurrentWorkingDirectory.Pop();
		}

		public static string GetXmlDocFile(ProjectNode project, ConfigurationNode conf) 
		{
			if( conf == null )
			{
				throw new ArgumentNullException("conf");
			}
			if( project == null )
			{
				throw new ArgumentNullException("project");
			}
			if(!(bool)conf.Options["GenerateXmlDocFile"]) //default to none, if the generate option is false
			{
				return string.Empty;
			}
			string docFile = (string)conf.Options["XmlDocFile"];
			if(docFile != null && docFile.Length == 0)//default to assembly name if not specified
			{
				return Path.GetFileNameWithoutExtension(project.AssemblyName) + ".xml";
			}
			return docFile;
		}

		private void WriteSolution(SolutionNode solution)
		{
			m_Kernel.Log.Write("Creating Visual Studio {0} solution and project files", this.VersionName);

			foreach(ProjectNode project in solution.Projects)
			{
				if(m_Kernel.AllowProject(project.FilterGroups)) 
				{
					m_Kernel.Log.Write("...Creating project: {0}", project.Name);
					WriteProject(solution, project);
				}
			}

			m_Kernel.Log.Write("");
			string solutionFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "sln");
			StreamWriter ss = new StreamWriter(solutionFile);

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(solutionFile));
            
			using(ss)
			{
				ss.WriteLine("Microsoft Visual Studio Solution File, Format Version {0}", this.SolutionVersion);
				foreach(ProjectNode project in solution.Projects)
				{
					if(!m_Tools.ContainsKey(project.Language))
					{
						throw new UnknownLanguageException("Unknown .NET language: " + project.Language);
					}

					ToolInfo toolInfo = (ToolInfo)m_Tools[project.Language];
                
					string path = Helper.MakePathRelativeTo(solution.FullPath, project.FullPath);
					ss.WriteLine("Project(\"{0}\") = \"{1}\", \"{2}\", \"{{{3}}}\"",
						toolInfo.Guid, project.Name, Helper.MakeFilePath(path, project.Name,
						toolInfo.FileExtension), project.Guid.ToString().ToUpper());

					ss.WriteLine("\tProjectSection(ProjectDependencies) = postProject");
					ss.WriteLine("\tEndProjectSection");

					ss.WriteLine("EndProject");
				}

				ss.WriteLine("Global");

				ss.WriteLine("\tGlobalSection(SolutionConfiguration) = preSolution");
				foreach(ConfigurationNode conf in solution.Configurations)
				{
					ss.WriteLine("\t\t{0} = {0}", conf.Name);
				}
				ss.WriteLine("\tEndGlobalSection");

				ss.WriteLine("\tGlobalSection(ProjectDependencies) = postSolution");
				foreach(ProjectNode project in solution.Projects)
				{
					for(int i = 0; i < project.References.Count; i++)
					{
						ReferenceNode refr = (ReferenceNode)project.References[i];
						if(solution.ProjectsTable.ContainsKey(refr.Name))
						{
							ProjectNode refProject = (ProjectNode)solution.ProjectsTable[refr.Name];
							ss.WriteLine("\t\t({{{0}}}).{1} = ({{{2}}})", 
								project.Guid.ToString().ToUpper()
								, i, 
								refProject.Guid.ToString().ToUpper()
								);
						}
					}
				}
				ss.WriteLine("\tEndGlobalSection");

				ss.WriteLine("\tGlobalSection(ProjectConfiguration) = postSolution");
				foreach(ProjectNode project in solution.Projects)
				{
					foreach(ConfigurationNode conf in solution.Configurations)
					{
						ss.WriteLine("\t\t{{{0}}}.{1}.ActiveCfg = {1}|.NET",
							project.Guid.ToString().ToUpper(),
							conf.Name);

						ss.WriteLine("\t\t{{{0}}}.{1}.Build.0 = {1}|.NET",
							project.Guid.ToString().ToUpper(),
							conf.Name);
					}
				}
				ss.WriteLine("\tEndGlobalSection");

				if(solution.Files != null)
				{
					ss.WriteLine("\tGlobalSection(SolutionItems) = postSolution");
					foreach(string file in solution.Files)
					{
						ss.WriteLine("\t\t{0} = {0}", file);
					}
					ss.WriteLine("\tEndGlobalSection");
				}

				ss.WriteLine("EndGlobal");
			}

			m_Kernel.CurrentWorkingDirectory.Pop();
		}

		private void CleanProject(ProjectNode project)
		{
			m_Kernel.Log.Write("...Cleaning project: {0}", project.Name);

			ToolInfo toolInfo = (ToolInfo)m_Tools[project.Language];
			string projectFile = Helper.MakeFilePath(project.FullPath, project.Name, toolInfo.FileExtension);
			string userFile = projectFile + ".user";
            
			Helper.DeleteIfExists(projectFile);
			Helper.DeleteIfExists(userFile);
		}

		private void CleanSolution(SolutionNode solution)
		{
			m_Kernel.Log.Write("Cleaning Visual Studio {0} solution and project files", this.VersionName, solution.Name);

			string slnFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "sln");
			string suoFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "suo");
            
			Helper.DeleteIfExists(slnFile);
			Helper.DeleteIfExists(suoFile);

			foreach(ProjectNode project in solution.Projects)
			{
				CleanProject(project);
			}

			m_Kernel.Log.Write("");
		}

		#endregion

		#region ITarget Members

		public virtual void Write(Kernel kern)
		{
			if( kern == null )
			{
				throw new ArgumentNullException("kern");
			}
			m_Kernel = kern;
			foreach(SolutionNode sol in m_Kernel.Solutions)
			{
				WriteSolution(sol);
			}
			m_Kernel = null;
		}

		public virtual void Clean(Kernel kern)
		{
			if( kern == null )
			{
				throw new ArgumentNullException("kern");
			}
			m_Kernel = kern;
			foreach(SolutionNode sol in m_Kernel.Solutions)
			{
				CleanSolution(sol);
			}
			m_Kernel = null;
		}

		public virtual string Name
		{
			get
			{
				return "vs2003";
			}
		}

		#endregion
	}
}
