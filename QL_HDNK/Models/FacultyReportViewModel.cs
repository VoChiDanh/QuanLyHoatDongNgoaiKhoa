using System.Collections.Generic;
using System.Web.Mvc;

namespace QL_HDNK.Models
{
    public class FacultyReportViewModel
    {
        public string MaKhoa { get; set; }
        public string TenKhoa { get; set; }
        public string MaNamHoc { get; set; }
        public string MaHocKy { get; set; }
        public string CheDo { get; set; }
        public string HeaderImageBase64 { get; set; }

        public SelectList KhoaList { get; set; }
        public SelectList NamHocList { get; set; }
        public SelectList HocKyList { get; set; }

        public List<FacultyReportEventHeader> Events { get; set; } = new List<FacultyReportEventHeader>();
        public List<FacultyReportRow> Rows { get; set; } = new List<FacultyReportRow>();

        public int TotalClasses => Rows?.Count ?? 0;
        public int TotalStudents { get; set; }
        public int TotalEvents => Events?.Count ?? 0;
        public int TotalAttendances { get; set; }
    }

    public class FacultyReportEventHeader
    {
        public string MaSuKien { get; set; }
        public string TenSuKien { get; set; }
        public string ThoiGianText { get; set; }
        public bool LaSuKienCha { get; set; }
        public int SoSuKienCon { get; set; }
    }

    public class FacultyReportRow
    {
        public string MaLop { get; set; }
        public string TenLop { get; set; }
        public string KhoaHoc { get; set; }
        public int SiSo { get; set; }
        public List<FacultyReportCell> Cells { get; set; }
    }

    public class FacultyReportCell
    {
        public string MaSuKien { get; set; }
        public int SoLuongThamGia { get; set; }
        public int SiSo { get; set; }
        public decimal TyLeThamGia { get; set; }
    }
}
