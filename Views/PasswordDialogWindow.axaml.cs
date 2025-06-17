using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace TextToImageCrypto.Views;
// диалоговое окно пароля
public partial class PasswordDialogWindow : Window
{
    public PasswordDialogWindow()
    {
        InitializeComponent();
    }
    
    public PasswordDialogWindow(string message) : this()
    {
        this.FindControl<TextBlock>("MessageText").Text = message;
    }

    private void InitializeComponent() => AvaloniaXamlLoader.Load(this);
    
    private void OnOkClick(object sender, RoutedEventArgs e)
    {
        var passwordBox = this.FindControl<TextBox>("PasswordBox");
        Close(passwordBox.Text);
    }
}