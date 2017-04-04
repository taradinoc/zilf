/* Copyright 2010-2017 Jesse McGrew
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
using System.Linq;
using Zilf.Interpreter.Values;
using Zilf.Language;
using Zilf.Diagnostics;

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [Subr]
        [Subr("ZPACKAGE")]
        [Subr("ZZPACKAGE")]
        public static ZilObject PACKAGE(Context ctx, string pname)
        {
            SubrContracts(ctx);

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

        [Subr]
        [Subr("ZSECTION")]
        [Subr("ZZSECTION")]
        public static ZilObject DEFINITIONS(Context ctx, string pname)
        {
            SubrContracts(ctx);

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

        [Subr]
        [Subr("END-DEFINITIONS")]
        [Subr("ENDSECTION")]
        public static ZilObject ENDPACKAGE(Context ctx)
        {
            SubrContracts(ctx);

            return ENDBLOCK(ctx);
        }

        [Subr]
        public static ZilObject ENTRY(Context ctx, ZilAtom[] args)
        {
            SubrContracts(ctx);

            if (!(ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) is ZilList currentObPath) ||
                currentObPath.StdTypeAtom != StdAtom.LIST ||
                ((IStructure)currentObPath).GetLength(1) != null ||
                currentObPath.Take(2).Any(zo => zo.StdTypeAtom != StdAtom.OBLIST))
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Value_Of_1_Must_Be_2,
                    "local",
                    "OBLIST",
                    "a list starting with 2 OBLISTs");
            }

            var internalObList = (ObList)currentObPath.First;
            var externalObList = (ObList)currentObPath.Rest.First;

            // make sure we're inside a PACKAGE
            var packageAtom = ctx.GetStdAtom(StdAtom.PACKAGE);
            if (ctx.GetProp(internalObList, packageAtom) != null || ctx.GetProp(externalObList, packageAtom) != packageAtom)
                throw new InterpreterError(InterpreterMessages._0_Must_Be_Called_From_Within_A_PACKAGE, "ENTRY");

            var onWrongOblist = args.Where(a => a.ObList != internalObList && a.ObList != externalObList);
            if (onWrongOblist.Any())
            {
                throw new InterpreterError(InterpreterMessages._0_All_Atoms_Must_Be_On_Internal_Oblist_1_Failed_For_2, "ENTRY", ctx.GetProp(internalObList, ctx.GetStdAtom(StdAtom.OBLIST)).ToStringContext(ctx, false), string.Join(", ", onWrongOblist.Select(a => a.ToStringContext(ctx, false))));
            }

            foreach (var atom in args)
                atom.ObList = externalObList;

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject RENTRY(Context ctx, ZilAtom[] args)
        {
            SubrContracts(ctx);

            if (!(ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) is ZilList currentObPath) ||
                currentObPath.StdTypeAtom != StdAtom.LIST ||
                ((IStructure)currentObPath).GetLength(1) != null ||
                currentObPath.Take(2).Any(zo => zo.StdTypeAtom != StdAtom.OBLIST))
            {
                throw new InterpreterError(
                    InterpreterMessages._0_Value_Of_1_Must_Be_2,
                    "local",
                    "OBLIST",
                    "a list starting with 2 OBLISTs");
            }

            var internalObList = (ObList)currentObPath.First;
            var externalObList = (ObList)currentObPath.Rest.First;

            // make sure we're inside a PACKAGE or DEFINITIONS
            var packageAtom = ctx.GetStdAtom(StdAtom.PACKAGE);
            var internalPackageProp = ctx.GetProp(internalObList, packageAtom);
            var externalPackageProp = ctx.GetProp(externalObList, packageAtom);
            if (internalPackageProp != ctx.GetStdAtom(StdAtom.DEFINITIONS) && externalPackageProp != packageAtom)
                throw new InterpreterError(InterpreterMessages._0_Must_Be_Called_From_Within_A_PACKAGE_Or_DEFINITIONS, "RENTRY");

            var onWrongOblist = args.Where(a => a.ObList != internalObList && a.ObList != ctx.RootObList);
            if (onWrongOblist.Any())
            {
                throw new InterpreterError(InterpreterMessages._0_All_Atoms_Must_Be_On_Internal_Oblist_1_Failed_For_2, "RENTRY", ctx.GetProp(internalObList, ctx.GetStdAtom(StdAtom.OBLIST)).ToStringContext(ctx, false), string.Join(", ", onWrongOblist.Select(a => a.ToStringContext(ctx, false))));
            }

            foreach (var atom in args)
                atom.ObList = ctx.RootObList;

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject USE(Context ctx, string[] args)
        {
            SubrContracts(ctx);

            return PerformUse(ctx, args, "USE", StdAtom.PACKAGE);
        }

        [Subr]
        public static ZilObject INCLUDE(Context ctx, string[] args)
        {
            SubrContracts(ctx);

            return PerformUse(ctx, args, "INCLUDE", StdAtom.DEFINITIONS);
        }

        [Subr("USE-WHEN")]
        public static ZilObject USE_WHEN(Context ctx, ZilObject condition, string[] args)
        {
            SubrContracts(ctx);

            if (condition.IsTrue)
            {
                return PerformUse(ctx, args, "USE-WHEN", StdAtom.PACKAGE);
            }
            return condition;
        }

        [Subr("INCLUDE-WHEN")]
        public static ZilObject INCLUDE_WHEN(Context ctx, ZilObject condition, string[] args)
        {
            SubrContracts(ctx);

            if (condition.IsTrue)
            {
                return PerformUse(ctx, args, "INCLUDE-WHEN", StdAtom.DEFINITIONS);
            }
            return condition;
        }

        static ZilObject PerformUse(Context ctx, string[] args, string name, StdAtom requiredPackageType)
        {
            SubrContracts(ctx);

            if (!(ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) is ZilList obpath) ||
                obpath.StdTypeAtom != StdAtom.LIST)
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

        [Subr("COMPILING?")]
        public static ZilObject COMPILING_P(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            // always true
            return ctx.TRUE;
        }
    }
}