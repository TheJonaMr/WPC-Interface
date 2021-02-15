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

using System.Management;    // Also add reference to system.Management (by default it is not available)

using System.IO;            // Used for file access

namespace WPC_Interface.Subwindows
{
    /// <summary>
    /// Interaction logic for FilterWindow.xaml
    /// </summary>
    public partial class FilterWindow : Window
    {
        private string[] filterSettings = new string[] { "", "", "" };

        public FilterWindow()
        {
            InitializeComponent();

            ReadFilterSettings();

            SearchPorts();

        }

        private void SearchPorts()
        {
            string[] portnames = System.IO.Ports.SerialPort.GetPortNames();
            int port_count = portnames.Length;

            using (var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'")) // [1]
            {
                var port_description = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Caption"].ToString());
                var Manufacturer = searcher.Get().Cast<ManagementBaseObject>().ToList().Select(p => p["Manufacturer"].ToString());

                string[] descriptions = new string[port_count];
                string[] manufacturers = new string[port_count];

                int counter = 0;
                foreach (string s in port_description)
                {
                    descriptions[counter] = s;
                    counter++;
                }

                counter = 0;
                foreach (string s in Manufacturer)
                {
                    manufacturers[counter] = s;
                    counter++;
                }

                List<Ports> items = new List<Ports>();
                for (int i = 0; i < port_count; i++)
                {
                    items.Add(new Ports() { COM = portnames[i], DESC = descriptions[i], MAN = manufacturers[i] });
                }
                lvPorts.ItemsSource = items;
                
            }
        }
        // You need to use the Microsoft.Bcl.Async NuGet package to bring in the appropriate library support for .NET 4.
        // private static async void Button_Click(object sender, RoutedEventArgs e)
        private void Apply_Click(object sender, RoutedEventArgs e)
        {
            string[] filters = new string[] { COM_f.Text, DESC_f.Text, MAN_f.Text };
            File.WriteAllLines("FilterSettings.txt", filters);
        }

        private void ReadFilterSettings()
        {
            filterSettings = File.ReadAllLines("FilterSettings.txt");
            COM_f.Text = filterSettings[0];
            DESC_f.Text = filterSettings[1];
            MAN_f.Text = filterSettings[2];
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("The filter is used to decide which COM ports" +
                "\r\nare added to the COM ports list in the main window." +
                "\r\nAny port that does not contain the value" +
                "\r\nof the filter will be excluded." +
                "\r\nThe list of COM ports in the FilterWindow" +
                "\r\ncontains all available ports.");
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void CloseCommandBinding_Executed(object sender, System.Windows.Input.ExecutedRoutedEventArgs e)
        {
            this.Close();
        }
    }

    public class Ports
    {
        public string COM { get; set; }     // See XAML -> <GridViewColumn Header="COM Port" Width="75" DisplayMemberBinding="{Binding COM}" />

        public string DESC { get; set; }    // See XAML -> DisplayMemberBinding="{Binding DESC}

        public string MAN { get; set; }     // See XAML -> DisplayMemberBinding="{Binding MAN}
    }
}


// Bibliography

// [0]
// Where:   https://www.wpf-tutorial.com/listview-control/listview-with-gridview/
// By:      
// Posted:  
// Edited:  
// Read:    2021-02-15

// [1]
// Where:   https://stackoverflow.com/a/46683622
// By:      humudu
// Posted:  answered Oct 11 '17 at 8:39
// Edited:  
// Read:    2021-02-15

// [2]
// Where:   https://stackoverflow.com/a/59508646
// By:      Muno
// Posted:  answered Dec 28 '19 at 6:24 
// Edited:  
// Read:    2021-02-15

// [3]
// Where:   https://stackoverflow.com/questions/21108585/the-type-or-namespace-name-async-could-not-be-found
// By:      
// Posted:  
// Edited:  
// Read:    2021-02-15

