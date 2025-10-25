using Microsoft.AspNetCore.Mvc;

namespace LifeHub.Controllers
{
    public class SubscriptionController : Controller
    {
        public IActionResult PlanSelection()
        {
            // Manejo seguro de valores nulos con operador de coalescencia nula
            var planId = TempData["SelectedPlanId"] as int? ?? 0;
            var planName = TempData["SelectedPlanName"] as string ?? "Plan Premium";

            ViewBag.PlanId = planId;
            ViewBag.PlanName = planName;
            
            return View();
        }

        public IActionResult Checkout(int planId)
        {
            // Validar que el planId sea válido
            if (planId <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PlanId = planId;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult ProcessCheckout(int planId)
        {
            // Validar que el planId sea válido
            if (planId <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            // Lógica para procesar el pago
            // Aquí integrarías con Stripe o otro procesador de pagos
            
            return RedirectToAction("PaymentSuccess", new { planId });
        }

        public IActionResult PaymentSuccess(int planId)
        {
            // Validar que el planId sea válido
            if (planId <= 0)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewBag.PlanId = planId;
            return View();
        }
    }
}