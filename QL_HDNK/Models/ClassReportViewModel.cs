using System.Collections.Generic;
using System.Web.Mvc;

namespace QL_HDNK.Models
{
    public class ClassReportViewModel
    {
        public string MaLop { get; set; }
        public string TenLop { get; set; }
        public string MaKhoa { get; set; }
        public string TenKhoa { get; set; }
        public string MaHocKy { get; set; }
        public string MaNamHoc { get; set; }
        public string CheDo { get; set; } // "so-luong" hoặc "phan-tram"

        public List<ClassReportEventHeader> Events { get; set; } = new List<ClassReportEventHeader>();
        public List<ClassReportRow> Rows { get; set; } = new List<ClassReportRow>();

        public int TotalStudents => Rows?.Count ?? 0;
        public int TotalEvents => Events?.Count ?? 0;

        // SelectLists for Filter
        public SelectList KhoaList { get; set; }
        public SelectList LopList { get; set; }
        public SelectList HocKyList { get; set; }
        public SelectList NamHocList { get; set; }
    }

    public class ClassReportEventHeader
    {
        public string MaSuKien { get; set; }
        public string TenSuKien { get; set; }
        public string ThoiGianText { get; set; }
        public bool LaSuKienCha { get; set; }
    }

    public class ClassReportRow
    {
        public string MaNguoiDung { get; set; }
        public string MaSinhVien { get; set; }
        public string HoTen { get; set; }
        public List<ClassReportCell> Cells { get; set; } = new List<ClassReportCell>();
    }

    public class ClassReportCell
    {
        public string MaSuKien { get; set; }
        public bool CoMat { get; set; }
        public int Diem { get; set; }
        public string TrangThaiText { get; set; }
    }
}
