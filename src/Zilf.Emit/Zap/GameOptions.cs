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

using JetBrains.Annotations;

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

namespace Zilf.Emit.Zap
{
    [UsedImplicitly(ImplicitUseKindFlags.InstantiatedNoFixedConstructorSignature)]
    public class GameOptions : IGameOptions
    {
        GameOptions()
        {
        }

        public sealed class V3 : GameOptions
        {
            public bool TimeStatusLine { get; set; }
            public bool SoundEffects { get; set; }
        }

        public abstract class V4Plus : GameOptions
        {
            public bool SoundEffects { get; set; }
        }

        public sealed class V4 : V4Plus { }

        public abstract class V5Plus : V4Plus
        {
            public bool DisplayOps { get; set; }
            public bool Undo { get; set; }
            public bool Mouse { get; set; }
            public bool Color { get; set; }

            public ITableBuilder HeaderExtensionTable { get; set; }

            public string Charset0 { get; set; }
            public string Charset1 { get; set; }
            public string Charset2 { get; set; }

            public int LanguageId { get; set; }
            public char? LanguageEscapeChar { get; set; }
        }

        public sealed class V5 : V5Plus { }

        public sealed class V6 : V5Plus
        {
            public bool Menus { get; set; }
        }
    }
}