/* Copyright 2010-2026 Tara McGrew
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
using System.IO;
using System.Linq;

namespace Zilf.Emit.Intermediate
{
    internal sealed class IrRoutineCoordinator
    {
        private readonly List<IrRoutineBuilder> routines = [];

#if DEBUG
        private readonly Dictionary<string, int> optimizationStatistics = new(StringComparer.Ordinal);
#endif

        internal int PendingCount => routines.Count;

        public void Add(IrRoutineBuilder routine) => routines.Add(routine);

        public void SetPropertyRoutineTargets(
            IReadOnlyDictionary<object, IReadOnlySet<IrRoutineEffectSummary>> targets)
        {
            foreach (var routine in routines)
                routine.SetPropertyRoutineTargets(targets);
        }

        public void FinalizeRoutines()
        {
            IrRoutineBuilder.FinalizeRoutines(routines);
            routines.Clear();
        }

#if DEBUG
        public void RecordOptimizationStatistics(IEnumerable<IrOptimizationStat> statistics)
        {
            foreach (var statistic in statistics)
            {
                optimizationStatistics[statistic.Name] =
                    optimizationStatistics.GetValueOrDefault(statistic.Name) + statistic.Count;
            }
        }

        public void WriteOptimizationStatistics(TextWriter writer, string indent)
        {
            if (optimizationStatistics.Count == 0)
                return;
            writer.WriteLine(indent + "; Routine IR optimization statistics (debug build)");
            foreach (var entry in optimizationStatistics.OrderBy(static entry => entry.Key, StringComparer.Ordinal))
                writer.WriteLine(indent + $";   {entry.Key}: {entry.Value}");
            writer.WriteLine();
        }
#else
        public void RecordOptimizationStatistics(IEnumerable<IrOptimizationStat> statistics)
        {
        }

        public void WriteOptimizationStatistics(TextWriter writer, string indent)
        {
        }
#endif
    }
}
