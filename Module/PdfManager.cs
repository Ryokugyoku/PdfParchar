using System.Collections.Generic;
using System.IO;
using iTextSharp.text.pdf;
using iTextSharp.text;

namespace PdfParchar.Module
{
    public class PdfManager
    {
        string SourcePdfPath, OutputDirectory;
        PdfReader Reader;
        int BookmarkIndex = 1;

        public PdfManager(string sourcePdfPath, string outputDirectory)
        {
            this.SourcePdfPath = sourcePdfPath;
            this.OutputDirectory = outputDirectory;
            Reader = new PdfReader(sourcePdfPath);
            IList<Dictionary<string, object>> bookmarks = SimpleBookmark.GetBookmark(Reader);
            if (bookmarks != null)
            {
                ProcessBookmarks(bookmarks, Reader, outputDirectory, null, "");
            }
            Reader.Close();
        }


        private void ProcessBookmarks(IList<Dictionary<string, object>> bookmarks, PdfReader reader, string outputDirectory, string parentTitle, string parentNumber, bool isRoot = true)
        {
            int localIndex = 1;
            for (int i = 0; i < bookmarks.Count; i++)
            {
                Dictionary<string, object> bookmark = bookmarks[i];
                string title = bookmark["Title"] as string;
                string fullTitle = parentTitle == null ? title : $"{parentTitle} - {title}";
                string bookmarkNumber = string.IsNullOrEmpty(parentNumber) ? $"{BookmarkIndex++}" : $"{parentNumber}-{localIndex++}";

                string bookmarkDirectory = Path.Combine(outputDirectory, $"{bookmarkNumber} {title}");
                if (isRoot) // ルートブックマークの場合のみディレクトリを作成
                {
                    Directory.CreateDirectory(bookmarkDirectory);
                }

                // サブブックマークの有無に応じてendPageを計算
                int endPage;
                if (bookmark.ContainsKey("Kids") && (bookmark["Kids"] as IList<Dictionary<string, object>>).Count > 0)
                {
                    // サブブックマークが存在する場合、次のブックマークのページの1つ前まで（またはドキュメントの最後のページまで）
                    endPage = (i + 1 < bookmarks.Count) ? ExtractPageNumber(bookmarks[i + 1]["Page"] as string) - 1 : reader.NumberOfPages;
                }
                else
                {
                    // サブブックマークが存在しない場合、そのブックマークが設定されたページのみ
                    endPage = ExtractPageNumber(bookmark["Page"] as string);
                }

               　// SplitAndSavePdfメソッドの呼び出しにサブブックマークの情報を追加
                SplitAndSavePdf(reader, bookmark, isRoot ? bookmarkDirectory : outputDirectory, fullTitle, bookmarkNumber, endPage, bookmark.ContainsKey("Kids") ? bookmark["Kids"] as IList<Dictionary<string, object>> : null);

                if (bookmark.ContainsKey("Kids"))
                {
                    IList<Dictionary<string, object>> subBookmarks = bookmark["Kids"] as IList<Dictionary<string, object>>;
                    ProcessBookmarks(subBookmarks, reader, bookmarkDirectory, fullTitle, bookmarkNumber, false);
                }
            }
        }

private void SplitAndSavePdf(PdfReader reader, Dictionary<string, object> bookmark, string outputDirectory, string title, string bookmarkNumber, int endPage, IList<Dictionary<string, object>> subBookmarks = null)
{
    var pageRange = bookmark["Page"] as string;
    int startPage = ExtractPageNumber(pageRange);
    int lastPage = endPage < startPage ? reader.NumberOfPages : endPage;

    using (MemoryStream memoryStream = new MemoryStream())
    {
        using (Document document = new Document())
        {
            using (PdfCopy copy = new PdfCopy(document, memoryStream))
            {
                document.Open();
                for (int page = startPage; page <= lastPage; page++)
                {
                    copy.AddPage(copy.GetImportedPage(reader, page));
                }
            }
        }

        if (subBookmarks != null && subBookmarks.Count > 0)
        {
            // サブブックマークのページ番号を更新
            var updatedSubBookmarks = new List<Dictionary<string, object>>();
            foreach (var subBookmark in subBookmarks)
            {
                var subPageRange = subBookmark["Page"] as string;
                int subStartPage = ExtractPageNumber(subPageRange);
                int newPageNumber = subStartPage - startPage + 1;
                subBookmark["Page"] = $"{newPageNumber} " + subPageRange.Substring(subPageRange.IndexOf(' '));
                updatedSubBookmarks.Add(subBookmark);
            }

            using (PdfReader tempReader = new PdfReader(memoryStream.ToArray()))
            {
                using (FileStream fs = new FileStream(Path.Combine(outputDirectory, $"{bookmarkNumber} {title}.pdf"), FileMode.Create))
                {
                    using (PdfStamper stamper = new PdfStamper(tempReader, fs))
                    {
                        stamper.Outlines = updatedSubBookmarks;
                    }
                }
            }
        }
        else
        {
            File.WriteAllBytes(Path.Combine(outputDirectory, $"{bookmarkNumber} {title}.pdf"), memoryStream.ToArray());
        }
    }
}
       private int ExtractPageNumber(string pageRange)
        {
            string[] tokens = pageRange.Split(' ');
            return int.Parse(tokens[0]);
        }
    }
}