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
            var list = new List<string>();

            foreach (var format in data.GetDataFormats())
            {
                if (data.Get(format) is string str)
                {
                    if (format == DataFormats.Text)
                    {
                        list.Add("text/plain");
                        list.Add(str);
                    }

                    list.Add($"web {format}");
                    list.Add(str);
                }
            }

            if (list.Count > 0)
            {
                await InputHelper.WriteClipboardAsync(BrowserWindowingPlatform.GlobalThis, list.ToArray());
            }
        }

        public async Task<string[]> GetFormatsAsync()
        {
            var pairs = await InputHelper.ReadClipboardAsync(BrowserWindowingPlatform.GlobalThis);
            var res = new List<string>();

            for (var i = 0; i + 1 < pairs.Length; i += 2)
            {
                var key = pairs[i];

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
            var pairs = await InputHelper.ReadClipboardAsync(BrowserWindowingPlatform.GlobalThis);

            for (var i = 0; i + 1 < pairs.Length; i += 2)
            {
                var key = pairs[i];
                var value = pairs[i + 1];

                if (format == DataFormats.Text && key == "text/plain")
                {
                    return value;
                }

                if (key == $"web {format}")
                {
                    return value;
                }
            }

            return null;
        }
    }
}
