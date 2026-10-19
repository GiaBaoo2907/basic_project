using System.Web.Mvc;

namespace DeviceManagement.Controllers
{
    public class RequestController : Controller
    {
        // GET: /Request/
        public ActionResult Index()
        {
            return RedirectToAction("Index", "Home");
        }

        // GET: /Request/Form
        public ActionResult Form()
        {
            ViewBag.IsEdit = false;
            return View("Form");
        }

        // GET: /Request/Create
        public ActionResult Create()
        {
            ViewBag.IsEdit = false;
            return View("Form");
        }

        // GET: /Request/Edit/5
        public ActionResult Edit(int id)
        {
            ViewBag.IsEdit = true;
            ViewBag.RequestId = id;
            return View("Form");
        }

        // GET: /Request/Display/5
        public ActionResult Display(int id)
        {
            ViewBag.RequestId = id;
            return View();
        }
    }
}
