using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace ColorCalcus
{
    public partial class MainWindow : Window
    {
        private CalcController Controller;

        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            Controller = new CalcController(ResultGrid);
            BtnToggleIntermediate_Click(sender, e);
        }
        

        private void BtnAddStep_Click(object s, RoutedEventArgs e)
        {
            if (StepDecreaseValueBox.Value.HasValue)
                Controller.AddNewStep(StepDecreaseValueBox.Value.Value);
            else Controller.AddNewStep();
            StepDecreaseValueBox.Value = 0.0;
        }
        private void BtnRemoveStep_Click(object s, RoutedEventArgs e) => Controller.RemoveLastStep();
        private void BtnAddColor_Click(object s, RoutedEventArgs e) => Controller.AddNewColor();
        private void BtnRemoveColor_Click(object s, RoutedEventArgs e) => Controller.RemoveSelectedColor();

        private void BtnClear_Click(object s, RoutedEventArgs e)
        {
            if(MessageBox.Show("Очистить расчёты?", "Вимание", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Controller.InitializeNewData();
        }

        private void CopyGridButton_Click(object sender, RoutedEventArgs e)
        {
            var dataGrid = ResultGrid;
            if (dataGrid.Items.Count == 0) return;

            StringBuilder sb = new StringBuilder();

            foreach (var column in dataGrid.Columns) {
                sb.Append(column.Header + "\t");
            }
            sb.AppendLine();

            foreach (var item in dataGrid.Items)
            {
                if (item is not null)
                {
                    foreach (var column in dataGrid.Columns)
                    {
                        if (column.GetCellContent(item) is TextBlock tb)
                            sb.Append(tb.Text + "\t");
                        else
                            sb.Append("\t");
                    }
                    sb.AppendLine();
                }
            }
            Clipboard.SetText(sb.ToString());
        }

        private void BtnToggleIntermediate_Click(object sender, RoutedEventArgs e)
        {
            Controller.HideIntermediateColumns = !Controller.HideIntermediateColumns;
            BtnToggleIntermediate.Content = Controller.HideIntermediateColumns
                ? "📈 Показать промежуточные расчёты"
                : "📉 Скрыть промежуточные расчёты";
        }
    }
}