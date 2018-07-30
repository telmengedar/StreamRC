var messagebackground;
var noborder = false;

function loadSettings() {
    var params = new URLSearchParams(window.location.search);

    if (params.has('background')) {
        messagebackground = params.get('background');
    }
    if (params.has('noborder'))
        noborder = params.get('noborder') !== "false";
}

function removeMessage(element) {
    setTimeout(function () {
        element.parentElement.removeChild(element);
    }, 1000);

}

function loadStatistics() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', "http://localhost/streamrc/statistics/data", true);
    xhr.responseType = 'json';
    xhr.onload = function () {
        var status = xhr.status;
        if (status === 200) {
            createStatistics(xhr.response);
        }
    };
    xhr.send();
}

function createStatistics(data) {
    var flipflop = false;
    for (var i = 0; i < data.statistics.length; ++i) {
        var statistic = data.statistics[i];
        var id = "statistic_" + statistic.name;        
        createStatistic(id, statistic, flipflop);
        flipflop = !flipflop;
    }
}

function createStatistic(id, statistic, flipflop) {
    var mdiv = document.getElementById(id);
    if (mdiv) {
        while (mdiv.firstChild)
            mdiv.removeChild(mdiv.firstChild);
    } else {
        mdiv = document.createElement("p");
        mdiv.id = id;
        mdiv.className = "statistic";

        var messages = document.getElementById("statistics");
        messages.appendChild(mdiv, messages.firstChild);
    }

    if (messagebackground !== null)
        mdiv.style.background = messagebackground;
    if (noborder)
        mdiv.style.borderWidth = 0;

    createMessageElement(statistic.content, mdiv, true);

    if (flipflop && messagebackground === null)
        mdiv.style.background = "linear-gradient(to right, rgba(40, 40, 40, 0.9), rgba(80, 80, 80, 0.4))";
}

loadSettings();
setInterval(loadStatistics, 1000);
