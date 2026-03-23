// 路径：src/Modules/IIoT.Edge.Module.Production/Equipment/EquipmentWidget.cs
using IIoT.Edge.Module.Production.Equipment.Models;
using IIoT.Edge.UI.Shared.PluginSystem;
using System.Collections.ObjectModel;

namespace IIoT.Edge.Module.Production.Equipment
{
    public class EquipmentWidget : WidgetBase
    {
        public override string WidgetId => "Core.Equipment";
        public override string WidgetName => "设备信息";

        // ── Tab 控制 ──────────────────────────────────────────────────
        private int _selectedTabIndex;
        public int SelectedTabIndex
        {
            get => _selectedTabIndex;
            set { _selectedTabIndex = value; OnPropertyChanged(); }
        }

        // ── Tab1：硬件状态 ────────────────────────────────────────────
        public ObservableCollection<HardwareItemViewModel> HardwareItems { get; } = new();

        // ── Tab2：配方信息 ────────────────────────────────────────────
        private string _recipeName = "暂无配方";
        public string RecipeName
        {
            get => _recipeName;
            set { _recipeName = value; OnPropertyChanged(); }
        }

        private string _recipeVersion = "--";
        public string RecipeVersion
        {
            get => _recipeVersion;
            set { _recipeVersion = value; OnPropertyChanged(); }
        }

        private string _processName = "--";
        public string ProcessName
        {
            get => _processName;
            set { _processName = value; OnPropertyChanged(); }
        }

        private bool _isRecipeActive;
        public bool IsRecipeActive
        {
            get => _isRecipeActive;
            set { _isRecipeActive = value; OnPropertyChanged(); }
        }

        public ObservableCollection<RecipeParamViewModel> Parameters { get; } = new();

        // ── Tab3：实时数据 ────────────────────────────────────────────
        private int _todayOutput;
        public int TodayOutput
        {
            get => _todayOutput;
            set { _todayOutput = value; OnPropertyChanged(); }
        }

        private string _todayYield = "0.00%";
        public string TodayYield
        {
            get => _todayYield;
            set { _todayYield = value; OnPropertyChanged(); }
        }

        private int _ngCount;
        public int NgCount
        {
            get => _ngCount;
            set { _ngCount = value; OnPropertyChanged(); }
        }

        private string _currentBatch = "--";
        public string CurrentBatch
        {
            get => _currentBatch;
            set { _currentBatch = value; OnPropertyChanged(); }
        }

        public EquipmentWidget()
        {
            LayoutRow = 1;
            LayoutColumn = 1;

            // 模拟数据，后期接入真实数据源
            LoadMockData();
        }

        private void LoadMockData()
        {
            // 硬件状态模拟数据
            HardwareItems.Add(new HardwareItemViewModel
            {
                Name = "1号叠片机",
                Address = "192.168.1.10",
                DeviceType = "PLC",
                IsConnected = true
            });
            HardwareItems.Add(new HardwareItemViewModel
            {
                Name = "2号叠片机",
                Address = "192.168.1.11",
                DeviceType = "PLC",
                IsConnected = false
            });
            HardwareItems.Add(new HardwareItemViewModel
            {
                Name = "入料扫码枪",
                Address = "COM3",
                DeviceType = "串口",
                IsConnected = true
            });
            HardwareItems.Add(new HardwareItemViewModel
            {
                Name = "出料扫码枪",
                Address = "COM4",
                DeviceType = "串口",
                IsConnected = false
            });

            // 配方模拟数据
            RecipeName = "冬季特调工艺";
            RecipeVersion = "V1.2";
            ProcessName = "叠片工序";
            IsRecipeActive = true;

            Parameters.Add(new RecipeParamViewModel
            {
                ParamName = "切刀速度",
                CurrentValue = "120",
                MinValue = "80",
                MaxValue = "150",
                Unit = "mm/s"
            });
            Parameters.Add(new RecipeParamViewModel
            {
                ParamName = "张力值",
                CurrentValue = "0.8",
                MinValue = "0.5",
                MaxValue = "1.2",
                Unit = "N"
            });
            Parameters.Add(new RecipeParamViewModel
            {
                ParamName = "温度",
                CurrentValue = "25",
                MinValue = "20",
                MaxValue = "35",
                Unit = "℃"
            });

            // 实时数据模拟
            TodayOutput = 1234;
            TodayYield = "98.5%";
            NgCount = 18;
            CurrentBatch = "LOT-20260323-001";
        }
    }
}