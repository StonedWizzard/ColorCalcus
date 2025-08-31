using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
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

        public void AddNewStep(double paintDecrease = 1.0)
        {
            Data.AddStep(paintDecrease);
            BuildGridColumns();
        }

        public void RemoveStep(StepInfo step)
        {
            Data.RemoveStep(step);
            BuildGridColumns();
        }
        public void RemoveLastStep()
        {
            if (Data.Steps.Count == 0) return;
            var lastStep = Data.Steps.Last();
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

            for (int i = 0; i < Data.Steps.Count; i++)
            {
                int stepIndex = i;
                Grid.Columns.Add(new DataGridTextColumn
                {
                    Header = $"Шаг #{Data.Steps[i].Index}",
                    Binding = new Binding($"StepResults[{stepIndex}].InputPaintValue")
                    {
                        Mode = BindingMode.TwoWay,
                        StringFormat = "0.00",
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
                    },
                    Width = 100
                });

                if (HideIntermediateColumns) continue;
                Grid.Columns.Add(new DataGridTextColumn
                {
                    Header = $"Шаг #{Data.Steps[i].Index} (промежуточн | дельта: {Data.Steps[i].ColoringDecreaseValueMult.ToString("0.00")})",
                    Binding = new Binding($"StepResults[{stepIndex}].CurrentPaintValue") { Mode = BindingMode.OneWay, StringFormat = "0.00" },
                    Width = 75,
                    IsReadOnly = true
                });

                Grid.Columns.Add(new DataGridTextColumn
                {
                    Header = $"Шаг #{Data.Steps[i].Index} (остаток | выкрас: {Data.Steps[i].ColoringDecreaseValue.ToString("0.00")})",
                    Binding = new Binding($"StepResults[{stepIndex}].PaintAfterColoringValue") { Mode = BindingMode.OneWay, StringFormat = "0.00" },
                    Width = 75,
                    IsReadOnly = true
                });
            }

            if (Data.Steps.Count > 0)
            {
                int lastIndex = Data.Steps.Count - 1;
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
            var summaryRow = GetColorSummaryRow(); // твой метод

            StepInfo? previousStep = null;
            foreach (var step in stepsOrdered)
            {
                double totalInput = 0.0;
                double totalCurrent = 0.0;
                double totalAfter = 0.0;

                // 1) current = input + residual(prev)
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

                double mult = totalCurrent <= 0 ? 1.0 : (totalCurrent - step.ColoringDecreaseValue) / totalCurrent;
                if (mult < 0) mult = 0;
                if (mult > 1) mult = 1;
                step.ColoringDecreaseValueMult = mult;

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
                previousStep = step;
            }
            UpdateColumnHeaders();
        }

        private ColorRow? GetColorSummaryRow() => Data?.Colors?.FirstOrDefault(c => c.IsSystem);

        private void UpdateColumnHeaders()
        {
            for (int i = 0; i < Data.Steps.Count; i++)
            {
                var step = Data.Steps[i];
                var headerColumn = Grid.Columns
                    .OfType<DataGridTextColumn>()
                    .FirstOrDefault(c => c.Header is string str && str.StartsWith($"Шаг #{step.Index} (промежуточн"));

                if (headerColumn != null)
                    headerColumn.Header = $"Шаг #{step.Index} (промежуточн | дельта: {step.ColoringDecreaseValueMult:0.00})";                
            }
        }
    }
}
