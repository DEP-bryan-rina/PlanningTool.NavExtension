using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using PlanningTool.NavExtension.Data;

namespace PlanningTool.NavExtension.Pages.Account
{
    public class LoginModel : PageModel
    {
   
        private readonly AppDbContext _context;

        public LoginModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        public string Message { get; set; }

        public bool IsLoggedIn { get; set; }
        public string DisplayUsername { get; set; }
        public string DisplayRole { get; set; }
        public string DisplayEmail { get; set; }

        public void OnGet()
        {
            LoadSession();
        }

        public void OnPost()
        {
            var user = _context.tblUserCenter
                .FirstOrDefault(u => u.Username == Username && u.PasswordHash == Password);

            if (user != null)
            {
                HttpContext.Session.SetString("Username", user.Username);
                HttpContext.Session.SetString("Role", user.Role);
                HttpContext.Session.SetString("Email", user.Email);

                Message = "Login successful!";
            }
            else
            {
                Message = "Invalid credentials!";
            }

            LoadSession();
        }

        private void LoadSession()
        {
            DisplayUsername = HttpContext.Session.GetString("Username");
            DisplayRole = HttpContext.Session.GetString("Role");
            DisplayEmail = HttpContext.Session.GetString("Email");

            IsLoggedIn = !string.IsNullOrEmpty(DisplayUsername);
        }
    }
}
