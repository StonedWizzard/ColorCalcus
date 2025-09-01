using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using ColorCalcus.CalcData;

namespace ColorCalcus
{
    internal class CalcController
    {
        private readonly DataGrid Grid;

        public GridData Data { get; private set; }

        private bool _hideIntermediateColumns;
        public bool HideIntermediateColumns 
        {
            get => _hideIntermediateColumns;
            set
            {
                _hideIntermediateColumns = value;
                BuildGridColumns();
            }
        }

        public CalcController(DataGrid grid)
        {
            if(grid is null)
                throw new ArgumentNullException(nameof(grid));

            Grid = grid;
            Data = new GridData();
            BuildGridColumns();
            Grid.ItemsSource = Data.Colors;
            Grid.CellEditEnding += GridCellEditEnding;
        }

        private void GridCellEditEnding(object sender, DataGridCellEditEndingEventArgs e) => CalculateValues();

        public void AddNewStep(double paintDecrease = 1.0, StepType stepType = StepType.Default)
        {
            Data.AddStep(paintDecrease, stepType);
            BuildGridColumns();
            if(stepType == StepType.Refill)
                CalculateValues();
        }

        public void RemoveStep(StepInfo step)
        {
            Data.RemoveStep(step);
            BuildGridColumns();
        }
        public void RemoveLastStep()
        {
            if (Data.Steps.Count == 0) return;
            var stepsOrdered = Data.Steps.OrderBy(s => s.Index).ToList();
            var lastStep = stepsOrdered.Last();
            RemoveStep(lastStep);
        }

        public void AddNewColor(string? name = null)
        {
            var sysRow = Data.Colors.FirstOrDefault(c => c.IsSystem);
            int idx = sysRow != null ? Data.Colors.IndexOf(sysRow) : Data.Colors.Count;

            var row = Data.AddColor(name);
            if (sysRow != null)
                Data.Colors.Move(Data.Colors.Count - 1, idx);            
            Data.ReindexColors();
        }

        public void RemoveColor(ColorRow row)
        {
            Data.RemoveColor(row);
            Data.ReindexColors();
        }
        public void RemoveSelectedColor()
        {
            if (Grid.SelectedItem is ColorRow row)
                RemoveColor(row);            
        }

        public void InitializeNewData()
        {
            Data = new GridData();
            BuildGridColumns();
            Grid.ItemsSource = Data.Colors;
        }

        public bool HasSpecialStep(StepType stepType) => Data.HasSpecialStep(stepType);

        private void GetStepHeaders(StepInfo stepInfo, out string inputColName, out string currentColName, out string afterColName)
        {
            if(stepInfo is null)
            {
                inputColName = "!!!";
                currentColName = "!!!";
                afterColName = "!!!";
                return;
            }

            if(stepInfo.Type == StepType.Default)
            {
                inputColName = $"Шаг #{stepInfo.Index}";
                currentColName = $"текущее | дельта: {stepInfo.StepValueMult.ToString("0.00")}";
                afterColName = $"остаток | выкрас: {stepInfo.StepValue.ToString("0.00")}";
            }
            else if (stepInfo.Type == StepType.Refill)
            {
                inputColName = $"Долив";
                currentColName = $"текущее | дельта: {stepInfo.StepValueMult.ToString("0.00")}";
                afterColName = $"остаток | долив: {stepInfo.StepValue.ToString("0.00")}";
            }
            else
            {
                inputColName = "???";
                currentColName = "???";
                afterColName = "???";
            }
        }

        private void BuildGridColumns()
        {
            Grid.Columns.Clear();
            Grid.Columns.Add(new DataGridTextColumn
            {
                Header = "Пигмент",
                Binding = new System.Windows.Data.Binding("ColorName"),
                IsReadOnly = false,
                Width = 200
            });

            var stepsOrdered = Data.Steps.OrderBy(s => s.Index).ToList();
            for (int i = 0; i < stepsOrdered.Count; i++)
            {
                int stepIndex = i;
                string inputColName, currentColName, afterColName;
                GetStepHeaders(stepsOrdered[i], out inputColName, out currentColName, out afterColName);

                Grid.Columns.Add(new DataGridTextColumn
                {
                    Header = inputColName,
                    Binding = new Binding($"StepResults[{stepIndex}].InputPaintValue")
                    {
                        Mode = BindingMode.TwoWay,
                        StringFormat = "0.00",
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    },
                    Width = 100,
                    IsReadOnly = stepsOrdered[i].Type == StepType.Refill
                });

                if (HideIntermediateColumns) continue;
                Grid.Columns.Add(new DataGridTextColumn
                {
                    Header = currentColName,
                    Binding = new Binding($"StepResults[{stepIndex}].CurrentPaintValue") { Mode = BindingMode.OneWay, StringFormat = "0.00" },
                    Width = 75,
                    IsReadOnly = true
                });

                Grid.Columns.Add(new DataGridTextColumn
                {
                    Header = afterColName,
                    Binding = new Binding($"StepResults[{stepIndex}].PaintAfterColoringValue") { Mode = BindingMode.OneWay, StringFormat = "0.00" },
                    Width = 75,
                    IsReadOnly = true
                });
            }

            if (stepsOrdered.Count > 0)
            {
                int lastIndex = stepsOrdered.Count - 1;
                Grid.Columns.Add(new DataGridTextColumn
                {
                    Header = "Результат",
                    Binding = new Binding($"StepResults[{lastIndex}].PaintAfterColoringValue")
                    {
                        Mode = BindingMode.OneWay,
                        StringFormat = "0.00"
                    },
                    Width = 100,
                    IsReadOnly = true,
                    ElementStyle = new Style(typeof(TextBlock))
                    {
                        Setters = { new Setter(TextBlock.FontWeightProperty, FontWeights.Bold) }
                    },
                    HeaderStyle = new Style(typeof(DataGridColumnHeader))
                    {
                        Setters =
                        {
                            new Setter(DataGridColumnHeader.HorizontalContentAlignmentProperty, HorizontalAlignment.Center),
                            new Setter(DataGridColumnHeader.FontWeightProperty, FontWeights.Bold)
                        }
                    }
                });
            }
        }

        private void CalculateValues()
        {
            if (Data.Steps.Count == 0 || Data.Colors.Count == 0) return;

            var stepsOrdered = Data.Steps.OrderBy(s => s.Index).ToList();
            var colorsOrdered = Data.Colors.OrderBy(c => c.Index).ToList();
            var workColors = colorsOrdered.Where(c => !c.IsSystem).ToList();
            var summaryRow = GetColorSummaryRow();

            if (summaryRow is null)
            {
                MessageBox.Show("Результатирующая строка не обнаружена!\r\n", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            StepInfo? previousStep = null;
            foreach (var step in stepsOrdered)
            {
                switch (step.Type)
                {
                    case StepType.Default:
                        CalculateDefaultStepValues(step, summaryRow, workColors, previousStep);
                        break;
                    case StepType.Refill:
                        CalculateRefillStepValues(step, summaryRow, workColors, previousStep);
                        break;
                    default:
                        continue;
                }
                previousStep = step;
            }
            UpdateColumnHeaders();
        }

        private void CalculateDefaultStepValues(StepInfo step, ColorRow summaryRow, IEnumerable<ColorRow> workColors, StepInfo? previousStep)
        {
            double totalInput = 0.0;
            double totalCurrent = 0.0;
            double totalAfter = 0.0;

            foreach (var color in workColors)
            {
                var cell = color.StepResults.FirstOrDefault(sr => sr.Step == step);
                if (cell == null) continue;

                totalInput += cell.InputPaintValue;
                double prevResidual = 0.0;
                if (previousStep != null)
                {
                    var prevCell = color.StepResults.FirstOrDefault(sr => sr.Step == previousStep);
                    if (prevCell != null)
                        prevResidual = prevCell.PaintAfterColoringValue;
                }

                cell.CurrentPaintValue = cell.InputPaintValue + prevResidual;
                totalCurrent += cell.CurrentPaintValue;
            }

            double mult = totalCurrent <= 0 ? 1.0 : (totalCurrent - step.StepValue) / totalCurrent;
            if (mult < 0) mult = 0.0;
            if (mult > 1) mult = 1.0;
            step.StepValueMult = mult;

            foreach (var color in workColors)
            {
                var cell = color.StepResults.FirstOrDefault(sr => sr.Step == step);
                if (cell == null) continue;

                cell.PaintAfterColoringValue = cell.CurrentPaintValue * mult;
                totalAfter += cell.PaintAfterColoringValue;
            }

            if (summaryRow != null)
            {
                var sumCell = summaryRow.StepResults.FirstOrDefault(sr => sr.Step == step);
                if (sumCell != null)
                {
                    sumCell.InputPaintValue = totalInput;
                    sumCell.CurrentPaintValue = totalCurrent;
                    sumCell.PaintAfterColoringValue = totalAfter;
                }
            }
        }

        private void CalculateRefillStepValues(StepInfo step, ColorRow summaryRow, IEnumerable<ColorRow> workColors, StepInfo? previousStep)
        {
            if (previousStep == null) return;

            double totalPreviousAfter = 0.0;
            foreach (var color in workColors)
            {
                var prevCell = color.StepResults.FirstOrDefault(sr => sr.Step == previousStep);
                if (prevCell != null)
                    totalPreviousAfter += prevCell.PaintAfterColoringValue;
            }

            if (totalPreviousAfter <= 0) return;

            double refillTarget = step.StepValue;
            double factor = refillTarget / totalPreviousAfter;

            double totalInput = 0.0;
            double totalCurrent = 0.0;
            double totalAfter = 0.0;

            foreach (var color in workColors)
            {
                var cell = color.StepResults.FirstOrDefault(sr => sr.Step == step);
                var prevCell = color.StepResults.FirstOrDefault(sr => sr.Step == previousStep);
                if (prevCell is null || cell is null) continue;

                cell.CurrentPaintValue = prevCell.PaintAfterColoringValue * factor;
                cell.InputPaintValue = cell.CurrentPaintValue;
                cell.PaintAfterColoringValue = cell.CurrentPaintValue + prevCell.PaintAfterColoringValue;

                totalInput += cell.InputPaintValue;
                totalCurrent += cell.CurrentPaintValue;
                totalAfter += cell.PaintAfterColoringValue;
            }

            var sumCell = summaryRow.StepResults.FirstOrDefault(sr => sr.Step == step);
            if (sumCell != null)
            {
                sumCell.InputPaintValue = totalInput;
                sumCell.CurrentPaintValue = totalCurrent;
                sumCell.PaintAfterColoringValue = totalAfter;
            }            
            step.StepValueMult = factor;
        }

        private ColorRow? GetColorSummaryRow() => Data?.Colors?.FirstOrDefault(c => c.IsSystem);

        private void UpdateColumnHeaders()
        {
            var stepsOrdered = Data.Steps.OrderBy(s => s.Index).ToList();
            for (int i = 0; i < stepsOrdered.Count; i++)
            {
                var step = stepsOrdered[i];
                string inputColName, currentColName, afterColName;
                GetStepHeaders(step, out inputColName, out currentColName, out afterColName);

                int baseIndex = 1 + i * (HideIntermediateColumns ? 1 : 3);
                if (baseIndex < Grid.Columns.Count && Grid.Columns[baseIndex] is DataGridTextColumn inputCol)
                    inputCol.Header = inputColName;

                if (!HideIntermediateColumns)
                {
                    if (baseIndex + 1 < Grid.Columns.Count && Grid.Columns[baseIndex + 1] is DataGridTextColumn currentCol)
                        currentCol.Header = currentColName;

                    if (baseIndex + 2 < Grid.Columns.Count && Grid.Columns[baseIndex + 2] is DataGridTextColumn afterCol)
                        afterCol.Header = afterColName;
                }
            }
        }
    }
}
