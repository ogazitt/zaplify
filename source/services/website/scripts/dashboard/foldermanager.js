//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// FolderManager.js

// ---------------------------------------------------------
// FolderManager control
function FolderManager(dataModel) {
    this.dataModel = dataModel;
    this.currentFolder = null;
    this.currentItem = null;

    // keep this in-sync with div generated in render
    // used by children to delay-render on first show
    this.container = '.manager-region';
    this.$managerRegion = null;

    this.toolbar = new Toolbar(this);
    this.listEditor = new ListEditor(this);
    this.managerHelp = new FolderManagerHelp(this);
}

FolderManager.prototype.render = function (container) {
    if (this.$managerRegion == null) {
        $(container).empty();

        // toolbar
        $('<div class="manager-toolbar"></div>').appendTo($(container));
        this.toolbar.render('.manager-toolbar');
        this.toolbar.bind(Toolbar.Refresh, 'click', this.dataModel.Refresh);
        this.toolbar.bind(Toolbar.AddFolder, 'click', function (handlerObj) { handlerObj.addFolder(); }, this.listEditor);
        this.toolbar.bind(Toolbar.AddList, 'click', function (handlerObj) { handlerObj.addItem(true); }, this.listEditor);
        this.toolbar.bind(Toolbar.AddItem, 'click', function (handlerObj) { handlerObj.addItem(false); }, this.listEditor);
        this.toolbar.bind(Toolbar.UpdateItem, 'click', function (handlerObj) { handlerObj.updateItem(); }, this.listEditor);
        this.toolbar.bind(Toolbar.DeleteItem, 'click', function (handlerObj) { handlerObj.deleteItem(); }, this.listEditor);

        // manager region
        this.$managerRegion = $('<div class="manager-region"></div>').appendTo($(container));

        // list editor
        this.listEditor.render(this.container);
    }
}

FolderManager.prototype.selectFolder = function (folder) {
    this.currentFolder = folder;
    if (this.currentFolder != null) {
        if (this.currentFolder.IsDefault) {
            this.toolbar.disableTools([Toolbar.UpdateItem, Toolbar.DeleteItem]);
        } else {
            this.toolbar.disableTools([Toolbar.UpdateItem]);
        }
        this.managerHelp.hide();
        this.listEditor.render(this.container);
        //this.listEditor.inputValue('');
        this.listEditor.show();
    } else {
        this.toolbar.disableTools([Toolbar.AddFolder, Toolbar.AddList, Toolbar.AddItem, Toolbar.UpdateItem, Toolbar.DeleteItem]);
        this.listEditor.hide();
        this.managerHelp.show();
    }
}

FolderManager.prototype.selectItem = function (item) {
    this.currentItem = item;
    if (this.currentItem != null) {
        if (this.currentItem.IsList) {
            this.toolbar.disableTools([Toolbar.AddFolder, Toolbar.AddList]);
            this.listEditor.render(this.container);
            //this.listEditor.inputValue('');
        } else {
            this.toolbar.disableTools([Toolbar.AddFolder, Toolbar.AddList, Toolbar.AddItem]);
            this.listEditor.render(this.container);
            //this.listEditor.inputValue(this.currentItem.Name);
        }
    } else {
        this.selectFolder(this.currentFolder);
    }
}

// ---------------------------------------------------------
// FolderManagerHelp control
function FolderManagerHelp(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
}

FolderManagerHelp.prototype.render = function (container) {
    this.$element = $('<div class="manager-help"><h1>Welcome to Zaplify!</h1></div>').appendTo($(container));
}

FolderManagerHelp.prototype.hide = function () {
    this.$element.hide();
}

FolderManagerHelp.prototype.show = function () {
    if (this.$element == null) {
        this.render(this.parentControl.container);
    }
    this.$element.show();
}

// ---------------------------------------------------------
// Toolbar control
function Toolbar(parentControl) {
    this.parentControl = parentControl;

    // elements
    this.toolButtons = {};
    this.$element = null;

    // create button for each tool in table
    for (name in Toolbar.Tools) {
        this.toolButtons[name] = new ToolButton(this, Toolbar.Tools[name]);
    }
}

// use table to define tools
Toolbar.Refresh = 'refresh';
Toolbar.AddFolder = 'addFolder';
Toolbar.AddList = 'addList';
Toolbar.AddItem = 'addItem';
Toolbar.UpdateItem = 'updateItem';
Toolbar.DeleteItem = 'deleteItem';
Toolbar.Tools = {};
Toolbar.Tools[Toolbar.Refresh] = { css: 'refresh', toolTip: "Refresh", separator: true };
Toolbar.Tools[Toolbar.AddFolder] = { css: 'add-folder', toolTip: "Add Folder", disabled: true };
Toolbar.Tools[Toolbar.AddList] = { css: 'add-list', toolTip: "Add List", disabled: true };
Toolbar.Tools[Toolbar.AddItem] = { css: 'add-item', toolTip: "Add Item", separator: true, disabled: true };
Toolbar.Tools[Toolbar.UpdateItem] = { css: 'update-item', toolTip: "Update", disabled: true };
Toolbar.Tools[Toolbar.DeleteItem] = { css: 'delete-item', toolTip: "Delete", disabled: true };

Toolbar.prototype.render = function (container) {
    $container = $(container).empty();

    for (tool in this.toolButtons) {
        this.toolButtons[tool].render(container)
    }
}

Toolbar.prototype.bind = function (tool, event, handler, handlerControl) {
    var toolButton = this.toolButtons[tool];
    if (toolButton != null) {
        // pass handlerControl as parameter to handler
        toolButton.$element.bind(event, function () { if (!$(this).hasClass('disabled')) { handler(handlerControl) } });
        return true;
    }
    return false;
}

Toolbar.prototype.disableTools = function (tools) {
    for (var name in this.toolButtons) {
        this.toolButtons[name].enable();
    }
    for (i in tools) {
        var name = tools[i];
        this.toolButtons[name].disable();
    }
}

Toolbar.prototype.hide = function () {
    this.$element.hide();
}

Toolbar.prototype.show = function () {
    this.$element.show();
}

// ---------------------------------------------------------
// ToolbarButton control
function ToolButton(parentControl, tool) {
    this.parentControl = parentControl;
    this.tool = tool;

    // elements
    this.$element = null;
}

ToolButton.prototype.render = function (container) {
    this.$element = $('<div class="toolbar-button"></div>').appendTo($(container));
    this.$element.attr('title', this.tool.toolTip);
    $('<div class="' + this.tool.css + '"></div>').appendTo(this.$element);

    if (this.tool.separator) {
        this.$element.css('padding-right', '5px');
        this.$element.css('margin-right', '10px');
        this.$element.css('border-right', '1px solid #C4BD97');
    }

    if (this.tool.disabled) {
        this.$element.addClass('disabled');
    }
}

ToolButton.prototype.disable = function () {
    this.$element.addClass('disabled');
}

ToolButton.prototype.enable = function () {
    this.$element.removeClass('disabled');
}

ToolButton.prototype.hide = function () {
    this.$element.hide();
}

ToolButton.prototype.show = function () {
    this.$element.show();
}

// ---------------------------------------------------------
// ListEditor control
function ListEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;

    this.itemPath = new ItemPath(this);
    this.itemEditor = new ItemEditor(this);
}

ListEditor.prototype.render = function (container) {
    var folder = this.parentControl.currentFolder;
    var item = this.parentControl.currentItem;
    if (this.$element == null) {
        this.$element = $('<div class="list-editor"></div>').appendTo(container);
    }

    this.itemPath.render(this.$element, folder, item);
    this.itemEditor.render(this.$element, folder, item);
}

ListEditor.prototype.hide = function () {
    this.$element.hide();
}

ListEditor.prototype.show = function () {
    this.$element.show();
}

ListEditor.prototype.editItem = function () {
    // TODO: track active editor
    return this.itemEditor.editItem();
}

ListEditor.prototype.handleEnterKey = function (event) {
    var item = this.parentControl.currentItem;
    if (event.which == 13) {
        if (item == null || item.IsList) {
            this.addItem(false);
        } else {
            this.updateItem();
        }
    }
}

ListEditor.prototype.addFolder = function () {
    var newFolder = this.editItem();

    // TODO: let user define ItemType (temporary for test purposes)
    var dataModel = this.parentControl.dataModel;
    newFolder.ItemTypeID = dataModel.FoldersMap.array[1].ItemTypeID;
    dataModel.InsertFolder(newFolder);
}

ListEditor.prototype.addItem = function (isList) {
    var folder = this.parentControl.currentFolder;
    var item = this.parentControl.currentItem;
    var newItem = this.editItem();
    newItem.IsList = isList;

    if (item != null) {
        item.InsertItem(newItem);
    } else if (folder != null) {
        folder.InsertItem(newItem);
    }
}

ListEditor.prototype.updateItem = function () {
    var item = this.parentControl.currentItem;
    var updatedItem = this.editItem();
    if (item != null) {
        item.Update(updatedItem);
    }
}

ListEditor.prototype.deleteItem = function () {
    var item = this.parentControl.currentItem;
    if (item != null) {
        // WARN USER when deleting a List
        if (item.IsList && !confirm('Are you sure?\n\nThis will delete the list and all items contained within!')) {
            return;
        }
        item.Delete();
    } else {
        if (this.parentControl.currentFolder.IsDefault) {
            alert('This is a default folder and cannot be deleted.');
        } else if (confirm('Are you sure?\n\nThis will delete the folder and all items contained within!')) {
            // WARN USER when deleting a Folder
            this.parentControl.currentFolder.Delete();
        }
    }
}

// ---------------------------------------------------------
// ItemPath control
function ItemPath(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
}

ItemPath.prototype.render = function (container, folder, item) {
    if (this.$element == null) {
        this.$element = $('<div class="item-path"></div>').appendTo(container);
    }

    this.$element.empty();
    if (folder != null) {
        this.$element.append('<span>' + folder.Name + '</span>');
    }
    if (item != null) {
        if (item.IsList) {
            this.$element.append('<span> : ' + item.Name + '</span>');
        } else if (item.ParentID != null) {
            this.$element.append('<span> : ' + item.GetParent().Name + '</span>');
        }
    }
}

ItemPath.prototype.hide = function () {
    this.$element.hide();
}

ItemPath.prototype.show = function () {
    this.$element.show();
}

// ---------------------------------------------------------
// ItemEditor control
function ItemEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.mode = ItemEditor.Modes.NotSet;
    this.item;
    this.itemType;
}

ItemEditor.Modes = { NotSet: 0, NewItem: 1, Item: 2 };

ItemEditor.prototype.render = function (container, folder, item) {
    if (this.$element == null) {
        this.$element = $('<div class="item-editor"></div>').appendTo(container);
    }

    if (folder == null && item == null)
        return;

    var $txtName;
    var mode = (item != null && !item.IsList) ? ItemEditor.Modes.Item : ItemEditor.Modes.New;

    this.item = (mode == ItemEditor.Modes.Item) ? $.extend({}, item) : { Name: '' };
    this.itemType = (item != null) ? item.GetItemType() : folder.GetItemType();
    this.item.ItemTypeID = this.itemType.ID;

    if (this.mode != mode) {
        this.mode = mode;
        this.$element.empty();
        if (this.mode == ItemEditor.Modes.Item) {
            $txtName = $('<input type="text" class="item-name_input" />').appendTo(this.$element);
            $txtName.data('control', this.parentControl);
            $txtName.keypress(function (event) { Control.get(this).handleEnterKey(event); });
        } else {
            $txtName = $('<input type="text" class="item-name_input" />').appendTo(this.$element);
            $txtName.data('control', this.parentControl);
            $txtName.keypress(function (event) { Control.get(this).handleEnterKey(event); });
        }
    }

    $txtName = this.$element.find('.item-name_input');
    $txtName.val(this.item.Name);
    $txtName.focus();
    $txtName.select();

}

ItemEditor.prototype.editItem = function () {
    if (this.$element != null) {
        var $txtName = this.$element.find('.item-name_input');
        if ($txtName != null) {
            this.item.Name = $txtName.val();
        }
    }
    return this.item;
}

ItemEditor.prototype.hide = function () {
    this.$element.hide();
}

ItemEditor.prototype.show = function () {
    this.$element.show();
}

