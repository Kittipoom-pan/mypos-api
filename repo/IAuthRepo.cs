using mypos_api.Models;

namespace mypos_api.repo
{
    public interface IAuthRepo
    {
        // return Users, boolean, string คือ token
        (Users, string) Login(Users user);

        void Register(Users user);
    }
}