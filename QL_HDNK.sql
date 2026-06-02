USE master;
GO

IF DB_ID(N'QuanLy_HDNK') IS NOT NULL
BEGIN
    ALTER DATABASE QuanLy_HDNK SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE QuanLy_HDNK;
END
GO

CREATE DATABASE QuanLy_HDNK COLLATE Vietnamese_CI_AS;
GO

USE QuanLy_HDNK;
GO

CREATE TABLE dbo.TrangThaiDangKy (
    MaTTDK nvarchar(10) NOT NULL CONSTRAINT PK_TrangThaiDangKy PRIMARY KEY,
    TenTTDK nvarchar(100) NOT NULL CONSTRAINT UQ_TrangThaiDangKy_Ten UNIQUE
);

CREATE TABLE dbo.TrangThaiSuKien (
    MaTTSK nvarchar(10) NOT NULL CONSTRAINT PK_TrangThaiSuKien PRIMARY KEY,
    TenTTSK nvarchar(100) NOT NULL CONSTRAINT UQ_TrangThaiSuKien_Ten UNIQUE
);

CREATE TABLE dbo.TrangThaiDiemDanh (
    MaTTDD nvarchar(10) NOT NULL CONSTRAINT PK_TrangThaiDiemDanh PRIMARY KEY,
    TenTTDD nvarchar(100) NOT NULL CONSTRAINT UQ_TrangThaiDiemDanh_Ten UNIQUE
);

CREATE TABLE dbo.ThamSoQuyDinh (
    MaTS nvarchar(10) NOT NULL CONSTRAINT PK_ThamSoQuyDinh PRIMARY KEY,
    TenTS nvarchar(255) NOT NULL,
    GiaTri float NOT NULL,
    DonViTinh nvarchar(50) NULL,
    TinhTrang bit NOT NULL CONSTRAINT DF_ThamSoQuyDinh_TinhTrang DEFAULT(1)
);

CREATE TABLE dbo.QuyenHan (
    MaQuyen nvarchar(10) NOT NULL CONSTRAINT PK_QuyenHan PRIMARY KEY,
    TenQuyen nvarchar(100) NOT NULL CONSTRAINT UQ_QuyenHan_Ten UNIQUE,
    MoTa nvarchar(500) NULL
);

CREATE TABLE dbo.Khoa (
    MaKhoa nvarchar(10) NOT NULL CONSTRAINT PK_Khoa PRIMARY KEY,
    TenKhoa nvarchar(255) NOT NULL
);

CREATE TABLE dbo.LopHoc (
    MaLop nvarchar(10) NOT NULL CONSTRAINT PK_LopHoc PRIMARY KEY,
    TenLop nvarchar(255) NOT NULL,
    MaKhoa nvarchar(10) NOT NULL,
    KhoaHoc nvarchar(50) NULL,
    CONSTRAINT FK_LopHoc_Khoa FOREIGN KEY (MaKhoa) REFERENCES dbo.Khoa(MaKhoa) ON DELETE CASCADE
);

CREATE TABLE dbo.DanhMucDiem (
    MaDanhMuc nvarchar(10) NOT NULL CONSTRAINT PK_DanhMucDiem PRIMARY KEY,
    TenDanhMuc nvarchar(500) NOT NULL,
    DiemToiDa int NOT NULL,
    MaDanhMucCha nvarchar(10) NULL,
    GhiChu nvarchar(500) NULL,
    CONSTRAINT FK_DanhMucDiem_Cha FOREIGN KEY (MaDanhMucCha) REFERENCES dbo.DanhMucDiem(MaDanhMuc)
);

CREATE TABLE dbo.NguoiDung (
    MaNguoiDung nvarchar(10) NOT NULL CONSTRAINT PK_NguoiDung PRIMARY KEY,
    MaSinhVien nvarchar(50) NULL CONSTRAINT UQ_NguoiDung_MaSinhVien UNIQUE,
    MatKhau nvarchar(255) NOT NULL,
    HoTen nvarchar(255) NOT NULL,
    NgaySinh date NULL,
    Email nvarchar(255) NOT NULL CONSTRAINT UQ_NguoiDung_Email UNIQUE,
    MaLop nvarchar(10) NULL,
    MaQuyen nvarchar(10) NOT NULL,
    CONSTRAINT FK_NguoiDung_LopHoc FOREIGN KEY (MaLop) REFERENCES dbo.LopHoc(MaLop) ON DELETE SET NULL,
    CONSTRAINT FK_NguoiDung_QuyenHan FOREIGN KEY (MaQuyen) REFERENCES dbo.QuyenHan(MaQuyen)
);

CREATE TABLE dbo.MucDiemRenLuyen (
    MaMucDRL nvarchar(10) NOT NULL CONSTRAINT PK_MucDiemRenLuyen PRIMARY KEY,
    TenMucDRL nvarchar(500) NOT NULL,
    Diem int NOT NULL,
    MaDanhMuc nvarchar(10) NOT NULL,
    CONSTRAINT FK_MucDiemRenLuyen_DanhMucDiem FOREIGN KEY (MaDanhMuc) REFERENCES dbo.DanhMucDiem(MaDanhMuc)
);

-- ĐÃ BỔ SUNG TuNgay VÀ DenNgay
CREATE TABLE dbo.HocKy (
    MaHocKy varchar(3) NOT NULL CONSTRAINT PK_HocKy PRIMARY KEY,
    TenHocKy nvarchar(20) NOT NULL,
    TuNgay date NULL,
    DenNgay date NULL
);

-- ĐÃ BỔ SUNG TuNgay VÀ DenNgay
CREATE TABLE dbo.NamHoc (
    MaNamHoc varchar(5) NOT NULL CONSTRAINT PK_NamHoc PRIMARY KEY,
    TenNamHoc nvarchar(20) NOT NULL,
    TuNgay date NULL,
    DenNgay date NULL
);

CREATE TABLE dbo.SuKien (
    MaSuKien nvarchar(10) NOT NULL CONSTRAINT PK_SuKien PRIMARY KEY,
    TenSuKien nvarchar(500) NOT NULL,
    Noidung nvarchar(max) NOT NULL,
    ThoiGianBatDau datetime NOT NULL,
    ThoiGianKetThuc datetime NOT NULL,
    MaHocKy varchar(3) NOT NULL,
    MaNamHoc varchar(5) NOT NULL,
    DiaDiem nvarchar(500) NOT NULL,
    SoLuongToiDa int NULL,
    MaMucDRL nvarchar(10) NOT NULL,
    CapToChuc nvarchar(50) NOT NULL,
    MaNguoiTao nvarchar(10) NOT NULL,
    MaTTSK nvarchar(10) NOT NULL,
    LyDoHuy nvarchar(max) NULL,
    SoLuongDaDangKy int NOT NULL CONSTRAINT DF_SuKien_SoLuongDaDangKy DEFAULT(0),
    MaSuKienCha nvarchar(10) NULL,
    CONSTRAINT FK_SuKien_MucDiemRenLuyen FOREIGN KEY (MaMucDRL) REFERENCES dbo.MucDiemRenLuyen(MaMucDRL),
    CONSTRAINT FK_SuKien_NguoiTao FOREIGN KEY (MaNguoiTao) REFERENCES dbo.NguoiDung(MaNguoiDung),
    CONSTRAINT FK_SuKien_TrangThai FOREIGN KEY (MaTTSK) REFERENCES dbo.TrangThaiSuKien(MaTTSK),
    CONSTRAINT FK_SuKien_HocKy FOREIGN KEY (MaHocKy) REFERENCES dbo.HocKy(MaHocKy),
    CONSTRAINT FK_SuKien_NamHoc FOREIGN KEY (MaNamHoc) REFERENCES dbo.NamHoc(MaNamHoc),
    CONSTRAINT FK_SuKien_Cha FOREIGN KEY (MaSuKienCha) REFERENCES dbo.SuKien(MaSuKien),
    CONSTRAINT CK_SuKien_CapToChuc CHECK (CapToChuc IN (N'Khoa', N'Lớp')),
    CONSTRAINT CK_SuKien_ThoiGian CHECK (ThoiGianBatDau < ThoiGianKetThuc),
    CONSTRAINT CK_SuKien_SoLuongToiDa CHECK (SoLuongToiDa IS NULL OR SoLuongToiDa > 0),
    CONSTRAINT CK_SuKien_KhongTuLamCha CHECK (MaSuKienCha IS NULL OR MaSuKienCha <> MaSuKien)
);

CREATE TABLE dbo.DangKy_DiemDanh (
    MaDangKy nvarchar(10) NOT NULL CONSTRAINT PK_DangKy_DiemDanh PRIMARY KEY,
    MaSuKien nvarchar(10) NOT NULL,
    MaNguoiDung nvarchar(10) NOT NULL,
    ThoiGianDangKy datetime NOT NULL CONSTRAINT DF_DangKy_DiemDanh_ThoiGianDangKy DEFAULT(GETDATE()),
    MaTTDK nvarchar(10) NOT NULL,
    ThoiGianDiemDanh datetime NULL,
    MinhChung nvarchar(max) NULL,
    MaTTDD nvarchar(10) NOT NULL,
    NguoiDuyet nvarchar(10) NULL,
    ThoiGianDuyet datetime NULL,
    GhiChu nvarchar(500) NULL,
    CONSTRAINT FK_DangKy_SuKien FOREIGN KEY (MaSuKien) REFERENCES dbo.SuKien(MaSuKien) ON DELETE CASCADE,
    CONSTRAINT FK_DangKy_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung(MaNguoiDung),
    CONSTRAINT FK_DangKy_TTDK FOREIGN KEY (MaTTDK) REFERENCES dbo.TrangThaiDangKy(MaTTDK),
    CONSTRAINT FK_DangKy_TTDD FOREIGN KEY (MaTTDD) REFERENCES dbo.TrangThaiDiemDanh(MaTTDD),
    CONSTRAINT FK_DangKy_NguoiDuyet FOREIGN KEY (NguoiDuyet) REFERENCES dbo.NguoiDung(MaNguoiDung),
    CONSTRAINT UQ_DangKy_SuKien_NguoiDung UNIQUE (MaSuKien, MaNguoiDung)
);

CREATE TABLE dbo.NhatKyHeThong (
    MaNhatKy nvarchar(10) NOT NULL CONSTRAINT PK_NhatKyHeThong PRIMARY KEY,
    MaNguoiDung nvarchar(10) NULL,
    ChiTiet nvarchar(max) NOT NULL,
    ThoiGian datetime NOT NULL CONSTRAINT DF_NhatKyHeThong_ThoiGian DEFAULT(GETDATE()),
    CONSTRAINT FK_NhatKy_NguoiDung FOREIGN KEY (MaNguoiDung) REFERENCES dbo.NguoiDung(MaNguoiDung) ON DELETE SET NULL
);

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
GO

CREATE INDEX IX_LopHoc_Khoa ON dbo.LopHoc(MaKhoa, KhoaHoc, TenLop);
CREATE INDEX IX_NguoiDung_Lop_Quyen ON dbo.NguoiDung(MaLop, MaQuyen);
CREATE INDEX IX_SuKien_Cha ON dbo.SuKien(MaSuKienCha);
CREATE INDEX IX_SuKien_ThoiGian ON dbo.SuKien(MaNamHoc, MaHocKy, ThoiGianBatDau);
CREATE INDEX IX_DangKy_SuKien_DiemDanh ON dbo.DangKy_DiemDanh(MaSuKien, ThoiGianDiemDanh);
CREATE INDEX IX_ThongBao_NguoiNhan_DaDoc ON dbo.ThongBao(MaNguoiNhan, DaDoc, ThoiGianTao DESC);
CREATE INDEX IX_ThongBao_Loai_LienQuan ON dbo.ThongBao(Loai, MaLienQuan);
GO

CREATE TRIGGER dbo.TRG_CapNhatSoLuongDangKy
ON dbo.DangKy_DiemDanh
AFTER INSERT, DELETE
AS
BEGIN
    SET NOCOUNT ON;

    ;WITH affected AS (
        SELECT MaSuKien FROM inserted
        UNION
        SELECT MaSuKien FROM deleted
    )
    UPDATE sk
    SET SoLuongDaDangKy = (
        SELECT COUNT(1)
        FROM dbo.DangKy_DiemDanh dk
        WHERE dk.MaSuKien = sk.MaSuKien
    )
    FROM dbo.SuKien sk
    INNER JOIN affected a ON a.MaSuKien = sk.MaSuKien;
END;
GO

INSERT INTO dbo.TrangThaiDangKy (MaTTDK, TenTTDK) VALUES
(N'TTDK01', N'Chờ duyệt'),
(N'TTDK02', N'Đã duyệt'),
(N'TTDK03', N'Từ chối'),
(N'TTDK04', N'Đã hủy bởi người dùng'),
(N'TTDK05', N'Trong danh sách chờ'),
(N'TTDK06', N'Đã xác nhận tham gia'),
(N'TTDK07', N'Đã tham gia'),
(N'TTDK08', N'Vắng mặt'),
(N'TTDK09', N'Hết hạn đăng ký'),
(N'TTDK10', N'Đã hủy bởi hệ thống');

INSERT INTO dbo.TrangThaiSuKien (MaTTSK, TenTTSK) VALUES
(N'TTSK01', N'Bản nháp'),
(N'TTSK02', N'Chờ kiểm duyệt'),
(N'TTSK03', N'Đã được phê duyệt'),
(N'TTSK04', N'Đang mở đăng ký'),
(N'TTSK05', N'Đã đóng đăng ký'),
(N'TTSK06', N'Đang diễn ra'),
(N'TTSK07', N'Đã kết thúc'),
(N'TTSK08', N'Bị hủy bỏ'),
(N'TTSK09', N'Đã tạm hoãn'),
(N'TTSK10', N'Đã lưu trữ');

INSERT INTO dbo.TrangThaiDiemDanh (MaTTDD, TenTTDD) VALUES
(N'TTDD01', N'Chưa điểm danh'),
(N'TTDD02', N'Có mặt'),
(N'TTDD03', N'Vắng mặt không phép'),
(N'TTDD04', N'Vắng mặt có phép'),
(N'TTDD05', N'Đi trễ'),
(N'TTDD06', N'Về sớm'),
(N'TTDD07', N'Chờ duyệt minh chứng'),
(N'TTDD08', N'Minh chứng hợp lệ'),
(N'TTDD09', N'Minh chứng bị từ chối'),
(N'TTDD10', N'Hệ thống tự động đánh vắng');

INSERT INTO dbo.ThamSoQuyDinh (MaTS, TenTS, GiaTri, DonViTinh, TinhTrang) VALUES
(N'TS01', N'Điểm rèn luyện tối đa mỗi học kỳ', 100, N'Điểm', 1),
(N'TS02', N'Số sự kiện tối đa đăng ký cùng lúc', 10, N'Sự kiện', 1),
(N'TS03', N'Thời gian mở đăng ký trước sự kiện', 7, N'Ngày', 1),
(N'TS04', N'Thời gian cho phép điểm danh trễ', 15, N'Phút', 1),
(N'TS05', N'Dung lượng tối đa file minh chứng', 10, N'MB', 1),
(N'TS06', N'Thời hạn nộp minh chứng sau sự kiện', 48, N'Giờ', 1),
(N'TS07', N'Điểm trừ khi vắng không phép', -2, N'Điểm', 1),
(N'TS08', N'Thời gian tối đa xét duyệt minh chứng', 5, N'Ngày', 1),
(N'TS09', N'Số ngày lưu trữ log hệ thống', 365, N'Ngày', 1),
(N'TS10', N'Thời gian tự động nhắc nhở trước sự kiện', 24, N'Giờ', 1),
(N'TS11', N'Thời gian hiệu lực mã QR điểm danh', 2, N'Phút', 1),
(N'TS12', N'Số giờ gửi email nhắc nhở trước sự kiện', 24, N'Giờ', 1);

INSERT INTO dbo.QuyenHan (MaQuyen, TenQuyen, MoTa) VALUES
(N'Q01', N'Quản trị viên hệ thống', N'Toàn quyền kiểm soát hệ thống'),
(N'Q02', N'Sinh viên', N'Người dùng cơ bản tham gia sự kiện'),
(N'Q03', N'Quản lý khoa', N'Quản lý các sự kiện cấp khoa'),
(N'Q04', N'Lớp trưởng', N'Quản lý sự kiện cấp lớp và duyệt điểm danh'),
(N'Q05', N'Cán bộ Đoàn Hội', N'Tạo và quản lý phong trào, sự kiện Đoàn Khoa/Liên Chi Hội'),
(N'Q06', N'Giảng viên Hướng dẫn', N'Theo dõi và đánh giá sinh viên'),
(N'Q07', N'Lớp phó', N'Hỗ trợ lớp trưởng quản lý sự kiện cấp lớp'),
(N'Q08', N'Bí thư Chi đoàn', N'Phụ trách Đoàn thanh niên của lớp'),
(N'Q09', N'Phó Bí thư Chi đoàn', N'Hỗ trợ Bí thư Chi đoàn'),
(N'Q10', N'Ủy viên BCH Chi đoàn', N'Thành viên ban chấp hành hỗ trợ phong trào');

INSERT INTO dbo.Khoa (MaKhoa, TenKhoa) VALUES
(N'KCNTT', N'Khoa Công nghệ Thông tin'),
(N'KNN', N'Khoa Ngoại ngữ'),
(N'KL', N'Khoa Luật'),
(N'KCK', N'Khoa Cơ khí'),
(N'KXD', N'Khoa Xây dựng'),
(N'KDDT', N'Khoa Điện và Điện tử'),
(N'KDL', N'Khoa Du lịch'),
(N'KKT', N'Khoa Kinh tế'),
(N'KTS', N'Khoa Thủy sản'),
(N'KCNTP', N'Khoa Công nghệ Thực phẩm');

INSERT INTO dbo.HocKy (MaHocKy, TenHocKy, TuNgay, DenNgay) VALUES
('101', N'Học kỳ 1', '2025-08-15', '2026-01-14'),
('102', N'Học kỳ 2', '2026-01-15', '2026-06-30'),
('103', N'Học kỳ 3', '2026-07-01', '2026-08-14');

INSERT INTO dbo.NamHoc (MaNamHoc, TenNamHoc, TuNgay, DenNgay) VALUES
('23-24', N'Năm học 2023-2024', '2023-08-15', '2024-08-14'),
('24-25', N'Năm học 2024-2025', '2024-08-15', '2025-08-14'),
('25-26', N'Năm học 2025-2026', '2025-08-15', '2026-08-14'),
('26-27', N'Năm học 2026-2027', '2026-08-15', '2027-08-14');

INSERT INTO dbo.DanhMucDiem (MaDanhMuc, TenDanhMuc, DiemToiDa, MaDanhMucCha, GhiChu) VALUES
(N'DM01', N'I. Ý thức học tập của sinh viên', 20, NULL, N'Mục I'),
(N'DM02', N'II. Chấp hành nội quy, quy chế nhà trường', 25, NULL, N'Mục II'),
(N'DM03', N'III. Tham gia hoạt động rèn luyện chính trị, xã hội, văn hóa, thể thao', 20, NULL, N'Mục III'),
(N'DM04', N'IV. Phẩm chất công dân và quan hệ cộng đồng', 25, NULL, N'Mục IV'),
(N'DM05', N'V. Tham gia hoạt động đoàn thể, tổ chức', 10, NULL, N'Mục V');

INSERT INTO dbo.DanhMucDiem (MaDanhMuc, TenDanhMuc, DiemToiDa, MaDanhMucCha, GhiChu) VALUES
(N'DM01_1', N'1.1 Kết quả học tập trong học kỳ', 4, N'DM01', N'Tối đa 4 điểm'),
(N'DM01_2', N'1.2 Tham gia hoạt động học thuật', 10, N'DM01', N'Tối đa 10 điểm'),
(N'DM02_1', N'2.1 Chấp hành nội quy, quy chế', 25, N'DM02', N'Tối đa 25 điểm'),
(N'DM03_1', N'3.1 Chào cờ, sinh hoạt lớp, Đoàn', 5, N'DM03', N'Tối đa 5 điểm'),
(N'DM03_2', N'3.2 Không vi phạm ATGT, tệ nạn xã hội', 10, N'DM03', N'Tối đa 10 điểm'),
(N'DM03_3', N'3.3 Thành viên tích cực CLB, đội tình nguyện', 5, N'DM03', N'Tối đa 5 điểm'),
(N'DM04_1', N'4.1 Chấp hành chủ trương, đường lối của Đảng, Nhà nước', 5, N'DM04', N'Tối đa 5 điểm'),
(N'DM04_2', N'4.2 Tham gia phong trào an ninh, phòng chống tội phạm', 5, N'DM04', N'Tối đa 5 điểm'),
(N'DM04_3', N'4.3 Lối sống lành mạnh, quan hệ cộng đồng tốt', 5, N'DM04', N'Tối đa 5 điểm'),
(N'DM04_4', N'4.4 Hoạt động tình nguyện, văn hóa văn nghệ, thể thao', 10, N'DM04', N'Tối đa 10 điểm'),
(N'DM05_1', N'5.1 Công tác tập thể lớp', 10, N'DM05', N'Tối đa 10 điểm'),
(N'DM05_2', N'5.2 Khen thưởng hoạt động', 10, N'DM05', N'Tối đa 10 điểm');

INSERT INTO dbo.MucDiemRenLuyen (MaMucDRL, TenMucDRL, Diem, MaDanhMuc) VALUES
(N'MDRL01', N'Kết quả học tập: đạt tất cả học phần', 4, N'DM01_1'),
(N'MDRL02', N'Tham gia tọa đàm, hội nghị, hội thảo khoa học', 2, N'DM01_2'),
(N'MDRL03', N'Là thành viên tích cực câu lạc bộ học thuật', 5, N'DM01_2'),
(N'MDRL04', N'Tham gia cuộc thi học thuật', 2, N'DM01_2'),
(N'MDRL05', N'Không vi phạm nội quy, quy định, quy chế', 25, N'DM02_1'),
(N'MDRL06', N'Tham gia đầy đủ chào cờ, sinh hoạt lớp, Đoàn', 5, N'DM03_1'),
(N'MDRL07', N'Vắng một buổi sinh hoạt lớp, Đoàn, học chính trị', -2, N'DM03_1'),
(N'MDRL08', N'Không vi phạm luật an toàn giao thông, tệ nạn xã hội', 10, N'DM03_2'),
(N'MDRL09', N'Thành viên tích cực đội tình nguyện, CLB sở thích, thể thao', 5, N'DM03_3'),
(N'MDRL10', N'Chấp hành nghiêm chủ trương, đường lối của Đảng, Nhà nước', 5, N'DM04_1'),
(N'MDRL11', N'Tham gia hoạt động tình nguyện, văn hóa văn nghệ, thể thao', 2, N'DM04_4'),
(N'MDRL12', N'Tham gia chiến dịch tình nguyện hè', 10, N'DM04_4'),
(N'MDRL13', N'Thành viên chi hội, chi đoàn hoàn thành tốt nhiệm vụ', 4, N'DM05_1'),
(N'MDRL14', N'BCS lớp, BCH chi đoàn hoàn thành tốt nhiệm vụ', 8, N'DM05_1'),
(N'MDRL15', N'BCS lớp, BCH chi đoàn hoàn thành xuất sắc nhiệm vụ', 10, N'DM05_1'),
(N'MDRL16', N'Được khen thưởng cấp Khoa', 3, N'DM05_2'),
(N'MDRL17', N'Được khen thưởng cấp Trường trở lên', 5, N'DM05_2');
GO

DECLARE @PasswordHash nvarchar(255) = N'8d969eef6ecad3c29a3a629280e686cf0c3f5d5a86aff3ca12020c923adc6c92';

INSERT INTO dbo.LopHoc (MaLop, TenLop, MaKhoa, KhoaHoc)
SELECT v.MaKhoa + CAST(k.KhoaSo AS nvarchar(2)) + CAST(l.LopSo AS nvarchar(1)),
       CAST(k.KhoaSo AS nvarchar(2)) + N'.' + v.TenNgan + N'-' + CAST(l.LopSo AS nvarchar(1)),
       v.MaKhoa,
       N'K' + CAST(k.KhoaSo AS nvarchar(2))
FROM (VALUES
    (N'CNTT', N'KCNTT'),
    (N'NNA', N'KNN'),
    (N'LU', N'KL'),
    (N'CK', N'KCK'),
    (N'XD', N'KXD'),
    (N'DDT', N'KDDT'),
    (N'DL', N'KDL'),
    (N'KT', N'KKT'),
    (N'TS', N'KTS'),
    (N'TP', N'KCNTP')
) v(TenNgan, MaKhoa)
CROSS JOIN (VALUES (63), (64), (65), (66)) k(KhoaSo)
CROSS JOIN (VALUES (1)) l(LopSo);

INSERT INTO dbo.LopHoc (MaLop, TenLop, MaKhoa, KhoaHoc) VALUES
(N'65CNTT1', N'65.CNTT-1', N'KCNTT', N'K65'),
(N'65CNTT2', N'65.CNTT-2', N'KCNTT', N'K65'),
(N'64CNTT1', N'64.CNTT-1', N'KCNTT', N'K64'),
(N'63CNTT1', N'63.CNTT-1', N'KCNTT', N'K63'),
(N'65NNA1', N'65.NNA-1', N'KNN', N'K65'),
(N'65NNA2', N'65.NNA-2', N'KNN', N'K65'),
(N'65LUAT1', N'65.LUAT-1', N'KL', N'K65'),
(N'65KETOAN', N'65.KETOAN-1', N'KKT', N'K65'),
(N'65QTDL', N'65.QTDL-1', N'KDL', N'K65'),
(N'64NTTS', N'64.NTTS-1', N'KTS', N'K64');

INSERT INTO dbo.NguoiDung (MaNguoiDung, MaSinhVien, MatKhau, HoTen, NgaySinh, Email, MaLop, MaQuyen) VALUES
(N'ND0001', N'ADMIN01', @PasswordHash, N'Admin Hệ thống NTU', '1990-05-15', N'admin @ntu.edu.vn', NULL, N'Q01'),
(N'ND0002', N'GV0001', @PasswordHash, N'ThS. Bùi Chí Thành', '1985-08-20', N'thanhbc @ntu.edu.vn', NULL, N'Q06');

INSERT INTO dbo.NguoiDung (MaNguoiDung, MaSinhVien, MatKhau, HoTen, NgaySinh, Email, MaLop, MaQuyen) VALUES
(N'ND001', N'ADMIN01_OLD', @PasswordHash, N'Admin Hệ Thống NTU', '1990-05-15', N'admin.old @ntu.edu.vn', NULL, N'Q01'),
(N'ND002', N'GV130001', @PasswordHash, N'ThS. Bùi Chí Thành', '1985-08-20', N'thanhbc.old @ntu.edu.vn', NULL, N'Q06'),
(N'ND003', N'LCHCNTT', @PasswordHash, N'Liên Chi Hội Khoa CNTT', '1995-12-12', N'lch.cntt @ntu.edu.vn', N'65CNTT1', N'Q05'),
(N'ND004', N'DTNNTU', @PasswordHash, N'Đoàn Thanh Niên NTU', '1992-10-10', N'doantn @ntu.edu.vn', N'65CNTT1', N'Q03'),
(N'ND005', N'65130387', @PasswordHash, N'Võ Chí Danh', '2005-09-26', N'danh.vc.65 @ntu.edu.vn', N'65CNTT1', N'Q02'),
(N'ND006', N'65131734', @PasswordHash, N'Nguyễn Lê Thùy Linh', '2005-04-12', N'linh.nlt.65 @ntu.edu.vn', N'65CNTT1', N'Q04'),
(N'ND007', N'65130902', @PasswordHash, N'Nguyễn Lê Phi Hào', '2005-07-30', N'hao.nlp.65 @ntu.edu.vn', N'65CNTT1', N'Q08'),
(N'ND008', N'65130101', @PasswordHash, N'Trần Minh Hoàng', '2005-02-14', N'hoang.tm.65 @ntu.edu.vn', N'65CNTT1', N'Q02'),
(N'ND009', N'65130102', @PasswordHash, N'Lê Thị Bảo Trâm', '2005-11-05', N'tram.ltb.65 @ntu.edu.vn', N'65CNTT1', N'Q02'),
(N'ND010', N'65130103', @PasswordHash, N'Phạm Quốc Tuấn', '2005-08-19', N'tuan.pq.65 @ntu.edu.vn', N'65CNTT1', N'Q02'),
(N'ND011', N'65130201', @PasswordHash, N'Hoàng Anh Dũng', '2005-03-25', N'dung.ha.65 @ntu.edu.vn', N'65CNTT2', N'Q04'),
(N'ND012', N'65130202', @PasswordHash, N'Đặng Thanh Thảo', '2005-06-10', N'thao.dt.65 @ntu.edu.vn', N'65CNTT2', N'Q02'),
(N'ND013', N'65130203', @PasswordHash, N'Bùi Khắc Đạt', '2005-09-02', N'dat.bk.65 @ntu.edu.vn', N'65CNTT2', N'Q02'),
(N'ND014', N'64130010', @PasswordHash, N'Phạm Quốc Huy', '2004-03-08', N'huy.pq.64 @ntu.edu.vn', N'64CNTT1', N'Q04'),
(N'ND015', N'64130011', @PasswordHash, N'Ngô Hữu Phước', '2004-12-20', N'phuoc.nh.64 @ntu.edu.vn', N'64CNTT1', N'Q02'),
(N'ND016', N'65140001', @PasswordHash, N'Trần Tuấn Anh', '2005-01-15', N'anh.tt.65 @ntu.edu.vn', N'65NNA1', N'Q04'),
(N'ND017', N'65140002', @PasswordHash, N'Lê Mai Phương', '2005-11-22', N'phuong.lm.65 @ntu.edu.vn', N'65NNA1', N'Q02'),
(N'ND018', N'65150001', @PasswordHash, N'Nguyễn Trọng Đại', '2005-05-18', N'dai.nt.65 @ntu.edu.vn', N'65KETOAN', N'Q04'),
(N'ND019', N'65160001', @PasswordHash, N'Vũ Hoàng Yến', '2005-07-07', N'yen.vh.65 @ntu.edu.vn', N'65QTDL', N'Q02'),
(N'ND020', N'65170001', @PasswordHash, N'Châu Văn Hùng', '2005-08-30', N'hung.cv.65 @ntu.edu.vn', N'65LUAT1', N'Q02'),
(N'ND021', N'65130388', @PasswordHash, N'Nguyễn Minh Khang', '2005-05-09', N'khang.nm.65 @ntu.edu.vn', N'65CNTT1', N'Q02'),
(N'ND022', N'65130389', @PasswordHash, N'Lê Hà My', '2005-10-21', N'my.lh.65 @ntu.edu.vn', N'65CNTT1', N'Q02'),
(N'ND023', N'65130390', @PasswordHash, N'Trần Gia Bảo', '2005-02-03', N'bao.tg.65 @ntu.edu.vn', N'65CNTT1', N'Q02');

;WITH faculty_users AS (
    SELECT ROW_NUMBER() OVER (ORDER BY k.MaKhoa) AS rn, k.MaKhoa, k.TenKhoa,
           (SELECT TOP 1 MaLop FROM dbo.LopHoc l WHERE l.MaKhoa = k.MaKhoa ORDER BY l.KhoaHoc, l.TenLop) AS MaLop
    FROM dbo.Khoa k
)
INSERT INTO dbo.NguoiDung (MaNguoiDung, MaSinhVien, MatKhau, HoTen, NgaySinh, Email, MaLop, MaQuyen)
SELECT N'ND00' + RIGHT(N'00' + CAST(rn + 2 AS nvarchar(3)), 2),
       N'LCH' + MaKhoa,
       @PasswordHash,
       N'Liên Chi Hội ' + TenKhoa,
       '1995-01-01',
       LOWER(N'lch.' + MaKhoa + N' @ntu.edu.vn'),
       MaLop,
       CASE WHEN rn % 2 = 0 THEN N'Q03' ELSE N'Q05' END
FROM faculty_users;

DECLARE @UserSeq int = 101;
DECLARE @MaLop nvarchar(10);
DECLARE lop_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MaLop FROM dbo.LopHoc ORDER BY MaKhoa, KhoaHoc, TenLop;
OPEN lop_cursor;
FETCH NEXT FROM lop_cursor INTO @MaLop;
WHILE @@FETCH_STATUS = 0
BEGIN
    DECLARE @Views_NamHocs_Details_cshtml int = 1;
    WHILE @Views_NamHocs_Details_cshtml <= 12
    BEGIN
        DECLARE @MaNguoiDung nvarchar(10) = N'ND' + RIGHT(N'0000' + CAST(@UserSeq AS nvarchar(4)), 4);
        DECLARE @MaSV nvarchar(50) = REPLACE(@MaLop, N'K', N'') + RIGHT(N'00' + CAST(@Views_NamHocs_Details_cshtml AS nvarchar(2)), 2);
        DECLARE @Filters_RoleAuthorizeAttribute_cs nvarchar(10) = CASE WHEN @Views_NamHocs_Details_cshtml = 1 THEN N'Q04' WHEN @Views_NamHocs_Details_cshtml = 2 THEN N'Q08' WHEN @Views_NamHocs_Details_cshtml = 3 THEN N'Q07' ELSE N'Q02' END;

        INSERT INTO dbo.NguoiDung (MaNguoiDung, MaSinhVien, MatKhau, HoTen, NgaySinh, Email, MaLop, MaQuyen)
        VALUES (
            @MaNguoiDung,
            @MaSV,
            @PasswordHash,
            CASE
                WHEN @Views_NamHocs_Details_cshtml = 1 THEN N'Lớp trưởng ' + @MaLop
                WHEN @Views_NamHocs_Details_cshtml = 2 THEN N'Bí thư ' + @MaLop
                WHEN @Views_NamHocs_Details_cshtml = 3 THEN N'Lớp phó ' + @MaLop
                ELSE N'Sinh viên ' + @MaLop + N' số ' + CAST(@Views_NamHocs_Details_cshtml AS nvarchar(2))
            END,
            DATEADD(DAY, @Views_NamHocs_Details_cshtml * 13, '2004-01-01'),
            LOWER(@MaNguoiDung + N' @sv.ntu.edu.vn'),
            @MaLop,
            @Filters_RoleAuthorizeAttribute_cs
        );

        SET @UserSeq += 1;
        SET @Views_NamHocs_Details_cshtml += 1;
    END

    FETCH NEXT FROM lop_cursor INTO @MaLop;
END
CLOSE lop_cursor;
DEALLOCATE lop_cursor;
GO

DECLARE @EventSeq int = 1;
DECLARE @RegistrationSeq int = 1;
DECLARE @LogSeq int = 1;
DECLARE @KhoaId nvarchar(10);
DECLARE @KhoaName nvarchar(255);
DECLARE @Manager nvarchar(10);
DECLARE @ParentEvent nvarchar(10);
DECLARE @FacultyEvent nvarchar(10);

DECLARE khoa_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT MaKhoa, TenKhoa FROM dbo.Khoa ORDER BY MaKhoa;
OPEN khoa_cursor;
FETCH NEXT FROM khoa_cursor INTO @KhoaId, @KhoaName;
WHILE @@FETCH_STATUS = 0
BEGIN
    SELECT TOP 1 @Manager = MaNguoiDung
    FROM dbo.NguoiDung nd
    INNER JOIN dbo.LopHoc lh ON lh.MaLop = nd.MaLop
    WHERE lh.MaKhoa = @KhoaId AND nd.MaQuyen IN (N'Q03', N'Q05')
    ORDER BY nd.MaNguoiDung;

    SET @ParentEvent = N'SK' + RIGHT(N'0000' + CAST(@EventSeq AS nvarchar(4)), 4);
    SET @EventSeq += 1;
    INSERT INTO dbo.SuKien (MaSuKien, TenSuKien, Noidung, ThoiGianBatDau, ThoiGianKetThuc, MaHocKy, MaNamHoc, DiaDiem, SoLuongToiDa, MaMucDRL, CapToChuc, MaNguoiTao, MaTTSK)
    VALUES (@ParentEvent, N'Tuần sinh hoạt công dân ' + @KhoaName + N' năm học 2025-2026', N'Sự kiện cha cấp khoa, các lớp tạo sự kiện con để điểm danh riêng nhưng báo cáo khoa gom thành một cột.', '2026-06-01 07:30:00', '2026-06-01 11:30:00', '102', '25-26', N'Hội trường trung tâm', 1000, N'MDRL06', N'Khoa', @Manager, N'TTSK04');

    SET @FacultyEvent = N'SK' + RIGHT(N'0000' + CAST(@EventSeq AS nvarchar(4)), 4);
    SET @EventSeq += 1;
    INSERT INTO dbo.SuKien (MaSuKien, TenSuKien, Noidung, ThoiGianBatDau, ThoiGianKetThuc, MaHocKy, MaNamHoc, DiaDiem, SoLuongToiDa, MaMucDRL, CapToChuc, MaNguoiTao, MaTTSK)
    VALUES (@FacultyEvent, N'Hội thảo học thuật và định hướng nghề nghiệp ' + @KhoaName, N'Sự kiện cấp khoa độc lập để test báo cáo và xuất Excel/PDF.', '2026-06-15 08:00:00', '2026-06-15 11:30:00', '102', '25-26', N'Phòng hội thảo khoa', 300, N'MDRL02', N'Khoa', @Manager, N'TTSK04');

    DECLARE @ClassId nvarchar(10);
    DECLARE @Officer nvarchar(10);
    DECLARE class_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT MaLop FROM dbo.LopHoc WHERE MaKhoa = @KhoaId ORDER BY KhoaHoc, TenLop;
    OPEN class_cursor;
    FETCH NEXT FROM class_cursor INTO @ClassId;
    WHILE @@FETCH_STATUS = 0
    BEGIN
        SELECT TOP 1 @Officer = MaNguoiDung FROM dbo.NguoiDung WHERE MaLop = @ClassId AND MaQuyen IN (N'Q04', N'Q08') ORDER BY MaQuyen;

        DECLARE @ChildEvent nvarchar(10) = N'SK' + RIGHT(N'0000' + CAST(@EventSeq AS nvarchar(4)), 4);
        SET @EventSeq += 1;
        INSERT INTO dbo.SuKien (MaSuKien, TenSuKien, Noidung, ThoiGianBatDau, ThoiGianKetThuc, MaHocKy, MaNamHoc, DiaDiem, SoLuongToiDa, MaMucDRL, CapToChuc, MaNguoiTao, MaTTSK, MaSuKienCha)
        VALUES (@ChildEvent, N'Tuần sinh hoạt công dân - ' + @ClassId, N'Sự kiện con cấp lớp, gắn về sự kiện cha cấp khoa để báo cáo không bị nở cột.', DATEADD(DAY, ABS(CHECKSUM(@ClassId)) % 10, '2026-06-02 07:30:00'), DATEADD(DAY, ABS(CHECKSUM(@ClassId)) % 10, '2026-06-02 10:30:00'), '102', '25-26', N'Phòng sinh hoạt ' + @ClassId, 80, N'MDRL06', N'Lớp', @Officer, N'TTSK04', @ParentEvent);

        DECLARE @ClassEvent nvarchar(10) = N'SK' + RIGHT(N'0000' + CAST(@EventSeq AS nvarchar(4)), 4);
        SET @EventSeq += 1;
        INSERT INTO dbo.SuKien (MaSuKien, TenSuKien, Noidung, ThoiGianBatDau, ThoiGianKetThuc, MaHocKy, MaNamHoc, DiaDiem, SoLuongToiDa, MaMucDRL, CapToChuc, MaNguoiTao, MaTTSK)
        VALUES (@ClassEvent, N'Sinh hoạt lớp ' + @ClassId + N' - bình xét điểm rèn luyện', N'Sự kiện lớp độc lập để test phân quyền lớp trưởng/bí thư.', '2026-07-01 18:00:00', '2026-07-01 20:00:00', '103', '25-26', N'Phòng học ' + @ClassId, 80, N'MDRL14', N'Lớp', @Officer, CASE WHEN RIGHT(@ClassId, 1) IN (N'1', N'3') THEN N'TTSK02' ELSE N'TTSK04' END);

        INSERT INTO dbo.NhatKyHeThong (MaNhatKy, MaNguoiDung, ChiTiet)
        VALUES (N'LOG' + RIGHT(N'0000' + CAST(@LogSeq AS nvarchar(4)), 4), @Officer, N'Tạo dữ liệu sự kiện lớp ' + @ClassId + N' phục vụ test báo cáo và duyệt sự kiện.');
        SET @LogSeq += 1;

        DECLARE @StudentId nvarchar(10);
        DECLARE student_cursor CURSOR LOCAL FAST_FORWARD FOR
            SELECT MaNguoiDung FROM dbo.NguoiDung WHERE MaLop = @ClassId ORDER BY MaNguoiDung;
        OPEN student_cursor;
        FETCH NEXT FROM student_cursor INTO @StudentId;
        WHILE @@FETCH_STATUS = 0
        BEGIN
            IF ABS(CHECKSUM(@StudentId + @ChildEvent)) % 100 < 78
            BEGIN
                INSERT INTO dbo.DangKy_DiemDanh (MaDangKy, MaSuKien, MaNguoiDung, MaTTDK, ThoiGianDiemDanh, MaTTDD, NguoiDuyet, ThoiGianDuyet, GhiChu)
                VALUES (N'DK' + RIGHT(N'00000' + CAST(@RegistrationSeq AS nvarchar(5)), 5), @ChildEvent, @StudentId, N'TTDK07', DATEADD(MINUTE, ABS(CHECKSUM(@StudentId)) % 30, '2026-06-02 07:30:00'), N'TTDD02', @Officer, GETDATE(), N'Có mặt tại sự kiện con của lớp.');
                SET @RegistrationSeq += 1;
            END

            IF ABS(CHECKSUM(@StudentId + @FacultyEvent)) % 100 < 45
            BEGIN
                INSERT INTO dbo.DangKy_DiemDanh (MaDangKy, MaSuKien, MaNguoiDung, MaTTDK, ThoiGianDiemDanh, MaTTDD, NguoiDuyet, ThoiGianDuyet, GhiChu)
                VALUES (N'DK' + RIGHT(N'00000' + CAST(@RegistrationSeq AS nvarchar(5)), 5), @FacultyEvent, @StudentId, N'TTDK07', DATEADD(MINUTE, ABS(CHECKSUM(@StudentId)) % 45, '2026-06-15 08:00:00'), N'TTDD02', @Manager, GETDATE(), N'Có mặt tại sự kiện cấp khoa.');
                SET @RegistrationSeq += 1;
            END

            FETCH NEXT FROM student_cursor INTO @StudentId;
        END
        CLOSE student_cursor;
        DEALLOCATE student_cursor;

        FETCH NEXT FROM class_cursor INTO @ClassId;
    END
    CLOSE class_cursor;
    DEALLOCATE class_cursor;

    FETCH NEXT FROM khoa_cursor INTO @KhoaId, @KhoaName;
END
CLOSE khoa_cursor;
DEALLOCATE khoa_cursor;
GO

INSERT INTO dbo.SuKien (MaSuKien, TenSuKien, Noidung, ThoiGianBatDau, ThoiGianKetThuc, MaHocKy, MaNamHoc, DiaDiem, SoLuongToiDa, MaMucDRL, CapToChuc, MaNguoiTao, MaTTSK, LyDoHuy) VALUES
(N'SK101', N'Demo điểm danh QR động - Sinh hoạt công dân CNTT', N'Sự kiện dùng để kiểm thử luồng đăng ký và điểm danh bằng QR động.', '2026-05-01 07:00:00', '2026-12-31 17:00:00', '102', '25-26', N'Phòng G201, Khoa Công nghệ Thông tin', 80, N'MDRL06', N'Lớp', N'ND006', N'TTSK06', NULL),
(N'SK102', N'Talkshow định hướng nghề nghiệp Backend và AI 2026', N'Hoạt động học thuật cấp khoa, có email nhắc lịch và xuất danh sách đăng ký Excel/PDF.', '2026-06-05 08:00:00', '2026-06-05 11:30:00', '102', '25-26', N'Hội trường số 2', 250, N'MDRL02', N'Khoa', N'ND003', N'TTSK04', NULL),
(N'SK103', N'Sinh hoạt lớp 65.CNTT-1 - Bình xét điểm rèn luyện cuối kỳ', N'Sự kiện cấp lớp đang chờ Liên Chi Hội/Đoàn Khoa phê duyệt.', '2026-06-12 18:00:00', '2026-06-12 20:00:00', '102', '25-26', N'Phòng G202', 65, N'MDRL06', N'Lớp', N'ND006', N'TTSK02', NULL),
(N'SK104', N'Ngày Chủ nhật xanh - Khuôn viên NTU', N'Dữ liệu đã hoàn thành để kiểm thử điểm rèn luyện, duyệt minh chứng và báo cáo.', '2026-04-21 07:00:00', '2026-04-21 10:30:00', '102', '25-26', N'Khuôn viên Trường Đại học Nha Trang', 300, N'MDRL11', N'Khoa', N'ND003', N'TTSK07', NULL),
(N'SK105', N'Giải bóng đá sinh viên CNTT 2026', N'Sự kiện bị hủy để kiểm thử trạng thái hủy và lý do hủy.', '2026-05-28 15:00:00', '2026-05-30 18:00:00', '102', '25-26', N'Sân bóng đá mini NTU', 120, N'MDRL11', N'Khoa', N'ND003', N'TTSK08', N'Trùng lịch thi học kỳ của nhiều lớp.');

INSERT INTO dbo.DangKy_DiemDanh (MaDangKy, MaSuKien, MaNguoiDung, ThoiGianDangKy, MaTTDK, ThoiGianDiemDanh, MinhChung, MaTTDD, NguoiDuyet, ThoiGianDuyet, GhiChu) VALUES
(N'DK101', N'SK101', N'ND005', '2026-05-10 08:00:00', N'TTDK06', NULL, N'QR:SK101:20260513131000', N'TTDD07', NULL, NULL, N'Sinh viên đã quét QR, chờ cán bộ duyệt điểm danh.'),
(N'DK102', N'SK101', N'ND008', '2026-05-10 08:10:00', N'TTDK06', NULL, N'QR:SK101:20260513131200', N'TTDD07', NULL, NULL, N'Sinh viên đã quét QR, chờ cán bộ duyệt điểm danh.'),
(N'DK103', N'SK104', N'ND005', '2026-04-18 09:00:00', N'TTDK07', '2026-04-21 06:55:00', N'/Uploads/MinhChung/ngay-chu-nhat-xanh-01.jpg', N'TTDD08', N'ND003', '2026-04-21 11:00:00', N'Minh chứng hợp lệ, cộng điểm tình nguyện.'),
(N'DK104', N'SK104', N'ND009', '2026-04-18 09:05:00', N'TTDK06', NULL, N'QRPHOTO:/Content/MinhChungDiemDanh/test-dk104.jpg', N'TTDD07', NULL, NULL, N'Ảnh minh chứng đang chờ Đoàn Khoa/LCH duyệt từ thông báo.'),
(N'DK105', N'SK102', N'ND021', '2026-05-11 10:00:00', N'TTDK06', NULL, NULL, N'TTDD01', NULL, NULL, N'Dùng để test gửi email nhắc nhở và xuất danh sách.');

INSERT INTO dbo.ThongBao (MaThongBao, MaNguoiNhan, TieuDe, NoiDung, DuongDan, Loai, MaLienQuan, DaDoc, ThoiGianTao) VALUES
(N'TBSEED0001', N'ND003', N'Có sự kiện mới cần duyệt', N'Sự kiện "Sinh hoạt lớp 65.CNTT-1 - Bình xét điểm rèn luyện cuối kỳ" đang chờ kiểm duyệt.', N'/SuKiens#event-SK103', N'EVENT_APPROVAL_NEEDED', N'SK103', 0, GETDATE()),
(N'TBSEED0002', N'ND003', N'Có minh chứng điểm danh chờ duyệt', N'Sinh viên gửi ảnh minh chứng cho sự kiện "Ngày Chủ nhật xanh - Khuôn viên NTU".', N'/DangKy_DiemDanh#dk-DK104', N'ATTENDANCE_PENDING', N'DK104', 0, GETDATE()),
(N'TBSEED0003', N'ND005', N'Sự kiện sắp diễn ra', N'Sự kiện demo điểm danh QR động sắp diễn ra, dùng để test thông báo sinh viên.', N'/SinhVien/LichCaNhan', N'EVENT_UPCOMING', N'SK101:202612310700', 0, GETDATE());

INSERT INTO dbo.NhatKyHeThong (MaNhatKy, MaNguoiDung, ChiTiet, ThoiGian) VALUES
(N'LOG9001', N'ND0001', N'Khởi tạo database demo đầy đủ nhiều khoa, lớp, sinh viên, sự kiện cha/con.', GETDATE()),
(N'LOG9002', N'ND0004', N'Tạo dữ liệu test duyệt sự kiện lớp và duyệt minh chứng từ thông báo.', GETDATE());
GO

-- SỬA LẠI ĐOẠT CODE NÀY Ở CUỐI FILE SQL CỦA BẠN
UPDATE sk
SET SoLuongDaDangKy = ISNULL(x.SoLuong, 0) -- Chuyển NULL thành 0 nếu chưa có ai đăng ký
FROM dbo.SuKien sk
LEFT JOIN (
    SELECT MaSuKien, COUNT(1) AS SoLuong
    FROM dbo.DangKy_DiemDanh
    GROUP BY MaSuKien
) x ON x.MaSuKien = sk.MaSuKien;
GO

PRINT N'Đã tạo database QuanLy_HDNK đầy đủ.';
GO