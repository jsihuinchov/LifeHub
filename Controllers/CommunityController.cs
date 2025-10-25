using Microsoft.AspNetCore.Mvc;
using LifeHub.Models.ViewModels;

namespace LifeHub.Controllers
{
    public class CommunityController : Controller
    {
        public IActionResult Index()
        {
            var model = new CommunityViewModel { ActiveSection = "Inicio" };
            return View(model);
        }

        public IActionResult Inicio()
        {
            var model = new CommunityViewModel { ActiveSection = "Inicio" };
            return PartialView("_Inicio", model);
        }

        public IActionResult Grupos()
        {
            var model = new CommunityViewModel { ActiveSection = "Grupos" };
            return PartialView("_Grupos", model);
        }

        public IActionResult Desafios()
        {
            var model = new CommunityViewModel { ActiveSection = "Desafios" };
            return PartialView("_Desafios", model);
        }

        public IActionResult Amigos()
        {
            var model = new CommunityViewModel { ActiveSection = "Amigos" };
            return PartialView("_Amigos", model);
        }

        public IActionResult Eventos()
        {
            var model = new CommunityViewModel { ActiveSection = "Eventos" };
            return PartialView("_Eventos", model);
        }
    }
}
