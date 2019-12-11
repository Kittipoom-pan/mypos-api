using System;
using System.Linq;
using AutoMapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using mypos_api.Models;
using mypos_api.repo;
using mypos_api.ViewModels;

namespace mypos_api.Controllers
{
    [ApiController] // check valid value
    [Route("[controller]")] //localhost../auth
    public class AuthController : ControllerBase
    {
        ILogger<AuthController> _logger;
        // เรียกใช้งาน IAuthRepo ผ่านทาง Dependencies Injection เพื่อใช้ในการติดต่อกับ database
        public AuthController(ILogger<AuthController> logger, IAuthRepo authRepo, IMapper mapper)
        {
            _logger = logger;
            _authRepo = authRepo;
            _mapper = mapper;
        }
        public IAuthRepo _authRepo { get; }

        private readonly IMapper _mapper;


        // ("path?")
        [HttpPost("login")]
        public IActionResult Login(UsersViewModels usersViewModels) // Username, password
        {
            try
            {
                Users user = _mapper.Map<Users>(usersViewModels);

                // insert to database
                (Users result, string token) = _authRepo.Login(user);
                if (result == null)
                {
                    return Unauthorized(new { token = String.Empty, message = "ไม่เจอยูสเซอร์เนม" }); // 401
                }
                // เป็น null หรือ empty หรือป่าว
                if (String.IsNullOrEmpty(token))
                {
                    return Unauthorized(new { token = String.Empty, message = "พาสเวิร์ดไม่ถูกต้อง" }); // 401
                }
                // ถ้่า login สำเร็จให้ return token , message
                return Ok(new { token = token, message = "ล็อคอินสำเร็จ" });
            }
            catch (Exception ex)
            {
                // ต่อ string
                _logger.LogError($"Login failure : {ex}");
                return StatusCode(500, new { token = String.Empty, message = ex });
            }
        }

        // action อิงกับ Register
        [HttpPost("[action]")]
        public IActionResult register(Users user)
        {
            try
            {
                _authRepo.Register(user);
                return Ok(new { result = "สำเร็จ", message = "สมัครสมาชิกสำเร็จ" });
            }
            catch (Exception ex)
            {
                _logger.LogError($"สมัครสมาชิกไม่สำเร็จ : {ex}");
                return StatusCode(500, new { result = "nok", message = "สมัครสมาชิกไม่สำเร็จ" });
            }
        }
    }
}