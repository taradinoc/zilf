var ZilfJsInterop = ZilfJsInterop || {};

// https://www.meziantou.net/generating-and-downloading-a-file-in-a-blazor-webassembly-application.htm
ZilfJsInterop.BlazorDownloadFileFast = function (name, contentType, content) {
    // Create the URL - content is already a Uint8Array passed from .NET
    const file = new File([content], name, { type: contentType });
    const exportUrl = URL.createObjectURL(file);

    // Create the <a> element and click on it
    const a = document.createElement("a");
    document.body.appendChild(a);
    a.href = exportUrl;
    a.download = name;
    a.target = "_self";
    a.click();

    // We don't need to keep the url, let's release the memory
    URL.revokeObjectURL(exportUrl);
};

ZilfJsInterop.scrollToBottom = function (control) {
    control.scrollTop = control.scrollHeight;
};

ZilfJsInterop.setFocus = function (element) {
    element.focus();
};

ZilfJsInterop.addEventListener = function (element, event, handler, handlerArgs) {
    //console.log("ZilfJsInterop.addEventListener", element, event, handler, handlerArgs);
    var actualHandler = eval(handler);
    element.addEventListener(event, (e) => actualHandler(e, ...handlerArgs));
};

ZilfJsInterop.handleReplInputKey = function (e, dotnetRef, dotnetMethod) {
    //console.log("ZilfJsInterop.handleReplInputKey", e, dotnetRef, dotnetMethod);
    if (e.key == "Enter" && !e.shiftKey) {
        //console.log("preventing default!");
        e.preventDefault();
        dotnetRef.invokeMethodAsync(dotnetMethod);
    }
}

ZilfJsInterop.showModalAndNotifyWhenHidden = function (element, dotnetRef, dotnetMethod) {
    // Bootstrap 5: use native Modal API, no jQuery
    var modal = bootstrap.Modal.getOrCreateInstance(element, { backdrop: true, keyboard: true, focus: true });
    var onShown = function () {
        // Focus first enabled input inside body or footer
        var target = element.querySelector('.modal-body, .modal-footer');
        if (target) {
            var input = target.querySelector('input, select, textarea, button, [tabindex]:not([tabindex="-1"])');
            if (input) input.focus();
        }
        element.removeEventListener('shown.bs.modal', onShown);
    };
    var onHidden = function () {
        element.removeEventListener('hidden.bs.modal', onHidden);
        dotnetRef.invokeMethodAsync(dotnetMethod);
    };
    element.addEventListener('shown.bs.modal', onShown);
    element.addEventListener('hidden.bs.modal', onHidden);
    modal.show();
}

ZilfJsInterop.hideModal = function (element) {
    var modal = bootstrap.Modal.getInstance(element) || bootstrap.Modal.getOrCreateInstance(element);
    modal.hide();
}

ZilfJsInterop.loadGameInParchment = function (gameData) {
    // Get the iframe
    const iframe = document.getElementById('parchment-frame');
    if (!iframe || !iframe.contentWindow) {
        console.error('Parchment iframe not found or not loaded');
        return null;
    }
    
    // Wait for Parchment to be initialized
    const tryLoad = () => {
        const parchmentWindow = iframe.contentWindow;
        if (parchmentWindow.parchment) {
            // Create a File object from the game data with a .z5 extension
            // (most ZILF games compile to Z-machine version 5)
            const file = new File([gameData], 'game.z5', { 
                type: 'application/x-zmachine' 
            });
            
            // Use Parchment's load_uploaded_file method which properly handles the file
            parchmentWindow.parchment.load_uploaded_file(file);
        } else {
            console.error('Parchment not initialized in iframe');
        }
    };
    
    // Check if iframe is loaded
    if (iframe.contentDocument && iframe.contentDocument.readyState === 'complete') {
        tryLoad();
    } else {
        iframe.onload = tryLoad;
    }
    
    return 'loading';
}
