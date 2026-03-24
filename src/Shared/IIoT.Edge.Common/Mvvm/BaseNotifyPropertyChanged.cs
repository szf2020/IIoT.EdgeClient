using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace IIoT.Edge.Common.Mvvm
{
    /// <summary>
    /// 属性变更通知类
    /// </summary>
    public class BaseNotifyPropertyChanged : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}