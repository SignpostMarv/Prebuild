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
using System.Collections;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using System.Xml;

namespace DNPreBuild.Core.Util
{
	public class Helper
    {
        #region Fields

        public static Stack m_DirStack = null;
        public static Regex m_VarRegex = null;

        #endregion

        #region Constructors

        static Helper()
        {
            m_DirStack = new Stack();
            m_VarRegex = new Regex(@"\${(?<var>[\w|_]+)}");
        }

        #endregion

        #region Properties

        public static Stack DirStack
        {
            get
            {
                return m_DirStack;
            }
        }

        #endregion
        
        #region Public Methods

        public static object TranslateValue(Type t, string val)
        {
            if(val == null)
                return null;

            try
            {
                string lowerVal = val.ToLower();
                if(t == typeof(bool))
                    return (lowerVal == "true" || lowerVal == "1" || lowerVal == "y" || lowerVal == "yes" || lowerVal == "on");
                else if(t == typeof(int))
                    return (Int32.Parse(val));
                else
                    return val;
            }
            catch(FormatException)
            {
                return null;
            }
        }

        public static bool DeleteIfExists(string file)
        {
            string resFile = null;
            try
            {
                resFile = ResolvePath(file);
            }
            catch(ArgumentException)
            {
                return false;
            }

            if(!File.Exists(resFile))
                return false;

            File.Delete(resFile);
            return true;
        }

        // This little gem was taken from the NeL source, thanks guys!
        public static string MakePathRelativeTo(string basePath, string relPath)
        {
            string tmp = NormalizePath(basePath, '/');
            string src = NormalizePath(relPath, '/');
            string prefix = "";

            while(true)
            {
                if(String.Compare(tmp, 0, src, 0, tmp.Length) == 0)
                {
                    int size = tmp.Length;
                    if(size == src.Length)
                        return "./";

                    string ret = prefix + relPath.Substring(size, relPath.Length - size);
                    ret = ret.Trim();
                    if(ret[0] == '/' || ret[0] == '\\')
                        ret = "." + ret;

                    return NormalizePath(ret);
                }

                if(tmp.Length < 2)
                    break;

                int lastPos = tmp.LastIndexOf('/', tmp.Length - 2);
                int prevPos = tmp.IndexOf('/');

                if((lastPos == prevPos) || (lastPos == -1))
                    break;

                tmp = tmp.Substring(0, lastPos + 1);
                prefix += "../";
            }

            return relPath;
        }

        public static string ResolvePath(string path)
        {
            string tmpPath = NormalizePath(path);
            if(tmpPath.Length < 1)
                tmpPath = ".";
            
            tmpPath = Path.GetFullPath(tmpPath);
            if(!File.Exists(tmpPath) && !Directory.Exists(tmpPath))
                throw new ArgumentException("Path could not be resolved: " + tmpPath);

            return tmpPath;
        }

        public static string NormalizePath(string path, char sepChar)
        {
            if(path == null)
                return "";

            string tmpPath = path.Replace('\\', '/');
            tmpPath = tmpPath.Replace('/', sepChar);
            return tmpPath;
        }

        public static string NormalizePath(string path)
        {
            return NormalizePath(path, Path.DirectorySeparatorChar);
        }
        
        public static string EndPath(string path, char sepChar)
        {
            if(path == null || path.Length < 1)
                return "";

            if(!path.EndsWith(sepChar.ToString()))
                return (path + sepChar);

            return path;
        }

        public static string EndPath(string path)
        {
            return EndPath(path, Path.DirectorySeparatorChar);
        }

        public static string MakeFilePath(string path, string name, string ext)
        {
            string ret = EndPath(NormalizePath(path));
            
            ret += name;
            if(!name.EndsWith("." + ext))
                ret += "." + ext;
            
            foreach(char c in Path.InvalidPathChars)
                ret = ret.Replace(c, '_');

            return ret;
        }

        public static void SetCurrentDir(string path)
        {
            if(path.Length < 1)
                return;

            Environment.CurrentDirectory = path;
        }

        public static object CheckType(Type t, Type attr, Type inter)
        {
            if(t == null || attr == null)
                return null;

            object[] attrs = t.GetCustomAttributes(attr, false);
            if(attrs == null || attrs.Length < 1)
                return null;

            if(t.GetInterface(inter.FullName) == null)
                return null;

            return attrs[0];
        }

        public static string ParseValue(string val)
        {
            if(val == null || val.Length < 1)
                return val;

            string tmp = val;
            Match m = m_VarRegex.Match(val);
            while(m.Success)
            {
                if(m.Groups["var"] == null)
                    continue;

                Capture c = m.Groups["var"].Captures[0];
                if(c == null)
                    continue;

                string var = c.Value;
                string envVal = Environment.GetEnvironmentVariable(var);
                if(envVal == null)
                    envVal = "";

                tmp = tmp.Replace("${" + var + "}", envVal);
                m = m.NextMatch();
            }

            return tmp;
        }

        public static string AttributeValue(XmlNode node, string attr, string def)
        {
            if(node.Attributes[attr] == null)
                return def;

            return ParseValue(node.Attributes[attr].Value);
        }

        public static object EnumAttributeValue(XmlNode node, string attr, Type enumType, object def)
        {
            string val = AttributeValue(node, attr, def.ToString());
            return Enum.Parse(enumType, val, true);
        }

        #endregion
	}
}
