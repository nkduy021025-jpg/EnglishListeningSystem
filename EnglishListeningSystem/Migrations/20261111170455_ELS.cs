using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishListeningSystem.Migrations
{
    /// <inheritdoc />
    public partial class ELS : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "DanhMuc",
                columns: table => new
                {
                    MaDM = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenDM = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MoTa = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThaiHienThi = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhMuc", x => x.MaDM);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TaiKhoan",
                columns: table => new
                {
                    MaTK = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    TenDangNhap = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    MatKhau = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    HoTen = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    Email = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    VaiTro = table.Column<string>(type: "varchar(20)", maxLength: 20, nullable: false, defaultValue: "Student")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    DiemTichLuy = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    TrangThaiTK = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TaiKhoan", x => x.MaTK);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "BaiHoc",
                columns: table => new
                {
                    MaBH = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaDM = table.Column<int>(type: "int", nullable: false),
                    MaTK = table.Column<int>(type: "int", nullable: true),
                    TenBH = table.Column<string>(type: "varchar(200)", maxLength: 200, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CapDo = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    AudioUrl = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    YoutubeUrl = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayTao = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BaiHoc", x => x.MaBH);
                    table.ForeignKey(
                        name: "FK_BaiHoc_DanhMuc",
                        column: x => x.MaDM,
                        principalTable: "DanhMuc",
                        principalColumn: "MaDM",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_BaiHoc_TaiKhoan",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.SetNull);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LoTrinh",
                columns: table => new
                {
                    MaLT = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    MaDM = table.Column<int>(type: "int", nullable: true),
                    TenLT = table.Column<string>(type: "varchar(150)", maxLength: 150, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThaiLT = table.Column<string>(type: "varchar(50)", maxLength: 50, nullable: true, defaultValue: "Đang học")
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    CauHienTai = table.Column<int>(type: "int", nullable: false, defaultValue: 1),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LoTrinh", x => x.MaLT);
                    table.ForeignKey(
                        name: "FK_LoTrinh_DanhMuc",
                        column: x => x.MaDM,
                        principalTable: "DanhMuc",
                        principalColumn: "MaDM",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_LoTrinh_TaiKhoan",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "CauChep",
                columns: table => new
                {
                    MaCau = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaBH = table.Column<int>(type: "int", nullable: false),
                    ThuTuCau = table.Column<int>(type: "int", nullable: false),
                    NoiDungChuan = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ThoiGianBatDau = table.Column<double>(type: "double", nullable: false),
                    ThoiGianKetThuc = table.Column<double>(type: "double", nullable: false),
                    GoiY = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThaiDuyet = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CauChep", x => x.MaCau);
                    table.ForeignKey(
                        name: "FK_CauChep_BaiHoc",
                        column: x => x.MaBH,
                        principalTable: "BaiHoc",
                        principalColumn: "MaBH",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "GhiChu",
                columns: table => new
                {
                    MaGC = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    MaBH = table.Column<int>(type: "int", nullable: false),
                    NoiDungGC = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayTaoGC = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_GhiChu", x => x.MaGC);
                    table.ForeignKey(
                        name: "FK_GhiChu_BaiHoc",
                        column: x => x.MaBH,
                        principalTable: "BaiHoc",
                        principalColumn: "MaBH",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_GhiChu_TaiKhoan",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TienDoBaiHoc",
                columns: table => new
                {
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    MaBH = table.Column<int>(type: "int", nullable: false),
                    MucDoHoanThanh = table.Column<decimal>(type: "decimal(5,2)", nullable: false, defaultValue: 0m),
                    NgayCapNhat = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TienDoBaiHoc", x => new { x.MaTK, x.MaBH });
                    table.ForeignKey(
                        name: "FK_TienDoBaiHoc_BaiHoc",
                        column: x => x.MaBH,
                        principalTable: "BaiHoc",
                        principalColumn: "MaBH",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_TienDoBaiHoc_TaiKhoan",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "TuVung",
                columns: table => new
                {
                    MaTV = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    MaBH = table.Column<int>(type: "int", nullable: true),
                    TuTiengAnh = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    PhienAm = table.Column<string>(type: "varchar(100)", maxLength: 100, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NghiaNguCanh = table.Column<string>(type: "longtext", nullable: false)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    ViDu = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TrangThaiOnTap = table.Column<bool>(type: "tinyint(1)", nullable: false, defaultValue: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TuVung", x => x.MaTV);
                    table.ForeignKey(
                        name: "FK_TuVung_BaiHoc",
                        column: x => x.MaBH,
                        principalTable: "BaiHoc",
                        principalColumn: "MaBH",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_TuVung_TaiKhoan",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "LamBai",
                columns: table => new
                {
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    MaCau = table.Column<int>(type: "int", nullable: false),
                    Lan = table.Column<int>(type: "int", nullable: false),
                    TuVietSai = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    TuDung = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    SoLanSai = table.Column<int>(type: "int", nullable: false, defaultValue: 0),
                    GoiYCaiThien = table.Column<string>(type: "longtext", nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayLam = table.Column<DateTime>(type: "datetime", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LamBai", x => new { x.MaTK, x.MaCau, x.Lan });
                    table.ForeignKey(
                        name: "FK_LamBai_CauChep",
                        column: x => x.MaCau,
                        principalTable: "CauChep",
                        principalColumn: "MaCau",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_LamBai_TaiKhoan",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_BaiHoc_MaDM",
                table: "BaiHoc",
                column: "MaDM");

            migrationBuilder.CreateIndex(
                name: "IX_BaiHoc_MaTK",
                table: "BaiHoc",
                column: "MaTK");

            migrationBuilder.CreateIndex(
                name: "IX_CauChep_MaBH",
                table: "CauChep",
                column: "MaBH");

            migrationBuilder.CreateIndex(
                name: "IX_GhiChu_MaBH",
                table: "GhiChu",
                column: "MaBH");

            migrationBuilder.CreateIndex(
                name: "IX_GhiChu_MaTK",
                table: "GhiChu",
                column: "MaTK");

            migrationBuilder.CreateIndex(
                name: "IX_LamBai_MaCau",
                table: "LamBai",
                column: "MaCau");

            migrationBuilder.CreateIndex(
                name: "IX_LoTrinh_MaDM",
                table: "LoTrinh",
                column: "MaDM");

            migrationBuilder.CreateIndex(
                name: "IX_LoTrinh_MaTK",
                table: "LoTrinh",
                column: "MaTK");

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_Email",
                table: "TaiKhoan",
                column: "Email",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TaiKhoan_TenDangNhap",
                table: "TaiKhoan",
                column: "TenDangNhap",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_TienDoBaiHoc_MaBH",
                table: "TienDoBaiHoc",
                column: "MaBH");

            migrationBuilder.CreateIndex(
                name: "IX_TuVung_MaBH",
                table: "TuVung",
                column: "MaBH");

            migrationBuilder.CreateIndex(
                name: "IX_TuVung_MaTK",
                table: "TuVung",
                column: "MaTK");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "GhiChu");

            migrationBuilder.DropTable(
                name: "LamBai");

            migrationBuilder.DropTable(
                name: "LoTrinh");

            migrationBuilder.DropTable(
                name: "TienDoBaiHoc");

            migrationBuilder.DropTable(
                name: "TuVung");

            migrationBuilder.DropTable(
                name: "CauChep");

            migrationBuilder.DropTable(
                name: "BaiHoc");

            migrationBuilder.DropTable(
                name: "DanhMuc");

            migrationBuilder.DropTable(
                name: "TaiKhoan");
        }
    }
}
