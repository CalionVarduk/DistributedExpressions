using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DistributedExpressions.Model;

namespace DistributedExpressions
{
    public partial class RemoveWindow : Window
    {
        ILocalVariables localVariables;

        public RemoveWindow(ILocalVariables localVariables)
        {
            InitializeComponent();
            this.localVariables = localVariables;
        }

        private void button_MouseEnter(object sender, MouseEventArgs e)
        {
            (removeButton.Foreground as SolidColorBrush).Color = MainWindow.ActiveButtonColor;
        }

        private void button_MouseLeave(object sender, MouseEventArgs e)
        {
            (removeButton.Foreground as SolidColorBrush).Color = MainWindow.InactiveButtonColor;
        }

        private void removeButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (localVariables.Remove(nameBox.Text))
            {
                DialogResult = true;
                Close();
            }
            else MessageBox.Show("Variable doesn't exist: " + nameBox.Text, "Remove Error");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            removeButton.MouseDown -= removeButton_MouseDown;
        }
    }
}
