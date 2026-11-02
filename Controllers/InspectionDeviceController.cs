using System.Web.Mvc;

namespace DeviceManagement.Controllers
{
    public class InspectionDeviceController : Controller
    {
        public ActionResult Index() { return View(); }
        public ActionResult Create() { return View(); }
        public ActionResult Edit(int id) { ViewBag.DeviceId = id; return View(); }
        public ActionResult Display(int id) { ViewBag.DeviceId = id; return View(); }
    }
}
