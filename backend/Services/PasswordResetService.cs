using System.Collections.Concurrent;

namespace WicStock_.Services
{
    public class PasswordResetService
    {
        private readonly ConcurrentDictionary<string, (string Code, DateTime Expiration)> _codes = new();

        private static string CleanKey(string identifier) => identifier.Trim().ToLowerInvariant();

        public string GenerateCode(string identifier)
        {
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(15);
            
            var key = CleanKey(identifier);
            _codes[key] = (code, expiration);

            Console.WriteLine($"[WicStock RESET CODE] Code pour {identifier} ({key}) : {code} (Expire à : {expiration} UTC)");

            return code;
        }

        public bool VerifyCode(string identifier, string code)
        {
            var key = CleanKey(identifier);
            if (_codes.TryGetValue(key, out var data))
            {
                if (data.Expiration > DateTime.UtcNow && data.Code.Trim() == code.Trim())
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveCode(string identifier)
        {
            var key = CleanKey(identifier);
            _codes.TryRemove(key, out _);
        }
    }
}
