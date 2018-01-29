var poll;
var decay = 0;
var isnew = false;
var init = true;

var timer = true;
var itemcount = 5;

function loadSettings() {
    var params = new URLSearchParams(window.location.search);

    if (params.has('titlealign'))
        document.getElementById('title').style.textAlign = params.get('titlealign');

    if(params.has('timer'))
        timer = params.get('timer') === "true";

    if (params.has('items'))
        itemcount = parseInt(params.get('items'), 10);
}

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
    xhr.open('GET', "http://localhost/streamrc/polls/data?items=" + itemcount + (init ? "&init=true" : ""), true);
    xhr.responseType = 'json';
    xhr.onload = function () {
        var status = xhr.status;
        if (status === 200 && xhr.response) {
            createPoll(xhr.response);
            init = false;
        }
    };
    xhr.send();
}

function createPoll(data) {
    if (timer && poll && poll.name !== data.name)
        return;

    poll = data;
    document.getElementById("title").textContent = poll.description;

    var i;
    for (i = 0; i < poll.items.length; ++i) {
        var item = poll.items[i];
        document.getElementById("description" + i).textContent = item.item;
        document.getElementById("description" + i).style.visibility = "visible";
        document.getElementById("votes" + i).textContent = item.count;
        document.getElementById("bar" + i).style.width = (item.percentage * 100.0) + "%";
        document.getElementById("bar" + i).style.visibility = "visible";
    }
    for (i = poll.items.length; i < 5; ++i) {
        document.getElementById("description" + i).textContent = "";
        document.getElementById("description" + i).style.visibility = "hidden";
        document.getElementById("votes" + i).textContent = "";
        document.getElementById("bar" + i).style.visibility = "hidden";
    }
    decay = 30.0;
    isnew = true;
}

loadSettings();

if(timer)
    setInterval(refresh, 1000);
else
    document.getElementById("box").style.opacity = 255;

setInterval(loadPollData, 1000);
