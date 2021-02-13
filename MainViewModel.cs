namespace WPC_Interface
{
    using System;
    using System.Windows.Input;
    using System.Windows;
    using System.Windows.Automation.Peers;
    using System.Windows.Automation.Provider;

    using OxyPlot;
    using OxyPlot.Series;

    public class MainViewModel
    {
        #region Oxyplot

        public MainViewModel()
        {
            this.MyModel = new PlotModel { Title = "Example 1" };
            this.MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
        }

        public PlotModel MyModel { get; private set; }

        #endregion

        /*
        #region Send_on_return

        private ICommand someCommand;
        public ICommand SomeCommand
        {
            get
            {
                return someCommand
                    ?? (someCommand = new ActionCommand(() =>
                    {
                        // MessageBox.Show("SomeCommand");
                        // MainWindow.SerialSend();

                        ButtonAutomationPeer peer = new ButtonAutomationPeer(MainWindow.Send_btn);
                        IInvokeProvider invokeProv = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                        invokeProv.Invoke();
                    }));
            }
        }
    }

    public class ActionCommand : ICommand
    {
        private readonly Action _action;

        public ActionCommand(Action action)
        {
            _action = action;
        }

        public void Execute(object parameter)
        {
            _action();
        }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public event EventHandler CanExecuteChanged;
        #endregion*/
    }


}