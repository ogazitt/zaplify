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
    this.$element = $element;
    var $newItem = this.newItemEditor.render($element, list);
    var newItemHeight = ($newItem != null) ? $newItem.outerHeight() : 0;
    this.listView.render($element, list, maxHeight - newItemHeight - 28);   // exclude top & bottom padding
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
        $field.focus();
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
    $nameField.attr('placeholder', '-- new item --');
    return $nameField;
}

// ---------------------------------------------------------
// ListView control
function ListView(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list = null;
}

// static helper for getting attached item from $element
ListView.getItem = function ListView$getItem($element) {
    return $element.parents('li').first().data('item');
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
        this.renderDeleteBtn($item);
        this.renderNameField($item, item);

        // click item to select
        $li.bind('click', function (e) {
            if ($(this).hasClass('sorting') ||
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
    // render name field
    $item.append(Control.Icons.forSources(item));
    field = fields[FieldNames.Name];
    Control.Text.renderLabel($item, item, field);
}

ListView.prototype.renderDeleteBtn = function ($element) {
    var $button = $('<i class="icon-remove-sign pull-right"></i>').appendTo($element);
    $button.attr('title', 'Delete Item').tooltip(Control.ttDelay);
    $button.bind('click', function () {
        var item = ListView.getItem($(this));
        var activeItem = (item.ParentID == null) ? item.GetFolder() : item.GetParent();
        $(this).tooltip('hide');
        item.Delete(activeItem);
        return false;   // do not propogate event
    });
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
            if (item.GetFieldValue(FieldNames.Complete) != true) {
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
    var $property = $('<input type="text" class="" />').appendTo($wrapper);
    $property.addClass('li-name');
    $property.data('control', this);
    $property.val(this.list.Name);
    $property.change(function (e) { Control.get(this).handleChange($(e.srcElement)); });
    $property.keypress(function (e) { return Control.get(this).handleEnterPress(e); });

    // itemtype property
    var $dropdown = Control.ItemType.renderDropdown($form, this.list);
    // display controls inline
    $wrapper.addClass('inline-left');
    $dropdown.addClass('inline-left');
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