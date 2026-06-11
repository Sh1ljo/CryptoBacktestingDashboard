using CryptoBacktestingDashboard.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace CryptoBacktestingDashboard.Controllers
{
    public abstract class BaseController : Controller
    {
        protected readonly UserManager<AppUser> UserManager;

        protected BaseController(UserManager<AppUser> userManager)
        {
            UserManager = userManager;
        }

        protected string UserId
        {
            get
            {
                return UserManager.GetUserId(User);
            }
        }
    }
}