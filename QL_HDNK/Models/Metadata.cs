using System;
using System.ComponentModel.DataAnnotations;

namespace QL_HDNK.Models
{
    [MetadataType(typeof(DangKy_DiemDanhMetadata))]
    public partial class DangKy_DiemDanh { }

    [MetadataType(typeof(DanhMucDiemMetadata))]
    public partial class DanhMucDiem { }

    [MetadataType(typeof(HocKyMetadata))]
    public partial class HocKy { }

    [MetadataType(typeof(KhoaMetadata))]
    public partial class Khoa { }

    [MetadataType(typeof(LopHocMetadata))]
    public partial class LopHoc { }

    [MetadataType(typeof(MucDiemRenLuyenMetadata))]
    public partial class MucDiemRenLuyen { }

    [MetadataType(typeof(NamHocMetadata))]
    public partial class NamHoc { }

    [MetadataType(typeof(NguoiDungMetadata))]
    public partial class NguoiDung { }

    [MetadataType(typeof(NhatKyHeThongMetadata))]
    public partial class NhatKyHeThong { }

    [MetadataType(typeof(QuyenHanMetadata))]
    public partial class QuyenHan { }

    [MetadataType(typeof(SuKienMetadata))]
    public partial class SuKien { }

    [MetadataType(typeof(ThamSoQuyDinhMetadata))]
    public partial class ThamSoQuyDinh { }

    [MetadataType(typeof(TrangThaiDangKyMetadata))]
    public partial class TrangThaiDangKy { }

    [MetadataType(typeof(TrangThaiDiemDanhMetadata))]
    public partial class TrangThaiDiemDanh { }

    [MetadataType(typeof(TrangThaiSuKienMetadata))]
    public partial class TrangThaiSuKien { }

    public class DangKy_DiemDanhMetadata
    {
        [Display(Name = "Mã đăng ký")]
        public string MaDangKy { get; set; }
        [Display(Name = "Mã sự kiện")]
        public string MaSuKien { get; set; }
        [Display(Name = "Mã người dùng")]
        public string MaNguoiDung { get; set; }
        [Display(Name = "Thời gian đăng ký")]
        public DateTime? ThoiGianDangKy { get; set; }
        [Display(Name = "Trạng thái đăng ký")]
        public string MaTTDK { get; set; }
        [Display(Name = "Thời gian điểm danh")]
        public DateTime? ThoiGianDiemDanh { get; set; }
        [Display(Name = "Minh chứng")]
        public string MinhChung { get; set; }
        [Display(Name = "Trạng thái điểm danh")]
        public string MaTTDD { get; set; }
        [Display(Name = "Người duyệt")]
        public string NguoiDuyet { get; set; }
        [Display(Name = "Thời gian duyệt")]
        public DateTime? ThoiGianDuyet { get; set; }
        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }
    }

    public class DanhMucDiemMetadata
    {
        [Display(Name = "Mã danh mục")]
        public string MaDanhMuc { get; set; }
        [Display(Name = "Tên danh mục")]
        public string TenDanhMuc { get; set; }
        [Display(Name = "Điểm tối đa")]
        public int? DiemToiDa { get; set; }
        [Display(Name = "Ghi chú")]
        public string GhiChu { get; set; }
    }

    public class HocKyMetadata
    {
        [Display(Name = "Mã học kỳ")]
        public string MaHocKy { get; set; }
        [Display(Name = "Tên học kỳ")]
        public string TenHocKy { get; set; }
        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? TuNgay { get; set; }
        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DenNgay { get; set; }
    }

    public class KhoaMetadata
    {
        [Display(Name = "Mã khoa")]
        public string MaKhoa { get; set; }
        [Display(Name = "Tên khoa")]
        public string TenKhoa { get; set; }
    }

    public class LopHocMetadata
    {
        [Display(Name = "Mã lớp")]
        public string MaLop { get; set; }
        [Display(Name = "Tên lớp")]
        public string TenLop { get; set; }
        [Display(Name = "Khóa học")]
        public string KhoaHoc { get; set; }
        [Display(Name = "Khoa")]
        public string MaKhoa { get; set; }
    }

    public class MucDiemRenLuyenMetadata
    {
        [Display(Name = "Mã mức điểm")]
        public string MaMucDRL { get; set; }
        [Display(Name = "Tên mức điểm rèn luyện")]
        public string TenMucDRL { get; set; }
        [Display(Name = "Điểm")]
        public int Diem { get; set; }
        [Display(Name = "Danh mục điểm")]
        public string MaDanhMuc { get; set; }
    }

    public class NamHocMetadata
    {
        [Display(Name = "Mã năm học")]
        public string MaNamHoc { get; set; }
        [Display(Name = "Tên năm học")]
        public string TenNamHoc { get; set; }
        [Display(Name = "Từ ngày")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? TuNgay { get; set; }
        [Display(Name = "Đến ngày")]
        [DataType(DataType.Date)]
        [DisplayFormat(DataFormatString = "{0:yyyy-MM-dd}", ApplyFormatInEditMode = true)]
        public DateTime? DenNgay { get; set; }
    }

    public class NguoiDungMetadata
    {
        [Display(Name = "Mã người dùng")]
        public string MaNguoiDung { get; set; }
        [Display(Name = "Mã sinh viên")]
        public string MaSinhVien { get; set; }
        [Display(Name = "Mật khẩu")]
        public string MatKhau { get; set; }
        [Display(Name = "Họ tên")]
        public string HoTen { get; set; }
        [Display(Name = "Ngày sinh")]
        public DateTime? NgaySinh { get; set; }
        [Display(Name = "Email")]
        public string Email { get; set; }
        [Display(Name = "Lớp")]
        public string MaLop { get; set; }
        [Display(Name = "Quyền hạn")]
        public string MaQuyen { get; set; }
    }

    public class NhatKyHeThongMetadata
    {
        [Display(Name = "Mã nhật ký")]
        public string MaNhatKy { get; set; }
        [Display(Name = "Người dùng")]
        public string MaNguoiDung { get; set; }
        [Display(Name = "Chi tiết")]
        public string ChiTiet { get; set; }
        [Display(Name = "Thời gian")]
        public DateTime? ThoiGian { get; set; }
    }

    public class QuyenHanMetadata
    {
        [Display(Name = "Mã quyền")]
        public string MaQuyen { get; set; }
        [Display(Name = "Tên quyền")]
        public string TenQuyen { get; set; }
        [Display(Name = "Mô tả")]
        public string MoTa { get; set; }
    }

    public class SuKienMetadata
    {
        [Display(Name = "Mã sự kiện")]
        public string MaSuKien { get; set; }
        [Display(Name = "Tên sự kiện")]
        public string TenSuKien { get; set; }
        [Display(Name = "Nội dung")]
        public string Noidung { get; set; }
        [Display(Name = "Thời gian bắt đầu")]
        public DateTime ThoiGianBatDau { get; set; }
        [Display(Name = "Thời gian kết thúc")]
        public DateTime ThoiGianKetThuc { get; set; }
        [Display(Name = "Học kỳ")]
        public string MaHocKy { get; set; }
        [Display(Name = "Năm học")]
        public string MaNamHoc { get; set; }
        [Display(Name = "Địa điểm")]
        public string DiaDiem { get; set; }
        [Display(Name = "Số lượng tối đa")]
        public int? SoLuongToiDa { get; set; }
        [Display(Name = "Mức điểm rèn luyện")]
        public string MaMucDRL { get; set; }
        [Display(Name = "Cấp tổ chức")]
        public string CapToChuc { get; set; }
        [Display(Name = "Người tạo")]
        public string MaNguoiTao { get; set; }
        [Display(Name = "Trạng thái sự kiện")]
        public string MaTTSK { get; set; }
        [Display(Name = "Lý do hủy")]
        public string LyDoHuy { get; set; }
        [Display(Name = "Số lượng đã đăng ký")]
        public int? SoLuongDaDangKy { get; set; }
    }

    public class ThamSoQuyDinhMetadata
    {
        [Display(Name = "Mã tham số")]
        public string MaTS { get; set; }
        [Display(Name = "Tên tham số")]
        public string TenTS { get; set; }
        [Display(Name = "Giá trị")]
        public string GiaTri { get; set; }
        [Display(Name = "Đơn vị tính")]
        public string DonViTinh { get; set; }
        [Display(Name = "Tình trạng")]
        public bool? TinhTrang { get; set; }
    }

    public class TrangThaiDangKyMetadata
    {
        [Display(Name = "Mã trạng thái đăng ký")]
        public string MaTTDK { get; set; }
        [Display(Name = "Tên trạng thái đăng ký")]
        public string TenTTDK { get; set; }
    }

    public class TrangThaiDiemDanhMetadata
    {
        [Display(Name = "Mã trạng thái điểm danh")]
        public string MaTTDD { get; set; }
        [Display(Name = "Tên trạng thái điểm danh")]
        public string TenTTDD { get; set; }
    }

    public class TrangThaiSuKienMetadata
    {
        [Display(Name = "Mã trạng thái sự kiện")]
        public string MaTTSK { get; set; }
        [Display(Name = "Tên trạng thái sự kiện")]
        public string TenTTSK { get; set; }
    }
}
