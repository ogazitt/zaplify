/* checkbox.js */

var checkbox = checkbox || {};
checkbox.hook_click = function () {
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
        }
        setTimeout(function () {
            checkClicked = false;
        }, 100);
    });
}
