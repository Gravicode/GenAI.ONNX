using System;
using System.Reactive;
using System.Threading.Tasks;
using System.Windows.Input;
using ReactiveUI;


namespace Genny.Utils
{

    public class RelayCommand<T>
    {
        public ReactiveCommand<T,Unit>  MyCommand { get; }
        Func<T,Task> MyAction{set;get;}

        Func<bool> CheckCondition{set;get;}
        public RelayCommand(Func<T,Task> act)
        {
            MyCommand = ReactiveCommand.Create<T>(Execute);
            this.MyAction = act;
        }

        public RelayCommand(Func<T,Task> act, Func<bool> CheckFunction)
        {
            MyCommand = ReactiveCommand.Create<T>(Execute);
            this.MyAction = act;
            this.CheckCondition = CheckFunction;
        }

        bool CanExecute(){
            if(CheckCondition != null){
                return CheckCondition();
            }else{
                return true;
            }
        }

        async void Execute(T obj){
            if(CanExecute())
                await MyAction(obj);
        }
    }

    /// <summary>
    /// Basic Relay command implemtation
    /// </summary>
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class RelayCommand
    {
        public ReactiveCommand<Unit,Unit>  MyCommand { get; }
        Func<Task> MyAction{set;get;}

        Func<bool> CheckCondition{set;get;}
        public RelayCommand(Func<Task> act)
        {
            MyCommand = ReactiveCommand.Create(Execute);
            this.MyAction = act;
        }

        public RelayCommand(Func<Task> act, Func<bool> CheckFunction)
        {
            MyCommand = ReactiveCommand.Create(Execute);
            this.MyAction = act;
            this.CheckCondition = CheckFunction;
        }

        bool CanExecute(){
            if(CheckCondition != null){
                return CheckCondition();
            }else{
                return true;
            }
        }

        async void Execute(){
            if(CanExecute())
                await MyAction();
                //await Task.Run(MyAction);
        }
/*        private readonly Func<Task> _execute;
        private readonly Func<bool> _canExecute;
        private long _isExecuting;

        public RelayCommand(Func<Task> execute, Func<bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (() => true);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanExecute(object parameter)
        {
            if (Interlocked.Read(ref _isExecuting) != 0)
                return false;

            return _canExecute();
        }

        public async void Execute(object parameter)
        {
            Interlocked.Exchange(ref _isExecuting, 1);
            RaiseCanExecuteChanged();

            try
            {
                await _execute();
            }
            finally
            {
                Interlocked.Exchange(ref _isExecuting, 0);
                RaiseCanExecuteChanged();
            }
        }
    }

    /// <summary>
    /// Basic Relay command with type argument implemtation
    /// </summary>
    /// <seealso cref="System.Windows.Input.ICommand" />
    public class RelayCommand<T> : ICommand
    {
        private readonly Func<T, Task> _execute;
        private readonly Func<T, bool> _canExecute;
        private long _isExecuting;

        public RelayCommand(Func<T, Task> execute, Func<T, bool> canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute ?? (o => true);
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        public void RaiseCanExecuteChanged()
        {
            CommandManager.InvalidateRequerySuggested();
        }

        public bool CanExecute(object parameter)
        {
            if (Interlocked.Read(ref _isExecuting) != 0)
                return false;

            return _canExecute(parameter is T r ? r : default);
        }

        public async void Execute(object parameter)
        {
            Interlocked.Exchange(ref _isExecuting, 1);
            RaiseCanExecuteChanged();

            try
            {
                await _execute((T)parameter);
            }
            finally
            {
                Interlocked.Exchange(ref _isExecuting, 0);
                RaiseCanExecuteChanged();
            }
        }
        */
    }
    
}
