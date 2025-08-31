using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ColorCalcus.CalcData
{
    public class ColorRow : INotifyPropertyChanged
    {
        private int _index;
        private string _colorName = string.Empty;
        private bool _isSystem;

        public int Index { get => _index; set { _index = value; OnPropertyChanged(); } }
        public string ColorName { get => _colorName; set { _colorName = value; OnPropertyChanged(); } }
        public bool IsSystem { get => _isSystem; set { _isSystem = value; OnPropertyChanged(); } }

        public ObservableCollection<StepResult> StepResults { get; set; } = new();

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
