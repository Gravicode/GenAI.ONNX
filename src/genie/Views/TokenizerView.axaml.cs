using Genny.Utils;
using Microsoft.ML.OnnxRuntimeGenAI;
using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
//using System.Windows;
//using System.Windows.Controls;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using ReactiveUI;
using System.Reactive;

namespace Genny.Views
{
    /// <summary>
    /// Interaction logic for TokenizerView.xaml
    /// </summary>
    public partial class TokenizerView : UserControl, INotifyPropertyChanged
    {
        private string _encodeResult;
        private string _decodeResult;

        public TokenizerView()
        {
            EncodeCommand = ReactiveCommand.Create<string>(EncodeAsync); //new RelayCommand<string>(EncodeAsync);
            DecodeCommand = ReactiveCommand.Create<string>(DecodeAsync);//new RelayCommand<string>(DecodeAsync);
            InitializeComponent();
        }

        public static readonly AvaloniaProperty<Tokenizer> TokenizerProperty =
           AvaloniaProperty.Register<TokenizerView,Tokenizer>(nameof(Tokenizer));

        public ReactiveCommand<string, Unit> EncodeCommand { get; }
        public ReactiveCommand<string, Unit> DecodeCommand { get; }

        public Tokenizer Tokenizer
        {
            get { return (Tokenizer)GetValue(TokenizerProperty); }
            set { SetValue(TokenizerProperty, value); }
        }

        public string EncodeResult
        {
            get { return _encodeResult; }
            set { _encodeResult = value; NotifyPropertyChanged(); }
        }

        public string DecodeResult
        {
            get { return _decodeResult; }
            set { _decodeResult = value; NotifyPropertyChanged(); }
        }


        private async void EncodeAsync(string input)
        {
            EncodeResult = null;
            try
            {
                var sequences = await Tokenizer.EncodeAsync(input);
                EncodeResult = string.Join(", ", sequences[0].ToArray());
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Tokenizer Encode Error", ex.Message,
                 MsBox.Avalonia.Enums.ButtonEnum.Ok);
                //MessageBox.Show(ex.Message, "Tokenizer Encode Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private async void DecodeAsync(string input)
        {
            DecodeResult = null;
            try
            {
                var intArray = input
                     .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                     .Select(int.Parse)
                     .ToArray();
                DecodeResult = await Tokenizer.DecodeAsync(intArray);
            }
            catch (Exception ex)
            {
                var box = MessageBoxManager.GetMessageBoxStandard("Tokenizer Decode Error", ex.Message,
                 MsBox.Avalonia.Enums.ButtonEnum.Ok);
                //MessageBox.Show(ex.Message, "Tokenizer Decode Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        #region INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        public void NotifyPropertyChanged([CallerMemberName] string property = "")
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
        #endregion
    }
}
