using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ColorCalcus.CalcData
{
    public class StepInfo : INotifyPropertyChanged
    {
        private int _index;
        private double _coloringDecreaseValue;
        private double _coloringDecreaseValueMult;

        public int Index { get => _index; set { _index = value; OnPropertyChanged(); } }
        public double ColoringDecreaseValue { get => _coloringDecreaseValue; set { _coloringDecreaseValue = value; OnPropertyChanged(); } }
        public double ColoringDecreaseValueMult { get => _coloringDecreaseValueMult; set { _coloringDecreaseValueMult = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
