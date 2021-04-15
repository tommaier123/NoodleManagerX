using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using Xilium.CefGlue.Avalonia;

namespace NoodleManagerX
{
    public class PreviewWindow : Window
    {
        private static AvaloniaCefBrowser webView;

        static PreviewWindow s_instance;

        public PreviewWindow()
        {
            s_instance = this;
            AvaloniaXamlLoader.Load(this);

            webView = new AvaloniaCefBrowser();
            webView.Width = 400;
            webView.Height = 225;
            
            Grid parent = s_instance.FindControl<Grid>("webViewParent");
            parent.Children.Add(webView);

#if DEBUG
            this.AttachDevTools();
#endif

            this.PointerLeave += pointerLeft;
        }

        public static void ShowPreview(Window parent, string url)
        {
            if (s_instance == null)
            {
                s_instance = new PreviewWindow();
            }

            s_instance.Position = new PixelPoint(MainWindow.s_instance.mouse.Position.X - (int)s_instance.Width / 2, MainWindow.s_instance.mouse.Position.Y - (int)s_instance.Height / 2);

            Console.WriteLine(url);
            webView.Address = url;

            if (parent != null)
            {

                s_instance.ShowDialog(parent);
            }
            else
            {
                s_instance.Show();
            }
        }

        public static void HidePreview()
        {
            webView.Address = "https://www..com";
            s_instance.Hide();
        }

        private void pointerLeft(object sender, PointerEventArgs e)
        {
            HidePreview();
        }
    }
}
