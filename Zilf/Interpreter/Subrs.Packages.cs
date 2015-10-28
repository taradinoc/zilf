﻿/* Copyright 2010, 2015 Jesse McGrew
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

namespace Zilf.Interpreter
{
    static partial class Subrs
    {
        [Subr]
        [Subr("ZPACKAGE")]
        [Subr("ZZPACKAGE")]
        public static ZilObject PACKAGE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("PACKAGE", 1, 1);
            if (args[0].GetTypeAtom(ctx).StdAtom != StdAtom.STRING)
                throw new InterpreterError("PACKAGE: arg must be a string");

            var pname = ((ZilString)args[0]).Text;
            if (ctx.PackageObList.Contains(pname))
                throw new InterpreterError("PACKAGE: already defined: " + pname);

            // external oblist
            var externalAtom = ctx.PackageObList[pname];
            var externalObList = ctx.MakeObList(externalAtom);

            // internal oblist
            var iname = "I" + pname;
            var internalAtom = externalObList[iname];
            var internalObList = ctx.MakeObList(internalAtom);

            // new oblist path
            var curObPath = (ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) as ZilList) ?? new ZilList(null, null);
            var newObPath = new ZilList(internalObList, new ZilList(externalObList, curObPath));
            ctx.PushObPath(newObPath);

            // package type
            ctx.PutProp(externalObList, ctx.GetStdAtom(StdAtom.PACKAGE), ctx.GetStdAtom(StdAtom.PACKAGE));

            return externalAtom;
        }

        [Subr]
        public static ZilObject DEFINITIONS(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Length != 1)
                throw new InterpreterError("DEFINITIONS", 1, 1);
            if (args[0].GetTypeAtom(ctx).StdAtom != StdAtom.STRING)
                throw new InterpreterError("DEFINITIONS: arg must be a string");

            var pname = ((ZilString)args[0]).Text;
            if (ctx.PackageObList.Contains(pname))
                throw new InterpreterError("DEFINITIONS: already defined: " + pname);

            // external oblist
            var externalAtom = ctx.PackageObList[pname];
            var externalObList = ctx.MakeObList(externalAtom);

            // new oblist path
            var curObPath = (ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) as ZilList) ?? new ZilList(null, null);
            var newObPath = new ZilList(externalObList, curObPath);
            ctx.PushObPath(newObPath);

            // package type
            ctx.PutProp(externalObList, ctx.GetStdAtom(StdAtom.PACKAGE), ctx.GetStdAtom(StdAtom.DEFINITIONS));

            return externalAtom;
        }

        [Subr]
        [Subr("END-DEFINITIONS")]
        public static ZilObject ENDPACKAGE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            return ENDBLOCK(ctx, args);
        }

        [Subr]
        public static ZilObject ENTRY(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Any(zo => zo.GetTypeAtom(ctx).StdAtom != StdAtom.ATOM))
                throw new InterpreterError("ENTRY: all args must be atoms");

            var currentObPath = ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) as ZilList;
            if (currentObPath == null || currentObPath.GetTypeAtom(ctx).StdAtom != StdAtom.LIST ||
                ((IStructure)currentObPath).GetLength(1) != null ||
                currentObPath.Take(2).Any(zo => zo.GetTypeAtom(ctx).StdAtom != StdAtom.OBLIST))
            {
                throw new InterpreterError("ENTRY: LVAL of OBLIST must be a list starting with 2 OBLISTs");
            }

            var internalObList = (ObList)currentObPath.First;
            var externalObList = (ObList)currentObPath.Rest.First;

            // make sure we're inside a PACKAGE
            var packageAtom = ctx.GetStdAtom(StdAtom.PACKAGE);
            if (ctx.GetProp(internalObList, packageAtom) != null || ctx.GetProp(externalObList, packageAtom) != packageAtom)
                throw new InterpreterError("ENTRY: must be called from within a PACKAGE");

            if (args.Cast<ZilAtom>().Any(a => a.ObList != internalObList))
                throw new InterpreterError("ENTRY: all atoms must be on internal oblist");

            foreach (ZilAtom atom in args)
                atom.ObList = externalObList;

            return ctx.TRUE;
        }

        [Subr]
        public static ZilObject USE(Context ctx, ZilObject[] args)
        {
            SubrContracts(ctx, args);

            if (args.Any(zo => zo.GetTypeAtom(ctx).StdAtom != StdAtom.STRING))
                throw new InterpreterError("USE: all args must be strings");

            var obpath = ctx.GetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST)) as ZilList;
            if (obpath == null || obpath.GetTypeAtom(ctx).StdAtom != StdAtom.LIST)
                throw new InterpreterError("USE: bad LVAL of OBLIST");

            if (args.Length == 0)
                return ctx.TRUE;

            var obpathList = obpath.ToList();

            foreach (ZilString packageName in args)
            {
                if (!ctx.PackageObList.Contains(packageName.Text))
                {
                    // try loading from file
                    PerformLoadFile(ctx, packageName.Text, "USE");  // throws on failure
                }

                ObList externalObList = null;
                if (ctx.PackageObList.Contains(packageName.Text))
                {
                    var packageNameAtom = ctx.PackageObList[packageName.Text];
                    externalObList = ctx.GetProp(packageNameAtom, ctx.GetStdAtom(StdAtom.OBLIST)) as ObList;
                }

                if (externalObList == null)
                    throw new InterpreterError("USE: no such package: " + packageName.Text);

                if (!obpathList.Contains(externalObList))
                    obpathList.Add(externalObList);
            }

            ctx.SetLocalVal(ctx.GetStdAtom(StdAtom.OBLIST), new ZilList(obpathList));
            return ctx.TRUE;
        }
    }
}