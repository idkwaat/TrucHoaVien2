# Trúc Họa Viên – ASP.NET Core

Trúc Họa Viên là một hệ thống website bán hàng được xây dựng bằng **ASP.NET Core**, mô phỏng nền tảng thương mại điện tử với đầy đủ chức năng quản lý sản phẩm, đơn hàng và thanh toán QR tự động.

Dự án được phát triển nhằm làm dự án thực tế cho sinh vien FPT và rèn luyện kỹ năng Full Stack và Backend với .NET, REST API, JWT Authentication.

---

## Tech Stack

- **Backend:** ASP.NET Core, C#
- **Authentication:** JWT (JSON Web Token)
- **Database:** SQL Server
- **Frontend:** ReactJS, Axios
- **Payment Integration:** SePay Webhook (QR Payment Notification)
- **Deployment:** Render, Railway
- **Version Control:** Git & GitHub

---

### Authentication & Authorization
- Đăng ký / Đăng nhập
- Xác thực bằng JWT
- Phân quyền User / Admin

### Product Management
- CRUD sản phẩm
- Upload hình ảnh sản phẩm
- Upload & hiển thị sản phẩm 3D
- Tìm kiếm sản phẩm
- Sắp xếp theo giá

### Order Management
- Tạo và lưu trữ đơn hàng
- Theo dõi trạng thái đơn hàng
- Lưu lịch sử mua hàng

### QR Payment Integration
- Tích hợp thanh toán QR thông qua SePay
- Nhận webhook khi thanh toán thành công
- Tự động cập nhật trạng thái đơn hàng

### Admin Dashboard
- Quản lý sản phẩm
- Quản lý đơn hàng
- Theo dõi hoạt động hệ thống

---
