using System.Text;

namespace EnglishListeningSystem.Models
{
    public static class ChamTranscript
    {
        public static List<KetQuaTu> SoSanh(string noiDungChuan, string noiDungNhap)
        {
            string[] tuChuan = TachTu(noiDungChuan);
            string[] tuNhap = TachTu(noiDungNhap);
            int[,] khoangCach = new int[tuChuan.Length + 1, tuNhap.Length + 1];
            for (int i = 0; i <= tuChuan.Length; i++)
            {
                khoangCach[i, 0] = i;
            }
            for (int j = 0; j <= tuNhap.Length; j++)
            {
                khoangCach[0, j] = j;
            }

            // Căn chỉnh theo số thao tác ít nhất để một từ thiếu không làm lệch cả câu.
            for (int i = 1; i <= tuChuan.Length; i++)
            {
                for (int j = 1; j <= tuNhap.Length; j++)
                {
                    if (tuChuan[i - 1] == tuNhap[j - 1])
                    {
                        khoangCach[i, j] = khoangCach[i - 1, j - 1];
                    }
                    else
                    {
                        int boTu = khoangCach[i - 1, j];
                        int themTu = khoangCach[i, j - 1];
                        int suaTu = khoangCach[i - 1, j - 1];
                        khoangCach[i, j] = 1 + Math.Min(suaTu, Math.Min(boTu, themTu));
                    }
                }
            }

            List<KetQuaTu> ketQua = new List<KetQuaTu>();
            int viTriChuan = tuChuan.Length;
            int viTriNhap = tuNhap.Length;
            while (viTriChuan > 0 || viTriNhap > 0)
            {
                if (viTriChuan > 0 && viTriNhap > 0 && tuChuan[viTriChuan - 1] == tuNhap[viTriNhap - 1])
                {
                    ketQua.Add(new KetQuaTu(tuChuan[--viTriChuan], tuNhap[--viTriNhap], "correct"));
                }
                else if (viTriChuan > 0 && viTriNhap > 0
                    && khoangCach[viTriChuan, viTriNhap] == khoangCach[viTriChuan - 1, viTriNhap - 1] + 1)
                {
                    ketQua.Add(new KetQuaTu(tuChuan[--viTriChuan], tuNhap[--viTriNhap], "incorrect"));
                }
                else if (viTriChuan > 0 && khoangCach[viTriChuan, viTriNhap] == khoangCach[viTriChuan - 1, viTriNhap] + 1)
                {
                    ketQua.Add(new KetQuaTu(tuChuan[--viTriChuan], "", "missing"));
                }
                else
                {
                    ketQua.Add(new KetQuaTu("", tuNhap[--viTriNhap], "extra"));
                }
            }

            ketQua.Reverse();
            return ketQua;
        }

        private static string[] TachTu(string noiDung)
        {
            string chuoi = noiDung.Normalize(NormalizationForm.FormKC).ToLowerInvariant()
                .Replace(".", "").Replace(",", "").Replace("?", "").Replace("!", "")
                .Replace('’', '\'');
            return chuoi.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        }
    }

    public class KetQuaTu
    {
        public KetQuaTu(string dapAn, string daNhap, string trangThai)
        {
            Expected = dapAn;
            Actual = daNhap;
            Status = trangThai;
        }

        public string Expected { get; }
        public string Actual { get; }
        public string Status { get; }
    }
}
