using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using TaskStoreWeb.Models;
using TaskStoreServerEntities;

namespace TaskStoreWeb.Controllers
{   
    public class CTasksController : Controller
    {
        private TaskStore context = new TaskStore();

        //
        // GET: /Tasks/

        public ViewResult Index()
        {
            ViewBag.PossibleTaskLists = context.TaskLists;
            ViewBag.PossiblePriorities = context.Priorities;
            return View(context.Tasks.Include(task => task.TaskTags).ToList());
        }

        //
        // GET: /Tasks/Details/5

        public ViewResult Details(System.Guid id)
        {
            Task task = context.Tasks.Single(x => x.ID == id);
            return View(task);
        }

        //
        // GET: /Tasks/Create

        public ActionResult Create()
        {
            ViewBag.PossibleTaskLists = context.TaskLists;
            ViewBag.PossiblePriorities = context.Priorities;
            return View();
        } 

        //
        // POST: /Tasks/Create

        [HttpPost]
        public ActionResult Create(Task task)
        {
            if (ModelState.IsValid)
            {
                task.ID = Guid.NewGuid();
                context.Tasks.Add(task);
                context.SaveChanges();
                return RedirectToAction("Index");  
            }

            ViewBag.PossibleTaskLists = context.TaskLists;
            ViewBag.PossiblePriorities = context.Priorities;
            return View(task);
        }
        
        //
        // GET: /Tasks/Edit/5
 
        public ActionResult Edit(System.Guid id)
        {
            Task task = context.Tasks.Single(x => x.ID == id);
            ViewBag.PossibleTaskLists = context.TaskLists;
            ViewBag.PossiblePriorities = context.Priorities;
            return View(task);
        }

        //
        // POST: /Tasks/Edit/5

        [HttpPost]
        public ActionResult Edit(Task task)
        {
            if (ModelState.IsValid)
            {
                context.Entry(task).State = EntityState.Modified;
                context.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.PossibleTaskLists = context.TaskLists;
            ViewBag.PossiblePriorities = context.Priorities;
            return View(task);
        }

        //
        // GET: /Tasks/Delete/5
 
        public ActionResult Delete(System.Guid id)
        {
            Task task = context.Tasks.Single(x => x.ID == id);
            return View(task);
        }

        //
        // POST: /Tasks/Delete/5

        [HttpPost, ActionName("Delete")]
        public ActionResult DeleteConfirmed(System.Guid id)
        {
            Task task = context.Tasks.Single(x => x.ID == id);
            context.Tasks.Remove(task);
            context.SaveChanges();
            return RedirectToAction("Index");
        }
    }
}