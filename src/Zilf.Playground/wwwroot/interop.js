// https://www.meziantou.net/generating-and-downloading-a-file-in-a-blazor-webassembly-application.htm
function BlazorDownloadFileFast(name, contentType, content) {
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
}