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

namespace DNPreBuild.Core.Parse
{
    public enum IfState
    {
        None,
        If,
        ElseIf,
        Else
    }

	/// <summary>
	/// Summary description for IfContext.
	/// </summary>
	// Inspired by the equivalent WiX class (see www.sourceforge.net/projects/wix/)
	public class IfContext
	{
        #region Properties

        bool m_Active = false;
        bool m_Keep = false;
        bool m_EverKept = false;
        IfState m_State = IfState.None;

        #endregion

        #region Constructors

		public IfContext(bool active, bool keep, IfState state)
		{
            m_Active = active;
            m_Keep = keep;
            m_EverKept = keep;
            m_State = state;
		}

        #endregion

        #region Properties

        public bool Active
        {
            get
            {
                return m_Active;
            }
            set
            {
                m_Active = value;
            }
        }

        public bool Keep
        {
            get
            {
                return m_Keep;
            }
            set
            {
                m_Keep = value;
                if(m_Keep)
                    m_EverKept = true;
            }
        }

        public bool EverKept
        {
            get
            {
                return m_EverKept;
            }
        }

        public IfState State
        {
            get
            {
                return m_State;
            }
            set
            {
                m_State = value;
            }
        }

        #endregion
	}
}
