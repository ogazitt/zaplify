/* user.js */

var user = user || {};

// manage settings dialog 

user.openManageSettingsDialog = function () {
    if (user.manageSettingsDialog == undefined)
        user.manageSettingsDialog = user.createManageSettingsDialog();
    // open the dialog
    dialogs.openDialog(user.manageSettingsDialog);

    // insert values into the fields of the dialog
    // this loop assumes the fields are all strings
    for (var ix in user.User) {
        var value = user.User[ix];
        if (value == null)
            value = "";
        var fieldName = ".managesettingsvalue-" + ix;
        $(fieldName).attr("value", value);
    }
};

user.createManageSettingsDialog = function () {
    var buttons = {};
    buttons[language.data.edit_ok] = function () { user.manageSettings(); dialogs.closeDialog(user.manageSettingsDialog); };
    buttons[language.data.edit_cancel] = function () { $(this).dialog('close') };

    // construct the dialog fields
    //   note - width='200' is required on td and span fields - haven't figured out why the 'fieldname' (appropriately styled in TaskStore.css)
    //   doesn't take care of the width automatically. 
    var table = "<table>";
    for (var ix in user.User) {
        table += "<tr>";
        table += "<td width='200'><span class='fieldname' width='200'>" + ix + "</span></td>";
        table += "<td>";
        table += "<input type='text' class='dialoginputfield managesettingsfield managesettingsvalue-" + ix + "' rel='" + ix +"'/>";
        table += "</td></tr>";
    }
    table += "</table>";

    // construct the html for the dialog
    var dlghtml = "<div class='editdialogcontent'>" +
                  table +
                  "</div>";

    // create the dialog
    var dlg = $('<div></div>').html(dlghtml).dialog({
        autoOpen: false,
        draggable: false,
        modal: true,
        dialogClass: 'dialog-manage-settings',
        title: language.data.manage_settings_dialog,
        buttons: buttons,
        open: function (event, ui) {
            $('.ui-dialog-buttonset button:last').focus();
            $('.ui-dialog-buttonset button:last').addClass("input-bold");
        }
    });
    return dlg;
};

user.manageSettings = function () {
    var oldUser = $.extend(true, {}, user.User);
    $(".managesettingsfield").each(function (index) {
        var fieldName = $(this).attr("rel");
        var value = $(this).val();
        if (value == "")
            value = null;
        user.User[fieldName] = value;
    });

    // call the web service
    taskstore.manageSettings(oldUser, user.User);
};

user.bindManageSettings = function () {
    $("#managesettings").die();
    $("#managesettings").live("click", function () {
        if (user.manageSettingsDialog == undefined || user.manageSettingsDialog.dialog("isOpen") == false) {
            user.openManageSettingsDialog();
        }
    });
};

// change password dialog

user.openChangePasswordDialog = function () {
    if (user.changePasswordDialog == undefined)
        user.changePasswordDialog = user.createChangePasswordDialog();
    // open the dialog
    dialogs.openDialog(user.changePasswordDialog);

    // insert values into the fields of the dialog
    $(".changepasswordvalue-existing").val("");
    $(".changepasswordvalue-new").val("");
    $(".changepasswordvalue-repeat").val("");
    $(".changepassworderror").hide();
};

user.createChangePasswordDialog = function () {
    var buttons = {};
    buttons[language.data.edit_ok] = function () { user.changePassword(); };
    buttons[language.data.edit_cancel] = function () { $(this).dialog('close') };

    // construct the dialog fields
    //   note - width='200' is required on td and span fields - haven't figured out why the 'fieldname' (appropriately styled in TaskStore.css)
    //   doesn't take care of the width automatically. 
    var table = "<table>";
    // existing password
    table += "<tr>";
    table += "<td width='200'><span class='fieldname' width='200'>Existing Password</span></td>";  
    table += "<td>";
    table += "<input type='password' class='dialoginputfield changepasswordvalue-existing'/>";
    table += "</td></tr>";
    // new password
    table += "<tr>";
    table += "<td width='200'><span class='fieldname' width='200'>New Password</span></td>";
    table += "<td>";
    table += "<input type='password' class='dialoginputfield changepasswordvalue-new'/>";
    table += "</td></tr>";
    // repeat new password 
    table += "<tr>";
    table += "<td width='200'><span class='fieldname' width='200'>Repeat New Password</span></td>";
    table += "<td>";
    table += "<input type='password' class='dialoginputfield changepasswordvalue-repeat'/>";
    table += "</td></tr>";
    // error span
    table += "<tr>";
    table += "<td width='200'></td>";
    table += "<td>";
    table += "<span class='changepassworderror red'>foo</span>";
    table += "</td></tr>";

    table += "</table>";

    // construct the html for the dialog
    var dlghtml = "<div class='editdialogcontent'>" +
                  table +
                  "</div>";

    // create the dialog
    var dlg = $('<div></div>').html(dlghtml).dialog({
        autoOpen: false,
        draggable: false,
        modal: true,
        dialogClass: 'dialog-change-password',
        title: language.data.change_password_dialog,
        buttons: buttons,
        open: function (event, ui) {
            $('.ui-dialog-buttonset button:last').focus();
            $('.ui-dialog-buttonset button:last').addClass("input-bold");
        }
    });
    return dlg;
};

user.changePassword = function () {
    $(".changepassworderror").hide();
    var existingPasswd = $(".changepasswordvalue-existing").val();
    var newPasswd = $(".changepasswordvalue-new").val();
    var repeatPasswd = $(".changepasswordvalue-repeat").val();
    if (newPasswd != repeatPasswd) {
        $(".changepassworderror").html("Passwords do not match");
        $(".changepassworderror").show();
        return;
    }

    if (existingPasswd != user.User.Password) {
        $(".changepassworderror").html("Existing password does not match");
        $(".changepassworderror").show();
        return;
    }

    var newUser = $.extend(true, {}, user.User);
    newUser.Password = newPasswd;

    // call the web service
    taskstore.updateUser(user.User, newUser);

    // close the dialog
    dialogs.closeDialog(user.changePasswordDialog);
};

user.bindChangePassword = function () {
    $("#changepasswd").die();
    $("#changepasswd").live("click", function () {
        if (user.changePasswordDialog == undefined || user.changePasswordDialog.dialog("isOpen") == false) {
            user.openChangePasswordDialog();
        }
    });
};

