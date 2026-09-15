using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ikousoft
{
    public partial class Form3 : Form
    {
        private Stopwatch _stopwatch = new Stopwatch();
        public Form3()
        {
            InitializeComponent();

            // 【修正】CenterOwner から CenterParent に変更しました
            this.StartPosition = FormStartPosition.CenterParent;

            // 移行中にユーザーがバツボタンで閉じられないようにする（任意）
            this.ControlBox = false;
        }



        private void Form3_Load(object sender, EventArgs e)
        {
            // 初期状態の設定（コントロール名が異なる場合はデザイナー側の名前に合わせてください）
            if (progressBar1 != null)
            {
                progressBar1.Minimum = 0;
                progressBar1.Maximum = 100;
                progressBar1.Value = 0;
            }
            if (label1 != null)
            {
                label1.Text = "準備中…";
            }
            if (label2 != null)
            {
                label2.Text = "残り時間　計算中…";
            }
            _stopwatch.Restart();

        }
        protected override void WndProc(ref Message m)
        {
            const int WM_SYSCOMMAND = 0x0112;
            const int SC_MAXIMIZE = 0xF030;
            const int SC_DOUBLECLICK = 0xF122; // タイトルバーのダブルクリック

            // 最大化コマンド、またはタイトルバーダブルクリックによる最大化命令を無視する
            if (m.Msg == WM_SYSCOMMAND)
            {
                int command = m.WParam.ToInt32() & 0xFFF0;
                if (command == SC_MAXIMIZE || command == SC_DOUBLECLICK)
                {
                    return; // 処理を中断して最大化させない
                }
            }

            base.WndProc(ref m);
        }
        /// <summary>
        /// Form1から呼ばれる進捗更新用のメソッド
        /// </summary>
        /// <param name="status">現在処理中のファイル名やステータス</param>
        /// <param name="percent">進捗率 (0～100)</param>
        public void UpdateStatus(string status, int percent)
        {
            // 念のため別スレッドからの安全なUI操作（Invoke）を保証
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => UpdateStatus(status, percent)));
                return;
            }

            // 1. 進捗テキストの更新（ラベル）
            if (label1 != null && !string.IsNullOrEmpty(status))
            {
                label1.Text = status;
            }

            // 2. プログレスバーの更新
            if (progressBar1 != null)
            {
                // 値が0～100の範囲内に収まるようにガード
                if (percent < 0) percent = 0;
                if (percent > 100) percent = 100;

                progressBar1.Value = percent;
            }
            UpdateRemainingTime(percent);
        }
        private void UpdateRemainingTime(int percent)
        {
            if (label2 == null) return;

            // 進捗が0の時、または100(完了)の時は計算をスキップ
            if (percent <= 0)
            {
                label2.Text = "残り時間: 計算中...";
                return;
            }
            if (percent >= 100)
            {
                label2.Text = "処理が完了しました。";
                _stopwatch.Stop();
                return;
            }

            // 経過時間を取得
            double elapsedMilliseconds = _stopwatch.ElapsedMilliseconds;

            // 1%あたりにかかった時間 × 残りの% で残りミリ秒を計算
            double remainingMilliseconds = (elapsedMilliseconds / percent) * (100 - percent);

            // TimeSpanに変換して、見やすいフォーマット（○分○秒）にする
            TimeSpan remainingTime = TimeSpan.FromMilliseconds(remainingMilliseconds);

            // 表示形式の整形（例:「残り時間: 01分25秒」）
            // 時、分、秒を2桁固定で表示したい場合は、以下のように記述します
            if (remainingTime.TotalHours >= 1)
            {
                label2.Text = string.Format("残り時間: 約 {0:00}時間{1:00}分", (int)remainingTime.TotalHours, remainingTime.Minutes);
            }
            else
            {
                label2.Text = string.Format("残り時間: 約 {0:00}分{1:00}秒", remainingTime.Minutes, remainingTime.Seconds);
            }
        }
    }
}