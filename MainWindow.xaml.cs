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
using System.IO.Ports;
using System.Windows.Threading;

using System.Management;    // Also add reference to system.Management (by default it is not available)

using System.IO;            // Used for file access

using LiveCharts;
using LiveCharts.Wpf;

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
        DispatcherTimer timer = new DispatcherTimer();

        SerialPort serial = new SerialPort();
        string recieved_data;

        private int comm_total = 0;
        private int comm_good = 0;
        private int comm_bad = 0;

        private string[] filterSettings = new string[] { "", "", "" };

        #endregion

        public MainWindow()
        {
            InitializeComponent();

            LiveChartSetup();
            DispatcherTimerSetup();
            ReadFilterSettings(null, null);

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

            // MessageBox.Show("Ready"); // For debugging
        }

        public SeriesCollection SeriesCollection { get; set; }
        public string[] Labels { get; set; }
        public Func<double, string> YFormatter { get; set; }

        #region serial_methods

        private void close_serial_port()
        {
            if (serial.IsOpen)
            {
                serial.Close();
                MessageBox.Show("Was open"); // For debugging
            }

            BrushConverter bc = new BrushConverter();
            Brush brush = (Brush)bc.ConvertFrom("#FFFFC8C8");
            brush.Freeze();
            con_status_label.Background = brush;

            con_btn.IsEnabled = true;
            dcon_btn.IsEnabled = false;
            con_status_label.Content = "Not connected";

            // MessageBox.Show("Closed"); // For debugging
        }

        public void DispatcherTimerSetup()
        {
            timer.Interval = TimeSpan.FromSeconds(1);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        void timer_Tick(object sender, EventArgs e)
        {
            // para.Inlines.Add("\r\nTick..\r\n\r\n");

            int counter = 0;
            int Combobox_Count = COM_box.Items.Count;

            string[] ComboBoxItem_portnames = new string[Combobox_Count];

            string[] init_portnames = System.IO.Ports.SerialPort.GetPortNames();
            int PORT_Count = init_portnames.Count();
            string[] desc = new string[PORT_Count];
            string[] man = new string[PORT_Count];
            bool[] not_keep = new bool[PORT_Count];
            int keep_count = PORT_Count;

            // Identify all COM ports, and retrieve information about them
            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'"))
            {
                var port_description = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());
                var Manufacturer = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Manufacturer"].ToString());

                // Put retreived info into string arrays
                counter = 0;
                foreach (string s in port_description)
                {
                    desc[counter] = s;
                    counter++;
                }

                // Put retreived info into string arrays
                counter = 0;
                foreach (string s in Manufacturer)
                {
                    man[counter] = s;
                    counter++;
                }
            }

            // Filter out the undesired COM ports
            for (int i = 0; i < PORT_Count; i++)
            {
                if (filterSettings[0] != "")  // Check if filter settings for COM are made
                {
                    if (!init_portnames[i].Contains(filterSettings[0]))  // Check if COM does not matches filter settings
                    {
                        not_keep[i] = true;
                        keep_count--;
                        continue;
                    }
                }

                if (filterSettings[1] != "")  // Check if filter settings for DESC are made
                {
                    if (!desc[i].Contains(filterSettings[1]))  // Check if DESC does not matches filter settings
                    {
                        not_keep[i] = true;
                        keep_count--;
                        continue;
                    }
                }

                if (filterSettings[2] != "")  // Check if filter settings for MAN are made
                {
                    if (!man[i].Contains(filterSettings[2]))  // Check if MAN does not matches filter settings
                    {
                        not_keep[i] = true;
                        keep_count--;
                        continue;
                    }
                }
            }

            // Make a list of the ports that are to be kept
            string[] portnames = new string[keep_count];

            counter = 0;
            for (int i = 0; i < PORT_Count; i++)
            {
                if (!not_keep[i])
                {
                    portnames[counter] = init_portnames[i];
                    counter++;
                }
            }

            // List of all com ports in combobox
            counter = 0;
            foreach (ComboBoxItem port in COM_box.Items)
                ComboBoxItem_portnames[counter++] = port.Content.ToString();

            // No change - All ports already in combobox
            if (ComboBoxItem_portnames.SequenceEqual(portnames))
                return;

            // New port - Add elements to combobox
            if (portnames.Except(ComboBoxItem_portnames).Any())
            {
                foreach (string cont in portnames.Except(ComboBoxItem_portnames))
                    COM_box.Items.Add(new ComboBoxItem { Content = cont });

                COM_box.SelectedIndex = 0; // Select a com port
            }

            // Disconnected port - Remove elements from combobox
            if (ComboBoxItem_portnames.Except(portnames).Any())
            {
                bool[] combo_index = new bool[Combobox_Count];

                foreach (string disc_port in ComboBoxItem_portnames.Except(portnames))
                {
                    for (int i = 0; i < Combobox_Count; i++)
                    {
                        ComboBoxItem cur_port = (ComboBoxItem) COM_box.Items[i];
                        // Checks if any port is selected
                        if (COM_box.SelectedValue != null)
                        {
                            ComboBoxItem sel_port = (ComboBoxItem)COM_box.SelectedValue;
                            // Checks if the selected port have been disconnected
                            if (disc_port == sel_port.Content.ToString())
                                close_serial_port(); // Make sure the application is set to disconnected state
                        }

                        if (cur_port.Content.ToString() == disc_port)
                        {
                            combo_index[i] = true;  // Remove the port at current index
                            break;  // Consider the next port that was disconnected
                        }
                            
                    }
                }

                // Remove the ports that have been disconnected
                for (int i = 0; i < Combobox_Count; i++)
                    if (combo_index[i]) COM_box.Items.RemoveAt(i);

                COM_box.SelectedIndex = 0; // Select a com port
            }

            // mcFlowDoc.Blocks.Add(para);
        }

        #endregion

        #region Recieving Serial Data // [0]

        private delegate void UpdateUiTextDelegate(string text);
        private void Recieve(object sender, System.IO.Ports.SerialDataReceivedEventArgs e)
        {
            // Collecting the characters received to our 'buffer' (string).
            recieved_data = serial.ReadExisting();
            // MainViewModel.Add_point();
            Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(WriteData), recieved_data);
            // Dispatcher.Invoke(DispatcherPriority.Send, new UpdateUiTextDelegate(MainViewModel.Add_point), recieved_data);
        }

        private void WriteData(string text)
        {
            comm_total += text.Length;
            comm_good += text.Length;

            comm_total_label.Content = "T: " + comm_total.ToString();
            comm_good_label.Content = "G: " + comm_good.ToString();
            comm_rate_label.Content = "R: " + (100 * comm_bad / comm_total).ToString() + " %";

            SeriesCollection[2].Values.Add(Convert.ToDouble(comm_total));

            if (raw_en.IsChecked.Value)
            {
                // Assign the value of the recieved_data to the RichTextBox.
                para.Inlines.Add(text);
                mcFlowDoc.Blocks.Add(para);
            }
                
        }

        #endregion

        #region Sending Serial Data // [0]

        public void SerialSend()
        {
            try
            {
                if (SerialCmdSend(Send_box.Text, serial))
                {
                    BrushConverter bc = new BrushConverter();
                    Brush brush = (Brush)bc.ConvertFrom("#FFDDDDDD");
                    brush.Freeze();
                    Send_btn.Background = brush;

                    if (clear_send_chk.IsChecked.Value) Send_box.Text = "";
                }
                else
                {
                    BrushConverter bc = new BrushConverter();
                    Brush brush = (Brush)bc.ConvertFrom("#FFFF6E6E");
                    brush.Freeze();
                    Send_btn.Background = brush;
                }
            }
            catch (Exception ex)
            {
                para.Inlines.Add("Failed to SEND" + Send_box.Text + "\n" + ex + "\n");
                mcFlowDoc.Blocks.Add(para);
                Commdata.Document = mcFlowDoc;
            }
        }

        public static bool SerialCmdSend(string data, SerialPort serial_port)
        {
            if (serial_port.IsOpen)
            {
                // Send the binary data out the port
                byte[] hexstring = Encoding.ASCII.GetBytes(data);
                foreach (byte hexval in hexstring)
                {
                    byte[] _hexval = new byte[] { hexval }; // need to convert byte to byte[] to write
                    serial_port.Write(_hexval, 0, 1);
                }
            }
            else
            {
                return false;
            }
            return true;
        }

        #endregion

        #region Form Controls

        private void con_btn_Click(object sender, RoutedEventArgs e)
        {
            if (COM_box.SelectedValue != null)
            {
                ComboBoxItem sel_port = (ComboBoxItem)COM_box.SelectedValue;

                serial.PortName = sel_port.Content.ToString();
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
            else
            {
                // Gjør knappen rød
            }
            
        }

        private void CloseCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e) // [2]
        {
            close_serial_port();
            this.Close();
        }

        private void TextBox_KeyEnterUpdate(object sender, KeyEventArgs e) // [1]
        {
            if (e.Key == Key.Enter)
            {
                SerialSend();
            }
        }

        private void clr_btn_Click(object sender, RoutedEventArgs e)
        {
            mcFlowDoc.Blocks.Clear();
            para.Inlines.Clear();
        }

        private void dcon_btn_Click(object sender, RoutedEventArgs e)
        {
            close_serial_port();
        }

        private void Commdata_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (auto_scroll_raw.IsChecked.Value) Commdata.ScrollToEnd();
        }

        private void Send_btn_Click(object sender, RoutedEventArgs e)
        {
            SerialSend();
        }

        #endregion

        private void create_log_btn_Click(object sender, RoutedEventArgs e)
        {
            //MainViewModel.Add_point();
        }

        #region Oxyplot
        /*
        public MainViewModel()
        {
            this.MyModel = new PlotModel { Title = "Example 1" };
            this.MyModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
        }

        public PlotModel MyModel { get; private set; }
        */
        #endregion
        
        #region LiveChart

        private void LiveChartSetup()
        {
            

            SeriesCollection = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Series 1",     // Optional
                    PointGeometry = null,   // Optional
                    PointGeometrySize = 15, // Optional
                    Values = new ChartValues<double> { 3, 5, 7, 4 }
                },
                new ColumnSeries
                {
                    Title = "Series 2",
                    Values = new ChartValues<decimal> { 5, 6, 2, 7 }
                }
            };

            Labels = new[] { "Jan", "Feb", "Mar", "Apr", "May" };
            YFormatter = value => value.ToString("C");

            //modifying the series collection will animate and update the chart
            SeriesCollection.Add(new LineSeries
            {
                Values = new ChartValues<double> { 5, 3, 2, 4 },
                LineSmoothness = 0 //straight lines, 1 really smooth lines
            });

            //modifying any series values will also animate and update the chart
            SeriesCollection[2].Values.Add(5d);

            DataContext = this;

        }

        #endregion

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            Subwindows.FilterWindow FilterWindow = new Subwindows.FilterWindow();
            FilterWindow.Closed += new EventHandler(ReadFilterSettings);
            FilterWindow.Show();
        }

        private void ReadFilterSettings(object sender, EventArgs e)
        {
            filterSettings = File.ReadAllLines("FilterSettings.txt");
        }
    }
}

// Bibliography

// [0]
// Where:   https://www.codeproject.com/Articles/130109/Serial-Communication-using-WPF-RS232-and-PIC-Commu
// By:      C_Johnson
// Posted:  24 Nov 2010
// Edited:  
// Read:    2021-02-10

// [1]
// Where:   https://stackoverflow.com/a/13289118/2883691
// By:      Ben
// Posted:  answered Nov 8 '12 at 12:26
// Edited:  edited Nov 8 '12 at 12:33
// Read:    2021-02-13

// [2]
// Where:   https://stackoverflow.com/a/63027984/2883691
// By:      dotNET
// Posted:  answered Jul 22 '20 at 6:11
// Edited:  
// Read:    2021-02-10