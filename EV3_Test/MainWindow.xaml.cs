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
using System.Windows.Threading;
using System.Diagnostics;
using Lego.Ev3.Desktop;
using Lego.Ev3.Core;

namespace EV3_Test
{
    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window
    {
        // EV3 brick
        private Brick brick;
        private DispatcherTimer timer;
        private Int32 time = 0;
        private double tick = 10.0;

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(tick);
            timer.Tick += new EventHandler(TickTimer);
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(async () =>
			{
                //brick = new Brick(new UsbCommunication());
                brick = new Brick(new BluetoothCommunication("COM4"));
                try
                {
                    await brick.ConnectAsync();
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }));

            brick.BrickChanged += OnBrickChanged;

            timer.Start();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (brick != null)
            {
                brick.Disconnect();
            }
            brick = null;
        }

        // EV3の状況変更時に起こるイベント
        private void OnBrickChanged(object sender, BrickChangedEventArgs e)
        {
            Debug.WriteLine("Changed");
        }

        // ボタンが押された時
        private async void GoForward_Click(object sender, RoutedEventArgs e)
        {
            brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, 40, 1000, false);
            brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, 40, 1000, false);
            await brick.BatchCommand.SendCommandAsync();
        }

        // tickミリ秒間隔で実行されるイベント
        private void TickTimer(object sender, EventArgs e)
        {
            SensorValue.Content = time.ToString() + ": " + 
                brick.Ports[InputPort.Three].SIValue.ToString() + ", " + 
                brick.Ports[InputPort.Four].RawValue.ToString();
            time++;
            Trace();
            return;
        }

        // ライントレーサー
        private async void Trace()
        {
            float ultrasonic = brick.Ports[InputPort.Three].SIValue;
            float light = brick.Ports[InputPort.Four].SIValue;
            if (ultrasonic > 100)
            {
                if (light > 11)
                {
                    brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, -20, 10, false);
                    Debug.WriteLine("Right");
                }
                else
                {
                    brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, -20, 10, false);
                    Debug.WriteLine("Left");
                }
                await brick.BatchCommand.SendCommandAsync();
            }
        }

    }
}
