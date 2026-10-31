using System.Windows;

namespace bmp_2_panel;

public partial class InfoCard
{
    public InfoCard()
    {
        InitializeComponent();
    }
    
    public void SetInfo(string title, string subtitle, string description)
    {
        TxtTitle.Text = title;
        TxtSubtitle.Text = subtitle;
        TxtDescription.Text = description;
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e)
    {
        Visibility = Visibility.Collapsed;
    }
}