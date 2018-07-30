var messages = [];
var timestamp = 0;

function refresh() {
    if (messages.length === 0)
        return;

    var message = messages[0];
    message.time -= 1.0;

    if (message.time <= 0.0) {
        var element = document.getElementById("box");
        element.style.opacity = 0;
        removeMessage();
        messages.splice(0, 1);
    }
    else if (message.isnew) {
        document.getElementById("box").style.opacity = 255;
        message.isnew = false;
    }
}

function removeMessage() {
    setTimeout(function () {
        clearMessage(document.getElementById("titlecontent"));
        clearMessage(document.getElementById("textcontent"));
        createNotification();
    }, 1000);
}

function createNotification() {
    if (messages.length === 0)
        return;

    // check if there is already an notification displaying
    var element = document.getElementById("textcontent");
    if (element.firstChild)
        return;

    var message = messages[0];

    createMessageElement(message.title.chunks, document.getElementById("titlecontent"), true);
    createMessageElement(message.text.chunks, element, true);
}

function loadMessages() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', "http://localhost/streamrc/notifications/data?timestamp=" + timestamp, true);
    xhr.responseType = 'json';
    xhr.onload = function () {
        var status = xhr.status;
        if (status === 200) {
            createMessages(xhr.response);
        }
    };
    xhr.send();
}

function createMessages(loadedmessages) {
    timestamp = loadedmessages.timestamp;
    for (var i = 0; i < loadedmessages.notifications.length; ++i) {
        var message = loadedmessages.notifications[i];
        messages.push({
            time: 15.0,
            title: message.title,
            text: message.text,
            isnew: true
        });
    }
    createNotification();
}

setInterval(refresh, 1000);
setInterval(loadMessages, 1000);
