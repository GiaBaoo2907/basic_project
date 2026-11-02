using System.Web.Mvc;
using System.Linq;
using DeviceManagement.Models;

namespace DeviceManagement.Controllers
{
    public class ManageController : Controller
    {
        private static readonly ManageCategory[] Categories =
        {
            new ManageCategory { Key = "roles", Name = "Vai trò", Description = "Danh mục vai trò người dùng." },
            new ManageCategory { Key = "departments", Name = "Cấu hình phòng ban", Description = "Danh mục và cấu hình phòng ban." },
            new ManageCategory { Key = "navigation", Name = "Dẫn hướng", Description = "Cấu trúc nhóm và danh mục trên thanh điều hướng." },
            new ManageCategory { Key = "statuses", Name = "Tình trạng", Description = "Danh mục tình trạng sử dụng." },
            new ManageCategory { Key = "units", Name = "Đơn vị tính", Description = "Danh mục đơn vị tính." },
            new ManageCategory { Key = "classifications", Name = "Phân loại", Description = "Danh mục phân loại thiết bị và tài sản." },
            new ManageCategory { Key = "projects", Name = "Bộ phận/Dự án", Description = "Danh mục bộ phận và dự án." },
            new ManageCategory { Key = "insurance-types", Name = "Loại bảo hiểm", Description = "Danh mục các loại bảo hiểm.", Group = "Quản lý bảo hiểm" },
            new ManageCategory { Key = "suppliers", Name = "Nhà cung cấp", Description = "Danh mục nhà cung cấp bảo hiểm.", Group = "Quản lý bảo hiểm" },
            new ManageCategory { Key = "materials", Name = "Vật tư", Description = "Danh mục vật tư." },
            new ManageCategory { Key = "inspection-types", Name = "Loại kiểm định", Description = "Danh mục loại kiểm định áp dụng cho thiết bị kiểm định.", Group = "Quản lý kiểm định" },
            new ManageCategory { Key = "accidents-cause", Name = "Nguyên nhân", Description = "Danh mục nguyên nhân tai nạn/sự cố.", Group = "Quản lý tai nạn/sự cố" },
            new ManageCategory { Key = "accidents-types", Name = "Loại tai nạn/sự cố", Description = "Danh mục loại tai nạn/sự cố.", Group = "Quản lý tai nạn/sự cố" }
        };

        public ActionResult Index()
        {
            return View(Categories);
        }

        public ActionResult Category(string id)
        {
            var category = Categories.FirstOrDefault(item => item.Key == id);
            if (category == null || category.Key == "navigation")
            {
                return HttpNotFound();
            }

            ViewBag.IsEditable = new[] 
            { 
                "statuses", "units", "classifications", "projects", "insurance-types", "suppliers", "materials",
                "inspection-types",
                "accidents-cause", "accidents-types"
            }
                .Contains(category.Key);
            return View(category);
        }

        public ActionResult Navigation()
        {
            return View();
        }
    }
}
