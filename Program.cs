using BaiTapThucHanh.Data;
using BaiTapThucHanh.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using System.IO;
using Microsoft.AspNetCore.Identity; // THÊM THƯ VIỆN NÀY ĐỂ SỬ DỤNG IDENTITYUSER

var builder = WebApplication.CreateBuilder(args);

// =========================================================================
// 1. CẤU HÌNH CÁC DỊCH VỤ (SERVICES) - PHẢI NẰM TRƯỚC builder.Build()
// =========================================================================

// Cấu hình kết nối SQL Server thông qua AppDbContext
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// -------------------------------------------------------------------------
// [THÊM MỚI TẠI ĐÂY] - CẤU HÌNH DỊCH VỤ BỘ NHỚ ĐỆM VÀ SESSION CHO GIỎ HÀNG
// -------------------------------------------------------------------------
builder.Services.AddDistributedMemoryCache(); // Bắt buộc phải có làm nền tảng cho Session
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Giỏ hàng tự hủy sau 30 phút không thao tác
    options.Cookie.HttpOnly = true;                 // Tăng cường bảo mật bảo vệ Cookie giỏ hàng
    options.Cookie.IsEssential = true;             // Đánh dấu cookie tối quan trọng cho ứng dụng
});
// -------------------------------------------------------------------------

// THÊM ĐOẠN NÀY: CẤU HÌNH DỊCH VỤ CỦA ASP.NET CORE IDENTITY
builder.Services.AddDefaultIdentity<IdentityUser>(options => {
    options.SignIn.RequireConfirmedAccount = false; // Tắt bắt buộc xác nhận Email để làm bài tập dễ dàng hơn
    options.Password.RequireDigit = false;          // Không bắt buộc mật khẩu phải có chữ số
    options.Password.RequiredLength = 6;            // Độ dài mật khẩu tối thiểu là 6 ký tự
    options.Password.RequireNonAlphanumeric = false; // Không bắt buộc có ký tự đặc biệt (@, #, $...)
    options.Password.RequireUppercase = false;       // Không bắt buộc phải có chữ viết hoa
    options.Password.RequireLowercase = false;       // Không bắt buộc phải có chữ viết thường
})
.AddRoles<IdentityRole>() // BẮT BUỘC THÊM: Để hệ thống kích hoạt tính năng quản lý phân vai trò (Roles) cho Bài 4
.AddEntityFrameworkStores<AppDbContext>();

// Bắt buộc phải có dịch vụ này thì ứng dụng mới đọc được giao diện Đăng ký/Đăng nhập (Razor Pages) của Identity UI
builder.Services.AddRazorPages();

// Thêm các dịch vụ hỗ trợ mô hình MVC (Controller & Views)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// =========================================================================
// 2. CẤU HÌNH PIPELINE / MIDDLEWARE - PHẢI NẰM SAU var app = builder.Build()
// =========================================================================

// Cấu hình xử lý lỗi môi trường Production
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();

// --- CẤU HÌNH PHỤC VỤ FILE TĨNH ---
app.UseStaticFiles();

// Cho phép ứng dụng đọc và hiển thị hình ảnh từ thư mục "images" ở gốc dự án
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(
        Path.Combine(app.Environment.ContentRootPath, "images")),
    RequestPath = "/images"
});

// ----------------------------------
app.UseRouting();

// -------------------------------------------------------------------------
// [THÊM MỚI TẠI ĐÂY] - KÍCH HOẠT MIDDLEWARE SESSION CHO ỨNG DỤNG
// Quy tắc bắt buộc: Phải nằm SAU UseRouting() và TRƯỚC hai lệnh xác thực tài khoản
// -------------------------------------------------------------------------
app.UseSession();
// -------------------------------------------------------------------------

// BẮT BUỘC: Phải kích hoạt Authentication (Xác thực danh tính) TRƯỚC Authorization (Cấp quyền)
app.UseAuthentication();
app.UseAuthorization();

// THÊM DÒNG NÀY: Định tuyến để hệ thống tự động ánh xạ các trang Đăng ký/Đăng nhập của Identity UI
app.MapRazorPages();

// Cấu hình đường tuyến (Route) mặc định điều hướng vào trang chủ HomeController
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

// =========================================================================
// THÊM ĐOẠN NÀY: TỰ ĐỘNG CHẠY SEEDER ĐỂ KHỞI TẠO TÀI KHOẢN ADMIN KHI KHỞI ĐỘNG
// =========================================================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // Chạy hàm tạo Role Admin và User Admin mẫu bất đồng bộ
        await DbSeeder.SeedRolesAndAdminAsync(services);
    }
    catch (Exception ex)
    {
        // Ghi nhận lỗi nếu hệ thống không kết nối tới SQL Server thành công lúc khởi động
    }
}

// =========================================================================
app.Run();