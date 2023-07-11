using Avalonia.Media;
using Avalonia.Metadata;
using MiniMvvm;
using System.Reactive.Linq;

namespace FontExplorer.ViewModels
{
    public class FontFamilyInspectorViewModel : ViewModelBase
    {
        private FontFamily? _fontFamily = null;

        public FontFamilyInspectorViewModel()
        {
            this.WhenAnyValue(x => x.FontFamily)
                .Subscribe(_ => {
                    RaisePropertyChanged(nameof(Name));
                    RaisePropertyChanged(nameof(Typefaces));
                });
        }

        public FontFamily? FontFamily
        {
            get { return _fontFamily; }
            set { RaiseAndSetIfChanged(ref _fontFamily, value); }
        }

        [DependsOn(nameof(FontFamily))]
        public string? Name => _fontFamily?.Name;

        public IEnumerable<TypefaceViewModel> Typefaces => GetTypefaces();

        private IEnumerable<TypefaceViewModel> GetTypefaces() 
        {
            if(_fontFamily is null)
                yield break;

            var fontManager = FontManager.Current;

            foreach (var fontStyle in Enum.GetValues<FontStyle>())
            {
                foreach (var fontWeight in Enum.GetValues<FontWeight>())
                {
                    foreach (var fontStretch in Enum.GetValues<FontStretch>())
                    {
                        var typeface = new Typeface(_fontFamily, fontStyle, fontWeight, fontStretch);

                        if (fontManager.TryGetGlyphTypeface(typeface, out var glyphTypeface))
                        {
                            yield return new TypefaceViewModel(typeface, glyphTypeface);
                        }
                    }
                }
            }
        }
        
    }
}
