using System;

namespace QL_HDNK.Models
{
    public class ThongBaoViewModel
    {
        public string MaThongBao { get; set; }
        public string MaNguoiNhan { get; set; }
        public string TieuDe { get; set; }
        public string NoiDung { get; set; }
        public string DuongDan { get; set; }
        public string Loai { get; set; }
        public string MaLienQuan { get; set; }
        public bool DaDoc { get; set; }
        public DateTime ThoiGianTao { get; set; }
    }
}
