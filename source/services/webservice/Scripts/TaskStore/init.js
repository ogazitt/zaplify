/* init.js */

var init = init || {};

$(document).ready(function () {
    // define the outerHTML function
    $.fn.outerHTML = function () {
        var $this = $(this);
        if ($this.length > 1)
            return $.map($this, function (el) { return $(el).outerHTML(); }).join('');
        return $this.clone().wrap('<div/>').parent().html();
    };
    // initialize the page
    init.ajax_init(null, null);
    init.login_init();
    constants.refresh();
    taskstore.refresh();
    settings.init();
});

init.ajax_init = function(username, password) {
    $.ajaxSetup({
        accepts: "application/json",
        dataType: "json",
        contentType: "application/json",
        headers: { 
            "TaskStore-Username": username,
            "TaskStore-Password": password,
            "Accept": "application/json"
        }/*,
        error: function (xhr, textStatus, errorThrown) {
            alert("operation failed: HTTP " + xhr.status + "(" + textStatus + "); error: " + errorThrown);
        }
        */
    });
};

init.controls_init = function () {
    init.checkbox_init();
    html.make_timestamp_to_string();
    init.date_init();
    init.tabs_init();
    init.placeHolders_init();
    init.header_init();
    task.bindCreateTask();
    task.bindDeleteTask();
    task.bindEditTask();
    task.bindPriority();
    tasklist.bindDeleteList();
};

init.tabs_init = function () {
    var initialized = false;
    $("div#tasklists").tabs("destroy");
    $("div#tasklists").tabs({
        collapsible: true, // hack to make the current tab selectable
        select: function (event, ui) {
            // initialized current tab if it's not set yet
            if (taskstore.currentTab == undefined) {
                for (ix in taskstore.tabs) {
                    taskstore.currentTab = ix;
                    break;
                }
            }

            // check if the tab clicked is the current tab
            var id = ui.tab.hash.substring("#tabs-".length);
            var ix = $("#tasklists").tabs("option", "selected");
            var curTab = taskstore.tabs[id];
            if (curTab == ix) {
                // prevent the tab from collapsing
                event.preventDefault();
                // unless we're initializing (which means this isn't a real user event), 
                // bring up edit dialog with the GUID for the current list
                if (initialized == true)
                    tasklist.editList(id);
            }
            else {
                // bring up the edit dialog if user clicked on the newlist tab
                if (id == "newlist") {
                    // if the newlist tab is the only tab, prevent the tab from being collapsed
                    if (tasklist.currentTab == "newlist")
                        event.preventDefault();
                    tasklist.editList(id);
                }
                else
                    taskstore.currentTab = id; // ui.index;
            }
        }
        /*
        ,tabTemplate: "<li><a href='#{href}'>#{label}</a><span class='ui-icon ui-icon-close'>Remove Tab</span></li>",
        add: function( event, ui ) {
        var tab_content = $tab_content_input.val() || "Tab " + tab_counter + " content.";
        $( ui.panel ).append( "<p>" + tab_content + "</p>" );
        }
        */
    });
    if (taskstore.currentTab != undefined)
        $("div#tasklists").tabs("select", taskstore.tabs[taskstore.currentTab]);
    initialized = true;
};

init.placeHolders_init = function () {
    $(':input[placeholder]').each(function () {
        var $this = $(this); 
        if ($this.val() === '') { $this.val($this.attr('placeholder')); }
        $this.focus(function () {
            if (($this).val() === $this.attr('placeholder')) {
                $(this).val('');
            }
        });
        $this.blur(function () {
            if ($(this).val() === '') {
                $(this).val($this.attr('placeholder'));
            } 
        });
    });
};

init.checkbox_init = function () {
    var checkClicked = false;
    $(".checkboxicon").die();
    $(".checkboxicon").live("click", function (event) {
        if (checkClicked == false) {
            checkClicked = true;
            $(this).toggleClass("checked");
            var is_checked = $(this).hasClass("checked");

            var id = $(this).parent().attr("id");
            var list = $(this).parent().attr("rel");

            if (id == undefined || list == undefined) {
                checkClicked = false;
                return;
            }

            var oldTask = taskstore.TaskLists[list].Tasks[id];

            // make a deep copy of the task
            var newTask = $.extend(true, {}, oldTask);

            if (is_checked) {
                newTask.Complete = true;
            }
            else {
                newTask.Complete = false;
            }
            // call the web service
            taskstore.updateTask(oldTask, newTask);

            // hide the clicked task
            $(this).parent().fadeOut(1000);
        }
        setTimeout(function () {
            checkClicked = false;
        }, 100);
    });
};

init.date_init = function() {
    $(".datepicker").datepicker("destroy");
    $('.tasklist li .showdate').live('click', function () {
        var object = $(this).parent(); 
        description = $(this).parent().find(".description"); 
        dateInput = $(this).parent().find(".datepicker"); 
        description.after("<input type='hidden' class='datepicker'/>"); 
        html.createDatepicker(); 
        datePickerInput = $(this).parent().find(".datepicker"); 
        datePickerImage = $(this).parent().find(".ui-datepicker-trigger"); 
        datePickerImage.click().remove(); 
        setTimeout(function () {
            if (object.find("input.hasDatepicker").length == 2)
                object.find("input.hasDatepicker").eq(0).remove();
        }, 100);
    });
};

init.header_init = function () {
    $(".header-pri").die();
    $(".header-pri").live("click", function (event) {
        if (taskstore.sortby == "PriorityID") {
            taskstore.sortreverse = !taskstore.sortreverse;
        }
        else {
            taskstore.sortby = "PriorityID";
            taskstore.sortreverse = true;
        }
        html.render_lists();
    });
    $(".header-name").die();
    $(".header-name").live("click", function (event) {
        if (taskstore.sortby == "Name") {
            taskstore.sortreverse = !taskstore.sortreverse;
        }
        else {
            taskstore.sortby = "Name";
            taskstore.sortreverse = false;
        }
        html.render_lists();
    });
    $(".header-date").die();
    $(".header-date").live("click", function (event) {
        if (taskstore.sortby == "Due") {
            taskstore.sortreverse = !taskstore.sortreverse;
        }
        else {
            taskstore.sortby = "Due";
            taskstore.sortreverse = false;
        }
        html.render_lists();
    });
};

init.login_init = function () {
    $("#loggedin")
		.button({
		    text: true,
		    icons: {
		        primary: "ui-icon-gear",
		        secondary: "ui-icon-triangle-1-s"
		    }
		})
		.click(function () {
		    // open the menu
		    $("#loginmenu").toggle("blind", null, 150);
		    // create a mousedown handler that closes the menu for any mousedown event
		    $("*").mousedown(function (event, ui) {
		        $("*").unbind("mousedown");
		        $("#loginmenu").hide();
		        // hack(y): switch on the text inside the target for the menu item to invoke
		        switch (event.target.innerHTML) {
		            case "Manage Settings":
		                user.openManageSettingsDialog();
		                break;
		            case "Change Password":
		                user.openChangePasswordDialog();
		                break;
		            case "Sign Out":
		                //document.getElementById("logoff").click();
		                window.location = "/Account/Logoff";
		                break;
		        };
		    });
		});

    // create the menu (hidden)
    $("#loginmenu").menu({
        focus: function (event, ui) {
        }
    }).hide();

    /*
    $("#signout").click(function () {
    document.getElementById("logoff").click();
    //$("#logoff").click();
    });
    */
};