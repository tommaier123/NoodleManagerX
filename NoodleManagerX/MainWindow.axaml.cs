using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using NoodleManagerX.Models;

namespace NoodleManagerX
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            this.DataContext = new MainViewModel();

#if DEBUG
            this.AttachDevTools();
#endif
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
