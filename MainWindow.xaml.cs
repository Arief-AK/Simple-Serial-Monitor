using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.IO.Ports;
using System.Threading;
using System.Collections.ObjectModel;
using System.Windows.Threading;

namespace SerialMonitor_1
{
    public partial class MainWindow : Window
    {
        //******************************************************************************
        /*
         * MEMBER VARIABLES
         */
        //******************************************************************************

        private ObservableCollection<string> com_ports = new ObservableCollection<string>();
        private ObservableCollection<string> message_received = new ObservableCollection<string>();

        private List<string> baud_rate_combo_box = new List<string>();

        private string current_com_port, current_baud_rate;
        private bool _communicate, display_timestamp, custom_text;

        private string current_message;

        // Thread to handle reading serial communication
        public Thread SPReadThread;

        DispatcherTimer timer;

        SerialPort port;

        //******************************************************************************
        /*
         * PROPERTIES
         */
        //******************************************************************************
        public List<string> BAUD_RATE_CB
        {
            get
            {
                return baud_rate_combo_box;
            }
            set
            {
                if(value != baud_rate_combo_box)
                {
                    value = baud_rate_combo_box;
                }
            }
        }

        public ObservableCollection<string> COM_PORT_CB
        {
            get
            {
                return com_ports;
            }
            set
            {
                if(value != com_ports)
                {
                    value = com_ports;
                }
            }
        }

        public ObservableCollection<string> MessageReceived
        {
            get
            {
                return message_received;
            }
            set
            {
                if(value != message_received)
                {
                    value = message_received;
                }
            }
        }

        public string OutputMessage
        {
            get
            {
                return current_message;
            }
            set
            {
                if(value != current_message)
                {
                    value = current_message;
                }
            }
        }

        //******************************************************************************
        /*
         * HELPER FUNCTIONS
         */
        //******************************************************************************

        private void InitTimer()
        {
            timer = new DispatcherTimer(DispatcherPriority.Render,Application.Current.Dispatcher);
            timer.Interval = TimeSpan.FromMilliseconds(75);
            timer.Tick += timer_Tick;
            timer.Start();
        }

        private void InitPage()
        {
            // Set variables
            current_message = "";
            display_timestamp = false;
            custom_text = false;

            // Set display variables
            SERIAL_OUTPUT_MONITOR.Text = "Press 'START' button to start monitoring...\n";
            StartButton.Content = "START";
            ClearButton.Content = "CLEAR";
            CustomTextLabel.Visibility = Visibility.Hidden;
            CustomTextTextBox.Visibility = Visibility.Hidden;

            // Set communication to false
            _communicate = false;

            // Get all available serial ports
            var names = SerialPort.GetPortNames();

            // Add recognised com ports
            foreach (var element in names)
            {
                com_ports.Add(element);
            }

            // Add typical baud rates
            baud_rate_combo_box.Add("3600");
            baud_rate_combo_box.Add("9600");
            baud_rate_combo_box.Add("115200");

            // Set default strings
            current_com_port = "COM PORT:";
            current_baud_rate = "BAUD RATE:";

            // Assign to xaml
            CURRENT_COM_PORT.Text = current_com_port;
            CURRENT_BAUD_RATE.Text = current_baud_rate;
        }

        // Function to start reading from serial ports
        private void ReadSerial()
        {
            while (_communicate)
            {
                try
                {
                    if(display_timestamp)
                    {
                        string message = "[" + DateTime.Now.ToString("HH:mm:ss") + "] ";
                        message += port.ReadLine();
                        current_message = message;
                    }
                    else
                    {
                        current_message = port.ReadLine();
                        //Debug.WriteLine($"Message received: {current_message}\n");
                    }
                }
                catch(Exception)
                {
                    // Prompt user to select serial com port
                    MessageBoxResult result = MessageBox.Show("COM port disconnected", "COM port", MessageBoxButton.OK);
                    _communicate = false;
                    port.Dispose();

                    current_message = "";
                }
            }
        }

        private void StartReadThread()
        {
            // Create a new thread responisble for reading
            SPReadThread = new Thread(ReadSerial);

            // Start thread responsible for reading
            SPReadThread.Start();
        }

        private void StartMonitor()
        {
            // Check if com ports are available
            if(com_ports.Count != 0)
            {
                // Check if a com port is selected
                if(COM_PORT_COMBO_BOX.SelectedIndex == -1)
                {
                    // Prompt user to select serial com port
                    MessageBoxResult result = MessageBox.Show("Please select a COM port", "Select COM port", MessageBoxButton.OK);
                }
                else
                {
                    // Assign available port to current serial port
                    port = new SerialPort();
                    port.PortName = com_ports[COM_PORT_COMBO_BOX.SelectedIndex];
                    port.BaudRate = int.Parse(baud_rate_combo_box[BAUD_RATE_COMBO_BOX.SelectedIndex]);
                    port.Parity = Parity.None;
                    port.DataBits = 8;
                    port.StopBits = StopBits.One;
                    port.ReadTimeout = 500;
                    port.WriteTimeout = 50;

                    // Start connection with serial port
                    if (!port.IsOpen)
                    {
                        try
                        {
                            port.Open();

                            _communicate = true;

                            StartButton.Content = "STOP";
                            SERIAL_OUTPUT_MONITOR.Text += "\nBEGIN MONITORING {" + DateTime.Now.ToString("HH:mm:ss") + "}\n";
                        }
                        catch(Exception)
                        {
                            // Prompt user to select serial com port
                            MessageBoxResult result = MessageBox.Show("No COM ports found!", "COM port", MessageBoxButton.OK);
                        }
                    }
                }
            }
        }

        // Function to stop serial communication
        public void StopMonitor()
        {
            _communicate = false;
            SPReadThread.Join();
            port.Close();

            current_message = "";
        }

        //******************************************************************************
        /*
         * CONSTRUCTOR
         */
        //******************************************************************************

        public MainWindow()
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
            InitializeComponent();
            DataContext = this;
            InitPage();
        }

        //******************************************************************************
        /*
         * ALL EVENTS ARE HANDLED BELOW
         */
        //******************************************************************************

        private void timer_Tick(object sender, EventArgs e)
        {
            try
            {
                // Clear past contents
                if(SERIAL_OUTPUT_MONITOR.LineCount >= 100)
                {
                    SERIAL_OUTPUT_MONITOR.Text = current_message + '\n';
                }

                // Append the current message to serial monitor
                if(current_message.Length != 0)
                {
                    SERIAL_OUTPUT_MONITOR.Text += current_message + '\n';
                }
            }
            catch
            { }
        }

        private void ChangedBAUDRate(object sender, SelectionChangedEventArgs e)
        {
            current_baud_rate = "BAUD RATE: ";
            current_baud_rate += BAUD_RATE_COMBO_BOX.SelectedItem.ToString();
            CURRENT_BAUD_RATE.Text = current_baud_rate;
        }

        private void OnMonitorStart(object sender, RoutedEventArgs e)
        {
            if((string)StartButton.Content == "START")
            {
                if(COM_PORT_COMBO_BOX.HasItems)
                {
                    try
                    {
                        StartMonitor();
                        StartReadThread();
                        InitTimer();
                    }
                    catch(Exception)
                    { }
                }
                else
                {
                    // Prompt user to select serial com port
                    MessageBoxResult result = MessageBox.Show("No COM ports found!", "COM port", MessageBoxButton.OK);
                }
            }
            else
            {
                timer.Stop();
                StopMonitor();
                StartButton.Content = "START";
            }
        }

        private void MonitorTextChanged(object sender, TextChangedEventArgs e)
        {
            MonitorScrollView.ScrollToBottom();
        }

        private void OnClearClick(object sender, RoutedEventArgs e)
        {
            SERIAL_OUTPUT_MONITOR.Clear();
            SERIAL_OUTPUT_MONITOR.Text = "Press 'START' button to start monitoring...\n";
        }

        private void CustomTextChecked(object sender, RoutedEventArgs e)
        {
            custom_text = CustomTextCheckBox.IsChecked.Value;
        }

        private void CustomTextClicked(object sender, RoutedEventArgs e)
        {
            custom_text = CustomTextCheckBox.IsChecked.Value;

            if(custom_text)
            {
                CustomTextLabel.Visibility = Visibility.Visible;
                CustomTextTextBox.Visibility = Visibility.Visible;
            }
            else
            {
                CustomTextLabel.Visibility = Visibility.Hidden;
                CustomTextTextBox.Visibility = Visibility.Hidden;
            }
        }

        private void TimestampClicked(object sender, RoutedEventArgs e)
        {
            display_timestamp = TimestampCheckBox.IsChecked.Value;
        }

        private void ChangedCOMPort(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                current_com_port = "COM PORT: ";
                current_com_port += COM_PORT_COMBO_BOX.SelectedItem.ToString();
                CURRENT_COM_PORT.Text = current_com_port;
            }
            catch(Exception)
            {
            }
            
        }

        private void RefreshPorts(object sender, RoutedEventArgs e)
        {
            // Get all available serial ports
            var names = SerialPort.GetPortNames();

            ObservableCollection<string> str = new ObservableCollection<string>();

            // Add recognised com ports
            foreach (var element in names)
            {
                str.Add(element);
            }

            // Display to combo box
            COM_PORT_COMBO_BOX.ItemsSource = str;
        }
    }
}
