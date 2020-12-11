var ZilfJsInterop = ZilfJsInterop || {};

// https://www.meziantou.net/generating-and-downloading-a-file-in-a-blazor-webassembly-application.htm
ZilfJsInterop.BlazorDownloadFileFast = function (name, contentType, content) {
    // Convert the parameters to actual JS types
    const nameStr = BINDING.conv_string(name);
    const contentTypeStr = BINDING.conv_string(contentType);
    const contentArray = Blazor.platform.toUint8Array(content);

    // Create the URL
    const file = new File([contentArray], nameStr, { type: contentTypeStr });
    const exportUrl = URL.createObjectURL(file);

    // Create the <a> element and click on it
    const a = document.createElement("a");
    document.body.appendChild(a);
    a.href = exportUrl;
    a.download = nameStr;
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
    $(element)
        .one("shown.bs.modal", function () { $(".modal-body, .modal-footer", element).find(":input:enabled:visible").first().focus(); })
        .one("hidden.bs.modal", function () { dotnetRef.invokeMethodAsync(dotnetMethod); })
        .modal("show");
}

ZilfJsInterop.hideModal = function (element) {
    $(element).modal("hide");
}
