using AutoMapper;
using mypos_api.Models;
using mypos_api.ViewModels;

namespace mypos_api.Util
{
    public class AutoMapper : Profile // Profile สืบทอดมาจาก Automapper
    {
        public AutoMapper()
        {
            // map UsersViewModels ให้เป็น Users
            CreateMap<UsersViewModels, Users>();
        }
    }
}