using System.Web.Mvc;

namespace DeviceManagement.Controllers
{
    public class InspectionController : Controller
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
            ViewBag.IsEdit = true;
            ViewBag.RequestId = id;
            return View("Form");
        }

        public ActionResult Display(int id)
        {
            ViewBag.RequestId = id;
            return View();
        }
    }
}
