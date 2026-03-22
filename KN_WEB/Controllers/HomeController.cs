using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace KN_WEB.Controllers
{
    public class HomeController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult About()
        {
            return View();
        }

        public ActionResult Horario()
        {
            return View();
        }

        public ActionResult Inicio()
        {
            return View();
        }

        public ActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public ActionResult Login(string nombre, string apellido, string email, string password, string confirmarPassword)
        {
            if (password != confirmarPassword)
            {
                ViewBag.Mensaje = "Las contraseñas no coinciden.";
                return View();
            }

            ViewBag.Mensaje = "Registro enviado correctamente. Falta guardar en base de datos.";
            return View();
        }

        public ActionResult Informacion()
        {
            return View();
        }
    }
}