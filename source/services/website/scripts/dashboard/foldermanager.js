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
    this.managerHelp = new FolderManagerHelp(this);

    // TEMPORARY 
    this.$itemPath = null;
}

FolderManager.prototype.render = function (container) {
    if (this.$managerRegion == null) {
        $(container).empty();

        // toolbar
        $('<div class="manager-toolbar"></div>').appendTo($(container));
        this.toolbar.render('.manager-toolbar');
        this.toolbar.bind(Toolbar.Refresh, 'click', this.dataModel.Refresh);
        this.toolbar.bind(Toolbar.AddFolder, 'click', function (myself) { alert('not implemented'); }, this);
        this.toolbar.bind(Toolbar.AddList, 'click', function (myself) { myself.addItem(true); }, this);
        this.toolbar.bind(Toolbar.AddItem, 'click', function (myself) { myself.addItem(false); }, this);
        this.toolbar.bind(Toolbar.UpdateItem, 'click', function (myself) { myself.updateItem(); }, this);
        this.toolbar.bind(Toolbar.DeleteItem, 'click', function (myself) { myself.deleteItem(); }, this);

        // manager region
        this.$managerRegion = $('<div class="manager-region"></div>').appendTo($(container));

        //TEMPORARY
        this.$itemEditor = $('<div class="item-editor"></div>').appendTo(this.$managerRegion);
        this.$itemPath = $('<div class="item-path"></div>').appendTo(this.$itemEditor);
        $input = $('<div><input id="txtName" type="text" class="input_item-name" /></div>').appendTo(this.$itemEditor);
        $input.data('control', this);
        $input.keypress(function (event) { Control.get(this).handleEnterKey(event); });
    }
}

FolderManager.prototype.handleEnterKey = function (event) {
    if (event.which == 13) {
        if (this.currentItem == null || this.currentItem.IsList) {
            this.addItem(false);
        } else {
            this.updateItem();
        }
    }
}

FolderManager.prototype.addItem = function (isList) {
    var value = $('#txtName').val();
    if (this.currentItem != null) {
        this.currentItem.InsertItem({ Name: value, IsList: isList });
    } else {
        this.currentFolder.InsertItem({ Name: value, IsList: isList });
    }
    $('#txtName').val('');
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
        if (this.currentItem.IsList && !confirm('Are you sure?\n\nThis will delete the list and all items contained within!')) {
            return;
        }
        this.currentItem.Delete();
    } else {
        // WARN USER when deleting a Folder
        //if (confirm('Are you sure?\n\nThis will delete the folder and all items contained within!')) {
        //    this.currentFolder.Delete();
        //}
    }
}

FolderManager.prototype.selectFolder = function (folder) {
    this.currentFolder = folder;
    if (this.currentFolder != null) {
        this.toolbar.disableTools([Toolbar.UpdateItem, Toolbar.DeleteItem]);
        this.$itemPath.empty();
        this.$itemPath.append('<span>' + folder.Name + '</span>');
        this.managerHelp.hide();
        this.$itemEditor.show();
    } else {
        this.managerHelp.show();
        this.$itemEditor.hide();
    }
}

// TEMPORARY
FolderManager.prototype.selectItem = function (item) {
    this.currentItem = item;
    if (this.currentItem != null) {
        if (this.currentItem.IsList) {
            $('#txtName').val('');
            this.toolbar.disableTools([Toolbar.AddFolder, Toolbar.AddList ]);
            this.$itemPath.empty();
            this.$itemPath.append('<span>' + this.currentFolder.Name + '</span>');
            this.$itemPath.append('<span>.' + item.Name + '</span>');
        } else {
            $('#txtName').val(item.Name);
            this.toolbar.disableTools([Toolbar.AddFolder, Toolbar.AddList, Toolbar.AddItem]);

            this.$itemPath.empty();
            this.$itemPath.append('<span>' + this.currentFolder.Name + '</span>');
            if (this.currentItem.ParentID != null) {
                this.$itemPath.append('<span>.' + item.GetParent().Name + '</span>');
            }
        }
    } else {
        $('#txtName').val('');
        this.toolbar.disableTools([Toolbar.UpdateItem, Toolbar.DeleteItem]);
        this.selectFolder(this.currentFolder);
    }
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
// ItemEditor control
function ItemEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
}

ItemEditor.prototype.render = function (container) {
    this.$element = $('<div class="manager-help"><h1>Welcome to Zaplify!</h1></div>').appendTo($(container));
}

ItemEditor.prototype.hide = function () {
    this.$element.hide();
}

ItemEditor.prototype.show = function () {
    this.$element.show();
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