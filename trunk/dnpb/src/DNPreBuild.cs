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
using System.Collections.Specialized;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;

using DNPreBuild.Core;
using DNPreBuild.Core.Util;

namespace DNPreBuild 
{
	class DNPreBuild
    {
        #region Main

        [STAThread]
		static void Main(string[] args) 
        {
            try 
            {
                Kernel kernel = Kernel.Instance;
                kernel.Initialize(LogTarget.File | LogTarget.Console, args);
                bool exit = false;

                if(kernel.CommandLine.WasPassed("usage"))
                {
                    exit = true;
                    OutputUsage();
                }
                if(kernel.CommandLine.WasPassed("showtargets"))
                {
                    exit = true;
                    OutputTargets(kernel);
                }

                if(exit)
                    Environment.Exit(0);

                kernel.Process();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Unhandled error: {0}", ex.Message);
#if DEBUG
                Console.WriteLine("{0}", ex.StackTrace);
#endif
            }
		}

        #endregion

        #region Private Methods

        private static void OutputUsage() 
        {
            Console.WriteLine("Usage: dnpb /target <target> [options]");
            Console.WriteLine("Available command-line switches:");
            Console.WriteLine("");
            Console.WriteLine("/target          Target for .NET Pre-Build");
            Console.WriteLine("/clean           Clean the build files for the given target");
            Console.WriteLine("/file            XML file to process");
            Console.WriteLine("/log             Log file to write to");
            Console.WriteLine("/ppo             Pre-process the file, but perform no other processing");
            Console.WriteLine("");
            Console.WriteLine("See 'dnpb /showtargets for a list of available targets");
            Console.WriteLine("");
        }

        private static void OutputTargets(Kernel kern)
        {
            Console.WriteLine("Targets available in .NET Pre-build:");
            Console.WriteLine("");
            if(kern.Targets.Keys.Count > 0)
            {
                string[] targs = new string[kern.Targets.Keys.Count];
                kern.Targets.Keys.CopyTo(targs, 0);
                Array.Sort(targs);
                foreach(string target in targs)
                    Console.WriteLine(target);
            }
            Console.WriteLine("");
        }
        
        #endregion
	}
}
