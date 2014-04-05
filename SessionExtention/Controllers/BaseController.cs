using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using SessionExtentionStore;

namespace SessionExtention.Controllers
{
    public class BaseController : Controller
    {
        private StoreContainer _store;
        public StoreContainer Store
        {
            get
            {
                if (!string.IsNullOrEmpty(Session.SessionID))
                {
                    Session["__TempCreate__"] = 1;
                    return new StoreContainer(Session.SessionID);
                }
                return _store ?? (_store = new StoreContainer(Session.SessionID));
            }
        }
    }
}
