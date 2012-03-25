﻿//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// Controls.js

// ---------------------------------------------------------
// Control static object
// shared helpers used by controls
var Control = function Control$() { };

// get the control object associated with the id
Control.get = function Control$get(element) {
    return $(element).data('control');
}

// get first parent control that contains member
Control.findParent = function Control$findParent(control, member) {
    while (control.parentControl != null) {
        control = control.parentControl;
        if (control[member] != null) {
            return control; 
        }
    }
    return null;
}

// dynamic ellipsis
Control.ellipsis = function Control$ellipsis(element, height) {
    while ($(element).outerHeight() > height) {
        $(element).text(function (index, text) {
            return text.replace(/\W*\s(\S)*$/, '...');
        });
    }
}

// ---------------------------------------------------------
// Dashboard static object - manages controls for dashboard
// assumes there are three panes marked by classes:
// dashboard-folders, dashboard-manager, dashboard-suggestions

var Dashboard = function Dashboard$() {
    // data members used
    this.dataModel = null;
    this.folderList = null;
    this.folderManager = null;
    this.suggestionList = null;
    this.suggestionManager = null;
}

// ---------------------------------------------------------
// public methods

Dashboard.Init = function Dashboard$Init(dataModel) {
    Dashboard.dataModel = dataModel;
    Dashboard.dataModel.AddDataChangedHandler('dashboard', Dashboard.render);

    // folders list
    Dashboard.folderList = new FolderList(this.dataModel.Folders);
    Dashboard.folderList.render('.dashboard-folders');
    Dashboard.folderList.addSelectionChangedHandler('dashboard', this.ManageFolder);

    // suggestions list
    Dashboard.suggestionList = new SuggestionList();
    Dashboard.suggestionList.addSelectionChangedHandler('dashboard', this.ManageChoice);

    // suggestions manager
    Dashboard.suggestionManager = new SuggestionManager(this.dataModel);

    // folder manager
    Dashboard.folderManager = new FolderManager(this.dataModel);
    Dashboard.ManageFolder();


    // bind events
    $(window).bind('load', Dashboard.resize);
    $(window).bind('resize', Dashboard.resize);
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageFolder = function Dashboard$ManageFolder(folderID, itemID) {
    var item;
    var folder = (folderID != null) ? Dashboard.dataModel.Folders[folderID] : null;
    if (itemID == null) {
        Dashboard.folderManager.render('.dashboard-manager');
        Dashboard.folderManager.selectFolder(folder);
    } else {
        item = (folder != null && itemID != null) ? folder.Items[itemID] : null;
        Dashboard.folderManager.selectItem(item);
    }
    if (!Dashboard.resizing) {
        // get suggestions for currently selected user, folder, or item
        Dashboard.getSuggestions(folder, item);
    }
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageChoice = function Dashboard$ManageChoice(suggestion) {
    var refresh = Dashboard.suggestionManager.select(suggestion);
    if (refresh) {      // refresh more suggestions
        // check for more suggestions every 5 seconds for 20 seconds
        $('.working').show();
        var nTries = 0;
        var checkPoint = new Date();

        var checkForSuggestions = function () {
            if (checkPoint > Dashboard.dataModel.SuggestionsRetrieved && nTries++ < 5) {
                Dashboard.getSuggestions(Dashboard.folderManager.currentFolder, Dashboard.folderManager.currentItem);
                setTimeout(checkForSuggestions, 5000);
            } else {
                $('.working').hide();
            }
        }
        checkForSuggestions();
    }
}

// ---------------------------------------------------------
// private methods

Dashboard.render = function Dashboard$render(folderID, itemID) {
    Dashboard.folderList.render(".dashboard-folders");
    Dashboard.ManageFolder(folderID, itemID);
}

Dashboard.resize = function Dashboard$resize() {
    if (Dashboard.resizing) { return; }

    Dashboard.resizing = true;
    $(window).unbind('resize', Dashboard.resize);

    var winHeight = $(window).height();
    var headerHeight = $('.header-region').height();
    var footerHeight = $('.footer-region').height();
    var dbHeight = winHeight - (headerHeight + footerHeight + 40);

    var $db = $('.dashboard-region');
    var $dbm = $('.dashboard-manager');
    var $dbf = $('.dashboard-folders');
    var $dbs = $('.dashboard-suggestions');
    var dbOuterHeight = $db.outerHeight();
    var dbWidth = $db.width();
    var dbfWidth = $dbf.width();
    var dbsWidth = $dbs.width();
    var dbmMargins = 26;

    $dbm.width(dbWidth - (dbfWidth + dbsWidth + dbmMargins));
    $dbf.height(dbHeight);
    $dbs.height(dbHeight);

    Dashboard.render();

    $(window).bind('resize', Dashboard.resize);
    Dashboard.resizing = false;
}

Dashboard.getSuggestions = function Dashboard$getSuggestions(folder, item) {
    if (item != null) {
        this.dataModel.GetSuggestions(Dashboard.renderSuggestions, item);
    } else if (folder != null) {
        this.dataModel.GetSuggestions(Dashboard.renderSuggestions, folder);
    } else {
        this.dataModel.GetSuggestions(Dashboard.renderSuggestions);
    }
}

Dashboard.renderSuggestions = function Dashboard$renderSuggestions(suggestions) {
    // process RefreshEntity suggestions
    var group = suggestions[FieldNames.RefreshEntity];
    if (group != null) {
        for (var id in group.Suggestions) {
            var suggestion = group.Suggestions[id];
            var item = Dashboard.dataModel.FindItem(suggestion.EntityID);
            if (item != null && !item.IsFolder()) {
                item.Refresh();
            }
            Dashboard.dataModel.SelectSuggestion(suggestion, Reasons.Ignore);
        }
        delete suggestions[FieldNames.RefreshEntity];
    }

    Dashboard.suggestionList.render('.dashboard-suggestions', suggestions);
    if (suggestions['Group_0'] != null) {
        $('.working').hide(); 
    }
}