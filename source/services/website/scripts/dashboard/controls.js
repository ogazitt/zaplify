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

    // folder manager
    Dashboard.folderManager = new FolderManager(this.dataModel);
    Dashboard.ManageFolder();

    // suggestions list
    Dashboard.suggestionList = new SuggestionList();
    Dashboard.suggestionList.addSelectionChangedHandler('dashboard', this.ManageChoice);

    // bind events
    $(window).bind('load', Dashboard.resize);
    $(window).bind('resize', Dashboard.resize);
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageFolder = function Dashboard$ManageFolder(folderID, itemID) {
    var folder = (folderID != null) ? Dashboard.dataModel.Folders[folderID] : null;
    var currentFolder = Dashboard.folderManager.currentFolder;
    if (folder == null || folder != currentFolder) {
        Dashboard.folderManager.render(".dashboard-manager");
        Dashboard.folderManager.selectFolder(folder);
    }

    var item = (folder != null && itemID != null) ? folder.Items[itemID] : null;
    var currentItem = Dashboard.folderManager.currentItem;
    if (item == null || item != currentItem) {
        Dashboard.folderManager.selectItem(item);
    }

    // get suggestions for currently selected user, folder, or item
    Dashboard.getSuggestions(Dashboard.folderManager.currentFolder, Dashboard.folderManager.currentItem);
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageChoice = function Dashboard$ManageChoice(suggestion) {
    if (suggestion.FieldName == FieldNames.FacebookConsent) {
        var msg = 'You may be directed to Facebook to give consent.\r Do you want to continue?';
        if (confirm(msg)) {
            Service.GetFacebookConsent();
        }
    }

    if (suggestion.FieldName == FieldNames.CloudADConsent) {
        var msg = 'You may be directed the Cloud Directory to give consent.\r Do you want to continue?';
        if (confirm(msg)) {
            alert('Not yet implemented!');
            //Service.GetCloudADConsent();
        }
    }
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
    // TODO: get suggestions for user, folder, or item
    if (item != null) {
        if (!item.IsList) {
            // TODO: should we get suggestions for Lists?
            this.dataModel.GetSuggestions(Dashboard.renderSuggestions, item.ID);
        }
    } else if (folder != null) {
        this.dataModel.GetSuggestions(Dashboard.renderSuggestions, folder.ID);
    } else {
        this.dataModel.GetSuggestions(Dashboard.renderSuggestions);
    }
}

Dashboard.renderSuggestions = function Dashboard$renderSuggestions(suggestions) {
    Dashboard.suggestionList.render('.dashboard-suggestions', suggestions);
}