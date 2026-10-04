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
