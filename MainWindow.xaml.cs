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
        private int counter = 0;

        //Richtextbox
        FlowDocument mcFlowDoc = new FlowDocument();
        Paragraph para = new Paragraph();

        // SERIAL PORT
        SerialPort serial = new SerialPort();
        string recieved_data;
        #endregion

        public MainWindow()
        {
            InitializeComponent();

            #region handles
            //this.PreviewKeyDown += new KeyEventHandler(HandleEsc);
            #endregion
        }

        /*private void Button_Click(object sender, RoutedEventArgs e)
        {
            counter++;
            label1.Content = "Text, hello: " + counter.ToString();
        }*/

        private void con_btn_Click(object sender, RoutedEventArgs e)
        {
            serial.PortName = "COM2"; // Comm_Port_Names.Text; //Com Port Name                
            serial.BaudRate = 9600; // Convert.ToInt32(Baud_Rates.Text); //COM Port Sp
            serial.Handshake = System.IO.Ports.Handshake.None;
            serial.Parity = Parity.None;
            serial.DataBits = 8;
            serial.StopBits = StopBits.One;
            serial.ReadTimeout = 200;
            serial.WriteTimeout = 50;

            serial.DataReceived += new System.IO.Ports.SerialDataReceivedEventHandler(Recieve);
        }

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
            // Assign the value of the recieved_data to the RichTextBox.
            para.Inlines.Add(text);
            mcFlowDoc.Blocks.Add(para);
            Commdata.Document = mcFlowDoc;
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
    }
}
