using MahApps.Metro.Controls;
using System.Windows;

namespace RandomTaikoSongProgram
{
    /// <summary>
    /// RandomWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class RandomWindow : MetroWindow
    {
        public RandomWindow(string songName) : this()
        {
            LblSearchName.Content = $"{songName}";
        }

        private void MetroWindow_Loaded(object sender, RoutedEventArgs e)
        {

        }

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {

        }
    }
}
