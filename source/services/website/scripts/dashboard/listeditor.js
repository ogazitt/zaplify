//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// ListEditor.js

// ---------------------------------------------------------
// ListEditor control
function ListEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;

    this.newItemEditor = new NewItemEditor(this);
    this.listView = new ListView(this);
}

ListEditor.prototype.render = function ($element, list, maxHeight) {
    if (this.$element == null) { this.$element = $element; }
    var $newItem = this.newItemEditor.render(this.$element, list);
    var newItemHeight = ($newItem != null) ? $newItem.outerHeight() : 0;
    this.listView.render(this.$element, list, maxHeight - newItemHeight - 28);   // exclude top & bottom padding
    $newItem.find('.fn-name').focus();
}

ListEditor.prototype.selectItem = function (item) {
    this.parentControl.fireSelectionChanged(item);
}

// ---------------------------------------------------------
// NewItemEditor control
function NewItemEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list;
    this.newItem;
}

NewItemEditor.prototype.render = function ($element, list) {
    if (list != null && (list.IsFolder() || list.IsList)) {
        if (this.$element == null) {
            this.$element = $('<div class="row-fluid" />').appendTo($element);
        }
        this.$element.empty();

        this.list = list;
        this.newItem = Item.Extend({ Name: '', ItemTypeID: this.list.ItemTypeID });

        // render name field for new item 
        $field = this.renderNameField(this.$element);
        $field.val('');
    }
    return this.$element;
}

NewItemEditor.prototype.renderNameField = function ($element) {
    // render name field
    var fields = this.newItem.GetFields();
    var nameField = fields[FieldNames.Name];
    var $form = $('<form class="form-inline"/>').appendTo($element);

    var $nameField = Control.Text.renderInputNew($form, this.newItem, nameField, this.list);

    // TODO: figure out how to append button but keep on one line 100% wide
    //var $append = $('<div class="input-append" />').appendTo($form);
    //var $addButton = $('<span class="add-on"><i class="icon-plus-sign"></i></span>').appendTo($append);

    $nameField.addClass('input-block-level');
    return $nameField;
}

// ---------------------------------------------------------
// ListView control
function ListView(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list = null;
}

ListView.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

ListView.prototype.show = function () {
    if (this.$element != null) {
        this.$element.show();
    }
}

ListView.prototype.render = function ($element, list, height) {
    if (list == null) { return; }
    if (this.$element == null) {
        this.$element = $('<ul class="nav nav-list" />').appendTo($element);
        Control.List.sortable(this.$element);
    }

    this.hide();
    this.$element.empty();
    if (height != null) { this.$element.css('max-height', height); }
    if (this.renderListItems(list.GetItems(true)) > 0) {
        this.show();
        var $selected = this.$element.find('li.selected');
        // scroll selected item into view
        var scroll = $selected.offset().top - height + this.$element.scrollTop();
        if (scroll > 0) {
            this.$element.animate({ scrollTop: scroll }, 500);
        }
    }
}

ListView.prototype.renderListItems = function (listItems) {
    var itemCount = 0;
    for (var id in listItems) {
        var item = listItems[id];
        var $li = $('<li />').appendTo(this.$element);
        $li.data('control', this);
        $li.data('item', item);
        if (item.IsSelected()) { $li.addClass('selected'); }

        var $item = $('<a class="form-inline" />').appendTo($li);
        var $deleteBtn = Control.Icons.deleteBtn(item).appendTo($item);
        $deleteBtn.addClass('pull-right');

        this.renderNameField($item, item);

        // click item to select
        $li.bind('click', function (e) {
            if ($(this).hasClass('ui-sortable-helper') ||
                $(e.srcElement).hasClass('dt-checkbox') ||
                $(e.srcElement).hasClass('dt-email')) {
                return;
            }
            var item = $(this).data('item');
            Control.get(this).parentControl.selectItem(item);
        });

        this.renderFields($item, item);
        itemCount++;
    }
    return itemCount;
}

ListView.prototype.renderNameField = function ($item, item) {
    var fields = item.GetFields();
    // render complete field if exists 
    var field = fields[FieldNames.Complete];
    if (field != null) {
        Control.Checkbox.render($item, item, field);
    }
    // render map icon if weblinks exists 
    var field = fields[FieldNames.WebLinks];
    if (field != null) {
        $item.append(Control.Icons.forMap(item));
    }
    // render name field
    $item.append(Control.Icons.forSources(item));
    field = fields[FieldNames.Name];
    Control.Text.renderLabel($item, item, field);
}

ListView.prototype.renderFields = function ($element, item) {
    var $fields = $('<div />').appendTo($element);
    var fields = item.GetFields();
    for (var name in fields) {
        var field = fields[name];
        this.renderField($fields, item, field);
    }
    $('<small>&nbsp;</small>').appendTo($fields); 
}

ListView.prototype.renderField = function ($element, item, field) {
    var $field;
    switch (field.Name) {
        case FieldNames.DueDate:
            if (item.HasField(FieldNames.EndDate)) {
                var endField = item.GetField(FieldNames.EndDate);
                $field = Control.DateTime.renderRange($element, item, field, endField, 'small');
            }
            else if (item.HasField(FieldNames.Complete) && item.GetFieldValue(FieldNames.Complete) != true) {
                $field = Control.Text.render($element, item, field, 'small', 'Due on ');
            }
            break;
        case FieldNames.CompletedOn:
            if (item.GetFieldValue(FieldNames.Complete) == true) {
                $field = Control.Text.render($element, item, field, 'small', 'Completed on ');
            }
            break;
        case FieldNames.Category:
            $field = Control.Text.render($element, item, field, 'small');
            break;
        case FieldNames.Email:
            $field = Control.Text.renderEmail($element, item, field);
            break;
        case FieldNames.Address:
            var address = item.GetFieldValue(FieldNames.Address);
            if (address != item.Name) {
                $field = Control.Text.render($element, item, field, 'small');
            }
            break;
        default:
            break;
    }
    return $field;
}

// ---------------------------------------------------------
// PropertyEditor control
function PropertyEditor(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list;
}

PropertyEditor.prototype.render = function ($element, list, maxHeight) {
    this.$element = $element.empty();
    var $form = $('<form class="row-fluid form-vertical" />').appendTo(this.$element);
    $form.data('control', this);
    this.list = list;

    // name property
    var $wrapper = $('<div class="control-group"><label class="control-label">Name of List</label></div>').appendTo($form);
    $wrapper.addClass('inline-left');
    var $nameInput = $('<input type="text" class="" />').appendTo($wrapper);
    $nameInput.addClass('li-name');
    $nameInput.data('control', this);
    $nameInput.val(this.list.Name);
    $nameInput.change(function (e) { Control.get(this).handleChange($(e.srcElement)); });
    $nameInput.keypress(function (e) { return Control.get(this).handleEnterPress(e); });

    // itemtype property
    var $itemTypePicker = Control.ItemType.renderDropdown($form, this.list);
    $itemTypePicker.addClass('inline-left');

    // actions dropdown
    var $actions = this.renderListActions($form, this.list);
    $actions.addClass('inline-left');
}

PropertyEditor.prototype.updateProperty = function ($element) {
    if ($element.hasClass('li-name')) {
        var updatedList = this.list.Copy();
        updatedList.Name = $element.val();
        return updatedList;
    }
    return null;
}

PropertyEditor.prototype.handleChange = function ($element) {
    var updatedList = this.updateProperty($element);
    if (updatedList != null) {
        this.list.Update(updatedList);
    }
}

PropertyEditor.prototype.handleEnterPress = function (e) {
    if (e.which == 13) {
        this.handleChange($(e.srcElement));
        return false;       // do not propogate event
    }
}

PropertyEditor.prototype.renderListActions = function ($element, list) {
    var $wrapper = $('<div class="control-group"><label class="control-label">&nbsp;</label></div>').appendTo($element);
    var $btnGroup = $('<div class="btn-group" />').appendTo($wrapper);
    var $btn = $('<a class="btn">Action</a>').appendTo($btnGroup);
    $btn = $('<a class="btn dropdown-toggle" data-toggle="dropdown"><span class="caret" /></a>').appendTo($btnGroup);

    var $dropdown = $('<ul class="dropdown-menu" />').appendTo($btnGroup);
    if (list.IsFolder()) {
        $('<li><a href="newfolder"><i class="icon-align-justify"></i> New List</a></li>').appendTo($dropdown);
        $('<li><a href="newlist"><i class="icon-list"></i> New Sublist</a></li>').appendTo($dropdown);
    } else {
        $('<li><a href="newlist"><i class="icon-align-justify"></i> New List</a></li>').appendTo($dropdown);
    }
    if (!list.IsDefault()) {
        $('<li class="divider"></li>').appendTo($dropdown);
        $('<li><a href="deletelist"><i class="icon-remove-sign"></i> Delete List</a></li>').appendTo($dropdown);
    }
    // action handler
    $dropdown.click(function (e) {
        var $element = $(e.target);
        var action = $element.attr('href');
        if (action == 'newfolder') {
            var newFolder = Folder.Extend({ Name: 'New List', ItemTypeID: list.ItemTypeID });
            DataModel.InsertFolder(newFolder);
        }
        if (action == 'newlist') {
            var folder = (list.IsFolder()) ? list : list.GetFolder();
            var newList = Item.Extend({ Name: 'New List', ItemTypeID: list.ItemTypeID, IsList: true });
            folder.Expand(true);
            folder.InsertItem(newList);
        }
        if (action == 'deletelist') {
            if (!list.IsDefault()) { list.Delete(); }
        }
        e.preventDefault();
    });

    return $wrapper;
}
