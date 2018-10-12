using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
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
using System.Windows.Threading;

namespace EventViewerLog
{
    public partial class MainWindow : Window
    {
        private System.Windows.Forms.NotifyIcon ni;
        private System.Windows.Forms.ContextMenuStrip cms;

        string eventLog, eventLevel, eventId, eventDataSearch;
        int eventCount = 0, eventCountNew = 0;

        DispatcherTimer timer = new DispatcherTimer();


        public MainWindow()
        {
            //Mutex mutex = new Mutex(true, "250C4597-BA73-45DF-B2CF-DD645F045845");
            //if (!mutex.WaitOne(TimeSpan.Zero, true))
            if (Process.GetProcessesByName(Process.GetCurrentProcess().ProcessName).Length > 1)
            {
                System.Windows.MessageBox.Show("Application already running!", "Application Start", MessageBoxButton.OK, MessageBoxImage.Warning);
                System.Windows.Application.Current.Shutdown();
            }

            Stream iconPath = System.Windows.Application.GetResourceStream(new Uri("pack://application:,,,/EventViewerLog;component/program.ico")).Stream;
            //String iconPath = @Environment.GetEnvironmentVariable("PROGRAMFILES") + "\\" + softComp + "\\" + softName + "\\icon\\" + "program.ico";
            //System.Windows.Forms.MessageBox.Show(iconPath);
            ni = new System.Windows.Forms.NotifyIcon();
            ni.Icon = new System.Drawing.Icon(iconPath);
            ni.DoubleClick += delegate (object sender, EventArgs args)
            {
                this.Show();
                this.WindowState = WindowState.Normal;
            };
            cms = new System.Windows.Forms.ContextMenuStrip(); cms.Items.Add("Exit", new System.Drawing.Bitmap(iconPath)).Click += new EventHandler(cmExit);
            ni.ContextMenuStrip = cms;

            InitializeComponent();

            timer.Tick += new EventHandler(timer_Tick);

        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            ni.Visible = true;
        }
        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Kapanmıyor. Bütün pencereler kapansa dahi Task ta çalışıyor.
            e.Cancel = true;
            //this.ShowInTaskbar = false;
            //this.WindowState = WindowState.Minimized; 
            this.Hide();

            ////Kapanıyor.
            //ni.Visible = false;
            //System.Windows.Application.Current.Shutdown();
            ////Environment.Exit(0);
        }
        private void cmExit(Object sender, EventArgs e)
        {
            ni.Visible = false;
            System.Windows.Application.Current.Shutdown();
            //Environment.Exit(0);
        }


        private void bt_start_Click(object sender, RoutedEventArgs e)
        {
            if (bt_start.Content.ToString() == "Start")
            {
                timer.Interval = new TimeSpan(0, 0, 0);
                timer.Start();

                bt_start.Foreground = new SolidColorBrush(Colors.Red);
                bt_start.Content = "Stop";
                //this.Hide();
            }
            else
            {
                timer.Stop();

                txb_eventLastDate.Text = string.Empty;
                txb_eventData.Text = string.Empty;
                eventCount = 0;
                txb_eventCount.Text = eventCount.ToString();
                txb_eventLastDate.Foreground = new SolidColorBrush(Colors.Black);
                eventCountNew = 0;
                txb_eventCountNew.Text = eventCount.ToString();
                txb_eventCountNew.Foreground = new SolidColorBrush(Colors.Black);

                bt_start.Foreground = new SolidColorBrush(Colors.Black);
                bt_start.Content = "Start";
            }
        }


        private void timer_Tick(object sender, EventArgs e)
        {
            txb_eventLastDate.Text = string.Empty;
            txb_eventData.Text = string.Empty;
            eventCount = 0;

            eventLog = cb_eventLog.Text;
            eventLevel = cb_eventLavel.Text;
            eventId = txb_eventId.Text;
            eventDataSearch = txb_eventDataSearch.Text;
            //eventDataSearch = "Wolf.Playout.exe";

            string query = "*[System/EventID=" + eventId + " and " + "System/Level=" + eventLevel + "]";
            EventLogQuery eventsQuery = new EventLogQuery(eventLog, PathType.LogName, query);
            try
            {
                EventLogReader logReader = new EventLogReader(eventsQuery);

                for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
                {
                    if (eventdetail.FormatDescription().Contains(eventDataSearch))
                    {
                        txb_eventLastDate.Text = eventdetail.TimeCreated.ToString();
                        txb_eventData.Text = eventdetail.FormatDescription();
                        eventCount++;

                        //MessageBox.Show(
                        //eventdetail.TimeCreated.ToString() + "\n" +
                        //eventdetail.FormatDescription()
                        //);
                        //string eventXml = eventdetail.ToXml();
                    }
                }
                txb_eventCount.Text = eventCount.ToString();
            }
            catch (EventLogNotFoundException ex)
            {
                MessageBox.Show("Error while reading the event logs:\n\n" + ex.Message);
                return;
            }

            timer.Interval = new TimeSpan(Convert.ToInt16(txb_timerH.Text), Convert.ToInt16(txb_timerM.Text), Convert.ToInt16(txb_timerS.Text));
            DateTime dt = new DateTime(2010, 1, 1);
            if (eventCount > 0)
                if (txb_eventLastDate.Text.Trim() != "")
                    dt = DateTime.Parse(txb_eventLastDate.Text); //son hata zamanı.
            TimeSpan ts = DateTime.Now - dt; //son hata zamanından sonra geçen süre.
            //TimeSpan ts =  DateTime.Now.Subtract(dt);

            if (ts < timer.Interval) //son timer periyodu içinde yeni bir hata olmuş ise.
            {
                //MessageBox.Show(
                // dt.ToString() + "\n" +
                // DateTime.Now.ToString() + "\n\n" +
                // ts.ToString() + "\n" +
                // timer.Interval.ToString()
                //);

                eventCountNew++;
                txb_eventCountNew.Text = eventCountNew.ToString();
                txb_eventCountNew.Foreground = new SolidColorBrush(Colors.Red);
                txb_eventLastDate.Foreground = new SolidColorBrush(Colors.Red);

                processKill(txb_eventDataSearch.Text.Trim());

                Thread.Sleep(500);
                System.Windows.Forms.Application.DoEvents();

                processStart(txb_eventDataSearch.Text.Trim());
            }
        }
        private void processStart(string pName)
        {
            try
            {
                if (Process.GetProcessesByName(pName).Length == 0)
                {
                    Process p = new Process();
                    
                    p.StartInfo.FileName = pName;
                    p.StartInfo.WorkingDirectory = txb_eventProcessPath.Text.Trim();
                    //p.StartInfo.WindowStyle = ProcessWindowStyle.Maximized;
                    //p.StartInfo.CreateNoWindow = true;
                    p.Start();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Starting " + pName + " :\n" + ex.Message, "processStart", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        private void processKill(string pName)
        {
            try
            {
                pName = pName.Replace(".exe", "").Trim();
                Process[] process = Process.GetProcessesByName(pName);
                if (process.Length > 0)
                    foreach (var p in process)
                        p.Kill();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show("Stopping " + pName + " :\n" + ex.Message, "processKill", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }





    }
}
