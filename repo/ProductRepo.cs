using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using mypos_api.Database;
using mypos_api.Models;

namespace mypos_api.repo
{
    // อิงกับหน้า IProductRepo 
    public class ProductRepo : IProductRepo  // สืบทอด IProductRepo 
    {
        // Constructor Overloading, DatabaseContext ฉีด service เข้ามา
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IWebHostEnvironment _webHostEnvironment;
        public ProductRepo(DatabaseContext context, IHttpContextAccessor httpContextAccessor, IWebHostEnvironment webHostEnvironment)
        {
            this._webHostEnvironment = webHostEnvironment;
            this._httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        public DatabaseContext _context { get; }

        public async Task<Products> AddProduct(Products product)
        {
            var image = await UploadProductImages();
            if (!String.IsNullOrEmpty(image))// กรณีรูปภาพเดียว
            {
                product.Image = image;
            }
            // insert database
            _context.Products.Add(product);
            _context.SaveChanges();
            return product; // return ไปมี primary key
        }

        public bool DeleteProduct(int id)
        {
            // เรียกใช้ฟังก์ชั่น GetProduct
            var result = GetProduct(id);
            if (result == null)
            {
                return false;
            }
            _context.Products.Remove(result); //delete product
            _context.SaveChanges(); // ลบเสร็จละเซฟ
            return true;
        }

        public async Task<Products> EditProduct(Products product, int id)
        {
            // result มาจาก database
            var result = GetProduct(id);
            if (result != null)
            {
                var image = await UploadProductImages(); // function UploadProductImages() รันแบบ async
                if (!String.IsNullOrEmpty(image))// กรณีรูปภาพเดียว
                {
                    result.Image = image;
                }
                // กำหนดให้ค่าใน database = ค่าที่ส่งมา
                result.Name = product.Name;
                result.Price = product.Price;
                result.Stock = product.Stock;

                // edit database
                _context.Products.Update(result);
                _context.SaveChanges();
            }
            return result;
        }

        public IEnumerable<Products> GETProduct()
        {
            // ef core : query database เอามาใช้แทน select * from
            return _context.Products.ToList(); // Products คือ ชื่อ table (DatabaseContext)
        }

        public Products GetProduct(int id)
        {
            // query แบบตัวเดียว
            return _context.Products.SingleOrDefault(product => product.ProductId == id); // ยูเซอร์โยน id เข้ามา
        }

        // Note: recommended used async Task
        public async Task<String> UploadProductImages()
        {
            // การส่งไฟล์รูป image : "xxxx"
            var files = _httpContextAccessor.HttpContext.Request.Form.Files;

            if (files.Count > 0)
            {
                const string folder = "/images/";
                string filePath = _webHostEnvironment.WebRootPath + folder; // wwwroot/images

                string fileName = "";
                // ที่ comment คือ กรณีอัพหลายรูปภาพ
                //var fileNameArray = new List<String>(); // multiple images case

                if (!Directory.Exists(filePath))
                {
                    Directory.CreateDirectory(filePath);
                }

                foreach (var formFile in files)
                {
                    // Guid ทำให้รูปยาวๆ
                    fileName = Guid.NewGuid().ToString() + System.IO.Path.GetExtension(formFile.FileName); // unique name
                    string fullPath = filePath + fileName;

                    if (formFile.Length > 0)
                    {
                        using (var stream = new FileStream(fullPath, FileMode.Create))
                        {
                            await formFile.CopyToAsync(stream); // อัพโหลดรูปแบบไม่รอ แต่จะใช้ cpu เยอะ แต่ได้ไฟล์ชัว
                        }
                    }

                    // fileNameArray.Add(fileName); // multiple images case
                }

                return fileName;
                //return fileNameArray; // multiple images case
            }
            return String.Empty;
            //return null;      // multiple images case
        }
    }
}