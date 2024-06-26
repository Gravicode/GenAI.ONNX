﻿using Genny.Utils;
using Genny.ViewModel;
using Microsoft.ML.OnnxRuntimeGenAI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using MsBox.Avalonia;
using ReactiveUI;
using System.Reactive;
using Splat.ModeDetection;
using System.IO;

//using System.Windows;
//using System.Windows.Controls;

namespace Genny.Views
{
    /// <summary>
    /// Interaction logic for StatefulView.xaml
    /// </summary>
    public partial class StatefulView : UserControl, INotifyPropertyChanged
    {
        private string _prompt;
        private readonly List<int> _pastTokens;
        private CancellationTokenSource _cancellationTokenSource;

        public StatefulView()
        {
            _pastTokens = new List<int>();
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
          AvaloniaProperty.Register<StatefulView,Model>(nameof(Model));

        public static readonly AvaloniaProperty<Tokenizer> TokenizerProperty =
            AvaloniaProperty.Register<StatefulView, Tokenizer>(nameof(Tokenizer));

        public static readonly AvaloniaProperty<ModelOptionsModel> ModelOptionsProperty =
            AvaloniaProperty.Register<StatefulView,ModelOptionsModel>(nameof(ModelOptions), new ModelOptionsModel());

        public static readonly AvaloniaProperty<SearchOptionsModel> SearchOptionsProperty =
            AvaloniaProperty.Register<StatefulView,SearchOptionsModel>(nameof(SearchOptions),new SearchOptionsModel());

        public ReactiveCommand<Unit, Unit>  ClearCommand { get; }
        public ReactiveCommand<Unit, Unit>  CancelCommand { get; }
        public ReactiveCommand<Unit, Unit>  GenerateCommand { get; }
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
            _pastTokens.Clear();
            ResultHistory.Clear();
            //return Task.CompletedTask;
        }


        private async IAsyncEnumerable<TokenModel> RunInferenceAsync(string prompt, [EnumeratorCancellation] CancellationToken cancellationToken)
        {
            var sequences = await Tokenizer.EncodeAsync(prompt, cancellationToken);

            // Add Tokens to history
            AddPastTokens(sequences);

            using var generatorParams = new GeneratorParams(Model);
            generatorParams.ApplySearchOptions(SearchOptions);

            // max_length is per message, so increment max_length for next call
            var newMaxLength = Math.Min(_pastTokens.Count + SearchOptions.MaxLength, ModelOptions.ContextLength);
            generatorParams.SetSearchOption("max_length", newMaxLength); 

            generatorParams.SetInputIDs(CollectionsMarshal.AsSpan(_pastTokens), (ulong)_pastTokens.Count, 1);

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


        private void AddPastTokens(Sequences sequences)
        {
            _pastTokens.AddRange(sequences[0].ToArray());

            // Only keep (context_length - max_length) worth of history
            while (_pastTokens.Count > ModelOptions.ContextLength - SearchOptions.MaxLength)
            {
                _pastTokens.RemoveAt(0);
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
