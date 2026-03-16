using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

namespace IIoT.Edge.UI.Shared.WpfBase
{
    /// <summary>
    /// 属性变更通知类
    /// </summary>
    public class BaseControlNotifyPropertyChanged : Control, INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged([CallerMemberName] string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}