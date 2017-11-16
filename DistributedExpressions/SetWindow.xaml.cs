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
    public partial class SetWindow : Window
    {
        private ILocalVariables localVariables;

        public SetWindow(ILocalVariables localVariables)
        {
            InitializeComponent();
            this.localVariables = localVariables;
        }

        private void valueBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            if (e.Text.Equals("-")) return;
            var s = valueBox.Text + e.Text;
            if (s.Length > 0)
            {
                decimal d;
                e.Handled = !decimal.TryParse(s.Replace('.', ','), out d);
            }
        }

        private void button_MouseEnter(object sender, MouseEventArgs e)
        {
            (setButton.Foreground as SolidColorBrush).Color = MainWindow.ActiveButtonColor;
        }

        private void button_MouseLeave(object sender, MouseEventArgs e)
        {
            (setButton.Foreground as SolidColorBrush).Color = MainWindow.InactiveButtonColor;
        }

        private void setButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            decimal d;
            if (decimal.TryParse(valueBox.Text.Replace('.', ','), out d) && localVariables.Set(new Variable<decimal>(nameBox.Text, d)))
            {
                DialogResult = true;
                Close();
            }
            else MessageBox.Show("Wrong variable name: " + nameBox.Text, "Set Error");
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            setButton.MouseDown -= setButton_MouseDown;
        }
    }
}
