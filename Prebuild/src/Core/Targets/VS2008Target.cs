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
 * $Author: borrillis $
 * $Date: 2007-05-24 10:03:16 -0600 (Thu, 24 May 2007) $
 * $Revision: 243 $
 */
#endregion

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Text;

using Prebuild.Core.Attributes;
using Prebuild.Core.Interfaces;
using Prebuild.Core.Nodes;
using Prebuild.Core.Utilities;
using System.CodeDom.Compiler;

namespace Prebuild.Core.Targets
{

    /// <summary>
    /// 
    /// </summary>
    [Target("vs2008")]
    public class VS2008Target : ITarget
    {
        #region Inner Classes

        #endregion

        #region Fields

        string solutionVersion = "10.00";
        string productVersion = "9.0.21022";
        string schemaVersion = "2.0";
        string versionName = "Visual C# 2008";
        VSVersion version = VSVersion.VS90;

        Hashtable tools;
        Kernel kernel;

        /// <summary>
        /// Gets or sets the solution version.
        /// </summary>
        /// <value>The solution version.</value>
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
        /// <summary>
        /// Gets or sets the product version.
        /// </summary>
        /// <value>The product version.</value>
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
        /// <summary>
        /// Gets or sets the schema version.
        /// </summary>
        /// <value>The schema version.</value>
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
        /// <summary>
        /// Gets or sets the name of the version.
        /// </summary>
        /// <value>The name of the version.</value>
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
        /// <summary>
        /// Gets or sets the version.
        /// </summary>
        /// <value>The version.</value>
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

        /// <summary>
        /// Initializes a new instance of the <see cref="VS2008Target"/> class.
        /// </summary>
        public VS2008Target()
        {
            this.tools = new Hashtable();

            this.tools["C#"] = new ToolInfo("C#", "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}", "csproj", "CSHARP", "$(MSBuildBinPath)\\Microsoft.CSHARP.Targets");
            this.tools["Database"] = new ToolInfo("Database", "{4F174C21-8C12-11D0-8340-0000F80270F8}", "dbp", "UNKNOWN");
            this.tools["Boo"] = new ToolInfo("Boo", "{45CEA7DC-C2ED-48A6-ACE0-E16144C02365}", "booproj", "Boo", "$(BooBinPath)\\Boo.Microsoft.Build.targets");
            this.tools["VisualBasic"] = new ToolInfo("VisualBasic", "{F184B08F-C81C-45F6-A57F-5ABD9991F28F}", "vbproj", "VisualBasic", "$(MSBuildBinPath)\\Microsoft.VisualBasic.Targets");
        }

        #endregion

        #region Private Methods

        private string MakeRefPath(ProjectNode project)
        {
            string ret = "";
            foreach (ReferencePathNode node in project.ReferencePaths)
            {
                try
                {
                    string fullPath = Helper.ResolvePath(node.Path);
                    if (ret.Length < 1)
                    {
                        ret = fullPath;
                    }
                    else
                    {
                        ret += ";" + fullPath;
                    }
                }
                catch (ArgumentException)
                {
                    this.kernel.Log.Write(LogType.Warning, "Could not resolve reference path: {0}", node.Path);
                }
            }

            return ret;
        }

        private void WriteProject(SolutionNode solution, ProjectNode project)
        {
            if (!tools.ContainsKey(project.Language))
            {
                throw new UnknownLanguageException("Unknown .NET language: " + project.Language);
            }

            ToolInfo toolInfo = (ToolInfo)tools[project.Language];
            string projectFile = Helper.MakeFilePath(project.FullPath, project.Name, toolInfo.FileExtension);
            StreamWriter ps = new StreamWriter(projectFile);

            kernel.CurrentWorkingDirectory.Push();
            Helper.SetCurrentDir(Path.GetDirectoryName(projectFile));

            #region Project File
            using (ps)
            {
                ps.WriteLine("<Project DefaultTargets=\"Build\" xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\" ToolsVersion=\"3.5\">");
                ps.WriteLine("  <PropertyGroup>");
                ps.WriteLine("    <ProjectType>Local</ProjectType>");
                ps.WriteLine("    <ProductVersion>{0}</ProductVersion>", this.ProductVersion);
                ps.WriteLine("    <SchemaVersion>{0}</SchemaVersion>", this.SchemaVersion);
                ps.WriteLine("    <ProjectGuid>{{{0}}}</ProjectGuid>", project.Guid.ToString().ToUpper());

                ps.WriteLine("    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>");
                ps.WriteLine("    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>");
                ps.WriteLine("    <ApplicationIcon>{0}</ApplicationIcon>", project.AppIcon);
                ps.WriteLine("    <AssemblyKeyContainerName>");
                ps.WriteLine("    </AssemblyKeyContainerName>");
                ps.WriteLine("    <AssemblyName>{0}</AssemblyName>", project.AssemblyName);
                foreach (ConfigurationNode conf in project.Configurations)
                {
                    if (conf.Options.KeyFile != "")
                    {
                        ps.WriteLine("    <AssemblyOriginatorKeyFile>{0}</AssemblyOriginatorKeyFile>", conf.Options.KeyFile);
                        ps.WriteLine("    <SignAssembly>true</SignAssembly>");
                        break;
                    }
                }
                ps.WriteLine("    <DefaultClientScript>JScript</DefaultClientScript>");
                ps.WriteLine("    <DefaultHTMLPageLayout>Grid</DefaultHTMLPageLayout>");
                ps.WriteLine("    <DefaultTargetSchema>IE50</DefaultTargetSchema>");
                ps.WriteLine("    <DelaySign>false</DelaySign>");
                ps.WriteLine("    <TargetFrameworkVersion>{0}</TargetFrameworkVersion>", project.FrameworkVersion.ToString().Replace("_", "."));

                ps.WriteLine("    <OutputType>{0}</OutputType>", project.Type == ProjectType.Web ? ProjectType.Library.ToString() : project.Type.ToString());
                ps.WriteLine("    <AppDesignerFolder>{0}</AppDesignerFolder>", project.DesignerFolder);
                ps.WriteLine("    <RootNamespace>{0}</RootNamespace>", project.RootNamespace);
                ps.WriteLine("    <StartupObject>{0}</StartupObject>", project.StartupObject);
                ps.WriteLine("    <FileUpgradeFlags>");
                ps.WriteLine("    </FileUpgradeFlags>");

                ps.WriteLine("  </PropertyGroup>");

                foreach (ConfigurationNode conf in project.Configurations)
                {
                    ps.Write("  <PropertyGroup ");
                    ps.WriteLine("Condition=\" '$(Configuration)|$(Platform)' == '{0}|AnyCPU' \">", conf.Name);
                    ps.WriteLine("    <AllowUnsafeBlocks>{0}</AllowUnsafeBlocks>", conf.Options["AllowUnsafe"]);
                    ps.WriteLine("    <BaseAddress>{0}</BaseAddress>", conf.Options["BaseAddress"]);
                    ps.WriteLine("    <CheckForOverflowUnderflow>{0}</CheckForOverflowUnderflow>", conf.Options["CheckUnderflowOverflow"]);
                    ps.WriteLine("    <ConfigurationOverrideFile>");
                    ps.WriteLine("    </ConfigurationOverrideFile>");
                    ps.WriteLine("    <DefineConstants>{0}</DefineConstants>", conf.Options["CompilerDefines"]);
                    ps.WriteLine("    <DocumentationFile>{0}</DocumentationFile>", Helper.NormalizePath(conf.Options["XmlDocFile"].ToString()));
                    ps.WriteLine("    <DebugSymbols>{0}</DebugSymbols>", conf.Options["DebugInformation"]);
                    ps.WriteLine("    <FileAlignment>{0}</FileAlignment>", conf.Options["FileAlignment"]);
                    ps.WriteLine("    <Optimize>{0}</Optimize>", conf.Options["OptimizeCode"]);
                    ps.WriteLine("    <OutputPath>{0}</OutputPath>",
                        Helper.EndPath(Helper.NormalizePath(conf.Options["OutputPath"].ToString())));
                    ps.WriteLine("    <RegisterForComInterop>{0}</RegisterForComInterop>", conf.Options["RegisterComInterop"]);
                    ps.WriteLine("    <RemoveIntegerChecks>{0}</RemoveIntegerChecks>", conf.Options["RemoveIntegerChecks"]);
                    ps.WriteLine("    <TreatWarningsAsErrors>{0}</TreatWarningsAsErrors>", conf.Options["WarningsAsErrors"]);
                    ps.WriteLine("    <WarningLevel>{0}</WarningLevel>", conf.Options["WarningLevel"]);
                    ps.WriteLine("    <NoWarn>{0}</NoWarn>", conf.Options["SuppressWarnings"]);
                    ps.WriteLine("  </PropertyGroup>");
                }

                //ps.WriteLine("      </Settings>");

                // Assembly References
                ps.WriteLine("  <ItemGroup>");
                foreach (ReferenceNode refr in project.References)
                {
                    if (!solution.ProjectsTable.ContainsKey(refr.Name))
                    {
                        ps.Write("    <Reference");
                        ps.Write(" Include=\"");
                        ps.Write(refr.Name);
                        ps.WriteLine("\">");
                        ps.Write("        <Name>");
                        ps.Write(refr.Name);
                        ps.WriteLine("</Name>");

                        // TODO: Allow reference to *.exe files
                        ps.WriteLine("      <HintPath>{0}</HintPath>", Helper.MakePathRelativeTo(project.FullPath, refr.Path + "\\" + refr.Name + ".dll"));
                        ps.WriteLine("    </Reference>");
                    }
                }
                ps.WriteLine("  </ItemGroup>");

                //Project References
                ps.WriteLine("  <ItemGroup>");
                foreach (ReferenceNode refr in project.References)
                {
                    if (solution.ProjectsTable.ContainsKey(refr.Name))
                    {
                        ProjectNode refProject = (ProjectNode)solution.ProjectsTable[refr.Name];
                        // TODO: Allow reference to visual basic projects
                        ps.WriteLine("    <ProjectReference Include=\"{0}\">", Helper.MakePathRelativeTo(project.FullPath, Helper.MakeFilePath(refProject.FullPath, refProject.Name, "csproj")));
                        //<ProjectReference Include="..\..\RealmForge\Utility\RealmForge.Utility.csproj">
                        ps.WriteLine("      <Name>{0}</Name>", refProject.Name);
                        //  <Name>RealmForge.Utility</Name>
                        ps.WriteLine("      <Project>{{{0}}}</Project>", refProject.Guid.ToString().ToUpper());
                        //  <Project>{6880D1D3-69EE-461B-B841-5319845B20D3}</Project>
                        ps.WriteLine("      <Package>{0}</Package>", toolInfo.Guid.ToString().ToUpper());
                        //  <Package>{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</Package>
                        ps.WriteLine("    </ProjectReference>");
                        //</ProjectReference>
                    }
                    else
                    {
                    }
                }
                ps.WriteLine("  </ItemGroup>");

                //                ps.WriteLine("    </Build>");
                ps.WriteLine("  <ItemGroup>");

                //                ps.WriteLine("      <Include>");
                ArrayList list = new ArrayList();
                foreach (string file in project.Files)
                {
                    //					if (file == "Properties\\Bind.Designer.cs")
                    //					{
                    //						Console.WriteLine("Wait a minute!");
                    //						Console.WriteLine(project.Files.GetSubType(file).ToString());
                    //					}

                    if (project.Files.GetSubType(file) != SubType.Code && project.Files.GetSubType(file) != SubType.Settings && project.Files.GetSubType(file) != SubType.Designer)
                    {
                        ps.WriteLine("    <EmbeddedResource Include=\"{0}\">", file.Substring(0, file.LastIndexOf('.')) + ".resx");

                        int slash = file.LastIndexOf('\\');
                        if (slash == -1)
                        {
                            ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", file);
                        }
                        else
                        {
                            ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", file.Substring(slash + 1, file.Length - slash - 1));
                        }
                        ps.WriteLine("      <SubType>Designer</SubType>");
                        ps.WriteLine("    </EmbeddedResource>");
                        //
                    }

                    if (project.Files.GetSubType(file) != SubType.Code && project.Files.GetSubType(file) == SubType.Designer)
                    {
                        ps.WriteLine("    <EmbeddedResource Include=\"{0}\">", file.Substring(0, file.LastIndexOf('.')) + ".resx");
                        ps.WriteLine("      <SubType>" + project.Files.GetSubType(file) + "</SubType>");
                        ps.WriteLine("      <Generator>ResXFileCodeGenerator</Generator>");
                        ps.WriteLine("      <LastGenOutput>Resources.Designer.cs</LastGenOutput>");
                        ps.WriteLine("    </EmbeddedResource>");
                        ps.WriteLine("    <Compile Include=\"{0}\">", file.Substring(0, file.LastIndexOf('.')) + ".Designer.cs");
                        ps.WriteLine("      <AutoGen>True</AutoGen>");
                        ps.WriteLine("      <DesignTime>True</DesignTime>");
                        ps.WriteLine("      <DependentUpon>Resources.resx</DependentUpon>");
                        ps.WriteLine("    </Compile>");
                        list.Add(file.Substring(0, file.LastIndexOf('.')) + ".Designer.cs");
                    }
                    if (project.Files.GetSubType(file).ToString() == "Settings")
                    {
                        //Console.WriteLine("File: " + file);
                        //Console.WriteLine("Last index: " + file.LastIndexOf('.'));
                        //Console.WriteLine("Length: " + file.Length);
                        ps.Write("    <{0} ", project.Files.GetBuildAction(file));
                        ps.WriteLine("Include=\"{0}\">", file);
                        int slash = file.LastIndexOf('\\');
                        string fileName = file.Substring(slash + 1, file.Length - slash - 1);
                        if (project.Files.GetBuildAction(file) == BuildAction.None)
                        {
                            ps.WriteLine("      <Generator>SettingsSingleFileGenerator</Generator>");

                            //Console.WriteLine("FileName: " + fileName);
                            //Console.WriteLine("FileNameMain: " + fileName.Substring(0, fileName.LastIndexOf('.')));
                            //Console.WriteLine("FileNameExt: " + fileName.Substring(fileName.LastIndexOf('.'), fileName.Length - fileName.LastIndexOf('.')));
                            if (slash == -1)
                            {
                                ps.WriteLine("      <LastGenOutput>{0}</LastGenOutput>", fileName.Substring(0, fileName.LastIndexOf('.')) + ".Designer.cs");
                            }
                            else
                            {
                                ps.WriteLine("      <LastGenOutput>{0}</LastGenOutput>", fileName.Substring(0, fileName.LastIndexOf('.')) + ".Designer.cs");
                            }
                        }
                        else
                        {
                            ps.WriteLine("      <SubType>Code</SubType>");
                            ps.WriteLine("      <AutoGen>True</AutoGen>");
                            ps.WriteLine("      <DesignTimeSharedInput>True</DesignTimeSharedInput>");
                            string fileNameShort = fileName.Substring(0, fileName.LastIndexOf('.'));
                            string fileNameShorter = fileNameShort.Substring(0, fileNameShort.LastIndexOf('.'));
                            ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", fileNameShorter + ".settings");
                        }
                        ps.WriteLine("    </{0}>", project.Files.GetBuildAction(file));
                    }
                    else if (project.Files.GetSubType(file) != SubType.Designer)
                    {
                        if (!list.Contains(file))
                        {
                            ps.Write("    <{0} ", project.Files.GetBuildAction(file));

                            int startPos = 0;
                            if (project.Files.GetPreservePath(file))
                            {
                                while ((@"./\").IndexOf(file.Substring(startPos, 1)) != -1)
                                    startPos++;

                            }
                            else
                            {
                                startPos = file.LastIndexOf(Path.GetFileName(file));
                            }
                            ps.WriteLine("Include=\"{0}\">", Helper.NormalizePath(file));


                            if (file.Contains("Designer.cs"))
                            {
                                string d = ".Designer.cs";
                                int index = file.Contains("\\") ? file.IndexOf("\\") + 1 : 0;
                                ps.WriteLine("      <DependentUpon>{0}</DependentUpon>", file.Substring(index, file.Length - index - d.Length) + ".cs");
                            }

                            if (project.Files.GetIsLink(file))
                            {
                                string alias = project.Files.GetLinkPath(file);
                                alias += file.Substring(startPos);
                                alias = Helper.NormalizePath(alias);
                                ps.WriteLine("      <Link>{0}</Link>", alias);
                            }
                            else if (project.Files.GetBuildAction(file) != BuildAction.None)
                            {
                                if (project.Files.GetBuildAction(file) != BuildAction.EmbeddedResource)
                                {
                                    ps.WriteLine("      <SubType>{0}</SubType>", project.Files.GetSubType(file));
                                }
                            }

                            if (project.Files.GetCopyToOutput(file) != CopyToOutput.Never)
                            {
                                ps.WriteLine("      <CopyToOutputDirectory>{0}</CopyToOutputDirectory>", project.Files.GetCopyToOutput(file));
                            }

                            ps.WriteLine("    </{0}>", project.Files.GetBuildAction(file));
                        }
                    }
                }

                ps.WriteLine("  </ItemGroup>");
                ps.WriteLine("  <Import Project=\"" + toolInfo.ImportProject + "\" />");
                ps.WriteLine("  <PropertyGroup>");
                ps.WriteLine("    <PreBuildEvent>");
                ps.WriteLine("    </PreBuildEvent>");
                ps.WriteLine("    <PostBuildEvent>");
                ps.WriteLine("    </PostBuildEvent>");
                ps.WriteLine("  </PropertyGroup>");
                ps.WriteLine("</Project>");
            }
            #endregion

            #region User File

            ps = new StreamWriter(projectFile + ".user");
            using (ps)
            {
                ps.WriteLine("<Project xmlns=\"http://schemas.microsoft.com/developer/msbuild/2003\">");
                //ps.WriteLine( "<VisualStudioProject>" );
                //ps.WriteLine("  <{0}>", toolInfo.XMLTag);
                //ps.WriteLine("    <Build>");
                ps.WriteLine("  <PropertyGroup>");
                //ps.WriteLine("      <Settings ReferencePath=\"{0}\">", MakeRefPath(project));
                ps.WriteLine("    <Configuration Condition=\" '$(Configuration)' == '' \">Debug</Configuration>");
                ps.WriteLine("    <Platform Condition=\" '$(Platform)' == '' \">AnyCPU</Platform>");
                ps.WriteLine("    <ReferencePath>{0}</ReferencePath>", MakeRefPath(project));
                ps.WriteLine("    <LastOpenVersion>{0}</LastOpenVersion>", this.ProductVersion);
                ps.WriteLine("    <ProjectView>ProjectFiles</ProjectView>");
                ps.WriteLine("    <ProjectTrust>0</ProjectTrust>");
                ps.WriteLine("  </PropertyGroup>");
                foreach (ConfigurationNode conf in project.Configurations)
                {
                    ps.Write("  <PropertyGroup");
                    ps.Write(" Condition = \" '$(Configuration)|$(Platform)' == '{0}|AnyCPU' \"", conf.Name);
                    ps.WriteLine(" />");
                }
                //ps.WriteLine("      </Settings>");

                //ps.WriteLine("    </Build>");
                //ps.WriteLine("  </{0}>", toolInfo.XMLTag);
                //ps.WriteLine("</VisualStudioProject>");
                ps.WriteLine("</Project>");
            }
            #endregion

            kernel.CurrentWorkingDirectory.Pop();
        }

        private void WriteSolution(SolutionNode solution)
        {
            kernel.Log.Write("Creating {0} solution and project files", this.VersionName);

            foreach (ProjectNode project in solution.Projects)
            {
                kernel.Log.Write("...Creating project: {0}", project.Name);
                WriteProject(solution, project);
            }

            foreach (DatabaseProjectNode project in solution.DatabaseProjects)
            {
                kernel.Log.Write("...Creating database project: {0}", project.Name);
                WriteDatabaseProject(solution, project);
            }

            kernel.Log.Write("");
            string solutionFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "sln");
            StreamWriter ss = new StreamWriter(solutionFile);

            kernel.CurrentWorkingDirectory.Push();
            Helper.SetCurrentDir(Path.GetDirectoryName(solutionFile));

            using (ss)
            {
                ss.WriteLine("Microsoft Visual Studio Solution File, Format Version {0}", this.SolutionVersion);
                ss.WriteLine("# Visual Studio 2008");

                foreach (ProjectNode project in solution.Projects)
                {
                    WriteProject(solution, ss, project);
                }

                foreach (DatabaseProjectNode dbProject in solution.DatabaseProjects)
                {
                    WriteProject(solution, ss, dbProject);
                }

                if (solution.Files != null)
                {
                    WriteSolutionFolder(solution.Files, ss);
                }

                ss.WriteLine("Global");

                ss.WriteLine("  GlobalSection(SolutionConfigurationPlatforms) = preSolution");
                foreach (ConfigurationNode conf in solution.Configurations)
                {
                    ss.WriteLine("    {0}|Any CPU = {0}|Any CPU", conf.Name);
                }
                ss.WriteLine("  EndGlobalSection");

                ss.WriteLine("  GlobalSection(ProjectConfigurationPlatforms) = postSolution");
                foreach (ProjectNode project in solution.Projects)
                {
                    WriteConfigurationLines(solution, ss, project);
                }
                ss.WriteLine("  EndGlobalSection");

                ss.WriteLine("EndGlobal");
            }

            kernel.CurrentWorkingDirectory.Pop();
        }

        private static void WriteConfigurationLines(SolutionNode solution, StreamWriter ss, ProjectNode project)
        {
            foreach (ConfigurationNode conf in solution.Configurations)
            {
                ss.WriteLine("    {{{0}}}.{1}|Any CPU.ActiveCfg = {1}|Any CPU",
                    project.Guid.ToString().ToUpper(),
                    conf.Name);

                ss.WriteLine("    {{{0}}}.{1}|Any CPU.Build.0 = {1}|Any CPU",
                    project.Guid.ToString().ToUpper(),
                    conf.Name);
            }
        }

        private static void WriteSolutionFolder(FilesNode files, StreamWriter ss)
        {
            ss.WriteLine("Project(\"{0}\") = \"Solution Items\", \"Solution Items\", \"{1}\"", "{2150E333-8FDC-42A3-9474-1A3956D46DE8}", Guid.NewGuid());
            ss.WriteLine("\tProjectSection(SolutionItems) = preProject");
            foreach (string file in files)
                ss.WriteLine("\t\t{0} = {0}", file);
            ss.WriteLine("\tEndProjectSection");
            ss.WriteLine("EndProject");
        }

        private void WriteProject(SolutionNode solution, StreamWriter ss, ProjectNode project)
        {
            WriteProject(ss, solution, project.Language, project.Guid, project.Name, project.FullPath);
        }

        private void WriteProject(SolutionNode solution, StreamWriter ss, DatabaseProjectNode dbProject)
        {
            WriteProject(ss, solution, "Database", Guid.NewGuid(), dbProject.Name, dbProject.FullPath);
        }

        const string ProjectDeclarationFormat = "Project(\"{0}\") = \"{1}\", \"{2}\", \"{{{3}}}\"\nEndProject";

        private void WriteProject(StreamWriter ss, SolutionNode solution, string language, Guid guid, string name, string projectFullPath)
        {
            if (!tools.ContainsKey(language))
                throw new UnknownLanguageException("Unknown .NET language: " + language);

            ToolInfo toolInfo = (ToolInfo)tools[language];

            string path = Helper.MakePathRelativeTo(solution.FullPath, projectFullPath);
            ss.WriteLine(ProjectDeclarationFormat,
                toolInfo.Guid, 
                name, 
                Helper.MakeFilePath(path, name, toolInfo.FileExtension), 
                guid.ToString().ToUpper());
        }

        private void WriteDatabaseProject(SolutionNode solution, DatabaseProjectNode project)
        {
            string projectFile = Helper.MakeFilePath(project.FullPath, project.Name, "dbp");
            IndentedTextWriter ps = new IndentedTextWriter(new StreamWriter(projectFile), "   ");

            kernel.CurrentWorkingDirectory.Push();

            Helper.SetCurrentDir(Path.GetDirectoryName(projectFile));

            using (ps)
            {
                ps.WriteLine("# Microsoft Developer Studio Project File - Database Project");
                ps.WriteLine("Begin DataProject = \"{0}\"", project.Name);
                    ps.Indent++;
                    ps.WriteLine("MSDTVersion = \"80\"");
                    // TODO: Use the project.Files property
                    if (ContainsSqlFiles(Path.GetDirectoryName(projectFile)))
                        WriteDatabaseFoldersAndFiles(ps, Path.GetDirectoryName(projectFile));
                    ps.Indent--;
                ps.WriteLine("End");

                ps.Flush();
            }

            kernel.CurrentWorkingDirectory.Pop();
        }

        private bool ContainsSqlFiles(string folder)
        {
            foreach (string file in Directory.GetFiles(folder, "*.sql"))
            {
                return true; // if the folder contains 1 .sql file, that's good enough
            }

            foreach (string child in Directory.GetDirectories(folder))
            {
                if (ContainsSqlFiles(child))
                    return true; // if 1 child folder contains a .sql file, still good enough
            }

            return false;
        }

        private void WriteDatabaseFoldersAndFiles(IndentedTextWriter writer, string folder)
        {
            foreach (string child in Directory.GetDirectories(folder))
            {
                if (ContainsSqlFiles(child))
                {
                    writer.WriteLine("Begin Folder = \"{0}\"", Path.GetFileName(child));
                    writer.Indent++;
                        WriteDatabaseFoldersAndFiles(writer, child);
                    writer.Indent--;
                    writer.WriteLine("End");
                }
            }
            foreach (string file in Directory.GetFiles(folder, "*.sql"))
            {
                writer.WriteLine("Script = \"{0}\"", Path.GetFileName(file));
            }
        }

        private void CleanProject(ProjectNode project)
        {
            kernel.Log.Write("...Cleaning project: {0}", project.Name);

            ToolInfo toolInfo = (ToolInfo)tools[project.Language];
            string projectFile = Helper.MakeFilePath(project.FullPath, project.Name, toolInfo.FileExtension);
            string userFile = projectFile + ".user";

            Helper.DeleteIfExists(projectFile);
            Helper.DeleteIfExists(userFile);
        }

        private void CleanSolution(SolutionNode solution)
        {
            kernel.Log.Write("Cleaning {0} solution and project files", this.VersionName, solution.Name);

            string slnFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "sln");
            string suoFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "suo");

            Helper.DeleteIfExists(slnFile);
            Helper.DeleteIfExists(suoFile);

            foreach (ProjectNode project in solution.Projects)
            {
                CleanProject(project);
            }

            kernel.Log.Write("");
        }

        #endregion

        #region ITarget Members

        /// <summary>
        /// Writes the specified kern.
        /// </summary>
        /// <param name="kern">The kern.</param>
        public virtual void Write(Kernel kern)
        {
            if (kern == null)
            {
                throw new ArgumentNullException("kern");
            }
            kernel = kern;
            foreach (SolutionNode sol in kernel.Solutions)
            {
                WriteSolution(sol);
            }
            kernel = null;
        }

        /// <summary>
        /// Cleans the specified kern.
        /// </summary>
        /// <param name="kern">The kern.</param>
        public virtual void Clean(Kernel kern)
        {
            if (kern == null)
            {
                throw new ArgumentNullException("kern");
            }
            kernel = kern;
            foreach (SolutionNode sol in kernel.Solutions)
            {
                CleanSolution(sol);
            }
            kernel = null;
        }

        /// <summary>
        /// Gets the name.
        /// </summary>
        /// <value>The name.</value>
        public virtual string Name
        {
            get
            {
                return "vs2008";
            }
        }

        #endregion
    }
}
