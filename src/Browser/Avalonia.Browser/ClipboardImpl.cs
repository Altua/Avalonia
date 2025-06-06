using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Browser
{
    internal class ClipboardImpl : IClipboard
    {
        public Task<string?> GetTextAsync()
        {
            return InputHelper.ReadClipboardTextAsync(BrowserWindowingPlatform.GlobalThis)!;
        }

        public Task SetTextAsync(string? text)
        {
            return InputHelper.WriteClipboardTextAsync(BrowserWindowingPlatform.GlobalThis, text ?? string.Empty);
        }

        public async Task ClearAsync() => await SetTextAsync("");

        public async Task SetDataObjectAsync(IDataObject data)
        {
            var dict = new Dictionary<string, string>();

            foreach (var format in data.GetDataFormats())
            {
                if (data.Get(format) is string str)
                {
                    if (format == DataFormats.Text)
                    {
                        dict["text/plain"] = str;
                    }

                    dict[$"web {format}"] = str;
                }
            }

            if (dict.Count > 0)
            {
                await InputHelper.WriteClipboardAsync(BrowserWindowingPlatform.GlobalThis, dict);
            }
        }

        public async Task<string[]> GetFormatsAsync()
        {
            var data = await InputHelper.ReadClipboardAsync(BrowserWindowingPlatform.GlobalThis);
            var res = new List<string>();

            foreach (var key in data.Keys)
            {
                if (key == "text/plain")
                {
                    res.Add(DataFormats.Text);
                }
                else if (key.StartsWith("web ", StringComparison.Ordinal))
                {
                    res.Add(key.Substring(4));
                }
            }

            return res.ToArray();
        }

        public async Task<object?> GetDataAsync(string format)
        {
            var data = await InputHelper.ReadClipboardAsync(BrowserWindowingPlatform.GlobalThis);

            if (format == DataFormats.Text && data.TryGetValue("text/plain", out var text))
            {
                return text;
            }

            if (data.TryGetValue($"web {format}", out var value))
            {
                return value;
            }

            return null;
        }
    }
}
