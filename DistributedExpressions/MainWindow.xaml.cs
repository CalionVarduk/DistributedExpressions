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
using System.Windows.Navigation;
using System.Windows.Shapes;
using DistributedExpressions.Comms;
using DistributedExpressions.Model;
using System.IO;

namespace DistributedExpressions
{
    public partial class MainWindow : Window
    {
        internal static bool ShutDown = false;
        internal static readonly Color ActiveButtonColor = Color.FromRgb(142, 188, 255);
        internal static readonly Color InactiveButtonColor = Color.FromRgb(0, 104, 255);

        private ILocalVariables localVariables;
        private ITcpCommunicator communicator;
        private StatementHandler handler;
        private LogWindow logger;

        public MainWindow()
        {
            InitializeComponent();
            logger = new LogWindow();
            statusBusyBar.Visibility = Visibility.Collapsed;
            mainGrid.Visibility = Visibility.Collapsed;
            ResizeMode = ResizeMode.NoResize;
            SetClientSize(200, 85);
        }

        private void StatementHandled_Event(object sender, StatementHandledEventArgs e)
        {
            if (e.Successful)
            {
                resultBox.Text = e.Result + " (" + handler.ElapsedMs + " ms)";
                RefreshVarView();
            }
            else if (e.Cancelled)
            {
                resultBox.Text = "Cancelled (" + handler.ElapsedMs + " ms)";
            }
            else if (e.Nan)
            {
                resultBox.Text = "NaN (" + handler.ElapsedMs + " ms)";
            }
            else
            {
                resultBox.Text = "Error (" + handler.ElapsedMs + " ms)";
            }
            UnlockUI();
            logger.Log(handler, e);
        }

        private void menuSet_Click(object sender, RoutedEventArgs e)
        {
            var form = new SetWindow(localVariables);
            form.ShowDialog();

            if (form.DialogResult.HasValue && form.DialogResult.Value)
                RefreshVarView();
        }

        private void menuRemove_Click(object sender, RoutedEventArgs e)
        {
            var form = new RemoveWindow(localVariables);
            form.ShowDialog();

            if (form.DialogResult.HasValue && form.DialogResult.Value)
                RefreshVarView();
        }

        private void menuClear_Click(object sender, RoutedEventArgs e)
        {
            localVariables.Clear();
            varView.Items.Clear();
            varViewTitle.Content = "Local Variables (Count: 0):";
        }

        private void menuDisconnect_Click(object sender, RoutedEventArgs e)
        {
            Disconnect();
            TransitToLoadGrid();
        }

        private void button_MouseEnter(object sender, MouseEventArgs e)
        {
            ((sender as Label).Foreground as SolidColorBrush).Color = ActiveButtonColor;
        }

        private void button_MouseLeave(object sender, MouseEventArgs e)
        {
            ((sender as Label).Foreground as SolidColorBrush).Color = InactiveButtonColor;
        }

        private void execButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (handler.IsRunning) handler.Cancel = true;
            else {
                resultBox.Text = "";
                LockUI();
                handler.Handle(exprBox.Text);
            }
        }

        private void exprBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (resultBox.Text.Length > 0)
                resultBox.Text = "";
        }

        private void exprBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                resultBox.Text = "";
                LockUI();
                handler.Handle(exprBox.Text);
            }
        }

        private void logButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (logger.Visibility != Visibility.Visible)
                logger.Show();
            else logger.Focus();
        }

        private void PacketReceived_Event(object sender, CommsEventArgs e)
        {
            logger.LogReceive(e);
            if (e.Ok && e.Packet.IsWriteRequest)
                RefreshVarView();
        }

        private void PacketSent_Event(object sender, CommsEventArgs e)
        {
            logger.LogSend(e);
        }

        private void localButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            localVariables = new LocalVariables();
            communicator = new TcpCommunicator(localVariables) { NoDelay = true };
            communicator.Received += PacketReceived_Event;
            communicator.Sent += PacketSent_Event;
            communicator.Open();
            handler = new StatementHandler(communicator, localVariables);
            handler.StatementHandled += StatementHandled_Event;

            statusVal.Content = "Local";
            statusLocalIp.Content = "Local IP: " + communicator.LocalIp;
            statusPublicIp.Content = "Public IP: N/A";
            statusPort.Content = "Port: " + communicator.Port;

            TransitToMainGrid();
        }

        private async void publicButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            loadLabel.Visibility = Visibility.Hidden;
            localButton.Visibility = Visibility.Hidden;
            publicButton.Visibility = Visibility.Hidden;
            loadLogsButton.Visibility = Visibility.Hidden;
            connectLabel.Visibility = Visibility.Visible;

            await Task.Run(() =>
            {
                localVariables = new LocalVariables();
                communicator = new TcpCommunicator(false, localVariables) { NoDelay = true };
                communicator.Received += PacketReceived_Event;
                communicator.Sent += PacketSent_Event;
                handler = new StatementHandler(communicator, localVariables);
                handler.StatementHandled += StatementHandled_Event;
            });

            communicator.Open();
            statusVal.Content = "Connected";
            statusLocalIp.Content = "Local IP: " + communicator.LocalIp;
            statusPublicIp.Content = "Public IP: " + communicator.PublicIp;
            statusPort.Content = "Port: " + communicator.Port;

            TransitToMainGrid();
        }

        private void RefreshVarView()
        {
            varView.Items.Clear();
            foreach (var v in localVariables)
                varView.Items.Add(v.Name + " = " + v.Value);
            varViewTitle.Content = "Local Variables (Count: " + localVariables.Count + "):";
        }

        private void LockUI()
        {
            exprBox.IsReadOnly = true;
            menuSet.IsEnabled = false;
            menuRemove.IsEnabled = false;
            menuClear.IsEnabled = false;
            menuDisconnect.IsEnabled = false;
            execButton.Content = "Cancel";
            statusBar.Background = new SolidColorBrush(Colors.OrangeRed);
            statusExec.Content = "Executing";
            statusBusyBar.Visibility = Visibility.Visible;
        }

        private void UnlockUI()
        {
            exprBox.IsReadOnly = false;
            menuSet.IsEnabled = true;
            menuRemove.IsEnabled = true;
            menuClear.IsEnabled = true;
            menuDisconnect.IsEnabled = true;
            execButton.Content = "Execute";
            statusBar.Background = new SolidColorBrush(Colors.SteelBlue);
            statusExec.Content = "Ready";
            statusBusyBar.Visibility = Visibility.Collapsed;
        }

        private void Disconnect()
        {
            communicator.Received -= PacketReceived_Event;
            communicator.Sent -= PacketSent_Event;
            localVariables = null;
            communicator.Dispose();
            logger.LogClose(communicator);
            communicator = null;
            handler.Dispose();
            handler = null;

            varView.Items.Clear();
            varViewTitle.Content = "Local Variables (Count: 0):";
            exprBox.Text = "";
            resultBox.Text = "";
        }

        private void TransitToMainGrid()
        {
            logger.LogOpen(communicator);

            ResizeMode = ResizeMode.CanResize;
            SetClientSize(525, 350);
            MinWidth = Width;
            MinHeight = Height;
            statusBusyBar.Visibility = Visibility.Collapsed;
            loadGrid.Visibility = Visibility.Collapsed;
            mainGrid.Visibility = Visibility.Visible;

            loadLabel.Visibility = Visibility.Visible;
            localButton.Visibility = Visibility.Visible;
            publicButton.Visibility = Visibility.Visible;
            loadLogsButton.Visibility = Visibility.Visible;
            connectLabel.Visibility = Visibility.Collapsed;
        }

        private void TransitToLoadGrid()
        {
            MinWidth = 0;
            MinHeight = 0;
            ResizeMode = ResizeMode.NoResize;
            SetClientSize(200, 85);
            loadGrid.Visibility = Visibility.Visible;
            mainGrid.Visibility = Visibility.Collapsed;
        }

        private void SetClientSize(double w, double h)
        {
            Width = w + SystemParameters.ResizeFrameHorizontalBorderHeight * 2;
            Height = h + SystemParameters.CaptionHeight + SystemParameters.ResizeFrameVerticalBorderWidth * 2;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (communicator != null)
                Disconnect();

            ShutDown = true;
            logger.Close();
            logger = null;
        }
    }
}
