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
};

Dashboard.Init = function Dashboard$Init(dataModel) {
    this.dataModel = dataModel;
    this.dataModel.AddDataChangedHandler('dashboard', Dashboard.Refresh);

    // folders list
    this.folderList = new FolderList(this.dataModel.Folders);
    this.folderList.render(".dashboard-folders");
    this.folderList.addSelectionChangedHandler('dashboard', this.ManageFolder);

    // folder manager
    this.folderManager = new FolderManager();
    this.folderManager.currentFolder = 0;
    this.ManageFolder();

    // suggestions list

}

Dashboard.Refresh = function Dashboard$Refresh(type, id) {
    Dashboard.folderList.render(".dashboard-folders");
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
// FolderList control
function FolderList(folders) {
    // fires notification when selected folder changes
    this.onSelectionChangedHandlers = {};

    this.folderButtons = [];
    for (var id in folders) {
        this.folderButtons = this.folderButtons.concat(new FolderButton(this, folders[id]));
    }
}

FolderList.prototype.addSelectionChangedHandler = function (name, handler) {
    this.onSelectionChangedHandlers[name] = handler;
}

FolderList.prototype.removeSelectionChangedHandler = function (name) {
    this.onSelectionChangedHandlers[name] = undefined;
}

FolderList.prototype.fireSelectionChanged = function (folderID, itemID) {
    for (var name in this.onSelectionChangedHandlers) {
        var handler = this.onSelectionChangedHandlers[name];
        if (typeof (handler) == "function") {
            handler(folderID, itemID);
        }
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
    this.$element = $('<div class="folder-button"></div>').attr('id', this.folder.ID).appendTo(container);
    this.$element.data('control', this);
    this.$element.click(function () { Control.get(this).expand(); });
    this.$element.append('<span>' + this.folder.Name + '</span>');

    var $folderSelector = $('<div class="folder-selector"></div>').appendTo(this.$element);
    $folderSelector.attr('title', 'Select');
    $folderSelector.click(function () { Control.get(this.parentNode).select(); return false; });

    if (this.folder.ViewState.Select) { this.select(); }
    if (this.folder.ViewState.Expand) { this.expand(); }
}

FolderButton.prototype.select = function () {
    var selected = this.$element.hasClass('selected');
    $('.folder-button').each(function (i) { Control.get(this).deselect(); });
    if (!selected) {
        this.folder.ViewState.Select = true;
        this.$element.toggleClass('selected');
        this.parentControl.fireSelectionChanged(this.folder.ID);
    } else {
        this.parentControl.fireSelectionChanged();
    }
}

FolderButton.prototype.deselect = function () {
    this.folder.ViewState.Select = false;
    this.$element.removeClass('selected');
}

FolderButton.prototype.expand = function () {
    var expanded = this.$element.hasClass('expanded');
    //$('.folder-button').each(function (i) { Control.get(this).collapseItems(); });
    this.collapseAllItems();
    if (!expanded) {
        this.$element.addClass('expanded');
        this.expandItems();
    }
}

FolderButton.prototype.expandItems = function () {
    this.folder.ViewState.Expand = true;
    var $container = $('<div class="folder-items"></div>').insertAfter(this.$element);
    var itemList = new ItemList(this, this.folder.GetItems());
    itemList.render('.folder-items');
}

FolderButton.prototype.collapseItems = function () {
    if (this.$element.hasClass('expanded')) {
        this.folder.ViewState.Expand = false;
        this.$element.removeClass('expanded');
        this.$element.next().remove();
    }
}

FolderButton.prototype.collapseAllItems = function () {
    for (var i in this.parentControl.folderButtons) {
        this.parentControl.folderButtons[i].collapseItems();
    }
}

// ---------------------------------------------------------
// ItemList control
function ItemList(parentControl, items) {
    this.parentControl = parentControl;
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
    this.$element.click(function () { Control.get(this).select(); });
    this.$element.append('<span>' + this.item.Name + '</span>');

    if (this.item.ViewState.Expand) {
        this.select();
    }
}

ItemButton.prototype.select = function () {
    var folderButton = this.inSelectMode();
    if (folderButton != null) {
        // parent folder is selected
        if (this.item.IsList) {
            this.selectList(folderButton);
        } else {
            this.selectItem(folderButton);
        }
    } else if (this.item.IsList) { 
        // expand or collapse list items
        if (this.$element.hasClass('expanded')) {
            this.collapseItems();
        } else {
            this.expandItems();
        }
    }
}

ItemButton.prototype.expandItems = function () {
    this.item.ViewState.Expand = true;
    this.$element.addClass('expanded');
    var $container = $('<div class="list-items"></div>').insertAfter(this.$element);
    var itemList = new ItemList(this, this.item.GetItems());
    itemList.render('#' + this.item.ID + " + .list-items");
}

ItemButton.prototype.collapseItems = function () {
    if (this.$element.hasClass('expanded')) {
        this.item.ViewState.Expand = false;
        this.$element.removeClass('expanded');
        this.$element.next().remove();
    }
}

ItemButton.prototype.collapseAllItems = function () {
    for (var i in this.parentControl.buttonItems) {
        this.parentControl.buttonItems[i].collapseItems();
    }
}

ItemButton.prototype.selectList = function (folderButton) {
    // fire selection changed if folder is selected
    if (folderButton != null) {
        var expanded = this.$element.hasClass('expanded');
        var selected = this.$element.hasClass('selected');
        var folderList = folderButton.parentControl;

        if (selected && expanded) {
            this.collapseItems();
        }
        if (selected && !expanded) {
            this.expandItems();
        }
        if (!selected && expanded) {
            this.deselectAllItems();
            this.$element.addClass('selected');
            folderList.fireSelectionChanged(this.item.FolderID, this.item.ID);
        }
        if (!selected && !expanded) {
            this.expandItems();
            this.deselectAllItems();
            this.$element.addClass('selected');
            folderList.fireSelectionChanged(this.item.FolderID, this.item.ID);
        }
    }
}

ItemButton.prototype.selectItem = function (folderButton) {
    // fire selection changed if folder is selected
    if (folderButton != null) {    
        var selected = this.$element.hasClass('selected');
        var folderList = folderButton.parentControl;
        this.deselectAllItems();
        if (selected) {
            folderList.fireSelectionChanged(this.item.FolderID, this.item.ParentID);
        } else {
            this.$element.addClass('selected');
            folderList.fireSelectionChanged(this.item.FolderID, this.item.ID);
        }        
    }
}

ItemButton.prototype.deselectAllItems = function () {
    // TODO: find way to deselect scoped to this folderButton
    $('.item-button').removeClass('selected');
    $('.item-list-button').removeClass('selected');
}

// return parent FolderButton if folder is currently selected
ItemButton.prototype.inSelectMode = function () {
    var folderButton = Control.findParent(this, 'folder');
    if (folderButton != null && folderButton.folder.ViewState.Select) {
        return folderButton;
    }
    return null;
}

// ---------------------------------------------------------
// FolderManager control
function FolderManager() {
    this.currentFolder = null;
    this.currentItem = null;

    // elements
    this.$toolbar = null;
    this.$managerActive = null;
    this.$itemPath = null;
    this.$managerHelp = null;
}

FolderManager.prototype.render = function (container) {
    $container = $(container).empty();
    // toolbar
    this.$toolbar = $('<div class="manager-toolbar"></div>').appendTo($container);
    $refreshButton = $('<div class="toolbar-button"><div class="refresh"></div></div>').appendTo(this.$toolbar);
    $refreshButton.attr('title', 'Refresh');
    $refreshButton.click(function () { DataModel.Refresh(); });

    // manager active
    this.$managerActive = $('<div class="manager-active"></div>').appendTo($container);
    this.$itemPath = $('<div class="item-path"></div>').appendTo(this.$managerActive);

    //TEMPORARY
    $temp = $('<div><input id="txtName" type="text" class="input_item-name" /></div>').appendTo(this.$managerActive);

    $btn = $('<input id="addItem" type="button" class="dialog-button" value="Add Item" />').appendTo($temp);
    $btn.data('control', this);
    $btn.click(function () { Control.get(this).addItem(false); });

    $btn = $('<input id="addList" type="button" class="dialog-button" value="Add List" />').appendTo($temp);
    $btn.data('control', this);
    $btn.click(function () { Control.get(this).addItem(true); });
    if (this.currentItem != null) { $btn.hide(); }

    $btn = $('<input id="updateItem" type="button" class="dialog-button" value="Update" />').appendTo($temp);
    $btn.data('control', this);
    $btn.click(function () { Control.get(this).updateItem(); });
    $btn.hide();

    $btn = $('<input id="deleteItem" type="button" class="dialog-button" value="Delete" />').appendTo($temp);
    $btn.data('control', this);
    $btn.click(function () { Control.get(this).deleteItem(); });
    $btn.hide();

    // manager help
    this.$managerHelp = $('<div class="manager-help"><h1>Welcome to Zaplify</h1></div>').appendTo($container);
}

FolderManager.prototype.addItem = function (isList) {
    var value = $('#txtName').val();
    if (this.currentItem != null) {
        this.currentItem.InsertItem({ Name: value, IsList: isList });
    } else {
        this.currentFolder.InsertItem({ Name: value, IsList: isList });
    }
}

FolderManager.prototype.updateItem = function () {
    var value = $('#txtName').val();
    if (this.currentItem != null) {
        var updatedItem = $.extend({}, this.currentItem);
        updatedItem.Name = value;
        this.currentItem.Update(updatedItem);
    }
}

FolderManager.prototype.deleteItem = function () {
    if (this.currentItem != null) {
        // WARN USER when deleting a List
        this.currentItem.Delete();
    }
}

FolderManager.prototype.selectFolder = function (folder) {
    this.currentFolder = folder;
    if (this.currentFolder != null) {
        this.$itemPath.empty();
        this.$itemPath.append('<span>' + folder.Name + '</span>');
        this.$managerHelp.hide();
        this.$managerActive.show();
    } else {
        this.$managerHelp.show();
        this.$managerActive.hide();
    }
}

// TEMPORARY
FolderManager.prototype.selectItem = function (item) {
    this.currentItem = item;
    if (this.currentItem != null) {
        if (this.currentItem.IsList) {
            $('#txtName').val('');
            $('#addItem').show();
            $('#addList').hide();
            $('#updateItem').show();
            $('#deleteItem').show();
            this.$itemPath.empty();
            this.$itemPath.append('<span>' + this.currentFolder.Name + '</span>');
            this.$itemPath.append('<span>.' + item.Name + '</span>');
        } else {
            $('#txtName').val(item.Name);
            $('#addItem').hide();
            $('#addList').hide();
            $('#updateItem').show();
            $('#deleteItem').show();

            if (this.currentItem.ParentID == null) {
                this.selectFolder(this.currentFolder);
            } else {
                this.$itemPath.empty();
                this.$itemPath.append('<span>' + this.currentFolder.Name + '</span>');
                this.$itemPath.append('<span>.' + item.GetParent().Name + '</span>');
            }
        }
    } else {
        $('#txtName').val('');
        $('#addItem').show();
        $('#addList').show();
        $('#updateItem').hide();
        $('#deleteItem').hide();
        this.selectFolder(this.currentFolder);
    }
}

