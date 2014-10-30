﻿using System;
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
        private double tick = 100.0;
        // 走っているかどうか
        private bool fRun = false;
        // 方向（表示用）
        private String direction = "None";
        // コースの色（キャリブレーションで設定可能）
        private double colorBlack = 3;
        private double colorWhite = 77;
        private double colorMiddle;

        public MainWindow()
        {
            InitializeComponent();
            timer = new DispatcherTimer();
            timer.Interval = TimeSpan.FromMilliseconds(tick);
            timer.Tick += new EventHandler(TickTimer);
        }

        // ウィンドウが開かれた時の処理
        private async void Window_Loaded(object sender, RoutedEventArgs e)
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

            // モーターの設定
            await brick.DirectCommand.StopMotorAsync(OutputPort.All, false);

            // ウィンドウタイトルにEV3のファームウェアバージョンを表示
            MainWindow1.Title = await brick.DirectCommand.GetFirmwareVersionAsync();

            // 起動音
            await brick.DirectCommand.PlayToneAsync(1, 987, 50);
            System.Threading.Thread.Sleep(50);
            await brick.DirectCommand.PlayToneAsync(1, 1319, 200);

            // 中間色設定、表示
            colorMiddle = colorBlack + ((colorWhite - colorBlack) / 2);
            CalibrateValueLabel.Content = "BL: " + colorBlack.ToString() + " HW: " + colorWhite.ToString() + " MD: " + ((int)colorMiddle).ToString();
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
            if (!fRun && brick != null)
            {
                // ラベルに表示されるテキストの更新
                SensorValue.Content = "Timer: " + time.ToString() + "\n" +
                    "Touch: " + brick.Ports[InputPort.One].SIValue.ToString() + "\n" +
                    "Color: " + brick.Ports[InputPort.Two].RawValue.ToString() + "\n" +
                    "NxtLight: " + brick.Ports[InputPort.Three].RawValue.ToString() + "\n" +
                    "Ultrasonic: " + brick.Ports[InputPort.Four].RawValue.ToString() + "\n" +
                    "Direction: " + direction;
            }
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
                if (light > colorMiddle)
                {
                    brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.A, 40, 100, false);
                    brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.B, 20, 100, false);
                    //await brick.DirectCommand.TurnMotorAtSpeedForTimeAsync(OutputPort.B, 20, 10, false);
                    //Debug.WriteLine("Right");
                    direction = "Right";
                }
                else //if (light < 6)
                {
                    brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.A, 20, 100, false);
                    brick.BatchCommand.TurnMotorAtPowerForTime(OutputPort.B, 40, 100, false);
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
                brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, 50, 10, false);
                //brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, 20, 10, false);
                //Debug.WriteLine("Right");
                direction = "Right";
            }
            else
            {
                //brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.A, 20, 10, false);
                brick.BatchCommand.TurnMotorAtSpeedForTime(OutputPort.B, 50, 10, false);
                //Debug.WriteLine("Left");
                direction = "Left";
            }
            await brick.BatchCommand.SendCommandAsync();
        }

        private void CalibrateButton_Click(object sender, RoutedEventArgs e)
        {
            // 値取得、ラベルへの反映
            MessageBox.Show("黒色にセンサーを近づけて、OKをクリックしてください。");
            colorBlack = brick.Ports[InputPort.Two].SIValue;
            MessageBox.Show("白色にセンサーを近づけて、OKをクリックしてください。");
            colorWhite = brick.Ports[InputPort.Two].SIValue;

            // 中間値算出
            colorMiddle = colorBlack + ((colorWhite - colorBlack) / 2);
            // 表示
            CalibrateValueLabel.Content = "BL: " + colorBlack.ToString() + " HW: " + colorWhite.ToString() + " MD: " + ((int)colorMiddle).ToString();
        }
    }
}
