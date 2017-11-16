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
using System.Net;
using System.IO;
using DistributedExpressions.Comms;
using DistributedExpressions.Model;

namespace DistributedExpressions
{
    public partial class LogWindow : Window
    {
        public int Count { get { return dateView.Items.Count; } }

        public LogWindow()
        {
            InitializeComponent();
        }

        public void LogOpen(ITcpCommunicator communicator)
        {
            AddDateTime();
            AddHost(communicator.LocalEndPoint);
            AddType("Tcp Communicator");
            AddContents("Listener opened.");
        }

        public void LogClose(ITcpCommunicator communicator)
        {
            AddDateTime();
            AddHost(communicator.LocalEndPoint);
            AddType("Tcp Communicator");
            AddContents("Listener closed after " + communicator.ElapsedListeningMs + " ms.");
        }

        public void LogSend(CommsEventArgs e)
        {
            StringBuilder sb = new StringBuilder(100);

            if (e.Ok)
            {
                sb.Append("Sent [ ");
                if (e.Packet.IsReadRequest) sb.Append("read request");
                else if (e.Packet.IsWriteRequest) sb.Append("write request");
                else
                {
                    sb.Append("read response");
                    if (!e.Packet.IsReadResponseOk) sb.Append(" (failed)");
                }
                sb.Append(" ], length: ").Append(e.Packet.Length).Append(", variable count: ").Append(e.Packet.VarCount).Append('.');
            }
            else
            {
                sb.Append("Send failure. Reason: ").Append(e.Msg.Replace(Environment.NewLine, ", "));
            }

            AddDateTime();
            AddHost(e.Packet.EndPoint);
            AddType("Packet");
            AddContents(sb.ToString());
        }

        public void LogReceive(CommsEventArgs e)
        {
            StringBuilder sb = new StringBuilder(100);

            if (e.Ok)
            {
                sb.Append("Received [ ");
                if (e.Packet.IsReadRequest) sb.Append("read request");
                else if (e.Packet.IsWriteRequest) sb.Append("write request");
                else
                {
                    sb.Append("read response");
                    if (!e.Packet.IsReadResponseOk) sb.Append(" (failed)");
                }
                sb.Append(" ], length: ").Append(e.Packet.Length).Append(", variable count: ").Append(e.Packet.VarCount).Append('.');
            }
            else
            {
                sb.Append("Receive failure. Reason: ").Append(e.Msg.Replace(Environment.NewLine, ", "));
            }

            AddDateTime();
            AddHost(e.Source);
            AddType("Packet");
            AddContents(sb.ToString());
        }

        public void Log(StatementHandler handler, StatementHandledEventArgs e)
        {
            StringBuilder sb = new StringBuilder(100);

            if (e.Successful)
            {
                sb.Append("Success. Result = ").Append(e.Result).Append(", calculated in ").Append(handler.ElapsedMs).Append(" ms.");
                if (e.Errors != null && e.Errors.Length > 0)
                {
                    sb.Append(" Warning(s): ");
                    foreach (var err in e.Errors)
                        sb.Append(err.Replace(Environment.NewLine, ", ")).Append("; ");
                    sb.Remove(sb.Length - 2, 2);
                }
            }
            else if (e.Cancelled)
            {
                sb.Append("Cancelled by the user after ").Append(handler.ElapsedMs).Append(" ms.");
            }
            else if (e.Nan)
            {
                sb.Append("Success. Result = NaN, calculated in ").Append(handler.ElapsedMs).Append(" ms.");
                if (e.Errors != null && e.Errors.Length > 0)
                {
                    sb.Append(" Warning(s): ");
                    foreach (var err in e.Errors)
                        sb.Append(err.Replace(Environment.NewLine, ", ")).Append("; ");
                    sb.Remove(sb.Length - 2, 2);
                }
            }
            else
            {
                sb.Append("Failure after ").Append(handler.ElapsedMs).Append(" ms.");
                if (e.Errors != null && e.Errors.Length > 0)
                {
                    sb.Append(" Reason(s): ");
                    foreach (var err in e.Errors)
                        sb.Append(err.Replace(Environment.NewLine, ", ")).Append("; ");
                    sb.Remove(sb.Length - 2, 2);
                }
            }

            AddDateTime();
            AddHost(handler.Communicator.LocalEndPoint);
            AddType("Statement Execution");
            AddContents(sb.ToString());
        }

        private void AddDateTime()
        {
            dateView.Items.Add(DateTime.Now.ToString());
        }

        private void AddHost(IPEndPoint ep)
        {
            hostView.Items.Add(ep.ToString());
        }

        private void AddType(string type)
        {
            typeView.Items.Add("[ " + type + " ]");
        }

        private void AddContents(string msg)
        {
            contentsView.Items.Add(msg);
        }

        private void button_MouseEnter(object sender, MouseEventArgs e)
        {
            ((sender as Label).Foreground as SolidColorBrush).Color = MainWindow.ActiveButtonColor;
        }

        private void button_MouseLeave(object sender, MouseEventArgs e)
        {
            ((sender as Label).Foreground as SolidColorBrush).Color = MainWindow.InactiveButtonColor;
        }

        private void clearButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            dateView.Items.Clear();
            hostView.Items.Clear();
            typeView.Items.Clear();
            contentsView.Items.Clear();
        }

        private void saveButton_MouseDown(object sender, MouseButtonEventArgs e)
        {
            var dialog = new Microsoft.Win32.SaveFileDialog();
            dialog.Filter = "text file (.txt)|*.txt";
            bool? result = dialog.ShowDialog();

            if (result.HasValue && result.Value)
            {
                using (var fs = new FileStream(dialog.FileName, FileMode.Create))
                using (var sw = new StreamWriter(fs))
                {
                    sw.WriteLine("[ " + DateTime.Now + " ] Distributed Expressions Logger report (" + Count + " entries).");
                    sw.WriteLine();
                    sw.WriteLine("=============================================================================================");
                    sw.WriteLine();
                    sw.WriteLine();

                    for (int i = 0; i < Count; ++i)
                    {
                        sw.WriteLine("[ Date & Time ]: " + dateView.Items[i]);
                        sw.WriteLine("[     Host    ]: " + hostView.Items[i]);
                        sw.WriteLine("[     Type    ]: " + typeView.Items[i]);
                        sw.WriteLine("[   Contents  ]: " + contentsView.Items[i]);
                        sw.WriteLine();
                        sw.WriteLine();
                    }
                }
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!MainWindow.ShutDown)
            {
                e.Cancel = true;
                Hide();
            }
        }
    }
}
