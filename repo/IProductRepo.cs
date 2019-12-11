using System.Collections.Generic;
using System.Threading.Tasks;
using mypos_api.Models;

namespace mypos_api.repo
{
    public interface IProductRepo
    {
        IEnumerable<Products> GETProduct();
        Products GetProduct(int id);
        // return ตัวเดียว 
        Task<Products> AddProduct(Products product);
        Task<Products> EditProduct(Products product, int id);
        // delete : success or false
        bool DeleteProduct(int id);
    }
}