/* settings.js */

var settings = settings || {};
settings.data = {};

settings.update = function () { /* settings.updateSettings(); */ settings.updateSettings2(); };

settings.setDefault = function () {
    settings.language = undefined;
    settings.background = undefined;
    settings.dateformat = undefined;
    settings.weekstartday = undefined;
    settings.deleteprompt = undefined;
    settings.sidebar = undefined;
}; 

settings.updateLanguage = function () {
    if (settings.language != undefined) {
        settings.data.language = settings.language;
        window.location.reload(); 
    }
}; 

settings.updateBackground = function () {
    if (settings.background != undefined)
        settings.data.background = settings.background;
}; 

settings.updateDateformat = function () {
    if (settings.dateformat != undefined) {
        settings.data.dateformat = settings.dateformat;
        html.make_timestamp_to_string(); 
    }
}; 

settings.updateWeekstartday = function () {
    if (settings.weekstartday != undefined) {
        settings.data.weekstartday = settings.weekstartday;
        $('div.add input.datepicker').datepicker('destroy');
        html.createDatepicker(); 
    }
}; 

settings.updateDeleteprompt = function () {
    if (settings.deleteprompt != undefined)
        settings.data.deleteprompt = settings.deleteprompt;
}; 

settings.updateSidebarPosition = function () {
    if (settings.sidebar != undefined)
        settings.data.sidebar = settings.sidebar;
};

settings.setShortcutkey = function () {
    settings.data.shortcutkey = settings.data.platform == 'Mac OS X' ? 'Cmd' : 'Strg';
}; 
    
settings.getBackground = function () {
    if (background.list[settings.data.background] == undefined)
        return 'bgone'; 
    else
        return html.str_replace('"', '', settings.data.background.toString());
}; 

settings.getDateformat = function () {
    if (settings.data.dateformat == undefined)
        return 'us'; 
    else
        return html.str_replace('"', '', settings.data.dateformat.toString());
}; 

settings.getWeekstartday = function () {
    if (settings.data.weekstartday == undefined)
        return '1'; 
    else
        return html.str_replace('"', '', settings.data.weekstartday.toString());
}; 

settings.getDeleteprompt = function () {
    if (settings.data.deleteprompt == undefined)
        return 1; 
    else
        return parseInt(html.str_replace('"', '', settings.data.deleteprompt.toString()));
}; 

settings.getSidebar = function () {
    if (settings.data.sidebar == undefined)
        return 'right'; 
    else
        return html.str_replace('"', '', settings.data.sidebar.toString());
}; 

settings.getShortcutkey = function () {
    if (settings.data.shortcutkey == undefined)
        return 'Strg'; else
        return html.str_replace('"', '', settings.data.shortcutkey.toString());
};

settings.updateSettings2 = function () {
    settings.updateLanguage();
    settings.updateBackground();
    settings.updateDateformat();
    settings.updateWeekstartday();
    settings.updateDeleteprompt();
    settings.updateSidebarPosition();
    settings.setDefault();
}

settings.updateSettings = function () {
    $.ajax({
        url: '/ajax/settings',
        type: 'POST',
        data: { 'settings': JSON.stringify(settings) }, 
        success: function (response_data, text, xhrobject) {
            var response = ajaxresponse.check(response_data); 
            if (response.status == 'success') {
                settings.updateLanguage();
                settings.updateBackground();
                settings.updateDateformat();
                settings.updateWeekstartday();
                settings.updateDeleteprompt();
                settings.updateSidebarPosition(); 
            }
            settings.setDefault();
        }, 
        error: settings.setDefault
    });
}

settings.init = function () {
    settings.setDefault();
    // Initiate user settings
    settings.data.background = "bgtwelve";
    settings.data.dateformat = "us";
    settings.data.weekstartday = "1";
    settings.data.deleteprompt = "1";
    settings.data.sidebar = "right";
    settings.data.language = "en";
    settings.data.platform = "Unknown Windows OS";
    settings.setShortcutkey();

    // initialize language
    language.load(settings.data.language, { "lists": "Lists", "delete_list_question": "Do you really want to delete this list?", "delete_list_no": "No, I'm Sorry", "delete_list_yes": "Yes, delete this list", "delete_task_question": "Do you really want to delete this task?", "delete_task_no": "No, I'm Sorry", "delete_task_yes": "Yes, delete this task", "search": "Search everywhere...", "add_task": "Add your task here", "delete_task": "Delete Task", "sidebar_toggle": "Toggle Sidebar", "add_list": "Add List", "edit_list": "Edit List", "save_list": "Save List", "delete_list": "Delete List", "history": "Tasks History", "search_results": "Search Results", "no_search_results": "No Search Results", "inbox": "Inbox", "today": "Today", "yesterday": "Yesterday", "starred": "Starred", "tomorrow": "Tomorrow", "done_today": "Recently done", "done_yesterday": "Done yesterday", "thisweek": "Next 7 Days", "someday": "Later", "all": "All", "done": "Done", "all_starred_tasks": "Important Tasks", "all_today_tasks": "Tasks Today", "all_tomorrow_tasks": "Tasks Tomorrow", "all_someday_tasks": "Tasks Someday", "all_later_tasks": "Tasks Later", "all_thisweeks_tasks": "Tasks within next 7 days", "all_done_tasks": "Done Tasks", "two": "Two", "three": "Three", "four": "Four", "five": "Five", "six": "Six", "seven": "Seven", "eight": "Eight", "nine": "Nine", "ten": "Ten", "days_ago": "days ago", "all_tasks": "All Tasks", "overdue_tasks": "Overdue Tasks", "no_date": "No Date", "choose_date": "Choose Date", "no_results": "No Results", "reset_window_size": "Reset Window Size", "options": "Options", "fav": "No prioritization", "default_task_1": "Doubleclick to edit me", "default_task_2": "Press N or T to create a new task", "default_task_3": "Press F to search for tasks", "default_task_4": "Change your background at the bottom bar", "default_task_5": "Press L to add a new list", "default_task_6": "Press Up or Down to step through your lists", "default_task_7": "Ignore me, I'm already done", "default_task_8": "Create a new list by clicking on the \"Add List\" button", "default_task_9": "Press Backspace to delete the selected list", "default_task_10": "Press H to open the hotkey help dialog", "favina": "Prioritize", "no_thanks": "No Thanks", "login": "Login", "register": "Register Now", "register_your_account": "Register Your Account", "email": "Email", "password": "Password", "forgot_password": "Forgot password?", "input_forgot_password": "Please enter your Email to reset password", "register_title": "Signup or login to use Wunderlist for Web", "edit_profile_title": "Change email address and password", "edit_profile_desc": "If you want to change your password or email address, just type your new email address and password!", "error_invalid_email": "Invalid email Address", "error_login_failed": "Login denied. Please try again.", "tutorials": "Tutorials", "overdue_text_pl": "tasks are overdue", "overdue_text_sl": "task is overdue", "day_ago": "day ago", "list_delete_no": "No, cancel", "list_delete_yes": "Yes, delete it", "logout": "Logout", "new_list": "New List", "sync_failure": "Synchronization failed. Please try again.", "sync_denied": "Synchronization was denied. Please try again.", "invalid_email": "The email you entered is invalid.", "registration_failed": "Registration failed. Please try again.", "password_success": "A new password has been sent to you.", "password_failed": "Resetting the password failed.", "register_create_user": "Yes, create user", "register_question": "Create a new user?", "language": "Language", "account": "Account", "extra": "Extras", "credits": "About Wunderlist", "wunderkinder": "6 Wunderkinder", "wunderkinder_fb": "Follow us on Facebook", "wunderkinder_tw": "Follow us on Twitter", "delete_account": "Delete Account", "sign_in": "Sign in", "restart": "The changes you made require a restart. Please restart Wunderlist manually.", "invitation": "Invite Your Friends", "invitetextarea": "Hey, I'm managing my tasks with Wunderlist now and it looks pretty cool. It's 100% free, maybe you want to check it out!", "older_tasks": "Show done tasks", "sync_error": "An error in the sync process occurred. Please try again.", "login_error": "An error in the login process occurred. Please try again.", "register_error": "An error in the register process occurred. Please try again.", "forgotpw_error": "An error in the password retrieval process occurred.", "error_occurred": "An error occurred.", "no_internet": "There is no internet connection available.", "withoutdate": "Without date", "sync_not_logged_in": "If you want to sync your data, you have to create an online account for free.", "sync_not_logged_in_yes": "Yes, I want to register", "sync_not_logged_in_no": "No, I don't want to register", "sync_not_logged_in_title": "Do you want to sync?", "sync_not_exist": "User does not exist", "change_login_data": "Edit Account", "new_email_address": "New email address", "new_password": "New password", "old_password": "Current password", "save_changes": "Save", "edit_changes": "Edit", "cancel": "Cancel", "reset": "Reset", "authentication_failed": "Authentification problem", "wrong_password": "Wrong password", "invitation_error": "An error in the invite process occurred. Please try again.", "invitation_success": "Your invitation has been successfully sent!", "invitation_invalid_email": "You have entered an invalid email address. Please try again.", "invite": "Invite your friends to Wunderlist", "invite_without_email": "Don't want to send an email?", "invite_spread_word": "Just spread the word on...", "invite_email": "yourfriend@email.com", "invite_more": "Invite more friends", "edit_profile_old_pw": "Please enter your old password to confirm your changes.", "delete_account_desc": "If you want to delete your account, please enter your password here:", "delete_account_title": "Delete account", "email_already_exists": "Email address already exists", "delete_account_failure": "The account could not be deleted", "delete_account_denied": "Access denied. The account could not be deleted", "okay": "Ok", "account_deleted": "Account Deleted", "account_del_successful": "The account has been successfully deleted", "no_sync": "Synchronizing failed", "password_not_empty": "The password must not be empty", "email_not_empty": "The email must not be empty", "login_hint": "You can also register later to use the synchronization.", "error_duplicated_email": "This email is already registered.", "create_tutorials": "Reset Tutorials", "changed_account_data": "Your account was successfully changed.", "changelog": "Changelog", "no_logout_sync": "You can't logout while synchronizing, wait until the synchronization is finished", "add_task_button": "Save Task", "backgrounds": "Background Credits", "switchdateformat": "Date Format", "startday": "Startday of the week", "sunday": "Sunday", "monday": "Monday", "cloudapp_title": "Are you sure to publish your tasks?", "cloudapp_1": "This feature allows you to share your list with CloudApp. If you click yes, your list gets uploaded to CloudApp and a secret URL will be generated. You can use the URL to share your tasks easily with others who don't have a Wunderlist account or share them public on Twitter.", "cloudapp_2": "HINT: This is permanent. Anyone who knows the URL will be able to see your current list. It gets deleted automatically after 30 days.", "cloudapp_no": "No, cancel", "cloudapp_yes": "Yes, share it", "exit_wunderlist": "Exit Wunderlist", "empty_list": "It is not possible to send or share an empty list", "try_again_later": "Mmmh, something went wrong. Maybe you try it again in a few minutes?", "share_denied": "You are not able to share this list, because you are not the owner. This list belongs to: ", "share_failure": "The sharing of the list failed", "share_list_with_cloudapp": "Share this list with other with CloudApp", "newsletter": "Sign up for the newsletter", "shared_successfully": "The list was shared successfully", "shared_not_changed": "No changes were made to the sharing options", "currently_shared_with": "Currently Shared with", "sharelist_info": "Add friends and colleagues with whom you want to share this list. Just add their email address to send an invitation to them. You can change the access to your list at any time on this screen. Have fun!", "sharelist_hint": "HINT", "sharelist_hint_text": "People without a Wunderlist account will get a normal invite mail. You will have to share the list again, when they signed up.", "sharelist_button": "Send invitation", "shared_delete_success": "The user was successfully deleted from the list", "shared_delete_all_success": "All users were successfully deleted from the list", "delete_shared_email": "Do you really want to delete the email from your shared list?", "share_own_email": "It's not possible to share this list with your own email address", "send": "Send", "share_this_list": "Share this list", "print_tasks": "Print tasks", "send_by_mail": "Send by email", "share_with_cloud": "Share with Cloud App", "copy_link": "COPY LINK", "saturday": "Saturday", "settings": "Settings", "delete_prompt_title": "Show confirmation prompt on deleting tasks or lists?", "delete_prompt_menu": "Delete prompt dialogs", "yes": "Yes", "no": "No", "hide_older_tasks": "Hide done tasks", "downloads": "Downloads", "aboutus": "About Us", "sharing_is_caring": "You are sharing: ", "show_note": "Show note", "delete_all_shared_lists": "Delete all", "sidebar_pos_menu": "Sidebar Position", "sidebar_pos_title": "On which position do you want your sidebar?", "sidebar_left": "Left", "sidebar_right": "Right", "hotkey_help_title": "Helpful hotkeys", "hotkey_help_task": "Add a new task", "hotkey_help_list": "Add a new list", "hotkey_help_search": "Start a new search", "hotkey_help_filters": "Step through the filters", "hotkey_help_updown_key": "Up\/Down", "hotkey_help_updown": "Step through your lists", "hotkey_help_help": "Open the hotkey help dialog", "hotkey_help_delete": "Delete selected list", "hotkey_help_inbox": "Open the inbox", "hotkey_help_sidebar": "Toggle the sidebar", "edit_task_dialog": "Edit task", "edit_list_dialog": "Edit list", "edit_ok": "Save changes", "edit_cancel": "Cancel changes", "change_password_dialog": "Change password", "manage_settings_dialog": "Manage settings" }, {});
};