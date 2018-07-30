var decay = 1.0;
var isnew = false;

var timer = true;
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

function initialize() {
    if (!notransparency) {
        for (var i = 0; i < 5; ++i) {
            document.getElementById('result' + i).style.opacity = 0.25 + (1.0 - i / 4) * 0.5;
        }
    }
}

function refresh() {
    if (timer)
        //decay -= 1.0;

    if (decay <= 0.0) {
        document.getElementById("box").style.opacity = 0;
    }
    else if (isnew) {
        document.getElementById("box").style.opacity = 255;
        isnew = false;
    }
}

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

function createEvents(data) {
    var i;
    for (i = 0; i < data.events.length; ++i) {
        var item = data.events[i];
        clearElement(document.getElementById("titlecontent" + i));
        clearElement(document.getElementById("textcontent" + i));
        createMessageElement(item.title.chunks, document.getElementById("titlecontent" + i));
        createMessageElement(item.message.chunks, document.getElementById("textcontent" + i));
        document.getElementById("result" + i).style.visibility = "visible";
    }
    for (i = data.events.length; i < 5; ++i) {
        clearElement(document.getElementById("titlecontent" + i));
        clearElement(document.getElementById("textcontent" + i));
        document.getElementById("result" + i).style.visibility = "hidden";
    }

    decay = 30.0;
    isnew = true;
}

loadSettings();
initialize();

if(timer)
    setInterval(refresh, 1000);
else
    document.getElementById("box").style.opacity = 255;

loadEventData();
setInterval(loadEventData, 60000);
