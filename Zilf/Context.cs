/* Copyright 2010 Jesse McGrew
 * 
 * This file is part of ZILF.
 * 
 * ZILF is free software: you can redistribute it and/or modify it
 * under the terms of the GNU General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * ZILF is distributed in the hope that it will be useful, but
 * WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * General Public License for more details.
 * 
 * You should have received a copy of the GNU General Public License
 * along with ZILF.  If not, see <http://www.gnu.org/licenses/>.
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using System.IO;

namespace Zilf
{
    enum RunMode
    {
        Interactive,
        Expression,
        Interpreter,
        Compiler,
    }

    class Context
    {
        private class Binding
        {
            public ZilObject Value;
            public Binding Prev;

            public Binding(ZilObject value)
            {
                this.Value = value;
            }
        }

        private struct AssocPair : IEquatable<AssocPair>
        {
            public readonly ZilObject First;
            public readonly ZilObject Second;

            public AssocPair(ZilObject first, ZilObject second)
            {
                this.First = first;
                this.Second = second;
            }

            public override bool Equals(object obj)
            {
                if (obj is AssocPair)
                    return Equals((AssocPair)obj);
                else
                    return false;
            }

            public bool Equals(AssocPair other)
            {
                return First.Equals(other.First) && Second.Equals(other.Second);
            }

            public override int GetHashCode()
            {
                return First.GetHashCode() ^ Second.GetHashCode();
            }
        }

        private RunMode runMode;
        private int errorCount;
        private bool ignoreCase, quiet, traceRoutines, wantDebugInfo;
        private string curFile;
        private ZilForm callingForm;

        private ObList rootObList;
        private Dictionary<ZilAtom, Binding> localValues;
        private Dictionary<ZilAtom, ZilObject> globalValues;
        private Dictionary<AssocPair, ZilObject> associations;
        private readonly ZEnvironment zenv;

        /// <summary>
        /// Gets a value representing truth (the atom T).
        /// </summary>
        public readonly ZilObject TRUE;
        /// <summary>
        /// Gets a value representing falsehood (a FALSE object with an empty list).
        /// </summary>
        public readonly ZilObject FALSE;

        private ZilAtom[] stdAtoms;

        public Context()
            : this(false)
        {
        }

        public Context(bool ignoreCase)
        {
            this.ignoreCase = ignoreCase;

            rootObList = new ObList(ignoreCase);
            localValues = new Dictionary<ZilAtom, Binding>();
            globalValues = new Dictionary<ZilAtom, ZilObject>();
            associations = new Dictionary<AssocPair, ZilObject>();

            zenv = new ZEnvironment(this);

            InitStdAtoms();
            InitSubrs();

            TRUE = GetStdAtom(StdAtom.T);
            FALSE = new ZilFalse(new ZilList(null, null));

            InitConstants();

            // initialize OBLIST path
            ObList userObList = new ObList(ignoreCase);
            ZilList olpath = new ZilList(new ZilObject[] { userObList, rootObList });
            ZilAtom olatom = GetStdAtom(StdAtom.OBLIST);
            localValues[olatom] = new Binding(olpath);
        }

        public ObList RootObList
        {
            get { return rootObList; }
        }

        public RunMode RunMode
        {
            get { return runMode; }
            set { runMode = value; }
        }

        public bool IgnoreCase
        {
            get { return ignoreCase; }
        }

        public bool Quiet
        {
            get { return quiet; }
            set { quiet = value; }
        }

        public bool TraceRoutines
        {
            get { return traceRoutines; }
            set { traceRoutines = value; }
        }

        public bool WantDebugInfo
        {
            get { return wantDebugInfo; }
            set { wantDebugInfo = value; }
        }

        public bool Compiling
        {
            get { return runMode == RunMode.Compiler; }
        }

        public IEnumerable<KeyValuePair<ZilAtom, ZilObject>> GlobalVals
        {
            get { return globalValues; }
        }

        public int ErrorCount
        {
            get { return errorCount; }
            set { errorCount = value; }
        }

        public ZEnvironment ZEnvironment
        {
            get { return zenv; }
        }

        public string CurrentFile
        {
            get { return curFile; }
            set { curFile = value; }
        }

        public ZilForm CallingForm
        {
            get { return callingForm; }
            set { callingForm = value; }
        }

        private void InitStdAtoms()
        {
            StdAtom[] ids = (StdAtom[])Enum.GetValues(typeof(StdAtom));

            StdAtom max = ids[ids.Length - 1];
            stdAtoms = new ZilAtom[(int)max + 1];

            foreach (StdAtom sa in ids)
                if (sa != StdAtom.None)
                {
                    string pname = Enum.GetName(typeof(StdAtom), sa);

                    object[] attrs = typeof(StdAtom).GetField(pname).GetCustomAttributes(
                        typeof(AtomAttribute), false);
                    if (attrs.Length > 0)
                        pname = ((AtomAttribute)attrs[0]).Name;

                    ZilAtom atom = new ZilAtom(pname, rootObList, sa);
                    rootObList[pname] = atom;
                    stdAtoms[(int)sa] = atom;
                }
        }

        private void InitSubrs()
        {
            MethodInfo[] methods = typeof(Subrs).GetMethods(BindingFlags.Static | BindingFlags.Public);
            foreach (MethodInfo mi in methods)
            {
                object[] attrs = mi.GetCustomAttributes(typeof(Subrs.SubrAttribute), false);
                if (attrs.Length == 0)
                    continue;

                Subrs.SubrDelegate del =
                    (Subrs.SubrDelegate)Delegate.CreateDelegate(typeof(Subrs.SubrDelegate), mi);

                foreach (Subrs.SubrAttribute attr in attrs)
                {
                    ZilSubr sub;
                    if (attr is Subrs.FSubrAttribute)
                        sub = new ZilFSubr(del);
                    else
                        sub = new ZilSubr(del);

                    string name = attr.Name ?? mi.Name;

                    ZilAtom atom = rootObList[name];
                    globalValues.Add(atom, sub);
                }
            }
        }

        private void InitConstants()
        {
            // compile-time constants
            globalValues.Add(GetStdAtom(StdAtom.ZILCH), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.ZILF), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.ZIL_VERSION), new ZilString(Program.VERSION));
            globalValues.Add(GetStdAtom(StdAtom.PREDGEN), TRUE);
            globalValues.Add(GetStdAtom(StdAtom.PLUS_MODE), zenv.ZVersion > 3 ? TRUE : FALSE);

            // runtime constants
            AddZConstant(GetStdAtom(StdAtom.TRUE_VALUE), TRUE);
            AddZConstant(GetStdAtom(StdAtom.FALSE_VALUE), FALSE);

            // parts of speech
            var parts = new[] {
                new { N="P1?OBJECT", V=PartOfSpeech.ObjectFirst },
                new { N="P1?VERB", V=PartOfSpeech.VerbFirst },
                new { N="P1?ADJECTIVE", V=PartOfSpeech.AdjectiveFirst },
                new { N="P1?DIRECTION", V=PartOfSpeech.DirectionFirst },

                new { N="PS?BUZZ-WORD", V=PartOfSpeech.Buzzword },
                new { N="PS?PREPOSITION", V=PartOfSpeech.Preposition },
                new { N="PS?DIRECTION", V=PartOfSpeech.Direction },
                new { N="PS?ADJECTIVE", V=PartOfSpeech.Adjective },
                new { N="PS?VERB", V=PartOfSpeech.Verb },
                new { N="PS?OBJECT", V=PartOfSpeech.Object },
            };

            foreach (var i in parts)
            {
                ZilAtom atom = rootObList[i.N];
                AddZConstant(atom, new ZilFix((int)i.V));
            }
        }

        public void SetDefaultConstants()
        {
            if (!globalValues.ContainsKey(GetStdAtom(StdAtom.SERIAL)))
                AddZConstant(GetStdAtom(StdAtom.SERIAL), new ZilFix(0));
        }

        private void AddZConstant(ZilAtom atom, ZilObject value)
        {
            ZilConstant constant = new ZilConstant(atom, value);
            zenv.Constants.Add(constant);
            globalValues.Add(atom, constant);
        }

        /// <summary>
        /// Copies the context and creates a new, empty local binding environment.
        /// </summary>
        /// <returns>The newly created context, which shares everything with
        /// this context except the local bindings. Only the OBLIST local is copied.</returns>
        public Context CloneWithNewLocals()
        {
            Context result = (Context)MemberwiseClone();
            result.localValues = new Dictionary<ZilAtom, Binding>();

            ZilAtom oblistAtom = GetStdAtom(StdAtom.OBLIST);
            Binding binding;
            if (localValues.TryGetValue(oblistAtom, out binding) == true)
            {
                while (binding.Prev != null)
                    binding = binding.Prev;
                result.localValues.Add(oblistAtom, binding);
            }

            return result;
        }

        /// <summary>
        /// Gets the specified standard atom.
        /// </summary>
        /// <param name="id">The identifier of the standard atom.</param>
        /// <returns>The atom.</returns>
        public ZilAtom GetStdAtom(StdAtom id)
        {
            return stdAtoms[(int)id];
        }

        /// <summary>
        /// Gets the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <returns>The associated value, or null if no value is associated with the pair.</returns>
        public ZilObject GetProp(ZilObject first, ZilObject second)
        {
            AssocPair pair = new AssocPair(first, second);
            ZilObject result;
            if (associations.TryGetValue(pair, out result))
                return result;

            return null;
        }

        /// <summary>
        /// Sets or clears the value associated with a pair of objects.
        /// </summary>
        /// <param name="first">The first object in the pair.</param>
        /// <param name="second">The second object in the pair.</param>
        /// <param name="value">The value to be associated with the pair, or
        /// null to clear the association.</param>
        public void PutProp(ZilObject first, ZilObject second, ZilObject value)
        {
            AssocPair pair = new AssocPair(first, second);
            if (value == null)
                associations.Remove(pair);
            else
                associations[pair] = value;
        }

        /// <summary>
        /// Gets the local value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The local value, or null if no local value is assigned.</returns>
        public ZilObject GetLocalVal(ZilAtom atom)
        {
            Binding b;
            if (localValues.TryGetValue(atom, out b))
                return b.Value;
            else
                return null;
        }

        /// <summary>
        /// Sets or clears the local value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new local value, or null to clear the local value.</param>
        public void SetLocalVal(ZilAtom atom, ZilObject value)
        {
            Binding b;
            if (localValues.TryGetValue(atom, out b))
                b.Value = value;
            else if (value != null)
                localValues[atom] = new Binding(value);
        }

        /// <summary>
        /// Creates a new local binding for an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new local value for the atom, or null.</param>
        public void PushLocalVal(ZilAtom atom, ZilObject value)
        {
            Binding newb = new Binding(value);
            localValues.TryGetValue(atom, out newb.Prev);
            localValues[atom] = newb;
        }

        /// <summary>
        /// Removes the most recent local binding for an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        public void PopLocalVal(ZilAtom atom)
        {
            Binding b;
            if (localValues.TryGetValue(atom, out b))
            {
                if (b.Prev == null)
                    localValues.Remove(atom);
                else
                    localValues[atom] = b.Prev;
            }
        }

        /// <summary>
        /// Gets the global value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <returns>The global value, or null if no global value is assigned.</returns>
        public ZilObject GetGlobalVal(ZilAtom atom)
        {
            ZilObject result;
            if (globalValues.TryGetValue(atom, out result))
                return result;
            else
                return null;
        }

        /// <summary>
        /// Sets or clears the global value assigned to an atom.
        /// </summary>
        /// <param name="atom">The atom.</param>
        /// <param name="value">The new global value, or null to clear the global value.</param>
        public void SetGlobalVal(ZilAtom atom, ZilObject value)
        {
            if (value == null)
                globalValues.Remove(atom);
            else
                globalValues[atom] = value;
        }

        public bool AllowRedefine
        {
            get
            {
                ZilObject lval = GetLocalVal(GetStdAtom(StdAtom.REDEFINE));
                return lval != null && lval.IsTrue;
            }
        }

        public void Redefine(ZilAtom atom)
        {
            ZilObject obj = GetGlobalVal(atom);

            if (obj is ZilGlobal)
                zenv.Globals.Remove((ZilGlobal)obj);
            else if (obj is ZilRoutine)
                zenv.Routines.Remove((ZilRoutine)obj);
            else if (obj is ZilModelObject)
                zenv.Objects.Remove((ZilModelObject)obj);
            else if (obj is ZilConstant)
                zenv.Constants.Remove((ZilConstant)obj);
        }

        public void HandleWarning(ISourceLine node, string message, bool compiler)
        {
            Console.Error.WriteLine("[warning] {0}{1}{2}",
                node == null ? "" : node.SourceInfo,
                node == null ? "" : ": ",
                message);
        }

        public void HandleError(ZilError ex)
        {
            errorCount++;
            Console.Error.WriteLine("[error] {0}{1}", ex.SourcePrefix, ex.Message);
        }

        public string FindIncludeFile(string name)
        {
            if (File.Exists(name))
                return name;

            name = Path.ChangeExtension(name, ".zil");
            if (File.Exists(name))
                return name;

            throw new FileNotFoundException();
        }
    }

    /// <summary>
    /// A base class for exceptions used to implement wacky interpreter flow control.
    /// </summary>
    abstract class ControlException : Exception
    {
        protected ControlException(string name)
            : base(name)
        {
        }
    }

    /// <summary>
    /// Indicates that an inner block is returning.
    /// </summary>
    class ReturnException : ControlException
    {
        private readonly ZilObject value;

        public ReturnException(ZilObject value)
            : base("RETURN")
        {
            this.value = value;
        }

        public ZilObject Value
        {
            get { return value; }
        }
    }

    /// <summary>
    /// Indicates that an inner block is repeating.
    /// </summary>
    class AgainException : ControlException
    {
        public AgainException()
            : base("AGAIN")
        {
        }
    }
}
