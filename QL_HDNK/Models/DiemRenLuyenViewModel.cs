using System;
using System.Collections.Generic;

namespace QL_HDNK.Models
{
    public class DiemRenLuyenViewModel
    {
        public int TongDiem { get; set; }
        public List<DiemRenLuyenDanhMuc> DanhMucs { get; set; } = new List<DiemRenLuyenDanhMuc>();
    }

    public class DiemRenLuyenDanhMuc
    {
        public string TenDanhMuc { get; set; }
        public int DiemToiDa { get; set; }
        public int DiemDatDuoc { get; set; }
        public List<DiemRenLuyenChiTiet> ChiTiets { get; set; } = new List<DiemRenLuyenChiTiet>();
    }

    public class DiemRenLuyenChiTiet
    {
        public string TenSuKien { get; set; }
        public string TenMucDiem { get; set; }
        public DateTime? ThoiGianDiemDanh { get; set; }
        public bool DaThamGia { get; set; }
        public bool IsEnded { get; set; }
        public int DiemCongTrangThai { get; set; } // Diem duoc cong hoac bi tru
    }
}
