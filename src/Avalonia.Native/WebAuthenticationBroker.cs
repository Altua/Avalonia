using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Compatibility;
using Avalonia.Native;
using Avalonia.Threading;

namespace Avalonia.Native
{
    // Only supports macOS
    public static class WebAuthenticationBroker
    {
        public static async Task<WebAuthenticationResult> AuthenticateAsync(WebAuthenticatorOptions options)
        {
            if (options is null)
            {
                throw new ArgumentNullException(nameof(options));
            }

            if (!OperatingSystemEx.IsMacOS())
            {
                throw new PlatformNotSupportedException("Web authentication broker is only supported on macOS.");
            }

            var callbackUri = await WebAuthenticationBrokerApi.AuthenticateAsync(options.StartUrl, options.CallbackUrl, CancellationToken.None);
            return new WebAuthenticationResult(callbackUri);
        }
    }

    public sealed class WebAuthenticatorOptions
    {
        public WebAuthenticatorOptions(Uri startUrl, Uri callbackUrl)
        {
            StartUrl = startUrl ?? throw new ArgumentNullException(nameof(startUrl));
            CallbackUrl = callbackUrl ?? throw new ArgumentNullException(nameof(callbackUrl));
        }

        public Uri StartUrl { get; }

        public Uri CallbackUrl { get; }
    }

    public sealed class WebAuthenticationResult
    {
        public WebAuthenticationResult(Uri callbackUri)
        {
            CallbackUri = callbackUri ?? throw new ArgumentNullException(nameof(callbackUri));
        }

        public Uri CallbackUri { get; }
    }
}
