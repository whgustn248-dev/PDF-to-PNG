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
