//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// FolderList.js

// ---------------------------------------------------------
// FolderList control
function FolderList(folders) {
    // fires notification when selected folder changes
    this.onSelectionChangedHandlers = {};
    this.init(folders);
}

FolderList.prototype.init = function (folders) {
    this.folders = folders;
    this.$element = null;
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

FolderList.prototype.render = function ($element, folders) {
    if (folders != null) {
        this.init(folders);
    }
    $element.empty();
    this.$element = $('<ul class="nav nav-pills nav-stacked" />').appendTo($element);
    Control.List.sortable(this.$element);
    for (var id in this.folders) {
        var folder = this.folders[id];
        $folder = $('<li><a><strong>&nbsp;' + folder.Name + '</strong></a></li>').appendTo(this.$element);
        $folder.data('control', this);
        $folder.data('item', folder);
        $folder.click(function () { Control.get(this).folderClicked($(this)); });
        $folder.find('strong').prepend(Control.Icons.forItemType(folder));
        if (folder.IsSelected()) { this.select($folder, folder); }
        this.renderItems($folder, folder);
    }
}

FolderList.prototype.renderItems = function ($folder, folder) {
    var items = folder.GetItems();
    $itemList = $('<ul class="itemlist nav nav-pills nav-stacked" />').appendTo($folder.parent());
    Control.List.sortable($itemList);
    for (var id in items) {
        var item = items[id];
        if (item.IsList) {
            $item = $('<li><a><span>&nbsp;' + item.Name + '</span></a></li>').appendTo($itemList);
            $item.data('control', this);
            $item.data('item', item);
            $item.click(function () { Control.get(this).itemClicked($(this)); });
            $item.find('span').prepend(Control.Icons.forItemType(item));
            if (item.IsSelected(true)) { this.select($item, item); }
        }
    }
    if (folder.IsExpanded()) { this.expand($folder); }
    else { this.collapse($folder); }
}

FolderList.prototype.folderClicked = function ($folder) {
    var folder = $folder.data('item');
    this.toggle($folder);
    this.select($folder, folder);
    this.fireSelectionChanged(folder.ID);
}

FolderList.prototype.itemClicked = function ($item) {
    //this.$element.find('li').removeClass('active');
    var item = $item.data('item');
    this.select($item, item);
    this.fireSelectionChanged(item.FolderID, item.ID);
}

FolderList.prototype.select = function ($item, item) {
    this.deselect();
    $item.addClass('active');
}

FolderList.prototype.deselect = function () {
    this.$element.find('li').removeClass('active');
}

FolderList.prototype.expand = function ($folder) {
    var folder = $folder.data('item');
    $folder.addClass('expanded');
    folder.Expand(true);
    Control.expand($folder.next('.itemlist'));
}

FolderList.prototype.collapse = function ($folder) {
    var folder = $folder.data('item');
    $folder.removeClass('expanded');
    folder.Expand(false);
    Control.collapse($folder.next('.itemlist'));
}

FolderList.prototype.toggle = function ($folder) {
    var expanded = $folder.hasClass('expanded');
    if ($folder.hasClass('active') && expanded) {
        this.collapse($folder);
    } else if (!expanded) {
        this.expand($folder);
    }
}
