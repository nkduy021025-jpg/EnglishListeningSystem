using System;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace EnglishListeningSystem.Migrations
{
    /// <inheritdoc />
    public partial class AddDanhGiaTable : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "DanhGia",
                columns: table => new
                {
                    MaDG = table.Column<int>(type: "int", nullable: false)
                        .Annotation("MySql:ValueGenerationStrategy", MySqlValueGenerationStrategy.IdentityColumn),
                    MaTK = table.Column<int>(type: "int", nullable: false),
                    MaBH = table.Column<int>(type: "int", nullable: false),
                    SoSao = table.Column<int>(type: "int", nullable: false),
                    NhanXet = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true)
                        .Annotation("MySql:CharSet", "utf8mb4"),
                    NgayDanhGia = table.Column<DateTime>(type: "datetime(6)", nullable: false, defaultValueSql: "CURRENT_TIMESTAMP(6)")
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DanhGia", x => x.MaDG);
                    table.CheckConstraint("CK_DanhGia_SoSao", "`SoSao` BETWEEN 1 AND 5");
                    table.ForeignKey(
                        name: "FK_DanhGia_BaiHoc_MaBH",
                        column: x => x.MaBH,
                        principalTable: "BaiHoc",
                        principalColumn: "MaBH",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_DanhGia_TaiKhoan_MaTK",
                        column: x => x.MaTK,
                        principalTable: "TaiKhoan",
                        principalColumn: "MaTK",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySql:CharSet", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_MaBH",
                table: "DanhGia",
                column: "MaBH");

            migrationBuilder.CreateIndex(
                name: "IX_DanhGia_MaTK_MaBH",
                table: "DanhGia",
                columns: new[] { "MaTK", "MaBH" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "DanhGia");
        }
    }
}
