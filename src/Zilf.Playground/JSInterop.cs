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
    public sealed class JSInterop
    {
        private readonly IJSRuntime js;

        public JSInterop(IJSRuntime js)
        {
            this.js = js;
        }

        public async Task DownloadBytesAsFileAsync(byte[] bytes, string filename, string contentType)
        {
            await js.InvokeVoidAsync("ZilfJsInterop.BlazorDownloadFileFast", filename, contentType, bytes);
        }

        public async Task<string> LoadGameInParchmentAsync(byte[] gameData)
        {
            return await js.InvokeAsync<string>("ZilfJsInterop.loadGameInParchment", gameData);
        }

        public ValueTask ScrollToBottomAsync(ElementReference element)
        {
            return js.InvokeVoidAsync("ZilfJsInterop.scrollToBottom", element);
        }

        public ValueTask<string> PromptAsync(string prompt, string value)
        {
            return js.InvokeAsync<string>("prompt", prompt, value);
        }

        public ValueTask<bool> ConfirmAsync(string message)
        {
            return js.InvokeAsync<bool>("confirm", message);
        }

        public ValueTask AddEventListenerAsync(ElementReference element, string eventName, string jsHandler, params object[] extraHandlerArgs)
        {
            return js.InvokeVoidAsync("ZilfJsInterop.addEventListener", element, eventName, jsHandler, extraHandlerArgs);
        }

        public async Task<string?> GetLocalStorageAsync(string key)
        {
            return await js.InvokeAsync<string?>("localStorage.getItem", key);
        }

        public async Task SetLocalStorageAsync(string key, string value)
        {
            await js.InvokeVoidAsync("localStorage.setItem", key, value);
        }

        public async Task RemoveLocalStorageAsync(string key)
        {
            await js.InvokeVoidAsync("localStorage.removeItem", key);
        }
    }
}
