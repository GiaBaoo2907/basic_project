using System.Web.Mvc;

namespace DeviceManagement.Controllers
{
    public class AccidentController : Controller
    {
        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Create()
        {
            ViewBag.IsEdit = false;
            return View("Form");
        }

        public ActionResult Edit(int id)
        {
            ViewBag.AccidentId = id;
            ViewBag.IsEdit = true;
            return View("Form");
        }

        public ActionResult Display(int id)
        {
            ViewBag.AccidentId = id;
            return View();
        }

        public ActionResult Delete(int id)
        {
            ViewBag.AccidentId = id;
            return View();
        }
    }
}
