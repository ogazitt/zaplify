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
}

// ---------------------------------------------------------
// public methods

Dashboard.Init = function Dashboard$Init(dataModel) {
    this.dataModel = dataModel;
    this.dataModel.AddDataChangedHandler('dashboard', Dashboard.render);

    // folders list
    this.folderList = new FolderList(this.dataModel.Folders);
    this.folderList.render(".dashboard-folders");
    this.folderList.addSelectionChangedHandler('dashboard', this.ManageFolder);

    // folder manager
    this.folderManager = new FolderManager(this.dataModel);
    this.folderManager.currentFolder = 0;
    this.ManageFolder();

    // suggestions list

    // bind events
    $(window).bind('load', Dashboard.resize);
    $(window).bind('resize', Dashboard.resize);
}

// event handler, do not reference this to access static Dashboard
Dashboard.ManageFolder = function Dashboard$ManageFolder(folderID, itemID) {
    var folder = (folderID != null) ? Dashboard.dataModel.Folders[folderID] : null;
    var currentFolder = Dashboard.folderManager.currentFolder;
    if (folder != currentFolder) {
        Dashboard.folderManager.render(".dashboard-manager");
        Dashboard.folderManager.selectFolder(folder);
    }

    var item = (folder != null && itemID != null) ? folder.Items[itemID] : null;
    var currentItem = Dashboard.folderManager.currentItem;
    if (item != currentItem) {
        Dashboard.folderManager.selectItem(item);
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
