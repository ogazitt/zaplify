//----------------------------------------------------------
// Copyright (C) BuiltSteady Inc. All rights reserved.
//----------------------------------------------------------
// Dashboard.js

// ---------------------------------------------------------
// Dashboard static object - manages controls for dashboard
// assumes there are three panes marked by classes:
// dashboard-left, dashboard-center, dashboard-right

var Dashboard = function Dashboard$() {
    // data members used
    this.dataModel = null;
    this.folderList = null;
    this.helpManager = null;
    this.settingsManager = null;
    this.folderManager = null;
    this.suggestionList = null;
    this.suggestionManager = null;
}

// ---------------------------------------------------------
// public methods

Dashboard.Init = function Dashboard$Init(dataModel, renewFBToken) {
    if (renewFBToken == true) {
        Service.GetFacebookConsent();
    }

    Dashboard.dataModel = dataModel;
    Dashboard.dataModel.AddDataChangedHandler('dashboard', Dashboard.ManageDataChange);

    // dashboard regions
    Dashboard.$element = $('.dashboard-region');
    Dashboard.$center = $('.dashboard-center');
    Dashboard.$left = $('.dashboard-left');
    Dashboard.$right = $('.dashboard-right');

    // folders list
    Dashboard.folderList = new FolderList(this.dataModel.Folders);
    Dashboard.folderList.addSelectionChangedHandler('dashboard', Dashboard.ManageFolder);

    // suggestions list
    Dashboard.suggestionList = new SuggestionList();
    Dashboard.suggestionList.addSelectionChangedHandler('dashboard', Dashboard.ManageChoice);

    // suggestions manager
    Dashboard.suggestionManager = new SuggestionManager(Dashboard.dataModel);
    // help and settings managers
    Dashboard.helpManager = new HelpManager(Dashboard, Dashboard.$center);
    Dashboard.settingsManager = new SettingsManager(Dashboard, Dashboard.$center);

    // folder manager
    Dashboard.folderManager = new FolderManager(Dashboard, Dashboard.$center);
    if (Dashboard.dataModel.UserSettings.ViewState.SelectedFolder != null) {
        Dashboard.showManager(Dashboard.folderManager);
        Dashboard.dataModel.restoreSelection();
    } else {
        Dashboard.showManager(Dashboard.helpManager);
    }

    // bind events
    $(window).bind('load', Dashboard.resize);
    $(window).bind('resize', Dashboard.resize);
    $(window).bind('unload', Dashboard.Close);
    $logo = $('.brand a');
    $logo.unbind('click');
    $logo.click(function () { Dashboard.dataModel.Refresh(); return false; });

    // add options to header dropdown
    Dashboard.showHeaderOptions();
}

Dashboard.Close = function Dashboard$Close(event) {
    $('.header-icons').hide();
    Dashboard.dataModel.Close();
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageDataChange = function Dashboard$ManageDataChange(folderID, itemID) {
    Dashboard.folderList.render(Dashboard.$left, Dashboard.dataModel.Folders);
    Dashboard.ManageFolder(folderID, itemID);
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageFolder = function Dashboard$ManageFolder(folderID, itemID) {
    var item;
    var folder = (folderID != null) ? Dashboard.dataModel.Folders[folderID] : null;
    if (itemID == null) {
        Dashboard.showManager(Dashboard.folderManager);
        Dashboard.folderManager.selectFolder(folder);
    } else {
        item = (folder != null && itemID != null) ? folder.Items[itemID] : null;
        Dashboard.showManager(Dashboard.folderManager);
        Dashboard.folderManager.selectItem(item);
    }
    Dashboard.dataModel.UserSettings.Selection(folderID, itemID);

    if (!Dashboard.resizing) {
        // get suggestions for currently selected user, folder, or item
        Dashboard.getSuggestions(folder, item);
    }
}

// event handler, do not reference 'this' to access static Dashboard
Dashboard.ManageChoice = function Dashboard$ManageChoice(suggestion) {
    var refresh = Dashboard.suggestionManager.select(suggestion);
    if (refresh) {      // refresh more suggestions
        // check for more suggestions every 5 seconds for 20 seconds
        $('.working').show();
        Dashboard.suggestionList.hideGroup(suggestion.groupID);
        var nTries = 0;
        var checkPoint = new Date();

        var checkForSuggestions = function () {
            if (checkPoint > Dashboard.dataModel.SuggestionsRetrieved && nTries++ < 5) {
                Dashboard.getSuggestions(Dashboard.folderManager.currentFolder, Dashboard.folderManager.currentItem);
                setTimeout(checkForSuggestions, 5000);
            } else {
                $('.working').hide();
            }
        }
        checkForSuggestions();
    }
}

// ---------------------------------------------------------
// private methods

Dashboard.render = function Dashboard$render(folderID, itemID) {
    Dashboard.folderList.render(Dashboard.$left);
}

// show options in header dropdown (refresh, settings, etc.)
Dashboard.showHeaderOptions = function Dashboard$showHeaderOptions() {
    $dropdown = $('.navbar-fixed-top .pull-right .dropdown-menu');
    // refresh
    $menuitem = $dropdown.find('.option-refresh');
    $menuitem.show();
    $menuitem.click(function (e) {
        Dashboard.dataModel.Refresh();
        e.preventDefault();
    });
    // user settings
    $menuitem = $dropdown.find('.option-settings');
    $menuitem.show();
    $menuitem.click(function (e) {
        Dashboard.showManager(Dashboard.settingsManager);
        e.preventDefault();
    });
    // help
    $menuitem = $dropdown.find('.option-help');
    $menuitem.show();
    $menuitem.click(function (e) {
        Dashboard.showManager(Dashboard.helpManager);
        e.preventDefault();
    });
}

Dashboard.showManager = function Dashboard$showManager(manager) {
    if (Dashboard.currentManager != manager) {
        Dashboard.currentManager = manager;
        Dashboard.folderManager.hide();
        Dashboard.settingsManager.hide();
        Dashboard.helpManager.hide();
        (manager.addWell == true) ? Dashboard.$center.addClass('well') : Dashboard.$center.removeClass('well'); 
    }
    // always show to force render if necessary
    manager.show();
}

Dashboard.resize = function Dashboard$resize() {
    if (Dashboard.resizing) { return; }

    Dashboard.resizing = true;
    $(window).unbind('resize', Dashboard.resize);

    var winHeight = $(window).height();
    var headerHeight = $('.navbar-fixed-top').height();
    var footerHeight = $('.navbar-fixed-bottom').height();
    var dbHeight = winHeight - (headerHeight + footerHeight + 30);

    Dashboard.$left.height(dbHeight);
    Dashboard.$center.height(dbHeight);
    Dashboard.$right.height(dbHeight);

    Dashboard.folderList.render(Dashboard.$left);
    Dashboard.folderManager.render();

    $(window).bind('resize', Dashboard.resize);
    Dashboard.resizing = false;
}

Dashboard.getSuggestions = function Dashboard$getSuggestions(folder, item) {
    if (item != null) {
        this.dataModel.GetSuggestions(Dashboard.renderSuggestions, item);
    } else if (folder != null) {
        this.dataModel.GetSuggestions(Dashboard.renderSuggestions, folder);
    } else {
        this.dataModel.GetSuggestions(Dashboard.renderSuggestions);
    }
}

Dashboard.renderSuggestions = function Dashboard$renderSuggestions(suggestions) {
    // process RefreshEntity suggestions
    var group = suggestions[SuggestionTypes.RefreshEntity];
    if (group != null) {
        // full user data refresh, select last item
        var itemID;
        for (var id in group.Suggestions) {
            var suggestion = group.Suggestions[id];
            Dashboard.dataModel.SelectSuggestion(suggestion, Reasons.Ignore);
            if (suggestion.EntityType == 'Item') {
                itemID = suggestion.EntityID;
            }
        }
        delete suggestions[SuggestionTypes.RefreshEntity];
        Dashboard.dataModel.Refresh(itemID);
    }

    Dashboard.suggestionList.render(Dashboard.$right, suggestions);
    if (suggestions['Group_0'] != null) {
        $('.working').hide();
    }
}

// ---------------------------------------------------------
// HelpManager control
function HelpManager(parentControl, $parentElement) {
    this.parentControl = parentControl;
    this.$parentControl = $parentElement;
    this.$element = null;
}

HelpManager.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

HelpManager.prototype.show = function () {
    if (this.$element == null) {
        this.$element = $('<div class="manager-help" />').appendTo(this.$parentControl);
        this.render();
    }
    this.$element.show();
}

// render is only called internally by show method
HelpManager.prototype.render = function () {
    var $help = $('<div class="hero-unit" />').appendTo(this.$element);
    $help.append('<h1>Zaplify!</h1>');
    $help.append(HelpManager.tagline);
    $help.append('<p><a class="btn btn-primary btn-large">Learn more</a></p>');
}

HelpManager.tagline =
'<p>The ultimate tool for managing your digital life. ' +
'Get connected and get organized. All your information just one click away. ' +
'Learns about you and provides recommendations. Your own personal assistant!</p>';

// ---------------------------------------------------------
// SettingsManager control
function SettingsManager(parentControl, $parentElement) {
    this.parentControl = parentControl;
    this.$parentControl = $parentElement;
    this.$element = null;
    this.addWell = true;
}

SettingsManager.prototype.hide = function () {
    if (this.$element != null) {
        this.$element.hide();
    }
}

SettingsManager.prototype.show = function () {
    if (this.$element == null) {
        this.$element = $('<div class="manager-settings" />').appendTo(this.$parentControl);
        this.render();
    }
    this.$element.show();
}

// render is only called internally by show method
SettingsManager.prototype.render = function () {
    var $header = $('<div class="manager-header ui-state-active"><label>User Preferences</label></div>').appendTo(this.$element);
    var $settings = $('<div class="manager-panel ui-widget-content" />').appendTo(this.$element);
    this.renderThemePicker($settings);

    // close button
    var $closeBtn = $('<div class="ui-icon ui-icon-circle-close" />').appendTo($header);
    $closeBtn.data('control', this);
    $closeBtn.attr('title', 'Close');
    $closeBtn.click(function () {
        var control = Control.get(this);
        control.parentControl.selectItem(control.parentControl.currentItem);
    });
}

SettingsManager.prototype.renderThemePicker = function (container) {
    var dataModel = Control.findParent(this, 'dataModel').dataModel;
    var themes = dataModel.Constants.Themes;
    var currentTheme = dataModel.UserSettings.Preferences.Theme;
    var $wrapper = $('<div class="ui-widget setting"><label>Theme </label></div>').appendTo(container);

    var $themePicker = $('<select />').appendTo($wrapper);
    for (var i in themes) {
        var $option = $('<option value="' + themes[i] + '">' + themes[i] + '</option>').appendTo($themePicker);
    }
    $themePicker.val(currentTheme);
    $themePicker.combobox({ selected: function () {
        var theme = $(this).val();
        dataModel.UserSettings.UpdateTheme(theme);
    }
    });
}

// ---------------------------------------------------------
// Control static object
// shared helpers used by controls
var Control = function Control$() { };
Control.ttDelay = { delay: { show: 500, hide: 200} };       // default tooltip delay

// get the control object associated with the id
Control.get = function Control$get(element) {
    return $(element).data('control');
}

// helpers for creating and invoking a delegate
Control.delegate = function Control$delegate(object, funcName) {
    var delegate = { object: object, handler: funcName };
    delegate.invoke = function () { return this.object[this.handler](); };
    return delegate;
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

// expand an element
Control.expand = function Control$expand($element, animate, callback) {
    if (animate == true) {
        $element.show('blind', { direction: 'vertical' }, 400, callback);   // animated
    } else {
        $element.collapse('show');
    }
}
// collapse an element
Control.collapse = function Control$collapse($element, animate, callback) {
    if (animate == true) {
        $element.hide('blind', { direction: 'vertical' }, 300, callback);   // animated
    } else {
        $element.collapse('hide');
    }
}

// return an element containing icons for item sources
Control.getIconsForSources = function Control$getIconsForSources(item) {
    var $icons = $('<span />');
    if (item.HasField(FieldNames.Sources)) {
        var sources = item.GetFieldValue(FieldNames.Sources);
        if (sources != null) {
            sources = sources.split(",");
            for (var i in sources) {
                switch (sources[i]) {
                    case "Facebook":
                        var fbID = item.GetFieldValue(FieldNames.FacebookID);
                        var $fbLink = $('<i class="icon-facebook-sign" />').appendTo($icons);
                        if (fbID != null) {
                            $fbLink.click(function () { window.open('http://www.facebook.com/' + fbID); });
                        }
                        break;
                    case "Directory":
                        $icons.append('<i class="azure-icon" />');
                        break;
                }
            }
        } else if (item.ItemTypeID == ItemTypes.Contact) {
            $icons.append('<i class="icon-user"></i>');
        }
    }
    return $icons;
}

// return an element that is an icon for the item type
Control.getIconForItemType = function Control$getIconForItemType(item) {
    var $icon = $('<i></i>');
    switch (item.ItemTypeID) {
        case ItemTypes.Task:
            (item.IsFolder()) ? $icon.addClass('icon-calendar') : $icon.addClass('icon-check');
            break;
        case ItemTypes.Contact:
            $icon.addClass('icon-user');
            break;
        case ItemTypes.Location:
            $icon.addClass('icon-map-marker');
            break;
        case ItemTypes.ShoppingItem:
            $icon.addClass('icon-shopping-cart');
            break;
        case ItemTypes.ListItem:
        default:
            $icon.addClass('icon-list-alt');
            break;
    }
    return $icon;
}
