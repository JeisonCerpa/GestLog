using System;
using System.Windows;

namespace GestLog.Views
{
    public partial class UpdateWindow : Window
    {
        public UpdateWindow()
        {
            InitializeComponent();
        }

        public void SetProgress(double percent)
        {
            ProgressBar.Value = percent;
            ProgressText.Text = $"{percent:0}%";
        }
    }
}
