using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace MsgBox
{
    public class GetAllDialog : Window
    {
        public GetAllDialog()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public static Task<bool> Show(Window parent, string title)
        {
            bool res = false;
            var msgbox = new MessageBox()
            {
                Title = title
            };
            var buttonPanel = msgbox.FindControl<StackPanel>("Buttons");

            var tcs = new TaskCompletionSource<bool>();
            msgbox.Closed += delegate { tcs.TrySetResult(res); };
            if (parent != null)
                msgbox.ShowDialog(parent);
            else msgbox.Show();
            return tcs.Task;
        }
    }
}
