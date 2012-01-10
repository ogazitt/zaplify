using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TaskStoreWeb.Models;
using System.Web.Security;

namespace TaskStoreWeb.Controllers
{   
    public class TLController : Controller
    {
        //private TaskStore context = new TaskStore();

        //
        // GET: /TL/

        public ViewResult Index()
        {
            //return View(context.Tasks.Include(task => task.TaskTags).ToList());
            return View();
        }
    }
}