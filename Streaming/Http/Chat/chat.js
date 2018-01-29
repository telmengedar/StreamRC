var messages = [];
var timestamp = 0;

var titlevisible = true;
var messagebackground;
var noborder = false;

function loadSettings() {
    var params = new URLSearchParams(window.location.search);

    if (params.has('notitle')) {
        titlevisible = false;
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
            if (titlevisible && messages.length === 0)
                document.getElementById("title").style.opacity = 0;
        }
        else if (message.isnew) {
            if (titlevisible && messages.length > 0)
                document.getElementById("title").style.opacity = 255;
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
            isnew: true
        });

        createMessage(id, message);
    }
}

function createMessage(id, message) {
    var mdiv = document.createElement("p");
    mdiv.id = id;
    mdiv.className = "message";
    if (messagebackground !== null)
        mdiv.style.backgroundColor = messagebackground;
    if (noborder)
        mdiv.style.borderWidth = 0;

    createMessageElement(message.content, mdiv);

    var messages = document.getElementById("messages");
    messages.appendChild(mdiv, messages.firstChild);
}

loadSettings();
setInterval(refresh, 1000);
setInterval(loadMessages, 1000);
