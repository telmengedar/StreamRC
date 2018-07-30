var decay = 1.0;
var isnew = false;

var timer = true;

function loadSettings() {
    var params = new URLSearchParams(window.location.search);

    if (params.has('nobackground')) {
        document.getElementById('box').style.background = "initial";
        document.getElementById('box').style.borderColor = "rgba(0,0,0,0)";
    }

    if (params.has('notransparency')) {
        document.getElementById('box').style.opacity = 255;
    }

    if(params.has('timer'))
        timer = params.get('timer') === "true";
}

function refresh() {
    if (timer)
        decay -= 1.0;

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
    xhr.open('GET', "http://localhost/streamrc/highlight/data", true);
    xhr.responseType = 'json';
    xhr.onload = function () {
        var status = xhr.status;
        if (status === 200 && xhr.response) {
            createHighlight(xhr.response);
        }
    };
    xhr.send();
}

function clearElement(parent) {
    while (parent.firstChild) {
        parent.removeChild(parent.firstChild);
    }
}

function createHighlight(data) {
    clearElement(document.getElementById("titlecontent"));
    clearElement(document.getElementById("textcontent"));
    createMessageElement(data.title.chunks, document.getElementById("titlecontent"));
    createMessageElement(data.message.chunks, document.getElementById("textcontent"));

    decay = 30.0;
    isnew = true;
}

loadSettings();

if(timer)
    setInterval(refresh, 1000);
else
    document.getElementById("box").style.opacity = 255;

loadEventData();
setInterval(loadEventData, 60000);
