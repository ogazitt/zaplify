var dialogs = dialogs || {};
dialogs.confirmationDialog = null;
dialogs.okDialog = null;
dialogs.shareOwnEmailDialog = null;
dialogs.deleteDialog = null;
dialogs.whileSyncDialog = null;
dialogs.shareSuccessDialog = null;
dialogs.modalDialog = false;
dialogs.generateDialog = function (title, html_code, dialogClass) {
    if (title == undefined) title = '';
    if (html_code == undefined) html = '';
    if (dialogClass == undefined) dialogClass = '';
    return $('<div></div>').html(html_code).dialog({
        autoOpen: false,
        draggable: false,
        modal: true,
        dialogClass: dialogClass,
        title: title
    });
};

dialogs.openDialog = function (customDialog) {
    $(customDialog).dialog('open');
    dialogs.modalDialog = true;
};

dialogs.closeDialog = function (customDialog) {
    $(customDialog).dialog('close');
    dialogs.modalDialog = false;
};

dialogs.destroyDialog = function (customDialog) {
    $(customDialog).dialog('close');
    $(customDialog).dialog('destroy');
    dialogs.modalDialog = false;
};

dialogs.showErrorDialog = function (message) {
    dialogs.openDialog(dialogs.generateDialog(language.data.error_occurred, '<p class="pl8">' + message + '</p>'));
}; 

dialogs.showConfirmationDialog = function (callbackFunction) {
    dialogs.confirmationDialog = dialogs.generateDialog(language.data.account_deleted,
        '<p>' + language.data.account_del_successful + '</p><input class="input-button input-bold" type="submit" id="okay" value="' + language.data.okay + '" />');
    dialogs.openDialog(dialogs.confirmationDialog); 
    $('input#okay').click(function () {
        dialogs.closeDialog(dialogs.confirmationDialog);
        if (callbackFunction != undefined)
			callbackFunction();
	});
}; 

dialogs.showShareOwnEmailDialog = function () {
	if (dialogs.shareOwnEmailDialog == undefined) {
	    dialogs.shareOwnEmailDialog = $('<div></div>').dialog({
	        autoOpen: true,
	        draggable: false,
	        modal: true,
	        title: language.data.share_own_email,
	        buttons: { 'OK': function () { $(this).dialog('close'); } }
	    }); 
    }
	else {
	    dialogs.openDialog(dialogs.shareOwnEmailDialog); 
    }
}; 

dialogs.showOKDialog = function (title, callbackFunction) {
    dialogs.okDialog = undefined;
    dialogs.okDialog = $('<div></div>').dialog({
        autoOpen: true,
        draggable: false,
        modal: true,
        dialogClass: 'dialog-confirm',
        title: title,
        buttons: { 
            'OK': function () {
                $(this).dialog('close'); 
                if (callbackFunction != undefined)
			        callbackFunction();
	        }
	    }
    }); 
    dialogs.openDialog(dialogs.okDialog);
};

dialogs.showFacebookConnectNotice = function () {
    $("<div><p>Your Facebook account is now connected to your existing Wunderlist account.</p><p><strong>If you do not remember signing up for Wunderlist before, you should change your Wunderlist password in the <em>settings</em> menu</strong>.</p></div>").dialog({
        autoOpen: true,
        draggable: false,
        modal: true,
        title: 'Your Account',
        height: 250,
        buttons: { 'OK': function () { $(this).dialog('close'); } }
    });
}; 

dialogs.showSharedSuccessDialog = function (title) {
	if (dialogs.shareSuccessDialog == undefined) {
	    dialogs.shareSuccessDialog = $('<div></div>').dialog({
	        autoOpen: true,
	        draggable: false,
	        modal: true,
	        title: title,
	        buttons: { 'OK': function () { $(this).dialog('close'); } }
	    }); 
    }
	else {
	    dialogs.openDialog(dialogs.shareSuccessDialog); 
    }
};

dialogs.showDeletedDialog = function (title) {
    if (dialogs.deleteDialog == undefined) {
        dialogs.deleteDialog = $('<div></div>').dialog({
            autoOpen: true,
            draggable: false,
            modal: true,
            minWidth: 400,
            title: title,
            buttons: { 'OK': function () { $(this).dialog('close'); } }
        });
        dialogs.deleteDialog.width("css", "auto !important");
    }
    else {
        dialogs.openDialog(dialogs.deleteDialog); 
    }
};  

dialogs.showHelpDialog = function () {
	if (dialogs.helpDialog == undefined) {
	    var helpHTML = '<p><b>L:</b> ' = language.data.hotkey_help_list + '</p>';
	    helpHTML += '<p><b>Backspace:</b> ' + language.data.hotkey_help_delete + '</p>';
	    helpHTML += '<p><b>I:</b> ' + language.data.hotkey_help_inbox + '</p>';
	    helpHTML += '<p><b>' + language.data.hotkey_help_updown_key + ':</b> ' + language.data.hotkey_help_updown + '</p>'; 
        helpHTML += '<p><b>T/N:</b> ' + language.data.hotkey_help_task + '</p>';
	    helpHTML += '<p><b>F:</b> ' + language.data.hotkey_help_search + '</p>';
	    helpHTML += '<p><b>1-8:</b> ' + language.data.hotkey_help_filters + '</p>';
	    helpHTML += '<p><b>B:</b> ' + language.data.hotkey_help_sidebar + '</p>';
	    helpHTML += '<p><b>H:</b> ' + language.data.hotkey_help_help + '</p>';
	    dialogs.helpDialog = $('<div>' + helpHTML + '</div>').dialog({
	        autoOpen: true,
	        draggable: false,
	        resizable: false,
	        modal: true,
	        title: language.data.hotkey_help_title
	    }); 
    }
	else {
	    dialogs.openDialog(dialogs.helpDialog); 
    }
};
