var itemcount = 0;
var notransparency = false;

function loadSettings() {
    var params = new URLSearchParams(window.location.search);

    if (params.has('align'))
        document.getElementById('box').style.flexDirection = params.get('align');
    if (params.has('element-align')) {
        for (var i = 0; i < 5; ++i)
            document.getElementById('result' + i).style.flexDirection = params.get('element-align');
    }

    if (params.has('nobackground')) {
        for (var i = 0; i < 5; ++i) {
            document.getElementById('result' + i).style.background = "initial";
            document.getElementById('result' + i).style.borderColor = "rgba(0,0,0,0)";
        }
    }

    if (params.has('notransparency')) {
        notransparency = true;
    }

    if (params.has('items'))
        itemcount = parseInt(params.get('items'), 10);

    if (itemcount > 0) {
        var box = document.getElementById('box');
        if (itemcount < 7)
            box.removeChild(document.getElementById('support'));
        if (itemcount < 6)
            box.removeChild(document.getElementById('social'));
        if (itemcount < 5)
            box.removeChild(document.getElementById('hoster'));
        if (itemcount < 4)
            box.removeChild(document.getElementById('donor'));
        if (itemcount < 3)
            box.removeChild(document.getElementById('leader'));
        if (itemcount < 2)
            box.removeChild(document.getElementById('lastdonation'));
        if (itemcount < 1)
            box.removeChild(document.getElementById('lastevent'));
    }
}

/*function initialize() {
    if (!notransparency) {
        for (var i = 0; i < 5; ++i) {
            document.getElementById('result' + i).style.opacity = 0.25 + (1.0 - i / 4) * 0.5;
        }
    }
}*/

function loadEventData() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', "http://localhost/streamrc/events/data?count=" + itemcount, true);
    xhr.responseType = 'json';
    xhr.onload = function () {
        var status = xhr.status;
        if (status === 200 && xhr.response) {
            createEvents(xhr.response);
        }
    };
    xhr.send();
}

function clearElement(parent) {
    while (parent.firstChild) {
        parent.removeChild(parent.firstChild);
    }
}

function createEvent(event, id) {
    if (document.getElementById(id) === null)
        return;

    clearElement(document.getElementById(id + "titlecontent"));
    clearElement(document.getElementById(id + "textcontent"));
    createMessageElement(event.title.chunks, document.getElementById(id + "titlecontent"));
    createMessageElement(event.message.chunks, document.getElementById(id + "textcontent"));
}

function createEvents(data) {
    createEvent(data.leader, "leader");
    createEvent(data.donor, "donor");
    createEvent(data.hoster, "hoster");
    createEvent(data.social, "social");
    createEvent(data.support, "support");
    createEvent(data.lastevent, "lastevent");
    createEvent(data.lastdonation, "lastdonation");
}

loadSettings();
//initialize();

loadEventData();
setInterval(loadEventData, 60000);
