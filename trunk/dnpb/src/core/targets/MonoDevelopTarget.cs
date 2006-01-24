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
using System.Text.RegularExpressions;

using DNPreBuild.Core.Attributes;
using DNPreBuild.Core.Interfaces;
using DNPreBuild.Core.Nodes;
using DNPreBuild.Core.Utilities;

namespace DNPreBuild.Core.Targets
{
	[Target("monodev")]
	public class MonoDevelopTarget : ITarget
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
			string ret = "<ProjectReference type=\"";
			if(solution.ProjectsTable.ContainsKey(refr.Name))
			{
				ret += "Project\"";
				ret += " localcopy=\"" + refr.LocalCopy.ToString() +  "\" refto=\"" + refr.Name + "\" />";
			}
			else
			{
				ProjectNode project = (ProjectNode)refr.Parent;
				string fileRef = FindFileReference(refr.Name, project);

				if(refr.Path != null || fileRef != null)
				{
					ret += "Assembly\" refto=\"";

					string finalPath = (refr.Path != null) ? Helper.MakeFilePath(refr.Path, refr.Name, "dll") : fileRef;

					ret += finalPath;
					ret += "\" localcopy=\"" + refr.LocalCopy.ToString() + "\" />";
					return ret;
				}

				ret += "Gac\"";
				ret += " localcopy=\"" + refr.LocalCopy.ToString() + "\"";
				ret += " refto=\"" + refr.Name + "\" />";
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

		private void WriteProject(SolutionNode solution, ProjectNode project)
		{
			string csComp = "Mcs";
			string netRuntime = "Mono";
			if(project.Runtime == ClrRuntime.Microsoft)
			{
				csComp = "Csc";
				netRuntime = "MsNet";
			}

			string projFile = Helper.MakeFilePath(project.FullPath, project.Name, "mdp");
			StreamWriter ss = new StreamWriter(projFile);

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(projFile));

			using(ss)
			{
				ss.WriteLine(
					"<Project name=\"{0}\" description=\"\" standardNamespace=\"{1}\" newfilesearch=\"None\" enableviewstate=\"True\" fileversion=\"2.0\" language=\"C#\" ctype=\"DotNetProject\">",
					project.Name,
					project.RootNamespace
					);
				
								int count = 0;
				
				ss.WriteLine("  <Configurations active=\"{0}\">", solution.ActiveConfig);

				foreach(ConfigurationNode conf in project.Configurations)
				{
					ss.WriteLine("    <Configuration name=\"{0}\" ctype=\"DotNetProjectConfiguration\">", conf.Name);
					ss.Write("      <Output");
					ss.Write(" directory=\"{0}\"", Helper.EndPath(Helper.NormalizePath(".\\" + conf.Options["OutputPath"].ToString())));
					ss.Write(" assembly=\"{0}\"", project.AssemblyName);
					ss.Write(" executeScript=\"\"");
					ss.Write(" executeBeforeBuild=\"\"");
					ss.Write(" executeAfterBuild=\"\"");
					ss.WriteLine(" />");
					
					ss.Write("      <Build");
					ss.Write(" debugmode=\"True\"");
					ss.Write(" target=\"{0}\"", project.Type);
					ss.WriteLine(" />");
					
					ss.Write("      <Execution");
					ss.Write(" runwithwarnings=\"True\"");
					ss.Write(" consolepause=\"True\"");
					ss.Write(" runtime=\"{0}\"", netRuntime);
					ss.WriteLine(" />");
					
					ss.Write("      <CodeGeneration");
					ss.Write(" compiler=\"{0}\"", csComp);
					ss.Write(" warninglevel=\"{0}\"", conf.Options["WarningLevel"]);
					ss.Write(" nowarn=\"{0}\"", conf.Options["SuppressWarnings"]);
					ss.Write(" includedebuginformation=\"{0}\"", conf.Options["DebugInformation"]);
					ss.Write(" optimize=\"{0}\"", conf.Options["OptimizeCode"]);
					ss.Write(" unsafecodeallowed=\"{0}\"", conf.Options["AllowUnsafe"]);
					ss.Write(" generateoverflowchecks=\"{0}\"", conf.Options["CheckUnderflowOverflow"]);
					ss.Write(" mainclass=\"{0}\"", project.StartupObject);
					ss.Write(" target=\"{0}\"", project.Type);
					ss.Write(" definesymbols=\"{0}\"", conf.Options["CompilerDefines"]);
					ss.Write(" generatexmldocumentation=\"{0}\"", conf.Options["GenerateXmlDocFile"]);
					ss.Write(" ctype=\"CSharpCompilerParameters\"");
					ss.WriteLine(" />");
					ss.WriteLine("    </Configuration>");

					count++;
				}                
				ss.WriteLine("  </Configurations>");

				ss.Write("  <DeploymentInformation");
				ss.Write(" target=\"\"");
				ss.Write(" script=\"\"");
				ss.Write(" strategy=\"File\"");
				ss.WriteLine(">");
				ss.WriteLine("    <excludeFiles />");
				ss.WriteLine("  </DeploymentInformation>");
				
				ss.WriteLine("  <Contents>");
				foreach(string file in project.Files)
				{
					string buildAction = "Compile";
					switch(project.Files.GetBuildAction(file))
					{
						case BuildAction.None:
							buildAction = "Nothing";
							break;

						case BuildAction.Content:
							buildAction = "Exclude";
							break;

						case BuildAction.EmbeddedResource:
							buildAction = "EmbedAsResource";
							break;

						default:
							buildAction = "Compile";
							break;
					}

					// Sort of a hack, we try and resolve the path and make it relative, if we can.
					string filePath = PrependPath(file);
					ss.WriteLine("    <File name=\"{0}\" subtype=\"Code\" buildaction=\"{1}\" dependson=\"\" data=\"\" />", filePath, buildAction);
				}
				ss.WriteLine("  </Contents>");

				ss.WriteLine("  <References>");
				foreach(ReferenceNode refr in project.References)
				{
					ss.WriteLine("    {0}", BuildReference(solution, refr));
				}
				ss.WriteLine("  </References>");


				ss.WriteLine("</Project>");
			}

			m_Kernel.CurrentWorkingDirectory.Pop();
		}

		private void WriteCombine(SolutionNode solution)
		{
			m_Kernel.Log.Write("Creating MonoDevelop combine and project files");
			foreach(ProjectNode project in solution.Projects)
			{
				if(m_Kernel.AllowProject(project.FilterGroups)) 
				{
					m_Kernel.Log.Write("...Creating project: {0}", project.Name);
					WriteProject(solution, project);
				}
			}

			m_Kernel.Log.Write("");
			string combFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "mds");
			StreamWriter ss = new StreamWriter(combFile);

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(combFile));
            
            int count = 0;
            
			using(ss)
			{
				ss.WriteLine("<Combine name=\"{0}\" fileversion=\"2.0\" description=\"\">", solution.Name);

				count = 0;
				foreach(ConfigurationNode conf in solution.Configurations)
				{
					if(count == 0)
					{
						ss.WriteLine("  <Configurations active=\"{0}\">", conf.Name);
					}

					ss.WriteLine("    <Configuration name=\"{0}\" ctype=\"CombineConfiguration\">", conf.Name);
					foreach(ProjectNode project in solution.Projects)
					{
						ss.WriteLine("      <Entry configuration=\"{1}\" build=\"True\" name=\"{0}\" />", project.Name, conf.Name);
					}
					ss.WriteLine("    </Configuration>");

					count++;
				}
				ss.WriteLine("  </Configurations>");
				
				count = 0;
				
				foreach(ProjectNode project in solution.Projects)
				{                    
					if(count == 0)
						ss.WriteLine("  <StartMode startupentry=\"{0}\" single=\"True\">", project.Name);

					ss.WriteLine("    <Execute type=\"None\" entry=\"{0}\" />", project.Name);
					count++;
				}
				ss.WriteLine("  </StartMode>");
				
				ss.WriteLine("  <Entries>");
				foreach(ProjectNode project in solution.Projects)
				{
					string path = Helper.MakePathRelativeTo(solution.FullPath, project.FullPath);
					ss.WriteLine("    <Entry filename=\"{0}\" />",
						Helper.MakeFilePath(path, project.Name, "mdp"));
				}
				ss.WriteLine("  </Entries>");
				
				ss.WriteLine("</Combine>");
			}

			m_Kernel.CurrentWorkingDirectory.Pop();
		}

		private void CleanProject(ProjectNode project)
		{
			m_Kernel.Log.Write("...Cleaning project: {0}", project.Name);
			string projectFile = Helper.MakeFilePath(project.FullPath, project.Name, "mdp");
			Helper.DeleteIfExists(projectFile);
		}

		private void CleanSolution(SolutionNode solution)
		{
			m_Kernel.Log.Write("Cleaning MonoDevelop combine and project files for", solution.Name);

			string slnFile = Helper.MakeFilePath(solution.FullPath, solution.Name, "mds");
			Helper.DeleteIfExists(slnFile);

			foreach(ProjectNode project in solution.Projects)
			{
				CleanProject(project);
			}
            
			m_Kernel.Log.Write("");
		}

		#endregion

		#region ITarget Members

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
