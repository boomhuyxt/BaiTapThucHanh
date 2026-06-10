using BaiTapThucHanh.Data;
using BaiTapThucHanh.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Text.Json;

namespace BaiTapThucHanh.Controllers
{
    public class CartController : Controller
    {
        private readonly AppDbContext _context;

        public CartController(AppDbContext context)
        {
            _context = context;
        }

        // Đọc danh sách giỏ hàng thực tế từ Session
        private List<CartItem> GetCartItems()
        {
            var sessionData = HttpContext.Session.GetString("ShoppingCart");
            return sessionData == null ? new List<CartItem>() : JsonSerializer.Deserialize<List<CartItem>>(sessionData)!;
        }

        // Lưu lại danh sách giỏ hàng vào Session
        private void SaveCartItems(List<CartItem> cart)
        {
            HttpContext.Session.SetString("ShoppingCart", JsonSerializer.Serialize(cart));
        }

        // HIỂN THỊ DANH SÁCH THỰC TẾ TẠI INDEX.CSHTML
        public IActionResult Index()
        {
            var cart = GetCartItems();
            return View(cart); // Truyền danh sách thực tế sang View
        }

        // XỬ LÝ THÊM ĐƠN HÀNG VÀO GIỎ
        [HttpPost]
        public IActionResult AddToCart(int productId, int quantity)
        {
            var product = _context.Products.Find(productId);
            if (product == null)
            {
                return NotFound();
            }

            var cart = GetCartItems();
            var cartItem = cart.Find(p => p.ProductId == productId);

            if (cartItem == null)
            {
                // Nếu chưa có, tạo mới phần tử trong giỏ
                cart.Add(new CartItem
                {
                    ProductId = product.Id,
                    Name = product.Name,
                    Price = product.Price,
                    ImageUrl = product.ImageUrl,
                    Quantity = quantity
                });
            }
            else
            {
                // Nếu đã tồn tại, cộng dồn số lượng thêm mới
                cartItem.Quantity += quantity;
            }

            SaveCartItems(cart);
            return RedirectToAction("Index"); // Nhảy thẳng qua file Giỏ Hàng
        }
    }
}