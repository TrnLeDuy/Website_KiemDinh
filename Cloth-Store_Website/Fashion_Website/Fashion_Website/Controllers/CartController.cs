﻿using Fashion_Website.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Fashion_Website.Models.shoppingCart;
using System.Data.Common;
using System.Data.Entity.Validation;
using System.Diagnostics;
using PayPal.Api;
using System.ComponentModel;
using Fashion_Website.Models.taoMa;
using System.Security.Policy;

namespace Fashion_Website.Controllers
{
    public class CartController : Controller
    {
        // GET: Cart
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult SanPhamGH()
        {
            fashionDBEntities db = new fashionDBEntities();
            var cart = Cart.GetCart();
            var cartItems = cart.Items?.GroupBy(item => new { item.ProductId, item.ProductSize });
            if (cartItems == null || cartItems.Count() == 0)
            {
                ViewBag.Message = "Your cart is empty.";
                return View();
            }

            var cartViewModel = new List<CartItemViewModel>();
            foreach (var itemGroup in cartItems)
            {
                var product = db.SANPHAMs.Find(itemGroup.Key.ProductId);
                var cartItemViewModel = new CartItemViewModel
                {
                    ProductId = itemGroup.Key.ProductId,
                    ProductName = product.TenSP,
                    ProductImg = product.HinhSP,
                    ProductSize = itemGroup.Key.ProductSize,
                    Quantity = itemGroup.Sum(i => i.Quantity),
                    Price = itemGroup.First().Price,
                    Subtotal = itemGroup.Sum(i => i.Quantity * i.Price)
                };
                cartViewModel.Add(cartItemViewModel);
            }

            return View(cartViewModel);
        }


        public ActionResult AddToCart(string productId, int quantity, string size)
        {
            fashionDBEntities db = new fashionDBEntities();
            var product = db.SANPHAMs.Find(productId);

            CartItem cartItem = new CartItem
            {
                ProductId = product.MaSP,
                ProductName = product.TenSP,
                ProductImg = product.HinhSP,
                ProductSize = size,
                Quantity = quantity,
                Price = product.GiaSP
            };

            var cart = Cart.GetCart();
            var existingCartItem = cart.Items != null ? cart.Items.FirstOrDefault(item => item.ProductId == productId && item.ProductSize == size) : null;

            if (existingCartItem != null)
            {
                // Item already exists in cart, update its quantity
                existingCartItem.Quantity += quantity;
            }
            else
            {
                // Item does not exist in cart, add it
                cart.AddItem(cartItem);
            }

            return RedirectToAction("SanPhamGH");
        }


        

        public ActionResult ClearCart()
        {
            var cart = Cart.GetCart();
            cart.Clear();

            return RedirectToAction("SanPhamGH");
        }

        public int GetCartTotal()
        {
            var cart = Cart.GetCart();
            int total = 0;
            if (cart != null && cart.Items != null)
            {
                total = cart.Items.Sum(item => item.Quantity);
            }
            return total;
        }

        public ActionResult RemoveFromCart(string productId)
        {
            var cart = Cart.GetCart();
            var itemToRemove = cart.Items.SingleOrDefault(item => item.ProductId == productId);
            if (itemToRemove != null)
            {
                cart.Items.Remove(itemToRemove);
                Cart.GetCart();
            }

            return RedirectToAction("SanPhamGH");
        }

        //public ActionResult SummaryOrder()
        //{
        //    fashionDBEntities db = new fashionDBEntities();
        //    var cart = Cart.GetCart();
        //    var cartItems = cart.Items.GroupBy(item => new { item.ProductId, item.ProductSize });

        //    string tenKH = Session["Fullname"].ToString().Trim();
        //    string userID = Session["ID"].ToString().Trim();
        //    var user = db.KHACHHANGs.FirstOrDefault(k => k.MaKH == userID);
        //    var phoneNum = user.SDT.ToString().Trim();  
        //    var diaChi = user.DiaChi.ToString().Trim();     
        //    ViewBag.TenKH = tenKH;
        //    ViewBag.PhoneNumber = phoneNum;
        //    ViewBag.Address = diaChi;
        //    var paymentViewModel = new PaymentViewModel
        //    {
                
        //        Fullname = tenKH,
        //        PhoneNumber = phoneNum,
        //        Address = diaChi,
        //        TotalAmount = cartItems.Sum(g => g.Sum(item => item.Quantity * item.Price))
        //    };
        //    return View(paymentViewModel);
        //}    

        public ActionResult ThanhToan(String paymentMethod)
        {
            fashionDBEntities db = new fashionDBEntities();
            string cod = "thanh toán khi nhận hàng";
            // Get the current cart
            var cart = Cart.GetCart();

            // Group the cart items by product and size
            var cartItems = cart.Items.GroupBy(item => new { item.ProductId, item.ProductSize });

            // Create a view model containing the cart items
            var cartViewModel = new List<CartItemViewModel>();
            //tạo đơn hàng và lưu đơn hàng
            string maKH = Session["IDKH"].ToString().Trim();
            string tenKH = Session["FullnameKH"].ToString().Trim();

            var user = db.KHACHHANGs.FirstOrDefault(k => k.MaKH == maKH);
            var phoneNum = user.SDT.ToString().Trim();
            var diaChi = user.DiaChi.ToString().Trim();

            ViewBag.TenKH = tenKH;
            ViewBag.PhoneNumber = phoneNum;
            ViewBag.Address = diaChi;

            DONHANG donHang = new DONHANG();
            donHang.MaDH = new Fashion_Website.Models.taoMa.taoMaDonHang().TaoMaDonHang();
            donHang.NgayDatHang = DateTime.Now;
            donHang.NgayGiaoHang = DateTime.Now.AddDays(1);
            donHang.TrangThaiDH = 0;
            donHang.PTThanhToan = " ";
            donHang.TongTien = cartItems.Sum(i => i.Sum(item => item.Quantity * item.Price));
            donHang.MaKH = maKH;
            try
            {
                // Lưu dữ liệu vào cơ sở dữ liệu
                db.DONHANGs.Add(donHang);
                db.SaveChanges();
                Session["DonHang"] = donHang;
            }
            catch (DbEntityValidationException ex)
            {
                foreach (var error in ex.EntityValidationErrors)
                {
                    foreach (var validationError in error.ValidationErrors)
                    {
                        Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                    }
                }
            }



            foreach (var itemGroup in cartItems)
            {
                var product = db.SANPHAMs.Find(itemGroup.Key.ProductId);
                var cartItemViewModel = new CartItemViewModel
                {
                    ProductId = itemGroup.Key.ProductId,
                    ProductName = product.TenSP,
                    ProductImg = product.HinhSP,
                    ProductSize = itemGroup.Key.ProductSize,
                    Quantity = itemGroup.Sum(i => i.Quantity),
                    Price = itemGroup.First().Price,
                    Subtotal = itemGroup.Sum(i => i.Quantity * i.Price)
                };
                var productSize = itemGroup.Key.ProductSize.Replace(" ", "");
                CTDONHANG ctDonHang = new CTDONHANG();

                try
                {
                    ctDonHang.MACTDH = new Fashion_Website.Models.taoMa.taoMaCTDH().TaoMaCTDH();
                    ctDonHang.SoLuongDat = itemGroup.Sum(i => i.Quantity);
                    ctDonHang.DonGia = itemGroup.First().Price;
                    ctDonHang.TenSP = product.TenSP;
                    ctDonHang.KichCo = productSize; // Use the trimmed size string
                    ctDonHang.MaDH = donHang.MaDH;
                    ctDonHang.MaSP = itemGroup.Key.ProductId;
                    cartViewModel.Add(cartItemViewModel);
                    db.CTDONHANGs.Add(ctDonHang);
                    db.SaveChanges();
                    Session["CTDH"] = ctDonHang;
                }
                catch (DbEntityValidationException ex)
                {
                    foreach (var error in ex.EntityValidationErrors)
                    {
                        foreach (var validationError in error.ValidationErrors)
                        {
                            Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
                        }
                    }
                }             
            }

            if (paymentMethod == "paypal")
            {
                DONHANG dhang = Session["DonHang"] as DONHANG;
                dhang.PTThanhToan = paymentMethod;
                Session["DonHang"] = dhang;
                return RedirectToAction("PaymentWithPaypal", "PayPal");
            }
            //else if (paymentMethod == cod) {

            //    CTDONHANG ctDonHang = Session["CTDH"] as CTDONHANG;

            //    HOADON modelHoaDon = new HOADON();
            //    modelHoaDon.MaHD = new Fashion_Website.Models.taoMa.taoMaHoaDon().TaoMaHoaDon();
            //    modelHoaDon.NgayLap = DateTime.Now;
            //    modelHoaDon.TongTien = donHang.TongTien;
            //    modelHoaDon.MaKH = maKH;
            //    modelHoaDon.MaAD = "AD001";
            //    modelHoaDon.MaDH = donHang.MaDH;
            //    try
            //    {
            //        // Lưu dữ liệu vào cơ sở dữ liệu
            //        db.HOADONs.Add(modelHoaDon);
            //        db.SaveChanges();
            //        Session["HoaDon"] = modelHoaDon;
            //    }
            //    catch (DbEntityValidationException ex)
            //    {
            //        foreach (var error in ex.EntityValidationErrors)
            //        {
            //            foreach (var validationError in error.ValidationErrors)
            //            {
            //                Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
            //            }
            //        }
            //    }
            //}


            // Pass the cart items to the view
            return View(cartViewModel);
        }
        //public ActionResult ProcessPayment()
        //{
        //    fashionDBEntities db = new fashionDBEntities();

        //    DONHANG donHang = Session["DonHang"] as DONHANG;
        //    CTDONHANG ctDonHang = Session["CTDH"] as CTDONHANG;

        //    //string maKH = Session["IDKH"].ToString().Trim();

        //    //HOADON modelHoaDon = new HOADON();
        //    //modelHoaDon.MaHD = new Fashion_Website.Models.taoMa.taoMaHoaDon().TaoMaHoaDon();
        //    //modelHoaDon.NgayLap = DateTime.Now;
        //    //modelHoaDon.TongTien = donHang.TongTien;
        //    //modelHoaDon.MaKH = maKH;
        //    //try
        //    //{
        //    //    // Lưu dữ liệu vào cơ sở dữ liệu
        //    //    db.HOADONs.Add(modelHoaDon);
        //    //    db.SaveChanges();
        //    //    Session["HoaDon"] = modelHoaDon;
        //    //}
        //    //catch (DbEntityValidationException ex)
        //    //{
        //    //    foreach (var error in ex.EntityValidationErrors)
        //    //    {
        //    //        foreach (var validationError in error.ValidationErrors)
        //    //        {
        //    //            Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
        //    //        }
        //    //    }
        //    //}

        //    //CTHOADON ctHoaDon = new CTHOADON();
        //    //ctHoaDon.MACTHD = new Fashion_Website.Models.taoMa.taoMaCTHD().TaoMaCTHD();
        //    //ctHoaDon.TenSP = ctDonHang.TenSP;
        //    //ctHoaDon.DonGia = ctDonHang.DonGia;
        //    //ctHoaDon.SoLuong = ctDonHang.SoLuongDat;
        //    //ctHoaDon.KichCo = ctDonHang.KichCo;
        //    //ctHoaDon.ThanhTien = ctDonHang.DonGia * ctDonHang.SoLuongDat;
        //    //ctHoaDon.MaHD = modelHoaDon.MaHD;
        //    //ctHoaDon.MaSP = ctDonHang.MaSP;
        //    //try
        //    //{
        //    //    // Lưu dữ liệu vào cơ sở dữ liệu
        //    //    db.CTHOADONs.Add(ctHoaDon);
        //    //    db.SaveChanges();
        //    //}
        //    //catch (DbEntityValidationException ex)
        //    //{
        //    //    foreach (var error in ex.EntityValidationErrors)
        //    //    {
        //    //        foreach (var validationError in error.ValidationErrors)
        //    //        {
        //    //            Debug.WriteLine($"Property: {validationError.PropertyName} Error: {validationError.ErrorMessage}");
        //    //        }
        //    //    }
        //    //}

        //    //if (donHang.PTThanhToan == "thanh toán khi nhận hàng")
        //    //{
        //    //    return View();
        //    //}
        //    //else if (donHang.PTThanhToan == "chuyển khoản")
        //    //{
        //    //    return RedirectToAction("PaymentWithPaypal", "PayPal");
        //    //}

        //    //Session.Remove("DonHang");
        //    //Session.Remove("CTDH");
        //    //Session.Remove("HoaDon");

        //    return RedirectToAction("PaymentWithPaypal", "PayPal");
        //}
    }
}