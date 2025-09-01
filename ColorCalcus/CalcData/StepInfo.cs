using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ColorCalcus.CalcData
{
    public enum StepType
    {
        Default,
        Refill
    }

    public class StepInfo : INotifyPropertyChanged
    {
        private StepType _type;
        private int _index;
        private double _stepValue;
        private double _stepValueMult;

        public StepType Type { get => _type; set { _type = value; OnPropertyChanged(); } }
        public int Index { get => _index; set { _index = value; OnPropertyChanged(); } }
        public double StepValue { get => _stepValue; set { _stepValue = value; OnPropertyChanged(); } }
        public double StepValueMult { get => _stepValueMult; set { _stepValueMult = value; OnPropertyChanged(); } }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
