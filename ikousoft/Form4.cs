using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics; // プロセスを起動するために追加
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ikousoft
{
    public partial class Form4 : Form
    {
        public Form4()
        {
            InitializeComponent();
        }

        // フォーム読み込み時にドライブ一覧を表示
        private void Form4_Load(object sender, EventArgs e)
        {
            comboBox1.Items.Clear();

            foreach (DriveInfo drive in DriveInfo.GetDrives())
            {
                comboBox1.Items.Add(drive.Name);
            }

            if (comboBox1.Items.Count > 0)
            {
                comboBox1.SelectedIndex = 0;
            }
        }

        // ボタン1をクリックしたときにチェックディスクを開始
        private void button1_Click(object sender, EventArgs e)
        {
            // ドライブが選択されているか確認
            if (comboBox1.SelectedItem == null)
            {
                MessageBox.Show("ドライブを選択してください。", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // 選択されたドライブ名を取得（例: "C:\"）
            string selectedDrive = comboBox1.SelectedItem.ToString();

            // chkdskコマンド用に末尾の「\」を削除（例: "C:\" から "C:" に変換）
            string driveLetter = selectedDrive.Replace("\\", "");

            // 実行するプロセスの設定
            ProcessStartInfo psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe"; // コマンドプロンプトを起動

            // 引数: /k を指定すると実行後に画面を閉じずに残します。
            // chkdsk ドライブ名 /f (エラーを自動修復するオプション)
            psi.Arguments = $"/k chkdsk {driveLetter} /f";

            psi.UseShellExecute = true;
            psi.Verb = "runas"; // ★管理者権限で実行するための設定（UAC画面が出ます）

            try
            {
                // チェックディスクを開始
                Process.Start(psi);
            }
            catch (Win32Exception)
            {
                // ユーザーが管理者権限の許可（はい）を押さなかった場合
                MessageBox.Show("管理者権限が許可されなかったため、実行をキャンセルしました。", "中断", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"起動エラーが発生しました: {ex.Message}", "エラー", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}
