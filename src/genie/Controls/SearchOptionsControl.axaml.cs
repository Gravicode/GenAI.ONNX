using Genny.ViewModel;
//using System.Windows;
//using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace Genny.Controls
{
    /// <summary>
    /// Interaction logic for SearchOptionsControl.xaml
    /// </summary>
    public partial class SearchOptionsControl : UserControl
    {
        public SearchOptionsControl()
        {
            InitializeComponent();
        }

        public static readonly AvaloniaProperty<SearchOptionsModel> SearchOptionsProperty =
           AvaloniaProperty.Register<SearchOptionsControl,SearchOptionsModel>(nameof(SearchOptions), new SearchOptionsModel());


        /// <summary>
        /// Gets or sets the search options.
        /// </summary>
        public SearchOptionsModel SearchOptions
        {
            get { return (SearchOptionsModel)GetValue(SearchOptionsProperty); }
            set { SetValue(SearchOptionsProperty, value); }
        }
    }
}
