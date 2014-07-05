using System.Windows;
using WpfApplication1.ViewModels;

namespace WpfApplication1
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        private MainViewModel VM 
        { get { return DataContext as MainViewModel; } }
        
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (VM == null) { return; }

            VM.Read("Sample.txt");
            dataGrid1.ItemsSource = VM.Records.Items;
        }
    }
}
