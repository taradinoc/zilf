/*
 * Copyright © 2008, Textfyre, Inc. - All Rights Reserved
 * Please read the accompanying COPYRIGHT file for licensing restrictions.
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace FyreVM
{
    public partial class Engine
    {
        /// <summary>
        /// Identifies a veneer routine that is intercepted, or a constant that
        /// the replacement routine needs to use.
        /// </summary>
        private enum VeneerSlot
        {
            // routine addresses
            Z__Region = 1,
            CP__Tab = 2,
            OC__Cl = 3,
            RA__Pr = 4,
            RT__ChLDW = 5,
            Unsigned__Compare = 6,
            RL__Pr = 7,
            RV__Pr = 8,
            OP__Pr = 9,
            RT__ChSTW = 10,
            RT__ChLDB = 11,
            Meta__class = 12,

            // object numbers and compiler constants
            String = 1001,
            Routine = 1002,
            Class = 1003,
            Object = 1004,
            RT__Err = 1005,
            NUM_ATTR_BYTES = 1006,
            classes_table = 1007,
            INDIV_PROP_START = 1008,
            cpv__start = 1009,
            ofclass_err = 1010,
            readprop_err = 1011,
        }

        /// <summary>
        /// Provides hardcoded versions of some commonly used veneer routines (low-level
        /// functions that are automatically compiled into every Inform game).
        /// </summary>
        /// <remarks>
        /// Inform games rely heavily on these routines, and substituting our C# versions
        /// for the Glulx versions in the story file can increase performance significantly.
        /// </remarks>
        private class Veneer
        {
            private uint zregion_fn, cp_tab_fn, oc_cl_fn, ra_pr_fn, rt_chldw_fn;
            private uint unsigned_compare_fn, rl_pr_fn, rv_pr_fn, op_pr_fn;
            private uint rt_chstw_fn, rt_chldb_fn, meta_class_fn;

            private uint string_mc, routine_mc, class_mc, object_mc;
            private uint rt_err_fn, num_attr_bytes, classes_table;
            private uint indiv_prop_start, cpv_start;
            private uint ofclass_err, readprop_err;

            // RAM addresses of compiler-generated global variables
            private const uint SELF_OFFSET = 16;
            private const uint SENDER_OFFSET = 20;
            // offsets of compiler-generated property numbers from INDIV_PROP_START
            private const uint CALL_PROP = 5;
            private const uint PRINT_PROP = 6;
            private const uint PRINT_TO_ARRAY_PROP = 7;

            private static readonly Dictionary<uint, VeneerSlot> funcSlotMap, paramSlotMap;

            static Veneer()
            {
                funcSlotMap = new Dictionary<uint, VeneerSlot>();
                funcSlotMap.Add(1, VeneerSlot.Z__Region);
                funcSlotMap.Add(2, VeneerSlot.CP__Tab);
                funcSlotMap.Add(3, VeneerSlot.RA__Pr);
                funcSlotMap.Add(4, VeneerSlot.RL__Pr);
                funcSlotMap.Add(5, VeneerSlot.OC__Cl);
                funcSlotMap.Add(6, VeneerSlot.RV__Pr);
                funcSlotMap.Add(7, VeneerSlot.OP__Pr);

                paramSlotMap = new Dictionary<uint, VeneerSlot>();
                paramSlotMap.Add(0, VeneerSlot.classes_table);
                paramSlotMap.Add(1, VeneerSlot.INDIV_PROP_START);
                paramSlotMap.Add(2, VeneerSlot.Class);
                paramSlotMap.Add(3, VeneerSlot.Object);
                paramSlotMap.Add(4, VeneerSlot.Routine);
                paramSlotMap.Add(5, VeneerSlot.String);
                //paramSlotMap.Add(6, VeneerSlot.self)
                paramSlotMap.Add(7, VeneerSlot.NUM_ATTR_BYTES);
                paramSlotMap.Add(8, VeneerSlot.cpv__start);
            }

            /// <summary>
            /// Registers a routine address or constant value, using the traditional
            /// FyreVM slot codes.
            /// </summary>
            /// <param name="slot">Identifies the address or constant being registered.</param>
            /// <param name="value">The address of the routine or value of the constant.</param>
            /// <returns><see langword="true"/> if registration was successful.</returns>
            public bool SetSlotFyre(uint slot, uint value)
            {
                switch ((VeneerSlot)slot)
                {
                    case VeneerSlot.Z__Region: zregion_fn = value; break;
                    case VeneerSlot.CP__Tab: cp_tab_fn = value; break;
                    case VeneerSlot.OC__Cl: oc_cl_fn = value; break;
                    case VeneerSlot.RA__Pr: ra_pr_fn = value; break;
                    case VeneerSlot.RT__ChLDW: rt_chldw_fn = value; break;
                    case VeneerSlot.Unsigned__Compare: unsigned_compare_fn = value; break;
                    case VeneerSlot.RL__Pr: rl_pr_fn = value; break;
                    case VeneerSlot.RV__Pr: rv_pr_fn = value; break;
                    case VeneerSlot.OP__Pr: op_pr_fn = value; break;
                    case VeneerSlot.RT__ChSTW: rt_chstw_fn = value; break;
                    case VeneerSlot.RT__ChLDB: rt_chldb_fn = value; break;
                    case VeneerSlot.Meta__class: meta_class_fn = value; break;

                    case VeneerSlot.String: string_mc = value; break;
                    case VeneerSlot.Routine: routine_mc = value; break;
                    case VeneerSlot.Class: class_mc = value; break;
                    case VeneerSlot.Object: object_mc = value; break;
                    case VeneerSlot.RT__Err: rt_err_fn = value; break;
                    case VeneerSlot.NUM_ATTR_BYTES: num_attr_bytes = value; break;
                    case VeneerSlot.classes_table: classes_table = value; break;
                    case VeneerSlot.INDIV_PROP_START: indiv_prop_start = value; break;
                    case VeneerSlot.cpv__start: cpv_start = value; break;
                    case VeneerSlot.ofclass_err: ofclass_err = value; break;
                    case VeneerSlot.readprop_err: readprop_err = value; break;

                    default:
                        // not recognized
                        return false;
                }

                // recognized
                return true;
            }

            /// <summary>
            /// Registers a routine address or constant value, using the acceleration
            /// codes defined in the Glulx specification.
            /// </summary>
            /// <param name="e">The <see cref="Engine"/> for which the value is being set.</param>
            /// <param name="isParam"><see langword="true"/> to set a constant value;
            /// <b>false</b> to set a routine address.</param>
            /// <param name="slot">The routine or constant index to set.</param>
            /// <param name="value">The address of the routine or value of the constant.</param>
            /// <returns><see langword="true"/> if registration was successful.</returns>
            public bool SetSlotGlulx(Engine e, bool isParam, uint slot, uint value)
            {
                if (isParam && slot == 6)
                {
                    if (value != e.image.RamStart + SELF_OFFSET)
                        throw new ArgumentException("Unexpected value for acceleration parameter 6");
                    return true;
                }

                Dictionary<uint, VeneerSlot> dict = isParam ? paramSlotMap : funcSlotMap;
                VeneerSlot fyreSlot;
                if (dict.TryGetValue(slot, out fyreSlot))
                    return SetSlotFyre((uint)fyreSlot, value);
                else
                    return false;
            }

            /// <summary>
            /// Tests whether a particular function is supported for acceleration,
            /// using the codes defined in the Glulx specification.
            /// </summary>
            /// <param name="slot">The routine index.</param>
            /// <returns><see langword="true"/> if the function code is supported.</returns>
            public bool ImplementsFuncGlulx(uint slot)
            {
                return funcSlotMap.ContainsKey(slot);
            }

            /// <summary>
            /// Intercepts a routine call if its address has previously been registered.
            /// </summary>
            /// <param name="e">The <see cref="Engine"/> attempting to call the routine.</param>
            /// <param name="address">The address of the routine.</param>
            /// <param name="args">The routine's arguments.</param>
            /// <param name="result">The routine's return value.</param>
            /// <returns><see langword="true"/> if the call was intercepted.</returns>
            /// <exception cref="IndexOutOfRangeException">
            /// <paramref name="address"/> matches a registered veneer routine, but
            /// <paramref name="args"/> is too short for that routine.
            /// </exception>
            public bool InterceptCall(Engine e, uint address, uint[] args, out uint result)
            {
                if (address != 0)
                {
                    if (address == zregion_fn)
                    {
                        result = Z__Region(e, args[0]);
                        return true;
                    }

                    if (address == cp_tab_fn)
                    {
                        result = CP__Tab(e, args[0], args[1]);
                        return true;
                    }

                    if (address == oc_cl_fn)
                    {
                        result = OC__Cl(e, args[0], args[1]);
                        return true;
                    }

                    if (address == ra_pr_fn)
                    {
                        result = RA__Pr(e, args[0], args[1]);
                        return true;
                    }

                    if (address == rt_chldw_fn)
                    {
                        result = RT__ChLDW(e, args[0], args[1]);
                        return true;
                    }

                    if (address == unsigned_compare_fn)
                    {
                        result = (uint)args[0].CompareTo(args[1]);
                        return true;
                    }

                    if (address == rl_pr_fn)
                    {
                        result = RL__Pr(e, args[0], args[1]);
                        return true;
                    }

                    if (address == rv_pr_fn)
                    {
                        result = RV__Pr(e, args[0], args[1]);
                        return true;
                    }

                    if (address == op_pr_fn)
                    {
                        result = OP__Pr(e, args[0], args[1]);
                        return true;
                    }

                    if (address == rt_chstw_fn)
                    {
                        result = RT__ChSTW(e, args[0], args[1], args[2]);
                        return true;
                    }

                    if (address == rt_chldb_fn)
                    {
                        result = RT__ChLDB(e, args[0], args[1]);
                        return true;
                    }

                    if (address == meta_class_fn)
                    {
                        result = Meta__class(e, args[0]);
                        return true;
                    }
                }

                result = 0;
                return false;
            }

            // distinguishes between strings, routines, and objects
            private uint Z__Region(Engine e, uint address)
            {
                if (address < 36 || address >= e.image.EndMem)
                    return 0;

                byte type = e.image.ReadByte(address);
                if (type >= 0xE0)
                    return 3;
                if (type >= 0xC0)
                    return 2;
                if (type >= 0x70 && type <= 0x7F && address >= e.image.RamStart)
                    return 1;

                return 0;
            }

            // finds an object's common property table
            private uint CP__Tab(Engine e, uint obj, uint id)
            {
                if (Z__Region(e, obj) != 1)
                {
                    e.NestedCall(rt_err_fn, 23, obj);
                    return 0;
                }

                uint otab = e.image.ReadInt32(obj + 16);
                if (otab == 0)
                    return 0;
                uint max = e.image.ReadInt32(otab);
                otab += 4;
                return e.PerformBinarySearch(id, 2, otab, 10, max, 0, SearchOptions.None);
            }

            // finds the location of an object ("parent()" function)
            private uint Parent(Engine e, uint obj)
            {
                return e.image.ReadInt32(obj + 1 + num_attr_bytes + 12);
            }

            // determines whether an object is a member of a given class ("ofclass" operator)
            private uint OC__Cl(Engine e, uint obj, uint cla)
            {
                switch (Z__Region(e, obj))
                {
                    case 3:
                        return (uint)(cla == string_mc ? 1 : 0);

                    case 2:
                        return (uint)(cla == routine_mc ? 1 : 0);

                    case 1:
                        if (cla == class_mc)
                        {
                            if (Parent(e, obj) == class_mc)
                                return 1;
                            if (obj == class_mc || obj == string_mc ||
                                obj == routine_mc || obj == object_mc)
                                return 1;
                            return 0;
                        }

                        if (cla == object_mc)
                        {
                            if (Parent(e, obj) == class_mc)
                                return 0;
                            if (obj == class_mc || obj == string_mc ||
                                obj == routine_mc || obj == object_mc)
                                return 0;
                            return 1;
                        }

                        if (cla == string_mc || cla == routine_mc)
                            return 0;

                        if (Parent(e, cla) != class_mc)
                        {
                            e.NestedCall(rt_err_fn, ofclass_err, cla, 0xFFFFFFFF);
                            return 0;
                        }

                        uint inlist = RA__Pr(e, obj, 2);
                        if (inlist == 0)
                            return 0;

                        uint inlistlen = RL__Pr(e, obj, 2) / 4;
                        for (uint jx = 0; jx < inlistlen; jx++)
                            if (e.image.ReadInt32(inlist + jx * 4) == cla)
                                return 1;

                        return 0;

                    default:
                        return 0;
                }
            }

            // finds the address of an object's property (".&" operator)
            private uint RA__Pr(Engine e, uint obj, uint id)
            {
                uint cla = 0;
                if ((id & 0xFFFF0000) != 0)
                {
                    cla = e.image.ReadInt32(classes_table + 4 * (id & 0xFFFF));
                    if (OC__Cl(e, obj, cla) == 0)
                        return 0;

                    id >>= 16;
                    obj = cla;
                }

                uint prop = CP__Tab(e, obj, id);
                if (prop == 0)
                    return 0;

                if (Parent(e, obj) == class_mc && cla == 0)
                    if (id < indiv_prop_start || id >= indiv_prop_start + 8)
                        return 0;

                if (e.image.ReadInt32(e.image.RamStart + SELF_OFFSET) != obj)
                {
                    int ix = (e.image.ReadByte(prop + 9) & 1);
                    if (ix != 0)
                        return 0;
                }

                return e.image.ReadInt32(prop + 4);
            }

            // finds the length of an object's property (".#" operator)
            private uint RL__Pr(Engine e, uint obj, uint id)
            {
                uint cla = 0;
                if ((id & 0xFFFF0000) != 0)
                {
                    cla = e.image.ReadInt32(classes_table + 4 * (id & 0xFFFF));
                    if (OC__Cl(e, obj, cla) == 0)
                        return 0;

                    id >>= 16;
                    obj = cla;
                }

                uint prop = CP__Tab(e, obj, id);
                if (prop == 0)
                    return 0;

                if (Parent(e, obj) == class_mc && cla == 0)
                    if (id < indiv_prop_start || id >= indiv_prop_start + 8)
                        return 0;

                if (e.image.ReadInt32(e.image.RamStart + SELF_OFFSET) != obj)
                {
                    int ix = (e.image.ReadByte(prop + 9) & 1);
                    if (ix != 0)
                        return 0;
                }

                return (uint)(4 * e.image.ReadInt16(prop + 2));
            }

            // performs bounds checking when reading from a word array ("-->" operator)
            private uint RT__ChLDW(Engine e, uint array, uint offset)
            {
                uint address = array + 4 * offset;
                if (address >= e.image.EndMem)
                {
                    return e.NestedCall(rt_err_fn, 25);
                }
                return e.image.ReadInt32(address);
            }

            // reads the value of an object's property ("." operator)
            private uint RV__Pr(Engine e, uint obj, uint id)
            {
                uint addr = RA__Pr(e, obj, id);
                if (addr == 0)
                {
                    if (id > 0 && id < indiv_prop_start)
                        return e.image.ReadInt32(cpv_start + 4 * id);

                    e.NestedCall(rt_err_fn, readprop_err, obj, id);
                    return 0;
                }

                return e.image.ReadInt32(addr);
            }

            // determines whether an object provides a given property ("provides" operator)
            private uint OP__Pr(Engine e, uint obj, uint id)
            {
                switch (Z__Region(e, obj))
                {
                    case 3:
                        if (id == indiv_prop_start + PRINT_PROP ||
                            id == indiv_prop_start + PRINT_TO_ARRAY_PROP)
                            return 1;
                        else
                            return 0;

                    case 2:
                        if (id == indiv_prop_start + CALL_PROP)
                            return 1;
                        else
                            return 0;

                    case 1:
                        if (id >= indiv_prop_start && id < indiv_prop_start + 8)
                            if (Parent(e, obj) == class_mc)
                                return 1;

                        if (RA__Pr(e, obj, id) != 0)
                            return 1;
                        else
                            return 0;

                    default:
                        return 0;
                }
            }

            // performs bounds checking when writing to a word array ("-->" operator)
            private uint RT__ChSTW(Engine e, uint array, uint offset, uint val)
            {
                uint address = array + 4 * offset;
                if (address >= e.image.EndMem || address < e.image.RamStart)
                {
                    return e.NestedCall(rt_err_fn, 27);
                }
                else
                {
                    e.image.WriteInt32(address, val);
                    return 0;
                }
            }

            // performs bounds checking when reading from a byte array ("->" operator)
            private uint RT__ChLDB(Engine e, uint array, uint offset)
            {
                uint address = array + offset;
                if (address >= e.image.EndMem)
                    return e.NestedCall(rt_err_fn, 24);

                return e.image.ReadByte(address);
            }

            // determines the metaclass of a routine, string, or object ("metaclass()" function)
            private uint Meta__class(Engine e, uint obj)
            {
                switch (Z__Region(e, obj))
                {
                    case 2:
                        return routine_mc;
                    case 3:
                        return string_mc;
                    case 1:
                        if (Parent(e, obj) == class_mc)
                            return class_mc;
                        if (obj == class_mc || obj == string_mc ||
                            obj == routine_mc || obj == object_mc)
                            return class_mc;
                        return object_mc;
                    default:
                        return 0;
                }
            }
        }
    }
}
