/* task.js */

var task = task || {};

task.ListTypeDialogs = {};

task.createTask = function () {
    createInProgress = true;
    if ($("input.input-add").val() != "") {
        // get the current list's index from the tab index (and convert from 0-based to 1-based)
        var listIndex = $("div#tasklists").tabs("option", "selected");
        if (listIndex < 0) listIndex = 0;
        listIndex++;
        // get the list ID by looking for the (1-based) nth <li> element of the list of tasklists <ul>
        // then get the href attribute from the <a> element, and strip off the "#tabs-" prefix (6 characters)
        var list = $("div#tasklists ul li:nth-child(" + listIndex + ") a").attr("href").substring("#tabs-".length);
        var name = $("input.input-add").val();
        if (name != "") {
            var t = {};
            t.Name = name;
            t.TaskListID = list;
            //t.PriorityID = 1;

            // if somehow this list doesn't have a real GUID (i.e. wasn't created on the server yet), do this now
            // this will happen only if the newlist is the only tab (e.g. when the app first initializes)
            if (list == "newlist") {
                list = {};
                list.Name = "New List";
                list.ListTypeID = constants.ListTypesByName["To Do List"].ID;
                list.Template = false;
                taskstore.insertTaskIntoNewList(list, t);
            }
            else {
                taskstore.insertTask(t);
            }
            createInProgress = false;
        }
        else {
            $("input.input-add").val("");
        }
    }
};

task.bindCreateTask = function () {
    var createInProgress = false;
    $("div.add input").die();
    $("div.add input").bind("keyup", function (e) {
        if (e.keyCode == 13 /* enter */ && createInProgress == false) {
            task.createTask();
            $("input.input-add").val("").blur().focus();
        }
        else if (e.keyCode == 27 /* esc */) {
            $(this).val("");
            $("div.add span.timestamp").remove();
            $(this).blur();
        }
    });
};

var deleteTaskDialog;

task.getTask = function (element) {
    var parent = element.parent("li");
    var id = parent.attr("id");
    var list = parent.attr("rel");
    var task = taskstore.TaskLists[list].Tasks[id];
    return task;
};

task.openTaskDeletePrompt = function (deleteElement) {
    var buttonsDeleteTask = {};
    buttonsDeleteTask[language.data.delete_task_no] = function () { $(this).dialog("close") };
    buttonsDeleteTask[language.data.delete_task_yes] = function () { 
        var t = task.getTask(deleteElement); 
        taskstore.removeTask(t);
        dialogs.closeDialog(deleteTaskDialog); 
    };
    deleteTaskDialog = $('<div></div>').dialog({
        autoOpen: false,
        draggable: false,
        modal: true,
        dialogClass: 'dialog-delete-task',
        title: language.data.delete_task_question,
        buttons: buttonsDeleteTask,
        open: function (event, ui) {
            $('.ui-dialog-buttonset button:last').focus();
            $('.ui-dialog-buttonset button:last').addClass("input-bold");
        }
    });

    dialogs.openDialog(deleteTaskDialog);
};

task.bindDeleteTask = function () {
    $("li span.delete").live("click", function () {
        var deleteElement = $(this);
        if (deleteTaskDialog == undefined || deleteTaskDialog.dialog("isOpen") == false) {
            task.openTaskDeletePrompt(deleteElement);
        }
    });
};

var editTaskDialog;

task.openTaskEditDialog = function (taskElement) {
    // get the list type
    var t = task.getTask(taskElement);
    var list = taskstore.TaskLists[t.TaskListID];
    var listType = constants.ListTypes[list.ListTypeID];
    task.currentTask = t;

    // each list type has an edit dialog that we cache.  if it doesn't exist, create it
    if (task.ListTypeDialogs[list.ListTypeID] == undefined)
        task.ListTypeDialogs[list.ListTypeID] = task.createTaskEditDialog(listType);
    editTaskDialog = task.ListTypeDialogs[list.ListTypeID];
    // open the dialog
    dialogs.openDialog(editTaskDialog);
    $("td input.datepicker").datepicker();
    $(".moreButton").die()
    $(".moreButton").button();
    $(".moreButton").show();
    $(".moreTable").hide();
    $(".moreButton").click(function () {
        $(".moreButton").hide();
        $(".moreTable").show();
    });

    // insert values into the fields of the dialog
    for (var ix in listType.Fields) {
        var field = listType.Fields[ix];
        var fieldType = constants.FieldTypes[field.FieldTypeID - 1];
        var value = t[fieldType.Name];
        if (value == null)
            value = "";
        var fieldName = ".taskfieldvalue-" + listType.ID + "." + fieldType.Name;

        switch (fieldType.DisplayType) {
            case "Date":
                $(fieldName).attr("value", html.getLocalizedDateString(value));
                break;
            case "Priority":
                if (value === "")
                    value = 1; // normal priority
                $(fieldName).val(constants.Priorities[value].Name);
                break;
            case "String":
            case "Address":
            case "PhoneNumber":
            case "Website":
            case "Email":
            case "TextBox":
            case "TagList":
            case "ListPointer":
            default:
                $(fieldName).attr("value", value);
                break;
            case "Boolean":
                if (value == true)
                    $(fieldName).addClass("checked");
                else
                    $(fieldName).removeClass("checked");
                break;
        }
    }
};

task.createTaskEditDialog = function (listType) {
    var buttonsEditTask = {};
    buttonsEditTask[language.data.edit_ok] = function () { task.saveTask(task.currentTask); dialogs.closeDialog(editTaskDialog); };
    buttonsEditTask[language.data.edit_cancel] = function () { $(this).dialog('close') };

    // construct the dialog fields
    //   note - width='108' is required on td and span fields - haven't figured out why the 'fieldname' (appropriately styled in TaskStore.css)
    //   doesn't take care of the width automatically. 
    var table = {};
    table["primary"] = "<table>";
    table["more"] = "<table class='moreTable'>";
    for (var ix in listType.Fields) {
        var field = listType.Fields[ix];
        var tableIndex = field.IsPrimary == true ? "primary" : "more";
        var fieldType = constants.FieldTypes[field.FieldTypeID - 1];
        var listTypeClass = "taskfieldvalue-" + listType.ID;

        table[tableIndex] += "<tr rel='" + fieldType.FieldTypeID + "'>";
        table[tableIndex] += "<td class='fieldname' width='108'><span class='fieldname'>" + fieldType.DisplayName + "</span></td>";
        table[tableIndex] += "<td>";
        switch (fieldType.DisplayType) {
            case "Date":
                table[tableIndex] += "<input type='date' class='dialoginputfield timestamp datepicker " + fieldType.Name + " " + listTypeClass + "'/>";
                break;
            case "Priority":
                var lp = html.createPriorityPicker(constants.Priorities);
                lp = $(lp).addClass("dialoginputfield").addClass(listTypeClass).addClass(fieldType.Name).outerHTML();
                table[tableIndex] += lp;
                break;
            case "String":
            case "Address":
            case "PhoneNumber":
            case "Website":
            case "Email":
            case "TextBox":
            case "TagList":
            case "ListPointer":
            default:
                table[tableIndex] += "<input type='text' class='dialoginputfield " + fieldType.Name + " " + listTypeClass + "'/>";
                break;
            case "Boolean":
                var checkbox = html.checkboxElement(false);
                checkbox = $(checkbox).addClass(listTypeClass).addClass(fieldType.Name).outerHTML();
                table[tableIndex] += checkbox;
                break;
        }
        table[tableIndex] += "</td></tr>";
    }
    table["primary"] += "</table>";
    table["more"] += "</table>";

    // construct the html for the dialog
    var dlghtml = "<div class='editdialogcontent'>" +
                  table["primary"] + 
                  "<button class='moreButton' width='150'>More Details</button>" +
                  table["more"] + 
                  "</div>";

    // create the dialog
    var dlg = $('<div></div>').html(dlghtml).dialog({
        autoOpen: false,
        draggable: false,
        modal: true,
        dialogClass: 'dialog-edit-task',
        title: language.data.edit_task_dialog,
        buttons: buttonsEditTask,
        open: function (event, ui) {
            $('.ui-dialog-buttonset button:last').focus();
            $('.ui-dialog-buttonset button:last').addClass("input-bold");
        }
    });
    return dlg;
};

task.saveTask = function (t) {
    // get the list type
    var oldTask = $.extend(true, {}, t);
    var list = taskstore.TaskLists[t.TaskListID];
    var listType = constants.ListTypes[list.ListTypeID];
    // construct the dialog fields
    //$(".taskfieldvalue").each(function (index) {
    $(".taskfieldvalue-" + listType.ID).each(function (index) {
        var fieldTypeID = $(this).parents("tr").attr("rel");
        var fieldType = constants.FieldTypes[fieldTypeID - 1];
        var value = $(this).val();
        if (value == "")
            value = null;
        switch (fieldType.DisplayType) {
            case "Date":
                if (value == null || value == "" || value == undefined)
                    t[fieldType.Name] = null;
                else
                    t[fieldType.Name] = new Date(value);
                break;
            case "String":
            case "Address":
            case "PhoneNumber":
            case "Website":
            case "Email":
            case "TextBox":
            case "TagList":
            case "ListPointer":
            default:
                t[fieldType.Name] = value;
                break;
            case "Boolean":
                var is_checked = $(this).hasClass("checked");
                t[fieldType.Name] = is_checked;
                break;
            case "Priority":
                t[fieldType.Name] = constants.PrioritiesByName[value].PriorityID;
                break;
        }
    });

    // call the web service
    taskstore.updateTask(oldTask, t);
};

task.bindEditTask = function () {
    $("li span.description").die();
    $("li span.description").live("click", function () {
        var taskElement = $(this);
        if (editTaskDialog == undefined || editTaskDialog.dialog("isOpen") == false) {
            task.openTaskEditDialog(taskElement);
        }
    });
};

task.bindPriority = function () {
    $("ul.mainlist span.lowpri").die();
    $("ul.mainlist span.lowpri").live("click", function () {
        var element = $(this);
        element.removeClass("lowpri").addClass("normalpri");

        var oldTask = task.getTask(element);

        // make a deep copy of the task
        var newTask = $.extend(true, {}, oldTask);
        newTask.PriorityID = 1;
        
        taskstore.updateTask(oldTask, newTask);
    });
    $("ul.mainlist span.normalpri").die();
    $("ul.mainlist span.normalpri").live("click", function () {
        var element = $(this);
        element.removeClass("normalpri").addClass("highpri");

        var oldTask = task.getTask(element);

        // make a deep copy of the task
        var newTask = $.extend(true, {}, oldTask);
        newTask.PriorityID = 2;

        taskstore.updateTask(oldTask, newTask);
    });
    $("ul.mainlist span.highpri").die();
    $("ul.mainlist span.highpri").live("click", function () {
        var element = $(this);
        element.removeClass("highpri").addClass("lowpri");

        var oldTask = task.getTask(element);

        // make a deep copy of the task
        var newTask = $.extend(true, {}, oldTask);
        newTask.PriorityID = 0;

        taskstore.updateTask(oldTask, newTask);
    });
};
