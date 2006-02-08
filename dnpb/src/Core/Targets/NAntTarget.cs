#region BSD License
/*
Copyright (c) 2004 Matthew Holmes (matthew@wildfiregames.com), Dan Moorehead (dan05a@gmail.com)

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
using System.Reflection;
using System.Text.RegularExpressions;

using Prebuild.Core.Attributes;
using Prebuild.Core.Interfaces;
using Prebuild.Core.Nodes;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Targets
{
	/// <summary>
	/// 
	/// </summary>
	[Target("nant")]
	public class NAntTarget : ITarget
	{
		#region Fields

		private Kernel m_Kernel;

		#endregion

		#region Private Methods

		private static string PrependPath(string path)
		{
			string tmpPath = Helper.NormalizePath(path, '/');
			Regex regex = new Regex(@"(\w):/(\w+)");
			Match match = regex.Match(tmpPath);
			if(match.Success || tmpPath[0] == '.' || tmpPath[0] == '/')
			{
				tmpPath = Helper.NormalizePath(tmpPath);
			}
			else
			{
				tmpPath = Helper.NormalizePath("./" + tmpPath);
			}

			return tmpPath;
		}

		private static string BuildReference(SolutionNode solution, ReferenceNode refr)
		{
			string ret = "<include name=\"";
			if(solution.ProjectsTable.ContainsKey(refr.Name))
			{
				//ret += "Project\"";
				//ret += " localcopy=\"" + refr.LocalCopy.ToString() +  "\" refto=\"" + refr.Name + "\" />";
			}
			else
			{
				ProjectNode project = (ProjectNode)refr.Parent;
				string fileRef = FindFileReference(refr.Name, project);

				if(refr.Path != null || fileRef != null)
				{
					//ret += "Assembly\" refto=\"";

					//string finalPath = (refr.Path != null) ? Helper.MakeFilePath(refr.Path, refr.Name, "dll") : fileRef;

					//ret += finalPath;
					//ret += "\" localcopy=\"" + refr.LocalCopy.ToString() + "\" />";
					return ret;
				}

				//ret += "Gac\"";
				//ret += " localcopy=\"" + refr.LocalCopy.ToString() + "\"";
				//ret += " refto=\"";
				try
				{
					Assembly assem = Assembly.LoadWithPartialName(refr.Name);
					if (assem != null)
					{
						ret += "${nant.settings.currentframework.frameworkassemblydirectory}/" + refr.Name + ".dll";
					}
				}
				catch (System.NullReferenceException e)
				{
					e.ToString();
					ret += refr.Name + ".dll";
				}
				ret += "\" />";
			}

			return ret;
		}

		private static string FindFileReference(string refName, ProjectNode project) 
		{
			foreach(ReferencePathNode refPath in project.ReferencePaths) 
			{
				string fullPath = Helper.MakeFilePath(refPath.Path, refName, "dll");

				if(File.Exists(fullPath)) 
				{
					return fullPath;
				}
			}

			return null;
		}

		/// <summary>
		/// Gets the XML doc file.
		/// </summary>
		/// <param name="project">The project.</param>
		/// <param name="conf">The conf.</param>
		/// <returns></returns>
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
			//			if(!(bool)conf.Options["GenerateXmlDocFile"]) //default to none, if the generate option is false
			//			{
			//				return string.Empty;
			//			}
			string docFile = (string)conf.Options["XmlDocFile"];
			if(docFile != null && docFile.Length == 0)//default to assembly name if not specified
			{
				return Path.GetFileNameWithoutExtension(project.AssemblyName) + ".xml";
			}
			return docFile;
		}

		private void WriteProject(SolutionNode solution, ProjectNode project)
		{
			//string csComp = "Mcs";
			//string netRuntime = "Mono";
			if(project.Runtime == ClrRuntime.Microsoft)
			{
				//	csComp = "Csc";
				//	netRuntime = "MsNet";
			}

			string projFile = Helper.MakeFilePath(project.FullPath, project.Name, "build");
			StreamWriter ss = new StreamWriter(projFile);

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(projFile));

			using(ss)
			{
				ss.WriteLine("<?xml version=\"1.0\" ?>");
				ss.WriteLine("<project name=\"{0}\" default=\"build\">", project.Name);

				int count = 0;

				ss.WriteLine("    <target name=\"{0}\">", "build");
				ss.WriteLine("        <echo message=\"Build Directory is ${nant.project.basedir}/${build.dir}\" />");
				ss.WriteLine("        <mkdir dir=\"${nant.project.basedir}/${build.dir}\" />");				
				ss.Write("        <csc");
				
				ss.Write(" target=\"{0}\"", project.Type.ToString().ToLower());
				//					}
				//					ss.WriteLine(" />");
				//					
				//					ss.Write("      <Execution");
				//					ss.Write(" runwithwarnings=\"True\"");
				//					ss.Write(" consolepause=\"True\"");
				//					ss.Write(" runtime=\"{0}\"", netRuntime);
				//					ss.WriteLine(" />");
				//					
				//					ss.Write("      <CodeGeneration");
				//					ss.Write(" compiler=\"{0}\"", csComp);
				//					ss.Write(" warninglevel=\"{0}\"", conf.Options["WarningLevel"]);
				//					ss.Write(" nowarn=\"{0}\"", conf.Options["SuppressWarnings"]);
				ss.Write(" debug=\"{0}\"", "${build.debug}");
				//					ss.Write(" optimize=\"{0}\"", conf.Options["OptimizeCode"]);
				ss.Write(" unsafe=\"{0}\"", "true");
				//					ss.Write(" generateoverflowchecks=\"{0}\"", conf.Options["CheckUnderflowOverflow"]);
				//					ss.Write(" mainclass=\"{0}\"", project.StartupObject);
				//					ss.Write(" target=\"{0}\"", project.Type);
				//					ss.Write(" definesymbols=\"{0}\"", conf.Options["CompilerDefines"]);
				foreach(ConfigurationNode conf in project.Configurations)
				{
					ss.Write(" doc=\"{0}\"", "${nant.project.basedir}/${build.dir}/" + GetXmlDocFile(project, conf));
					break;
				}
				ss.Write(" output=\"{0}", "${nant.project.basedir}/${build.dir}/${nant.project.name}");
				if (project.Type == ProjectType.Library)
				{
					ss.Write(".dll\"");
				}
				else
				{
					ss.Write(".exe\"");
				}
				ss.Write(" win32icon=\"{0}\"", Helper.NormalizePath(project.AppIcon,'/'));
				ss.WriteLine(">");
				ss.WriteLine("            <sources failonempty=\"true\">");
				foreach(string file in project.Files)
				{
					switch(project.Files.GetBuildAction(file))
					{
						case BuildAction.Compile:
							ss.WriteLine("                <include name=\"" + Helper.NormalizePath(PrependPath(file), '/') + "\" />");
							break;
						default:
							break;
					}
				}
				ss.WriteLine("            </sources>");
				ss.WriteLine("            <references basedir=\"${nant.project.basedir}/${build.dir}\">");
				foreach(ReferenceNode refr in project.References)
				{
					ss.WriteLine("                {0}", BuildReference(solution, refr));
				}
				ss.WriteLine("            </references>");
				ss.WriteLine("            <resources>");
				foreach(string file in project.Files)
				{
					switch(project.Files.GetBuildAction(file))
					{
						case BuildAction.EmbeddedResource:
							ss.WriteLine("                {0}", "<include name=\"" + Helper.NormalizePath(PrependPath(file), '/') + "\" />");
							break;
						default:
							break;
					}
				}
				ss.WriteLine("            </resources>");
				ss.WriteLine("        </csc>");
				ss.WriteLine("    </target>");

				ss.WriteLine("    <target name=\"clean\">");
				ss.WriteLine("        <delete dir=\"${bin.dir}\" failonerror=\"false\" />");
				ss.WriteLine("        <delete dir=\"${obj.dir}\" failonerror=\"false\" />");
				ss.WriteLine("    </target>");

				ss.WriteLine("</project>");

				count++;
			}                
			//				ss.WriteLine("  </Configurations>");
			//
			//				ss.Write("  <DeploymentInformation");
			//				ss.Write(" target=\"\"");
			//				ss.Write(" script=\"\"");
			//				ss.Write(" strategy=\"File\"");
			//				ss.WriteLine(">");
			//				ss.WriteLine("    <excludeFiles />");
			//				ss.WriteLine("  </DeploymentInformation>");
			//				
			//				ss.WriteLine("  <Contents>");
			//				foreach(string file in project.Files)
			//				{
			//					string buildAction = "Compile";
			//					switch(project.Files.GetBuildAction(file))
			//					{
			//						case BuildAction.None:
			//							buildAction = "Nothing";
			//							break;
			//
			//						case BuildAction.Content:
			//							buildAction = "Exclude";
			//							break;
			//
			//						case BuildAction.EmbeddedResource:
			//							buildAction = "EmbedAsResource";
			//							break;
			//
			//						default:
			//							buildAction = "Compile";
			//							break;
			//					}
			//
			//					// Sort of a hack, we try and resolve the path and make it relative, if we can.
			//					string filePath = PrependPath(file);
			//					ss.WriteLine("    <File name=\"{0}\" subtype=\"Code\" buildaction=\"{1}\" dependson=\"\" data=\"\" />", filePath, buildAction);
			//				}
			//				ss.WriteLine("  </Contents>");
			//
			//				ss.WriteLine("  <References>");
			//				foreach(ReferenceNode refr in project.References)
			//				{
			//					ss.WriteLine("    {0}", BuildReference(solution, refr));
			//				}
			//				ss.WriteLine("  </References>");
			//
			//
			//				ss.WriteLine("</Project>");
			//			}

			m_Kernel.CurrentWorkingDirectory.Pop();
		}

		private void WriteCombine(SolutionNode solution)
		{
			m_Kernel.Log.Write("Creating NAnt build files");
			foreach(ProjectNode project in solution.Projects)
			{
				if(m_Kernel.AllowProject(project.FilterGroups)) 
				{
					m_Kernel.Log.Write("...Creating project: {0}", project.Name);
					WriteProject(solution, project);
				}
			}

			m_Kernel.Log.Write("");
			string combFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "build");
			StreamWriter ss = new StreamWriter(combFile);

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(combFile));
            
			int count = 0;
            
			using(ss)
			{
				ss.WriteLine("<?xml version=\"1.0\" ?>");
				ss.WriteLine("<project name=\"{0}\" default=\"build\">", solution.Name);
				ss.WriteLine("    <echo message=\"Using '${nant.settings.currentframework}' Framework\"/>");
				ss.WriteLine();
				ss.WriteLine("    <property name=\"bin.dir\" value=\"bin\" />");
				ss.WriteLine("    <property name=\"obj.dir\" value=\"obj\" />");
				ss.WriteLine("    <property name=\"project.main.dir\" value=\"${nant.project.basedir}\" />");

				count = 0;
				foreach(ConfigurationNode conf in solution.Configurations)
				{
					if(count == 0)
					{
						ss.WriteLine("    <property name=\"project.config\" value=\"{0}\" />", conf.Name);
						ss.WriteLine();
					}
					ss.WriteLine("    <target name=\"{0}\" description=\"\">", conf.Name);
					//foreach(ProjectNode project in solution.Projects)
					//{
					//	Console.WriteLine(project);
						ss.WriteLine("        <property name=\"project.config\" value=\"{0}\" />", conf.Name);
						ss.WriteLine("        <property name=\"build.debug\" value=\"{0}\" />", conf.Options["DebugInformation"].ToString().ToLower());
					//}
					ss.WriteLine("    </target>");
					ss.WriteLine();
					count++;
				}
				
				count = 0;

				ss.WriteLine("    <target name=\"init\" description=\"\">");
				ss.WriteLine("        <call target=\"${project.config}\" />");
				ss.WriteLine("        <sysinfo />");
				ss.WriteLine("        <echo message=\"Platform ${sys.os.platform}\" />");
				ss.WriteLine("        <property name=\"build.dir\" value=\"${bin.dir}/${project.config}\" />");
				ss.WriteLine("    </target>");
				ss.WriteLine();
				
				ss.WriteLine("    <target name=\"clean\" description=\"\">");
				ss.WriteLine("        <echo message=\"Deleting all builds from all configurations\" />");
				foreach(ProjectNode project in solution.Projects)
				{
					string path = Helper.MakePathRelativeTo(solution.FullPath, project.FullPath);
					ss.Write("        <nant buildfile=\"{0}\"",
						Helper.NormalizePath(Helper.MakeFilePath(path, project.Name, "build"),'/'));
					ss.WriteLine(" target=\"clean\" />");
				}
				ss.WriteLine("    </target>");

				ss.WriteLine("    <target name=\"build\" depends=\"init\" description=\"\">");
				foreach(ProjectNode project in solution.Projects)
				{
					string path = Helper.MakePathRelativeTo(solution.FullPath, project.FullPath);
					ss.Write("        <nant buildfile=\"{0}\"",
						Helper.NormalizePath(Helper.MakeFilePath(path, project.Name, "build"),'/'));
					ss.WriteLine(" target=\"build\" />");
				}
				ss.WriteLine("    </target>");
				ss.WriteLine("</project>");
			}

			m_Kernel.CurrentWorkingDirectory.Pop();
		}

		private void CleanProject(ProjectNode project)
		{
			m_Kernel.Log.Write("...Cleaning project: {0}", project.Name);
			string projectFile = Helper.MakeFilePath(project.FullPath, project.Name, "build");
			Helper.DeleteIfExists(projectFile);
		}

		private void CleanSolution(SolutionNode solution)
		{
			m_Kernel.Log.Write("Cleaning NAnt build files for", solution.Name);

			string slnFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "build");
			Helper.DeleteIfExists(slnFile);

			foreach(ProjectNode project in solution.Projects)
			{
				CleanProject(project);
			}
            
			m_Kernel.Log.Write("");
		}

		#endregion

		#region ITarget Members

		/// <summary>
		/// Writes the specified kern.
		/// </summary>
		/// <param name="kern">The kern.</param>
		public void Write(Kernel kern)
		{
			if( kern == null )
			{
				throw new ArgumentNullException("kern");
			}
			m_Kernel = kern;
			foreach(SolutionNode solution in kern.Solutions)
			{
				WriteCombine(solution);
			}
			m_Kernel = null;
		}

		/// <summary>
		/// Cleans the specified kern.
		/// </summary>
		/// <param name="kern">The kern.</param>
		public virtual void Clean(Kernel kern)
		{
			if( kern == null )
			{
				throw new ArgumentNullException("kern");
			}
			m_Kernel = kern;
			foreach(SolutionNode sol in kern.Solutions)
			{
				CleanSolution(sol);
			}
			m_Kernel = null;
		}

		/// <summary>
		/// Gets the name.
		/// </summary>
		/// <value>The name.</value>
		public string Name
		{
			get
			{
				return "sharpdev";
			}
		}

		#endregion
	}
}
