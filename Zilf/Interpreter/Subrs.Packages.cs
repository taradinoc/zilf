/* Copyright 2010-2018 Jesse McGrew
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

using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [NotNull]
        [Subr]
        [Subr("ZPACKAGE")]
        [Subr("ZZPACKAGE")]
        public static ZilObject PACKAGE([NotNull] Context ctx, [NotNull] string pname)
        {
            // external oblist
            var externalAtom = ctx.PackageObList[pname];
            var externalObList = ctx.GetProp(externalAtom, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList ?? ctx.MakeObList(externalAtom);

            // internal oblist
            var iname = "I" + pname;
            var internalAtom = externalObList[iname];
            var internalObList = ctx.GetProp(internalAtom, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList ?? ctx.MakeObList(internalAtom);

            // new oblist path
            var newObPath = new ZilList(new ZilObject[] { internalObList, externalObList, ctx.RootObList });
            ctx.PushObPath(newObPath);
            ctx.SetGlobalVal(externalAtom, newObPath);

            // package type
            ctx.PutProp(externalObList, ctx.GetStdAtom(StdAtom.PACKAGE), ctx.GetStdAtom(StdAtom.PACKAGE));

            return externalAtom;
        }

        [NotNull]
        [Subr]
        [Subr("ZSECTION")]
        [Subr("ZZSECTION")]
        public static ZilObject DEFINITIONS([NotNull] Context ctx, [NotNull] string pname)
        {
            // external oblist
            var externalAtom = ctx.PackageObList[pname];
            var externalObList = ctx.GetProp(externalAtom, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList ?? ctx.MakeObList(externalAtom);

            // new oblist path
            var newObPath = new ZilList(new ZilObject[] { externalObList, ctx.RootObList });
            ctx.PushObPath(newObPath);
            ctx.SetGlobalVal(externalAtom, newObPath);

            // package type
            ctx.PutProp(externalObList, ctx.GetStdAtom(StdAtom.PACKAGE), ctx.GetStdAtom(StdAtom.DEFINITIONS));

            return externalAtom;
        }

        [NotNull]
        [Subr]
        [Subr("END-DEFINITIONS")]
        [Subr("ENDSECTION")]
        public static ZilObject ENDPACKAGE([NotNull] Context ctx)
        {
            return ENDBLOCK(ctx);
        }

        /// <exception cref="InterpreterError">OBLIST path is malformed.</exception>
        [NotNull]
        [Subr]
        public static ZilObject ENTRY([NotNull] Context ctx, [NotNull] ZilAtom[] args)
        {
            if (!(ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) is ZilList currentObPath) ||
                currentObPath.GetLength(1) != null ||
                currentObPath.Take(2).Any(zo => zo.StdTypeAtom != StdAtom.OBLIST))
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Value_Of_1_Must_Be_2,
                    "local",
                    "OBLIST",
                    "a list starting with 2 OBLISTs");
            }

            Debug.Assert(currentObPath.First != null);
            Debug.Assert(currentObPath.Rest != null);
            Debug.Assert(currentObPath.Rest.First is ObList);

            var internalObList = (ObList)currentObPath.First;
            var externalObList = (ObList)currentObPath.Rest.First;

            // make sure we're inside a PACKAGE
            var packageAtom = ctx.GetStdAtom(StdAtom.PACKAGE);
            if (ctx.GetProp(internalObList, packageAtom) != null || ctx.GetProp(externalObList, packageAtom) != packageAtom)
                throw new InterpreterError(InterpreterMessages._0_Must_Be_Called_From_Within_A_PACKAGE, "ENTRY");

            var onWrongOblist = args.Where(a => a.ObList != internalObList && a.ObList != externalObList).ToList();
            if (onWrongOblist.Count > 0)
            {
                throw new InterpreterError(
                    InterpreterMessages._0_All_Atoms_Must_Be_On_Internal_Oblist_1_Failed_For_2,
                    "ENTRY",
                    ctx.GetProp(internalObList, ctx.GetStdAtom(StdAtom.OBLIST))?.ToStringContext(ctx, false),
                    string.Join(", ", onWrongOblist.Select(a => a.ToStringContext(ctx, false))));
            }

            foreach (var atom in args)
                atom.ObList = externalObList;

            return ctx.TRUE;
        }

        /// <exception cref="InterpreterError">OBLIST path is malformed.</exception>
        [NotNull]
        [Subr]
        public static ZilObject RENTRY([NotNull] Context ctx, [NotNull] ZilAtom[] args)
        {
            if (!(ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) is ZilList currentObPath) ||
                currentObPath.GetLength(1) != null ||
                currentObPath.Take(2).Any(zo => zo.StdTypeAtom != StdAtom.OBLIST))
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Value_Of_1_Must_Be_2,
                    "local",
                    "OBLIST",
                    "a list starting with 2 OBLISTs");
            }

            Debug.Assert(currentObPath.First != null);
            Debug.Assert(currentObPath.Rest != null);
            Debug.Assert(currentObPath.Rest.First is ObList);

            var internalObList = (ObList)currentObPath.First;
            var externalObList = (ObList)currentObPath.Rest.First;

            // make sure we're inside a PACKAGE or DEFINITIONS
            var packageAtom = ctx.GetStdAtom(StdAtom.PACKAGE);
            var internalPackageProp = ctx.GetProp(internalObList, packageAtom);
            var externalPackageProp = ctx.GetProp(externalObList, packageAtom);
            if (internalPackageProp != ctx.GetStdAtom(StdAtom.DEFINITIONS) && externalPackageProp != packageAtom)
                throw new InterpreterError(InterpreterMessages._0_Must_Be_Called_From_Within_A_PACKAGE_Or_DEFINITIONS, "RENTRY");

            var onWrongOblist = args.Where(a => a.ObList != internalObList && a.ObList != ctx.RootObList).ToList();
            if (onWrongOblist.Count > 0)
            {
                throw new InterpreterError(InterpreterMessages._0_All_Atoms_Must_Be_On_Internal_Oblist_1_Failed_For_2,
                    "RENTRY",
                    ctx.GetProp(internalObList, ctx.GetStdAtom(StdAtom.OBLIST))?.ToStringContext(ctx, false),
                    string.Join(", ", onWrongOblist.Select(a => a.ToStringContext(ctx, false))));
            }

            foreach (var atom in args)
                atom.ObList = ctx.RootObList;

            return ctx.TRUE;
        }

        [NotNull]
        [Subr]
        public static ZilObject USE([NotNull] Context ctx, [NotNull] string[] args)
        {
            return PerformUse(ctx, args, "USE", StdAtom.PACKAGE);
        }

        [NotNull]
        [Subr]
        public static ZilObject INCLUDE([NotNull] Context ctx, [NotNull] string[] args)
        {
            return PerformUse(ctx, args, "INCLUDE", StdAtom.DEFINITIONS);
        }

        [NotNull]
        [Subr("USE-WHEN")]
        public static ZilObject USE_WHEN(Context ctx, [NotNull] ZilObject condition, string[] args)
        {
            if (condition.IsTrue)
            {
                return PerformUse(ctx, args, "USE-WHEN", StdAtom.PACKAGE);
            }
            return condition;
        }

        [NotNull]
        [Subr("INCLUDE-WHEN")]
        public static ZilObject INCLUDE_WHEN(Context ctx, [NotNull] ZilObject condition, string[] args)
        {
            if (condition.IsTrue)
            {
                return PerformUse(ctx, args, "INCLUDE-WHEN", StdAtom.DEFINITIONS);
            }
            return condition;
        }

        [NotNull]
        static ZilObject PerformUse([NotNull] Context ctx, [NotNull] string[] args, string name, StdAtom requiredPackageType)
        {
            if (!(ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) is ZilList obpath))
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Value_Of_1_Must_Be_2,
                    "local",
                    "OBLIST",
                    "a list starting with 2 OBLISTs");
            }

            if (args.Length == 0)
                return ctx.TRUE;

            var obpathList = obpath.ToList();

            foreach (var packageName in args)
            {
                if (!ctx.PackageObList.Contains(packageName))
                {
                    // try loading from file
                    PerformLoadFile(ctx, packageName, name);  // throws on failure
                }

                ObList externalObList = null;
                if (ctx.PackageObList.Contains(packageName))
                {
                    var packageNameAtom = ctx.PackageObList[packageName];
                    externalObList = ctx.GetProp(packageNameAtom, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList;
                }

                if (externalObList == null)
                    throw new InterpreterError(InterpreterMessages._0_Unrecognized_1_2, name, "package", packageName);

                if (!(ctx.GetProp(externalObList, ctx.GetStdAtom(StdAtom.PACKAGE)) is ZilAtom pkgTypeAtom) ||
                    pkgTypeAtom.StdAtom != requiredPackageType)
                {
                    throw new InterpreterError(InterpreterMessages._0_Wrong_Package_Type_Expected_1, name, ctx.GetStdAtom(requiredPackageType).ToString());
                }

                if (!obpathList.Contains(externalObList))
                    obpathList.Add(externalObList);
            }

            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST), new ZilList(obpathList));
            return ctx.TRUE;
        }

        [NotNull]
        [Subr("COMPILING?")]
        public static ZilObject COMPILING_P([NotNull] Context ctx, ZilObject[] args)
        {
            // always true
            return ctx.TRUE;
        }
    }
}