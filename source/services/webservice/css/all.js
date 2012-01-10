/**** wunderlist.js ****/

var wunderlist = wunderlist || {}; wunderlist.list_cache = new Array(); wunderlist.filter_cache = new Array(); wunderlist.active_list = 0;
$(function () { background.init(); filters.init(); sidebarFF.init(); share.init(); language.replaceBasics(); wunderlist.activeTheMagic($('a.list:first').attr('id').replace('list', '')); });

wunderlist.loadContent = function (content, list_id) { var element = $('#content'); element.html("").hide(); element.html(content); element.show(); wunderlist.activeTheMagic(list_id); }; 
wunderlist.activeTheMagic = function (list_id) {
    if (list_id != undefined)
    { wunderlist.active_list = list_id; html.activateDragAndDrop(list_id); }
    bindAddTask(); makeSortable(); makeListsDropable(); makeFilterDropable(); html.make_timestamp_to_string(); html.createDatepicker(); search.clear();
}; 

wunderlist.liveSearch = function (value) {
    $.ajax({ url: '/ajax/search/', type: 'POST', data: { 'search': value }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success')
            wunderlist.loadContent(response.data);
    }
    });
}; 

wunderlist.updateSettings = function () {
    $.ajax({ url: '/ajax/settings', type: 'POST', data: { 'settings': JSON.stringify(settings) }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success')
        { settings.updateLanguage(); settings.updateBackground(); settings.updateDateformat(); settings.updateWeekstartday(); settings.updateDeleteprompt(); settings.updateSidebarPosition(); }
        settings.setDefault();
    }, error: settings.setDefault
    });
}
wunderlist.recreateTutorials = function () {
    list.name = language.data.tutorials; $.ajax({ url: '/ajax/lists/insert/', type: 'POST', data: { 'list': JSON.stringify(list) }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success') {
            $('div#lists').append(html.generateNewListElementHTML('x', language.data.tutorials, 'none', 9)); list.id = response.id; list.show(true); setTimeout(function () {
                makeListsSortable(); for (ix = 1; ix <= 10; ix++) {
                    task.done = undefined; task.done_date = undefined; task.important = undefined; task.position = undefined; task.name = language.data['default_task_' + ix]; task.list_id = response.id; if (ix == 1)
                    { task.important = 1; task.position = 1; }
                    if (ix == 7)
                    { task.done = 1; task.done_date = html.getWorldWideDate(); }
                    task.insert(true);
                }
            }, 100);
        }
        list.setDefault();
    }, error: list.setDefault
    });
}
wunderlist.insertList = function () {
    $.ajax({ url: '/ajax/lists/insert/', type: 'POST', data: { 'list': JSON.stringify(list) }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success') {
            if (response.name != undefined)
                list.name = response.name; list.id = response.id; list.show();
        }
        list.setDefault();
    }, error: list.setDefault
    });
}; wunderlist.updateList = function () {
    $.ajax({ url: '/ajax/lists/update/', type: 'POST', data: { 'list': JSON.stringify(list) }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success') {
            if (response.name != undefined)
                list.name = response.name; list.updateName(); list.updateShared(); list.updateDeleted();
        }
        list.setDefault();
    }, error: list.setDefault
    });
}; wunderlist.updateListsPositions = function (lists) { $.ajax({ url: '/ajax/lists/positions/', type: 'POST', data: { 'lists': JSON.stringify(lists) }, success: function (response_data) { ajaxresponse.check(response_data); } }); }; 

wunderlist.updateListCount = function (list_id, count) {
    if (count != undefined && typeof count === 'number' && isFinite(count)) {
        $('div#lists a#list' + list_id + ' span').html(count); $('div#lists a').each(function () {
            if ($(this).attr('rel') == list_id)
                $(this).children('span').html(count);
        });
    }
    else {
        $.ajax({ url: '/ajax/lists/count/' + list_id, success: function (response_data, text, xhrobject) {
            var response = ajaxresponse.check(response_data); if (response.status == 'success') {
                $('div#lists a#list' + list_id + ' span').html(response.count); $('div#lists a').each(function () {
                    if ($(this).attr('rel') == list_id)
                        $(this).children('span').html(response.count);
                });
            }
        }
        });
    }
}; 

wunderlist.getListById = function (list_id) {
    if (wunderlist.list_cache[list_id] !== undefined) { cache = wunderlist.list_cache[list_id]; wunderlist.loadContent(cache.data, list_id, true); wunderlist.updateListCount(list_id, parseInt(cache.count)); fade_in_xhr = false; } else { fade_in_xhr = true; }
    $.ajax({ url: '/ajax/lists/id/' + list_id, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success')
        { wunderlist.loadContent(response.data, list_id, fade_in_xhr); wunderlist.updateListCount(list_id, parseInt(response.count)); wunderlist.list_cache[list_id] = response; $("a.list").droppable({ disabled: false }); $("#lists a#list" + list_id).droppable({ disabled: true }); }
    }
    });
}; wunderlist.getFilteredTasks = function (type, date_type) {
    if (wunderlist.filter_cache[type] !== undefined) { cache = wunderlist.filter_cache[type]; wunderlist.loadContent(cache.data, null, true); fade_in_xhr = false; } else { fade_in_xhr = true; }
    $.ajax({ url: '/ajax/lists/filtered/', type: 'POST', data: { 'current_date': html.getWorldWideDate(), 'type': type, 'date_type': date_type }, success: function (response_data, text, xhrobject) { var response = ajaxresponse.check(response_data); if (response.status == 'success') { wunderlist.loadContent(response.data, null, fade_in_xhr); wunderlist.filter_cache[type] = response; } } });
}; wunderlist.insertTask = function (nohtml) {
    $.ajax({ url: '/ajax/tasks/insert/', type: 'POST', data: { 'task': JSON.stringify(task) }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success' && (nohtml == undefined || nohtml == false))
            task.show(response.html); task.setDefault();
    }, error: task.setDefault
    });
}; wunderlist.updateTask = function () {
    if (typeof (task.done) != 'undefined') { task.updateDone(); }
    $.ajax({ url: '/ajax/tasks/update/', type: 'POST', data: { 'task': JSON.stringify(task) }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success') {
            if (response.name != undefined && response.name != '')
                task.name = response.name; if (response.note != undefined && response.note != '')
                task.note = response.note; task.updateName(); task.updateImportant(); task.updateDeleted(); task.updateList(); wunderlist.updateBadgeCount();
        }
        task.setDefault();
    }, error: task.setDefault
    });
}; wunderlist.updateTaskOnly = function () {
    $.ajax({ url: '/ajax/tasks/update/', type: 'POST', data: { 'task': JSON.stringify(task) }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success')
            wunderlist.updateBadgeCount(); task.setDefault();
    }, error: task.setDefault
    });
}; wunderlist.updateTasksPositions = function (tasks) { $.ajax({ url: '/ajax/tasks/positions/', type: 'POST', data: { 'tasks': JSON.stringify(tasks) }, success: function (response_data) { ajaxresponse.check(response_data); } }); }; wunderlist.updateBadgeCount = function () {
    $.ajax({ url: '/ajax/tasks/badgecounts/', type: 'POST', data: { 'date': html.getWorldWideDate() }, success: function (response_data, text, xhrobject) {
        var response = ajaxresponse.check(response_data); if (response.status == 'success')
            filters.updateBadges(response.today, response.overdue);
    }
    });
};

/**** settings.js ****/

var settings = settings || {}
settings.update = function () { wunderlist.updateSettings(); }; settings.setDefault = function () { settings.language = undefined; settings.background = undefined; settings.dateformat = undefined; settings.weekstartday = undefined; settings.deleteprompt = undefined; settings.sidebar = undefined; }; settings.updateLanguage = function () {
    if (settings.language != undefined)
    { settings.data.language = settings.language; window.location.reload(); }
}; settings.updateBackground = function () {
    if (settings.background != undefined)
        settings.data.background = settings.background;
}; settings.updateDateformat = function () {
    if (settings.dateformat != undefined)
    { settings.data.dateformat = settings.dateformat; html.make_timestamp_to_string(); }
}; settings.updateWeekstartday = function () {
    if (settings.weekstartday != undefined)
    { settings.data.weekstartday = settings.weekstartday; $('div.add input.datepicker').datepicker('destroy'); html.createDatepicker(); }
}; settings.updateDeleteprompt = function () {
    if (settings.deleteprompt != undefined)
        settings.data.deleteprompt = settings.deleteprompt;
}; settings.updateSidebarPosition = function () {
    if (settings.sidebar != undefined)
        settings.data.sidebar = settings.sidebar;
}; settings.setShortcutkey = function () { settings.data.shortcutkey = settings.data.platform == 'Mac OS X' ? 'Cmd' : 'Strg'; }; settings.getBackground = function () {
    if (background.list[settings.data.background] == undefined)
        return 'bgone'; else
        return html.str_replace('"', '', settings.data.background.toString());
}; settings.getDateformat = function () {
    if (settings.data.dateformat == undefined)
        return 'us'; else
        return html.str_replace('"', '', settings.data.dateformat.toString());
}; settings.getWeekstartday = function () {
    if (settings.data.weekstartday == undefined)
        return '1'; else
        return html.str_replace('"', '', settings.data.weekstartday.toString());
}; settings.getDeleteprompt = function () {
    if (settings.data.deleteprompt == undefined)
        return 1; else
        return parseInt(html.str_replace('"', '', settings.data.deleteprompt.toString()));
}; settings.getSidebar = function () {
    if (settings.data.sidebar == undefined)
        return 'right'; else
        return html.str_replace('"', '', settings.data.sidebar.toString());
}; settings.getShortcutkey = function () {
    if (settings.data.shortcutkey == undefined)
        return 'Strg'; else
        return html.str_replace('"', '', settings.data.shortcutkey.toString());
}; settings.setDefault(); settings.data = {};

/**** account.js ****/

var account = account || {}; var logging_in = false; $(function () { account.init(); }); account.init = function () {
    account.loginUrl = 'ajax/user'; account.registerUrl = 'ajax/user/register'; account.logoutUrl = 'ajax/user/logout'; account.forgotPasswordUrl = 'ajax/user/password'; account.editAccountUrl = 'ajax/user/edit'; account.deleteAccountUrl = 'ajax/user/delete'; account.inviteUrl = 'ajax/invite'; account.status_codes = { 'REGISTER_SUCCESS': 100, 'REGISTER_DUPLICATE': 101, 'REGISTER_INVALID_EMAIL': 102, 'REGISTER_FAILURE': 103, 'LOGIN_SUCCESS': 200, 'LOGIN_FAILURE': 201, 'LOGIN_DENIED': 202, 'LOGIN_NOT_EXIST': 203, 'PASSWORD_SUCCESS': 400, 'PASSWORD_INVALID_EMAIL': 401, 'PASSWORD_FAILURE': 402, 'INVITE_SUCCESS': 500, 'INVITE_INVALID_EMAIL': 501, 'INVITE_FAILURE': 502, 'EDIT_PROFILE_SUCCESS': 600, 'EDIT_PROFILE_AUTHENTICATION_FAILED': 601, 'EDIT_PROFILE_EMAIL_ALREADY_EXISTS': 602, 'EDIT_PROFILE_INVALID_EMAIL_ADDRESS': 603, 'EDIT_PROFILE_FAILURE': 604, 'DELETE_ACCOUNT_SUCCESS': 700, 'DELETE_ACCOUNT_FAILURE': 701, 'DELETE_ACCOUNT_INVALID_EMAIL': 702, 'DELETE_ACCOUNT_NOT_EXISTS': 703, 'DELETE_ACCOUNT_DENIED': 704 }; $('#loginsubmit').live('click', function () {
        $("#login").submit(); if ($(".showregisterdialog").is(':visible')) { account.register(); } else { account.login(); }
        return true;
    }); $('#forgot-pwd').live('click', account.forgotpw); $('input#login-email, input#login-password').live('keyup', function (evt) { var email = $('input#login-email').val(); var password = $('input#login-password').val(); if (evt.keyCode == 13 && email != '' && email.toLowerCase() != 'email' && password != '' && password.toLowerCase() != 'password') { $("#login").submit(); account.login(); } });
}; account.validate = function (email, password) {
    var valid = true; if (email == '' || email.toLowerCase() == 'email')
    { valid = false; account.showEmailError(language.data.email_not_empty); }
    else if (account.validateEmail(email) == false)
    { valid = false; account.showEmailError(language.data.error_invalid_email); }
    if (password == '' || password.toLowerCase() == 'password')
    { valid = false; account.showPasswordError(language.data.password_not_empty); }
    return valid;
}; account.login = function () {
    $('div.errorwrap p').text(''); $('input.input-red').removeClass('input-red'); var data = { 'email': $.trim($('input#login-email').val().toLowerCase()), 'password': $.trim($('input#login-password').val()) }; var newsletter = $('input#login-newsletter').attr('checked'); if (newsletter == true)
        data['newsletter'] = 1; if (account.validate(data['email'], data['password']) == true) {
        data['password'] = $.md5(data['password']); account.startLoginAnimation(); $.ajax({ url: account.loginUrl, type: 'POST', data: data, success: function (response_data, text, xhrobject) {
            if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); switch (response.code) {
                    case account.status_codes.LOGIN_SUCCESS: window.location = '/home'; break; case account.status_codes.LOGIN_FAILURE: account.stopLoginAnimation(); account.showError(language.data.error_login_failed); break; case account.status_codes.LOGIN_DENIED: account.stopLoginAnimation(); account.showPasswordError(language.data.error_login_failed); break; case account.status_codes.LOGIN_NOT_EXIST: var buttonOptions = {}; buttonOptions[language.data.list_delete_no] = function () { $(this).dialog('close') }; buttonOptions[language.data.register_create_user] = function () {
                        if (logging_in == false)
                        { logging_in = true; $("#showregistersubmit").click(); $(this).dialog('close'); $('div.ui-widget-overlay').removeClass('ui-widget-overlay-wood'); setTimeout(function () { logging_in = false }, 5000); }
                    }; create_user_dialog = $('<div></div>').dialog({ autoOpen: false, draggable: false, dialogClass: 'dialog-delete-list', title: language.data.register_question, buttons: buttonOptions }); account.stopLoginAnimation(); $(create_user_dialog).dialog('open'); break; default: account.stopLoginAnimation(); account.showError(language.data.error_occurred); break;
                }
            }
        }, error: function (xhrobject) { account.showError(language.data.error_occurred); account.stopLoginAnimation(); }
        });
    }
}; account.logout = function () {
    if (typeof (FB) != 'undefined') { var facebook_session = FB.getSession(); }
    if (facebook_session !== null && facebook_session != undefined) { FB.logout(function (response) { $.ajax({ url: account.logoutUrl, success: function (response_data) { ajaxresponse.check(response_data); } }); }); } else { $.ajax({ url: account.logoutUrl, success: function (response_data) { ajaxresponse.check(response_data); } }); }
}; account.register = function () {
    $('div.errorwrap p').text(''); $('input.input-red').removeClass('input-red'); var data = { 'email': $.trim($('input#register-email').val().toLowerCase()), 'password': $.trim($('input#register-password').val()) }; var newsletter = $('input#register-newsletter').attr('checked'); if (newsletter == true)
        data['newsletter'] = 1; if (account.validate(data['email'], data['password']) == true) {
        data['password'] = $.md5(data['password']); account.startLoginAnimation(); $.ajax({ url: account.registerUrl, type: 'POST', data: data, success: function (response_data, text, xhrobject) {
            if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); switch (response.code)
                { case account.status_codes.REGISTER_SUCCESS: window.location = '/home'; break; case account.status_codes.REGISTER_DUPLICATE: account.stopLoginAnimation(); account.showEmailError(language.data.error_duplicated_email); break; case account.status_codes.REGISTER_INVALID_EMAIL: account.stopLoginAnimation(); account.showEmailError(language.data.invalid_email); break; case account.status_codes.REGISTER_FAILURE: account.stopLoginAnimation(); account.showError(language.data.registration_failed); break; default: account.stopLoginAnimation(); account.showError(language.error_occurred); break; }
            }
        }, error: function () { account.stopLoginAnimation(); account.showError(language.data.register_error); }
        });
    }
}; account.forgotpw = function () {
    var data = { 'email': $('input#forgotpw-email').val().toLowerCase() }; if (account.validateEmail(data['email'])) {
        $.ajax({ url: account.forgotPasswordUrl, type: 'POST', data: data, success: function (response_data, text, xhrobject) {
            if (xhrobject.status == 0)
                dialogs.showErrorDialog(language.data.no_internet); else if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); switch (response.code)
                { case account.status_codes.PASSWORD_SUCCESS: account.showForgotPasswordError(language.data.password_success); break; case account.status_codes.PASSWORD_INVALID_EMAIL: account.showForgotPasswordError(language.data.invalid_email); break; case account.status_codes.PASSWORD_FAILURE: account.showForgotPasswordError(language.data.password_failed); break; default: dialogs.showErrorDialog(language.data.error_occurred); break; }
            }
        }, error: function () { dialogs.showErrorDialog(language.data.error_occurred); }
        });
    }
    else
        account.showForgotPasswordError(language.data.invalid_email);
}; account.validateEmail = function (email) {
    var reg = /^([A-Za-z0-9\+_\-\.])+\@([A-Za-z0-9_\-\.])+\.([A-Za-z]{2,4})$/; if (reg.test(email) == false) { account.showError(language.error_invalid_email); return false; }
    else { return true; }
}; account.startLoginAnimation = function () { $('#account-buttons input, .fb_button').hide(); $('#account-buttons #account-loader').show(); $('.error').text(''); }; account.stopLoginAnimation = function () { $('#account-buttons input, .fb_button').fadeIn('slow'); $('#account-buttons #account-loader').fadeOut(); }; account.showError = function (msg) { $('input.input-red').removeClass('input-red'); $('p.error-email').text(''); $('p.error-password').hide().text('').fadeIn("fast").text(msg); }; account.showEmailError = function (msg) {
    var pClass = ''; if ($(".showlogindialog").is(':visible'))
    { pClass = '.showlogindialog '; $('input#login-email').addClass('input-red'); }
    else
    { pClass = '.showregisterdialog '; $('input#register-email').addClass('input-red'); }
    $(pClass + 'p.error-email').hide().text('').fadeIn("fast").text(msg);
}; account.showPasswordError = function (msg) {
    var pClass = ''; if ($(".showlogindialog").is(':visible'))
    { pClass = '.showlogindialog '; $('input#login-password').addClass('input-red'); }
    else
    { pClass = '.showregisterdialog '; $('input#register-password').addClass('input-red'); }
    $(pClass + 'p.error-password').hide().text('').fadeIn("fast").text(msg);
}; account.showForgotPasswordError = function (msg) { $('.forgotpwbuttons div.errorwrap').fadeIn(); $('p.error-forgotpw').hide().text('').fadeIn("fast").text(msg); }; account.hideForgotPasswordError = function () { $('.forgotpwbuttons div.errorwrap').hide(); }; account.inviteDialog = undefined; account.inviteCloseDialog = undefined; account.showInviteDialog = function () {
    var inviteEventListener = 0; if (account.inviteDialog == undefined)
        account.inviteDialog = dialogs.generateDialog(language.data.invite, html.generateSocialDialogHTML(), 'dialog-social'); dialogs.openDialog(account.inviteDialog); $('div#invitebox input#send_invitation').live('click', function () {
            input = $('div#invitebox input#email'); if (input.val() != language.data.invite_email && account.validateEmail(input.val()))
                account.invite(); else
                input.select();
        }); $('div#invitebox input#email').live('keyup', function (evt) {
            if (evt.keyCode == 13) {
                if (inviteEventListener == 0) {
                    if ($(this).val() != language.data.invite_email && account.validateEmail($(this).val()))
                        account.invite(); else
                        $(this).select();
                }
                inviteEventListener++; setTimeout(function () { inviteEventListener = 0 }, 100);
            }
        }); $('div#invitebox input#email').live('blur', function () {
            if ($(this).val() == '')
                $(this).val(language.data.invite_email);
        }); $('div#invitebox input#email').live('focus', function () {
            if ($(this).val() == language.data.invite_email)
                $(this).val('');
        });
}; account.invite = function () {
    var data = {}; var input = $('div#invitebox input#email'); var text = $('textarea#invite-text'); data['invite_email'] = input.val().toLowerCase(); data['invite_text'] = text.val(); if (html.validateEmail(data['invite_email'])) {
        $.ajax({ url: 'ajax/invite/', type: 'POST', data: data, success: function (response_data, text, xhrobject) {
            if (xhrobject.status == 0)
                account.showInviteOKDialog(language.data.no_internet); else if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); switch (response.code) {
                    case account.status_codes.INVITE_SUCCESS: if (account.inviteCloseDialog == undefined)
                        { var buttonOptions = {}; buttonOptions['OK'] = function () { $(this).dialog('close'); input.val(language.data.invite_email); dialogs.closeDialog(account.inviteDialog); }; buttonOptions[language.data.invite_more] = function () { $(this).dialog('close'); input.select(); }; account.inviteCloseDialog = $('<div></div>').dialog({ autoOpen: false, draggable: false, modal: false, title: language.data.invitation_success, buttons: buttonOptions }); }
                        dialogs.openDialog(account.inviteCloseDialog); break; case account.status_codes.INVITE_INVALID_EMAIL: account.showInviteOKDialog(language.data.invitation_invalid_email); break; case account.status_codes.INVITE_FAILURE: account.showInviteOKDialog(language.data.invitation_error); break; default: account.showInviteOKDialog(language.data.error_occurred); break;
                }
            }
        }, error: function () { account.showInviteOKDialog(language.data.invite_email); }
        });
    }
}; account.showInviteOKDialog = function (title) {
    if (account.inviteOKDialog == undefined)
    { account.inviteOKDialog = $('<div></div>').dialog({ autoOpen: false, draggable: false, modal: false, title: title, buttons: { 'OK': function () { $(this).dialog('close'); $(this).dialog('destroy'); delete account.inviteOKDialog; input.val(language.data.invite_email); dialogs.closeDialog(account.inviteDialog); } } }); }
    dialogs.openDialog(account.inviteOKDialog);
}; account.editProfile = function () {
    edit_profile_dialog = $('<div></div>').html(html.generateEditProfileDialogHTML()).dialog({ autoOpen: false, draggable: false, resizable: false, modal: true, dialogClass: 'dialog-edit-profile', title: language.data.edit_profile_title, open: function ()
    { $('#new_email').val(''); $('#new_password').val(''); $('#old_password').val(''); $('.error').hide().fadeIn("fast").text(''); $('#new_email').blur(); }
    }); dialogs.openDialog(edit_profile_dialog); $('#cancel_edit_profile').die(); $('#submit_edit_profile').die(); $('#new_email,#new_password,#old_password').die(); $('#new_email,#new_password,#old_password').live('keyup', function (evt) {
        if (evt.keyCode == 13)
            account.change_profile_data(); else if (evt.keyCode == 27)
            $(edit_profile_dialog).dialog('close');
    }); $('#cancel_edit_profile').live('click', function () { $(edit_profile_dialog).dialog('close'); }); $('#submit_edit_profile').live('click', function () { account.change_profile_data(); return false; });
}; account.change_profile_data = function () {
    var data = {}; new_email_address = $('input#new_email').val().toLowerCase(); if (new_email_address != '') {
        if (account.validateEmail(new_email_address))
            data['new_email'] = new_email_address; else
        { dialogs.showErrorDialog(language.data.invalid_email); return false; }
    }
    new_password = $('input#new_password').val(); if (new_password != language.data.new_password && new_password != '')
    { data['new_password'] = $.md5(new_password); }
    if (data['new_email'] == undefined && data['new_password'] == undefined)
        return false; if ($('#old_password').val() == '')
    { dialogs.showErrorDialog(language.data.wrong_password); return false; }
    else
        data['password'] = $.md5($('#old_password').val()); if (data['new_email'] != undefined || data['new_password'] != undefined) {
        $.ajax({ url: this.editAccountUrl, type: 'POST', data: data, success: function (response_data, text, xhrobject) {
            if (xhrobject.status == 0)
                dialogs.showErrorDialog(language.data.no_internet); else if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); switch (response.code)
                { case account.status_codes.EDIT_PROFILE_SUCCESS: dialogs.closeDialog(edit_profile_dialog); dialogs.showOKDialog(language.data.changed_account_data, function () { window.location.reload() }); break; case account.status_codes.EDIT_PROFILE_AUTHENTICATION_FAILED: dialogs.showErrorDialog(language.data.authentication_failed); break; case account.status_codes.EDIT_PROFILE_EMAIL_ALREADY_EXISTS: dialogs.showErrorDialog(language.data.email_already_exists); break; case account.status_codes.EDIT_PROFILE_INVALID_EMAIL_ADDRESS: dialogs.showErrorDialog(language.data.error_invalid_email); break; default: dialogs.showErrorDialog(language.data.error_occurred); break; }
            }
        }, error: function () { dialogs.showErrorDialog(language.data.error_occurred); }
        });
    }
}; account.deleteAccount = function () {
    var html_code = '<p>' + language.data.delete_account_desc + '</p>' + '<input class="input-normal"          type="password" id="delete_password" name="delete_password" placeholder="' + language.data.password + '" />' + '<div class="ui-dialog-buttonset"><input class="input-button"          type="submit"   id="cancel_delete_profile" value="' + language.data.cancel + '" />' + '<input class="input-button input-bold" type="submit"   id="submit_delete_profile" value="' + language.data.delete_account + '" /></div>' + '<span class="error"></span>'; delete_account_dialog = $('<div></div>').html(html_code).dialog({ autoOpen: false, draggable: false, resizable: false, modal: true, dialogClass: 'dialog-edit-profile', title: language.data.delete_account_title, open: function ()
    { $('#delete_email').val(''); $('#delete_password').val(''); $('#delete_email').blur(); }
    }); $(delete_account_dialog).dialog('open'); $('#delete_email,#delete_password').live('keyup', function (evt) {
        if (evt.keyCode == 13)
        { account.delete_account_data(); }
        else if (evt.keyCode == 27)
            $(delete_account_dialog).dialog('close');
    }); $('#cancel_delete_profile').live('click', function () { $(delete_account_dialog).dialog('close'); }); $('#submit_delete_profile').live('click', function () { account.delete_account_data(); return false; }); $(delete_account_dialog).dialog('open');
}; account.delete_account_data = function () {
    $.ajax({ url: this.deleteAccountUrl, type: 'POST', data: { 'password': $.md5($('input#delete_password').val()) }, success: function (response_data, text, xhrobject) {
        if (xhrobject.status == 0)
            showErrorDialog(language.data.no_internet); else if (xhrobject.status == 200) {
            var response = ajaxresponse.check(response_data); switch (response.code)
            { case account.status_codes.DELETE_ACCOUNT_SUCCESS: $(delete_account_dialog).dialog('close'); dialogs.showConfirmationDialog(function () { account.logout(); }); break; case account.status_codes.DELETE_ACCOUNT_NOT_EXISTS: dialogs.showErrorDialog(language.data.sync_not_exist); break; case account.status_codes.DELETE_ACCOUNT_INVALID_EMAIL: dialogs.showErrorDialog(language.data.error_invalid_email); break; case account.status_codes.DELETE_ACCOUNT_FAILURE: dialogs.showErrorDialog(language.data.delete_account_failure); break; case account.status_codes.DELETE_ACCOUNT_DENIED: dialogs.showErrorDialog(language.data.delete_account_denied); break; default: dialogs.showErrorDialog(language.data.error_occurred); break; }
        }
    }, error: function () { dialogs.showErrorDialog(language.data.error_occurred); }
    });
};

/**** sharing.js ****/

var sharing = sharing || {}; $(function () { sharing.init(); }); sharing.init = function () {
    sharing.shareUrl = 'ajax/share'; sharing.sharedEmailsUrl = 'ajax/share/emails'; sharing.deleteSharedEmailUrl = 'ajax/share/delete'; sharing.deleteALLSharedEmailsUrl = 'ajax/share/remove'; sharing.getOwnerUrl = 'ajax/share/owner'; sharing.shareListDialog = null; sharing.deletedMails = []; sharing.openedNoInternetDialog = false; sharing.status_codes = { 'SHARE_SUCCESS': 800, 'SHARE_FAILURE': 801, 'SHARE_DENIED': 802, 'SHARE_NOT_EXISTS': 803, 'SHARE_NOT_SHARED': 804, 'SHARE_OWN_EMAIL': 805 }; sharing.addedEmail = false; $('.input-sharelist').live('keydown', function (event) {
        if (event.keyCode == 13) {
            if (sharing.addedEmail == false)
            { sharing.addedEmail = true; $('#send_share_invitation').click(); setTimeout(function () { sharing.addedEmail = false }, 1000); }
        }
    }); sharing.clickedSharingButton = false; $('a.list .sharedlist, a.list .sharelist').live('click', function () {
        if (sharing.clickedSharingButton == false) {
            sharing.clickedSharingButton = true; $('#share-list-email').val(''); var shareList = $('.sharelistusers'); shareList.empty(); $('.invitedpeople').remove(); sharing.deletedMails = []; list_id = $(this).parent('b').parent('a').attr('id').replace('list', ''); list_name = $(this).parent('b').text(); if ($(this).hasClass('sharedlist'))
                sharing.getSharedEmails(list_id); else
                sharing.openShareListDialog(list_id, list_name); $('#share-list-email').blur(); setTimeout(function () { sharing.clickedSharingButton = false; }, 300);
        }
    }); sharing.deletedEmail = false; $('.dialog-sharelist li span').live('click', function () {
        if (sharing.deletedEmail == false) {
            sharing.deletedEmail = true; var deleteButton = $(this); var shareListItems = $('.sharelistusers').children('li'); if (sharing.deleteSharedEmailDialog == undefined) {
                var buttons = {}; buttons[language.data.no] = function () { $(this).dialog('close'); }; buttons[language.data.yes] = function () {
                    var list_id = $('input#share-list-id').attr('rel').replace('list', ''); var email = $.trim(deleteButton.parent().text()); sharing.deletedMails.push(email); sharing.deleteSharedEmail(list_id, deleteButton.parent()); sharing.deletedMails = []; if ($('.sharelistusers').children('li').length == 0)
                        $('.invitedpeople').remove(); $(this).dialog('close');
                }; sharing.deleteSharedEmailDialog = $('<div></div>').dialog({ autoOpen: true, draggable: false, modal: false, title: language.data.delete_shared_email, buttons: buttons });
            }
            else
                dialogs.openDialog(sharing.deleteSharedEmailDialog); if (shareListItems.length == 0)
                $('p.invitedpeople').remove(); setTimeout(function () { sharing.deletedEmail = false; }, 300)
        }
    }); sharing.sendInvitation = false; $('#send_share_invitation').live('click', function () {
        if (sharing.sendInvitation == false)
        { sharing.sendInvitation = true; sharing.shareLists(); dialogs.closeDialog(sharing.shareListDialog); setTimeout(function () { sharing.sendInvitation = false }, 2000); }
    }); $('.delete-all-shared-lists').live('click', function () { sharing.deleteAllSharedEmails($('input#share-list-id').attr('rel').replace('list', '')); });
}; sharing.shareLists = function () {
    if ($('#share-list-email').val() == '')
        dialogs.showErrorDialog(language.data.invalid_email); else
    { list.id = $('input#share-list-id').attr('rel').replace('list', ''); list.shared = 1; list.update(); }
}; sharing.deleteSharedEmail = function (list_id, deletedElement) {
    $.ajax({ url: sharing.deleteSharedEmailUrl, type: 'POST', data: { 'list_id': list_id, 'delete': sharing.deletedMails[0] }, success: function (response_data, text, xhrobject) {
        if (response_data != '' && text != '' && xhrobject != undefined) {
            if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); switch (response.code) {
                    case sharing.status_codes.SHARE_SUCCESS: var ulElement = deletedElement.parent(); deletedElement.remove(); dialogs.showDeletedDialog(language.data.shared_delete_success); if (ulElement.children('li').length == 0)
                        { $('p.invitedpeople').remove(); $('ul.sharelistusers').children('li').remove(); list.id = list_id; list.shared = 0; list.update(); }
                        break; case sharing.status_codes.SHARE_FAILURE: dialogs.showErrorDialog(language.data.share_failure); break; case sharing.status_codes.SHARE_DENIED: sharing.unshareList(list_id); dialogs.showErrorDialog(language.data.share_denied); break; case sharing.status_codes.SHARE_NOT_EXIST: sharing.unshareList(list_id); dialogs.showErrorDialog(language.data.sync_not_exist); break; default: dialogs.showErrorDialog(language.data.error_occurred); break;
                }
            }
        }
    }, error: function () { dialogs.showErrorDialog(language.data.share_failure); }
    });
}; sharing.deleteAllSharedEmails = function (list_id) {
    $.ajax({ url: sharing.deleteALLSharedEmailsUrl, type: 'POST', data: { 'list_id': list_id }, success: function (response_data, text, xhrobject) {
        if (response_data != '' && text != '' && xhrobject != undefined) {
            if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); switch (response.code)
                { case sharing.status_codes.SHARE_SUCCESS: sharing.shareListDialog.dialog('close'); dialogs.showDeletedDialog(language.data.shared_delete_all_success); list.id = list_id; list.shared = 0; list.update(); break; case sharing.status_codes.SHARE_FAILURE: dialogs.showErrorDialog(language.data.share_failure); break; default: dialogs.showErrorDialog(language.data.error_occurred); break; }
            }
        }
    }, error: function () { dialogs.showErrorDialog(language.data.share_failure); }
    });
}; sharing.sendSharedList = function () {
    var collected_emails = []; var emails = $('input#share-list-email').val().split(','); var list_id = $('input#share-list-id').attr('rel').replace('list', ''); if (emails.length > 0 && emails[0] != '') {
        for (value in emails) {
            if (html.validateEmail(emails[value]))
                collected_emails.push(emails[value]);
        }
    }
    else {
        if (sharing.deletedMails.length == 0)
        { dialogs.showErrorDialog(language.data.shared_not_changed); sharing.unshareList(list_id); return false; }
    }
    if (collected_emails.length >= 1) {
        $.ajax({ url: sharing.shareUrl, type: 'POST', data: { 'list_id': list_id, 'add': collected_emails }, success: function (response_data, text, xhrobject) {
            if (response_data != '' && text != '' && xhrobject != undefined) {
                if (xhrobject.status == 200) {
                    var response = ajaxresponse.check(response_data); switch (response.code)
                    { case sharing.status_codes.SHARE_SUCCESS: $('a#list' + list_id + ' b').find("div").removeClass("sharelist").addClass('sharedlist'); dialogs.showSharedSuccessDialog(language.data.shared_successfully); break; case sharing.status_codes.SHARE_FAILURE: dialogs.showErrorDialog(language.data.share_failure); break; case sharing.status_codes.SHARE_DENIED: sharing.unshareList(list_id); dialogs.showErrorDialog(language.data.share_denied); break; case sharing.status_codes.SHARE_NOT_EXIST: sharing.unshareList(list_id); dialogs.showErrorDialog(language.data.sync_not_exist); break; default: dialogs.showErrorDialog(language.data.error_occurred); break; }
                }
            }
        }, error: function () { dialogs.showErrorDialog(language.data.sync_error); }
        });
    }
}; sharing.unshareList = function (list_id) {
    if ($('.sharelistusers').children('li').length == 0)
    { $('a#list' + list_id + ' b').find("div").removeClass('sharedlist').addClass("sharelist"); $('p.invitedpeople').remove(); list.id = list_id; list.shared = 0; list.update(); }
}; sharing.getSharedEmails = function (list_id) {
    $.ajax({ url: sharing.sharedEmailsUrl, type: 'POST', data: { 'list_id': list_id }, success: function (response_data, text, xhrobject) {
        if (response_data != '' && text != '' && xhrobject != undefined) {
            if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); var sharedListNameElement = $('a#list' + list_id).children('b'); switch (response.code) {
                    case sharing.status_codes.SHARE_SUCCESS: sharing.openShareListDialog(list_id, sharedListNameElement.text()); var shareList = $('ul.sharelistusers'); var shareListItems = shareList.children('li'); if (response.emails != undefined && response.emails.length > 0) {
                            if (shareListItems.length == 0)
                            { shareHTML = "<p class='invitedpeople'>"; shareHTML += "<b>" + language.data.currently_shared_with + ":</b>"; shareHTML += "<button class='input-button delete-all-shared-lists'>" + language.data.delete_all_shared_lists + "</button>"; shareHTML += "</p>"; shareList.before(shareHTML); }
                            for (value in response.emails)
                                shareList.append('<li><span></span> ' + $.trim(response.emails[value]) + '</li>');
                        }
                        else if (response.emails.length == 0 && sharedListNameElement.find("div").hasClass('sharedlist'))
                        { list.id = list_id; list.shared = 0; list.update(); }
                        break; case sharing.status_codes.SHARE_FAILURE: dialogs.showErrorDialog(language.data.share_failure); break; case sharing.status_codes.SHARE_DENIED: sharing.getOwnerOfList(list_id); break; case sharing.status_codes.SHARE_NOT_EXIST: dialogs.showErrorDialog(language.data.sync_not_exist); break; case sharing.status_codes.SHARE_NOT_SHARED: if (sharedListNameElement.find("div").hasClass('sharedlist'))
                        { list.id = list_id; list.shared = 0; list.update(); }
                        sharing.openShareListDialog(list_id); break; default: dialogs.showErrorDialog(language.data.error_occurred); break;
                }
            }
        }
    }, error: function () { dialogs.showErrorDialog(language.data.sync_error); }
    });
}; sharing.getOwnerOfList = function (list_id) {
    $.ajax({ url: sharing.getOwnerUrl, type: 'POST', data: { 'list_id': list_id }, success: function (response_data, text, xhrobject) {
        if (response_data != '' && text != '' && xhrobject != undefined) {
            if (xhrobject.status == 200) {
                var response = ajaxresponse.check(response_data); switch (response.code) {
                    case sharing.status_codes.SHARE_SUCCESS: if (response.list_id == list_id)
                            dialogs.showErrorDialog(language.data.share_denied + '<br /><b>' + response.owner + '</b>'); else
                            dialogs.showErrorDialog(language.data.share_denied); break; case sharing.status_codes.SHARE_FAILURE: dialogs.showErrorDialog(language.data.share_failure); break; case sharing.status_codes.SHARE_NOT_EXIST: dialogs.showErrorDialog(language.data.sync_not_exist); break; default: dialogs.showErrorDialog(language.data.error_occurred); break;
                }
            }
        }
    }, error: function () { dialogs.showErrorDialog(language.data.sync_error); }
    });
}; sharing.openShareListDialog = function (list_id, list_name) { $(".dialog-sharelist").children().remove(); $(".dialog-sharelist").remove(); sharing.shareListDialog = dialogs.generateDialog(language.data.sharing_is_caring + list_name, html.generateShareListDialogHTML(list_id), 'dialog-sharelist'); dialogs.openDialog(sharing.shareListDialog); };

/**** ajaxresponse.js ****/

var ajaxresponse = ajaxresponse || {}; ajaxresponse.check = function (response) {
    if (response != 400) {
        response = $.parseJSON(response); if (response.status != undefined && response.status == 'logout')
            window.location = '/account'; else
            return response;
    }
    else
        account.logout();
};

/**** helpers/task.js ****/

var task = task || {}; task.insert = function (nohtml) { wunderlist.insertTask(nohtml); }
task.update = function () { wunderlist.updateTask(); }; task.updateOnly = function () { wunderlist.updateTaskOnly(); }; task.setDefault = function () { task.id = undefined; task.list_id = undefined; task.name = undefined; task.note = undefined; task.date = undefined; task.important = undefined; task.position = undefined; task.deleted = undefined; task.done = undefined; task.done_date = undefined; }
task.updateName = function () {
    if (task.name != undefined && task.name != '' && task.id != undefined && task.id > 0)
        $("li#" + task.id).children('span.description').html(html.replace_http_link(unescape(task.name)));
}; task.updateNote = function () {
    if (task.name != undefined && task.note != '' && task.id != undefined && task.id > 0)
        $('li#' + task.id).children('span.note').html(unescape(task.note));
}; task.updateImportant = function () {
    if (task.important != undefined && task.id != undefined && task.id > 0) {
        var taskElement = $('li#' + task.id); var ulElement = taskElement.parent('ul'); if (task.important == 0) {
            var item = ulElement.find('span.fav:last').parent(); taskElement.children('span.fav').addClass('favina').removeClass('fav'); if (taskElement.attr('id') != item.attr('id') && ulElement.children('li').length > 1)
            { taskElement.slideUp('fast', function () { taskElement.insertAfter(item).slideDown(400, task.updatePositions); }); }
        }
        if (task.important == 1) {
            var item = ulElement.find('li:first'); taskElement.children('span.favina').removeClass('favina').addClass('fav'); if (ulElement.children('li').length > 1 && taskElement.attr('id') != item.attr('id'))
            { taskElement.slideUp('fast', function () { taskElement.prependTo(ulElement).slideDown(400, task.updatePositions); }); }
        }
    }
}; task.updateDeleted = function () {
    if (task.deleted != undefined && task.deleted == 1 && task.id != undefined && task.id > 0 && task.list_id != undefined && task.list_id > 0) {
        var removeList = false; var liElement = $('li#' + task.id); var ulElement = liElement.parent('ul'); if (ulElement.hasClass('filterlist')) {
            if (ulElement.children('li').length == 1 || ulElement.children('li').length == 0)
                removeList = true;
        }
        else {
            if (ulElement.children('li').length == 1 && liElement.find('.checked').length == 1)
                removeList = true;
        }
        if (removeList == true) {
            var hElement = ulElement.prev(); if (hElement.is('h3'))
                hElement.remove(); ulElement.remove();
        }
        liElement.remove(); wunderlist.updateListCount(task.list_id);
    }
}; task.updatePositions = function () {
    var tasks = $('div#content ul.mainlist li'); var newTasks = []; for (var ix = 0; ix < tasks.length; ix++)
    { var newTask = { 'id': tasks.eq(ix).attr('id'), 'position': ix + 1 }; newTasks.push(newTask); }
    if (newTasks.length > 0)
        wunderlist.updateTasksPositions(newTasks);
}; task.updateDone = function () {
    if (task.id != undefined && task.id > 0 && task.done != undefined) {
        var liElement = $('li#' + task.id); wunderlist.updateListCount(task.list_id); if (task.done == 1) {
            liElement.addClass('done'); if ($('ul.searchlist').length == 0 && $('ul.filterlist').length == 0) {
                if ($("ul#donelist_list_today").length == 0) { $("ul.mainlist").after("<h3 class='head_today'>" + language.data.done_today + "</h3>"); $("div#content h3.head_today").after("<ul id='donelist_list_today' class='donelist'></ul>") }
                liElement.slideUp('fast', function () { liElement.prependTo('ul#donelist_list_today').slideDown(); });
            }
            else if ($('ul.searchlist').length > 0) {
                var lastLiElement = liElement.parent('ul.searchlist').find('li:last'); if (liElement.attr('id') != lastLiElement.attr('id'))
                { liElement.slideUp('fast', function () { liElement.appendTo('ul.searchlist').slideDown(); }); }
            }
            else {
                var lastLiElement = liElement.parent('ul.filterlist').find('li:last'); if (liElement.attr('id') != lastLiElement.attr('id'))
                { liElement.slideUp('fast', function () { liElement.appendTo('ul#filterlist' + liElement.attr('rel')).slideDown(); }); }
            }
        }
        if (task.done == 0) {
            if ($('a#done').hasClass('active')) {
                if (liElement.parent('ul.filterlist').find('li').length == 1)
                { liElement.parent().prev().remove(); liElement.parent().remove(); }
                liElement.remove(); return;
            }
            if (liElement.parent('ul.donelist').find('li').length == 1)
            { liElement.parent().prev().remove(); liElement.parent().remove(); }
            var ulElement = 'ul.mainlist'; if ($('ul.filterlist').length > 0 || $('ul.searchlist').length > 0)
            { var parentElement = liElement.parent($('ul.filterlist').length > 0 ? 'ul.filterlist' : 'ul.searchlist'); var lastLiElement = parentElement.find('li:last'); var firstDoneLiElement = parentElement.find('li.done:first'); var doneLiElementCount = parentElement.find('li.done').length; ulElement = $('ul.filterlist').length > 0 ? 'ul#filterlist' + liElement.attr('rel') : 'ul.searchlist'; }
            if (doneLiElementCount != undefined) {
                if (doneLiElementCount > 1) {
                    if (liElement.attr('id') == lastLiElement.attr('id') || (liElement.attr('id') != lastLiElement.attr('id') && liElement.attr('id') != firstDoneLiElement.attr('id'))) {
                        liElement.slideUp('fast', function () {
                            if (liElement.find('span.fav').length == 1)
                                liElement.prependTo(ulElement).slideDown(); else
                                liElement.insertBefore(firstDoneLiElement).slideDown();
                        });
                    }
                }
            }
            else {
                liElement.slideUp('fast', function () {
                    if (liElement.find('span.fav').length == 1)
                        liElement.prependTo(ulElement).slideDown(); else
                        liElement.appendTo(ulElement).slideDown();
                });
            }
            liElement.removeClass('done'); html.make_timestamp_to_string();
        }
    }
}; task.updateList = function () {
    if (task.id != undefined && task.id > 0 && task.list_id != undefined && task.list_id > 0) {
        var liElement = $('li#' + task.id); var oldListId = liElement.attr('rel'); if (oldListId != task.list_id) {
            if ($('ul.filterlist').length == 0)
                liElement.remove(); else {
                var ulElement = $('ul#filterlist' + oldListId); var liCount = ulElement.children('li').length; if (taskDroped == true) {
                    if ($('ul#filterlist' + task.list_id).is('ul')) {
                        if (liElement.find('span.fav').length == 1)
                            liElement.prependTo('ul#filterlist' + task.list_id).slideDown(); else
                            liElement.appendTo('ul#filterlist' + task.list_id).slideDown();
                    }
                    else
                    { listHTML = '<h3 class="clickable cursor" rel="' + task.list_id + '">' + $('a#list' + task.list_id + ' b').text() + '</h3>'; listHTML += '<ul id="filterlist' + task.list_id + '" rel="' + ulElement.attr('rel') + '" class="mainlist sortable filterlist"></ul>'; $('div#content').append(listHTML); liElement.appendTo('ul#filterlist' + task.list_id).slideDown(); makeSortable(); }
                    taskDroped = false;
                }
                if (liCount == 0 || liCount == 1)
                { ulElement.prev().remove(); ulElement.remove(); }
            }
            liElement.attr('rel', task.list_id);
        }
        wunderlist.updateListCount(oldListId); wunderlist.updateListCount(task.list_id);
    }
}; task.show = function (liElement, count) {
    if (liElement != '' && task.list_id != undefined && task.list_id > 0) {
        if ($("ul.filterlist").length > 0 || $('#left a.active').length == 1) {
            var ulElement = $('ul#filterlist' + task.list_id); if (ulElement != undefined && ulElement.is('ul'))
            { ulElement.append(liElement); }
            else
            { listHTML = '<h3 class="clickable cursor" rel="' + task.list_id + '">' + $('a#list' + task.list_id + ' b').text() + '</h3>'; listHTML += '<ul id="filterlist' + task.list_id + '" rel="' + ulElement.attr('rel') + '" class="mainlist sortable filterlist">' + html.replace_http_link(liElement) + '</ul>'; $('div#content').append(listHTML); }
        }
        else
            $(".mainlist").append(html.replace_http_link(liElement)).find("li:last").hide().fadeIn(225); $(".add .showdate").remove(); makeSortable(); html.createDatepicker(); $('.datepicker').val(''); html.make_timestamp_to_string();
    }
    if (task.list_id != undefined && task.list_id > 0)
    { wunderlist.updateListCount(task.list_id, count); wunderlist.updateBadgeCount(); }
}; task.setDefault();

/**** helpers/list.js ****/

var list = list || {}; list.insert = function () { wunderlist.insertList(); }
list.update = function () { wunderlist.updateList(); }; list.setDefault = function () { list.id = undefined; list.name = undefined; list.position = undefined; list.deleted = undefined; list.shared = undefined; }
list.updateName = function () {
    if (list.name != undefined && list.id != undefined && list.id > 0) {
        var listElement = $('a#list' + list.id); var listElementInput = listElement.children('input'); var listElementShared = parseInt(listElementInput.attr('rel')); var listNameElement = listElement.children('b'); listElementName = unescape(list.name); if (listElement.hasClass('ui-state-disabled') && listElementName != '')
            $('#content h1').text(listElementName); listElementInput.remove(); if (listNameElement.hasClass('inbox') || list.id == account.inbox)
            html_code = '<b class="inbox">' + listElementName + '</b>'; else if (listElementShared == 1)
            html_code = '<b class="sharep">' + listElementName + '<div class="sharedlist"></div></b>'; else
            html_code = '<b class="sharep">' + listElementName + '<div class="sharelist"></div></b>'; listElement.children('.savep').hide(); listElement.children('.deletep').hide(); listElement.find('span').before(html_code); if (listElementName.length > 30)
            listNameElement.attr('title', listElementName);
    }
}
list.updateShared = function () {
    if (list.shared != undefined && list.id != undefined && list.id > 0) {
        var listElement = $('a#list' + list.id); var listNameElement = listElement.children('b'); if (listNameElement.hasClass('inbox') == false) {
            if (list.shared == 1)
            { listNameElement.find("div").removeClass("sharelist").addClass('sharedlist'); sharing.sendSharedList(); }
            else
            { listNameElement.find("div").removeClass('sharedlist').addClass('sharelist'); }
        }
    }
}
list.updatePositions = function () {
    var lists = $('div#sidebar div#lists a'); var newLists = []; for (var ix = 0; ix < lists.length; ix++)
    { var newList = { 'id': lists.eq(ix).attr('id').replace('list', ''), 'position': ix + 1 }; newLists.push(newList); }
    if (newLists.length > 0)
        wunderlist.updateListsPositions(newLists);
}
list.updateDeleted = function () {
    if (list.id != undefined && list.deleted != undefined && list.deleted == 1) {
        if ($('a#list' + list.id).children('b').hasClass('inbox') == false) {
            var selectedListId = $('a.ui-state-disabled').attr('id').replace('list', ''); wunderlist.updateBadgeCount(); if (delete_dialog != undefined)
                $(delete_dialog).dialog('close'); $('a#list' + list.id).remove(); $('a.addlist').show(); if (list.id == selectedListId) {
                $('a.list').each(function () {
                    if ($(this).children('b').hasClass('inbox'))
                        $(this).click();
                });
            }
        }
    }
}
list.show = function (noclick) {
    var listElement = $('a#x'); listElementName = unescape(list.name); listElement.children('input').remove(); listElement.children('.savep').hide(); listElement.children('.deletep').hide(); if (noclick == undefined || noclick == false)
        listElement.find('span').before('<b class="sharep">' + listElementName + '<div class="sharelist"></div></b>'); listElement.attr('id', 'list' + list.id); if (listElementName.length > 30)
        listElement.children('b').attr('title', listElementName); $('a.addlist').fadeIn(); makeListsDropable(); if (noclick == undefined || noclick == false)
        listElement.click();
}
list.setDefault();

/**** helpers/background.js ****/

var background = background || {}; background.settings = { defaultBgColor: '#000', defaultBgRootPath: 'https://duv2rxyjq20ja.cloudfront.net/wlcom1/backgrounds/', defaultBgPosition: 'top center' }; background.list = { 'bgsix': { bgPath: background.settings.defaultBgRootPath + 'darkfade.jpg', bgPosition: background.settings.defaultBgPosition, bgColor: '#242424' }, 'bgseven': { bgPath: background.settings.defaultBgRootPath + 'whitefade.jpg', bgPosition: background.settings.defaultBgPosition, bgColor: '#9c9c9c' }, 'bgfour': { bgPath: background.settings.defaultBgRootPath + 'blue.jpg', bgPosition: background.settings.defaultBgPosition, bgColor: '#2b1023' }, 'bgone': { bgPath: background.settings.defaultBgRootPath + 'wood.jpg', bgPosition: background.settings.defaultBgPosition, bgColor: background.settings.defaultBgColor }, 'bgnine': { bgPath: background.settings.defaultBgRootPath + 'darkwood.jpg', bgPosition: 'center center', bgColor: background.settings.defaultBgColor }, 'bgfive': { bgPath: background.settings.defaultBgRootPath + 'royal_purple.jpg', bgPosition: background.settings.defaultBgPosition, bgColor: background.settings.defaultBgColor }, 'bgeight': { bgPath: background.settings.defaultBgRootPath + 'monster.jpg', bgPosition: 'top right', bgColor: '#81bcb8' }, 'bgtwo': { bgPath: background.settings.defaultBgRootPath + 'wheat.jpg', bgPosition: 'center center', bgColor: background.settings.defaultBgColor }, 'bgthree': { bgPath: background.settings.defaultBgRootPath + 'bokeh.jpg', bgPosition: 'center center', bgColor: background.settings.defaultBgColor }, 'bgten': { bgPath: background.settings.defaultBgRootPath + 'chalkboard.jpg', bgPosition: background.settings.defaultBgPosition, bgColor: '#000' }, 'bgtwelve': { bgPath: background.settings.defaultBgRootPath + 'leaf.jpg', bgPosition: background.settings.defaultBgPosition, bgColor: '#000' }, 'bgeleven': { bgPath: background.settings.defaultBgRootPath + 'forrest.jpg', bgPosition: background.settings.defaultBgPosition, bgColor: '#000'} }; background.init = function () {
    var bgCounter = 1; $('#right').prepend('<a class="backgroundswitcher"><span class="activebackground"></span></a>'); $.each(background.list, function (bgClass) { $('#backgroundList').prepend('<a class="' + bgClass + '">' + (bgCounter++) + '</a>'); $('a.' + bgClass).bind('click', function () { $(".activebackground").removeClass().addClass("activebackground").addClass(bgClass); settings.background = bgClass; settings.update(); $('#bghelp').fadeOut(200, function () { $(this).css('background', background.list[bgClass].bgColor + ' url(' + background.list[bgClass].bgPath + ') ' + background.list[bgClass].bgPosition + ' no-repeat fixed').fadeIn(200) }); $('body').css('background', 'none'); background.setActive(this); }); }); $("#bghelp").hide(); if (settings.getBackground() != undefined)
        $('a.' + settings.getBackground()).click();
}; background.setActive = function (object) { $("#bottombar #right a").removeClass("active"); $(object).addClass("active"); };

/**** helpers/html.js ****/

var html = html || {}; html.generateNotesDialogHTML = function () { var html_code = '<div class="notes_buffer">' + '<textarea></textarea><div class="savednote"><div class="inner"></div></div>' + '</div>' + '<div class="notes_buttons"><input id="save-note" class="input-button" type="submit" value="' + language.data.edit_changes + '" /></div>'; return html_code; }; html.generateShareListDialogHTML = function (list_id) { var html_code = '<div class="shareContent"><p>' + language.data.sharelist_info + '</p>' + '<p class="small"><b>' + language.data.sharelist_hint + '</b>: ' + language.data.sharelist_hint_text + '</p>' + '<input type="hidden" id="share-list-id" rel="' + list_id + '" />' + '<p><input class="input-login input-sharelist" type="text" id="share-list-email" name="email" placeholder="' + language.data.invite_email + ',' + language.data.invite_email + '..." />' + '<p style="height:40px"><input id="send_share_invitation" class="input-button input-bold" type="submit" value="' + language.data.sharelist_button + '" /></p>' + '<ul class="sharelistusers"></ul><br/></div>'; return html_code; }; html.generateListContentHTML = function (list_id, list_name) {
    var html_code = ''; if (list_id != 1 && wunderlist.isUserLoggedIn() == true)
        html_code += "<div id='listfunctions'><a rel='" + language.data.share_this_list + "' class='list-share'></a><a rel='" + language.data.print_tasks + "' class='list-print'></a><a rel='" + language.data.send_by_mail + "' class='list-email'></a><a rel='" + language.data.share_with_cloud + "' class='list-cloud'></a><div id='cloudtip'><span class='triangle'></span><span class='copy'>" + language.data.copy_link + "</span><span class='link'></span></div></div>"; else
        html_code += "<div id='listfunctions'><a rel='" + language.data.print_tasks + "' class='list-print'></a><a rel='" + language.data.send_by_mail + "' class='list-email'></a><a rel='" + language.data.share_with_cloud + "' class='list-cloud'></a><div id='cloudtip'><span class='triangle'></span><span class='copy'>" + language.data.copy_link + "</span><span class='link'></span></div></div>"; html_code += "<h1>" + unescape(list_name) + "</h1>"; html_code += "<div class='add'>"; html_code += "<div class='addwrapper'><input type='text' class='input-add' placeholder='" + language.data.add_task + "' /></div>"; html_code += "<input type='hidden' class='datepicker'/>"; html_code += "</div>"; html_code += "<ul id='list' rel='" + list_id + "' class='mainlist'></ul>"; return html_code;
}; html.generateNewListElementHTML = function (listId, listElementName, listElementInputClass, listElementTaskCount) {
    if (listId == undefined || listId == '')
        listId = 'x'; if (listElementName == undefined || listElementName == '')
        listElementName = language.data.new_list; if (listElementInputClass == undefined || listElementInputClass == '')
        listElementInputClass = 'input-list'; if (listElementTaskCount == undefined || listElementTaskCount == '')
        listElementTaskCount = 0; var html_code = "<a id='" + listId + "' class='list sortablelist'>"; html_code += "<span>" + listElementTaskCount + "</span>"; html_code += "<div class='deletep'></div>"; html_code += "<div class='savep'></div>"; html_code += "<div class='editp'></div>"; if (listElementInputClass == 'none')
        html_code += "<b class='sharep'>" + listElementName + "<div class='sharelist'></div></b>"; else
        html_code += "<input class='" + listElementInputClass + "' maxlength='50' type='text' rel='0' value='" + listElementName + "' />"; html_code += "</a>"; return html_code;
}; html.generateCreditsDialogHTML = function () { var html_code = '<p><b>Wunderlist</b> is an easy-to-use task management tool, that runs on Windows, Mac, iPhone, iPad and the Android. Register for free to sync your todos online. No matter where you are, your Wunderlist follows you.</p>' + '<p><b>What´s next?</b></p>' + '<p>We are currently working on something pretty big. We call it <b>Wunderkit</b>, an online productivity platform that will change the way you look at collaborative work.<br/><br/>' + 'Please enjoy Wunderlist and tell us what you think about it.</p>'; return html_code; }; html.generateBackgroundsDialogHTML = function () { var html_code = '<p><a href="http://downloads.dvq.co.nz" target="_blank">Handcrafted Wood Texture</a> (DVQ)<br/>' + '<a href="http://blog.artcore-illustrations.de" target="_blank">Balloon Monster</a> (Artcore)<br/>' + '<a href="http://www.galaxygui.com/" target="_blank">Dark Wood Texture</a> (Galaxgui)</p>'; return html_code; }; html.generateDeletePromptHTML = function () { var html_code = '<div id="task-delete-radios" class="radios">' + '<p><b>' + language.data.delete_prompt_title + '</b></p>' + '<p><input id="task_delete_1" type="radio" name="taskDelete" value="1" /> <span>' + language.data.yes + '</span> &nbsp; &nbsp; &nbsp; <input id="task_delete_0" type="radio" name="taskDelete" value="0" /> <span>' + language.data.no + '</span></p>' + '</div>' + '<p class="ui-dialog-buttonset"><input id="cancel-settings" class="input-button" type="submit" value="' + language.data.cancel + '" /> <input id="confirm-settings" class="input-button input-bold" type="submit" value="' + language.data.save_changes + '" /></p>'; return html_code; }; html.generateSidebarPosHTML = function () { var html_code = '<div id="sidebar-pos-radios" class="radios">' + '<p><b>' + language.data.sidebar_pos_title + '</b></p>' + '<p><input id="sidebar_pos_left" type="radio" name="sidebarPos" value="left" /> <span>' + language.data.sidebar_left + '</span> &nbsp; &nbsp; &nbsp; <input id="sidebar_pos_right" type="radio" name="sidebarPos" value="right" /> <span>' + language.data.sidebar_right + '</span></p>' + '</div>' + '<p class="ui-dialog-buttonset"><input id="cancel-settings" class="input-button" type="submit" value="' + language.data.cancel + '" /> <input id="confirm-settings" class="input-button input-bold" type="submit" value="' + language.data.save_changes + '" /></p>'; return html_code; }; html.generateSwitchDateFormatHTML = function () { var html_code = '<div id="date-format-radios" class="radios"><p><input type="radio" id="date_de" name="switchDate" value="de"> <span>dd.mm.yyyy</span></p>' + '<p><input type="radio" id="date_us" name="switchDate" value="us"> <span>mm/dd/yyyy</span></p>' + '<p><input type="radio" id="date_en" name="switchDate" value="en"> <span>dd/mm/yyyy</span></p>' + '<p><input type="radio" id="date_iso" name="switchDate" value="iso"> <span>yyyy/mm/dd</span></p>' + '<p><input type="radio" id="date_us-dash" name="switchDate" value="us-dash"> <span>yyyy-mm-dd</span></p></div>' + '<div id="week-start-day-radios" class="radios">' + '<span class="ui-widget-header custom-dialog-headline">' + language.data.startday + '</span>' + '<p><input id="startday_1" type="radio" name="startDay" value="1" /> <span>' + language.data.monday + '</span></p>' + '<p><input id="startday_6" type="radio" name="startDay" value="6" /> <span>' + language.data.saturday + '</span></p>' + '<p><input id="startday_0" type="radio" name="startDay" value="0" /> <span>' + language.data.sunday + '</span></p>' + '</div>' + '<p class="ui-dialog-buttonset"><input id="cancel-dateformat" class="input-button" type="submit" value="' + language.data.cancel + '" /> <input id="confirm-dateformat" class="input-button input-bold" type="submit" value="' + language.data.save_changes + '" /></p>'; return html_code; }; html.generateSocialDialogHTML = function () {
    var html_code = '<div id="invitebox"><div class="wunderlistlogo"></div>' + '<div class="socialform"><p><textarea class="textarea-dialog" id="invite-text" maxlength="140">' + language.data.invitetextarea + '</textarea>' + '<p class="ui-dialog-buttonset"><input class="input-login input-social" type="text" id="email" name="email" value="' + language.data.invite_email + '" />' + '<input id="send_invitation" class="input-button button-social" type="submit" value="' + language.data.send + '" /></p></div>' + '<p class="socialmedia clearfix"><span class="icons">' + '<a href="http://www.stumbleupon.com/submit/?url=' + encodeURI('http://www.6wunderkinder.com') + '" target="_blank" class="stumbleupon"></a> ' + '<a href="http://digg.com/submit?url=' + encodeURI('http://www.6wunderkinder.com') + '" target="_blank" class="digg"></a> ' + '<a href="http://twitter.com/home?status=' + encodeURI('Wunderlist - http://www.6wunderkinder.com') + '" target="_blank" class="twitter"></a> ' + '<a href="http://www.facebook.com/sharer.php?u=' + encodeURI('http://www.6wunderkinder.com') + '&t=' + encodeURI('wunderlist') + '" target="_blank" class="facebook"></a> ' + '</span>' +
language.data.invite_without_email + '<span>' + language.data.invite_spread_word + '</span></p></div>'; return html_code;
}; html.generateEditProfileDialogHTML = function () { var html_code = '<p>' + language.data.edit_profile_desc + '</p>' + '<input class="input-normal"          type="text"     id="new_email"    name="new_email" placeholder="' + language.data.new_email_address + '" />' + '<input class="input-normal"          type="password" id="new_password" name="new_password" placeholder="' + language.data.new_password + '" />' + '<br /><p>' + language.data.edit_profile_old_pw + '</p>' + '<input class="input-normal"          type="password" id="old_password" name="old_password" placeholder="' + language.data.old_password + '" />' + '<div class="ui-dialog-buttonset"><input class="input-button"          type="submit"   id="cancel_edit_profile" value="' + language.data.cancel + '" />' + '<input class="input-button register" type="submit"   id="submit_edit_profile" value="' + language.data.save_changes + '" /></div>' + '<span class="error"></div>'; return html_code; }; html.strip_tags = function (input, allowed) {
    allowed = (((allowed || "") + "").toLowerCase().match(/<[a-z][a-z0-9]*>/g) || []).join(''); var tags = /<\/?([a-z][a-z0-9]*)\b[^>]*>/gi, commentsAndPhpTags = /<!--[\s\S]*?-->|<\?(?:php)?[\s\S]*?\?>/gi; return input.replace(commentsAndPhpTags, '').replace(tags, function ($0, $1)
    { return allowed.indexOf('<' + $1.toLowerCase() + '>') > -1 ? $0 : ''; });
}; html.replace_http_link = function (text) { var exp = /((http|https|ftp):\/\/[\w?=&.\/-;#~%-]+(?![\w\s?&.\/;#~%"=-]*>))/g; return text.replace(exp, "<a href='$1'>$1</a>"); }; html.validateEmail = function (email) {
    var reg = /^([A-Za-z0-9\+_\-\.])+\@([A-Za-z0-9_\-\.])+\.([A-Za-z]{2,4})$/; if (reg.test(email) == false) { $('.error').text(language.data.error_invalid_email); return false; }
    else { return true; }
}; html.str_replace = function (search, replace, subject) { return subject.split(search).join(replace); }; html.isInteger = function (s) { return (s.toString().search(/^-?[0-9]+$/) == 0); }; html.convertString = function (string, length) {
    string = string.split('<').join(escape('<')); string = string.split('>').join(escape('>')); string = string.split("'").join(escape("'")); if (length != undefined && length > 0)
        string = string.substr(0, length); return string;
}; html.xss_clean = function (str) {
    str = html.convertString(str); str = str.replace(/\\0/gi, '')
    str = str.replace(/\\\\0/gi, '')
    str = str.replace(/#(&\#*\w+)[\x00-\x20]+;#u/g, "$1;")
    str = str.replace(/#(&\#x*)([0-9A-F]+);*#iu/g, "$1$2;")
    str = str.replace(/%u0([a-z0-9]{3})/gi, "&#x$1;")
    str = str.replace(/%([a-z0-9]{2})/gi, "&#x$1;")
    results = str.match(/<.*?>/g, str)
    if (results) {
        var i
        for (i = 0; i < results.length; i++)
        { str = str.replace(results[i], html.html_entity_decode(results[i])); }
    }
    str = str.replace(/\\t+/g, " ")
    str = str.replace(/<\?php/g, '&lt;?php'); str = str.replace(/<\?PHP/g, '&lt;?PHP'); str = str.replace(/<\?/g, '&lt;?'); str = str.replace(/\?>/g, '?&gt;'); words = new Array('javascript', 'vbscript', 'script', 'applet', 'alert', 'document', 'write', 'cookie', 'window'); for (t in words) {
        temp = ''; for (i = 0; i < words[t].length; i++)
        { temp += words[t].substr(i, 1) + "\\s*"; }
        temp = temp.substr(0, temp.length - 3); myRegExp = new RegExp(temp, "gi")
        str = str.replace(myRegExp, words[t]);
    }
    str = str.replace(/\/<a.+?href=.*?(alert\(|alert&\#40;|javascript\:|window\.|document\.|\.cookie|<script|<xss).*?\>.*?<\/a>/gi, "")
    str = str.replace(/<img.+?src=.*?(alert\(|alert&\#40;|javascript\:|window\.|document\.|\.cookie|<script|<xss).*?\>/gi, ""); str = str.replace(/<(script|xss).*?\>/gi, "")
    str = str.replace(/(<[^>]+.*?)(onblur|onchange|onclick|onfocus|onload|onmouseover|onmouseup|onmousedown|onselect|onsubmit|onunload|onkeypress|onkeydown|onkeyup|onresize)[^>]*>/gi, "$1"); str = str.replace(/<(\/*\s*)(alert|applet|basefont|base|behavior|bgsound|blink|body|embed|expression|form|frameset|frame|head|html|ilayer|iframe|input|layer|link|meta|object|plaintext|style|script|textarea|title|xml|xss)([^>]*)>/ig, "&lt;$1$2$3&gt;"); str = str.replace(/(alert|cmd|passthru|eval|exec|system|fopen|fsockopen|file|file_get_contents|readfile|unlink)(\s*)\((.*?)\)/gi, "$1$2&#40;$3&#41;"); bad = new Array('document.cookie', 'document.write', 'window.location', "javascript\s*:", "Redirect\s+302")
    for (val in bad) {
        myRegExp = new RegExp(bad[val], "gi")
        str = str.replace(myRegExp, bad[val]);
    }
    str = str.replace(/<!--/g, "&lt;!--")
    str = str.replace(/-->/g, "--&gt;")
    return str
}; html.html_entity_decode = function (str) { var ta = document.createElement("textarea"); ta.innerHTML = str.replace(/</g, "&lt;").replace(/>/g, "&gt;"); result = ta.value; result = result.replace(/&#x([0-9a-f]{2,5})/g, String.fromCharCode("$1")); result = result.replace(/&#([0-9]{2,4})/g, String.fromCharCode("$1")); return result; }; html.showDateByLanguage = function (day, month, year) {
    var dateformat = settings.getDateformat(); if (dateformat == 'de')
        return day + '.' + month + '.' + year; else if (dateformat == 'en')
        return day + '/' + month + '/' + year; else if (dateformat == 'us')
        return month + '/' + day + '/' + year; else if (dateformat == 'us-dash')
        return year + '-' + month + '-' + day; else
        return year + '/' + month + '/' + day;
}; html.make_timestamp_to_string = function () {
    $('.timestamp').each(function (intIndex) {
        var timestamp = $(this).attr('rel'); var selected_date = new Date(timestamp * 1000); var day = selected_date.getDate(); var month = selected_date.getMonth() + 1; var year = selected_date.getFullYear(); var today = new Date(); var tday = today.getDate(); var tmonth = today.getMonth() + 1; var tyear = today.getFullYear(); $(this).removeClass('red'); if ((day < (tday - 1) && month == tmonth && year == tyear) || (month < tmonth && year == tyear) || (year < tyear)) {
            $(this).addClass('red'); if (day < 10) { day = '0' + day }
            if (month < 10) { month = '0' + month }
            $(this).html(html.showDateByLanguage(day, month, year));
        }
        else if ((day < tday && day > tday - 2) && month == tmonth && year == tyear)
        { $(this).addClass('red'); $(this).html(language.data.yesterday); }
        else if (day == tday && month == tmonth && year == tyear)
        { $(this).html(language.data.today); }
        else if ((day > tday && day < (tday + 2)) && month == tmonth && year == tyear)
        { $(this).html(language.data.tomorrow); }
        else {
            if (day < 10) { day = '0' + day }
            if (month < 10) { month = '0' + month }
            $(this).html(html.showDateByLanguage(day, month, year));
        }
    });
}; html.getWorldWideDate = function (date) {
    if (date == undefined)
        currentLocationDate = new Date(); else
        currentLocationDate = date; currentLocationDate.setMinutes(0); currentLocationDate.setHours(0); currentLocationDate.setSeconds(0); currentLocationDate.setMilliseconds(0); var offset = (currentLocationDate.getTimezoneOffset() / 60) * (-1); utc = currentLocationDate.getTime() + (currentLocationDate.getTimezoneOffset() * 60000); timeZoneLocation = new Date(utc + (3600000 * offset)); var timestamp = timeZoneLocation.getTime() / 1000; timestamp = Math.round(timestamp); return timestamp;
}; html.getMonthName = function (month_number) { var month = new Array(12); month[0] = "January"; month[1] = "February"; month[2] = "March"; month[3] = "April"; month[4] = "May"; month[5] = "June"; month[6] = "July"; month[7] = "August"; month[8] = "September"; month[9] = "October"; month[10] = "November"; month[11] = "December"; return month[month_number]; }; html.getDayName = function (day_number) { var day = new Array(7); day[0] = 'Sunday'; day[1] = 'Monday'; day[2] = 'Tuesday'; day[3] = 'Wednesday'; day[4] = 'Thursday'; day[5] = 'Friday'; day[6] = 'Saturday'; return day[day_number]; }; html.addRemoveDateButton = function (object) {
    $('#ui-datepicker-div div.remove_date').remove(); $('#ui-datepicker-div').append("<div class='remove_date'>" + language.data.no_date + "</div>"); $('#ui-datepicker-div div.remove_date').die(); $('#ui-datepicker-div div.remove_date').live('click', function ()
    { object.children('.ui-datepicker-trigger').remove(); object.children('input.datepicker').remove(); object.children('.showdate').remove(); object.children('.description').after("<input type='hidden' class='datepicker'/>"); html.createDatepicker(); $('#ui-datepicker-div').hide(); object.children('.timestamp').attr('rel', '0'); task.id = object.attr('id'); task.date = 0; task.update(); setTimeout(function () { datePickerOpen = false }, 10); });
}; html.createDatepicker = function () {
    var dayNamesEN = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday']; var dayNamesMinEN = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa']; var dayNamesShortEN = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat']; var monthNamesEN = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December']; var monthNamesShortEN = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec']; var dayNamesFR = ['Dimanche', 'Lundi', 'Mardi', 'Mercredi', 'Jeudi', 'Vendredi', 'Samedi']; var dayNamesMinFR = ['Di', 'Lu', 'Ma', 'Me', 'Je', 'Ve', 'Sa']; var dayNamesShortFR = ['Dim', 'Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam']; var monthNamesFR = ['Janvier', 'Février', 'Mars', 'Avril', 'Mai', 'Juin', 'Juillet', 'Août', 'Septembre', 'Octobre', 'Novembre', 'Décembre']; var monthNamesShortFR = ['Jan', 'Fév', 'Mar', 'Avr', 'Mai', 'Jui', 'Jui', 'Aoû', 'Sep', 'Oct', 'Nov', 'Dec']; var dayNamesDE = ['Sonntag', 'Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag']; var dayNamesMinDE = ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa']; var dayNamesShortDE = ['Son', 'Mon', 'Din', 'Mit', 'Don', 'Fre', 'Sam']; var monthNamesDE = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni', 'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember']; var monthNamesShortDE = ['Jan', 'Feb', 'Mär', 'Apr', 'Mai', 'Jun', 'Jul', 'Aug', 'Sep', 'Okt', 'Nov', 'Dez']; if (language.code == 'de')
    { var dayNamesLang = dayNamesDE; var dayNamesMinLang = dayNamesMinDE; var dayNamesShortLang = dayNamesShortDE; var monthNamesLang = monthNamesDE; var monthNamesShortLang = monthNamesShortDE; }
    else if (language.code == 'fr')
    { var dayNamesLang = dayNamesFR; var dayNamesMinLang = dayNamesMinFR; var dayNamesShortLang = dayNamesShortFR; var monthNamesLang = monthNamesFR; var monthNamesShortLang = monthNamesShortFR; }
    else
    { var dayNamesLang = dayNamesEN; var dayNamesMinLang = dayNamesMinEN; var dayNamesShortLang = dayNamesShortEN; var monthNamesLang = monthNamesEN; var monthNamesShortLang = monthNamesShortEN; }
    $(".datepicker").datepicker({ constrainInput: true, buttonImage: 'css/time.png', buttonImageOnly: true, buttonText: language.data.choose_date, showOn: 'both', firstDay: parseInt(settings.getWeekstartday()), dayNames: dayNamesLang, dayNamesMin: dayNamesMinLang, dayNamesShort: dayNamesShortLang, monthNames: monthNamesLang, monthNamesShort: monthNamesShortLang, beforeShow: function () {
        var $edit_li = $(this).parent(); setTimeout(function () {
            var timestamp = $edit_li.children('.timestamp').attr('rel'); if (timestamp != undefined && timestamp != 0)
            { var currentDate = new Date(timestamp * 1000); $edit_li.find('.datepicker').datepicker("setDate", currentDate); }
            html.addRemoveDateButton($edit_li);
        }, 5); datePickerOpen = true;
    }, onChangeMonthYear: function (year, month, inst) { var $edit_li = $(this).parent(); setTimeout(function () { html.addRemoveDateButton($edit_li); }, 5); }, onSelect: function (dateText, inst) {
        setTimeout(function () { datePickerOpen = false }, 10); var date = new Date(dateText); var timestamp = html.getWorldWideDate(date); if ($(this).parent().find('.input-add').length == 1) {
            var $date = $(".add input.datepicker").val(); var $html = '<span class="showdate timestamp" rel="' + timestamp + '">&nbsp;</span>'; $('.add .showdate').remove(); $('.add .input-add').after($html); if ($('.add .input-add').val().length > 0)
                $('.add .input-add').select(); else
                $('.add .input-add').focus();
        }
        else {
            var $date = $("li input.datepicker").val(); var $html = '<span class="showdate timestamp" rel="' + timestamp + '">&nbsp;</span>'; $(this).parent().find('img.ui-datepicker-trigger').remove(); if ($(this).parent().find('.showdate').length == 0) { $(this).parent().find('.description').after($html); }
            else { $(this).parent().find('.showdate').attr("rel", timestamp); $(this).parent().find('.datepicker').hide(); }
            task.id = $(this).parent().attr("id"); task.date = $(this).parent().find('span.timestamp').attr('rel'); task.update();
        }
        html.make_timestamp_to_string();
    }
    });
}; html.activateDragAndDrop = function (list_id) { $("a.list").droppable({ disabled: false }); $("#lists a#list" + list_id).droppable({ disabled: true }); $('#bottombar #left a').removeClass('active'); }; $(function () { $('a[href^=http]').live('click', function () { window.open(this.href); return false; }); });

/**** helpers/dialogs.js ****/

var dialogs = dialogs || {}; dialogs.confirmationDialog = null; dialogs.okDialog = null; dialogs.shareOwnEmailDialog = null; dialogs.deleteDialog = null; dialogs.whileSyncDialog = null; dialogs.cloudAppDialog = null; dialogs.shareSuccessDialog = null; dialogs.modalDialog = false; dialogs.generateDialog = function (title, html_code, dialogClass) { if (title == undefined) title = ''; if (html_code == undefined) html = ''; if (dialogClass == undefined) dialogClass = ''; return $('<div></div>').html(html_code).dialog({ autoOpen: false, draggable: false, modal: true, dialogClass: dialogClass, title: title }); }; dialogs.openDialog = function (customDialog) { $(customDialog).dialog('open'); dialogs.modalDialog = true; }; dialogs.closeDialog = function (customDialog) { $(customDialog).dialog('close'); dialogs.modalDialog = false; }; dialogs.showErrorDialog = function (message) { dialogs.openDialog(dialogs.generateDialog(language.data.error_occurred, '<p class="pl8">' + message + '</p>')); }; dialogs.showConfirmationDialog = function (callbackFunction) {
    dialogs.confirmationDialog = dialogs.generateDialog(language.data.account_deleted, '<p>' + language.data.account_del_successful + '</p><input class="input-button input-bold" type="submit" id="okay" value="' + language.data.okay + '" />'); dialogs.openDialog(dialogs.confirmationDialog); $('input#okay').click(function () {
        dialogs.closeDialog(dialogs.confirmationDialog); if (callbackFunction != undefined)
            callbackFunction();
    });
}; dialogs.showShareOwnEmailDialog = function () {
    if (dialogs.shareOwnEmailDialog == undefined)
    { dialogs.shareOwnEmailDialog = $('<div></div>').dialog({ autoOpen: true, draggable: false, modal: true, title: language.data.share_own_email, buttons: { 'OK': function () { $(this).dialog('close'); } } }); }
    else
    { dialogs.openDialog(dialogs.shareOwnEmailDialog); }
}; dialogs.showOKDialog = function (title, callbackFunction) {
    dialogs.okDialog = undefined; dialogs.okDialog = $('<div></div>').dialog({ autoOpen: true, draggable: false, modal: true, dialogClass: 'dialog-confirm', title: title, buttons: { 'OK': function () {
        $(this).dialog('close'); if (callbackFunction != undefined)
            callbackFunction();
    }
    }
    }); dialogs.openDialog(dialogs.okDialog);
}; dialogs.showFacebookConnectNotice = function () { $("<div><p>Your Facebook account is now connected to your existing Wunderlist account.</p><p><strong>If you do not remember signing up for Wunderlist before, you should change your Wunderlist password in the <em>settings</em> menu</strong>.</p></div>").dialog({ autoOpen: true, draggable: false, modal: true, title: 'Your Account', height: 250, buttons: { 'OK': function () { $(this).dialog('close'); } } }); }; dialogs.showSharedSuccessDialog = function (title) {
    if (dialogs.shareSuccessDialog == undefined)
    { dialogs.shareSuccessDialog = $('<div></div>').dialog({ autoOpen: true, draggable: false, modal: true, title: title, buttons: { 'OK': function () { $(this).dialog('close'); } } }); }
    else
    { dialogs.openDialog(dialogs.shareSuccessDialog); }
}; dialogs.showDeletedDialog = function (title) {
    if (dialogs.deleteDialog == undefined)
    { dialogs.deleteDialog = $('<div></div>').dialog({ autoOpen: true, draggable: false, modal: true, minWidth: 400, title: title, buttons: { 'OK': function () { $(this).dialog('close'); } } }); dialogs.deleteDialog.width("css", "auto !important") }
    else
    { dialogs.openDialog(dialogs.deleteDialog); }
}; dialogs.showCloudAppDialog = function () {
    if (dialogs.cloudAppDialog == undefined)
    { var buttons = {}; buttons[language.data.cloudapp_no] = function () { $(this).dialog('close'); }; buttons[language.data.cloudapp_yes] = function () { share.cloudapp(); $(this).dialog('close'); }; dialogs.cloudAppDialog = $('<div><p>' + language.data.cloudapp_1 + '</p><p class="small">' + language.data.cloudapp_2 + '</p></div>').dialog({ autoOpen: true, draggable: false, resizable: false, modal: true, title: language.data.cloudapp_title, buttons: buttons, open: function (event, ui) { $('.ui-dialog-buttonset button:last').focus(); $('.ui-dialog-buttonset button:last').addClass("input-bold"); } }); }
    else
    { dialogs.openDialog(dialogs.cloudAppDialog); }
}; dialogs.showHelpDialog = function () {
    if (dialogs.helpDialog == undefined)
    { var helpHTML = '<p><b>L:</b> ' + language.data.hotkey_help_list + '</p>'; helpHTML += '<p><b>Backspace:</b> ' + language.data.hotkey_help_delete + '</p>'; helpHTML += '<p><b>I:</b> ' + language.data.hotkey_help_inbox + '</p>'; helpHTML += '<p><b>' + language.data.hotkey_help_updown_key + ':</b> ' + language.data.hotkey_help_updown + '</p>'; helpHTML += '<p><b>T/N:</b> ' + language.data.hotkey_help_task + '</p>'; helpHTML += '<p><b>F:</b> ' + language.data.hotkey_help_search + '</p>'; helpHTML += '<p><b>1-8:</b> ' + language.data.hotkey_help_filters + '</p>'; helpHTML += '<p><b>B:</b> ' + language.data.hotkey_help_sidebar + '</p>'; helpHTML += '<p><b>H:</b> ' + language.data.hotkey_help_help + '</p>'; dialogs.helpDialog = $('<div>' + helpHTML + '</div>').dialog({ autoOpen: true, draggable: false, resizable: false, modal: true, title: language.data.hotkey_help_title }); }
    else
    { dialogs.openDialog(dialogs.helpDialog); }
};

/**** helpers/sidebar.js ****/

var sidebarFF = sidebarFF || {}; var sidebarToggle = false; var sidebarStatus = true; sidebarFF.init = function () {
    if (settings.getSidebar() == "right") { $("body").removeClass("sidebarleft"); if (sidebarStatus == true) { $(".togglesidebar").removeClass("hidden"); $("#sidebar").css("right", "0px"); $("#lists").css("right", "0px"); $("#content").css("right", "259px"); } else { $(".togglesidebar").addClass("hidden"); $("#sidebar").css("right", "-269px"); $("#lists").css("right", "-269px"); $("#content").css("right", "0px"); } }
    else if (settings.getSidebar() == "left") { $("body").addClass("sidebarleft"); if (sidebarStatus == true) { $(".togglesidebar").removeClass("hidden"); $("#sidebar").css("left", "0px"); $("#lists").css("left", "0px"); $("#content").css("left", "259px"); } else { $(".togglesidebar").addClass("hidden"); $("#sidebar").css("left", "-269px"); $("#lists").css("left", "-269px"); $("#content").css("left", "0px"); } }
    sidebarFF.toggle();
}; sidebarFF.toggle = function () { $(".togglesidebar").live('click', function () { if (settings.getSidebar() == "right") { if (sidebarStatus == true) { $(this).addClass("hidden"); $("#sidebar").stop().animate({ right: '-269' }); $("#lists").stop().animate({ right: '-269' }); $("#content").stop().animate({ right: '0' }); sidebarStatus = false; } else { $(this).removeClass("hidden"); $("#sidebar").stop().animate({ right: '0' }); $("#lists").stop().animate({ right: '0' }); $("#content").stop().animate({ right: '259' }); sidebarStatus = true; } } else if (settings.getSidebar() == "left") { if (sidebarStatus == true) { $(this).addClass("hidden"); $("#sidebar").stop().animate({ left: '-269' }); $("#lists").stop().animate({ left: '-269' }); $("#content").stop().animate({ left: '0' }); sidebarStatus = false; } else { $(this).removeClass("hidden"); $("#sidebar").stop().animate({ left: '0' }); $("#lists").stop().animate({ left: '0' }); $("#content").stop().animate({ left: '259' }); sidebarStatus = true; } } }); }; sidebarFF.refreshLists = function () { $.ajax({ url: '/ajax/lists/lists_html', type: 'GET', dataType: 'html', success: function (response) { $("#lists").empty(); $(response).appendTo("#lists").hide().fadeIn('fast'); } }); }

/**** frontend/menu.js ****/

var menu = menu || {}; menu.switchDateFormatDialog; menu.recreateTutsClicked = false; menu.init = function () { var menuTimer; var blurActive = 0; $("a.backgroundswitcher").click(function () { clearTimeout(menuTimer); $("#backgroundList").fadeIn("100"); $("#menublur").show(); }); $("#backgroundList").mouseenter(function () { clearTimeout(menuTimer); }); $("#left ul a").each(function () { if ($(this).next("ul").length != 0) { $(this).parent().addClass("hasChild"); } }); $("#left a:first").click(function () { clearTimeout(menuTimer); $(this).next().show(); $("#menublur").show(); }); $("#left ul a").mouseenter(function () { clearTimeout(menuTimer); $(this).parent().parent().find("ul").hide(); submenu = $(this).next("ul"); submenu.show(); $(this).parent().parent().find("li").removeClass("currenttree"); $(this).parent("li").addClass("currenttree"); }); $("#menublur").mouseenter(function () { menuTimer = setTimeout(function () { $("#left ul").hide(); $("#left a:first").removeClass("active"); $("#left li").removeClass("currenttree"); $("#menublur").hide(); $('#backgroundList').fadeOut(100); }, 350); }); }; menu.openSwitchDateFormatDialog = function () {
    if (menu.switchDateFormatDialog == undefined || $(menu.switchDateFormatDialog).dialog('isOpen') == false)
        menu.switchDateFormatDialog = dialogs.generateDialog(language.data.switchdateformat, html.generateSwitchDateFormatHTML()); dialogs.openDialog(menu.switchDateFormatDialog, 'switchdateformat-credits'); $('input#cancel-dateformat').die(); $('input#confirm-dateformat').die(); $('div#date-format-radios input#date_' + settings.getDateformat()).attr('checked', true); $('div#week-start-day-radios input#startday_' + settings.getWeekstartday()).attr('checked', true); $('input#cancel-dateformat').live('click', function () { $(menu.switchDateFormatDialog).dialog('close'); }); $('input#confirm-dateformat').live('click', function () { settings.dateformat = $('div#date-format-radios input:checked').val(); settings.weekstartday = $('div#week-start-day-radios input:checked').val(); settings.update(); $(menu.switchDateFormatDialog).dialog('close'); });
}; menu.openDeletePromptDialog = function () { var settingsDialog = undefined; settingsDialog = dialogs.generateDialog(language.data.delete_prompt_menu, html.generateDeletePromptHTML()); dialogs.openDialog(settingsDialog); $('input#cancel-settings').die(); $('input#confirm-settings').die(); var task_delete = settings.getDeleteprompt(); $('div.radios#task-delete-radios input#task_delete_' + task_delete).attr('checked', 'checked'); $('input#cancel-settings').live('click', function () { $(settingsDialog).dialog('close') }); $('input#confirm-settings').live('click', function () { settings.deleteprompt = $('div.radios#task-delete-radios input:checked').val(); settings.update(); $(settingsDialog).dialog('close'); }); }; menu.openSidebarPositionDialog = function () { var sidebarDialog = undefined; sidebarDialog = dialogs.generateDialog(language.data.sidebar_pos_menu, html.generateSidebarPosHTML()); dialogs.openDialog(sidebarDialog); $('input#cancel-settings').die(); $('input#confirm-settings').die(); var sidebar = settings.getSidebar(); $('div.radios#sidebar-pos-radios input#sidebar_pos_' + sidebar).attr('checked', 'checked'); $('input#cancel-settings').live('click', function () { $(sidebarDialog).dialog('close') }); $('input#confirm-settings').live('click', function () { settings.sidebar = $('div.radios#sidebar-pos-radios input:checked').val(); settings.update(); $(sidebarDialog).dialog('close'); $("#sidebar").fadeOut(500); $("#bottombar").fadeOut(500); $("#content").fadeOut(500, function () { location.reload(); }); }); }; menu.openCreditsDialog = function () { dialogs.openDialog(dialogs.generateDialog('What is Wunderlist?', html.generateCreditsDialogHTML(), 'dialog-credits')); }
menu.openBackgroundsDialog = function () { dialogs.openDialog(dialogs.generateDialog('Background Credits', html.generateBackgroundsDialogHTML(), 'background-credits')); }
menu.recreateTutorials = function () {
    if (menu.recreateTutsClicked == false)
    { menu.recreateTutsClicked = true; wunderlist.recreateTutorials(); }
    setTimeout(function () { menu.recreateTutsClicked = false; }, 200);
}
$(function () { menu.init(); $('ul.menu a.change_login_data').bind('click', function () { account.editProfile(); $("#menublur").click(); }); $('ul.menu a.delete_account').bind('click', function () { account.deleteAccount(); $("#menublur").click(); }); $('ul.menu a.credits').bind('click', function () { menu.openCreditsDialog(); $("#menublur").click(); }); $('ul.menu a.backgrounds').bind('click', function () { menu.openBackgroundsDialog(); $("#menublur").click(); }); $('ul.menu a.logout').bind('click', function () { account.logout(); }); $('ul.menu a.lang').bind('click', function () { language.switchLang($(this).attr('rel')); }); $('ul.menu a.switchdateformat').bind('click', function () { menu.openSwitchDateFormatDialog(); $("#menublur").click(); }); $('ul.menu a.deleteprompt').bind('click', function () { menu.openDeletePromptDialog(); $("#menublur").click(); }); $('ul.menu a.sidebarposition').bind('click', function () { menu.openSidebarPositionDialog(); $("#menublur").click(); }); $('ul.menu a.create_tutorials').bind('click', function () { menu.recreateTutorials(); $("#menublur").click(); }); $('ul.menu a.invitation').bind('click', function () { account.showInviteDialog(); $("#menublur").click(); }); });

/**** frontend/layout.js ****/

var layout = layout || {}; layout.registerProcess = function () { $("#showregistersubmit").click(function () { $('input.input-red').removeClass('input-red'); $('div.errorwrap p').text(''); $(".showlogindialog").hide(); $(".showregisterdialog").fadeIn("slow"); $("#account-loader").hide(); $('input#register-email').val($('input#login-email').val()); $('input#register-password').val($('input#login-password').val()); if ($('input#login-email').val() != language.data.email) { $("#loginsubmit").click(); } }); $("#showloginsubmit").click(function () { $('input.input-red').removeClass('input-red'); $('div.errorwrap p').text(''); $(".showregisterdialog").hide(); $(".showlogindialog").fadeIn("slow"); $("#account-loader").hide(); $('input#register-email').val(language.data.email); $('input#register-password').val(language.data.password); }); $("#showforgotpw").click(function () { $('input.input-red').removeClass('input-red'); $('div.errorwrap p').text(''); $(".showlogindialog .ui-dialog-title").text(language.data.forgot_password); $(".loginbuttons").hide(); $(".forgotpwbuttons").fadeIn(); $("#account-loader").hide(); if ($('input#login-email').val() != language.data.email) { $('#forgotpw-email').val($('input#login-email').val()).focus(); } }); $("#cancelforgotpw").click(function () { $(".forgotpwbuttons .errorwrap").hide(); $("#forgotpw-email").val(""); $(".showlogindialog .ui-dialog-title").text(language.data.register_title); $(".forgotpwbuttons").hide(); $(".loginbuttons").fadeIn(); }); $("#forgotpw-email").live('keyup', function (evt) { if (evt.keyCode == 13) { $('#forgot-pwd').click(); } }); }; layout.placeHolders = function () {
    $(':input[placeholder]').each(function () {
        var $this = $(this); if ($this.val() === '') { $this.val($this.attr('placeholder')); }
        $this.focus(function () { if ($this.val() === $this.attr('placeholder')) { $this.val(''); } }); $this.blur(function () { if ($this.val() === '') { $this.val($this.attr('placeholder')); } });
    });
}; layout.init = function () { layout.toolTips(); layout.placeHolders(); }; layout.toolTips = function () {
    $("a.more, span.more, #listfunctions a").live("mouseenter", function (e) {
        var content = $(this).attr("rel"); var offset = $(this).offset(); var width = $(this).width(); $("body").append("<p id='tooltip'>" + content + "</p>"); var tipWidth = $("#tooltip").width(); if ($(this).attr("id") == "sync") { tipWidth = "36"; }
        $("#tooltip").css("top", (offset.top - 35) + "px").css("left", (offset.left - tipWidth / 2) + "px").fadeIn("fast"); if ($(this).parent().attr("id") == "listfunctions") { $("#tooltip").css("top", (offset.top + 25)); }
        if (settings.getSidebar() == "left" && e.target.className == "list-cloud") { $("#tooltip").css("left", (offset.left - 40 - tipWidth / 2) + "px"); }
        if ($("#cloudtip:visible").length == 1 && $(this).parent().attr("id") == "listfunctions") { $("#tooltip").hide(); }
        if ($(this).parent().attr("id") == "left") { $("#tooltip").css("left", (offset.left + 17 - tipWidth / 2) + "px"); }
    }); $("a.more, span.more, #listfunctions a").live("mouseleave", function (e) { $("#tooltip").remove(); });
}; $(function () { layout.init(); layout.registerProcess(); document.onselectstart = function () { return false; }; $("a.showhelp").click(function () { dialogs.showHelpDialog(); }); });

/**** frontend/language.js ****/

var language = language || {}; language.availableLang = new Array('ar', 'ca', 'cs', 'da', 'de', 'en', 'es', 'fr', 'hr', 'hu', 'it', 'ja', 'ko', 'nl', 'no', 'pl', 'pt', 'ru', 'se', 'sk', 'tr', 'uk', 'zh'); language.load = function (code, data, fallback) {
    language.code = code; language.data = data; if (language.code != 'en') {
        language.translation = fallback; if (language.translation != '') {
            for (langstring in language.translation) {
                var translation = language.data[langstring]; if (translation == undefined)
                    language.data[langstring] = language.translation[langstring];
            }
        }
    }
}; language.switchLang = function (code) {
    if (language.availableLang.join(' ').indexOf(code) == -1)
        language.code = 'en'; else
        language.code = code; settings.language = language.code; settings.update();
}; language.replaceBasics = function () { $("a.history").text(language.data.history); $("a.addtask").text(language.data.add_task); $("a.button-add").text(language.data.add_task); $('h3 a.add').attr('title', language.data.add_list); $('.editp').attr('title', language.data.edit_list); $('.savep').attr('title', language.data.save_list); $('.deletep').attr('title', language.data.delete_list); };

/**** frontend/lists.js ****/

var listEventListener = false; var listShortcutListener = 0; var delete_dialog; var delete_item; bindListAddMode = function () { $('a.addlist').live('click', function () { addList(); makeListsSortable(); if (sidebarStatus == false) { $(".togglesidebar").click(); } }); }; addList = function () { $('div#lists').append(html.generateNewListElementHTML()); $('a.addlist').hide(); $('input.input-list').parent().children('.savep').show(); $('input.input-list').parent().children('.deletep').show(); setTimeout(function () { $('input.input-list').focus(); }, 50); setTimeout(function () { listShortcutListener = 0; }, 50); makeListsSortable(); }; bindListEditMode = function () {
    $('#lists div.editp').live('click', function () { $(this).hide(); listEditMode($(this).parent('a')); }); $('#lists a.list').live('dblclick', function () {
        if ($(this).children('input').length == 0)
        { $(this).children('div.editp').hide(); listEditMode($(this)); }
    });
}; listEditMode = function (listElement) { var ListElementName = listElement.find('b').text(); var ListElementShared = listElement.find('b').find("div").hasClass('sharedlist'); listElement.find('b').remove(); listElement.find('span').before('<input type="text" maxlength="50" value="' + ListElementName + '" rel="' + (ListElementShared == true ? 1 : 0) + '" />'); listElement.find('input').focus(); listElement.find('.savep').show(); listElement.find('.deletep').show(); }; bindListSaveMode = function () {
    $('#lists div.savep').live('click', function () {
        var listElement = $(this).parent('a'); var listId = listElement.attr('id').replace('list', ''); if (listId != 'x')
            saveList(listElement); else
            saveNewList(listElement);
    });
}; saveList = function (listElement) {
    list.id = listElement.attr("id").replace('list', ''); list.name = listElement.children('input').val(); if (list.name == '')
        list.name = language.data.new_list; list.update();
}; cancelSaveList = function () { $('div#lists a#x:last').remove(); }; saveNewList = function (listElement) {
    list.name = listElement.children('input').val(); if (list.name == '')
        list.name = language.data.new_list; list.insert(); setTimeout(function () { $('input.input-add').focus(); }, 100);
}; deleteList = function () {
    listEventListener = false; list.id = $(delete_item).parent().attr('id').replace('list', ''); list.deleted = 1; if (list.id != account.inbox) {
        if (list.id != 'x')
            list.update(); else
        { list.updateDeleted(); list.setDefault(); }
    }
}; bindListDeleteMode = function () {
    var buttonOptions = {}; buttonOptions[language.data.list_delete_no] = function () { $(this).dialog('close'); $('a.list input').focus(); }; buttonOptions[language.data.list_delete_yes] = function () { deleteList(); }; delete_dialog = $('<div></div>').dialog({ autoOpen: false, modal: true, draggable: false, dialogClass: 'dialog-delete-list', title: language.data.delete_list_question, buttons: buttonOptions, open: function (event, ui) { $('.ui-dialog-buttonset button:last').focus(); $('.ui-dialog-buttonset button:last').addClass("input-bold"); } }); $('#lists div.deletep').live('click', function (event) {
        delete_item = this; if (settings.getDeleteprompt() == 1)
            $(delete_dialog).dialog('open'); else
            deleteList(); event.stopPropagation();
    });
}; makeListsSortable = function () { $("div#lists").sortable({ axis: 'y', scroll: false, cursor: 'pointer', placeholder: 'placeholder', distance: 20, items: '.sortablelist', update: list.updatePositions }); }; $(function () {
    makeListsSortable(); bindListAddMode(); bindListEditMode(); bindListDeleteMode(); bindListSaveMode(); $("a.list").live('click', function () { if ($(this).attr('id').replace('list', '') != 'x') { wunderlist.getListById($(this).attr('id').replace('list', '')); } }); $("a.list").live('mouseover', function () {
        var countInput = $(this).children('input').length; if (countInput == 0)
            $(this).children('.editp').show(); if ($(this).attr('id').replace('list', '') != 'x')
            $(this).children('.deletep').show();
    }); $("a.list").live('mouseout', function () {
        var countInput = $(this).children('input').length; $(this).children('.editp').hide(); if (countInput == 0)
            $(this).children('.deletep').hide();
    }); $('a.list .deletep').live('mouseover', function () { listEventListener = true; }); $('a.list .deletep').live('mouseout', function () { listEventListener = false; }); $('a.list input').live('keyup', function (event) {
        if (event.keyCode == 13 || event.keyCode == 27) {
            listEventListener = true; var listElement = $(this).parent('a'); var list_id = listElement.attr('id').replace('list', ''); if (list_id != 'x')
                saveList(listElement); else
                saveNewList(listElement);
        }
    }); $('a.list input').live('focusout', function (event) {
        if (event.keyCode != 13 && event.keyCode != 27 && listEventListener == false) {
            var listElement = $(this).parent('a'); var listId = listElement.attr('id').replace('list', ''); if (listId != 'x')
                saveList(listElement); else
                saveNewList(listElement); listElement.children('.editp').hide(); listElement.children('.deletep').hide();
        }
        else
            listEventListener = false;
    }); $('#older_tasks_head').live('click', function () { $('#older_tasks').slideDown(function () { $('#hide_older_tasks').fadeIn(); }); $(this).hide(); return false; }); $('#hide_older_tasks').live('click', function () { $('#older_tasks').slideUp(function () { $('#older_tasks_head').fadeIn(); }); $(this).hide(); return false; });
});

/**** frontend/filters.js ****/

var filters = filters || {}; filters.init = function () {
    $('.list').click(filters.clearActiveStates); $('a#someday').click(function () { wunderlist.getFilteredTasks('date', 'withdate') }); $('a#withoutdate').click(function () { wunderlist.getFilteredTasks('date', 'nodate') }); $('a#all').click(function () { wunderlist.getFilteredTasks('all') }); $('a#starred').click(function () { wunderlist.getFilteredTasks('starred') }); $('a#today').click(function () { wunderlist.getFilteredTasks('today') }); $('a#tomorrow').click(function () { wunderlist.getFilteredTasks('tomorrow') }); $('a#thisweek').click(function () { wunderlist.getFilteredTasks('thisweek') }); $('a#done').click(function () { wunderlist.getFilteredTasks('done') }); $('#bottombar #left a.filter').click(function () {
        if ($(this).hasClass('loggedinas') == false)
        { filters.setActiveState(this); $("a.list").droppable({ disabled: false }); html.make_timestamp_to_string(); }
        else
            $(this).addClass('active');
    }); $('div#sidebar div#notification div').click(function () { wunderlist.getFilteredTasks('overdue'); html.make_timestamp_to_string(); $("a.list").droppable({ disabled: false }); $('#bottombar #left a').removeClass('active'); }); $('h3.clickable').live('click', function () { $('a#list' + $(this).attr('rel')).click(); }); wunderlist.updateBadgeCount();
}; filters.setActiveState = function (object) { $('#bottombar #left a').removeClass('active'); $('.list').removeClass('ui-state-disabled'); $(object).addClass('active'); }; filters.clearActiveStates = function (object) { $('#bottombar #left a').removeClass('active'); }; filters.updateBadges = function (todaycount, overduecount) {
    var today_has_no_badge = $('#bottombar #left a#today span').length == 0; var overdue_has_no_badge = $('#bottombar #left a#overdue span').length == 0; if (today_has_no_badge == true)
    { $('#left a#today').append('<span>' + todaycount + '</span>'); }
    else
    { $('#left a#today span').text(todaycount); $('#left a#today span').fadeOut('fast').fadeIn('fast'); $("#lists").css("bottom", "74px"); }
    if (overduecount > 1)
    { overdue_text = overduecount + ' ' + language.data.overdue_text_pl; $('div#sidebar div#notification').fadeIn('fast'); $("#lists").css("bottom", "74px"); }
    else if (overduecount == 1)
    { overdue_text = overduecount + ' ' + language.data.overdue_text_sl; $('div#sidebar div#notification').fadeIn('fast'); $("#lists").css("bottom", "74px"); }
    else
    { overdue_text = ''; $('div#sidebar div#notification').fadeOut('fast'); $("#lists").css("bottom", "36px"); }
    if (overdue_has_no_badge)
    { $('div#sidebar div#notification div').text(overdue_text); }
    else
    { $('div#sidebar div#notification div').text(overduecount); $('div#sidebar div#notification').fadeOut('fast').fadeIn('fast'); $("#lists").css("bottom", "74px"); }
    if (todaycount == 0)
    { $('#left a#today span').remove(); }
};

/**** frontend/tasks-add.js ****/

saveNewTask = function () {
    addTaskFocused = true; if ($("input.input-add").val() != '') {
        list_id = $(".mainlist").attr("rel"); task_text = $("input.input-add").val(); if (task_text != '') {
            timestamp = $(".add .showdate").attr('rel'); if (timestamp == undefined) timestamp = 0; addTaskFocused = false; if (isNaN(parseInt(list_id)) || $('#left a.active').length == 1) {
                var activeFilter = $('#left a.active'); if (list_id == 'today' || activeFilter.attr('id') == 'today')
                    timestamp = html.getWorldWideDate(); else if (list_id == 'tomorrow' || activeFilter.attr('id') == 'tomorrow')
                    timestamp = html.getWorldWideDate() + 86400; else if (list_id == 'starred' || activeFilter.attr('id') == 'starred')
                    task.important = 1; list_id = $('div#lists a b.inbox').parent('a').attr('id').replace('list', '');
            }
            task.name = task_text; task.list_id = list_id; task.date = timestamp; task.insert();
        }
        else
        { $("input.input-add").val(''); }
    }
}; bindAddTask = function () {
    var addTaskFocused = false; $("div.add input").bind('keyup', function (evt) {
        if (evt.keyCode == 13 && addTaskFocused == false)
        { saveNewTask(); $("input.input-add").val('').blur().focus(); }
        else if (evt.keyCode == 27)
        { $(this).val(''); $('div.add span.timestamp').remove(); $(this).blur(); }
    });
};

/**** frontend/tasks-edit.js ****/

var focusOutEnabled = true; var totalFocusOut = false; var datePickerOpen = false; var dateBeforEdit = ''; function saveTask() { focusOutEnabled = false; task.id = $('#task-edit').parent().attr("id"); task.name = $('#task-edit').val(); task.update(); $('#task-edit').parent().find('span.description').show(); $('#task-edit').remove(); focusOutEnabled = true; }
function cancelSaveTask() {
    focusOutEnabled = false; var listElement = $('#task-edit').parent(); listElement.children('.datepicker').remove(); listElement.children('img.ui-datepicker-trigger').remove(); listElement.children('input#task-edit').remove(); if (listElement.has('span.timestamp'))
        listElement.children('span.timestamp').replaceWith(dateBeforEdit); listElement.children('span.fav').show(); listElement.children('span.favina').show(); listElement.children('span.description').show(); focusOutEnabled = true;
}
$(function () {
    $('.mainlist li .description').live('dblclick', function () {
        var timestampElement = $(this).parent().children('span.timestamp'); if (timestampElement.is('span'))
            dateBeforEdit = timestampElement.clone(); else
            dateBeforEdit = ''; var doneIsActive = ($('div#left a#done.active').length == 1); if ($('#task-edit').length == 0 && doneIsActive == false)
        { var spanElement = $(this); var liElement = spanElement.parent(); titleText = spanElement.text(); spanElement.hide(); var html_code = '<input type="text" id="task-edit" value="" />'; liElement.children(".checkboxcon").after(html_code); $('input#task-edit').val(titleText); $("input#task-edit").select(); totalFocusOut = false; }
    }); $('.mainlist li .showdate').live('click', function () {
        var object = $(this).parent(); description = $(this).parent().find(".description"); dateInput = $(this).parent().find(".datepicker"); if ($("input#task-edit").length == 1) { saveTask(); }
        description.after("<input type='hidden' class='datepicker'/>"); html.createDatepicker(); datePickerInput = $(this).parent().find(".datepicker"); datePickerImage = $(this).parent().find(".ui-datepicker-trigger"); datePickerImage.click().remove(); setTimeout(function () {
            if (object.find("input.hasDatepicker").length == 2)
                object.find("input.hasDatepicker").eq(0).remove();
        }, 100);
    }); $('html').live('click', function (event) {
        var editInput = $('input#task-edit'); if (editInput.is('input')) {
            var clickedElement = $(event.target); if (clickedElement.attr('id') != 'task-edit') {
                if (editInput.val().length > 0)
                    saveTask();
            }
        }
    }); $('#task-edit').live('keyup', function (e) {
        if (e.keyCode == 13)
            saveTask(); else if (e.keyCode == 27)
        { totalFocusOut = false; cancelSaveTask(); }
    });
});

/**** frontend/tasks-check.js ****/

$(function () {
    var checkClicked = false; $('.checkboxcon').live('click', function (event) {
        if (checkClicked == false) {
            checkClicked = true; $(this).toggleClass("checked"); var is_checked = $(this).hasClass("checked"); task.id = $(this).parent().attr("id"); task.list_id = $(this).parent().attr("rel").replace('list', ''); if (is_checked)
            { task.done = 1; task.done_date = Math.round(new Date().getTime() / 1000); }
            else
            { task.done = 0; task.done_date = 0; }
            task.update();
        }
        setTimeout(function () { checkClicked = false; }, 100);
    });
});

/**** frontend/tasks-important.js ****/

$(function () {
    $("ul.mainlist span.fav").live("click", function () {
        if ($('a#done.active').length == 0)
        { task.id = $(this).parent('li').attr('id'); task.important = 0; task.update(); }
    }); $("ul.mainlist span.favina").live("click", function () {
        if ($('a#done.active').length == 0)
        { task.id = $(this).parent('li').attr('id'); task.important = 1; task.update(); }
    });
});

/**** frontend/tasks-note.js ****/

var notes = notes || {}; notes.openNotesDialog = function () { $(".dialog-notes").remove(); var notesDialog = undefined; notesDialog = dialogs.generateDialog(notes.currentNoteTitle, html.generateNotesDialogHTML(), 'dialog-notes'); dialogs.openDialog(notesDialog); }; notes.format = function (formattedText) { var linkExp = /((http|https|ftp):\/\/[\w?=&.\/-;#~%-]+(?![\w\s?&.\/;#~%"=-]*>))/g; var mailExp = /(([a-z0-9*._+]){1,}\@(([a-z0-9]+[-]?){1,}[a-z0-9]+\.){1,}([a-z]{2,4}|museum)(?![\w\s?&.\/;#~%"=-]*>))/g; formattedText = formattedText.replace(linkExp, "<a href='$1'>$1</a>"); formattedText = formattedText.replace(/\n/g, '<br>'); formattedText = formattedText.replace(mailExp, '<a href="mailto:$1">$1</a>'); return formattedText; }
notes.onlyRead = false; $(function () {
    noteEditMode = false; $('li span.note').live('click', function () {
        notes.openNotesDialog(); noteEditMode = false; notes.noteIcons = $('ul.mainlist li span.note, ul.donelist li span.note'); notes.note = $('.dialog-notes textarea').hide(); notes.savednote = $('.dialog-notes .savednote'); notes.savednoteInner = $('.dialog-notes .savednote .inner'); notes.currentNoteIcon = $(this); notes.currentNoteTitle = notes.currentNoteIcon.parent().children(".description").text(); notes.note.attr('id', notes.currentNoteIcon.parent().attr('id')); notes.onlyRead = $(this).parent('li').hasClass('done'); $(".dialog-notes .ui-dialog-title").text(notes.format(notes.currentNoteTitle)); if (notes.currentNoteIcon.html() != '' || notes.onlyRead == true) {
            notes.savednoteInner.html(notes.format(notes.currentNoteIcon.html())); if (notes.onlyRead == true)
                $('input#save-note').hide();
        }
        else
        { noteEditMode = true; $('input#save-note').addClass("button-login").val(language.data.save_changes); notes.note.show().focus(); notes.note.val(notes.currentNoteIcon.html()); notes.savednote.hide(); }
    }); $('.dialog-notes input#save-note').live('click', function () {
        notes.noteIcons = $('ul.mainlist li span.note, ul.donelist li span.note'); notes.note = $('.dialog-notes textarea'); notes.savednote = $('.dialog-notes .savednote'); notes.savednoteInner = $('.dialog-notes .savednote .inner'); if (noteEditMode == false)
        { noteEditMode = true; $(this).addClass("button-login").val(language.data.save_changes); notes.note.val(html.html_entity_decode(notes.currentNoteIcon.html())).show().focus(); notes.savednote.hide(); }
        else if (noteEditMode == true)
        { noteEditMode = false; $(this).removeClass("button-login").val(language.data.edit_changes); notes.currentNoteIcon.html(html.xss_clean(notes.note.val())); notes.savednoteInner.html(notes.format(notes.currentNoteIcon.html())); notes.savednote.show(); notes.note.hide(); task.id = notes.note.attr('id'); task.note = notes.note.val(); task.update(); }
        if (notes.currentNoteIcon.html().length == 0)
            notes.currentNoteIcon.removeClass("activenote"); else
            notes.currentNoteIcon.addClass("activenote");
    }); $('.dialog-notes .savednote').live('dblclick', function () {
        if (notes.onlyRead == false)
            $('.dialog-notes input#save-note').click();
    });
});

/**** frontend/tasks-delete.js ****/

var deleteTaskDialog; deleteTask = function (deleteElement) { var liElement = deleteElement.parent(); task.id = liElement.attr('id'); task.list_id = liElement.attr('rel'); task.deleted = 1; task.update(); }; openTaskDeletePrompt = function (deleteElement) { var buttonsDeleteTask = {}; buttonsDeleteTask[language.data.delete_task_no] = function () { $(this).dialog('close') }; buttonsDeleteTask[language.data.delete_task_yes] = function () { deleteTask(deleteElement); dialogs.closeDialog(deleteTaskDialog); }; deleteTaskDialog = $('<div></div>').dialog({ autoOpen: false, draggable: false, modal: true, dialogClass: 'dialog-delete-task', title: language.data.delete_task_question, buttons: buttonsDeleteTask, open: function (event, ui) { $('.ui-dialog-buttonset button:last').focus(); $('.ui-dialog-buttonset button:last').addClass("input-bold"); } }); dialogs.openDialog(deleteTaskDialog); }; $(function () {
    $("ul.mainlist li, ul.donelist li").live('mouseover', function () {
        var description = $(this).find('span.description'); var deletebutton = $(this).find('span.delete'); if (description.length == 1)
            deletebutton.show(); else
            deletebutton.hide();
    }); $(".mainlist li, .donelist li").live('mouseout', function () { $(this).find('.delete').hide(); }); $("li span.delete").live('click', function () {
        var deleteElement = $(this); if (deleteTaskDialog == undefined || deleteTaskDialog.dialog('isOpen') == false) {
            if (settings.getDeleteprompt() == 1)
                openTaskDeletePrompt(deleteElement); else
                deleteTask(deleteElement);
        }
    });
});

/**** frontend/tasks-draganddrop.js ****/

var taskDroped = false; makeListsDropable = function () { $('.list').droppable({ accept: 'ul.mainlist li', hoverClass: 'hover', drop: function (ev, ui) { taskDroped = true; var list = $(this); ui.draggable.hide('fast', function () { task.id = $(this).attr('id'); task.list_id = list.attr('id').replace('list', ''); task.update(); }); } }); }; makeFilterDropable = function () {
    $('a.filter').droppable({ accept: 'ul.mainlist li', hoverClass: 'droppable', drop: function (ev, ui) {
        var taskID = ui.draggable.attr('id'); var droppedTask = $('li#' + taskID); var droppedTaskParent = ($('ul.filterlist').length > 0 ? $('ul#filterlist' + droppedTask.attr('rel')) : $('ul#' + droppedTask.attr('rel'))); var activeFilter = droppedTaskParent.attr('rel'); var droppedFilter = $(this).attr('id'); var today = html.getWorldWideDate(); var tomorrow = (today + 86400); var droppedTaskDate = droppedTask.children('span.showdate'); var droppedTaskDateInput = droppedTask.children('input.datepicker'); var droppedTaskDateTrigger = droppedTask.children('.ui-datepicker-trigger'); var acceptFilter = false; if (droppedFilter == 'starred') {
            acceptFilter = true; if (activeFilter != 'starred' || !isNaN(parseInt(activeFilter))) {
                if (droppedTask.children('span.favina').length == 1)
                { task.id = taskID; task.important = 1; task.update(); }
            }
        }
        if (droppedFilter == 'today') {
            acceptFilter = true; if (activeFilter != 'today' || !isNaN(parseInt(activeFilter))) {
                if (droppedTaskDate.hasClass('timestamp') == false || droppedTaskDate.attr('rel') != today) {
                    if (droppedTaskDate.length == 0) { droppedTaskDateInput.remove(); droppedTaskDateTrigger.remove(); droppedTask.children('.description').after('<span class="showdate timestamp" rel="' + today + '">&nbsp;</span>'); } else { droppedTaskDate.addClass('timestamp').attr('rel', today); }
                    task.id = taskID; task.date = today; task.updateOnly(); html.make_timestamp_to_string();
                }
            }
        }
        if (droppedFilter == 'tomorrow') {
            acceptFilter = true; if (activeFilter != 'tomorrow' || !isNaN(parseInt(activeFilter))) {
                if (droppedTaskDate.hasClass('timestamp') == false || droppedTaskDate.attr('rel') != tomorrow) {
                    if (droppedTaskDate.length == 0) { droppedTaskDateInput.remove(); droppedTaskDateTrigger.remove(); droppedTask.children('.description').after('<span class="showdate timestamp" rel="' + tomorrow + '">&nbsp;</span>'); } else { droppedTaskDate.addClass('timestamp').attr('rel', tomorrow); }
                    task.id = taskID; task.date = tomorrow; task.updateOnly(); html.make_timestamp_to_string();
                }
            }
        }
        if (droppedFilter == 'withoutdate') {
            acceptFilter = true; if (activeFilter != 'withoutdate' || !isNaN(parseInt(activeFilter))) {
                if (droppedTaskDate.hasClass('timestamp') == true)
                { droppedTaskDate.remove(); droppedTask.children('.description').after("<input type='hidden' class='datepicker'/>"); html.createDatepicker(); task.id = taskID; task.date = 0; task.updateOnly(); }
            }
        }
        if ($('ul.filterlist').length > 0 && acceptFilter == true) {
            if ((droppedFilter != 'starred' && activeFilter != 'thisweek' && activeFilter != 'all' && activeFilter != droppedFilter) || (activeFilter == 'thisweek' && droppedFilter == 'withoutdate')) {
                if (droppedTaskParent.children('li').length == 2)
                { droppedTaskParent.prev().remove(); droppedTaskParent.remove(); }
                droppedTask.remove();
            }
        }
    }
    });
}; makeSortable = function () {
    $("ul.sortable").sortable({ scroll: false, delay: 100, appendTo: 'body', helper: function () { return $("<div class='dragging'></div>"); }, cursorAt: { top: 15, left: 15 }, cursor: 'pointer', placeholder: 'placeholder', update: function (ev, ui) {
        if ($('ul.filterlist').length > 0) {
            task.id = ui.item.attr('id'); task.list_id = (ui.item.parent('ul').attr('id') != undefined ? parseInt(ui.item.parent('ul').attr('id').replace('filterlist', '')) : ui.item.attr('rel')); if (task.list_id != ui.item.attr('rel'))
                task.update();
        }
        task.updatePositions();
    }
    });
};

/**** frontend/search.js ****/

var search = search || {}; search.clear = function () { $("input#search").val('').blur(); }; search.cancelEditTask = false; $(function () {
    $(".searchside .clearsearch").click(function () { search.clear(); }); $("input#search").keyup(function (event) {
        if (event.keyCode == 13) {
            if ($(this).val() != '')
            { wunderlist.liveSearch($(this).val()); html.make_timestamp_to_string(); }
            $('#left a').removeClass('active');
        }
        else if (event.keyCode == 27)
            $(this).val('').blur();
    }); var focusSearch = 0;
});

/**** frontend/layout.js ****/

var layout = layout || {}; layout.registerProcess = function () { $("#showregistersubmit").click(function () { $('input.input-red').removeClass('input-red'); $('div.errorwrap p').text(''); $(".showlogindialog").hide(); $(".showregisterdialog").fadeIn("slow"); $("#account-loader").hide(); $('input#register-email').val($('input#login-email').val()); $('input#register-password').val($('input#login-password').val()); if ($('input#login-email').val() != language.data.email) { $("#loginsubmit").click(); } }); $("#showloginsubmit").click(function () { $('input.input-red').removeClass('input-red'); $('div.errorwrap p').text(''); $(".showregisterdialog").hide(); $(".showlogindialog").fadeIn("slow"); $("#account-loader").hide(); $('input#register-email').val(language.data.email); $('input#register-password').val(language.data.password); }); $("#showforgotpw").click(function () { $('input.input-red').removeClass('input-red'); $('div.errorwrap p').text(''); $(".showlogindialog .ui-dialog-title").text(language.data.forgot_password); $(".loginbuttons").hide(); $(".forgotpwbuttons").fadeIn(); $("#account-loader").hide(); if ($('input#login-email').val() != language.data.email) { $('#forgotpw-email').val($('input#login-email').val()).focus(); } }); $("#cancelforgotpw").click(function () { $(".forgotpwbuttons .errorwrap").hide(); $("#forgotpw-email").val(""); $(".showlogindialog .ui-dialog-title").text(language.data.register_title); $(".forgotpwbuttons").hide(); $(".loginbuttons").fadeIn(); }); $("#forgotpw-email").live('keyup', function (evt) { if (evt.keyCode == 13) { $('#forgot-pwd').click(); } }); }; layout.placeHolders = function () {
    $(':input[placeholder]').each(function () {
        var $this = $(this); if ($this.val() === '') { $this.val($this.attr('placeholder')); }
        $this.focus(function () { if ($this.val() === $this.attr('placeholder')) { $this.val(''); } }); $this.blur(function () { if ($this.val() === '') { $this.val($this.attr('placeholder')); } });
    });
}; layout.init = function () { layout.toolTips(); layout.placeHolders(); }; layout.toolTips = function () {
    $("a.more, span.more, #listfunctions a").live("mouseenter", function (e) {
        var content = $(this).attr("rel"); var offset = $(this).offset(); var width = $(this).width(); $("body").append("<p id='tooltip'>" + content + "</p>"); var tipWidth = $("#tooltip").width(); if ($(this).attr("id") == "sync") { tipWidth = "36"; }
        $("#tooltip").css("top", (offset.top - 35) + "px").css("left", (offset.left - tipWidth / 2) + "px").fadeIn("fast"); if ($(this).parent().attr("id") == "listfunctions") { $("#tooltip").css("top", (offset.top + 25)); }
        if (settings.getSidebar() == "left" && e.target.className == "list-cloud") { $("#tooltip").css("left", (offset.left - 40 - tipWidth / 2) + "px"); }
        if ($("#cloudtip:visible").length == 1 && $(this).parent().attr("id") == "listfunctions") { $("#tooltip").hide(); }
        if ($(this).parent().attr("id") == "left") { $("#tooltip").css("left", (offset.left + 17 - tipWidth / 2) + "px"); }
    }); $("a.more, span.more, #listfunctions a").live("mouseleave", function (e) { $("#tooltip").remove(); });
}; $(function () { layout.init(); layout.registerProcess(); document.onselectstart = function () { return false; }; $("a.showhelp").click(function () { dialogs.showHelpDialog(); }); });

/**** frontend/share.js ****/

var share = share || {}; share.init = function () { $('a.list-email').live("click", share.email); $('a.list-cloud').live("click", dialogs.showCloudAppDialog); $('a.list-print').live("click", function () { window.print(); }); $('#cloudtip span.link').live("click", function () { window.open($(this).text()); $('#cloudtip').hide(); }); $('a#open-email').live('click', function () { $(this).attr('href', '#'); }); }; share.email = function () {
    var sharingTasks = $('ul.mainlist li'); if (sharingTasks.length > 0) {
        var name = encodeURI('Wunderlist - ' + $('#content h1:first').text()); var body = ''; $('ul.mainlist li').each(function () {
            body += encodeURI('• ' + $(this).children('span.description').text()); var showdate = $(this).children('span.showdate'); if ($(showdate).text() != '' && $(showdate).text() != null)
                body += '%20(' + $(showdate).text() + ')'; if ($(this).children('span.note').text() != '')
                body += '%0d%0a%0d%0a' + encodeURI($(this).children('span.note').text()) + '%0d%0a'; body += '%0d%0a';
        }); window.location.href = 'mailto:?subject=' + name + '&body=' + body + '%0d%0a' + encodeURI(' I generated this list with my task tool wunderlist from 6 Wunderkinder - Get it from http://www.6wunderkinder.com');
    }
    else
    { dialogs.showErrorDialog(language.data.empty_list); }
}; share.cloudapp = function () {
    if ($('ul.mainlist span.description').length > 0) {
        var data = { 'list': $('#content h1:first').text(), 'tasks': [] }; $('ul.mainlist li').each(function () {
            var new_task = []; new_task.push($(this).children('span.description').html()); if ($(this).children('span.showdate').html() != '')
                new_task.push($(this).children('span.showdate').html()); else
                new_task.push(0); if ($(this).children('span.note').html() != '')
                new_task.push($(this).children('span.note').html()); data['tasks'].push(new_task);
        }); $.ajax({ url: 'ajax/share/cloudapp', type: 'POST', data: data, success: function (response_data, text, xhrobject) {
            var response = ajaxresponse.check(response_data); if (response.status == 'success')
            { $('#cloudtip span.link').text(response.url).parent().show(); }
            else
            { dialogs.showErrorDialog(language.data.try_again_later); }
        }, error: function () { dialogs.showErrorDialog(language.data.try_again_later); }
        });
    }
    else
    { dialogs.showErrorDialog(language.data.empty_list); }
}; share.print = function () {
    if ($('ul.mainlist span.description').length > 0) {
        var data = { 'list': $('#content h1:first').text(), 'tasks': [] }; $('ul.mainlist li').each(function () {
            var new_task = []; new_task.push($(this).children('span.description').html()); if ($(this).children('span.showdate').html() != '')
                new_task.push($(this).children('span.showdate').html()); else
                new_task.push(0); if ($(this).children('span.note').html() != '')
                new_task.push($(this).children('span.note').html()); data['tasks'].push(new_task);
        }); $.ajax({ url: 'ajax/share/printing', type: 'POST', data: data, success: function (response_data, text, xhrobject) {
            var response = ajaxresponse.check(response_data); if (response.status == 'success')
            { var printWindow = window.open('/blank.html'); printWindow.document.write(response.html); printWindow.print(); }
            else
            { dialogs.showErrorDialog(language.data.try_again_later); }
        }, error: function () { dialogs.showErrorDialog(language.data.try_again_later); }
        });
    }
    else
    { dialogs.showErrorDialog(language.data.empty_list); }
};

/**** frontend/hotkeys.js ****/

var hotkeys = hotkeys || {}; hotkeys.eventListener = false; hotkeys.filters = ['all', 'starred', 'done', 'today', 'tomorrow', 'thisweek', 'someday', 'withoutdate']; hotkeys.deleteList = function () {
    cancelSaveTask(); var listElement = $('a.ui-state-disabled'); if (listElement.attr('id').replace('list', '') != account.inbox) {
        var buttonOptions = {}; buttonOptions[language.data.list_delete_no] = function () { $(this).dialog('close'); $('a.list input').focus(); }; buttonOptions[language.data.list_delete_yes] = function () {
            list.id = listElement.attr('id').replace('list', ''); list.deleted = 1; if (list.id != account.inbox) {
                if (list.id != 'x')
                    list.update(); else
                { list.updateDeleted(); list.setDefault(); }
            }
            $(this).dialog('close');
        }; $('<div></div>').dialog({ autoOpen: true, modal: true, draggable: false, dialogClass: 'dialog-delete-list', title: language.data.delete_list_question, buttons: buttonOptions, open: function (event, ui) { $('.ui-dialog-buttonset button:last').focus(); $('.ui-dialog-buttonset button:last').addClass("input-bold"); } });
    }
}; $(function () {
    $(document).bind('keydown', 'l', function (event) {
        if ($('textarea:focus').length == 0 && $('input:focus').length == 0) {
            if (hotkeys.eventListener == false) {
                cancelSaveTask(); if (listShortcutListener == 0)
                    addList(); if (sidebarStatus == false) { $(".togglesidebar").click(); }
                listShortcutListener++; hotkeys.eventListener = true; setTimeout(function () { hotkeys.eventListener = false; }, 100);
            }
        }
    }); $(document).bind('keydown', 'up', function (event) {
        if ($('textarea:focus').length == 0 && $('input:focus').length == 0) {
            if (hotkeys.eventListener == false) {
                hotkeys.eventListener = true; if ($('div#lists > a.ui-state-disabled').prev().attr('id') == undefined)
                    $('div#lists a').last().click(); else
                    $('div#lists > a.ui-state-disabled').prev().click(); setTimeout(function () { hotkeys.eventListener = false; }, 100);
            }
        }
    }); $(document).bind('keydown', 'down', function (event) {
        if ($('textarea:focus').length == 0 && $('input:focus').length == 0) {
            if (hotkeys.eventListener == false) {
                hotkeys.eventListener = true; if ($('div#lists > a.ui-state-disabled').next().attr('id') == undefined)
                    $('div#lists a').first().click(); else
                    $('div#lists > a.ui-state-disabled').next().click(); setTimeout(function () { hotkeys.eventListener = false; }, 100);
            }
        }
    }); $(document).bind('keydown', 'n', function (event) { if ($('textarea:focus').length == 0 && $('input:focus').length == 0) { cancelSaveTask(); setTimeout(function () { $('.add input.input-add').focus(); }, 100); } }); $(document).bind('keydown', 't', function (event) { if ($('textarea:focus').length == 0 && $('input:focus').length == 0) { cancelSaveTask(); setTimeout(function () { $('.add input.input-add').focus(); }, 100); } }); $(document).bind('keydown', 'f', function (event) { if ($('textarea:focus').length == 0 && $('input:focus').length == 0) { cancelSaveTask(); setTimeout(function () { $('input#search').focus(); }, 100); } }); for (var ix = 0; ix < hotkeys.filters.length; ix++) {
        $(document).bind('keydown', '' + (ix + 1), function (event) {
            if ($('textarea:focus').length == 0 && $('input:focus').length == 0) {
                if (hotkeys.eventListener == false)
                { hotkeys.eventListener = true; $('a#' + hotkeys.filters[event.keyCode - 49]).click(); setTimeout(function () { hotkeys.eventListener = false; }, 100); }
            }
        });
    }
    $(document).bind('keydown', 'h', function (event) { if ($('textarea:focus').length == 0 && $('input:focus').length == 0) { cancelSaveTask(); dialogs.showHelpDialog(); } }); $(document).bind('keydown', 'Backspace', function (event) {
        if ($('textarea:focus').length == 0 && $('input:focus').length == 0) {
            if (hotkeys.eventListener == false)
            { hotkeys.eventListener = true; hotkeys.deleteList(); setTimeout(function () { hotkeys.eventListener = false; }, 100); }
            event.stopPropagation(); event.preventDefault();
        }
    }); $(document).bind('keydown', 'i', function (event) {
        if ($('textarea:focus').length == 0 && $('input:focus').length == 0) {
            if (hotkeys.eventListener == false)
            { hotkeys.eventListener = true; cancelSaveTask(); $('a#' + account.inbox).click(); setTimeout(function () { hotkeys.eventListener = false; }, 100); }
        }
    }); $(document).bind('keydown', 'b', function (event) {
        if ($('textarea:focus').length == 0 && $('input:focus').length == 0) {
            if (hotkeys.eventListener == false)
            { $(".togglesidebar").click(); }
        }
    });
});