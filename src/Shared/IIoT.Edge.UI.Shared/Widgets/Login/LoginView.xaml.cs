// 路径：src/Shared/IIoT.Edge.UI.Shared/Widgets/Login/LoginView.xaml.cs
using MaterialDesignThemes.Wpf;
using System.Windows.Controls;

namespace IIoT.Edge.UI.Shared.Widgets.Login
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();

            this.DataContextChanged += (s, e) =>
            {
                if (e.NewValue is LoginWidget vm)
                {
                    PasswordBox.PasswordChanged += (_, _) =>
                        vm.Password = PasswordBox.Password;

                    vm.LoginSucceeded += () =>
                        DialogHost.CloseDialogCommand.Execute(null, null);
                }
            };
        }
    }
}