using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Avalonia.Controls;
//using Genny.Utils;
using Genny.ViewModel;
using Microsoft.ML.OnnxRuntimeGenAI;
using System.IO;
using System.Text.Json;
//using System.Windows;
using ReactiveUI;
using System.Reactive;
using MsBox.Avalonia;
using Genny.Utils;

namespace Genie;

/// <summary>
/// Interaction logic for MainWindow.xaml
/// </summary>
public partial class MainWindow : Window, INotifyPropertyChanged
{
    private Model _model;
    private Tokenizer _tokenizer;
    private ConfigurationModel _configuration;
    private string _modelPath = "D:\\Repositories\\phi2_onnx";
    private bool _isModelLoaded;

    public MainWindow()
    {
        OpenModelCommand = ReactiveCommand.Create(OpenModelAsync);//new RelayCommand(OpenModelAsync);
        LoadModelCommand = ReactiveCommand.Create(LoadModelAsync);//new RelayCommand(LoadModelAsync);//new RelayCommand(LoadModelAsync, CanExecuteLoadModel);
        InitializeComponent();
        //DoTheThing = ReactiveCommand.Create(RunTheThing);
        }
        /*
        public ReactiveCommand<Unit,Unit> DoTheThing { get; }

        void RunTheThing()
        {
            // Code for executing the command here.
        }
        */

    public ReactiveCommand<Unit, Unit> OpenModelCommand { get; }
    public ReactiveCommand<Unit, Unit> LoadModelCommand { get; }

    public Model Model
    {
        get { return _model; }
        set { _model = value; NotifyPropertyChanged(); }
    }

    public Tokenizer Tokenizer
    {
        get { return _tokenizer; }
        set { _tokenizer = value; NotifyPropertyChanged(); }
    }

    public ConfigurationModel Configuration
    {
        get { return _configuration; }
        set { _configuration = value; NotifyPropertyChanged(); }
    }


    public bool IsModelLoaded
    {
        get { return _isModelLoaded; }
        set { _isModelLoaded = value; NotifyPropertyChanged(); }
    }

    public string ModelPath
    {
        get { return _modelPath; }
        set { _modelPath = value; NotifyPropertyChanged(); }
    }


    private async void OpenModelAsync()
    {
         var dialog = new OpenFolderDialog();
         dialog.Title = "Model Folder Path";
    var result = await dialog.ShowAsync(this);

    if (result != null)
    {
        ModelPath = result;
    }
        /*
        var folderBrowserDialog = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "Model Folder Path",
            UseDescriptionForTitle = true,
        };
        var dialogResult = folderBrowserDialog.ShowDialog();
        if (dialogResult == System.Windows.Forms.DialogResult.OK)
            ModelPath = folderBrowserDialog.SelectedPath;
        */
        
    }


    private async void LoadModelAsync()
    {
        await UnloadModelAsync();
        try
        {
            Configuration = await LoadConfigAsync(ModelPath);
            await Task.Run(() =>
            {
                Model = new Model(ModelPath);
                Tokenizer = new Tokenizer(_model);
            });
            IsModelLoaded = true;
        }
        catch (Exception ex)
        {
            var box = MessageBoxManager.GetMessageBoxStandard("Model Load Error", ex.Message,
                 MsBox.Avalonia.Enums.ButtonEnum.Ok);
                 var result = await box.ShowAsync();
            //MessageBox.Show(ex.Message, "Model Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }


    private bool CanExecuteLoadModel()
    {
        return !string.IsNullOrWhiteSpace(ModelPath);
    }


    private Task UnloadModelAsync()
    {
        _model?.Dispose();
        _tokenizer?.Dispose();
        IsModelLoaded = false;
        return Task.CompletedTask;
    }


    private static async Task<ConfigurationModel> LoadConfigAsync(string modelPath)
    {
        var configPath = Path.Combine(modelPath, "genai_config.json");
        var configJson = await File.ReadAllTextAsync(configPath);
        return JsonSerializer.Deserialize<ConfigurationModel>(configJson);
    }

    #region INotifyPropertyChanged
    public event PropertyChangedEventHandler PropertyChanged;
    public void NotifyPropertyChanged([CallerMemberName] string property = "")
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    }
    #endregion
}