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
        // 時間計測に使うタイマー
        private DispatcherTimer timer;
        // 実行時間を保持する
        private Int32 time = 0;
        // 実行間隔[ミリ秒]
        private double tick = 10.0;
        // 走っているかどうか
        private bool fRun = false;
        // 方向（表示用）
        private String direction = "None";

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(tick);
            timer.Tick += new EventHandler(TickTimer);
        }

        // ウィンドウが開かれた時の処理
        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            Dispatcher.Invoke(new Action(async () =>
			{
                // EV3への接続
                //brick = new Brick(new UsbCommunication()); // USBで接続
                brick = new Brick(new BluetoothCommunication("COM3")); // 青歯で接続
                try
                {
                    await brick.ConnectAsync();
                }
                // 接続失敗の場合
                catch (Exception ex)
                {
                    MessageBox.Show(ex.ToString());
                }
            }));

            // EV3の機体状態（センサの値など）が変更された時に呼び出されるメソッド
            brick.BrickChanged += OnBrickChanged;
        }

        // ウィンドウが閉じられた時の処理
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
            //Debug.WriteLine(e.Ports.ToString());
        }

        // ボタンが押された時
        private void GoForward_Click(object sender, RoutedEventArgs e)
        {
            //brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, -40, 10000, false);
            //brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, -40, 10000, false);
            //await brick.BatchCommand.SendCommandAsync();
            if (!fRun)
            {
                time = 0;
                timer.Start();
                fRun = true;
                GoForward.Content = "Stop";
            }
            else
            {
                timer.Stop();
                fRun = false;
                GoForward.Content = "Start";
            }
        }

        // 指定されたtick[ミリ秒]間隔で実行されるイベント
        private void TickTimer(object sender, EventArgs e)
        {
            // ラベルに表示されるテキストの更新
            SensorValue.Content = "Timer: " + time.ToString() + "\n" +
                "Touch: " + brick.Ports[InputPort.One].SIValue.ToString() + "\n" +
                "Color: " + brick.Ports[InputPort.Two].RawValue.ToString() + "\n" +
                "NxtLight: " + brick.Ports[InputPort.Three].RawValue.ToString() + "\n" +
                "Ultrasonic: " + brick.Ports[InputPort.Four].RawValue.ToString() + "\n" +
                "Direction: " + direction;
            // 経過時間を加算
            time++;
            // ライントレースする
            Trace();
            return;
        }

        // ライントレーサー
        private async void Trace()
        {
            float ultrasonic = brick.Ports[InputPort.Four].SIValue;
            float light = brick.Ports[InputPort.Two].SIValue;
            if (ultrasonic > 50 || !(ultrasonic < 0))
            {
                if (light > 10)
                {
                    brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, 60, 10, false);
                    brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, 30, 10, false);
                    //Debug.WriteLine("Right");
                    direction = "Right";
                }
                else //if (light < 6)
                {
                    brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, 30, 10, false);
                    brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, 60, 10, false);
                    //Debug.WriteLine("Left");
                    direction = "Left";
                }
                /*
                else
                {
                    brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, 30, 10, false);
                    brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, 30, 10, false);
                }*/
                await brick.BatchCommand.SendCommandAsync();
            }
        }

        // モーターの短時間切り替えテスト用（Trace()の引数に何か数字を入れれば作動）
        private async void Trace(int a)
        {
            if (time%2 == 0)
            {
                brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, -50, 10, false);
                //brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, 20, 10, false);
                //Debug.WriteLine("Right");
                direction = "Right";
            }
            else
            {
                //brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, 20, 10, false);
                brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, -50, 10, false);
                //Debug.WriteLine("Left");
                direction = "Left";
            }
            await brick.BatchCommand.SendCommandAsync();
        }
    }
}
