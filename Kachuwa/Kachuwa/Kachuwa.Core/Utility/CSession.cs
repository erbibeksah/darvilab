using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Kachuwa.Core.Utility
{
    public class CSession
    {
        public string Email { get; set; }
        public string Username { get; set; }
        public long UserId { get; set; }
        public int Roles { get; set; }
        public bool TwoFactorEnabled { get; set; }
    }

    public class SessionExt
    {
        private readonly IHttpContextAccessor _context;

        public SessionExt(IHttpContextAccessor context)
        {
            _context = context;
        }
        public void Set<T>(string key, T value)
        {
            string jsonString = JsonConvert.SerializeObject(value);
            _context.HttpContext.Session.SetString(key, jsonString);
        }

        public T Get<T>(string key)
        {
            var json = _context.HttpContext.Session.GetString(key);
            if (string.IsNullOrEmpty(json))
                return default(T);

            try
            {
                return JsonConvert.DeserializeObject<T>(json);
            }
            catch
            {
                return default(T);
            }
        }
        public void Remove(string key)
        {
            _context.HttpContext.Session.Remove(key);
        }
    }
}
