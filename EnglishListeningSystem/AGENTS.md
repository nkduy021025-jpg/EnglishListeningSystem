# Quy ước dự án EnglishListeningSystem

- Dùng Bootstrap 5 và Bootstrap Icons cho giao diện mới. Giữ bố cục cân đối, dễ đọc trên điện thoại và máy tính. Khi chỉnh sửa giao diện có sẵn, giữ các card, badge, modal, icon và trạng thái hover, focus.
- Trang chủ dùng nền kem `#f8f5ee`, khối giới thiệu xanh rừng `#142f28` và điểm nhấn cam đào `#ffbd8e`.
- Phân hệ Student dùng nền kem `#f8f5ee`, card trắng ngà `#fffcf6`, viền be `#e4ddd0` và chữ xanh rừng `#19382f`. Icon chủ đề và accordion dùng nền xanh xám nhạt `#e4ece3`. Khung đánh giá dùng nền xám đậm `#282d33`, viền `#383e45` và sao vàng `#ffc107`. Danh sách dùng lưới ba cột trên màn hình rộng và giữ accordion chia phần.
- Phân hệ Admin dùng giao diện Bootstrap sáng, bảng dễ đọc và modal xác nhận rõ ràng. Dùng xanh dương cho thống kê, xanh lá cho trạng thái hoạt động, đỏ cho trạng thái bị khóa và vàng cho nút khóa.
- Trong Controllers, Models và ViewModels, dùng namespace có ngoặc nhọn, constructor truyền thống, kiểu đối tượng tường minh và `if/else` dễ đọc. Tách truy vấn dài thành từng bước có tên rõ nghĩa. Dùng chú thích tiếng Việt ngắn ở những đoạn xử lý cần giải thích.
- Khi refactor, giữ luồng nghiệp vụ, công thức tính điểm, `PasswordHasher`, phân quyền và tên dùng cho định tuyến, model binding và ánh xạ MySQL. Kiểm tra lại bằng `dotnet build`.
