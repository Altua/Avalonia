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

        private async Task<byte[]> GetBytesAsync(string type)
        {
            string base64String = await InputHelper.ReadClipboardBytesAsync(BrowserWindowingPlatform.GlobalThis, type);
            return System.Convert.FromBase64String(base64String);
        }

        private Task SetBytesAsync(string type, byte[] data)
        {
            string base64String = System.Convert.ToBase64String(data);
            return InputHelper.WriteClipboardBytesAsync(BrowserWindowingPlatform.GlobalThis, base64String, type);
        }

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
            foreach (var format in data.GetDataFormats())
            {
                var o = data.Get(format);
                switch (o)
                {
                    case string s when format == DataFormats.Text:
                        await SetTextAsync(s);
                        break;

                    case byte[] bytes:
                        await SetBytesAsync($"application/{format}", bytes);
                        break;

                    default:
                        break;
                }
            }
        }

        public async Task<string[]> GetFormatsAsync()
        {
            List<string> formatList = [];

            var formatsString = await InputHelper.ReadClipboardFormatsAsync(BrowserWindowingPlatform.GlobalThis);
            var formats = formatsString.Split([',']);
            if (formats is not null)
            {
                foreach (var format in formats)
                {
                    if (format == "text/plain")
                    {
                        formatList.Add(DataFormats.Text);
                    }
                    else if (format.StartsWith("application/"))
                    {
                        formatList.Add(format[12..]);
                    }
                }
            }
            
            return [.. formatList];
        }

        public async Task<object?> GetDataAsync(string format)
        {
            if (format == DataFormats.Text)
                return await GetTextAsync();

            return await GetBytesAsync(format);
        }
    }
}
