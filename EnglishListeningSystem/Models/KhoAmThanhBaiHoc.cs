using System.Globalization;

namespace EnglishListeningSystem.Models
{
    public class KhoAmThanhBaiHoc
    {
        public const long GioiHanMp3 = 50 * 1024 * 1024;
        public const long GioiHanSrt = 1024 * 1024;
        public const long GioiHanYeuCau = GioiHanMp3 + GioiHanSrt + 64 * 1024;
        public const int GioiHanTranscript = 2000;

        private readonly string _thuMucAudio;
        private readonly ILogger<KhoAmThanhBaiHoc> _logger;

        public KhoAmThanhBaiHoc(IWebHostEnvironment moiTruong, ILogger<KhoAmThanhBaiHoc> logger)
        {
            _thuMucAudio = Path.Combine(moiTruong.ContentRootPath, "Audio");
            _logger = logger;
            Directory.CreateDirectory(_thuMucAudio);
        }

        public string? TimFileMp3(string? tenFile)
        {
            if (string.IsNullOrWhiteSpace(tenFile) || tenFile.Length > 255
                || tenFile.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0
                || tenFile.Contains('/') || tenFile.Contains('\\') || tenFile.Contains(':')
                || tenFile != Path.GetFileName(tenFile)
                || !string.Equals(Path.GetExtension(tenFile), ".mp3", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            string duongDan = Path.Combine(_thuMucAudio, tenFile);
            return LaFileThuong(duongDan) ? duongDan : null;
        }

        public string? TimFileSrt(string? tenFileMp3)
        {
            string? duongDanMp3 = TimFileMp3(tenFileMp3);
            if (duongDanMp3 == null)
            {
                return null;
            }

            string duongDan = Path.ChangeExtension(duongDanMp3, ".srt");
            return LaFileThuong(duongDan) ? duongDan : null;
        }

        public async Task<(string TenFile, List<CauChep>? CacCau)> LuuTepAsync(
            IFormFile? audioFile, IFormFile? srtFile, string? tenAudioHienTai, CancellationToken huy)
        {
            KiemTraTep(audioFile, ".mp3", GioiHanMp3, "Choose an MP3 file up to 50 MB.");
            KiemTraTep(srtFile, ".srt", GioiHanSrt, "Choose an SRT file up to 1 MB.");

            string? audioHienTai = null;
            if (audioFile == null)
            {
                audioHienTai = TimFileMp3(tenAudioHienTai);
                if (audioHienTai == null)
                {
                    throw new InvalidDataException("Choose an MP3 file to pair with the subtitles.");
                }
                if (new FileInfo(audioHienTai).Length > GioiHanMp3)
                {
                    throw new InvalidDataException("Choose an MP3 file up to 50 MB.");
                }
            }

            if (LaLienKet(_thuMucAudio))
            {
                throw new IOException("The audio directory must be a regular directory.");
            }

            // Dùng tên do máy chủ sinh, không dùng tên hoặc đường dẫn từ yêu cầu.
            string tenFile = Guid.NewGuid().ToString("N") + ".mp3";
            string duongDanAudio = Path.Combine(_thuMucAudio, tenFile);
            string duongDanSrt = Path.ChangeExtension(duongDanAudio, ".srt");
            try
            {
                List<CauChep>? cacCau = null;
                if (srtFile != null)
                {
                    using (FileStream file = new FileStream(duongDanSrt, FileMode.CreateNew))
                    {
                        await srtFile.CopyToAsync(file, huy);
                    }
                    cacCau = DocFileSrt(duongDanSrt);
                }

                using Stream nguonAudio = audioFile == null ? File.OpenRead(audioHienTai!) : audioFile.OpenReadStream();
                byte[] dauFile = new byte[4];
                int soByte = await nguonAudio.ReadAtLeastAsync(dauFile, dauFile.Length, false, huy);
                bool coTheId3 = soByte == 4 && dauFile[0] == 'I' && dauFile[1] == 'D' && dauFile[2] == '3'
                    && dauFile[3] >= 2 && dauFile[3] <= 4;
                bool coKhungMp3 = soByte == 4 && dauFile[0] == 0xff && (dauFile[1] & 0xe6) == 0xe2
                    && (dauFile[1] & 0x18) != 0x08 && (dauFile[2] & 0xf0) != 0xf0 && (dauFile[2] & 0x0c) != 0x0c;
                if (!coTheId3 && !coKhungMp3)
                {
                    throw new InvalidDataException("The selected audio file is not a recognized MP3.");
                }

                using FileStream dich = new FileStream(duongDanAudio, FileMode.CreateNew);
                await dich.WriteAsync(dauFile.AsMemory(0, soByte), huy);
                await nguonAudio.CopyToAsync(dich, huy);
                return (tenFile, cacCau);
            }
            catch
            {
                XoaTepDaTaiLen(tenFile);
                throw;
            }
        }

        public void XoaTepDaTaiLen(string tenFile)
        {
            string duongDan = Path.Combine(_thuMucAudio, tenFile);
            try
            {
                File.Delete(duongDan);
                File.Delete(Path.ChangeExtension(duongDan, ".srt"));
            }
            catch (Exception loi) when (loi is IOException || loi is UnauthorizedAccessException)
            {
                _logger.LogWarning(loi, "Could not clean up the uploaded media for {AudioFile}.", tenFile);
            }
        }

        public static List<CauChep> DocFileSrt(string duongDanFileSrt)
        {
            if (new FileInfo(duongDanFileSrt).Length > GioiHanSrt)
            {
                throw new InvalidDataException("Choose an SRT file up to 1 MB.");
            }

            string[] cacDong = System.IO.File.ReadAllLines(duongDanFileSrt);
            List<CauChep> cacCau = new List<CauChep>();
            HashSet<int> cacSoThuTu = new HashSet<int>();
            int viTri = 0;
            // Mỗi khối gồm số thứ tự, mốc thời gian và các dòng nội dung.
            while (viTri < cacDong.Length)
            {
                if (string.IsNullOrWhiteSpace(cacDong[viTri]))
                {
                    viTri++;
                    continue;
                }

                int dongDau = viTri + 1;
                if (!int.TryParse(cacDong[viTri].Trim(), NumberStyles.None, CultureInfo.InvariantCulture, out int thuTu)
                    || thuTu < 1 || !cacSoThuTu.Add(thuTu) || ++viTri >= cacDong.Length)
                {
                    throw new InvalidDataException($"Invalid or duplicate subtitle number at line {dongDau}.");
                }

                string[] mocThoiGian = cacDong[viTri++].Split("-->", StringSplitOptions.TrimEntries);
                if (mocThoiGian.Length != 2 || !DocMocThoiGian(mocThoiGian[0], out double batDau)
                    || !DocMocThoiGian(mocThoiGian[1], out double ketThuc) || ketThuc <= batDau)
                {
                    throw new InvalidDataException($"Invalid subtitle time range at line {dongDau + 1}.");
                }

                List<string> noiDung = new List<string>();
                while (viTri < cacDong.Length && !string.IsNullOrWhiteSpace(cacDong[viTri]))
                {
                    noiDung.Add(cacDong[viTri++].Trim());
                }

                string noiDungChuan = string.Join(" ", noiDung);
                if (noiDungChuan.Length == 0 || noiDungChuan.Length > GioiHanTranscript
                    || noiDungChuan.Contains('\0') || noiDungChuan.Contains('\uFFFD'))
                {
                    throw new InvalidDataException($"Invalid subtitle text in entry {thuTu}. Use up to 2,000 characters per entry.");
                }

                CauChep cau = new CauChep();
                cau.ThuTuCau = thuTu;
                cau.ThoiGianBatDau = batDau;
                cau.ThoiGianKetThuc = ketThuc;
                cau.NoiDungChuan = noiDungChuan;
                cau.TrangThaiDuyet = true;
                cacCau.Add(cau);
            }

            if (cacCau.Count == 0)
            {
                throw new InvalidDataException("The SRT file does not contain any subtitles.");
            }

            return cacCau;
        }

        private static bool DocMocThoiGian(string moc, out double soGiay)
        {
            soGiay = 0;
            string[] phan = moc.Split(':', ',');
            if (phan.Length != 4 || phan[0].Length != 2 || phan[1].Length != 2
                || phan[2].Length != 2 || phan[3].Length != 3
                || !int.TryParse(phan[0], NumberStyles.None, CultureInfo.InvariantCulture, out int gio)
                || !int.TryParse(phan[1], NumberStyles.None, CultureInfo.InvariantCulture, out int phut)
                || !int.TryParse(phan[2], NumberStyles.None, CultureInfo.InvariantCulture, out int giay)
                || !int.TryParse(phan[3], NumberStyles.None, CultureInfo.InvariantCulture, out int miliGiay)
                || phut > 59 || giay > 59)
            {
                return false;
            }

            soGiay = gio * 3600 + phut * 60 + giay + miliGiay / 1000d;
            return true;
        }

        private bool LaFileThuong(string duongDan)
        {
            return File.Exists(duongDan) && !LaLienKet(_thuMucAudio) && !LaLienKet(duongDan);
        }

        private static bool LaLienKet(string duongDan)
        {
            return (File.GetAttributes(duongDan) & FileAttributes.ReparsePoint) != 0;
        }

        private static void KiemTraTep(IFormFile? file, string duoiFile, long gioiHan, string thongBao)
        {
            if (file != null && (file.Length <= 0 || file.Length > gioiHan
                || !string.Equals(Path.GetExtension(file.FileName), duoiFile, StringComparison.OrdinalIgnoreCase)))
            {
                throw new InvalidDataException(thongBao);
            }
        }
    }
}
