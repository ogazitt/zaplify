/* taskstore.js */

var taskstore = taskstore || {};
// tasklists is an array
// TaskLists is an associative array (object) organized by TaskList ID
// tasklists.tasks is an array
// TaskLists.Tasks is an associative array organized by Task ID

taskstore.refresh = function () {
    $("#loadingimg").show(); 
    // get tasklists
    $.ajax({
        url: "/users",
        contentType: "application/json",
        statusCode: {
            404: function () {
                $(".add").remove();
                $("#loadingimg").hide(); 
                $("#tasklists").html("<p>Not logged in - please login using the link in the top right side of the page.</p>");
            },
            403: function () {
                $(".add").remove();
                $("#loadingimg").hide(); 
                $("#tasklists").html("<p>Not logged in - please login using the link in the top right side of the page.</p>");
            },
            500: function () {
                $(".add").remove();
                $("#loadingimg").hide(); 
                $("#tasklists").html("<p>Our sincere apologies - the taskstore web application is experiencing difficulties.  Please try again later.</p>");
            }
        },
        success: function (data, textStatus, xhr) {
            // validate the response
            var response = ajaxhelper.validate(data);
            // reinitialize the jquery ajax helper with the user credentials
            init.ajax_init(response.Name, response.Password);
            // save the username and password in the user object
            user.User = {
                ID: response.ID,
                Name: response.Name,
                Password: response.Password,
                Email: response.Email
            };

            // save the tasklists array into the tasklists property
            taskstore.tasklists = response.TaskLists;
            taskstore.TaskLists = {};
            taskstore.tabs = {};
            // copy the tasklists from the array returned into object properties (which is used as an associative array)
            for (var ix = 0; ix < taskstore.tasklists.length; ix++) {
                // get the current tasklist and copy it into a new key in the TaskLists object
                var tl = taskstore.tasklists[ix];
                taskstore.TaskLists[tl.ID] = tl;
                taskstore.tabs[tl.ID] = ix;
                // initialize a new tasks object in the TaskList
                taskstore.TaskLists[tl.ID].tasks = taskstore.TaskLists[tl.ID].Tasks;
                taskstore.TaskLists[tl.ID].Tasks = {};
                for (var jx = 0; jx < tl.tasks.length; jx++) {
                    // copy the task from the array to the object using the ID as the key
                    var t = tl.tasks[jx];
                    taskstore.TaskLists[tl.ID].Tasks[t.ID] = t;
                    //REMOVE
                    //if (t.Due != undefined && t.Due != null)
                    //    t.Due = new Date(parseInt(t.Due.replace(/(^.*\()|([+-].*$)/g, '')));
                    // add a property called Due which is the Date value for the DueDate string property
                    if (t.DueDate != undefined && t.DueDate != null)
                        t.Due = new Date(t.DueDate);
                    else
                        t.Due = null;
                }
            }

            // turn off the "loading" animation
            $("#loadingimg").hide(); 

            // render the retrieved tasklist
            html.render_lists();
        }
    });
};

taskstore.routes = {
    task: "tasks",
    tasklist: "tasklists",
    user: "users"
};

taskstore.updateList = function (oldList, newList) {
    var list = taskstore.update("tasklist", oldList, newList, function (list) {
        // replace the existing list with the new one from the server
        taskstore.TaskLists[list.ID].Name = list.Name;
        taskstore.TaskLists[list.ID].ListTypeID = list.ListTypeID;
        taskstore.TaskLists[list.ID].Template = list.Template;
        html.render_lists();
    });
};

taskstore.updateTask = function (oldTask, newTask) {
    // create copies of the old and new tasks
    var oldTaskCopy = $.extend(true, {}, oldTask);
    var newTaskCopy = $.extend(true, {}, newTask);
    //REMOVE
    //if (oldTaskCopy.Due != null)
    //    oldTaskCopy.Due = html.serializeDate(oldTaskCopy.Due);
    if (newTaskCopy.Due != null)
        newTaskCopy.DueDate = html.getUniversalDateString(newTaskCopy.Due);
    else
        newTaskCopy.DueDate = null;
    // update the LastModified date and serialize in JSON format - /Date(value-TZ)/
    newTaskCopy.LastModified = html.serializeDate(new Date());
    // remove the Due property from the objects to serialize
    oldTaskCopy.Due = undefined;
    newTaskCopy.Due = undefined;
    //undate the task
    taskstore.update("task", oldTaskCopy, newTaskCopy, function (t) {
        if (t != null && t != undefined) {
            //REMOVE
            //if (t.Due != undefined && t.Due != null)
            //    t.Due = new Date(parseInt(t.Due.replace(/(^.*\()|([+-].*$)/g, '')));
            if (t.DueDate != undefined && t.DueDate != null)
                t.Due = new Date(t.DueDate);
            // replace the existing task with the new one from the server
            taskstore.TaskLists[t.TaskListID].Tasks[t.ID] = t;
            html.render_lists();
        }
    });
};

taskstore.update = function (objType, objOld, objNew, successFunc) {
    var route = taskstore.routes[objType];
    if (route == null)
        return;

    var content = JSON.stringify(new Array(objOld, objNew));
    // update the object
    $.ajax({
        url: "/" + route + "/" + objNew.ID,
        type: "PUT",
        contentType: "application/json",
        data: content,
        success: function (data, textStatus, xhr) {
            // validate the response
            var resp = ajaxhelper.validate(data);
            if (resp != null && resp != undefined)
                successFunc(resp);
        }
    });
}

taskstore.insertList = function (obj) {
    taskstore.insert("tasklist", obj, function (list) {
        // create a new list using the server-assigned ID
        list.Tasks = {};
        taskstore.TaskLists[list.ID] = list;
        var ix = $("#tasklists").tabs("option", "selected");
        taskstore.tabs[list.ID] = ix;
        taskstore.currentTab = list.ID;
        html.render_lists();
    });
};

taskstore.insertTask = function (obj) {
    taskstore.insert("task", obj, function (t) {
        if (t.Due != undefined && t.Due != null)
            t.Due = new Date(parseInt(t.Due.replace(/(^.*\()|([+-].*$)/g, '')));
        // create a new task in the tasklist using the server-assigned IDs
        taskstore.TaskLists[t.TaskListID].Tasks[t.ID] = t;
        html.render_lists();

        // transfer focus back to the add control
        $("input.input-add").val("").blur().focus();
    });
};

taskstore.insertTaskIntoNewList = function (l, task) {
    taskstore.insert("tasklist", l, function (list) {
        // create a new list using the server-assigned ID
        list.Tasks = {};
        taskstore.TaskLists[list.ID] = list;
        var ix = $("#tasklists").tabs("option", "selected");
        taskstore.tabs[list.ID] = ix;
        taskstore.currentTab = list.ID;
        html.render_lists();

        // assign the new List ID to the task
        task.TaskListID = list.ID;
        taskstore.insertTask(task);
    });
};

taskstore.insert = function (objType, obj, successFunc) {
    var route = taskstore.routes[objType];
    if (route == null)
        return;

    var content = JSON.stringify(obj);
    // create the object (ID will be assigned by server)
    $.ajax({
        url: "/" + route,
        type: "POST",
        contentType: "application/json",
        data: content,
        success: function (data, textStatus, xhr) {
            // validate the response
            var resp = ajaxhelper.validate(data);
            if (resp != null && resp != undefined)
                successFunc(resp);
        },
    });
}

taskstore.removeList = function (obj) {
    taskstore.remove("tasklist", obj, function (response) {
        // remove the list
        if (taskstore.TaskLists[response.ID] != undefined) {
            var ix = $("#tasklists").tabs("option", "selected");
            delete taskstore.TaskLists[response.ID];
            delete taskstore.tabs[response.ID];
            // if the current tab is the one we just deleted, reset the current tab 
            if (taskstore.currentTab == response.ID)
                taskstore.currentTab = undefined;

            html.render_lists();
        }
    });
};

taskstore.removeTask = function (obj) {
    //REMOVE
    //if (obj.Due != null)
    //    obj.Due = html.serializeDate(obj.Due);
    if (obj.Due != null) {
        obj.DueDate = html.getUniversalDateString(obj.Due);
        obj.Due = undefined;
    }
    taskstore.remove("task", obj, function (response) {
        // remove the task from the tasklist
        if (taskstore.TaskLists[response.TaskListID].Tasks[response.ID] != undefined) {
            delete taskstore.TaskLists[response.TaskListID].Tasks[response.ID];
            html.render_lists();
        }
    });
};

taskstore.remove = function (objType, obj, successFunc) {
    var route = taskstore.routes[objType];
    if (route == null)
        return;

    var content = JSON.stringify(obj);
    // delete the object 
    $.ajax({
        url: "/" + route + "/" + obj.ID,
        type: "DELETE",
        contentType: "application/json",
        data: content,
        success: function (data, textStatus, xhr) {
            // validate the response
            var resp = ajaxhelper.validate(data);
            if (resp != null && resp != undefined)
                successFunc(resp);
        }
    });
};

taskstore.updateUser = function(oldUser, newUser) {
    //update the user
    taskstore.update("user", oldUser, newUser, function (u) {
        if (u != null && u != undefined) {
            user.User = {   
                ID: u.ID, 
                Name: u.Name,
                Password: u.Password,
                Email: u.Email
            };
            init.ajax_init(u.Name, u.Password);
        }
    });    
};