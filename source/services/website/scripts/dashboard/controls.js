//----------------------------------------------------------
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
    var selectionChanged = false;
    var currentFolderID = (Dashboard.folderManager.currentFolder != null) ? Dashboard.folderManager.currentFolder.ID : null;
    var folder = (folderID != null) ? Dashboard.dataModel.Folders[folderID] : null;
    if (folderID == null || folderID != currentFolderID) {
        Dashboard.folderManager.render(".dashboard-manager");
        Dashboard.folderManager.selectFolder(folder);
        selectionChanged = true;
    }

    var currentItemID = (Dashboard.folderManager.currentItem != null) ? Dashboard.folderManager.currentItem.ID : null;
    if (itemID == null || itemID != currentItemID) {
        var item = (folder != null && itemID != null) ? folder.Items[itemID] : null;
        Dashboard.folderManager.selectItem(item);
        selectionChanged = true;
    }

    if (!Dashboard.resizing /*&& selectionChanged*/) {
        // get suggestions for currently selected user, folder, or item
        Dashboard.getSuggestions(Dashboard.folderManager.currentFolder, Dashboard.folderManager.currentItem);
    }
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageChoice = function Dashboard$ManageChoice(suggestion) {
    Dashboard.suggestionManager.select(suggestion);
    // refresh suggestions immediately and in 15 seconds
    Dashboard.getSuggestions(Dashboard.folderManager.currentFolder, Dashboard.folderManager.currentItem);
    setTimeout(function () {
        Dashboard.getSuggestions(Dashboard.folderManager.currentFolder, Dashboard.folderManager.currentItem);
        }, 15000);
}

// ---------------------------------------------------------
// private methods

Dashboard.render = function Dashboard$render(folderID, itemID) {
    Dashboard.folderList.render(".dashboard-folders");
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
}