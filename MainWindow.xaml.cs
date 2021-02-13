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

// https://www.codeproject.com/Articles/130109/Serial-Communication-using-WPF-RS232-and-PIC-Commu
using System.IO.Ports;
using System.Windows.Threading;

// Plotting
// https://github.com/oxyplot
// https://github.com/Live-Charts/Live-Charts

namespace WPC_Interface
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        #region variables
        //Richtextbox
        FlowDocument mcFlowDoc = new FlowDocument();
        Paragraph para = new Paragraph();

        // SERIAL PORT
        SerialPort serial = new SerialPort();
        string recieved_data;

        private int comm_total = 0;
        private int comm_good = 0;
        private int comm_bad = 0;

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            #region handles
            //this.PreviewKeyDown += new KeyEventHandler(HandleEsc);

            // Where:   https://stackoverflow.com/a/55290660/2883691
            // By:      Logan K
            // Posted:  answered Mar 21 '19 at 23:20
            // Edited:  
            // Read:    2021-02-13
            // 
            // The handler has to be made here since we are accessing an element in the UI (auto_scroll_raw, a checkbox) in the handler.
            // The handler is called before InitializeComponent() is finished running, 
            // which means that the checkbox have not finished initializing (most likely). 
            // Therefore it throws null errors if this is added in the graphical editor of Visual Studio, and most likely the XAML too.
            Commdata.TextChanged += new TextChangedEventHandler(Commdata_TextChanged);
            #endregion

            Commdata.Document = mcFlowDoc;
            serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(Recieve);
        }

        #region Recieving Serial Data

        private delegate void UpdateUiTextDelegate(string text);
        private void Recieve(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            // Collecting the characters received to our 'buffer' (string).
            recieved_data = serial.ReadExisting();
            
            Dispatcher.Invoke(DispatcherPriority.Send,
            new UpdateUiTextDelegate(WriteData), recieved_data);
        }

        private void WriteData(string text)
        {
            comm_total += text.Length;
            comm_good += text.Length;

            comm_total_label.Content = "T: " + comm_total.ToString();
            comm_good_label.Content = "G: " + comm_good.ToString();
            comm_rate_label.Content = "R: " + (100 * comm_bad / comm_total).ToString() + " %";

            // Assign the value of the recieved_data to the RichTextBox.
            para.Inlines.Add(text);
            mcFlowDoc.Blocks.Add(para);
        }

        #endregion

        #region Form Controls

        private void con_btn_Click(object sender, RoutedEventArgs e)
        {
            serial.PortName = COM_box.SelectedValue.ToString();
            serial.BaudRate = Convert.ToInt32(Baud_box.SelectedValue.ToString());
            serial.DataBits = Convert.ToInt32(Data_box.SelectedValue.ToString());

            if (Stop_box.SelectedValue.ToString() == "1") serial.StopBits = StopBits.One;
            else if (Stop_box.SelectedValue.ToString() == "1.5") serial.StopBits = StopBits.OnePointFive;
            else if (Stop_box.SelectedValue.ToString() == "2") serial.StopBits = StopBits.Two;

            if (Parity_box_N.IsSelected) serial.Parity = Parity.None;
            else if (Parity_box_E.IsSelected) serial.Parity = Parity.Even;
            else if (Parity_box_O.IsSelected) serial.Parity = Parity.Odd;

            serial.Handshake = System.IO.Ports.Handshake.None;
            serial.ReadTimeout = 200;
            serial.WriteTimeout = 50;

            serial.Open();

            BrushConverter bc = new BrushConverter();
            Brush brush = (Brush)bc.ConvertFrom("#FFD7FF92");
            brush.Freeze();
            con_status_label.Background = brush;

            con_btn.IsEnabled = false;
            dcon_btn.IsEnabled = true;
            con_status_label.Content = "Connected";
        }

        private void CloseCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            // Where:   https://stackoverflow.com/a/63027984/2883691
            // By:      dotNET
            // Posted:  answered Jul 22 '20 at 6:11
            // Edited:  
            // Read:    2021-02-10

            //if (MessageBox.Show("Close?", "Close", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                this.Close();
        }

        private void clr_btn_Click(object sender, RoutedEventArgs e)
        {
            mcFlowDoc.Blocks.Clear();
            para.Inlines.Clear();
        }

        private void dcon_btn_Click(object sender, RoutedEventArgs e)
        {
            serial.Close();

            BrushConverter bc = new BrushConverter();
            Brush brush = (Brush)bc.ConvertFrom("#FFFFC8C8");
            brush.Freeze();
            con_status_label.Background = brush;

            con_btn.IsEnabled = true;
            dcon_btn.IsEnabled = false;
            con_status_label.Content = "Not connected";
        }

        private void Commdata_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (auto_scroll_raw.IsChecked.Value) Commdata.ScrollToEnd();
        }

        #endregion
    }
}
