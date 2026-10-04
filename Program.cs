using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using PDFtoImage;
using SkiaSharp;

namespace PdfToPng;

internal static class Program
{
    [STAThread]
    static void Main()
    {
        ApplicationConfiguration.Initialize();
        Application.Run(new MainForm());
    }
}

public sealed class MainForm : Form
{
    private readonly ListBox fileList = new();
    private readonly ComboBox dpiBox = new();
    private readonly ProgressBar progress = new();
    private readonly Label status = new();
    private readonly Button convertButton = new();
    private readonly List<string> files = new();

    public MainForm()
    {
        Text = "PDF → PNG 변환기";
        Width = 720;
        Height = 560;
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(720, 560);

        Controls.Add(new Label
        {
            Text = "PDF → PNG 변환기",
            Font = new Font("맑은 고딕", 20, FontStyle.Bold),
            AutoSize = true,
            Location = new Point(25, 20)
        });

        Controls.Add(new Label
        {
            Text = "PDF를 페이지별 PNG 이미지로 변환합니다.",
            Font = new Font("맑은 고딕", 10),
            AutoSize = true,
            Location = new Point(28, 62)
        });

        var selectButton = new Button
        {
            Text = "PDF 파일 선택",
            Width = 150,
            Height = 38,
            Location = new Point(28, 95)
        };
        selectButton.Click += (_, _) => SelectFiles();
        Controls.Add(selectButton);

        var clearButton = new Button
        {
            Text = "목록 비우기",
            Width = 150,
            Height = 38,
            Location = new Point(188, 95)
        };
        clearButton.Click += (_, _) => ClearFiles();
        Controls.Add(clearButton);

        Controls.Add(new Label
        {
            Text = "여기에 PDF를 끌어놓을 수도 있습니다.",
            AutoSize = true,
            ForeColor = Color.DimGray,
            Location = new Point(355, 108)
        });

        fileList.Location = new Point(28, 145);
        fileList.Size = new Size(640, 190);
        fileList.AllowDrop = true;
        fileList.HorizontalScrollbar = true;
        fileList.DragEnter += FileList_DragEnter;
        fileList.DragDrop += FileList_DragDrop;
        Controls.Add(fileList);

        Controls.Add(new Label
        {
            Text = "해상도:",
            AutoSize = true,
            Location = new Point(28, 355)
        });

        dpiBox.Location = new Point(90, 351);
        dpiBox.Width = 100;
        dpiBox.DropDownStyle = ComboBoxStyle.DropDownList;
        dpiBox.Items.AddRange(new object[] { "150", "200", "300", "400", "600" });
        dpiBox.SelectedItem = "300";
        Controls.Add(dpiBox);

        Controls.Add(new Label
        {
            Text = "DPI   (일반 문서는 300 DPI 권장)",
            AutoSize = true,
            Location = new Point(200, 355)
        });

        progress.Location = new Point(28, 395);
        progress.Size = new Size(640, 24);
        Controls.Add(progress);

        status.Text = "PDF 파일을 선택해주세요.";
        status.AutoSize = true;
        status.Location = new Point(28, 430);
        Controls.Add(status);

        convertButton.Text = "PNG로 변환 시작";
        convertButton.Font = new Font("맑은 고딕", 11, FontStyle.Bold);
        convertButton.Width = 230;
        convertButton.Height = 45;
        convertButton.Location = new Point(28, 460);
        convertButton.Click += async (_, _) => await ConvertAsync();
        Controls.Add(convertButton);

        Controls.Add(new Label
        {
            Text = "변환된 파일은 각 PDF와 같은 위치의 '[PDF이름]_PNG' 폴더에 저장됩니다.",
            AutoSize = true,
            Location = new Point(275, 475),
            ForeColor = Color.DimGray
        });
    }

    private void SelectFiles()
    {
        using var dialog = new OpenFileDialog
        {
            Title = "PDF 파일 선택",
            Filter = "PDF 파일 (*.pdf)|*.pdf",
            Multiselect = true
        };

        if (dialog.ShowDialog() == DialogResult.OK)
            AddFiles(dialog.FileNames);
    }

    private void ClearFiles()
    {
        files.Clear();
        fileList.Items.Clear();
        progress.Value = 0;
        status.Text = "PDF 파일을 선택해주세요.";
    }

    private void FileList_DragEnter(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            e.Effect = DragDropEffects.Copy;
    }

    private void FileList_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] paths)
            AddFiles(paths);
    }

    private void AddFiles(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (File.Exists(path) &&
                string.Equals(Path.GetExtension(path), ".pdf", StringComparison.OrdinalIgnoreCase) &&
                !files.Contains(path, StringComparer.OrdinalIgnoreCase))
            {
                files.Add(path);
                fileList.Items.Add(path);
            }
        }

        status.Text = $"{files.Count}개 PDF 선택됨";
    }

 private async Task ConvertAsync()
    {
        if (files.Count == 0)
        {
            MessageBox.Show("PDF 파일을 먼저 선택해주세요.", "알림",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        int dpi = int.Parse(dpiBox.SelectedItem?.ToString() ?? "300");
        convertButton.Enabled = false;
        progress.Value = 0;

        try
        {
            // PDF마다 별도의 스트림을 사용해 페이지 수를 먼저 확인합니다.
            // 이후 렌더링에서도 새 스트림을 열어 스트림 위치 문제를 피합니다.
            int totalPages = 0;

            foreach (var pdf in files)
            {
                using var countStream = File.OpenRead(pdf);
                totalPages += Conversion.GetPageCount(countStream);
            }

            int done = 0;

            foreach (var pdf in files.ToArray())
            {
                string outputDir = Path.Combine(
                    Path.GetDirectoryName(pdf)!,
                    Path.GetFileNameWithoutExtension(pdf) + "_PNG");

                Directory.CreateDirectory(outputDir);

                var options = new RenderOptions(Dpi: dpi);

                // GetPageCount에 사용한 스트림과 다른 새 스트림을 사용합니다.
                using var renderStream = File.OpenRead(pdf);

                int pageNumber = 0;

                foreach (SKBitmap bitmap in Conversion.ToImages(
                    renderStream,
                    leaveOpen: false,
                    password: null,
                    options: options))
                {
                    using (bitmap)
                    {
                        pageNumber++;

                        string outputPath = Path.Combine(
                            outputDir,
                            $"{pageNumber:0000}.png");

                     using SKData? data =
                            bitmap.Encode(SKEncodedImageFormat.Png, 100);

                        if (data == null)
                            throw new InvalidOperationException(
                                $"PNG 인코딩에 실패했습니다: {outputPath}");

                        using FileStream output = File.Create(outputPath);
                        data.SaveTo(output);
                    }

                    done++;
                    int percent = totalPages == 0
                        ? 100
                        : done * 100 / totalPages;

 progress.Value = Math.Min(percent, 100);
                    status.Text = $"변환 중... {done} / {totalPages} 페이지";
                    Application.DoEvents();
                }
            }

            progress.Value = 100;
            status.Text = "변환 완료!";

            MessageBox.Show(
                "모든 PDF 변환이 완료되었습니다.\n\n" +
                "각 PDF와 같은 위치에 '[PDF이름]_PNG' 폴더가 생성되었습니다.",
                "변환 완료",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                "변환 중 오류가 발생했습니다.\n\n" + ex.Message,
                "변환 오류",
                MessageBoxButtons.OK,
                MessageBoxIcon.Error);

            status.Text = "변환 실패";
        }
        finally
        {
            convertButton.Enabled = true;
        }
    }
}
