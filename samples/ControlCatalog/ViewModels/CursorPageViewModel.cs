using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using MiniMvvm;

namespace ControlCatalog.ViewModels
{
    public class CursorPageViewModel : ViewModelBase
    {
        public CursorPageViewModel()
        {
            StandardCursors = Enum.GetValues(typeof(StandardCursorType))
                .Cast<StandardCursorType>()
                .Select(x => new StandardCursorModel(x))
                .ToList();

            var loader = AvaloniaLocator.Current.GetRequiredService<IAssetLoader>();
            var s = loader.Open(new Uri("avares://ControlCatalog/Assets/avalonia-32.png"));
            var bitmap = new Bitmap(s);
            CustomCursor = new Cursor(bitmap, new PixelPoint(16, 16));


            var cursorFactory = AvaloniaLocator.Current.GetRequiredService<ICursorFactory>();

            if (cursorFactory is Avalonia.Win32.CursorFactory winCursorFactory)
            {
                CurFileCursor = new Cursor(
                    winCursorFactory.CreateCursorFromCurFile(new Uri("avares://ControlCatalog/Assets/Duplication.cur"), true),
                    "EyeDropper");
            }
        }

        public IEnumerable<StandardCursorModel> StandardCursors { get; }
        
        public Cursor CustomCursor { get; }

        public Cursor CurFileCursor { get; }
    }
    
    public class StandardCursorModel
    {
        public StandardCursorModel(StandardCursorType type)
        {
            Type = type;
            Cursor = new Cursor(type);
        }

        public StandardCursorType Type { get; }
            
        public Cursor Cursor { get; }
    }
}
