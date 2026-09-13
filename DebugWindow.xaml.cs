using System.Windows;

namespace FoldVision
{
    public partial class DebugWindow : Window
    {
        public DebugWindow()
        {
            InitializeComponent();
        }

        private void AngleSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            SensorService.Instance.SetDebugAngle((float)e.NewValue);
        }
    }
}
