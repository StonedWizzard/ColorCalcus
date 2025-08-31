using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ColorCalcus.CalcData
{
    public class StepResult : INotifyPropertyChanged
    {
        public StepInfo Step { get; set; }
        public ColorRow Color { get; set; }

        private double _inputPaintValue;
        private double _currentPaintValue;
        private double _paintAfterColoringValue;

        public double InputPaintValue { get => _inputPaintValue; set { _inputPaintValue = value; OnPropertyChanged(); } }
        public double CurrentPaintValue { get => _currentPaintValue; set { _currentPaintValue = value; OnPropertyChanged(); } }
        public double PaintAfterColoringValue { get => _paintAfterColoringValue; set { _paintAfterColoringValue = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
