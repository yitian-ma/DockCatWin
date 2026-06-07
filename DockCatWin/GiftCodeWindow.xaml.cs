using System.Windows;

namespace DockCatWin;

public partial class GiftCodeWindow : Window
{
    public GiftCodeWindow()
    {
        InitializeComponent();
    }

    public string Code => CodeBox.Text;

    private void Submit_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}
