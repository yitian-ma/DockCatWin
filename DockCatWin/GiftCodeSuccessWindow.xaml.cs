using System.Windows;
using System.Windows.Media;

namespace DockCatWin;

public partial class GiftCodeSuccessWindow : Window
{
    public GiftCodeSuccessWindow(string collectableName, ImageSource? image)
    {
        InitializeComponent();
        CollectableNameText.Text = collectableName;
        CollectableImage.Source = image;
        CollectableImage.Visibility = image is null ? Visibility.Collapsed : Visibility.Visible;
    }

    private void Done_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
