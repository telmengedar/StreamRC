var messages = [];
var timestamp = 0;

var messagebackground;
var noborder = false;

var flipflop = false;

function loadSettings() {
    var params = new URLSearchParams(window.location.search);

    if (params.has('notitle')) {
        document.getElementById('messages').removeChild(document.getElementById('title'));
    }

    if (params.has('background')) {
        messagebackground = params.get('background');
    }
    if (params.has('noborder'))
        noborder = params.get('noborder') !== "false";
}

function refresh() {
    for (var i = messages.length - 1; i >= 0; --i) {
        if (i > 5)
            continue;

        var message = messages[i];
        message.time -= 1.0;
        if (message.time <= 0.0) {
            var element = document.getElementById(message.id);
            element.style.opacity = 0;
            removeMessage(element);
            messages.splice(i, 1);
            if (messages.length === 0)
                document.getElementById("messages").style.opacity = 0;
        }
        else if (message.isnew) {
            if (messages.length > 0)
                document.getElementById("messages").style.opacity = 0.8;
            document.getElementById(message.id).style.opacity = 255;
            message.isnew = false;
        }
    }
}

function removeMessage(element) {
    setTimeout(function () {
        element.parentElement.removeChild(element);
    }, 1000);

}

function loadMessages() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', "http://localhost/streamrc/chat/messages?timestamp=" + timestamp, true);
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
    for (var i = 0; i < loadedmessages.messages.length; ++i) {
        var id = timestamp + "-" + i;
        var message = loadedmessages.messages[i];
        messages.push({
            id: id,
            time: 15.0,
            isnew: true,
            flipflop: flipflop
        });

        flipflop = !flipflop;
        createMessage(id, message);
    }
}

function createMessage(id, message) {
    var mdiv = document.createElement("p");
    mdiv.id = id;
    mdiv.className = "message";
    if (messagebackground !== null)
        mdiv.style.background = messagebackground;
    if (noborder)
        mdiv.style.borderWidth = 0;

    createMessageElement(message.content, mdiv, true);

    if (message.flipflop && messagebackground === null)
        mdiv.style.background = "linear-gradient(to right, rgba(40, 40, 40, 0.9), rgba(80, 80, 80, 0.4))";

    var messages = document.getElementById("messages");
    messages.appendChild(mdiv, messages.firstChild);
}

loadSettings();
setInterval(refresh, 1000);
setInterval(loadMessages, 1000);
