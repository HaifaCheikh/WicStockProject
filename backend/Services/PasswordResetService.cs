using System.Collections.Concurrent;

namespace WicStock_.Services
{
    public class PasswordResetService
    {
        private readonly ConcurrentDictionary<string, (string Code, DateTime Expiration)> _codes = new();

        public string GenerateCode(string identifier)
        {
            var random = new Random();
            var code = random.Next(100000, 999999).ToString();
            var expiration = DateTime.UtcNow.AddMinutes(15);
            
            _codes[identifier] = (code, expiration);

            // Pour faciliter le test en développement, on l'affiche dans la console
            Console.WriteLine($"[WicStock RESET CODE] Code pour {identifier} : {code} (Expire à : {expiration} UTC)");

            return code;
        }

        public bool VerifyCode(string identifier, string code)
        {
            if (_codes.TryGetValue(identifier, out var data))
            {
                if (data.Expiration > DateTime.UtcNow && data.Code == code)
                {
                    return true;
                }
            }
            return false;
        }

        public void RemoveCode(string identifier)
        {
            _codes.TryRemove(identifier, out _);
        }
    }
}
