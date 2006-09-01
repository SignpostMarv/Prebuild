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
 * $Author: jendave $
 * $Date: 2006-07-28 22:43:24 -0700 (Fri, 28 Jul 2006) $
 * $Revision: 136 $
 */
#endregion

using System;
using System.Collections;
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

using Prebuild.Core.Attributes;
using Prebuild.Core.Interfaces;
using Prebuild.Core.Nodes;
using Prebuild.Core.Parse;
using Prebuild.Core.Utilities;

namespace Prebuild.Core.Targets
{
	/// <summary>
	/// 
	/// </summary>
	[Target("autotools")]
	public class AutotoolsTarget : ITarget
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
			string ret = "";
			if(solution.ProjectsTable.ContainsKey(refr.Name))
			{
				ProjectNode project = (ProjectNode)solution.ProjectsTable[refr.Name];				
				string fileRef = FindFileReference(refr.Name, project);
				string finalPath = Helper.NormalizePath(Helper.MakeFilePath(project.FullPath + "/$(BUILD_DIR)/$(CONFIG)/", refr.Name, "dll"), '/');
				ret += finalPath;
				return ret;
			}
			else
			{
				ProjectNode project = (ProjectNode)refr.Parent;
				string fileRef = FindFileReference(refr.Name, project);

				if(refr.Path != null || fileRef != null)
				{
					string finalPath = (refr.Path != null) ? Helper.NormalizePath(refr.Path + "/" + refr.Name + ".dll", '/') : fileRef;
					ret += finalPath;
					return ret;
				}

				try
				{
					Assembly assem = Assembly.LoadWithPartialName(refr.Name);
					if (assem != null)
					{
						ret += (refr.Name + ".dll");
					}
					else
					{
						ret += (refr.Name + ".dll");
					}
				}
				catch (System.NullReferenceException e)
				{
					e.ToString();
					ret += refr.Name + ".dll";
				}
			}
			return ret;
		}

		private static string BuildReferencePath(SolutionNode solution, ReferenceNode refr)
		{
			string ret = "";
			if(solution.ProjectsTable.ContainsKey(refr.Name))
			{
				ProjectNode project = (ProjectNode)solution.ProjectsTable[refr.Name];				
				string fileRef = FindFileReference(refr.Name, project);
				string finalPath = Helper.NormalizePath(Helper.MakeReferencePath(project.FullPath + "/${build.dir}/"), '/');
				ret += finalPath;
				return ret;
			}
			else
			{
				ProjectNode project = (ProjectNode)refr.Parent;
				string fileRef = FindFileReference(refr.Name, project);

				if(refr.Path != null || fileRef != null)
				{
					string finalPath = (refr.Path != null) ? Helper.NormalizePath(refr.Path, '/') : fileRef;
					ret += finalPath;
					return ret;
				}

				try
				{
					Assembly assem = Assembly.LoadWithPartialName(refr.Name);
					if (assem != null)
					{
						ret += "";
					}
					else
					{
						ret += "";
					}
				}
				catch (System.NullReferenceException e)
				{
					e.ToString();
					ret += "";
				}
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
			string docFile = (string)conf.Options["XmlDocFile"];
			//			if(docFile != null && docFile.Length == 0)//default to assembly name if not specified
			//			{
			//				return Path.GetFileNameWithoutExtension(project.AssemblyName) + ".xml";
			//			}
			return docFile;
		}

		/// <summary>
		/// Normalizes the path.
		/// </summary>
		/// <param name="path">The path.</param>
		/// <returns></returns>
		public static string NormalizePath(string path)
		{
			if(path == null)
			{
				return "";
			}

			StringBuilder tmpPath;

			if (Core.Parse.Preprocessor.GetOS() == "Win32")
			{
				tmpPath = new StringBuilder(path.Replace('\\', '/'));
				tmpPath.Replace("/", @"\\");
			}
			else
			{
				tmpPath = new StringBuilder(path.Replace('\\', '/'));
				tmpPath = tmpPath.Replace('/', Path.DirectorySeparatorChar);
			}
			return tmpPath.ToString();
		}

		private void WriteProject(SolutionNode solution, ProjectNode project)
		{
			string projFile = Helper.MakeFilePath(project.FullPath, "Makefile", "am");
			StreamWriter ss = new StreamWriter(projFile);

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(projFile));
			//bool hasDoc = false;

			using(ss)
			{
				ss.WriteLine("ASSEMBLY_NAME	= {0}", project.Name);
				ss.WriteLine("TARGET 		= {0}", project.Type.ToString().ToLower());
				ss.WriteLine();
				foreach(ConfigurationNode conf in project.Configurations)
				{
					if (GetXmlDocFile(project, conf) !="")
					{
						ss.WriteLine("DOCS = 1");
					}
					break;
				}
				foreach(ConfigurationNode conf in project.Configurations)
				{
					if (conf.Options.AllowUnsafe)
					{
						ss.WriteLine("UNSAFE = 1");
						ss.WriteLine();
					}
					break;
				}
				
				ss.WriteLine("include $(top_builddir)/rules.mk");
				ss.WriteLine();
				foreach(ConfigurationNode conf in project.Configurations)
				{
					if (conf.Options.KeyFile !="")
					{
						ss.WriteLine("SNKFILE = {0}", conf.Options.KeyFile);
						break;
					}
				}
				ss.WriteLine();
				ss.Write("FILES =");
				foreach(string file in project.Files)
				{
					switch(project.Files.GetBuildAction(file))
					{
						case BuildAction.Compile:
							ss.WriteLine(" \\");
							ss.Write("\t" + NormalizePath(file));
							break;
						default:
							break;
					}
				}
				
				ss.WriteLine();
				ss.WriteLine();
				
				if (project.References.Count > 0)
				{
					ss.WriteLine("REFERENCES = \\");
					ss.Write(" -r:");
					bool firstref = true;
					foreach(ReferenceNode refr in project.References)
					{
						if (firstref)
						{
							firstref = false;
						}
						else
						{
							ss.Write(",");
						}
						ss.Write("{0}", Helper.NormalizePath(Helper.MakePathRelativeTo(project.FullPath, BuildReference(solution, refr)), '/'));
					}
				}
				else
				{
					ss.WriteLine("REFERENCES =");
				}
				ss.WriteLine();
				ss.WriteLine();
				ss.WriteLine("BUILD_FILES = $(addprefix $(srcdir)/, $(FILES))");
				ss.WriteLine();
				ss.WriteLine("all: $(ASSEMBLY_NAME).$(ASSEMBLY_EXT)");
				ss.WriteLine();

				if (project.Type == ProjectType.Library)
				{
					ss.WriteLine("install-data-local:");
					ss.WriteLine("	echo \"$(GACUTIL) /i $(ASSEMBLY_NAME).$(ASSEMBLY_EXT) /f $(GACUTIL_FLAGS)\";  \\");
					ss.WriteLine("	$(GACUTIL) /i $(ASSEMBLY_NAME).$(ASSEMBLY_EXT) /f $(GACUTIL_FLAGS) || exit 1;");
					ss.WriteLine();
					ss.WriteLine("uninstall-local:");
					ss.WriteLine("	echo \"$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS)\"; \\");
					ss.WriteLine("	$(GACUTIL) /u $(ASSEMBLY_NAME) $(GACUTIL_FLAGS) || exit 1;");
					ss.WriteLine();
				}
				ss.WriteLine("$(ASSEMBLY_NAME).$(ASSEMBLY_EXT): $(FILES)");
				ss.WriteLine("	mkdir -p $(BUILD_DIR)/$(CONFIG)/");
				ss.WriteLine("	$(CSC) -out:$(BUILD_DIR)/$(CONFIG)/$(ASSEMBLY_NAME).$(ASSEMBLY_EXT) \\");
				ss.WriteLine("		-target:$(TARGET) \\");
				ss.WriteLine("		$(REFERENCES) \\");
				ss.WriteLine("		$(CSFLAGS) \\");
				ss.WriteLine("		$(BUILD_FILES)");
				ss.WriteLine();
				ss.WriteLine("CLEANFILES = $(BUILD_DIR)/$(CONFIG)/$(ASSEMBLY_NAME).$(ASSEMBLY_EXT) $(BUILD_DIR)/$(CONFIG)/$(ASSEMBLY_NAME).mdb $(BUILD_DIR)/$(CONFIG)/$(ASSEMBLY_NAME).pdb $(ASSEMBLY_NAME).xml");
				ss.WriteLine("EXTRA_DIST = \\");
				ss.Write("	$(FILES)");
				foreach(ConfigurationNode conf in project.Configurations)
				{
					if (conf.Options.KeyFile != "")
					{
						ss.Write(" \\");
						ss.WriteLine("\t" + conf.Options.KeyFile);
					}
					break;
				}
				//ss.WriteLine("	Tao.Sdl.dll.config");
			}                
			m_Kernel.CurrentWorkingDirectory.Pop();
		}

		private void WriteCombine(SolutionNode solution)
		{
			m_Kernel.Log.Write("Creating Autotools make files");
			foreach(ProjectNode project in solution.Projects)
			{
				if(m_Kernel.AllowProject(project.FilterGroups)) 
				{
					m_Kernel.Log.Write("...Creating makefile: {0}", project.Name);
					WriteProject(solution, project);
				}
			}
		}

		private void CleanProject(ProjectNode project)
		{
			m_Kernel.Log.Write("...Cleaning project: {0}", project.Name);
			string projectFile = Helper.MakeFilePath(project.FullPath, project.Name + (project.Type == ProjectType.Library ? ".dll" : ".exe"), "build");
			Helper.DeleteIfExists(projectFile);
		}

		private void CleanSolution(SolutionNode solution)
		{
			m_Kernel.Log.Write("Cleaning Autotools make files for", solution.Name);

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
				return "autotools";
			}
		}

		#endregion
	}
}
