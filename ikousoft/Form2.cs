using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using static System.Windows.Forms.VisualStyles.VisualStyleElement;

namespace ikousoft
{
    public partial class Form2 : Form
    {
        public Form2()
        {
            InitializeComponent();
        }

        private void Form2_Load(object sender, EventArgs e)
        {
            //コンポボックスのイベント
            comboBox1.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox2.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox3.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            comboBox4.SelectedIndexChanged += ComboBox_SelectedIndexChanged;
            // 先にドロップダウンのスタイルを統一
            comboBox1.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox2.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox3.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBox4.DropDownStyle = ComboBoxStyle.DropDownList;

            // ラジオボタンの初期状態を設定（アカウントフォルダモードを標準にする）
            radioButton1.Checked = true;
            radioButton2.Checked = false;
            List<string> drivesList = new List<string>();
            foreach (string drive in Directory.GetLogicalDrives())
            {
                try
                {
                    if (Directory.Exists(drive)) drivesList.Add(drive);
                }
                catch { }
            }

            // 1. ドライブ一覧の取得
            string[] drives = DriveInfo.GetDrives().Select(d => d.Name).ToArray();
            comboBox3.Items.AddRange(drives);
            comboBox4.Items.AddRange(drives);

            if (comboBox3.Items.Count > 0) comboBox3.SelectedIndex = 0;
            if (comboBox4.Items.Count > 1) comboBox4.SelectedIndex = 1;
            else if (comboBox4.Items.Count > 0) comboBox4.SelectedIndex = 0;

            // 2. C:\Users 直下のアカウントフォルダを取得（アクセス拒否を回避する安全な方法）
            string targetPath = @"C:\Users";
            try
            {
                if (Directory.Exists(targetPath))
                {
                    // GetDirectories(TopDirectoryOnly) は1つアクセス拒否があると全滅するため、
                    // DirectoryInfo を使って1つずつ安全に精査します
                    var di = new DirectoryInfo(targetPath);
                    List<string> folderNames = new List<string>();

                    foreach (var subDir in di.GetDirectories("*", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            // 隠し属性・システム属性、および不要な特殊フォルダを除外
                            if ((subDir.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;

                            string nameLower = subDir.Name.ToLower();
                            if (nameLower == "public" || nameLower == "all users" ||
                                nameLower == "default" || nameLower == "default user") continue;

                            folderNames.Add(subDir.Name);
                        }
                        catch { /* アクセス権のないフォルダは単にスキップ */ }
                    }

                    var orderedFolders = folderNames.OrderBy(name => name).ToArray();
                    comboBox1.Items.AddRange(orderedFolders);
                    comboBox2.Items.AddRange(orderedFolders);

                    if (comboBox1.Items.Count > 0) comboBox1.SelectedIndex = 0;
                    if (comboBox2.Items.Count > 0) comboBox2.SelectedIndex = 0;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"フォルダ取得中にエラーが発生しました: {ex.Message}", "エラー");
            }

            // モードの切り替えイベントを手動で1回実行してコントロールの有効・無効を正しく確定させる
            ToggleModeControls();
        }
        private void ComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            ValidateSelection();
        }
        private void ValidateSelection()
        {
            bool isDriveMode = radioButton2.Checked;

            if(isDriveMode)
            {
                string drive1 = comboBox3.SelectedItem?.ToString();
                string drive2 = comboBox4.SelectedItem?.ToString();

                button1.Enabled = (!string.IsNullOrEmpty(drive1) && !string.IsNullOrEmpty(drive2) && drive1 != drive2);
            }else
            {
                string folder1 = comboBox1.SelectedItem?.ToString();
                string folder2 = comboBox2.SelectedItem?.ToString();

                button1.Enabled = (!string.IsNullOrEmpty(folder1) && !string.IsNullOrEmpty(folder2) && folder1 != folder2);
            }
        }




        // ラジオボタンの選択が変わった時の共通処理
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            ToggleModeControls();
        }

        private void radioButton2_CheckedChanged(object sender, EventArgs e)
        {
            ToggleModeControls();
        }

        // 選択されたモードに応じて、コンボボックスの有効・無効を「正しく」切り替えるメソッド
        private void ToggleModeControls()
        {
            bool isDriveMode = radioButton2.Checked;

            // ドライブモードなら、ドライブ用コンボを有効、フォルダ用コンボを無効に
            comboBox3.Enabled = isDriveMode;
            comboBox4.Enabled = isDriveMode;

            comboBox1.Enabled = !isDriveMode;
            comboBox2.Enabled = !isDriveMode;

            ValidateSelection();
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            Form1 form1 = this.Owner as Form1;
            if (form1 == null)
            {
                MessageBox.Show("メイン画面（Form1）の参照が見つかりません。", "エラー");
                return;
            }

            button1.Enabled = false;
            Cursor.Current = Cursors.WaitCursor;
            form1.ClearListViews();

            bool isDriveMode = radioButton2.Checked;
            string selDrive1 = comboBox3.SelectedItem?.ToString();
            string selDrive2 = comboBox4.SelectedItem?.ToString();
            string selFolder1 = comboBox1.SelectedItem?.ToString();
            string selFolder2 = comboBox2.SelectedItem?.ToString();

            List<ListViewItem> list1Items = new List<ListViewItem>();
            List<ListViewItem> list2Items = new List<ListViewItem>();

            // 送信する最終パスを事前に確定させる（null参照防止）
            string finalPath1 = isDriveMode ? selDrive1 : (string.IsNullOrEmpty(selFolder1) ? "" : Path.Combine(@"C:\Users", selFolder1));
            string finalPath2 = isDriveMode ? selDrive2 : (string.IsNullOrEmpty(selFolder2) ? "" : Path.Combine(@"C:\Users", selFolder2));

            try
            {
                await Task.Run(() =>
                {
                    // 元の引数2つの呼び出し形式に戻しました
                    if (!string.IsNullOrEmpty(finalPath1) && Directory.Exists(finalPath1))
                    {
                        GetTargetDirectoryItems(finalPath1, list1Items);
                    }
                    if (!string.IsNullOrEmpty(finalPath2) && Directory.Exists(finalPath2))
                    {
                        GetTargetDirectoryItems(finalPath2, list2Items);
                    }
                });

                // Form1のListViewにデータを引き渡す
                form1.UpdateListView1(list1Items, finalPath1);
                form1.UpdateListView2(list2Items, finalPath2);

                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"スキャン中にエラーが発生しました:\n{ex.Message}", "エラー");
            }
            finally
            {
                button1.Enabled = true;
                Cursor.Current = Cursors.Default;
            }
        }

        // メソッドの定義も元の引数2つの状態に戻しました
        private void GetTargetDirectoryItems(string basePath, List<ListViewItem> resultList)
        {
            try
            {
                var dirInfo = new DirectoryInfo(basePath);

                // 1. フォルダ一覧の安全な取得
                try
                {
                    foreach (var d in dirInfo.GetDirectories("*", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            if ((d.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;

                            ListViewItem item = new ListViewItem(d.Name); // 1列目: 名前
                            item.SubItems.Add(d.FullName);               // 2列目: フルパス
                            item.ForeColor = Color.Blue;                  // フォルダは青文字
                            resultList.Add(item);
                        }
                        catch { /* 個別のアクセス拒否はスキップ */ }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    ListViewItem errItem = new ListViewItem("⚠️ [アクセス拒否] 管理者権限が必要です");
                    errItem.ForeColor = Color.Red;
                    errItem.SubItems.Add(basePath);
                    resultList.Add(errItem);
                    return;
                }

                // 2. ファイル一覧の安全な取得
                try
                {
                    foreach (var f in dirInfo.GetFiles("*", SearchOption.TopDirectoryOnly))
                    {
                        try
                        {
                            if ((f.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;

                            ListViewItem item = new ListViewItem(f.Name); // 1列目: 名前
                            item.SubItems.Add(f.FullName);               // 2列目: フルパス
                            resultList.Add(item);
                        }
                        catch { /* 個別のアクセス拒否はスキップ */ }
                    }
                }
                catch { /* ファイル一覧取得自体の失敗はスキップ */ }
            }
            catch { /* ディレクトリ存在チェック等の失敗 */ }
        }
    }
}