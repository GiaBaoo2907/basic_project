using System.Web.Mvc;

namespace DeviceManagement.Controllers
{
    public class DeviceController : Controller
    {
        // GET: /Device/
        public ActionResult Index()
        {
            return View();
        }

        // GET: /Device/Create
        public ActionResult Create()
        {
            return View();
        }

        // GET: /Device/Edit/5
        public ActionResult Edit(int id)
        {
            ViewBag.DeviceId = id;
            return View();
        }

        // GET: /Device/Display/5
        public ActionResult Display(int id)
        {
            ViewBag.DeviceId = id;
            return View();
        }
    }
}
