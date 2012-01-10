/* html.js */

var html = html || {};

html.createListPicker = function (list) {
    var str = "<select>";
    for (var opt in list) {
        var entry = list[opt];
        str += "<option id='" + entry.Name + "' rel='" + entry.ID + "'>" + entry.Name + "</option>";
    }
    str += "</select>";
    return str;
};

html.createPriorityPicker = function (list) {
    var str = "<select>";
    for (var opt in list) {
        var priority = list[opt];
        str += "<option id='" + priority.Name + "' rel='" + priority.PriorityID + "'>" + priority.Name + "</option>";
    }
    str += "</select>";
    return str;
};

html.priorityIDElement = function (priorityID) {
    var icon = "";
    var txt = "";
    switch (priorityID) {
        case 0:
            icon = "lowpri";
            txt = "low priority";
            break;
        case 1: 
            icon = "normalpri";
            txt = "normal priority";
            break;
        case 2: 
            icon = "highpri";
            txt = "high priority";
            break;
    }
    var str = "<span class='icon " + icon + "' title='" + txt + "'></span>";
    return str;
};

html.checkboxElement = function (checked) {
    var str = "<div class='checkboxicon" + (checked == true ? " checked" : "") + "'><input class='input-checked' type='checkbox' /></div>";
    return str;
};

html.strip_tags = function (input, allowed) {
    allowed = (((allowed || "") + "").toLowerCase().match(/<[a-z][a-z0-9]*>/g) || []).join('');
    var tags = /<\/?([a-z][a-z0-9]*)\b[^>]*>/gi, commentsAndPhpTags = /<!--[\s\S]*?-->|<\?(?:php)?[\s\S]*?\?>/gi; 
    return input.replace(commentsAndPhpTags, '').replace(tags, function ($0, $1) {
        return allowed.indexOf('<' + $1.toLowerCase() + '>') > -1 ? $0 : ''; 
    });
};

html.replace_http_link = function (text) {
    var exp = /((http|https|ftp):\/\/[\w?=&.\/-;#~%-]+(?![\w\s?&.\/;#~%"=-]*>))/g;
    return text.replace(exp, "<a href='$1'>$1</a>"); 
}; 

html.validateEmail = function (email) {
    var reg = /^([A-Za-z0-9\+_\-\.])+\@([A-Za-z0-9_\-\.])+\.([A-Za-z]{2,4})$/; 
    if (reg.test(email) == false) { $('.error').text(language.data.error_invalid_email); return false; }
    else { return true; }
};

html.str_replace = function (search, replace, subject) { return subject.split(search).join(replace); };

html.isInteger = function (s) { return (s.toString().search(/^-?[0-9]+$/) == 0); }; 

html.convertString = function (string, length) {
    string = string.split('<').join(escape('<'));
    string = string.split('>').join(escape('>'));
    string = string.split("'").join(escape("'")); 
    if (length != undefined && length > 0)
        string = string.substr(0, length); 
    return string;
};

html.xss_clean = function (str) {
    str = html.convertString(str); str = str.replace(/\\0/gi, '');
    str = str.replace(/\\\\0/gi, '');
    str = str.replace(/#(&\#*\w+)[\x00-\x20]+;#u/g, "$1;");
    str = str.replace(/#(&\#x*)([0-9A-F]+);*#iu/g, "$1$2;");
    str = str.replace(/%u0([a-z0-9]{3})/gi, "&#x$1;");
    str = str.replace(/%([a-z0-9]{2})/gi, "&#x$1;");
    results = str.match(/<.*?>/g, str);
    if (results) {
        var i;
        for (i = 0; i < results.length; i++) {
            str = str.replace(results[i], html.html_entity_decode(results[i]));
        }
    }
    str = str.replace(/\\t+/g, " ");
    str = str.replace(/<\?php/g, '&lt;?php');
    str = str.replace(/<\?PHP/g, '&lt;?PHP');
    str = str.replace(/<\?/g, '&lt;?');
    str = str.replace(/\?>/g, '?&gt;');
    words = new Array('javascript', 'vbscript', 'script', 'applet', 'alert', 'document', 'write', 'cookie', 'window');
    for (t in words) {
        temp = '';
        for (i = 0; i < words[t].length; i++) {
            temp += words[t].substr(i, 1) + "\\s*";
        }
        temp = temp.substr(0, temp.length - 3);
        myRegExp = new RegExp(temp, "gi");
        str = str.replace(myRegExp, words[t]);
    }
    str = str.replace(/\/<a.+?href=.*?(alert\(|alert&\#40;|javascript\:|window\.|document\.|\.cookie|<script|<xss).*?\>.*?<\/a>/gi, "");
    str = str.replace(/<img.+?src=.*?(alert\(|alert&\#40;|javascript\:|window\.|document\.|\.cookie|<script|<xss).*?\>/gi, "");
    str = str.replace(/<(script|xss).*?\>/gi, "");
    str = str.replace(/(<[^>]+.*?)(onblur|onchange|onclick|onfocus|onload|onmouseover|onmouseup|onmousedown|onselect|onsubmit|onunload|onkeypress|onkeydown|onkeyup|onresize)[^>]*>/gi, "$1");
    str = str.replace(/<(\/*\s*)(alert|applet|basefont|base|behavior|bgsound|blink|body|embed|expression|form|frameset|frame|head|html|ilayer|iframe|input|layer|link|meta|object|plaintext|style|script|textarea|title|xml|xss)([^>]*)>/ig, "&lt;$1$2$3&gt;");
    str = str.replace(/(alert|cmd|passthru|eval|exec|system|fopen|fsockopen|file|file_get_contents|readfile|unlink)(\s*)\((.*?)\)/gi, "$1$2&#40;$3&#41;");
    bad = new Array('document.cookie', 'document.write', 'window.location', "javascript\s*:", "Redirect\s+302");
    for (val in bad) {
        myRegExp = new RegExp(bad[val], "gi");
        str = str.replace(myRegExp, bad[val]);
    }
    str = str.replace(/<!--/g, "&lt;!--");
    str = str.replace(/-->/g, "--&gt;");
    return str;
};

html.html_entity_decode = function (str) {
    var ta = document.createElement("textarea");
    ta.innerHTML = str.replace(/</g, "&lt;").replace(/>/g, "&gt;");
    result = ta.value;
    result = result.replace(/&#x([0-9a-f]{2,5})/g, String.fromCharCode("$1"));
    result = result.replace(/&#([0-9]{2,4})/g, String.fromCharCode("$1"));
    return result;
}; 

html.showDateByLanguage = function (day, month, year) {
    var dateformat = settings.getDateformat(); 
    if (dateformat == 'de')
        return day + '.' + month + '.' + year; 
    else if (dateformat == 'en')
        return day + '/' + month + '/' + year; 
    else if (dateformat == 'us')
        return month + '/' + day + '/' + year; 
    else if (dateformat == 'us-dash')
        return year + '-' + month + '-' + day; 
    else
        return year + '/' + month + '/' + day;
};

html.make_timestamp_to_string = function () {
    $('.timestamp').each(function (intIndex) {
        var timestamp = $(this).attr('rel');
        var selected_date = new Date(timestamp * 1000);
        var day = selected_date.getDate();
        var month = selected_date.getMonth() + 1;
        var year = selected_date.getFullYear();
        var today = new Date();
        var tday = today.getDate();
        var tmonth = today.getMonth() + 1;
        var tyear = today.getFullYear();
        $(this).removeClass('red');
        if ((day < (tday - 1) && month == tmonth && year == tyear) || (month < tmonth && year == tyear) || (year < tyear)) {
            $(this).addClass('red');
            if (day < 10) { day = '0' + day; }
            if (month < 10) { month = '0' + month; }
            $(this).html(html.showDateByLanguage(day, month, year));
        }
        else if ((day < tday && day > tday - 2) && month == tmonth && year == tyear) {
            $(this).addClass('red');
            $(this).html(language.data.yesterday); 
        }
        else if (day == tday && month == tmonth && year == tyear) {
            $(this).html(language.data.today); 
        }
        else if ((day > tday && day < (tday + 2)) && month == tmonth && year == tyear) {
            $(this).html(language.data.tomorrow); 
        }
        else {
            if (day < 10) { day = '0' + day }
            if (month < 10) { month = '0' + month }
            $(this).html(html.showDateByLanguage(day, month, year));
        }
    });
};

html.getLocalizedDateString = function (date) {
    if (date == undefined || date == null || date == "")
        return "";
    var day = date.getDate();
    var month = date.getMonth() + 1;
    var year = date.getFullYear();
    if (day < 10) { day = '0' + day; }
    if (month < 10) { month = '0' + month; }
    return html.showDateByLanguage(day, month, year);
}

html.getUniversalDateString = function (date) {
    if (date == undefined || date == null || date == "")
        return "";
    var day = date.getDate();
    var month = date.getMonth() + 1;
    var year = date.getFullYear();
    if (day < 10) { day = '0' + day; }
    if (month < 10) { month = '0' + month; }
    return year + '/' + month + '/' + day;
}

html.getDateStringInServiceFormat = function (date) {
    if (date == undefined || date == null || date == "")
        return "";
    var day = date.getDate();
    var month = date.getMonth() + 1;
    var year = date.getFullYear();
    if (day < 10) { day = '0' + day; }
    if (month < 10) { month = '0' + month; }
    return year + '-' + month + '-' + day;
}

html.getWorldWideDate = function (date) {
    if (date == undefined)
        currentLocationDate = new Date(); 
    else
        currentLocationDate = date;
    currentLocationDate.setMinutes(0);
    currentLocationDate.setHours(0);
    currentLocationDate.setSeconds(0); 
    currentLocationDate.setMilliseconds(0);
    var offset = (currentLocationDate.getTimezoneOffset() / 60) * (-1);
    utc = currentLocationDate.getTime() + (currentLocationDate.getTimezoneOffset() * 60000);
    timeZoneLocation = new Date(utc + (3600000 * offset));
    var timestamp = timeZoneLocation.getTime() / 1000;
    timestamp = Math.round(timestamp); 
    return timestamp;
};

html.getMonthName = function (month_number) {
    var month = new Array(12);
    month[0] = "January";
    month[1] = "February";
    month[2] = "March";
    month[3] = "April";
    month[4] = "May";
    month[5] = "June";
    month[6] = "July";
    month[7] = "August";
    month[8] = "September";
    month[9] = "October";
    month[10] = "November";
    month[11] = "December";
    return month[month_number];
};

html.getDayName = function (day_number) {
    var day = new Array(7);
    day[0] = 'Sunday';
    day[1] = 'Monday';
    day[2] = 'Tuesday';
    day[3] = 'Wednesday';
    day[4] = 'Thursday';
    day[5] = 'Friday';
    day[6] = 'Saturday';
    return day[day_number];
};

html.addRemoveDateButton = function (object) {
    $('#ui-datepicker-div div.remove_date').remove();
    $('#ui-datepicker-div').append("<div class='remove_date'>" + language.data.no_date + "</div>");
    $('#ui-datepicker-div div.remove_date').die();
    $('#ui-datepicker-div div.remove_date').live('click', function () {
        object.children('.ui-datepicker-trigger').remove();
        object.children('input.datepicker').remove();
        object.children('.showdate').remove();
        object.children('.description').after("<input type='hidden' class='datepicker'/>");
        html.createDatepicker();
        $('#ui-datepicker-div').hide();
        object.children('.timestamp').attr('rel', '0');

        var oldTask = task.getTask(object);
        var newTask = $.extend(true, {}, oldTask);
        newTask.Due = null;

        taskstore.updateTask(oldTask, newTask);
        
        setTimeout(function () { datePickerOpen = false }, 10);
    });
};

html.createDatepicker = function () {
    var dayNamesEN = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
    var dayNamesMinEN = ['Su', 'Mo', 'Tu', 'We', 'Th', 'Fr', 'Sa'];
    var dayNamesShortEN = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
    var monthNamesEN = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
    var monthNamesShortEN = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
    var dayNamesFR = ['Dimanche', 'Lundi', 'Mardi', 'Mercredi', 'Jeudi', 'Vendredi', 'Samedi'];
    var dayNamesMinFR = ['Di', 'Lu', 'Ma', 'Me', 'Je', 'Ve', 'Sa'];
    var dayNamesShortFR = ['Dim', 'Lun', 'Mar', 'Mer', 'Jeu', 'Ven', 'Sam'];
    var monthNamesFR = ['Janvier', 'Février', 'Mars', 'Avril', 'Mai', 'Juin', 'Juillet', 'Août', 'Septembre', 'Octobre', 'Novembre', 'Décembre'];
    var monthNamesShortFR = ['Jan', 'Fév', 'Mar', 'Avr', 'Mai', 'Jui', 'Jui', 'Aoû', 'Sep', 'Oct', 'Nov', 'Dec'];
    var dayNamesDE = ['Sonntag', 'Montag', 'Dienstag', 'Mittwoch', 'Donnerstag', 'Freitag', 'Samstag'];
    var dayNamesMinDE = ['So', 'Mo', 'Di', 'Mi', 'Do', 'Fr', 'Sa'];
    var dayNamesShortDE = ['Son', 'Mon', 'Din', 'Mit', 'Don', 'Fre', 'Sam'];
    var monthNamesDE = ['Januar', 'Februar', 'März', 'April', 'Mai', 'Juni', 'Juli', 'August', 'September', 'Oktober', 'November', 'Dezember'];
    var monthNamesShortDE = ['Jan', 'Feb', 'Mär', 'Apr', 'Mai', 'Jun', 'Jul', 'Aug', 'Sep', 'Okt', 'Nov', 'Dez'];
    if (language.code == 'de') {
        var dayNamesLang = dayNamesDE;
        var dayNamesMinLang = dayNamesMinDE;
        var dayNamesShortLang = dayNamesShortDE;
        var monthNamesLang = monthNamesDE;
        var monthNamesShortLang = monthNamesShortDE;
    }
    else if (language.code == 'fr') {
        var dayNamesLang = dayNamesFR;
        var dayNamesMinLang = dayNamesMinFR;
        var dayNamesShortLang = dayNamesShortFR;
        var monthNamesLang = monthNamesFR;
        var monthNamesShortLang = monthNamesShortFR;
    }
    else {
        var dayNamesLang = dayNamesEN;
        var dayNamesMinLang = dayNamesMinEN;
        var dayNamesShortLang = dayNamesShortEN;
        var monthNamesLang = monthNamesEN;
        var monthNamesShortLang = monthNamesShortEN;
    }
    $(".datepicker").datepicker({
        constrainInput: true,
        buttonImage: 'css/time.png',
        buttonImageOnly: true,
        buttonText: language.data.choose_date,
        showOn: 'both',
        firstDay: parseInt(settings.getWeekstartday()),
        dayNames: dayNamesLang,
        dayNamesMin: dayNamesMinLang,
        dayNamesShort: dayNamesShortLang,
        monthNames: monthNamesLang,
        monthNamesShort: monthNamesShortLang,
        beforeShow: function () {
            var $edit_li = $(this).parent();
            setTimeout(function () {
                var timestamp = $edit_li.children('.timestamp').attr('rel');
                if (timestamp != undefined && timestamp != 0) {
                    var currentDate = new Date(timestamp * 1000);
                    $edit_li.find('.datepicker').datepicker("setDate", currentDate);
                }
                //html.addRemoveDateButton($edit_li);
            }, 5);
            datePickerOpen = true;
        },
        onChangeMonthYear: function (year, month, inst) {
            var $edit_li = $(this).parent();
            setTimeout(function () {
                //html.addRemoveDateButton($edit_li); 
            }, 5);
        },
        onSelect: function (dateText, inst) {
            setTimeout(function () { datePickerOpen = false }, 10);
            var date = new Date(dateText);
            var timestamp = html.getWorldWideDate(date);
            if ($(this).parent().find('.input-add').length == 1) {
                var $date = $(".add input.datepicker").val();
                var $html = '<span class="showdate timestamp" rel="' + timestamp + '">&nbsp;</span>';
                $('.add .showdate').remove();
                $('.add .input-add').after($html);
                if ($('.add .input-add').val().length > 0)
                    $('.add .input-add').select();
                else
                    $('.add .input-add').focus();
            }
            else {
                var $date = $("li input.datepicker").val();
                var $html = '<span class="showdate timestamp" rel="' + timestamp + '">&nbsp;</span>';
                $(this).parent().find('img.ui-datepicker-trigger').remove();
                if ($(this).parent().find('.showdate').length == 0) {
                    $(this).parent().find('.description').after($html);
                }
                else {
                    $(this).parent().find('.showdate').attr("rel", timestamp);
                    $(this).parent().find('.datepicker').hide();
                }

                var t = task.getTask($(this));
                var oldTask = $.extend(true, {}, t);
                // change the date to the new value
                t.Due = new Date($(this).parent().find('span.timestamp').attr('rel') * 1000);
                taskstore.updateTask(oldTask, t);
            }
            html.make_timestamp_to_string();
        }
    });
};

html.activateDragAndDrop = function (list_id) {
    $("a.list").droppable({ disabled: false });
    $("#lists a#list" + list_id).droppable({ disabled: true });
    $('#bottombar #left a').removeClass('active');
}; 

//$(function () { $('a[href^=http]').live('click', function () { window.open(this.href); return false; }); });

html.serializeDate = function (date) {
    var tzoffset = -date.getTimezoneOffset() * 100 / 60;
    var str = "/Date(" + date.valueOf() + tzoffset.toString() + ")/";
    return str;
};

html.render_lists = function () {
    // default the sort field to Name if it hasn't been set yet
    if (taskstore.sortby == undefined) {
        taskstore.sortby = "Name";
        taskstore.sortreverse = false;
    }

    // create the tab list
    $("div#tasklists").html("<ul id='tablist'></ul>");

    // iterate through the tasklists and create the html for the tabs
    for (var ix in taskstore.TaskLists) {
        var tl = taskstore.TaskLists[ix];
        $("ul#tablist").append("<li><a href='#tabs-" + tl.ID + "'>" + tl.Name + "</a><span class='ui-icon ui-icon-close'>Remove Tab</span></li>");
        $("div#tasklists").append("<div class='tasklist' id='tabs-" + tl.ID + "'>" +
                                  "<ul id='" + tl.ID + "' class='mainlist sortable'>" +
                                  html.headerElement(taskstore.sortby) +
                                  "</ul></div>");

        // build one array of completed and one array of non-completed tasks 
        var tasks = [];
        var completeTasks = [];
        for (var t in tl.Tasks) {
            if (tl.Tasks[t].Complete == false)
                tasks.push(tl.Tasks[t]);
            else
                completeTasks.push(tl.Tasks[t]);
        }

        var sortFunc = function (a, b) {
            var left = a[taskstore.sortby];
            var right = b[taskstore.sortby];

            // take care of null cases.  all nulls sort below non-nulls
            if (left == null && right == null)
                return 0;
            if (left == null) return 1;
            if (right == null) return -1;

            // sort by the selected sort field
            if (left < right)
                return -1;
            if (left > right)
                return 1;
            return 0;
        };

        // sort the array of tasks
        tasks.sort(sortFunc);
        if (taskstore.sortreverse)
            tasks.reverse();

        // sort the array of completed tasks
        completeTasks.sort(sortFunc);
        if (taskstore.sortreverse)
            completeTasks.reverse();

        // iterate through each of the tasks and create the li elements
        for (var jx in tasks) {
            var t = tasks[jx];
            var newTask = "<li class='more' rel='" + t.TaskListID + "' id='" + t.ID + "'>" +
                            html.checkboxElement(t.Complete) +
                            html.priorityIDElement(t.PriorityID) +
                            "<span class='description'>" + t.Name + "</span>" +
                            (t.Due != undefined && t.Due != null ? "<span class='timestamp showdate' rel='" + t.Due.valueOf() / 1000 + "'>" + t.Due + "</span>" : "") +
                            "<span class='icon delete' title='Delete Task'></span>" +
                            "</li>";
            $("ul#" + tl.ID).append(newTask);
        }

        // add a separator line if both arrays are non-empty
        if (tasks.length > 0 && completeTasks.length > 0)
            $("ul#" + tl.ID).append("<hr class='separator' />");

        // iterate through each of the completed tasks and create the li elements
        for (var jx in completeTasks) {
            var t = completeTasks[jx];
            var newTask = "<li class='more' rel='" + t.TaskListID + "' id='" + t.ID + "'>" +
                            html.checkboxElement(t.Complete) +
                            html.priorityIDElement(t.PriorityID) +
                            "<span class='description'>" + t.Name + "</span>" +
                            (t.Due != undefined && t.Due != null ? "<span class='timestamp showdate' rel='" + t.Due.valueOf() / 1000 + "'>" + t.Due + "</span>" : "") +
                            "<span class='icon delete' title='Delete Task'></span>" +
                            "</li>";
            $("ul#" + tl.ID).append(newTask);
        }
    }

    // create a "New List" list (and tab) for new lists
    $("ul#tablist").append("<li><a href='#tabs-newlist'>New List</a></li>");
    $("div#tasklists").append("<div class='tasklist' id='tabs-newlist'>" +
                                  "<ul id='newlist' class='mainlist sortable'></ul></div>");

    // initialize behaviors for all the controls
    init.controls_init();
}

html.headerElement = function (sortField) {
    var arrow = taskstore.sortreverse == true ? "&uarr;" : "&darr;";
    var str = "<li class='tasklistheader'>";
    str += "<span class='header-pri'><b>!</b>" + (sortField == "PriorityID" ? arrow : "") + "</span>";
    str += "<span class='header-name'>Name" + (sortField == "Name" ? arrow : "") + "</span>";
    str += "<span class='header-date'>Date" + (sortField == "Due" ? arrow : "") + "</span>";
    str += "</li>";
    return str;
};