#region BSD License
/*
Copyright (c) 2004 Matthew Holmes (kerion@houston.rr.com)

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
using System.Diagnostics;
using System.IO;
using System.Xml;

namespace DNPreBuild.Core.Util
{
	public class Helper
    {
        #region Fields

        public static Stack m_DirStack = null;

        #endregion

        #region Constructors

        static Helper()
        {
            m_DirStack = new Stack();
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
                    return (lowerVal == "true" || lowerVal == "1" || lowerVal == "y" || lowerVal == "yes");
                else if(t == typeof(int))
                    return (Int32.Parse(val));
                else
                    return val;
            }
            catch
            {
                return null;
            }
        }

        public static string ResolvePath(string path)
        {
            string tmpPath = NormalizePath(path);
            if(tmpPath.Length < 1)
                tmpPath = ".";
            
            return Path.GetFullPath(tmpPath);
        }

        public static string NormalizePath(string path)
        {
            if(path == null)
                return "";

            string tmpPath = path.Replace('\\', '/');
            tmpPath = tmpPath.Replace('/', Path.DirectorySeparatorChar);
            return tmpPath;
        }
        
        public static string EndPath(string path)
        {
            if(!path.EndsWith(Path.DirectorySeparatorChar.ToString()))
                return (path + Path.DirectorySeparatorChar);

            return path;
        }

        public static string MakeFilePath(string path, string name, string ext)
        {
            string ret = EndPath(NormalizePath(path));
            ret += name + "." + ext;
            
            foreach(char c in Path.InvalidPathChars)
                ret = ret.Replace(c, '_');

            return ret;
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

        public static string AttributeValue(XmlNode node, string attr, string def)
        {
            if(node.Attributes[attr] == null)
                return def;

            return node.Attributes[attr].Value;
        }

        public static object EnumAttributeValue(XmlNode node, string attr, Type enumType, object def)
        {
            string val = AttributeValue(node, attr, def.ToString());
            return Enum.Parse(enumType, val, true);
        }

        #endregion
	}
}
