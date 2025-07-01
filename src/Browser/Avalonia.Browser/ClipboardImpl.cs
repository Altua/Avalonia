using System;
using System.Collections.Generic;
using System.Reflection.Metadata;
using System.Threading.Tasks;
using Avalonia.Browser.Interop;
using Avalonia.Input;
using Avalonia.Input.Platform;

namespace Avalonia.Browser
{
    internal class ClipboardImpl : IClipboard
    {

        private const string CUSTOM_MIMETYPE_PREFIX = "web application/";

        // Custom formats must be prefixed with "web " and follow MIME type (e.g; web application/GruntObject)
        // Otherwise browser throws an exception 
        private static string GetCustomMimeType(string format)
        {
            return CUSTOM_MIMETYPE_PREFIX + format.ToLower(System.Globalization.CultureInfo.CurrentCulture);
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
            List<string> list = [];
            foreach (var format in data.GetDataFormats())
            {
                var o = data.Get(format);
                switch (o)
                {
                    case string s when format == DataFormats.Text:
                        list.Add("text/plain");
                        list.Add(s);
                        break;

                    case byte[] bytes:
                        list.Add(GetCustomMimeType(format));
                        list.Add(System.Convert.ToBase64String(bytes));
                        break;

                    default:
                        break;
                }
            }

            await InputHelper.WriteClipboardAsync(BrowserWindowingPlatform.GlobalThis, [.. list]);
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
                    else if (format.StartsWith(CUSTOM_MIMETYPE_PREFIX))
                    {
                        formatList.Add(format[CUSTOM_MIMETYPE_PREFIX.Length ..]);
                    }
                }
            }
            
            return [.. formatList];
        }

        public async Task<object?> GetDataAsync(string format)
        {
            if (format == DataFormats.Text)
                return await GetTextAsync();

            var base64 = await InputHelper.ReadClipboardAsync(BrowserWindowingPlatform.GlobalThis, GetCustomMimeType(format));
            return System.Convert.FromBase64String(base64);
        }
    }
}
