using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Data.SqlClient;
using System.Linq;
using QL_HDNK.Filters;
using QL_HDNK.Models;

namespace QL_HDNK.Services
{
    public class NotificationService
    {
        private readonly QuanLy_HDNKEntities db;

        public NotificationService(QuanLy_HDNKEntities db)
        {
            this.db = db;
        }

        public int CountUnread(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return 0;
            }

            EnsureSchema();
            CreateUpcomingEventNotifications(userId);

            return db.Database.SqlQuery<int>(
                "SELECT COUNT(1) FROM dbo.ThongBao WHERE MaNguoiNhan = @userId AND DaDoc = 0",
                new SqlParameter("@userId", userId)).First();
        }

        public List<ThongBaoViewModel> ListForUser(string userId, int take = 50)
        {
            EnsureSchema();
            CreateUpcomingEventNotifications(userId);

            return db.Database.SqlQuery<ThongBaoViewModel>(
                @"SELECT TOP (@take)
                    MaThongBao, MaNguoiNhan, TieuDe, NoiDung, DuongDan, Loai, MaLienQuan, DaDoc, ThoiGianTao
                  FROM dbo.ThongBao
                  WHERE MaNguoiNhan = @userId
                  ORDER BY DaDoc ASC, ThoiGianTao DESC",
                new SqlParameter("@take", take),
                new SqlParameter("@userId", userId)).ToList();
        }

        public void MarkRead(string userId, string id)
        {
            EnsureSchema();
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.ThongBao SET DaDoc = 1, ThoiGianDoc = GETDATE() WHERE MaThongBao = @id AND MaNguoiNhan = @userId",
                new SqlParameter("@id", id),
                new SqlParameter("@userId", userId));
        }

        public void MarkAllRead(string userId)
        {
            EnsureSchema();
            db.Database.ExecuteSqlCommand(
                "UPDATE dbo.ThongBao SET DaDoc = 1, ThoiGianDoc = GETDATE() WHERE MaNguoiNhan = @userId AND DaDoc = 0",
                new SqlParameter("@userId", userId));
        }

        public void MarkRelatedRead(string type, string relatedKey)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(relatedKey))
            {
                return;
            }

            EnsureSchema();
            db.Database.ExecuteSqlCommand(
                @"UPDATE dbo.ThongBao
                  SET DaDoc = 1, ThoiGianDoc = GETDATE()
                  WHERE Loai = @type AND MaLienQuan = @relatedKey AND DaDoc = 0",
                new SqlParameter("@type", type),
                new SqlParameter("@relatedKey", relatedKey));
        }

        public void Notify(string userId, string title, string body, string url, string type, string relatedKey = null)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return;
            }

            EnsureSchema();

            db.Database.ExecuteSqlCommand(
                @"INSERT INTO dbo.ThongBao
                    (MaThongBao, MaNguoiNhan, TieuDe, NoiDung, DuongDan, Loai, MaLienQuan, DaDoc, ThoiGianTao)
                  VALUES
                    (@id, @userId, @title, @body, @url, @type, @relatedKey, 0, GETDATE())",
                new SqlParameter("@id", CreateId()),
                new SqlParameter("@userId", userId),
                new SqlParameter("@title", title),
                new SqlParameter("@body", body),
                new SqlParameter("@url", (object)url ?? DBNull.Value),
                new SqlParameter("@type", (object)type ?? DBNull.Value),
                new SqlParameter("@relatedKey", (object)relatedKey ?? DBNull.Value));
        }

        public void NotifyOnce(string userId, string title, string body, string url, string type, string relatedKey)
        {
            if (string.IsNullOrWhiteSpace(userId) || string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(relatedKey))
            {
                return;
            }

            EnsureSchema();
            var existed = db.Database.SqlQuery<int>(
                @"SELECT COUNT(1) FROM dbo.ThongBao
                  WHERE MaNguoiNhan = @userId AND Loai = @type AND MaLienQuan = @relatedKey",
                new SqlParameter("@userId", userId),
                new SqlParameter("@type", type),
                new SqlParameter("@relatedKey", relatedKey)).First();

            if (existed == 0)
            {
                Notify(userId, title, body, url, type, relatedKey);
            }
        }

        public void NotifyEventCreator(SuKien suKien, bool approved)
        {
            var title = approved ? "Sự kiện được duyệt" : "Sự kiện bị từ chối";
            var body = approved
                ? "Sự kiện \"" + suKien.TenSuKien + "\" đã được duyệt và mở đăng ký."
                : "Sự kiện \"" + suKien.TenSuKien + "\" bị từ chối. Lý do: " + suKien.LyDoHuy;

            Notify(suKien.MaNguoiTao, title, body, "/SuKiens/Details/" + suKien.MaSuKien, approved ? "EVENT_APPROVED" : "EVENT_REJECTED", suKien.MaSuKien);
        }

        public void NotifyAttendanceEvidence(DangKy_DiemDanh item, bool approved)
        {
            if (item.SuKien == null)
            {
                db.Entry(item).Reference(x => x.SuKien).Load();
            }

            var eventName = item.SuKien != null ? item.SuKien.TenSuKien : item.MaSuKien;
            var title = approved ? "Minh chứng điểm danh được duyệt" : "Minh chứng điểm danh bị từ chối";
            var body = approved
                ? "Minh chứng điểm danh của bạn cho sự kiện \"" + eventName + "\" đã được duyệt."
                : "Minh chứng điểm danh của bạn cho sự kiện \"" + eventName + "\" bị từ chối.";

            Notify(item.MaNguoiDung, title, body, "/SinhVien/LichCaNhan", approved ? "ATTENDANCE_APPROVED" : "ATTENDANCE_REJECTED", item.MaDangKy);
        }

        public void NotifyEvidenceSubmitted(DangKy_DiemDanh item)
        {
            if (item.SuKien == null)
            {
                db.Entry(item).Reference(x => x.SuKien).Load();
            }

            if (item.NguoiDung == null)
            {
                db.Entry(item).Reference(x => x.NguoiDung).Load();
            }

            if (item.SuKien == null)
            {
                return;
            }

            var studentName = item.NguoiDung != null ? item.NguoiDung.HoTen : item.MaNguoiDung;
            Notify(
                item.SuKien.MaNguoiTao,
                "Có minh chứng điểm danh chờ duyệt",
                studentName + " đã gửi ảnh minh chứng cho sự kiện \"" + item.SuKien.TenSuKien + "\".",
                "/DangKy_DiemDanh#dk-" + item.MaDangKy,
                "ATTENDANCE_PENDING",
                item.MaDangKy);
            NotifyAttendanceApprovers(item, studentName);
        }

        private void NotifyAttendanceApprovers(DangKy_DiemDanh item, string studentName)
        {
            if (item.SuKien == null)
            {
                db.Entry(item).Reference(x => x.SuKien).Load();
            }

            if (item.SuKien == null) return;

            // Lấy thông tin người tạo sự kiện để biết sự kiện thuộc Khoa/Lớp nào
            var creator = db.NguoiDungs.Include(x => x.QuyenHan).Include(x => x.LopHoc)
                .FirstOrDefault(x => x.MaNguoiDung == item.SuKien.MaNguoiTao);

            if (creator == null) return;

            var facultyId = creator.LopHoc != null ? creator.LopHoc.MaKhoa : null;
            var classId = creator.MaLop;
            var creatorRole = RoleHelper.Normalize(creator.MaQuyen, creator.QuyenHan != null ? creator.QuyenHan.TenQuyen : "");

            var approverIds = db.NguoiDungs.Include(x => x.QuyenHan).Include(x => x.LopHoc).ToList()
                .Where(x =>
                {
                    var role = RoleHelper.Normalize(x.MaQuyen, x.QuyenHan != null ? x.QuyenHan.TenQuyen : "");
                    // Admin luôn nhận được
                    if (role == RoleKeys.Admin) return true;

                    // Nếu là sự kiện cấp Khoa -> Gửi cho BCH Khoa đó
                    if (role == RoleKeys.FacultyUnion && !string.IsNullOrEmpty(facultyId) && x.LopHoc != null && x.LopHoc.MaKhoa == facultyId)
                    {
                        return true;
                    }

                    // Nếu là sự kiện cấp Lớp -> Gửi cho tất cả cán bộ lớp đó
                    if (role == RoleKeys.ClassOfficer && !string.IsNullOrEmpty(classId) && x.MaLop == classId)
                    {
                        return true;
                    }

                    return false;
                })
                .Select(x => x.MaNguoiDung)
                .Distinct()
                .ToList();

            foreach (var approverId in approverIds)
            {
                NotifyOnce(
                    approverId,
                    "Có minh chứng điểm danh chờ duyệt",
                    studentName + " đã gửi ảnh minh chứng cho sự kiện \"" + item.SuKien.TenSuKien + "\".",
                    "/DangKy_DiemDanh#dk-" + item.MaDangKy,
                    "ATTENDANCE_PENDING",
                    item.MaDangKy);
            }
        }

        private string GetUserFacultyId(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                return null;
            }

            var user = db.NguoiDungs.Include(x => x.LopHoc).FirstOrDefault(x => x.MaNguoiDung == userId);
            return user != null && user.LopHoc != null ? user.LopHoc.MaKhoa : null;
        }

        private void CreateUpcomingEventNotifications(string userId)
        {
            var now = DateTime.Now;
            var until = now.AddHours(24);
            var registrations = db.DangKy_DiemDanh
                .Include("SuKien")
                .Where(x => x.MaNguoiDung == userId && x.SuKien.ThoiGianBatDau >= now && x.SuKien.ThoiGianBatDau <= until)
                .ToList();

            foreach (var registration in registrations)
            {
                NotifyOnce(
                    userId,
                    "Sự kiện sắp diễn ra",
                    "Sự kiện \"" + registration.SuKien.TenSuKien + "\" sẽ diễn ra lúc " + registration.SuKien.ThoiGianBatDau.ToString("dd/MM/yyyy HH:mm") + ".",
                    "/SinhVien/LichCaNhan",
                    "EVENT_UPCOMING",
                    registration.MaSuKien + ":" + registration.SuKien.ThoiGianBatDau.ToString("yyyyMMddHHmm"));
            }
        }

        private void EnsureSchema()
        {
            db.Database.ExecuteSqlCommand(
                @"IF OBJECT_ID('dbo.ThongBao', 'U') IS NULL
                  BEGIN
                    CREATE TABLE dbo.ThongBao (
                        MaThongBao nvarchar(30) NOT NULL CONSTRAINT PK_ThongBao PRIMARY KEY,
                        MaNguoiNhan nvarchar(20) NOT NULL,
                        TieuDe nvarchar(200) NOT NULL,
                        NoiDung nvarchar(1000) NULL,
                        DuongDan nvarchar(500) NULL,
                        Loai nvarchar(50) NULL,
                        MaLienQuan nvarchar(100) NULL,
                        DaDoc bit NOT NULL CONSTRAINT DF_ThongBao_DaDoc DEFAULT(0),
                        ThoiGianTao datetime NOT NULL CONSTRAINT DF_ThongBao_ThoiGianTao DEFAULT(GETDATE()),
                        ThoiGianDoc datetime NULL
                    );
                    CREATE INDEX IX_ThongBao_NguoiNhan_DaDoc ON dbo.ThongBao(MaNguoiNhan, DaDoc, ThoiGianTao DESC);
                    CREATE INDEX IX_ThongBao_UniqueEvent ON dbo.ThongBao(MaNguoiNhan, Loai, MaLienQuan);
                  END");
        }

        private string CreateId()
        {
            return "TB" + DateTime.Now.ToString("yyyyMMddHHmmssfff") + Guid.NewGuid().ToString("N").Substring(0, 5).ToUpper();
        }
    }
}
