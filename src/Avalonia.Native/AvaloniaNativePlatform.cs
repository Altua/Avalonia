﻿using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Avalonia.Compatibility;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Platform;
using Avalonia.Input;
using Avalonia.Input.Platform;
using Avalonia.Native.Interop;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Rendering.Composition;
using Avalonia.Threading;
using MicroCom.Runtime;
#nullable enable

namespace Avalonia.Native
{
    class AvaloniaNativePlatform : IWindowingPlatform, IOverlayPlatform
    {
        private readonly IAvaloniaNativeFactory _factory;
        private AvaloniaNativePlatformOptions? _options;
        private IPlatformGraphics? _platformGraphics;

        [DllImport("libAvaloniaNative")]
        static extern IntPtr CreateAvaloniaNative();

        internal static readonly KeyboardDevice KeyboardDevice = new KeyboardDevice();
        internal static Compositor Compositor { get; private set; } = null!;

        public static AvaloniaNativePlatform Initialize(IntPtr factory, AvaloniaNativePlatformOptions options)
        {
            var factoryProxy = MicroComRuntime.CreateProxyFor<IAvaloniaNativeFactory>(factory, true);

            AvaloniaLocator.CurrentMutable.Bind<IAvaloniaNativeFactory>().ToConstant(factoryProxy);
            
            var result = new AvaloniaNativePlatform(factoryProxy);
            
            result.DoInitialize(options);

            return result;
        }

        delegate IntPtr CreateAvaloniaNativeDelegate();

        public static AvaloniaNativePlatform Initialize(AvaloniaNativePlatformOptions options)
        {
            if (options.AvaloniaNativeLibraryPath != null)
            {
                var lib = NativeLibraryEx.Load(options.AvaloniaNativeLibraryPath);
                if (!NativeLibraryEx.TryGetExport(lib, "CreateAvaloniaNative", out var proc))
                {
                    throw new InvalidOperationException(
                        "Unable to get \"CreateAvaloniaNative\" export from AvaloniaNativeLibrary library");
                }
                var d = Marshal.GetDelegateForFunctionPointer<CreateAvaloniaNativeDelegate>(proc);

                return Initialize(d(), options);
            }
            else
                return Initialize(CreateAvaloniaNative(), options);
        }

        public void SetupApplicationMenuExporter()
        {
            var exporter = new AvaloniaNativeMenuExporter(_factory);
        }

        public void SetupApplicationName()
        {
            if (!string.IsNullOrWhiteSpace(Application.Current!.Name))
            {
                _factory.MacOptions.SetApplicationTitle(Application.Current.Name);
            }
        }

        private AvaloniaNativePlatform(IAvaloniaNativeFactory factory)
        {
            _factory = factory;
        }

        class GCHandleDeallocator : NativeCallbackBase, IAvnGCHandleDeallocatorCallback
        {
            public void FreeGCHandle(IntPtr handle)
            {
                GCHandle.FromIntPtr(handle).Free();
            }
        }

        void DoInitialize(AvaloniaNativePlatformOptions options)
        {
            _options = options;

            var applicationPlatform = new AvaloniaNativeApplicationPlatform();

            var macOpts = AvaloniaLocator.Current.GetService<MacOSPlatformOptions>() ?? new MacOSPlatformOptions();
            
            if (_factory.MacOptions != null)
                _factory.MacOptions.SetDisableAppDelegate(macOpts.DisableAvaloniaAppDelegate ? 1 : 0);

            _factory.Initialize(new GCHandleDeallocator(), applicationPlatform, new AvnDispatcher());
            
            if (_factory.MacOptions != null)
            {
                _factory.MacOptions.SetShowInDock(macOpts.ShowInDock ? 1 : 0);
                _factory.MacOptions.SetDisableSetProcessName(macOpts.DisableSetProcessName ? 1 : 0);
            }

            AvaloniaLocator.CurrentMutable
                .Bind<IDispatcherImpl>().ToConstant(new DispatcherImpl(_factory.CreatePlatformThreadingInterface()))
                .Bind<ICursorFactory>().ToConstant(new CursorFactory(_factory.CreateCursorFactory()))
                .Bind<IScreenImpl>().ToConstant(new ScreenImpl(_factory.CreateScreens))
                .Bind<IPlatformIconLoader>().ToSingleton<IconLoader>()
                .Bind<IKeyboardDevice>().ToConstant(KeyboardDevice)
                .Bind<IPlatformSettings>().ToConstant(new NativePlatformSettings(_factory.CreatePlatformSettings()))
                .Bind<IWindowingPlatform>().ToConstant(this)
                .Bind<IOverlayPlatform>().ToConstant(this)
                .Bind<IClipboard>().ToConstant(new ClipboardImpl(_factory.CreateClipboard()))
                .Bind<IRenderTimer>().ToConstant(new ThreadProxyRenderTimer(new AvaloniaNativeRenderTimer(_factory.CreatePlatformRenderTimer())))
                .Bind<IMountedVolumeInfoProvider>().ToConstant(new MacOSMountedVolumeInfoProvider())
                .Bind<IPlatformDragSource>().ToConstant(new AvaloniaNativeDragSource(_factory))
                .Bind<IPlatformLifetimeEventsImpl>().ToConstant(applicationPlatform)
                .Bind<INativeApplicationCommands>().ToConstant(new MacOSNativeMenuCommands(_factory.CreateApplicationCommands()))
                .Bind<IActivatableLifetime>().ToSingleton<MacOSActivatableLifetime>()
                .Bind<IStorageProviderFactory>().ToConstant(new StorageProviderApi(_factory.CreateStorageProvider(), options.AppSandboxEnabled))
                .Bind<IStorageProviderFactory2>().ToConstant(new StorageProviderApi(_factory.CreateStorageProvider(), options.AppSandboxEnabled));

            var hotkeys = new PlatformHotkeyConfiguration(KeyModifiers.Meta, wholeWordTextActionModifiers: KeyModifiers.Alt);
            hotkeys.MoveCursorToTheStartOfLine.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers));
            hotkeys.MoveCursorToTheStartOfLineWithSelection.Add(new KeyGesture(Key.Left, hotkeys.CommandModifiers | hotkeys.SelectionModifiers));
            hotkeys.MoveCursorToTheEndOfLine.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers));
            hotkeys.MoveCursorToTheEndOfLineWithSelection.Add(new KeyGesture(Key.Right, hotkeys.CommandModifiers | hotkeys.SelectionModifiers));

            AvaloniaLocator.CurrentMutable.Bind<PlatformHotkeyConfiguration>().ToConstant(hotkeys);

            AvaloniaLocator.CurrentMutable.Bind<KeyGestureFormatInfo>().ToConstant(new KeyGestureFormatInfo(new Dictionary<Key, string>()
                    {
                        { Key.Back , "⌫" }, { Key.Down , "↓" }, { Key.End , "↘" }, { Key.Escape , "⎋" },
                        { Key.Home , "↖" }, { Key.Left , "←" }, { Key.Return , "↩" }, { Key.PageDown , "⇟" },
                        { Key.PageUp , "⇞" }, { Key.Right , "→" }, { Key.Space , "␣" }, { Key.Tab , "⇥" },
                        { Key.Up , "↑" }
                    }, ctrl: "⌃", meta: "⌘", shift: "⇧", alt: "⌥"));

            foreach (var mode in _options.RenderingMode)
            {
                if (mode == AvaloniaNativeRenderingMode.OpenGl)
                {
                    try
                    {
                        _platformGraphics = new AvaloniaNativeGlPlatformGraphics(_factory.ObtainGlDisplay());
                        break;
                    }
                    catch (Exception)
                    {
                        // ignored
                    }
                }
                else if (mode == AvaloniaNativeRenderingMode.Metal)
                {
                    try
                    {
                        var metal = new MetalPlatformGraphics(_factory);
                        metal.CreateContext().Dispose();
                        _platformGraphics = metal;
                        break;
                    }
                    catch
                    {
                        // Ignored
                    }
                }
                else if (mode == AvaloniaNativeRenderingMode.Software)
                    break;
            }

            if (_platformGraphics != null)
                AvaloniaLocator.CurrentMutable
                    .Bind<IPlatformGraphics>().ToConstant(_platformGraphics);
            

            Compositor = new Compositor(_platformGraphics, true);
            AvaloniaLocator.CurrentMutable.Bind<Compositor>().ToConstant(Compositor);

            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        private void OnProcessExit(object? sender, EventArgs e)
        {
            AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
            _factory.Dispose();
        }

        public ITrayIconImpl CreateTrayIcon()
        {
            return new TrayIconImpl(_factory);
        }

        public IWindowImpl CreateWindow()
        {
            return new WindowImpl(_factory, _options);
        }

        public IWindowImpl CreateEmbeddableWindow()
        {
            throw new NotImplementedException();
        }

        public IWindowImpl CreateOverlay(IntPtr parentWindow, string parentView)
        {
            return new WindowImpl(parentWindow, parentView, _factory, _options);
        }

        public bool AppActivate(string bundleIdentifier)
        {
            var result = _factory.AppActivate(bundleIdentifier).FromComBool();
            return result;
        }

        public void HideWindow(IntPtr nsWindow)
        {
            _factory.HideWindow(nsWindow);
        }
        
        public ITopLevelImpl CreateEmbeddableTopLevel()
        {
            return new EmbeddableTopLevelImpl(_factory);
        }

        public void ShowFolder(string filePath)
        {
            _factory.ShowFolder(filePath);
        }
    }
}
