using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Media;
using FontExplorer.Views;
using MiniMvvm;

namespace FontExplorer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        public IList<FontFamily> Fonts { get; } = LoadFonts();

        public FontFamilyInspectorViewModel Inspector { get; } = new FontFamilyInspectorViewModel();

        private static IList<FontFamily> LoadFonts()
        {
            return FontManager.Current.SystemFonts
                .ToList();
        }

        public FontFamily? SelectedFontFamily
        {
            get { return Inspector.FontFamily; }
            set { Inspector.FontFamily = value; }
        }
    }
}
