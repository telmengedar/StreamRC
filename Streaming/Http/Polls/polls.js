var poll;
var decay = 0;
var isnew = false;

function refresh() {
    if (!poll)
        return;

    decay -= 1.0;

    if (decay <= 0.0) {
        var element = document.getElementById("box");
        element.style.opacity = 0;
        poll = null;
    }
    else if (isnew) {
        document.getElementById("box").style.opacity = 255;
        isnew = false;
    }
}

function loadPollData() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', "http://localhost/streamrc/polls/data", true);
    xhr.responseType = 'json';
    xhr.onload = function () {
        var status = xhr.status;
        if (status === 200 && xhr.response) {
            createPoll(xhr.response);
        }
    };
    xhr.send();
}

function createPoll(data) {
    if (poll && poll.name !== data.name)
        return;

    poll = data;
    document.getElementById("title").textContent = poll.description;
    for (var i = 0; i < poll.items.length; ++i) {
        var item = poll.items[i];
        document.getElementById("description" + i).textContent = item.item;
        document.getElementById("description" + i).style.visibility = "visible";
        document.getElementById("votes" + i).textContent = item.count;
        document.getElementById("bar" + i).style.height = "calc(" + (item.percentage * 100.0) + "% - 40px)";
        document.getElementById("bar" + i).style.visibility = "visible";
    }
    for (var i = poll.items.length; i < 5; ++i) {
        document.getElementById("description" + i).textContent = "";
        document.getElementById("description" + i).style.visibility = "hidden";
        document.getElementById("votes" + i).textContent = "";
        document.getElementById("bar" + i).style.visibility = "hidden";
    }
    decay = 30.0;
    isnew = true;
}

setInterval(refresh, 1000);
setInterval(loadPollData, 1000);
