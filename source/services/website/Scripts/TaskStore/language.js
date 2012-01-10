/* language.js */

var language = language || {};
language.availableLang = new Array('ar', 'ca', 'cs', 'da', 'de', 'en', 'es', 'fr', 'hr', 'hu', 'it', 'ja', 'ko', 'nl', 'no', 'pl', 'pt', 'ru', 'se', 'sk', 'tr', 'uk', 'zh'); 
language.load = function (code, data, fallback) {
    language.code = code;
    language.data = data; 
    if (language.code != 'en') {
        language.translation = fallback; 
        if (language.translation != '') {
            for (langstring in language.translation) {
                var translation = language.data[langstring]; 
                if (translation == undefined)
                    language.data[langstring] = language.translation[langstring];
            }
        }
    }
}; 

language.switchLang = function (code) {
    if (language.availableLang.join(' ').indexOf(code) == -1)
        language.code = 'en'; 
    else
        language.code = code;
    settings.language = language.code; 
    settings.update();
};

language.replaceBasics = function () {
    $("a.history").text(language.data.history);
    $("a.addtask").text(language.data.add_task);
    $("a.button-add").text(language.data.add_task);
    $('h3 a.add').attr('title', language.data.add_list);
    $('.editp').attr('title', language.data.edit_list);
    $('.savep').attr('title', language.data.save_list);
    $('.deletep').attr('title', language.data.delete_list); 
};


