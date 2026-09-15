using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ikousoft
{
    public partial class title : Form
    {
        public title()
        {
            InitializeComponent();
        }

        private void button1_Click(object sender, EventArgs e)
        {
            Form1 f1 = new Form1();
            f1.Show();
            this.Hide();
        }

        private void button2_Click(object sender, EventArgs e)
        {
            // 32ビットプロセスで起動された場合のリダイレクト対策（64bit Windowsの場合）
            string system32Path = Environment.GetFolderPath(Environment.SpecialFolder.System);
            if (Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess)
            {
                system32Path = Path.Combine(Directory.GetParent(system32Path).FullName, "sysnative");
            }

            string recoveryDrivePath = Path.Combine(system32Path, "RecoveryDrive.exe");

            if (File.Exists(recoveryDrivePath))
            {
                try
                {
                    ProcessStartInfo psi = new ProcessStartInfo(recoveryDrivePath)
                    {
                        UseShellExecute = true,
                        Verb = "runas" // 管理者権限で実行
                    };

                    Process.Start(psi);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("エラー: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("RecoveryDrive.exeが見つかりません。");
            }
        }

        private void button3_Click(object sender, EventArgs e)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo();
                psi.FileName = "shutdown.exe";

                // /r:再起動, /o:次回起動時に回復環境を開く, /f:アプリ強制終了, /t 0:待ち時間0秒
                psi.Arguments = "/r /o /f /t 0";

                psi.UseShellExecute = false;
                psi.CreateNoWindow = true;
                psi.Verb = "runas"; // 管理者権限を要求（必要に応じて）

                Console.WriteLine("Windows回復環境（WinRE）に移行するため再起動します...");
                Process.Start(psi);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"エラーが発生しました: {ex.Message}");
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            Form1 f1 = new Form1();
            f1.Show();
            this.Hide();
        }

        // async を付与して非同期メソッドにする
        private async void button4_Click(object sender, EventArgs e)
        {
            // 実行中はボタンを連打できないように無効化
            button4.Enabled = false;

            MessageBox.Show("システム修復（sfc /scannow）を開始します。\n完了までこのまましばらくお待ちください。",
                            "修復開始", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // バックグラウンドで修復コマンドを実行し、終わるまでここで待機（待っている間も画面はフリーズしません）
            await Task.Run(() => RunCommand("sfc", "/scannow"));

            // 【完了のお知らせ】すべての処理が終わったらメッセージボックスを表示
            MessageBox.Show("Windowsのシステム修復（SFC）が正常に完了しました！",
                            "完了のお知らせ", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // ボタンを元の状態に戻す
            button4.Enabled = true;
        }

        private static void RunCommand(string fileName, string arguments)
        {
            try
            {
                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = arguments,
                    UseShellExecute = true,  // 管理者権限（UAC）を要求するために true に設定
                    Verb = "runas",          // 管理者として実行
                    CreateNoWindow = false   // ユーザーに進捗（％表示）が見えるように黒い画面を表示
                };

                using (Process process = Process.Start(psi))
                {
                    // コマンドプロンプトが閉じる（修復が完了する）まで待機
                    process?.WaitForExit();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"エラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button5_Click(object sender, EventArgs e)
        {
            try
            {
                // cmd.exe 自体のパスを取得（SysNative問題の影響を受けないように自動判別）
                string system32Path = Environment.Is64BitOperatingSystem && !Environment.Is64BitProcess
                    ? Path.Combine(Environment.GetEnvironmentVariable("Windir"), "SysNative")
                    : Path.Combine(Environment.GetEnvironmentVariable("Windir"), "System32");

                ProcessStartInfo psi = new ProcessStartInfo
                {
                    FileName = Path.Combine(system32Path, "cmd.exe"),

                    // 【修正ポイント】SysNativeの直接指定をやめ、%SystemRoot%\System32\sdclt.exe としてcmdに渡します
                    Arguments = "/c start \"\" \"%SystemRoot%\\System32\\sdclt.exe\" /BLBBACKUPWIZARD",

                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden, // 黒い画面を一瞬だけ隠す
                    Verb = "runas" // 必ず管理者権限で実行
                };

                using (Process process = Process.Start(psi))
                { 
                }
            }
            catch (Win32Exception ex)
            {
                MessageBox.Show($"起動がキャンセルされたか、エラーが発生しました。\n{ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"予期せぬエラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void button6_Click(object sender, EventArgs e)
        {
            Form4 f4 = new Form4();
            f4.Show();
        }
    }
}
