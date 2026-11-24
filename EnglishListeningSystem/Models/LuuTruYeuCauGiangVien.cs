using System.Globalization;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace EnglishListeningSystem.Models
{
    public class LuuTruYeuCauGiangVien
    {
        private readonly string _thuMuc;
        private readonly string _duongDan;

        public LuuTruYeuCauGiangVien(IWebHostEnvironment moiTruong)
        {
            _thuMuc = Path.Combine(moiTruong.ContentRootPath, "App_Data");
            _duongDan = Path.Combine(_thuMuc, "yeu_cau_giang_vien.json");
        }

        public async Task<FileStream> KhoaGhiAsync()
        {
            try
            {
                Directory.CreateDirectory(_thuMuc);
                string duongDanKhoa = _duongDan + ".lock";

                // Giữ tệp khóa đến khi đăng ký, duyệt hoặc hủy xử lý xong.
                for (int lan = 1; ; lan++)
                {
                    try
                    {
                        return File.Open(duongDanKhoa, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                    }
                    catch (IOException) when (lan < 30)
                    {
                        await Task.Delay(100);
                    }
                }
            }
            catch (UnauthorizedAccessException loi)
            {
                throw new IOException("Không có quyền khóa danh sách yêu cầu giảng viên.", loi);
            }
        }

        public List<YeuCauGiangVien> LayDanhSach()
        {
            try
            {
                using FileStream tep = File.Open(_duongDan, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
                List<YeuCauGiangVien>? dsYeuCau = JsonSerializer.Deserialize<List<YeuCauGiangVien>>(tep);
                if (dsYeuCau == null)
                {
                    throw new IOException("Tệp yêu cầu giảng viên không chứa danh sách.");
                }

                return dsYeuCau;
            }
            catch (FileNotFoundException)
            {
                return new List<YeuCauGiangVien>();
            }
            catch (DirectoryNotFoundException)
            {
                return new List<YeuCauGiangVien>();
            }
            catch (JsonException loi)
            {
                throw new IOException("Không đọc được danh sách yêu cầu giảng viên.", loi);
            }
            catch (UnauthorizedAccessException loi)
            {
                throw new IOException("Không có quyền đọc danh sách yêu cầu giảng viên.", loi);
            }
        }

        public void ThemYeuCau(YeuCauGiangVien yeuCau)
        {
            List<YeuCauGiangVien> dsYeuCau = LayDanhSach();
            dsYeuCau.Add(yeuCau);
            LuuDanhSach(dsYeuCau);
        }

        public bool XoaYeuCau(string id)
        {
            List<YeuCauGiangVien> dsYeuCau = LayDanhSach();
            int soYeuCauDaXoa = dsYeuCau.RemoveAll(yeuCau => yeuCau.Id == id);
            if (soYeuCauDaXoa == 0)
            {
                return false;
            }

            LuuDanhSach(dsYeuCau);
            return true;
        }

        public static bool TrungThongTin(string giaTri, string giaTriCanTim)
        {
            // So sánh tên và email theo quy tắc không phân biệt hoa thường và dấu.
            int ketQua = CultureInfo.InvariantCulture.CompareInfo.Compare(
                giaTri, giaTriCanTim, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace);
            return ketQua == 0;
        }

        private void LuuDanhSach(List<YeuCauGiangVien> dsYeuCau)
        {
            string tepTam = _duongDan + "." + Guid.NewGuid().ToString("N") + ".tmp";
            try
            {
                Directory.CreateDirectory(_thuMuc);
                // Ghi xong tệp tạm rồi mới thay tệp cũ để người đọc luôn thấy JSON đầy đủ.
                string json = JsonSerializer.Serialize(dsYeuCau);
                File.WriteAllText(tepTam, json);
                File.Move(tepTam, _duongDan, overwrite: true);
            }
            catch (UnauthorizedAccessException loi)
            {
                throw new IOException("Không có quyền ghi danh sách yêu cầu giảng viên.", loi);
            }
            finally
            {
                if (File.Exists(tepTam))
                {
                    File.Delete(tepTam);
                }
            }
        }
    }
}
