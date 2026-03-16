using System.Windows.Input;

namespace IIoT.Edge.UI.Shared.WpfBase
{
    /// <summary>
    /// 基础命令类
    /// </summary>
    public class BaseCommand : ICommand
    {
        public event EventHandler CanExecuteChanged;

        public Action<object> DoExectue { set; get; }

        public bool CanExecute(object parameter)
        {
            return true;
        }

        public void Execute(object parameter)
        {
            if (DoExectue != null)
            {
                DoExectue.Invoke(parameter);
            }
        }
    }
}