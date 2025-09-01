using System.Collections.ObjectModel;

namespace ColorCalcus.CalcData
{
    public class GridData
    {
        public ObservableCollection<ColorRow> Colors { get; set; } = new();
        public ObservableCollection<StepInfo> Steps { get; set; } = new();

        public GridData()
        {
            Colors = new();
            Steps = new();
            AddColor("Всего:", isSystem: true);
            //AddStep(0);
        }
       
        public StepInfo AddStep(double paintDecrease = 1.0, StepType type = StepType.Default)
        {
            if (HasSpecialStep(type)) return null;

            var step = new StepInfo
            {
                Type = type,
                Index = Steps.Count,
                StepValue = paintDecrease
            };
            Steps.Add(step);

            foreach (var color in Colors)
                color.StepResults.Add(new StepResult { Step = step, Color = color });
            ReindexSteps();
            return step;
        }

        public void RemoveStep(StepInfo step)
        {
            if (step == null) return;

            Steps.Remove(step);
            foreach (var color in Colors)
            {
                var cell = color.StepResults.FirstOrDefault(r => r.Step == step);
                if (cell != null) color.StepResults.Remove(cell);
            }
            ReindexSteps();
        }

        public ColorRow AddColor(string? name = null, bool isSystem = false)
        {
            var color = new ColorRow
            {
                Index = Colors.Count + 1,
                ColorName = name ?? $"Цвет {Colors.Count}",
                IsSystem = isSystem
            };

            foreach (var step in Steps)
                color.StepResults.Add(new StepResult { Step = step, Color = color });

            Colors.Add(color);
            return color;
        }

        public void RemoveColor(ColorRow color)
        {
            if (color == null || color.IsSystem) return;
            Colors.Remove(color);
            ReindexColors();
        }

        public void ReindexColors()
        {
            int idx = 1;
            foreach (var c in Colors.Where(c => !c.IsSystem))
                c.Index = idx++;
            var summary = Colors.FirstOrDefault(c => c.IsSystem);
            if (summary != null)
                summary.Index = idx;
        }

        public void ReindexSteps()
        {
            int idx = 0;

            foreach (var step in Steps.Where(s => s.Type == StepType.Default))
                step.Index = idx++;

            foreach (var step in Steps.Where(s => s.Type != StepType.Default))
                step.Index = idx++;
        }

        public void ReindexAll()
        {
            ReindexColors();
            ReindexSteps();
        }

        public bool HasSpecialStep(StepType stepType)
        {
            if(stepType == StepType.Default) return false;
            return Steps.FirstOrDefault(s => s.Type == stepType) != null;
        }
    }
}
