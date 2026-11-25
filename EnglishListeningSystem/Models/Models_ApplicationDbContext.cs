using System;
using Microsoft.EntityFrameworkCore;

#nullable disable

namespace EnglishListeningSystem.Models
{
    public partial class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext()
        {
        }

        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public virtual DbSet<BaiHoc> BaiHocs { get; set; }
        public virtual DbSet<CauChep> CauCheps { get; set; }
        public virtual DbSet<DanhGia> DanhGias { get; set; }
        public virtual DbSet<DanhMuc> DanhMucs { get; set; }
        public virtual DbSet<GhiChu> GhiChus { get; set; }
        public virtual DbSet<LamBai> LamBais { get; set; }
        public virtual DbSet<LoTrinh> LoTrinhs { get; set; }
        public virtual DbSet<TaiKhoan> TaiKhoans { get; set; }
        public virtual DbSet<TienDoBaiHoc> TienDoBaiHocs { get; set; }
        public virtual DbSet<TuVung> TuVungs { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            if (!optionsBuilder.IsConfigured)
            {
                var connectionString = "Server=localhost;Port=3306;Database=EnglishListeningDb;Uid=root;Pwd=MySQL*duy*0210*abc;";
                optionsBuilder.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString));
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TaiKhoan>(entity =>
            {
                entity.HasKey(e => e.MaTK);
                entity.ToTable("TaiKhoan");

                entity.Property(e => e.TenDangNhap).IsRequired().HasMaxLength(50);
                entity.HasIndex(e => e.TenDangNhap).IsUnique();

                entity.Property(e => e.MatKhau).IsRequired().HasMaxLength(255);
                entity.Property(e => e.HoTen).IsRequired().HasMaxLength(100);

                entity.Property(e => e.Email).IsRequired(false).HasMaxLength(150);
                entity.HasIndex(e => e.Email).IsUnique();

                entity.Property(e => e.VaiTro).IsRequired().HasMaxLength(20).HasDefaultValue("Student");
                entity.Property(e => e.DiemTichLuy).HasDefaultValue(0);
                entity.Property(e => e.TrangThaiTK).HasDefaultValue(true);
            });

            modelBuilder.Entity<DanhMuc>(entity =>
            {
                entity.HasKey(e => e.MaDM);
                entity.ToTable("DanhMuc");

                entity.Property(e => e.TenDM).IsRequired().HasMaxLength(100);
                entity.Property(e => e.TrangThaiHienThi).HasDefaultValue(true);
            });

            modelBuilder.Entity<LoTrinh>(entity =>
            {
                entity.HasKey(e => e.MaLT);
                entity.ToTable("LoTrinh");

                entity.Property(e => e.TenLT).IsRequired().HasMaxLength(150);
                entity.Property(e => e.TrangThaiLT).HasMaxLength(50).HasDefaultValue("Đang học");
                entity.Property(e => e.CauHienTai).HasDefaultValue(1);
                entity.Property(e => e.NgayCapNhat).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.MaTKNavigation)
                    .WithMany(p => p.LoTrinhs)
                    .HasForeignKey(d => d.MaTK)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_LoTrinh_TaiKhoan");

                entity.HasOne(d => d.MaDMNavigation)
                    .WithMany(p => p.LoTrinhs)
                    .HasForeignKey(d => d.MaDM)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_LoTrinh_DanhMuc");
            });

            modelBuilder.Entity<BaiHoc>(entity =>
            {
                entity.HasKey(e => e.MaBH);
                entity.ToTable("BaiHoc");

                entity.Property(e => e.TenBH).IsRequired().HasMaxLength(200);
                entity.Property(e => e.CapDo).IsRequired().HasMaxLength(50);
                entity.Property(e => e.AudioUrl).HasMaxLength(255);
                entity.Property(e => e.YoutubeUrl).HasMaxLength(255);
                entity.Property(e => e.NgayTao).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.MaDMNavigation)
                    .WithMany(p => p.BaiHocs)
                    .HasForeignKey(d => d.MaDM)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_BaiHoc_DanhMuc");

                entity.HasOne(d => d.MaTKNavigation)
                    .WithMany(p => p.BaiHocs)
                    .HasForeignKey(d => d.MaTK)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_BaiHoc_TaiKhoan");
            });

            modelBuilder.Entity<DanhGia>(entity =>
            {
                entity.ToTable("DanhGia", table => table.HasCheckConstraint("CK_DanhGia_SoSao", "`SoSao` BETWEEN 1 AND 5"));
                entity.HasKey(e => e.MaDG);
                entity.Property(e => e.MaDG).ValueGeneratedOnAdd();
                entity.Property(e => e.SoSao).IsRequired();
                entity.Property(e => e.NhanXet).HasMaxLength(500);
                entity.Property(e => e.NgayDanhGia).HasDefaultValueSql("CURRENT_TIMESTAMP(6)");

                // Mỗi học viên có một đánh giá cho mỗi bài học.
                entity.HasIndex(e => new { e.MaTK, e.MaBH }).IsUnique();

                entity.HasOne(d => d.MaTKNavigation)
                    .WithMany(p => p.DanhGias)
                    .HasForeignKey(d => d.MaTK)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(d => d.MaBHNavigation)
                    .WithMany(p => p.DanhGias)
                    .HasForeignKey(d => d.MaBH)
                    .OnDelete(DeleteBehavior.Cascade);
            });

            modelBuilder.Entity<TienDoBaiHoc>(entity =>
            {
                entity.HasKey(e => new { e.MaTK, e.MaBH });
                entity.ToTable("TienDoBaiHoc");

                entity.Property(e => e.MucDoHoanThanh).HasColumnType("decimal(5, 2)").HasDefaultValue(0m);
                entity.Property(e => e.NgayCapNhat).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.MaTKNavigation)
                    .WithMany(p => p.TienDoBaiHocs)
                    .HasForeignKey(d => d.MaTK)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TienDoBaiHoc_TaiKhoan");

                entity.HasOne(d => d.MaBHNavigation)
                    .WithMany(p => p.TienDoBaiHocs)
                    .HasForeignKey(d => d.MaBH)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TienDoBaiHoc_BaiHoc");
            });

            modelBuilder.Entity<CauChep>(entity =>
            {
                entity.HasKey(e => e.MaCau);
                entity.ToTable("CauChep");

                entity.Property(e => e.NoiDungChuan).IsRequired();
                entity.Property(e => e.GoiY).HasMaxLength(255);
                entity.Property(e => e.TrangThaiDuyet).HasDefaultValue(true);

                entity.HasOne(d => d.MaBHNavigation)
                    .WithMany(p => p.CauCheps)
                    .HasForeignKey(d => d.MaBH)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_CauChep_BaiHoc");
            });

            modelBuilder.Entity<LamBai>(entity =>
            {
                entity.HasKey(e => new { e.MaTK, e.MaCau, e.Lan });
                entity.ToTable("LamBai");

                entity.Property(e => e.SoLanSai).HasDefaultValue(0);
                entity.Property(e => e.NgayLam).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.MaTKNavigation)
                    .WithMany(p => p.LamBais)
                    .HasForeignKey(d => d.MaTK)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_LamBai_TaiKhoan");

                entity.HasOne(d => d.MaCauNavigation)
                    .WithMany(p => p.LamBais)
                    .HasForeignKey(d => d.MaCau)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_LamBai_CauChep");
            });

            modelBuilder.Entity<GhiChu>(entity =>
            {
                entity.HasKey(e => e.MaGC);
                entity.ToTable("GhiChu");

                entity.Property(e => e.NoiDungGC).IsRequired();
                entity.Property(e => e.NgayTaoGC).HasColumnType("datetime").HasDefaultValueSql("CURRENT_TIMESTAMP");

                entity.HasOne(d => d.MaTKNavigation)
                    .WithMany(p => p.GhiChus)
                    .HasForeignKey(d => d.MaTK)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_GhiChu_TaiKhoan");

                entity.HasOne(d => d.MaBHNavigation)
                    .WithMany(p => p.GhiChus)
                    .HasForeignKey(d => d.MaBH)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_GhiChu_BaiHoc");
            });

            modelBuilder.Entity<TuVung>(entity =>
            {
                entity.HasKey(e => e.MaTV);
                entity.ToTable("TuVung");

                entity.Property(e => e.TuTiengAnh).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PhienAm).HasMaxLength(100);
                entity.Property(e => e.NghiaNguCanh).IsRequired();
                entity.Property(e => e.TrangThaiOnTap).HasDefaultValue(false);

                entity.HasOne(d => d.MaTKNavigation)
                    .WithMany(p => p.TuVungs)
                    .HasForeignKey(d => d.MaTK)
                    .OnDelete(DeleteBehavior.Cascade)
                    .HasConstraintName("FK_TuVung_TaiKhoan");

                entity.HasOne(d => d.MaBHNavigation)
                    .WithMany(p => p.TuVungs)
                    .HasForeignKey(d => d.MaBH)
                    .OnDelete(DeleteBehavior.SetNull)
                    .HasConstraintName("FK_TuVung_BaiHoc");
            });

            OnModelCreatingPartial(modelBuilder);
        }

        partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
    }
}
