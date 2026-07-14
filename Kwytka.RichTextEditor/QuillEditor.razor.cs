using Ganss.Xss;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Kwytka.RichTextEditor;

public partial class QuillEditor
{
    [Parameter] public string Value { get; set; } = string.Empty;

    [Parameter] public string MinHeight { get; set; } = "10rem";

    [Parameter] public EventCallback<string> ValueChanged { get; set; }

    [Parameter] public bool Sanitize { get; set; } = true;

    private readonly HtmlSanitizer _sanitizer = new();
    private const string ModulePath = "./_content/Kwytka.RichTextEditor/quill-editor.js";

    private readonly string _editorId = Guid.NewGuid().ToString("N");
    private ElementReference _editorElement;
    private string _value = string.Empty;
    private DotNetObjectReference<QuillEditor>? _dotNetReference;
    private IJSObjectReference? _module;
    private bool _isInitialized;
    private bool _hasPendingUpdate;

    protected override async Task OnParametersSetAsync()
    {
        var nextValue = SanitizeValue(Value);
        if (_value == nextValue)
        {
            return;
        }

        _value = nextValue;

        if (!_isInitialized || _module is null)
        {
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("setContent", _editorId, _value);
        }
        catch (JSDisconnectedException)
        {
            _hasPendingUpdate = true;
        }
    }

    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!firstRender)
        {
            return;
        }

        _module = await JsRuntime.InvokeAsync<IJSObjectReference>("import", ModulePath);
        _dotNetReference = DotNetObjectReference.Create(this);
        await _module.InvokeVoidAsync("initialize",
            _editorId,
            _editorElement,
            _value,
            _dotNetReference);

        _isInitialized = true;

        if (_hasPendingUpdate)
        {
            _hasPendingUpdate = false;
            await _module.InvokeVoidAsync("setContent", _editorId, _value);
        }
    }

    [JSInvokable]
    public async Task NotifyValueChanged(string value)
    {
        var sanitizedValue = SanitizeValue(value);

        if (sanitizedValue == _value)
        {
            return;
        }

        _value = sanitizedValue;
        await ValueChanged.InvokeAsync(_value);
    }

    private string SanitizeValue(string value)
    {
        if (!Sanitize)
        {
            return value;
        }

        return _sanitizer.Sanitize(value);
    }

    public async ValueTask DisposeAsync()
    {
        if (_module is null)
        {
            _dotNetReference?.Dispose();
            return;
        }

        try
        {
            await _module.InvokeVoidAsync("dispose", _editorId);
        }
        catch (JSDisconnectedException)
        {
            // The circuit is gone; no cleanup call is possible.
        }
        finally
        {
            try
            {
                await _module.DisposeAsync();
            }
            catch (JSDisconnectedException)
            {
                // The circuit is gone; disposal can be skipped.
            }

            _dotNetReference?.Dispose();
            _module = null;
            _dotNetReference = null;
        }
    }
}
