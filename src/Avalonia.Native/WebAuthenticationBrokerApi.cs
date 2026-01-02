using System;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Native.Interop;

namespace Avalonia.Native;

public static class WebAuthenticationBrokerApi
{
    public static async Task<Uri> AuthenticateAsync(Uri startUrl, Uri redirectUrl, CancellationToken cancellationToken)
    {
        if (startUrl is null)
        {
            throw new ArgumentNullException(nameof(startUrl));
        }

        if (redirectUrl is null)
        {
            throw new ArgumentNullException(nameof(redirectUrl));
        }

        var factory = AvaloniaLocator.Current.GetService<IAvaloniaNativeFactory>() ?? throw new PlatformNotSupportedException("AvaloniaNative factory is unavailable.");

        var broker = factory.CreateWebAuthenticationBroker();
        using var events = new SystemDialogEvents();
        using var registration = cancellationToken.Register(() => events.OnCompleted(null));

        broker.Authenticate(startUrl.ToString(), redirectUrl.ToString(), events);

        var results = await events.Task.ConfigureAwait(false);

        broker.Dispose();

        if (results.Length == 0)
        {
            throw new OperationCanceledException("Authentication was canceled.", cancellationToken);
        }

        if (!Uri.TryCreate(results[0], UriKind.Absolute, out var callbackUri))
        {
            throw new InvalidOperationException("Authentication callback URI was invalid.");
        }


        return callbackUri;
    }

    internal class SystemDialogEvents : NativeCallbackBase, IAvnSystemDialogEvents
    {
        private readonly TaskCompletionSource<string[]> _tcs = new();

        public Task<string[]> Task => _tcs.Task;

        public void OnCompleted(IAvnStringArray? ppv)
        {
            using (ppv)
            {
                _tcs.TrySetResult(ppv?.ToStringArray() ?? []);
            }
        }
    }
}
