//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// SuggestionList.js

// ---------------------------------------------------------
// SuggestionList control
function SuggestionList() {
    // fires notification when suggestion is selected
    this.onSelectionChangedHandlers = {};
    this.suggestions = {};
    this.$element = null;

    this.groupButtons = [];
}

SuggestionList.prototype.addSelectionChangedHandler = function (name, handler) {
    this.onSelectionChangedHandlers[name] = handler;
}

SuggestionList.prototype.removeSelectionChangedHandler = function (name) {
    this.onSelectionChangedHandlers[name] = undefined;
}

SuggestionList.prototype.fireSelectionChanged = function (suggestion) {
    for (var name in this.onSelectionChangedHandlers) {
        var handler = this.onSelectionChangedHandlers[name];
        if (typeof (handler) == "function") {
            if (suggestion != null) {
                handler(suggestion);
            }
        }
    }
}

SuggestionList.prototype.getGroupButtons = function () {
    // create buttons for each group of suggestions
    this.groupButtons = [];
    for (var i in this.suggestions) {
        this.groupButtons = this.groupButtons.concat(new GroupButton(this, this.suggestions[i]));
    }
    return this.groupButtons;
}

SuggestionList.prototype.render = function (container, suggestions) {
    this.suggestions = suggestions;
    this.$element = $(container).empty();
    this.getGroupButtons();
    for (var i in this.groupButtons) {
        this.groupButtons[i].render(container);
    }
}

SuggestionList.prototype.hideGroup = function (groupID) {
    for (var i in this.groupButtons) {
        var button = this.groupButtons[i];
        if (button.group.groupID == groupID) {
            button.hide();
            break;
        }
    }
}

// ---------------------------------------------------------
// GroupButton control
function GroupButton(parentControl, group) {
    this.parentControl = parentControl;
    this.group = group;
    this.choiceList = null;
    this.$element = null;
}

GroupButton.prototype.render = function (container) {
    if (this.group.Suggestions != {}) {
        this.$element = $('<div class="folder-button ui-widget ui-state-active"></div>').appendTo(container);
        this.$element.append('<span>' + this.group.DisplayName + '</span>');
        this.$element.hover(function () { $(this).addClass('ui-state-hover'); }, function () { $(this).removeClass('ui-state-hover'); });

        var choiceList = new ChoiceList(this, this.group.Suggestions);
        var $container = $('<div class="folder-items"></div>').insertAfter(this.$element);
        choiceList.render($container);
    }
}

GroupButton.prototype.hide = function () {
    Control.animateCollapse(this.$element.next());
    Control.animateCollapse(this.$element);
}

// ---------------------------------------------------------
// ChoiceList control
function ChoiceList(parentControl, choices) {
    this.parentControl = parentControl;
    this.choiceButtons = [];
    for (var i in choices) {
        this.choiceButtons = this.choiceButtons.concat(new ChoiceButton(this, choices[i]));
    }
}

ChoiceList.prototype.render = function (container) {
    $(container).empty();
    for (var i in this.choiceButtons) {
        this.choiceButtons[i].render(container);
    }
    Control.animateExpand($(container));
}

// ---------------------------------------------------------
// ChoiceButton control
function ChoiceButton(parentControl, choice) {
    this.parentControl = parentControl;
    this.choice = choice;
    this.indent = (choice.ParentID == null) ? 'indent-none' : 'indent-1';
    this.$element = null;
}

ChoiceButton.prototype.render = function (container) {
    this.$element = $('<div></div>').attr('id', this.choice.ID).appendTo(container);
    this.$element.hover(function () { $(this).addClass('ui-state-highlight'); }, function () { $(this).removeClass('ui-state-highlight'); });
    this.$element.data('control', this);
    if (this.choice.Suggestions == null) {
        this.$element.addClass('item-button');
    } else {
        this.$element.addClass('item-list-button');
        this.$element.addClass('ui-state-default');
    }
    this.$element.addClass(this.indent);
    this.$element.click(function () { Control.get(this).select(); });
    this.$element.append('<span>' + this.choice.DisplayName + '</span>');

    if (this.choice.SuggestionType == SuggestionTypes.ChooseOneSubject) {
        var contact = $.extend(new Item(), $.parseJSON(this.choice.Value));
        Control.renderSourceIcons(this.$element, contact);
    }
}

ChoiceButton.prototype.select = function () {
    var groupButton = this.getGroupButton();
    if (groupButton != null) {
        if (this.choice.Suggestions == null) {
            this.selectChoice(groupButton);
        } else {
            this.selectList(groupButton);
        }
    }
}

ChoiceButton.prototype.selectList = function (groupButton) {
    if (groupButton != null) {
        var expanded = this.$element.hasClass('expanded');
        var groupList = groupButton.parentControl;

        this.$element.addClass('ui-state-active');
        if (!expanded) {
            this.collapseAllChoices();
            this.expandChoices();
        }
    }
}

ChoiceButton.prototype.selectChoice = function (groupButton) {
    // fire choice selected
    if (groupButton != null) {
        var groupList = groupButton.parentControl;
        groupList.fireSelectionChanged(this.choice);
    }
}

ChoiceButton.prototype.expandChoices = function () {
    this.$element.addClass('expanded');
    var $container = $('<div class="list-items"></div>').insertAfter(this.$element);
    var choiceList = new ChoiceList(this, this.choice.Suggestions);
    choiceList.render('#' + this.choice.ID + " + .list-items");
}

ChoiceButton.prototype.collapseChoices = function () {
    if (this.$element != null && this.$element.next().hasClass('list-items')) {
        this.$element.removeClass('expanded');
        this.$element.removeClass('ui-state-active');
        this.$element.next().remove();
    }
}

ChoiceButton.prototype.collapseAllChoices = function () {
    for (var i in this.parentControl.choiceButtons) {
        this.parentControl.choiceButtons[i].collapseChoices();
    }
}

// return parent GroupButton
ChoiceButton.prototype.getGroupButton = function () {
    return Control.findParent(this, 'group');
}

