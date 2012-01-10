/* tasklist.js */

var tasklist = tasklist || {};

tasklist.getList = function (element) {
    var list = element.parent("li").children("a").attr("href").substring("#tabs-".length);
    var tl = taskstore.TaskLists[list];
    return tl;
};

var deleteListDialog;

tasklist.openListDeletePrompt = function (deleteElement) {
    var buttonsDeleteList = {};
    buttonsDeleteList[language.data.delete_list_no] = function () { $(this).dialog("close") };
    buttonsDeleteList[language.data.delete_list_yes] = function () {
        var list = tasklist.getList(deleteElement);
        taskstore.currentTab = undefined;
        taskstore.removeList(list);

        /*
        var $tabs = $("#tablist");
        var index = $("li", $tabs).index($(this).parent());
        // remove the list
        $tabs.tabs("remove", index);
        */

        dialogs.closeDialog(deleteListDialog);
    };
    deleteListDialog = $('<div></div>').dialog({
        autoOpen: false,
        draggable: false,
        modal: true,
        dialogClass: 'dialog-delete-list',
        title: language.data.delete_list_question,
        buttons: buttonsDeleteList,
        open: function (event, ui) {
            $('.ui-dialog-buttonset button:last').focus();
            $('.ui-dialog-buttonset button:last').addClass("input-bold");
        }
    });

    dialogs.openDialog(deleteListDialog);
};

tasklist.bindDeleteList = function () {
    $("#tablist span.ui-icon-close").live("click", function () {
        var deleteElement = $(this);
        if (deleteListDialog == undefined || deleteListDialog.dialog("isOpen") == false) {
            tasklist.openListDeletePrompt(deleteElement);
        }
    });
};

var editListDialog;

tasklist.createListEditDialog = function () {
    var buttonsEditList = {};
    buttonsEditList[language.data.edit_ok] = function () { tasklist.saveList(tasklist.currentID); dialogs.closeDialog(editListDialog); };
    buttonsEditList[language.data.edit_cancel] = function () {
        dialogs.closeDialog(editListDialog);
        // if the dialog was canceled and was positioned on the new list tab, reset to the first tab
        if (tasklist.currentID == "newlist") {
            taskstore.currentTab = undefined;
            html.render_lists();
            //$("#tasklists").tabs("select", 0);
        }
    };
    var listPicker = html.createListPicker(constants.ListTypes);
    listPicker = $(listPicker).addClass("dialoginputfield").addClass("listtype").outerHTML();
    var template = html.checkboxElement(false);
    template = $(template).addClass("listtemplate").outerHTML();
    // construct the dialog fields
    //   note - width='200' is required on td and span fields - haven't figured out why the 'fieldname' (appropriately styled in TaskStore.css)
    //   doesn't take care of the width automatically. 
    var table = "<table>";
    // Name
    table += "<tr>";
    table += "<td width='200'><span class='fieldname' width='200'>Name</span></td>";
    table += "<td><input type='text' class='dialoginputfield listname' value=''></input></td>";
    table += "</tr>";
    // Type
    table += "<tr>";
    table += "<td><span>Type</span></td>";
    table += "<td>" + listPicker + "</td>";
    table += "</tr>";
    // Template
    table += "<tr>";
    table += "<td><span>Template</span></td>";
    table += "<td>" + template + "</td>";
    table += "</tr>";
    table += "</table>";
    // create the dialog
    editListDialog = $('<div></div>').html(table).dialog({
        autoOpen: false,
        draggable: false,
        modal: true,
        dialogClass: 'dialog-edit-list',
        title: language.data.edit_list_dialog,
        buttons: buttonsEditList,
        open: function (event, ui) {
            $('.ui-dialog-buttonset button:first').focus();
            $('.ui-dialog-buttonset button:first').addClass("input-bold");
        }
    });
};

tasklist.openListEditDialog = function (id) {
    tasklist.currentID = id;
    if (editListDialog == undefined)
        tasklist.createListEditDialog();

    // open the dialog
    dialogs.openDialog(editListDialog);

    if (id != "newlist") {
        // get the list type
        var list = taskstore.TaskLists[id];
        var listType = constants.ListTypes[list.ListTypeID];

        // initialize the dialog values
        $(".listname").val(list.Name);
        $(".listtype").val(listType.Name);
        if (list.Template)
            $(".listtemplate").addClass("checked");
        else
            $(".listtemplate").removeClass("checked");
    }
    else {
        $(".listname").val("New List");
}
};

tasklist.saveList = function (id) {
    var listName = $(".listname").val();
    var listType = $(".listtype").val();
    var listTemplate = $(".listtemplate").hasClass("checked");
    if (listName == null || listName == undefined)
        return;
    if (listType == null || listType == undefined)
        return;
    if (listTemplate == null || listTemplate == undefined)
        return;
    var listTypeID = constants.ListTypesByName[listType].ID;

    if (id == "newlist") {
        var list = {};
        list.Name = listName;
        list.ListTypeID = listTypeID;
        list.Template = listTemplate;

        // call the web service and update the html
        taskstore.insertList(list);
    }
    else {
        var list = taskstore.TaskLists[id];
        var oldList = $.extend(true, {}, list);
        list.Name = listName;
        list.ListTypeID = listTypeID;
        list.Template = listTemplate;

        // call the web service and update the html
        taskstore.updateList(oldList, list);
    }
};

tasklist.editList = function (id) {
    if (editListDialog == undefined || editListDialog.dialog("isOpen") == false) {
        tasklist.openListEditDialog(id);
    }
};

tasklist.bindEditList = function () {
/*
    $("li span.description").live("click", function () {
        var listElement = $(this);
        if (editListDialog == undefined || editListDialog.dialog("isOpen") == false) {
            tasklist.openListEditDialog(listElement);
        }
    });
    */
};
