//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// ItemViewer.js

// ---------------------------------------------------------
// ItemViewer control
function ItemViewer(parentControl) {
    this.parentControl = parentControl;
    this.$element = null;
    this.list = null;
}

// static helper for getting attached item from $element
ItemViewer.getItem = function ItemViewer$getItem($element) {
    return $element.parents('.item-view').data('item');
}

ItemViewer.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

ItemViewer.prototype.show = function (height) {
    if (this.$element != null) {
        if (height != null) { this.$element.height(height); }
        this.$element.show();
    }
}

ItemViewer.prototype.render = function (list, container) {
    if (list == null) { return; }
    if (this.$element == null) {
        this.$element = $('<div class="manager-list-panel ui-widget-content" />').appendTo(container);
    }
    this.$element.empty();
    if (this.renderListItems(list.GetItems()) == 0) { this.hide(); }
}

ItemViewer.prototype.renderListItems = function (listItems) {
    var itemCount = 0;
    for (var id in listItems) {
        var item = listItems[id];
        if (!item.IsList) {
            var $item = $('<div class="item-view" />').appendTo(this.$element);
            $item.data('item', item);
            var $wrapper = $('<div />').appendTo($item);
            this.renderNameField($wrapper, item);
            this.renderDeleteBtn($wrapper);
            this.renderEditBtn($wrapper);
            this.renderFields($item, item);
            itemCount++;
        }
    }
    return itemCount;
}

ItemViewer.prototype.renderNameField = function ($element, item) {
    var fields = item.GetFields();
    // render complete field if exists 
    var field = fields[FieldNames.Complete];
    if (field != null) {
        $element.addClass('checked');
        this.renderCheckbox($element, item, field);
    }
    // render name field
    Control.renderSourceIcons($element, item);
    field = fields[FieldNames.Name];
    this.renderText($element, item, field);
}

ItemViewer.prototype.renderDeleteBtn = function ($element) {
    var $button = $('<div class="ui-icon delete-icon" />').appendTo($element);
    $button.attr('title', 'Delete Item');
    $button.bind('click', function () {
        var item = ItemViewer.getItem($(this));
        var activeItem = (item.ParentID == null) ? item.GetFolder() : item.GetParent();
        item.Delete(activeItem);
    });
}

ItemViewer.prototype.renderEditBtn = function ($element) {
    var $button = $('<div class="ui-icon edit-icon" />').appendTo($element);
    $button.attr('title', 'Edit Item');
    $button.bind('click', function () {
        var item = ItemViewer.getItem($(this));
        $('#' + item.ID).click();   // select item
    });
}

ItemViewer.prototype.renderFields = function ($element, item) {
    var fields = item.GetFields();
    for (var name in fields) {
        var field = fields[name];
        this.renderField($element, item, field);
    }
}

ItemViewer.prototype.renderField = function ($element, item, field) {
    var $field;
    var renderByDisplayType = false;
    var $wrapper = $('<div class="item-view-field" />').appendTo($element);

    switch (field.Name) {
        case FieldNames.Name:
            break;
        case FieldNames.DueDate:
            $field = this.renderText($wrapper, item, field, 'Due on ');
            break;
        case FieldNames.Category:
            $field = this.renderText($wrapper, item, field);
            break;
        case FieldNames.Email:
            $field = this.renderEmail($wrapper, item, field);
            break;
        case FieldNames.Address:
            var address = item.GetFieldValue(FieldNames.Address);
            if (address != item.Name) {
                $field = this.renderText($wrapper, item, field);
            }
            break;
        default:
            break;
    }

    if (renderByDisplayType) {
        switch (field.DisplayType) {
            case DisplayTypes.Checkbox:
            case DisplayTypes.Hidden:
            case DisplayTypes.Priority:
            case DisplayTypes.Reference:
            case DisplayTypes.TagList:
                break;
            default:
                $field = this.renderText($wrapper, item, field);
                break;
        }
    }

    return $field;
}

ItemViewer.prototype.renderText = function ($element, item, field, textBefore, textAfter) {
    var $field;
    var value = item.GetFieldValue(field);
    if (value != null) {
        $field = $('<span />').appendTo($element);
        $field.addClass(field.Class);
        value = ((textBefore == null) ? '' : textBefore) + value + ((textAfter == null) ? '' : textAfter);
        $field.html(value);
    }
    return $field;
}

ItemViewer.prototype.renderEmail = function ($element, item, field) {
    var $field;
    var value = item.GetFieldValue(field);
    if (value != null) {
        $field = $('<a />').appendTo($element);
        $field.addClass(field.Class);
        $field.attr('href', 'mailto:' + value);
        $field.html(value);
    }
    return $field;
}

ItemViewer.prototype.renderCheckbox = function ($element, item, field) {
    $field = $('<input type="checkbox" />').appendTo($element);
    $field.addClass(field.Class);
    $field.attr('title', field.DisplayName);
    $field.data('control', this);
    if (item.GetFieldValue(field) == 'true') {
        $field.attr('checked', 'checked');
    }
    $field.change(function () { Control.get(this).updateCheckbox($(this), field); });
    return $field;
}

ItemViewer.prototype.updateCheckbox = function ($checkbox, field) {
    var item = ItemViewer.getItem($checkbox);
    if (item != null) {
        var currentValue = item.GetFieldValue(field);
        var value = ($checkbox.attr('checked') == 'checked');
        if (value != currentValue) {
            var updatedItem = item.Copy();
            updatedItem.SetFieldValue(field, value);
            item.Update(updatedItem, null);
        }
    }
}
