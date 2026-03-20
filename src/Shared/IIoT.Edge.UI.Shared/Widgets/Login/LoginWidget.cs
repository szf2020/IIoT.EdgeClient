// 路径：src/Shared/IIoT.Edge.UI.Shared/Widgets/Login/LoginWidget.cs
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.Contracts;
using IIoT.Edge.Contracts.Model;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Windows.Input;

namespace IIoT.Edge.UI.Shared.Widgets.Login
{
    public class LoginWidget : WidgetBase
    {
        public override string WidgetId => "Core.Login";
        public override string WidgetName => "登录";

        private readonly IAuthService _authService;

        private string _employeeNo = string.Empty;
        public string EmployeeNo
        {
            get => _employeeNo;
            set { _employeeNo = value; OnPropertyChanged(); }
        }

        private string _password = string.Empty;
        public string Password
        {
            get => _password;
            set { _password = value; OnPropertyChanged(); }
        }

        private bool _isLocalMode = false;
        public bool IsLocalMode
        {
            get => _isLocalMode;
            set
            {
                _isLocalMode = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsCloudMode));
                OnPropertyChanged(nameof(ModeTitle));
                EmployeeNo = string.Empty;
                Password = string.Empty;
                ErrorMessage = string.Empty;
            }
        }

        public bool IsCloudMode => !_isLocalMode;
        public string ModeTitle => _isLocalMode ? "本地紧急管理员" : "云端账号登录";

        private bool _isBusy;
        public bool IsBusy
        {
            get => _isBusy;
            set { _isBusy = value; OnPropertyChanged(); }
        }

        private string _errorMessage = string.Empty;
        public string ErrorMessage
        {
            get => _errorMessage;
            set { _errorMessage = value; OnPropertyChanged(); }
        }

        public ICommand LoginCommand { get; }
        public ICommand SwitchModeCommand { get; }
        public ICommand CloseCommand { get; }

        public event Action? LoginSucceeded;

        public LoginWidget(IAuthService authService)
        {
            _authService = authService;

            LoginCommand = new AsyncCommand(ExecuteLoginAsync);
            SwitchModeCommand = new BaseCommand(_ => IsLocalMode = !IsLocalMode);
            CloseCommand = new BaseCommand(_ =>
            {
                EmployeeNo = string.Empty;
                Password = string.Empty;
                ErrorMessage = string.Empty;
            });
        }

        private async Task ExecuteLoginAsync()
        {
            ErrorMessage = string.Empty;

            if (string.IsNullOrWhiteSpace(Password))
            {
                ErrorMessage = "请输入密码";
                return;
            }

            if (IsCloudMode && string.IsNullOrWhiteSpace(EmployeeNo))
            {
                ErrorMessage = "请输入工号";
                return;
            }

            IsBusy = true;
            try
            {
                AuthResult result = IsLocalMode
                    ? await _authService.LoginLocalAsync(Password)
                    : await _authService.LoginCloudAsync(EmployeeNo, Password);

                if (result.Success)
                    LoginSucceeded?.Invoke();
                else
                    ErrorMessage = result.Message;
            }
            finally
            {
                IsBusy = false;
            }
        }
    }
}