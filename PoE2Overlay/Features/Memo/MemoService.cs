using System;
using System.IO;
using Serilog;

namespace PoE2Overlay.Features.Memo
{
    public class MemoService
    {
        private static readonly string MemoDir =
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PoE2Overlay");

        private static readonly string MemoPath = Path.Combine(MemoDir, "memo.txt");

        public string Load()
        {
            try
            {
                if (File.Exists(MemoPath))
                    return File.ReadAllText(MemoPath);
            }
            catch (Exception ex) { Log.Warning(ex, "Memo load failed"); }
            return string.Empty;
        }

        public bool Save(string content)
        {
            try
            {
                Directory.CreateDirectory(MemoDir);
                File.WriteAllText(MemoPath, content);
                return true;
            }
            catch (Exception ex)
            {
                Log.Warning(ex, "Memo save failed");
                return false;
            }
        }
    }
}
