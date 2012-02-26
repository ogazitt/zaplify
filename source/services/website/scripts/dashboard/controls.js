//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// Controls.js

// ---------------------------------------------------------
// Control static object
// shared helpers used by controls
var Control = function Control$() { };

// get the control object associated with the id
Control.get = function Control$get(id) {
    return $('#' + id).data('control');
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
};

Dashboard.Init = function Dashboard$Init(dataModel) {
    this.dataModel = dataModel;

    // folders list
    this.folderList = new FolderList(this.dataModel.Folders);
    this.folderList.render(".dashboard-folders");
    this.folderList.onSelectionChanged = this.ManageFolder;

    // folder manager
    this.folderManager = new FolderManager();
    this.ManageFolder();

    // suggestions list

}

// event handler, do not reference this to access static Dashboard
Dashboard.ManageFolder = function Dashboard$ManageFolder(folderID) {
    if (folderID == null) {
        Dashboard.folderManager.currentFolder = null;
    } else {
        Dashboard.folderManager.currentFolder = Dashboard.dataModel.Folders[folderID];
    }
    Dashboard.folderManager.render(".dashboard-manager");
}

// ---------------------------------------------------------
// FolderList control
function FolderList(folders) {
    // fires notification when selected folder changes
    this.onSelectionChanged = null;

    this.folderButtons = [];
    for (var id in folders) {
        this.folderButtons = this.folderButtons.concat(new FolderButton(this, folders[id]));
    }
}

FolderList.prototype.fireSelectionChanged = function (folderID) {
    if (typeof (this.onSelectionChanged) == "function") {
        this.onSelectionChanged(folderID);
    }
}

FolderList.prototype.render = function (container) {
    $(container).empty();
    for (var i in this.folderButtons) {
        this.folderButtons[i].render(container);
    }
}

// ---------------------------------------------------------
// FolderButton control
function FolderButton(parentControl, folder) {
    this.parentControl = parentControl;
    this.folder = folder;
    this.$element = null;
}

FolderButton.prototype.render = function (container) {
    this.$element = $('<div></div>').attr('id', this.folder.ID).appendTo(container);
    this.$element.data('control', this);
    this.$element.addClass('folder-button');
    this.$element.click(function () { Control.get(this.id).expand(); });
    this.$element.append('<span>' + this.folder.Name + '</span>');
    var $folderSelector = $('<div class="folder-selector"></div>').appendTo(this.$element);
    $folderSelector.click(function () { Control.get(this.parentNode.id).select(); return false; });
}

FolderButton.prototype.select = function () {
    var selected = this.$element.hasClass('selected');
    $('.folder-button').each(function (i) { Control.get(this.id).deselect(); });
    if (!selected) {
        this.$element.toggleClass('selected');
        this.parentControl.fireSelectionChanged(this.folder.ID);
    } else {
        this.parentControl.fireSelectionChanged();
    }
}

FolderButton.prototype.deselect = function () {
    this.$element.removeClass('selected');
}

FolderButton.prototype.expand = function () {
    var expanded = this.$element.hasClass('expanded');
    $('.folder-button').each(function (i) { Control.get(this.id).collapse(); });
    if (!expanded) {
        this.$element.toggleClass('expanded');
        this.expandItems();
    }
}

FolderButton.prototype.collapse = function () {
    this.$element.removeClass('expanded');
    this.collapseItems();
}

FolderButton.prototype.expandItems = function () {
    var $container = $('<div></div>').insertAfter(this.$element);
    $container.addClass('folder-items');
    var itemList = new ItemList(this.folder.GetItems());
    itemList.render('.folder-items');
}

FolderButton.prototype.collapseItems = function () {
    $('.folder-items').remove();
}


// ---------------------------------------------------------
// ItemList control
function ItemList(items) {
    this.itemButtons = [];
    for (var id in items) {
        this.itemButtons = this.itemButtons.concat(new ItemButton(this, items[id]));
    }
}

ItemList.prototype.render = function (container) {
    $(container).empty();
    for (var i in this.itemButtons) {
        this.itemButtons[i].render(container);
    }
}

// ---------------------------------------------------------
// ItemButton control
function ItemButton(parentControl, item) {
    this.parentControl = parentControl;
    this.item = item;
    this.$element = null;
    this.indent = 'indent-none';
    // TODO: traverse parents
    if (item.ParentID != null) {
        this.indent = 'indent-1';
    }
}

ItemButton.prototype.render = function (container) {
    this.$element = $('<div></div>').attr('id', this.item.ID).appendTo(container);
    this.$element.data('control', this);
    if (this.item.IsList) {
        this.$element.addClass('item-list-button');
    } else {
        this.$element.addClass('item-button');
    }
    this.$element.addClass(this.indent);
    this.$element.click(function () { Control.get(this.id).expand(); });
    this.$element.append('<span>' + this.item.Name + '</span>');

    if (this.item.ViewState.Expand) {
        this.expand();
    }
}

ItemButton.prototype.expand = function () {
    if (this.$element.hasClass('item-list-button')) {
        if (this.$element.hasClass('expanded')) {
            this.collapseItems();
        } else {
            this.expandItems();
        }
        this.$element.toggleClass('expanded');
    } else {
        this.selectItem();
    }
}

ItemButton.prototype.expandItems = function () {
    this.item.ViewState.Expand = true;
    var $container = $('<div></div>').insertAfter(this.$element);
    $container.addClass('list-items');
    var itemList = new ItemList(this.item.GetItems());
    itemList.render('#' + this.item.ID + " + .list-items");
}

ItemButton.prototype.collapseItems = function () {
    this.item.ViewState.Expand = false;
    this.$element.next().remove();
}

ItemButton.prototype.selectItem = function () {
    var selected = this.$element.hasClass('selected');
    $('.item-button').removeClass('selected');
    if (!selected) {
        this.$element.toggleClass('selected');
    }
}

// ---------------------------------------------------------
// FolderManager control
function FolderManager(folders) {
    this.currentFolder = null;

    this.folderButtons = [];
    for (var id in folders) {
        this.folderButtons = this.folderButtons.concat(new FolderButton(this, folders[id]));
    }
}

FolderManager.prototype.render = function (container) {
    $container = $(container).empty();
    this.$element = $('<div></div>').appendTo($container);
    var x = (this.currentFolder != null) ? this.currentFolder.Name : "";
    $('<span>' + x + '</span>').appendTo(this.$element);
}
