//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// FolderList.js

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
    this.itemList = null;
    this.$element = null;
}

FolderButton.prototype.render = function (container) {
    this.$element = $('<div class="folder-button"></div>').attr('id', this.folder.ID).appendTo(container);
    this.$element.data('control', this);
    this.$element.click(function () { Control.get(this).select(); });
    this.$element.append('<span>' + this.folder.Name + '</span>');

    if (this.folder.ViewState.Select) {
        this.select();
    }
}

FolderButton.prototype.select = function () {
    var selected = this.$element.hasClass('selected');
    if (selected) {
        var selectedItems = this.$element.next('.folder-items').find('.selected');
        if (selectedItems.length == 0) {
            // no items are selected, deselect ALL folders
            this.deselectAll();
            this.collapseItems();
            this.parentControl.fireSelectionChanged();
        } else {
            // otherwise select folder itself
            this.deselectItems();
            this.parentControl.fireSelectionChanged(this.folder.ID);
        }
    } else {
        this.deselectAll();
        this.folder.ViewState.Select = true;
        this.$element.toggleClass('selected');
        this.parentControl.fireSelectionChanged(this.folder.ID);
        this.expand();
    }
}

FolderButton.prototype.deselectAll = function () {
    var folderButtons = this.parentControl.folderButtons;
    for (i in folderButtons) {
        folderButtons[i].$element.removeClass('selected');
        folderButtons[i].folder.ViewState.Select = false;
    }
}

FolderButton.prototype.deselectItems = function () {
    var selectedItems = this.$element.next('.folder-items').find('.selected');
    selectedItems.each(function () { Control.get(this).deselect(); });
}

FolderButton.prototype.expand = function () {
    this.collapseAllItems();
    this.expandItems();
}

FolderButton.prototype.expandItems = function () {
    var $container = $('<div class="folder-items"></div>').insertAfter(this.$element);
    var itemList = new ItemList(this, this.folder.GetItems());
    itemList.render('.folder-items');
}

FolderButton.prototype.collapseItems = function () {
    if (this.$element.next().hasClass('folder-items')) {
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

    if (this.item.ViewState.Select) {
        this.select();
    } else if (this.item.IsList) {
        var items = this.item.GetItems();
        for (var id in items) {
            if (items[id].ViewState.Select) {
                this.expandItems();
                Control.get('#' + id).select();
                break;
            }
        }
    }
}

ItemButton.prototype.select = function () {
    var folderButton = this.getFolderButton();
    if (folderButton != null) {
         if (this.item.IsList) {
            this.selectList(folderButton);
        } else {
            this.selectItem(folderButton);
        }
    } 
}

ItemButton.prototype.expandItems = function () {
    this.$element.addClass('expanded');
    var $container = $('<div class="list-items"></div>').insertAfter(this.$element);
    var itemList = new ItemList(this, this.item.GetItems());
    itemList.render('#' + this.item.ID + " + .list-items");
}

ItemButton.prototype.collapseItems = function () {
    if (this.$element != null && this.$element.next().hasClass('list-items')) {
        this.$element.removeClass('expanded');
        this.$element.next().remove();
    }
}

ItemButton.prototype.collapseAllItems = function () {
    for (var i in this.parentControl.itemButtons) {
        this.parentControl.itemButtons[i].collapseItems();
    }
}

ItemButton.prototype.selectList = function (folderButton) {
    // fire selection changed if folder is selected
    if (folderButton != null) {
        var selected = this.$element.hasClass('selected');
        var folderList = folderButton.parentControl;

        if (!selected) {
            this.collapseAllItems();
            this.expandItems();
            folderButton.deselectItems();
            this.$element.addClass('selected');
            this.item.ViewState.Select = true;
            folderList.fireSelectionChanged(this.item.FolderID, this.item.ID);
        }
    }
}

ItemButton.prototype.selectItem = function (folderButton) {
    // fire selection changed 
    if (folderButton != null) {
        var selected = this.$element.hasClass('selected');
        var folderList = folderButton.parentControl;
        var isFolderItem = (this.item.ParentID == null);
        if (!selected) {
            if (isFolderItem) { this.collapseAllItems(); }
            folderButton.deselectItems();
            this.$element.addClass('selected');
            this.item.ViewState.Select = true;
            folderList.fireSelectionChanged(this.item.FolderID, this.item.ID);
        }
    }
}

ItemButton.prototype.deselect = function () {
    this.item.ViewState.Select = false;
    this.$element.removeClass('selected');
}

// return parent FolderButton
ItemButton.prototype.getFolderButton = function () {
    return Control.findParent(this, 'folder');
}

