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
using Microsoft.VisualBasic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ikousoft
{
    public partial class Form1 : Form
    {
        // ベースルートパスを保持する変数
        private string currentPathLV1 = "";
        private string currentPathLV2 = "";
        private string currentOpenPathLV1 = "";
        private string currentOpenPathLV2 = "";

        private ImageList systemImageList = new ImageList();

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;     // アイコンを取得
        private const uint SHGFI_SMALLICON = 0x000000001; // 小さいアイコン(16x16)を取得
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010; // ファイルを開かずに属性から取得

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SHGetFileInfo(string pszPath, uint dwFileAttributes, ref SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public Form1()
        {
            InitializeComponent();
            InitializeListViewSettings();
        }

        private void Form1_Load_1(object sender, EventArgs e)
        {
            // コンストラクタで初期化しているため、ここではボタンの状態更新のみ行う
            UpdateButtonStates();
        }

        private void InitializeListViewSettings()
        {
            listView1.View = View.Details;
            listView2.View = View.Details;

            listView1.GridLines = true;
            listView1.FullRowSelect = true;
            listView2.GridLines = true;
            listView2.FullRowSelect = true;

            listView1.CheckBoxes = true;
            listView2.CheckBoxes = true;

            systemImageList.ImageSize = new Size(16, 16);
            systemImageList.ColorDepth = ColorDepth.Depth32Bit;

            listView1.SmallImageList = systemImageList;
            listView2.SmallImageList = systemImageList;

            if (listView1.Columns.Count == 0)
            {
                listView1.Columns.Add("名前", 200);
                listView1.Columns.Add("フルパス", 400);
            }
            if (listView2.Columns.Count == 0)
            {
                listView2.Columns.Add("名前", 200);
                listView2.Columns.Add("フルパス", 400);
            }

            listView1.DoubleClick -= ListView1_DoubleClick;
            listView1.DoubleClick += ListView1_DoubleClick;
            listView2.DoubleClick -= ListView2_DoubleClick;
            listView2.DoubleClick += ListView2_DoubleClick;

            listView1.ItemCheck -= ListView1_ItemCheck;
            listView1.ItemCheck += ListView1_ItemCheck;
        }

        private string GetSystemIconKey(string path, bool isDirectory)
        {
            try
            {
                string key = isDirectory ? "dir_default" : Path.GetExtension(path).ToLower();
                if (string.IsNullOrEmpty(key)) key = "file_default";

                if (systemImageList.Images.ContainsKey(key))
                {
                    return key;
                }

                SHFILEINFO shfi = new SHFILEINFO();
                uint flags = SHGFI_ICON | SHGFI_SMALLICON;

                if (!isDirectory)
                {
                    flags |= SHGFI_USEFILEATTRIBUTES;
                }

                IntPtr hSuccess = SHGetFileInfo(path, isDirectory ? (uint)0x00000010 : (uint)0x00000020, ref shfi, (uint)Marshal.SizeOf(shfi), flags);

                if (hSuccess != IntPtr.Zero && shfi.hIcon != IntPtr.Zero)
                {
                    using (Icon icon = Icon.FromHandle(shfi.hIcon))
                    {
                        systemImageList.Images.Add(key, icon.ToBitmap());
                    }
                    DestroyIcon(shfi.hIcon);
                    return key;
                }
            }
            catch
            {
                // エラー時は処理をスルー
            }
            return "";
        }

        private void ListView1_ItemCheck(object sender, ItemCheckEventArgs e)
        {
            ListViewItem item = listView1.Items[e.Index];
            if (item.Text == "📁 [上の階層へ戻る]" || item.Text.StartsWith("⚠️"))
            {
                e.NewValue = CheckState.Unchecked;
            }
        }

        public void ClearListViews()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(ClearListViews));
                return;
            }
            listView1.Items.Clear();
            listView2.Items.Clear();
            UpdateButtonStates();
        }

        public void UpdateListView1(List<ListViewItem> items, string selectedPath)
        {
            currentPathLV1 = selectedPath;
            currentOpenPathLV1 = selectedPath;
            FillListViewData(listView1, items);
        }

        public void UpdateListView2(List<ListViewItem> items, string selectedPath)
        {
            currentPathLV2 = selectedPath;
            currentOpenPathLV2 = selectedPath;
            FillListViewData(listView2, items);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (sender is Button btn) btn.BackColor = SystemColors.Control;

            Form2 f2 = new Form2();
            f2.Owner = this;
            f2.ShowDialog();
        }

        // ボタン2：移行を実行する（弾かれない・エラー自動スキップ版）
        private async void button2_Click(object sender, EventArgs e)
        {
            if (sender is Button btn) btn.BackColor = SystemColors.Control;

            if (currentOpenPathLV1 == "LOCAL_APPS")
            {
                MessageBox.Show("アプリケーション一覧モードです。この画面での自動移行には対応していません。",
                                "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string srcFolder = currentOpenPathLV1;
            string destFolder = currentOpenPathLV2;

            if (string.IsNullOrEmpty(srcFolder) || !Directory.Exists(srcFolder))
            {
                MessageBox.Show("移行元のフォルダ（左側）が正しく選択されていないか、存在しません。", "通知", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            if (string.IsNullOrEmpty(destFolder) || !Directory.Exists(destFolder))
            {
                MessageBox.Show("移行先のフォルダ（右側）が正しく選択されていないか、存在しません。", "通知", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // チェックされた項目の取得
            var selectedList = new List<(string Name, string FullPath)>();
            foreach (ListViewItem item in listView1.CheckedItems)
            {
                if (item.Text != "📁 [上の階層へ戻る]" && !item.Text.StartsWith("⚠️"))
                {
                    selectedList.Add((item.Text, item.SubItems[1].Text));
                }
            }

            if (selectedList.Count == 0)
            {
                MessageBox.Show("左側のリストで、移行したい項目のチェックボックスにチェックを入れてください。", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string fileConfirmMsg = $"選択された {selectedList.Count} 個の項目を\n{destFolder}\nへコピーします。よろしいですか？";
            DialogResult dr = MessageBox.Show(fileConfirmMsg, "移行の確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Question);
            if (dr != DialogResult.OK) return;

            this.UseWaitCursor = true;
            button1.Enabled = false;
            button2.Enabled = false;
            button3.Enabled = false;

            Form3 progressForm = null;
            bool isTaskFinished = false;
            string currentStatusText = "準備中…";
            int currentPercent = 0;
            List<string> errorLogs = new List<string>();

            Action<string, int> reportProgress = (status, percent) =>
            {
                if (progressForm == null || progressForm.IsDisposed) return;
                this.BeginInvoke(new Action(() =>
                {
                    if (!progressForm.IsDisposed)
                    {
                        progressForm.UpdateStatus(status, percent);
                    }
                }));
            };

            // 【重要】保護されたフォルダやアクセス不可ファイルがあっても止めずにスキップする安全コピー関数
            void SafeCopyDirectory(string sourceDir, string targetDir)
            {
                try
                {
                    Directory.CreateDirectory(targetDir);
                }
                catch (Exception ex)
                {
                    errorLogs.Add($"[作成不可] {targetDir}: {ex.Message}");
                    return;
                }

                // ファイルコピー（アクセス拒否ファイルは個別スキップ）
                try
                {
                    foreach (string file in Directory.GetFiles(sourceDir))
                    {
                        try
                        {
                            string fileName = Path.GetFileName(file);
                            string destFile = Path.Combine(targetDir, fileName);
                            currentStatusText = fileName;
                            reportProgress(currentStatusText, currentPercent);

                            File.Copy(file, destFile, true);
                        }
                        catch (UnauthorizedAccessException)
                        {
                            // アクセス拒否された特殊ファイルはログに記録して次へ進む（アプリを落とさない）
                            errorLogs.Add($"[アクセス保護] {Path.GetFileName(file)}");
                        }
                        catch (Exception ex)
                        {
                            errorLogs.Add($"[コピー失敗] {Path.GetFileName(file)}: {ex.Message}");
                        }
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    errorLogs.Add($"[アクセス保護フォルダ] {sourceDir}");
                    return;
                }

                // サブフォルダコピー
                try
                {
                    foreach (string subDir in Directory.GetDirectories(sourceDir))
                    {
                        string dirName = Path.GetFileName(subDir);
                        string destSubDir = Path.Combine(targetDir, dirName);
                        SafeCopyDirectory(subDir, destSubDir);
                    }
                }
                catch (UnauthorizedAccessException)
                {
                    // 権限のないサブフォルダ検索はスキップ
                }
            }

            var copyTask = Task.Run(() =>
            {
                try
                {
                    int totalItems = selectedList.Count;
                    int processedCount = 0;

                    foreach (var item in selectedList)
                    {
                        string name = item.Name;
                        string fullPath = item.FullPath;
                        currentStatusText = name;

                        if (Directory.Exists(fullPath))
                        {
                            string targetDestDir = Path.Combine(destFolder, name);
                            SafeCopyDirectory(fullPath, targetDestDir);
                        }
                        else if (File.Exists(fullPath))
                        {
                            try
                            {
                                string targetDestFile = Path.Combine(destFolder, name);
                                File.Copy(fullPath, targetDestFile, true);
                            }
                            catch (Exception ex)
                            {
                                errorLogs.Add($"[コピー失敗] {name}: {ex.Message}");
                            }
                        }

                        processedCount++;
                        currentPercent = (int)((double)processedCount / totalItems * 100);
                        reportProgress(currentStatusText, currentPercent);
                    }
                }
                catch (Exception ex)
                {
                    errorLogs.Add($"[全体エラー] {ex.Message}");
                }
                finally
                {
                    isTaskFinished = true;
                }
            });

            await Task.Delay(500);
            if (!isTaskFinished)
            {
                progressForm = new Form3();
                progressForm.Owner = this;
                progressForm.Show();
                progressForm.UpdateStatus(currentStatusText, currentPercent);
            }

            await copyTask;

            if (progressForm != null && !progressForm.IsDisposed) progressForm.Close();

            this.UseWaitCursor = false;
            UpdateButtonStates();

            if (errorLogs.Count > 0)
            {
                MessageBox.Show($"移行処理が完了しました。\n（※一部の保護されたシステムファイル等はスキップされました：{errorLogs.Count}件）",
                                "完了（一部スキップあり）", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show("すべてのデータのコピーが完了しました！", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }

            RefreshListViewCustom(listView2, currentOpenPathLV2, currentPathLV2);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            if (currentOpenPathLV2 == "REMOTE_APPS")
            {
                MessageBox.Show("アプリケーション一覧モードではデータを削除できません。", "通知", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            List<ListViewItem> targetItems = new List<ListViewItem>();

            if (listView2.CheckedItems.Count > 0)
            {
                foreach (ListViewItem item in listView2.CheckedItems)
                {
                    if (item.Text != "📁 [上の階層へ戻る]" && !item.Text.StartsWith("⚠️"))
                    {
                        targetItems.Add(item);
                    }
                }
            }
            else if (listView2.SelectedItems.Count > 0)
            {
                foreach (ListViewItem item in listView2.SelectedItems)
                {
                    if (item.Text != "📁 [上の階層へ戻る]" && !item.Text.StartsWith("⚠️"))
                    {
                        targetItems.Add(item);
                    }
                }
            }

            if (targetItems.Count == 0)
            {
                MessageBox.Show("削除する項目にチェックを入れるか、またはマウスで選択してください。", "通知", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            string confirmMsg = $"選択された {targetItems.Count} 個の項目を完全に削除しますか？\n(ゴミ箱には入らず完全に消去されます)";
            DialogResult dr = MessageBox.Show(confirmMsg, "削除の確認", MessageBoxButtons.OKCancel, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
            if (dr != DialogResult.OK) return;

            int successCount = 0;
            int failCount = 0;
            StringBuilder errorLog = new StringBuilder();

            foreach (ListViewItem item in targetItems)
            {
                string fullPath = item.SubItems[1].Text;

                try
                {
                    if (Directory.Exists(fullPath))
                    {
                        Directory.Delete(fullPath, true);
                        successCount++;
                    }
                    else if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        successCount++;
                    }
                    else
                    {
                        failCount++;
                        errorLog.AppendLine($"- {item.Text} (パスが見つかりません)");
                    }
                }
                catch (Exception ex)
                {
                    failCount++;
                    errorLog.AppendLine($"- {item.Text} ({ex.Message})");
                }
            }

            if (failCount == 0)
            {
                MessageBox.Show($"{successCount} 個の項目を正常に削除しました。", "完了", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            else
            {
                MessageBox.Show($"{successCount} 個の削除に成功、{failCount} 個に失敗しました。\n\n【失敗詳細】\n{errorLog}", "一部削除失敗", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }

            RefreshListViewCustom(listView2, currentOpenPathLV2, currentPathLV2);
        }

        private void ListView1_DoubleClick(object sender, EventArgs e)
        {
            if (listView1.SelectedItems.Count == 0) return;
            ListViewItem selected = listView1.SelectedItems[0];
            string targetPath = selected.SubItems[1].Text;

            if (selected.Text == "📁 [上の階層へ戻る]")
            {
                DirectoryInfo parent = Directory.GetParent(currentOpenPathLV1);
                if (parent != null)
                {
                    currentOpenPathLV1 = parent.FullName;
                    RefreshListViewCustom(listView1, currentOpenPathLV1, currentPathLV1);
                }
            }
            else if (Directory.Exists(targetPath))
            {
                currentOpenPathLV1 = targetPath;
                RefreshListViewCustom(listView1, currentOpenPathLV1, currentPathLV1);
            }
        }

        private void ListView2_DoubleClick(object sender, EventArgs e)
        {
            if (listView2.SelectedItems.Count == 0) return;
            ListViewItem selected = listView2.SelectedItems[0];
            string targetPath = selected.SubItems[1].Text;

            if (selected.Text == "📁 [上の階層へ戻る]")
            {
                DirectoryInfo parent = Directory.GetParent(currentOpenPathLV2);
                if (parent != null)
                {
                    currentOpenPathLV2 = parent.FullName;
                    RefreshListViewCustom(listView2, currentOpenPathLV2, currentPathLV2);
                }
            }
            else if (Directory.Exists(targetPath))
            {
                currentOpenPathLV2 = targetPath;
                RefreshListViewCustom(listView2, currentOpenPathLV2, currentPathLV2);
            }
        }

        private void RefreshListViewCustom(ListView lv, string folderPath, string basePath)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(() => RefreshListViewCustom(lv, folderPath, basePath)));
                return;
            }

            try
            {
                List<ListViewItem> currentItems = new List<ListViewItem>();

                if (!string.Equals(folderPath.TrimEnd('\\'), basePath.TrimEnd('\\'), StringComparison.OrdinalIgnoreCase))
                {
                    ListViewItem backItem = new ListViewItem("📁 [上の階層へ戻る]");
                    backItem.SubItems.Add(folderPath);
                    backItem.ForeColor = Color.Green;
                    currentItems.Add(backItem);
                }

                if (Directory.Exists(folderPath))
                {
                    foreach (string dir in Directory.GetDirectories(folderPath))
                    {
                        var di = new DirectoryInfo(dir);
                        if ((di.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;

                        ListViewItem item = new ListViewItem(Path.GetFileName(dir));
                        item.SubItems.Add(dir);
                        item.ForeColor = Color.Blue;

                        string iconKey = GetSystemIconKey(dir, true);
                        if (!string.IsNullOrEmpty(iconKey)) item.ImageKey = iconKey;

                        currentItems.Add(item);
                    }

                    foreach (string file in Directory.GetFiles(folderPath))
                    {
                        var fi = new FileInfo(file);
                        if ((fi.Attributes & (FileAttributes.Hidden | FileAttributes.System)) != 0) continue;

                        ListViewItem item = new ListViewItem(Path.GetFileName(file));
                        item.SubItems.Add(file);

                        string iconKey = GetSystemIconKey(file, false);
                        if (!string.IsNullOrEmpty(iconKey)) item.ImageKey = iconKey;

                        currentItems.Add(item);
                    }
                }

                FillListViewData(lv, currentItems);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"フォルダの展開に失敗しました: {ex.Message}", "エラー");
            }
        }

        private void FillListViewData(ListView lv, List<ListViewItem> items)
        {
            if (lv.InvokeRequired)
            {
                lv.Invoke(new Action(() => FillListViewData(lv, items)));
                return;
            }

            lv.BeginUpdate();
            lv.Items.Clear();
            lv.View = View.Details;

            if (items != null && items.Count > 0)
            {
                List<ListViewItem> clonedItems = new List<ListViewItem>();
                foreach (var item in items)
                {
                    ListViewItem newItem = (ListViewItem)item.Clone();

                    if (string.IsNullOrEmpty(newItem.ImageKey) && newItem.SubItems.Count > 1)
                    {
                        string path = newItem.SubItems[1].Text;
                        if (Directory.Exists(path))
                        {
                            newItem.ImageKey = GetSystemIconKey(path, true);
                        }
                        else if (File.Exists(path))
                        {
                            newItem.ImageKey = GetSystemIconKey(path, false);
                        }
                    }
                    clonedItems.Add(newItem);
                }
                lv.Items.AddRange(clonedItems.ToArray());
            }
            lv.EndUpdate();

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(UpdateButtonStates));
                return;
            }

            button3.Enabled = (listView2.Items.Count > 0);
            button2.Enabled = (listView1.Items.Count > 0 && listView2.Items.Count > 0);
            button1.Enabled = true;
        }

        private void button1_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Button btn) btn.BackColor = ColorTranslator.FromHtml("#FFE4B5");
        }

        private void button2_MouseDown(object sender, MouseEventArgs e)
        {
            if (sender is Button btn) btn.BackColor = ColorTranslator.FromHtml("#FFE4B5");
        }

        private void button2_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is Button btn) btn.BackColor = SystemColors.Control;
        }

        private void button1_MouseUp(object sender, MouseEventArgs e)
        {
            if (sender is Button btn) btn.BackColor = SystemColors.Control;
        }

        private void button4_Click(object sender, EventArgs e)
        {
            title title1 = new title();
            title1.Show();
            this.Hide();
        }
    }
}