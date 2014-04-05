using System.Web.Mvc;

namespace SessionExtention.Controllers
{
    public class SampleController : BaseController
    {

        public ContentResult Index()
        {
            Store["test"] = "test";
            return Content(Store["test"].ToString());
        }

        public ContentResult Test()
        {
            return Content(Store["test"].ToString());
        }

    }
}
