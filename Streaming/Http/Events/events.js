var itemcount = 5;
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

    if(params.has('timer'))
        timer = params.get('timer') === "true";

    if (params.has('items'))
        itemcount = parseInt(params.get('items'), 10);
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
