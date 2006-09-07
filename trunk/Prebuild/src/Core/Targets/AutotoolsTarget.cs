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
			string projFile = Helper.MakeFilePath(project.FullPath, "Include", "am");
			StreamWriter ss = new StreamWriter(projFile);
			ss.NewLine = "\n";

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(projFile));
			//bool hasDoc = false;

			using(ss)
			{
				ss.WriteLine(Helper.AssemblyFullName(project.AssemblyName, project.Type) + ":");
				ss.WriteLine("\tmkdir -p " + Helper.MakePathRelativeTo(solution.FullPath, project.Path) + "/$(BUILD_DIR)/$(CONFIG)/");
				ss.WriteLine("\t$(CSC)\t/out:" + Helper.MakePathRelativeTo(solution.FullPath, project.Path) + "/$(BUILD_DIR)/$(CONFIG)/" + Helper.AssemblyFullName(project.AssemblyName, project.Type) + " \\");
				ss.WriteLine("\t\t/target:" + project.Type.ToString().ToLower() + " \\");
				if (project.References.Count > 0)
				{
					ss.Write("\t\t/reference:");
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
						ss.Write("{0}", Helper.NormalizePath(Helper.MakePathRelativeTo(solution.FullPath, BuildReference(solution, refr)), '/'));
					}
					ss.WriteLine(" \\");
				}
				
				foreach(ConfigurationNode conf in project.Configurations)
				{
					if (conf.Options.KeyFile !="")
					{
						//ss.WriteLine("\t\t/keyfile:" + Helper.NormalizePath(Helper.MakePathRelativeTo(solution.FullPath, conf.Options.KeyFile), '/') + " \\");
						break;
					}
				}
				foreach(ConfigurationNode conf in project.Configurations)
				{
					if (conf.Options.AllowUnsafe)
					{
						ss.WriteLine("\t\t/unsafe \\");
						break;
					}
				}
				foreach(ConfigurationNode conf in project.Configurations)
				{
					if (GetXmlDocFile(project, conf) !="")
					{
						ss.WriteLine("\t\t/doc:" + project.Name + ".xml \\");
						break;
					}
				}
				foreach(string file in project.Files)
				{
					switch(project.Files.GetBuildAction(file))
					{
						case BuildAction.Compile:
							ss.WriteLine("\t\t\\");
							ss.Write("\t\t" + NormalizePath(Helper.MakePathRelativeTo(solution.FullPath, project.Path) + "\\\\" + file));
							break;
						default:
							break;
					}
				}
				ss.WriteLine();
				ss.WriteLine();

				if (project.Type == ProjectType.Library)
				{
					ss.WriteLine("install-data-local:");
					ss.WriteLine("	echo \"$(GACUTIL) /i " + project.Name + " /f $(GACUTIL_FLAGS)\";  \\");
					ss.WriteLine("	$(GACUTIL) /i " + project.AssemblyName + " /f $(GACUTIL_FLAGS) || exit 1;");
					ss.WriteLine();
					ss.WriteLine("uninstall-local:");
					ss.WriteLine("	echo \"$(GACUTIL) /u " + project.Name + " $(GACUTIL_FLAGS)\"; \\");
					ss.WriteLine("	$(GACUTIL) /u " + project.Name + " $(GACUTIL_FLAGS) || exit 1;");
					ss.WriteLine();
				}
				ss.WriteLine("CLEANFILES = $(BUILD_DIR)/$(CONFIG)/" + Helper.AssemblyFullName(project.AssemblyName, project.Type) + " $(BUILD_DIR)/$(CONFIG)/" + project.AssemblyName + ".mdb $(BUILD_DIR)/$(CONFIG)/" + project.AssemblyName + ".pdb " + project.AssemblyName + ".xml");
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
		bool hasLibrary = false;

		private void WriteCombine(SolutionNode solution)
		{
			hasLibrary = false;
			m_Kernel.Log.Write("Creating Autotools make files");
			foreach(ProjectNode project in solution.Projects)
			{
				if(m_Kernel.AllowProject(project.FilterGroups)) 
				{
					m_Kernel.Log.Write("...Creating makefile: {0}", project.Name);
					WriteProject(solution, project);
				}
			}

			m_Kernel.Log.Write("");
			string combFile = Helper.MakeFilePath(solution.FullPath, "Makefile", "am");
			StreamWriter ss = new StreamWriter(combFile);
			ss.NewLine = "\n";

			m_Kernel.CurrentWorkingDirectory.Push();
			Helper.SetCurrentDir(Path.GetDirectoryName(combFile));
            
			using(ss)
			{
				foreach(ProjectNode project in solution.ProjectsTableOrder)
				{
					if (project.Type == ProjectType.Library)
					{
						hasLibrary = true;
						break;
					}
				}


				if (hasLibrary)
				{
					ss.Write("pkgconfig_in_files = ");
					foreach(ProjectNode project in solution.ProjectsTableOrder)
					{
						if (project.Type == ProjectType.Library)
						{
							string combFilepc = Helper.MakeFilePath(solution.FullPath, project.Name, "pc.in");
							ss.Write(" " + project.Name + ".pc.in ");
							StreamWriter sspc = new StreamWriter(combFilepc);
							sspc.NewLine = "\n";
							using(sspc)
							{
								sspc.WriteLine("prefix=@prefix@");
								sspc.WriteLine("exec_prefix=${prefix}");
								sspc.WriteLine("libdir=${exec_prefix}/lib");
								sspc.WriteLine();
								sspc.WriteLine("Name: {0}", project.Name);
								sspc.WriteLine("Description: {0}", project.Name);
								sspc.WriteLine("Version: @VERSION@");
								sspc.WriteLine("Libs:  -r:${libdir}/mono/" + project.Name + "/" + Helper.AssemblyFullName(project.Name, project.Type));
							}
						}
					}
				
				
					ss.WriteLine();
					ss.WriteLine("pkgconfigdir=$(prefix)/lib/pkgconfig");
					ss.WriteLine("pkgconfig_DATA=$(pkgconfig_in_files:.pc.in=.pc)");
				}
				ss.WriteLine();
				foreach(ProjectNode project in solution.ProjectsTableOrder)
				{
					string path = Helper.MakePathRelativeTo(solution.FullPath, project.FullPath);
					ss.WriteLine("-include x {0}",
						Helper.NormalizePath(Helper.MakeFilePath(path, "Include", "am"),'/'));
				}
				ss.WriteLine();
				ss.WriteLine("all: \\");
				ss.Write("\t");
				foreach(ProjectNode project in solution.ProjectsTableOrder)
				{
					string path = Helper.MakePathRelativeTo(solution.FullPath, project.FullPath);
					ss.Write(Helper.AssemblyFullName(project.Name, project.Type) + " ");
						
				}
				ss.WriteLine();
				if (hasLibrary)
				{
					ss.WriteLine("EXTRA_DIST = \\");
					ss.WriteLine("\t$(pkgconfig_in_files)");
				}
				else
				{
					ss.WriteLine("EXTRA_DIST = ");
				}
				ss.WriteLine();
				ss.WriteLine("DISTCLEANFILES = \\");
				ss.WriteLine("\tconfigure \\");
				ss.WriteLine("\tMakefile.in  \\");
				ss.WriteLine("\taclocal.m4");
			}
			combFile = Helper.MakeFilePath(solution.FullPath, "configure", "ac");
			StreamWriter ts = new StreamWriter(combFile);
			ts.NewLine = "\n";
			using(ts)
			{
				if (this.hasLibrary)
				{
					bool done = false;
					foreach(ProjectNode project in solution.ProjectsTableOrder)
					{
						if (project.Type == ProjectType.Library)
						{
							ts.WriteLine("AC_INIT(" + project.Name + ".pc.in)");
							break;
						}
					}
				}
				else
				{
					ts.WriteLine("AC_INIT(Makefile.am)");
				}
				ts.WriteLine("AC_PREREQ(2.53)");
				ts.WriteLine("AC_CANONICAL_SYSTEM");
				ts.WriteLine("AM_INIT_AUTOMAKE([" + solution.Name + "],[2.0.0],[])");
				ts.WriteLine();
				ts.WriteLine("AM_MAINTAINER_MODE");
				ts.WriteLine();
				ts.WriteLine("dnl AC_PROG_INTLTOOL([0.25])");
				ts.WriteLine();
				ts.WriteLine("AC_PROG_INSTALL");
				ts.WriteLine();
				ts.WriteLine("MONO_REQUIRED_VERSION=1.1");
				ts.WriteLine();
				ts.WriteLine("AC_MSG_CHECKING([whether we're compiling from CVS])");
				ts.WriteLine("if test -f \"$srcdir/.cvs_version\" ; then");
				ts.WriteLine("        from_cvs=yes");
				ts.WriteLine("else");
				ts.WriteLine("  if test -f \"$srcdir/.svn\" ; then");
				ts.WriteLine("        from_cvs=yes");
				ts.WriteLine("  else");
				ts.WriteLine("        from_cvs=no");
				ts.WriteLine("  fi");
				ts.WriteLine("fi");
				ts.WriteLine();
				ts.WriteLine("AC_MSG_RESULT($from_cvs)");
				ts.WriteLine();
				ts.WriteLine("AC_PATH_PROG(MONO, mono)");
				ts.WriteLine("AC_PATH_PROG(GMCS, gmcs)");
				ts.WriteLine("AC_PATH_PROG(GACUTIL, gacutil)");
				ts.WriteLine();
				ts.WriteLine("AC_MSG_CHECKING([for mono])");
				ts.WriteLine("dnl if test \"x$MONO\" = \"x\" ; then");
				ts.WriteLine("dnl  AC_MSG_ERROR([Can't find \"mono\" in your PATH])");
				ts.WriteLine("dnl else");
				ts.WriteLine("  AC_MSG_RESULT([found])");
				ts.WriteLine("dnl fi");
				ts.WriteLine();
				ts.WriteLine("AC_MSG_CHECKING([for gmcs])");
				ts.WriteLine("dnl if test \"x$GMCS\" = \"x\" ; then");
				ts.WriteLine("dnl  AC_MSG_ERROR([Can't find \"gmcs\" in your PATH])");
				ts.WriteLine("dnl else");
				ts.WriteLine("  AC_MSG_RESULT([found])");
				ts.WriteLine("dnl fi");
				ts.WriteLine();
				ts.WriteLine("AC_MSG_CHECKING([for gacutil])");
				ts.WriteLine("if test \"x$GACUTIL\" = \"x\" ; then");
				ts.WriteLine("  AC_MSG_ERROR([Can't find \"gacutil\" in your PATH])");
				ts.WriteLine("else");
				ts.WriteLine("  AC_MSG_RESULT([found])");
				ts.WriteLine("fi");
				ts.WriteLine();
				ts.WriteLine("AC_SUBST(PATH)");
				ts.WriteLine("AC_SUBST(LD_LIBRARY_PATH)");
				ts.WriteLine();
				ts.WriteLine("dnl CSFLAGS=\"-debug -nowarn:1574\"");
				ts.WriteLine("CSFLAGS=\"\"");
				ts.WriteLine("AC_SUBST(CSFLAGS)");
				ts.WriteLine();
//				ts.WriteLine("AC_MSG_CHECKING(--disable-sdl argument)");
//				ts.WriteLine("AC_ARG_ENABLE(sdl,");
//				ts.WriteLine("    [  --disable-sdl         Disable Sdl interface.],");
//				ts.WriteLine("    [disable_sdl=$disableval],");
//				ts.WriteLine("    [disable_sdl=\"no\"])");
//				ts.WriteLine("AC_MSG_RESULT($disable_sdl)");
//				ts.WriteLine("if test \"$disable_sdl\" = \"yes\"; then");
//				ts.WriteLine("  AC_DEFINE(FEAT_SDL)");
//				ts.WriteLine("fi");
				ts.WriteLine();
				ts.WriteLine("dnl Find pkg-config");
				ts.WriteLine("AC_PATH_PROG(PKGCONFIG, pkg-config, no)");
				ts.WriteLine("if test \"x$PKG_CONFIG\" = \"xno\"; then");
				ts.WriteLine("        AC_MSG_ERROR([You need to install pkg-config])");
				ts.WriteLine("fi");
				ts.WriteLine();
				ts.WriteLine("PKG_CHECK_MODULES(MONO_DEPENDENCY, mono >= $MONO_REQUIRED_VERSION, has_mono=true, has_mono=false)");
				ts.WriteLine("BUILD_DIR=\"bin\"");
				ts.WriteLine("CONFIG=\"Release\"");
				ts.WriteLine("AC_SUBST(BUILD_DIR)");
				ts.WriteLine("AC_SUBST(CONFIG)");
				ts.WriteLine();
				ts.WriteLine("if test \"x$has_mono\" = \"xtrue\"; then");
				ts.WriteLine("  AC_PATH_PROG(RUNTIME, mono, no)");
				ts.WriteLine("  AC_PATH_PROG(CSC, gmcs, no)");
				ts.WriteLine("  if test `uname -s` = \"Darwin\"; then");
				ts.WriteLine("        LIB_PREFIX=");
				ts.WriteLine("        LIB_SUFFIX=.dylib");
				ts.WriteLine("  else");
				ts.WriteLine("        LIB_PREFIX=.so");
				ts.WriteLine("        LIB_SUFFIX=");
				ts.WriteLine("  fi");
				ts.WriteLine("else");
				ts.WriteLine("  AC_PATH_PROG(CSC, csc.exe, no)");
				ts.WriteLine("  if test x$CSC = \"xno\"; then");
				ts.WriteLine("        AC_MSG_ERROR([You need to install either mono or .Net])");
				ts.WriteLine("  else");
				ts.WriteLine("    RUNTIME=");
				ts.WriteLine("    LIB_PREFIX=");
				ts.WriteLine("    LIB_SUFFIX=.dylib");
				ts.WriteLine("  fi");
				ts.WriteLine("fi");
				ts.WriteLine();
				ts.WriteLine("AC_SUBST(LIB_PREFIX)");
				ts.WriteLine("AC_SUBST(LIB_SUFFIX)");
				ts.WriteLine();
				ts.WriteLine("AC_SUBST(BASE_DEPENDENCIES_CFLAGS)");
				ts.WriteLine("AC_SUBST(BASE_DEPENDENCIES_LIBS)");
				ts.WriteLine();
				ts.WriteLine("dnl Find monodoc");
				ts.WriteLine("MONODOC_REQUIRED_VERSION=1.0");
				ts.WriteLine("AC_SUBST(MONODOC_REQUIRED_VERSION)");
				ts.WriteLine("PKG_CHECK_MODULES(MONODOC_DEPENDENCY, monodoc >= $MONODOC_REQUIRED_VERSION, enable_monodoc=yes, enable_monodoc=no)");
				ts.WriteLine();
				ts.WriteLine("if test \"x$enable_monodoc\" = \"xyes\"; then");
				ts.WriteLine("        AC_PATH_PROG(MONODOC, monodoc, no)");
				ts.WriteLine("        if test x$MONODOC = xno; then");
				ts.WriteLine("           enable_monodoc=no");
				ts.WriteLine("        fi");
				ts.WriteLine("else");
				ts.WriteLine("        MONODOC=");
				ts.WriteLine("fi");
				ts.WriteLine();
				ts.WriteLine("AC_SUBST(MONODOC)");
				ts.WriteLine("AM_CONDITIONAL(ENABLE_MONODOC, test \"x$enable_monodoc\" = \"xyes\")");
				ts.WriteLine();
				ts.WriteLine("AC_PATH_PROG(GACUTIL, gacutil, no)");
				ts.WriteLine("if test \"x$GACUTIL\" = \"xno\" ; then");
				ts.WriteLine("        AC_MSG_ERROR([No gacutil tool found])");
				ts.WriteLine("fi");
				ts.WriteLine();
//				foreach(ProjectNode project in solution.ProjectsTableOrder)
//				{
//					if (project.Type == ProjectType.Library)
//					{
//					}
//				}
				ts.WriteLine("GACUTIL_FLAGS='/package " + solution.Name + " /gacdir $(DESTDIR)$(prefix)/lib'");
				ts.WriteLine("AC_SUBST(GACUTIL_FLAGS)");
				ts.WriteLine();
				ts.WriteLine("winbuild=no");
				ts.WriteLine("case \"$host\" in");
				ts.WriteLine("       *-*-mingw*|*-*-cygwin*)");
				ts.WriteLine("               winbuild=yes");
				ts.WriteLine("               ;;");
				ts.WriteLine("esac");
				ts.WriteLine("AM_CONDITIONAL(WINBUILD, test x$winbuild = xyes)");
				ts.WriteLine();
//				ts.WriteLine("dnl Check for SDL");
//				ts.WriteLine();
//				ts.WriteLine("AC_PATH_PROG([SDL_CONFIG], [sdl-config])");
//				ts.WriteLine("have_sdl=no");
//				ts.WriteLine("if test -n \"${SDL_CONFIG}\"; then");
//				ts.WriteLine("    have_sdl=yes");
//				ts.WriteLine("    SDL_CFLAGS=`$SDL_CONFIG --cflags`");
//				ts.WriteLine("    SDL_LIBS=`$SDL_CONFIG --libs`");
//				ts.WriteLine("    #");
//				ts.WriteLine("    # sdl-config sometimes emits an rpath flag pointing at its library");
//				ts.WriteLine("    # installation directory.  We don't want this, as it prevents users from");
//				ts.WriteLine("    # linking sdl-viewer against, for example, a locally compiled libGL when a");
//				ts.WriteLine("    # version of the library also exists in SDL's library installation");
//				ts.WriteLine("    # directory, typically /usr/lib.");
//				ts.WriteLine("    #");
//				ts.WriteLine("    SDL_LIBS=`echo $SDL_LIBS | sed 's/-Wl,-rpath,[[^ ]]* //'`");
//				ts.WriteLine("fi");
//				ts.WriteLine("AC_SUBST([SDL_CFLAGS])");
//				ts.WriteLine("AC_SUBST([SDL_LIBS])");
				ts.WriteLine();
				ts.WriteLine("AC_OUTPUT([");
				ts.WriteLine("Makefile");
				foreach(ProjectNode project in solution.ProjectsTableOrder)
				{
					if (project.Type == ProjectType.Library)
					{
						ts.WriteLine(project.Name + ".pc");
					}
					string path = Helper.MakePathRelativeTo(solution.FullPath, project.FullPath);
					ts.WriteLine(Helper.NormalizePath(Helper.MakeFilePath(path, "Include"),'/'));
				}
				ts.WriteLine("])");
				ts.WriteLine();
				ts.WriteLine("#po/Makefile.in");
				ts.WriteLine();
				ts.WriteLine("echo \"---\"");
				ts.WriteLine("echo \"Configuration summary\"");
				ts.WriteLine("echo \"\"");
				ts.WriteLine("echo \"   * Installation prefix: $prefix\"");
				ts.WriteLine("echo \"   * compiler: $CSC\"");
				ts.WriteLine("echo \"   * Documentation: $enable_monodoc ($MONODOC)\"");
				ts.WriteLine("echo \"\"");
				ts.WriteLine("echo \"---\"");
				ts.WriteLine();
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
