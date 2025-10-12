/* Copyright 2010-2025 Tara McGrew
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

#nullable enable

using System;
using System.Threading.Tasks;

namespace Zilf.Playground.Services
{
    /// <summary>
    /// Singleton service that manages the persistent story player iframe state.
    /// </summary>
    public sealed class StoryPlayerService
    {
        private bool gameLoaded = false;
        private bool storyVisible = false;

        public bool IsGameLoaded => gameLoaded;
        public bool IsStoryVisible => storyVisible;

        public event EventHandler? StateChanged;

        public void SetGameLoaded(bool loaded)
        {
            if (gameLoaded != loaded)
            {
                gameLoaded = loaded;
                if (!loaded)
                {
                    storyVisible = false;
                }
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void SetStoryVisible(bool visible)
        {
            if (storyVisible != visible)
            {
                storyVisible = visible;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }

        public void ToggleStoryVisible()
        {
            SetStoryVisible(!storyVisible);
        }

        public void Reset()
        {
            if (gameLoaded || storyVisible)
            {
                gameLoaded = false;
                storyVisible = false;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
        }
    }
}
