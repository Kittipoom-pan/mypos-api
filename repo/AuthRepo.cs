using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using mypos_api.Database;
using mypos_api.Models;

namespace mypos_api.repo
{
    public class AuthRepo : IAuthRepo
    {
        private readonly DatabaseContext _context;

        public IConfiguration _configuration { get; }

        public AuthRepo(DatabaseContext context, IConfiguration configuration)
        {
            this._context = context;
            _configuration = configuration;
        }
        public (Users, string) Login(Users user)
        {
            // ถ้าเกิด exception จะคืนค่า default, u.Username มาจาก database, user.Username มาจาก user กรอกเข้ามา
            var result = _context.Users.SingleOrDefault(u => u.Username == user.Username);
            if (result == null)
            {
                return (null, String.Empty);
            }
            if (!CheckPassword(result.Password, user.Password))
            {
                return (result, String.Empty);
            }
            // result query มาจาก database
            return (result, BuildToken(result));
        }

        // โยน username, password มา
        private string BuildToken(Users user)
        {
            // key is case-sensitive
            var claims = new[] {
                // payload จะเก็บอะไรก็ได้
                // JwtRegisteredClaimNames.Sub คือ "sub"
                new Claim(JwtRegisteredClaimNames.Sub, _configuration["Jwt:Subject"]),
                new Claim("id", user.Id.ToString()),
                new Claim("username", user.Username),
                new Claim("position", user.Position),
                // ClaimTypes.Role คือ "role"
                new Claim(ClaimTypes.Role, user.Position),
            };
            // AddDays ดึงค่าวันหนึ่ง
            var expires = DateTime.Now.AddDays(Convert.ToDouble(_configuration["Jwt:ExpireDay"]));
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        // hash มาจาก database , password  
        private bool CheckPassword(string hash, string password)
        {
            // ถอดจุด hash จาก database ออกมา เป็น array
            var parts = hash.Split('.', 2);
            // เช็คขนาด hash
            if (parts.Length != 2)
            {
                return false;
            }
            // [adwdwfwfwfwfw] : เป็น array
            var salt = Convert.FromBase64String(parts[0]);
            var passwordHash = parts[1];

            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));

            return passwordHash == hashed;
        }

        public void Register(Users user)
        {
            user.Password = HashPassword(user.Password);
            _context.Users.Add(user);
            _context.SaveChanges();
        }

        private string HashPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            string hashed = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA512,
                iterationCount: 10000,
                numBytesRequested: 256 / 8));
            return $"{Convert.ToBase64String(salt)}.{hashed}"; // การต่อ string
        }
    }
}