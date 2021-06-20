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
