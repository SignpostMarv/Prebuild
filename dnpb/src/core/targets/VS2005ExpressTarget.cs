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
using System.Runtime.InteropServices;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Nodes;
using DNPreBuild.Core.Utilities;

namespace DNPreBuild.Core.Targets
{
	[Target("vs2005express")]
	public class VS2005ExpressTarget : ITarget
	{

		public VS2005ExpressTarget()
		{
			this.tools["C#"] = new ToolInfo("C#", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "csproj", "CSHARP");
		}

		// Fields
		private Kernel kernel;
		string productVersion = "8.0.40607.16";
		string schemaVersion = "2.0";
		string solutionVersion = "9.00";
		Hashtable tools = new Hashtable();
		VSVersion version = VSVersion.VS80;
		string versionName = "Express 2005";

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

		public virtual void Clean(Kernel kern)
		{
			if( kern == null )
			{
				throw new ArgumentNullException("kern");
			}
			this.kernel = kern;
			foreach (SolutionNode node1 in this.kernel.Solutions)
			{
				this.CleanSolution(node1);
			}
			this.kernel = null;
		}
		private void CleanProject(ProjectNode project)
		{
			object[] objArray1 = new object[] { project.Name } ;
			this.kernel.Log.Write("...Cleaning project: {0}", objArray1);
			ToolInfo info1 = (ToolInfo) this.tools[project.Language];
			string text1 = Helper.MakeFilePath(project.FullPath, project.Name, info1.FileExtension);
			string text2 = text1 + ".user";
			Helper.DeleteIfExists(text1);
			Helper.DeleteIfExists(text2);
		}
		private void CleanSolution(SolutionNode solution)
		{
			object[] objArray1 = new object[] { this.VersionName, solution.Name } ;
			this.kernel.Log.Write("Cleaning Visual {0} solution and project files", objArray1);
			string text1 = Helper.MakeFilePath(solution.FullPath, solution.Name, "sln");
			string text2 = Helper.MakeFilePath(solution.FullPath, solution.Name, "suo");
			Helper.DeleteIfExists(text1);
			Helper.DeleteIfExists(text2);
			foreach (ProjectNode node1 in solution.Projects)
			{
				this.CleanProject(node1);
			}
			this.kernel.Log.Write("");
		}
		private string MakeRefPath(ProjectNode project)
		{
			string text1 = "";
			foreach (ReferencePathNode node1 in project.ReferencePaths)
			{
				try
				{
					string text2 = Helper.ResolvePath(node1.Path);
					if (text1.Length < 1)
					{
						text1 = text2;
						continue;
					}
					text1 = text1 + ";" + text2;
					continue;
				}
				catch (ArgumentException)
				{
					object[] objArray1 = new object[] { node1.Path } ;
					this.kernel.Log.Write(LogType.Warning, "Could not resolve reference path: {0}", objArray1);
					continue;
				}
			}
			return text1;
		}
		public virtual void Write(Kernel kern)
		{
			if( kern == null )
			{
				throw new ArgumentNullException("kern");
			}
			this.kernel = kern;
			foreach (SolutionNode node1 in this.kernel.Solutions)
			{
				this.WriteSolution(node1);
			}
			this.kernel = null;
		}
		private void WriteProject(SolutionNode solution, ProjectNode project)
		{
			if (!this.tools.ContainsKey(project.Language))
			{
				throw new UnknownLanguageException("Unknown .NET language: " + project.Language);
			}
			ToolInfo info1 = (ToolInfo) this.tools[project.Language];
			string text1 = Helper.MakeFilePath(project.FullPath, project.Name, info1.FileExtension);
			StreamWriter writer1 = new StreamWriter(text1);
			this.kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(text1));
			using (StreamWriter writer2 = writer1)
			{
				writer1.WriteLine("<Project DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
				writer1.WriteLine("\t<PropertyGroup>");
				writer1.WriteLine("\t\t<ProjectType>Local</ProjectType>");
				writer1.WriteLine("\t\t<ProductVersion>{0}</ProductVersion>", this.ProductVersion);
				writer1.WriteLine("\t\t<SchemaVersion>{0}</SchemaVersion>", this.SchemaVersion);
				writer1.WriteLine("\t\t<ProjectGuid>{{{0}}}</ProjectGuid>", project.Guid.ToString().ToUpper());
				writer1.WriteLine("\t\t<Configuration Condition = \" '$(Configuration)' == '' \">Debug</Configuration>");
				writer1.WriteLine("\t\t<Platform Condition = \" '$(Platform)' == '' \">AnyCPU</Platform>");
				writer1.WriteLine("\t\t<ApplicationIcon></ApplicationIcon>");
				writer1.WriteLine("\t\t<AssemblyKeyContainerName></AssemblyKeyContainerName>");
				writer1.WriteLine("\t\t<AssemblyName>{0}</AssemblyName>", project.AssemblyName);
				writer1.WriteLine("\t\t<AssemblyOriginatorKeyFile></AssemblyOriginatorKeyFile>");
				writer1.WriteLine("\t\t<DefaultClientScript>JScript</DefaultClientScript>");
				writer1.WriteLine("\t\t<DefaultHTMLPageLayout>Grid</DefaultHTMLPageLayout>");
				writer1.WriteLine("\t\t<DefaultTargetSchema>IE50</DefaultTargetSchema>");
				writer1.WriteLine("\t\t<DelaySign>false</DelaySign>");
				writer1.WriteLine("\t\t<OutputType>{0}</OutputType>", project.Type.ToString());
				writer1.WriteLine("\t\t<RootNamespace>{0}</RootNamespace>", project.RootNamespace);
				writer1.WriteLine("\t\t<StartupObject>{0}</StartupObject>", project.StartupObject);
				writer1.WriteLine("\t\t<FileUpgradeFlags></FileUpgradeFlags>");
				writer1.WriteLine("\t</PropertyGroup>");
				foreach (ConfigurationNode node1 in project.Configurations)
				{
					writer1.Write("\t<PropertyGroup ");
					writer1.WriteLine("Condition=\" '$(Configuration)|$(Platform)' == '{0}|AnyCPU' \">", node1.Name);
					writer1.WriteLine("\t\t<AllowUnsafeBlocks>{0}</AllowUnsafeBlocks>", node1.Options["AllowUnsafe"]);
					writer1.WriteLine("\t\t<BaseAddress>{0}</BaseAddress>", node1.Options["BaseAddress"]);
					writer1.WriteLine("\t\t<CheckForOverflowUnderflow>{0}</CheckForOverflowUnderflow>", node1.Options["CheckUnderflowOverflow"]);
					writer1.WriteLine("\t\t<ConfigurationOverrideFile></ConfigurationOverrideFile>");
					writer1.WriteLine("\t\t<DefineConstants>{0}</DefineConstants>", node1.Options["CompilerDefines"]);
					writer1.WriteLine("\t\t<DocumentationFile>{0}</DocumentationFile>", VS2003Target.GetXmlDocFile(project, node1));
					writer1.WriteLine("\t\t<DebugSymbols>{0}</DebugSymbols>", node1.Options["DebugInformation"]);
					writer1.WriteLine("\t\t<FileAlignment>{0}</FileAlignment>", node1.Options["FileAlignment"]);
					writer1.WriteLine("\t\t<Optimize>{0}</Optimize>", node1.Options["OptimizeCode"]);
					writer1.WriteLine("\t\t<OutputPath>{0}</OutputPath>", Helper.EndPath(Helper.NormalizePath(node1.Options["OutputPath"].ToString())));
					writer1.WriteLine("\t\t<RegisterForComInterop>{0}</RegisterForComInterop>", node1.Options["RegisterComInterop"]);
					writer1.WriteLine("\t\t<RemoveIntegerChecks>{0}</RemoveIntegerChecks>", node1.Options["RemoveIntegerChecks"]);
					writer1.WriteLine("\t\t<TreatWarningsAsErrors>{0}</TreatWarningsAsErrors>", node1.Options["WarningsAsErrors"]);
					writer1.WriteLine("\t\t<WarningLevel>{0}</WarningLevel>", node1.Options["WarningLevel"]);
					writer1.WriteLine("\t</PropertyGroup>");
				}
				writer1.WriteLine("\t<ItemGroup>");
				foreach (ReferenceNode node2 in project.References)
				{
					writer1.Write("\t\t<Reference");
					writer1.WriteLine(" Include = \"{0}\">", node2.Name);
					writer1.WriteLine("\t\t\t<Name>{0}</Name>", node2.Name);
					writer1.Write("\t\t</Reference>");
				}
				writer1.WriteLine("\t</ItemGroup>");
				writer1.WriteLine("\t<ItemGroup>");
				foreach (string text2 in project.Files)
				{
					writer1.Write("\t\t<{0} ", project.Files.GetBuildAction(text2));
					writer1.WriteLine(" Include =\"{0}\">", text2.Replace(@".\", ""));
					writer1.WriteLine("\t\t\t<SubType>Code</SubType>");
					writer1.WriteLine("\t\t</{0}>", project.Files.GetBuildAction(text2));
				}
				writer1.WriteLine("\t</ItemGroup>");
				writer1.WriteLine("\t<Import Project=\"$(MSBuildBinPath)\\Microsoft.CSHARP.Targets\" />");
				writer1.WriteLine("\t<PropertyGroup>");
				writer1.WriteLine("\t\t<PreBuildEvent>");
				writer1.WriteLine("\t\t</PreBuildEvent>");
				writer1.WriteLine("\t\t<PostBuildEvent>");
				writer1.WriteLine("\t\t</PostBuildEvent>");
				writer1.WriteLine("\t</PropertyGroup>");
				writer1.WriteLine("</Project>");
			}
			writer1 = new StreamWriter(text1 + ".user");
			using (StreamWriter writer3 = writer1)
			{
				writer1.WriteLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
				writer1.WriteLine("\t<PropertyGroup>");
				writer1.WriteLine("\t\t<Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>");
				writer1.WriteLine("\t\t<Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>");
				writer1.WriteLine("\t\t<ReferencePath>{0}</ReferencePath>", this.MakeRefPath(project));
				writer1.WriteLine("\t\t<LastOpenVersion>{0}</LastOpenVersion>", this.ProductVersion);
				writer1.WriteLine("\t\t<ProjectView>ProjectFiles</ProjectView>");
				writer1.WriteLine("\t\t<ProjectTrust>0</ProjectTrust>");
				writer1.WriteLine("\t</PropertyGroup>");
				foreach (ConfigurationNode node3 in project.Configurations)
				{
					writer1.Write("\t<PropertyGroup");
					writer1.Write(" Condition = \" '$(Configuration)|$(Platform)' == '{0}|AnyCPU' \"", node3.Name);
					writer1.WriteLine(" />");
				}
				writer1.WriteLine("</Project>");
			}
			this.kernel.CurrentWorkingDirectory.Pop();
		}
		private void WriteSolution(SolutionNode solution)
		{
			object[] objArray1 = new object[] { this.VersionName } ;
			this.kernel.Log.Write("Creating Visual {0} solution and project files", objArray1);
			foreach (ProjectNode node1 in solution.Projects)
			{
				if (this.kernel.AllowProject(node1.FilterGroups))
				{
					object[] objArray2 = new object[] { node1.Name } ;
					this.kernel.Log.Write("...Creating project: {0}", objArray2);
					this.WriteProject(solution, node1);
				}
			}
			this.kernel.Log.Write("");
			string text1 = Helper.MakeFilePath(solution.FullPath, solution.Name, "sln");
			StreamWriter writer1 = new StreamWriter(text1);
			this.kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(text1));
			using (StreamWriter writer2 = writer1)
			{
				writer1.WriteLine("Microsoft Visual Studio Solution File, Format Version {0}", this.SolutionVersion);
				writer1.WriteLine("# Visual C# Express 2005");
				foreach (ProjectNode node2 in solution.Projects)
				{
					if (!this.tools.ContainsKey(node2.Language))
					{
						throw new UnknownLanguageException("Unknown .NET language: " + node2.Language);
					}
					ToolInfo info1 = (ToolInfo) this.tools[node2.Language];
					string text2 = Helper.MakePathRelativeTo(solution.FullPath, node2.FullPath);
					object[] objArray3 = new object[] { info1.Guid, node2.Name, Helper.MakeFilePath(text2, node2.Name, info1.FileExtension), node2.Guid.ToString().ToUpper() } ;
					writer1.WriteLine("Project(\"{0}\") = \"{1}\", \"{2}\", \"{{{3}}}\"", objArray3);
					writer1.WriteLine("EndProject");
				}
				writer1.WriteLine("Global");
				writer1.WriteLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
				foreach (ConfigurationNode node3 in solution.Configurations)
				{
					writer1.WriteLine("\t\t{0}|Any CPU = {0}|Any CPU", node3.Name);
				}
				writer1.WriteLine("\tEndGlobalSection");
				if (solution.Projects.Count > 1)
				{
					writer1.WriteLine("\tGlobalSection(ProjectDependencies) = postSolution");
				}
				foreach (ProjectNode node4 in solution.Projects)
				{
					for (int num1 = 0; num1 < node4.References.Count; num1++)
					{
						ReferenceNode node5 = (ReferenceNode) node4.References[num1];
						if (solution.ProjectsTable.ContainsKey(node5.Name))
						{
							ProjectNode node6 = (ProjectNode) solution.ProjectsTable[node5.Name];
							writer1.WriteLine("\t\t({{{0}}}).{1} = ({{{2}}})", node4.Guid.ToString().ToUpper(), num1, node6.Guid.ToString().ToUpper());
						}
					}
				}
				if (solution.Projects.Count > 1)
				{
					writer1.WriteLine("\tEndGlobalSection");
				}
				writer1.WriteLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
				foreach (ProjectNode node7 in solution.Projects)
				{
					foreach (ConfigurationNode node8 in solution.Configurations)
					{
						writer1.WriteLine("\t\t{{{0}}}.{1}|Any CPU.ActiveCfg = {1}|Any CPU", node7.Guid.ToString().ToUpper(), node8.Name);
						writer1.WriteLine("\t\t{{{0}}}.{1}|Any CPU.Build.0 = {1}|Any CPU", node7.Guid.ToString().ToUpper(), node8.Name);
					}
				}
				writer1.WriteLine("\tEndGlobalSection");
				writer1.WriteLine("\tGlobalSection(SolutionProperties) = preSolution");
				writer1.WriteLine("\t\tHideSolutionNode = FALSE");
				writer1.WriteLine("\tEndGlobalSection");
				writer1.WriteLine("EndGlobal");
			}
			this.kernel.CurrentWorkingDirectory.Pop();
		}
		public virtual string Name
		{
			get
			{
				return "vs2005CSE";
			}
		}
	}
}

