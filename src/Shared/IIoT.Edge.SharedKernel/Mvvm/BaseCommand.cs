using System;
using System.Windows.Input;

namespace IIoT.Edge.Common.Mvvm
{
    /// <summary>
    /// 纯净的 MVVM 命令基类 (不依赖任何 WPF 专属组件)
    /// </summary>
    public class BaseCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Func<object?, bool>? _canExecute;

        // 原生的事件，不再依赖 WPF 的 CommandManager
        public event EventHandler? CanExecuteChanged;

        /// <summary>
        /// 构造函数 (支持传入 1 个参数的执行逻辑，及可选的判断逻辑)
        /// </summary>
        public BaseCommand(Action<object?> execute, Func<object?, bool>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter)
        {
            return _canExecute == null || _canExecute(parameter);
        }

        public void Execute(object? parameter)
        {
            _execute(parameter);
        }

        /// <summary>
        /// 如果你需要手动通知 UI 刷新按钮的可用状态，可以调用这个方法
        /// </summary>
        public void RaiseCanExecuteChanged()
        {
            CanExecuteChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}