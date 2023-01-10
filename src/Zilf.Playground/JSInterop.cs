/* Copyright 2010-2023 Tara McGrew
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

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Zilf.Playground
{
    internal sealed class JSInterop
    {
        private readonly IJSRuntime js;

        public JSInterop(IJSRuntime js)
        {
            this.js = js;
        }

        public void DownloadBytesAsFile(byte[] bytes, string filename, string contentType)
        {
            var jsu = (IJSUnmarshalledRuntime)js;
            jsu.InvokeUnmarshalled<string, string, byte[], bool>("ZilfJsInterop.BlazorDownloadFileFast", filename, contentType, bytes);
        }

        public ValueTask ScrollToBottomAsync(ElementReference element)
        {
            return js.InvokeVoidAsync("ZilfJsInterop.scrollToBottom", element);
        }

        public ValueTask<string> PromptAsync(string prompt, string value)
        {
            return js.InvokeAsync<string>("prompt", prompt, value);
        }

        public ValueTask AddEventListenerAsync(ElementReference element, string eventName, string jsHandler, params object[] extraHandlerArgs)
        {
            return js.InvokeVoidAsync("ZilfJsInterop.addEventListener", element, eventName, jsHandler, extraHandlerArgs);
        }
    }
}
