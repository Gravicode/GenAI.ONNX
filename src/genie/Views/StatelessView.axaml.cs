using Genny.Utils;
using Genny.ViewModel;
using Microsoft.ML.OnnxRuntimeGenAI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
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
    /// Interaction logic for StatelessView.xaml
    /// </summary>
    public partial class StatelessView : UserControl, INotifyPropertyChanged
    {
        private string _prompt;
        private CancellationTokenSource _cancellationTokenSource;

        public StatelessView()
        {
            ClearCommand = ReactiveCommand.Create(ClearAsync);//new RelayCommand(ClearAsync);
            CancelCommand = ReactiveCommand.Create(CancelAsync);//new RelayCommand(CancelAsync);
            GenerateCommand = ReactiveCommand.Create(GenerateAsync);//new RelayCommand(GenerateAsync, CanExecuteGenerate);
            ResultHistory = new ObservableCollection<ResultModel>();
            InitializeComponent();
            ScrollViewer1.ScrollChanged+=ScrollViewer_ScrollChanged;
        }
        void ScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentDelta.Y != 0)
            {
                var scrollViewer = sender as ScrollViewer;
                scrollViewer?.ScrollToEnd();
            }
        }

        public static readonly AvaloniaProperty<Model> ModelProperty =
          AvaloniaProperty.Register<StatelessView,Model>(nameof(Model));

        public static readonly AvaloniaProperty<Tokenizer> TokenizerProperty =
            AvaloniaProperty.Register<StatelessView,Tokenizer>(nameof(Tokenizer));

        public static readonly AvaloniaProperty<ModelOptionsModel> ModelOptionsProperty =
            AvaloniaProperty.Register<StatelessView,ModelOptionsModel>(nameof(ModelOptions));

        public static readonly AvaloniaProperty<SearchOptionsModel> SearchOptionsProperty =
            AvaloniaProperty.Register<StatelessView,SearchOptionsModel>(nameof(SearchOptions));

        public ReactiveCommand<Unit, Unit> ClearCommand { get; }
        public ReactiveCommand<Unit, Unit> CancelCommand { get; }
        public ReactiveCommand<Unit, Unit> GenerateCommand { get; }
        public ResultModel CurrentResult { get; set; }
        public ObservableCollection<ResultModel> ResultHistory { get; }

        public Model Model
        {
            get { return (Model)GetValue(ModelProperty); }
            set { SetValue(ModelProperty, value); }
        }

        public Tokenizer Tokenizer
        {
            get { return (Tokenizer)GetValue(TokenizerProperty); }
            set { SetValue(TokenizerProperty, value); }
        }

        public ModelOptionsModel ModelOptions
        {
            get { return (ModelOptionsModel)GetValue(ModelOptionsProperty); }
            set { SetValue(ModelOptionsProperty, value); }
        }

        public SearchOptionsModel SearchOptions
        {
            get { return (SearchOptionsModel)GetValue(SearchOptionsProperty); }
            set { SetValue(SearchOptionsProperty, value); }
        }

        public string Prompt
        {
            get { return _prompt; }
            set { _prompt = value; NotifyPropertyChanged(); }
        }


        private async void GenerateAsync()
        {
            try
            {
                var userInput = new ResultModel
                {
                    Content = Prompt,
                    IsUserInput = true
                };

                Prompt = null;
                CurrentResult = null;
                ResultHistory.Add(userInput);
                _cancellationTokenSource = new CancellationTokenSource();
                await foreach (var sentencePiece in RunInferenceAsync(userInput.Content, _cancellationTokenSource.Token))
                {
                    if (CurrentResult == null)
                    {
                        if (string.IsNullOrWhiteSpace(sentencePiece.Content)) // Ingore preceding '\n'
                            continue;

                        ResultHistory.Add(CurrentResult = new ResultModel());
                    }
                    CurrentResult.Content += sentencePiece.Content;
                }
            }
            catch (OperationCanceledException)
            {
                CurrentResult.Content += "\n\n[Operation Canceled]";
            }
            catch (Exception ex)
            {
                 var box = MessageBoxManager.GetMessageBoxStandard("Inference Error", ex.Message,
                 MsBox.Avalonia.Enums.ButtonEnum.Ok);
                 var result = await box.ShowAsync();
                //MessageBox.Show(ex.Message, "Inference Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

       
        private bool CanExecuteGenerate()
        {
            return !string.IsNullOrWhiteSpace(Prompt);
        }


        private async void CancelAsync()
        {
            _cancellationTokenSource?.Cancel();
            //return Task.CompletedTask;
        }


        private async void ClearAsync()
        {
            ResultHistory.Clear();
            //return Task.CompletedTask;
        }

        private async IAsyncEnumerable<TokenModel> RunInferenceAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var sequences = await Tokenizer.EncodeAsync(prompt, cancellationToken);

            using var generatorParams = new GeneratorParams(Model);
            generatorParams.ApplySearchOptions(SearchOptions);
            generatorParams.SetInputSequences(sequences);

            using var tokenizerStream = Tokenizer.CreateStream();
            using var generator = new Generator(Model, generatorParams);
            while (!generator.IsDone())
            {
                cancellationToken.ThrowIfCancellationRequested();

                yield return await Task.Run(() =>
                {
                    generator.ComputeLogits();
                    generator.GenerateNextToken();

                    var tokenId = generator.GetSequence(0)[^1];
                    return new TokenModel(tokenId, tokenizerStream.Decode(tokenId));
                }, cancellationToken);
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
