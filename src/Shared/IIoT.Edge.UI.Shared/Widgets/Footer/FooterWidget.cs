// 路径：src/Shared/IIoT.Edge.UI.Shared/Widgets/Footer/FooterWidget.cs
using IIoT.Edge.Common.Mvvm;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Windows.Threading;

namespace IIoT.Edge.UI.Shared.Widgets.Footer
{
    public class FooterWidget : WidgetBase
    {
        public override string WidgetId => "Core.Footer";
        public override string WidgetName => "系统底栏";

        private readonly DispatcherTimer _timer;
        private readonly DateTime _startTime = DateTime.Now;

        // ── 设备编号 ──────────────────────────────────────────────────
        private string _deviceCode = "未识别";
        public string DeviceCode
        {
            get => _deviceCode;
            set { _deviceCode = value; OnPropertyChanged(); }
        }

        // ── 实时时钟 ──────────────────────────────────────────────────
        private string _currentTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
        public string CurrentTime
        {
            get => _currentTime;
            private set { _currentTime = value; OnPropertyChanged(); }
        }

        // ── 运行时长 ──────────────────────────────────────────────────
        private string _upTime = "00:00:00";
        public string UpTime
        {
            get => _upTime;
            private set { _upTime = value; OnPropertyChanged(); }
        }

        public FooterWidget()
        {
            LayoutRow = 2;
            LayoutColumn = 0;
            ColumnSpan = 12;

            // 每秒刷新时钟和运行时长
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(1)
            };
            _timer.Tick += OnTimerTick;
            _timer.Start();
        }

        private void OnTimerTick(object? sender, EventArgs e)
        {
            CurrentTime = DateTime.Now.ToString("yyyy/MM/dd HH:mm:ss");
            var elapsed = DateTime.Now - _startTime;
            UpTime = $"{(int)elapsed.TotalHours:D2}:{elapsed.Minutes:D2}:{elapsed.Seconds:D2}";
        }

        /// <summary>
        /// 设备MAC寻址成功后由外部调用更新设备编号
        /// </summary>
        public void SetDeviceCode(string deviceCode)
        {
            DeviceCode = deviceCode;
        }
    }
}