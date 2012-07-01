//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// Dashboard.js

// ---------------------------------------------------------
// Dashboard static object - manages controls for dashboard
// assumes there are three panes marked by classes:
// dashboard-left, dashboard-center, dashboard-right

var Dashboard = function Dashboard$() {
    // data members used
    this.dataModel = null;
    this.folderList = null;
    this.helpManager = null;
    this.settingsManager = null;
    this.folderManager = null;
    this.suggestionList = null;
    this.suggestionManager = null;
}

// ---------------------------------------------------------
// public methods

Dashboard.Init = function Dashboard$Init(dataModel, renewFBToken, consentStatus) {
    Dashboard.dataModel = dataModel;

    Dashboard.checkBrowser();
    Dashboard.checkConsent(consentStatus);
    if (renewFBToken == true) {
        Service.GetFacebookConsent();
    }

    Dashboard.dataModel.AddDataChangedHandler('dashboard', Dashboard.ManageDataChange);

    // dashboard regions
    Dashboard.$element = $('.dashboard-region');
    Dashboard.$center = $('.dashboard-center');
    Dashboard.$left = $('.dashboard-left');
    Dashboard.$right = $('.dashboard-right');

    // folders list
    Dashboard.folderList = new FolderList(this.dataModel.Folders);
    Dashboard.folderList.addSelectionChangedHandler('dashboard', Dashboard.ManageFolder);

    // suggestions list
    Dashboard.suggestionList = new SuggestionList();
    Dashboard.suggestionList.addSelectionChangedHandler('dashboard', Dashboard.ManageChoice);

    // suggestions manager
    Dashboard.suggestionManager = new SuggestionManager(Dashboard.dataModel);
    // help and settings managers
    Dashboard.helpManager = new HelpManager(Dashboard, Dashboard.$center);
    Dashboard.settingsManager = new SettingsManager(Dashboard, Dashboard.$center);

    // folder manager
    Dashboard.folderManager = new FolderManager(Dashboard, Dashboard.$center);
    Dashboard.folderManager.addSelectionChangedHandler('dashboard', Dashboard.ManageFolder);
    if (Dashboard.dataModel.UserSettings.ViewState.SelectedFolder != null) {
        Dashboard.showManager(Dashboard.folderManager);
        Dashboard.dataModel.restoreSelection();
    } else {
        Dashboard.showManager(Dashboard.helpManager);
    }

    // bind events
    $(window).bind('load', Dashboard.resize);
    $(window).bind('resize', Dashboard.resize);
    $(window).bind('unload', Dashboard.Close);
    $logo = $('.brand a');
    $logo.unbind('click');
    $logo.click(function () { Dashboard.dataModel.Refresh(); return false; });

    // add options to header dropdown
    Dashboard.showHeaderOptions();
}

Dashboard.Close = function Dashboard$Close(event) {
    $('.header-icons').hide();
    Dashboard.dataModel.Close();
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageDataChange = function Dashboard$ManageDataChange(folderID, itemID) {
    Dashboard.ManageFolder(folderID, itemID);
    Dashboard.folderList.render(Dashboard.$left, Dashboard.dataModel.Folders);
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageFolder = function Dashboard$ManageFolder(folderID, itemID) {
    var item;
    var folder = (folderID != null) ? Dashboard.dataModel.Folders[folderID] : null;
    if (itemID == null) {
        Dashboard.dataModel.UserSettings.Selection(folderID, itemID);
        if (folder != null) {
            Dashboard.showManager(Dashboard.folderManager);
            Dashboard.folderManager.selectFolder(folder);
        } else {
            Dashboard.showManager(Dashboard.helpManager);
        }
    } else {
        item = (folder != null && itemID != null) ? folder.Items[itemID] : null;
        Dashboard.dataModel.UserSettings.Selection(folderID, itemID);
        Dashboard.showManager(Dashboard.folderManager);
        Dashboard.folderManager.selectItem(item);
    }

    if (!Dashboard.resizing) {
        Dashboard.getSuggestions(folder, item);     // get suggestions for currently selected user, folder, or item
    }
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageChoice = function Dashboard$ManageChoice(suggestion) {
    var refresh = Dashboard.suggestionManager.select(suggestion);
    if (refresh) {      // refresh more suggestions
        // check for more suggestions every 5 seconds for 20 seconds
        Dashboard.suggestionList.hideGroup(suggestion.GroupID);
        Dashboard.suggestionList.working(true);
        var nTries = 0;
        var checkPoint = new Date();

        var checkForSuggestions = function () {
            if (checkPoint > Dashboard.dataModel.SuggestionsRetrieved && nTries++ < 5) {
                Dashboard.getSuggestions(Dashboard.folderManager.currentFolder, Dashboard.folderManager.currentItem);
                setTimeout(checkForSuggestions, 5000);
            } else {
                Dashboard.suggestionList.working(false);
            }
        }
        checkForSuggestions();
    }
}

// ---------------------------------------------------------
// private methods

Dashboard.render = function Dashboard$render(folderID, itemID) {
    Dashboard.folderList.render(Dashboard.$left);
}

// show options in header dropdown (refresh, settings, etc.)
Dashboard.showHeaderOptions = function Dashboard$showHeaderOptions() {
    $dropdown = $('.navbar-fixed-top .pull-right .dropdown-menu');
    // refresh
    $menuitem = $dropdown.find('.option-refresh');
    $menuitem.show();
    $menuitem.click(function (e) {
        Dashboard.dataModel.Refresh();
        e.preventDefault();
    });
    // user settings
    $menuitem = $dropdown.find('.option-settings');
    $menuitem.show();
    $menuitem.click(function (e) {
        Dashboard.showManager(Dashboard.settingsManager);
        e.preventDefault();
    });
    // help
    $menuitem = $dropdown.find('.option-help');
    $menuitem.show();
    $menuitem.click(function (e) {
        Dashboard.showManager(Dashboard.helpManager);
        e.preventDefault();
    });
}

Dashboard.showManager = function Dashboard$showManager(manager, forceRender) {
    if (Dashboard.currentManager != manager) {
        Dashboard.currentManager = manager;
        Dashboard.folderManager.hide();
        Dashboard.settingsManager.hide();
        Dashboard.helpManager.hide();
        (manager.addWell == true) ? Dashboard.$center.addClass('well') : Dashboard.$center.removeClass('well');
    }
    // always show to force render if necessary
    manager.show(forceRender);
}

Dashboard.resize = function Dashboard$resize() {
    if (Dashboard.resizing) { return; }

    Dashboard.resizing = true;
    $(window).unbind('resize', Dashboard.resize);

    var winHeight = $(window).height();
    var headerHeight = $('.navbar-fixed-top').height();
    var footerHeight = $('.navbar-fixed-bottom').height();
    var dbHeight = winHeight - (headerHeight + footerHeight + 30);

    Dashboard.$left.height(dbHeight);
    Dashboard.$center.height(dbHeight);
    Dashboard.$right.height(dbHeight);

    Dashboard.showManager(Dashboard.currentManager, true);
    Dashboard.folderList.render(Dashboard.$left);
    //Dashboard.folderManager.render();

    $(window).bind('resize', Dashboard.resize);
    Dashboard.resizing = false;
}

Dashboard.checkBrowser = function Dashboard$checkBrowser() {
    if (Browser.IsMSIE() && Browser.MSIE_Version() < 9) {
        var header = "We're Sorry!";
        var message = "This application currently requires an HTML5-compatible browser. We encourage you to try using the application with the latest version of Internet Explorer, Chrome, or FireFox.<br/>Thank you";
        Control.alert(message, header);
    }
}

Dashboard.checkConsent = function Dashboard$checkConsent(consentStatus) {
    if (consentStatus != null && consentStatus.length > 0) {
        var message, header;
        switch (consentStatus) {
            case "FBConsentFail":
                header = "Failed!";
                message = "This application was not able to receive consent for accessing your Facebook information.";
                break;
            case "GoogleConsentFail":
                header = "Failed!";
                message = "This application was not able to receive consent for managing your Google calendar.";
                break;
            case "CloudADConsentFail":
                header = "Failed!";
                message = "This application was not able to receive consent for accessing your Cloud Directory.";
                break;
            default:
                {
                    Dashboard.consentStatus = consentStatus;
                    Dashboard.dataModel.GetSuggestions(Dashboard.completeConsent);
                }
        }
        if (message != null) {
            Control.alert(message, header, function () { Service.NavigateToDashboard(); });
        }
    }
}

Dashboard.completeConsent = function Dashboard$completeConsent(suggestions) {
    if (Dashboard.consentStatus != null && Dashboard.consentStatus.length > 0) {
        var message, header, suggestion;

        var findSuggestion = function (suggestions, type) {
            if (suggestions != null) {
                for (var g in suggestions) {
                    var childSuggestions = suggestions[g].Suggestions;
                    if (childSuggestions != null) {
                        for (var s in childSuggestions) {
                            if (childSuggestions[s].SuggestionType == type)
                                return childSuggestions[s];
                        }
                    }
                }
            }
            return null;
        }

        switch (Dashboard.consentStatus) {
            case "FBConsentSuccess":
                header = "Success!";
                message = "This application has successfully received consent for accessing your Facebook information.";
                suggestion = findSuggestion(suggestions, SuggestionTypes.GetFBConsent);
                break;
            case "GoogleConsentSuccess":
                header = "Success!";
                message = "This application has successfully received consent for managing your Google calendar.";
                suggestion = findSuggestion(suggestions, SuggestionTypes.GetGoogleConsent);
                break;
            case "CloudADConsentSuccess":
                header = "Success!";
                message = "This application has successfully received consent for accessing your Cloud Directory.";
                suggestion = findSuggestion(suggestions, SuggestionTypes.GetADConsent);
                break;
        }
        Dashboard.consentStatus = null;
        if (suggestion != null && suggestion.ReasonSelected != Reasons.Chosen) {
            Dashboard.dataModel.SelectSuggestion(suggestion, Reasons.Chosen);
            Control.alert(message, header, function () { Service.NavigateToDashboard(); });
        }
    }
}

Dashboard.getSuggestions = function Dashboard$getSuggestions(folder, item) {
    if (item != null) {
        if (item.hasOwnProperty('Created')) {
            this.dataModel.GetSuggestions(Dashboard.renderSuggestions, item);
        } else {    // clear existing suggestions
            Dashboard.renderSuggestions({});
        }
    } else if (folder != null) {
        if (folder.hasOwnProperty('Items')) {
            Dashboard.dataModel.GetSuggestions(Dashboard.renderSuggestions, folder);
        } else {    // clear existing suggestions
            Dashboard.renderSuggestions({});
        }
    } else {
        return Dashboard.dataModel.GetSuggestions(Dashboard.renderSuggestions);
    }   
}

Dashboard.renderSuggestions = function Dashboard$renderSuggestions(suggestions) {
    // process RefreshEntity suggestions
    var group = suggestions[SuggestionTypes.RefreshEntity];
    if (group != null) {
        // full user data refresh, select last item
        var itemID;
        for (var id in group.Suggestions) {
            var suggestion = group.Suggestions[id];
            Dashboard.dataModel.SelectSuggestion(suggestion, Reasons.Ignore);
            if (suggestion.EntityType == 'Item') {
                itemID = suggestion.EntityID;
            }
        }
        delete suggestions[SuggestionTypes.RefreshEntity];
        Dashboard.dataModel.Refresh(itemID);
    }

    Dashboard.suggestionList.render(Dashboard.$right, suggestions);
    if (suggestions['Group_0'] != null) {
        Dashboard.suggestionList.working(false);
    }
}