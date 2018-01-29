var isnew = false;
var time = 0;
var timerenabled = true;

function refresh() {
    if (!timerenabled) {
        if (isnew) {
            document.getElementById("box").style.opacity = 255;
            isnew = false;
        }
        return;
    }

    if (time <= 0.0)
        return;

    time -= 1.0;

    if (time <= 0.0) {
        var element = document.getElementById("box");
        element.style.opacity = 0;
    }
    else if (isnew) {
        document.getElementById("box").style.opacity = 255;
        isnew = false;
    }
}

function mix(color1, color2, ratio) {
    return "rgb(" + Math.floor(((1.0 - ratio) * color1[0]) + (ratio * color2[0])) + "," + Math.floor(((1.0 - ratio) * color1[1]) + (ratio * color2[1])) + "," + Math.floor(((1.0 - ratio) * color1[2]) + (ratio * color2[2])) + ")";
}

function getColor(value) {
    if (value <= 5)
        return mix([255, 121, 124], [255, 255, 81], value / 5.0);
    else return mix([255, 255, 81], [85, 255, 85], (value - 5.0) / 5.0);
}

function clearReview() {
    var element = document.getElementById("items");
    while (element.firstChild)
        element.removeChild(element.firstChild);
}

function createReview(review) {
    if (review == null)
        return;

    clearReview();

    var itemselement = document.getElementById("items");
    for (var i = 0; i < review.items.length; ++i) {
        var row = document.createElement("div");
        row.className = "row";
        row.style.color = getColor(review.items[i].value);

        var value = document.createElement("div");
        value.className = "name";
        value.textContent = review.items[i].category;
        row.appendChild(value);

        value = document.createElement("div");
        value.className = "weight";
        value.textContent = review.items[i].weight;
        row.appendChild(value);

        value = document.createElement("div");
        value.className = "value";
        value.textContent = review.items[i].value;
        row.appendChild(value);

        itemselement.appendChild(row);
    }

    document.getElementById("resultvalue").textContent = review.result;
    document.getElementById("result").style.color = getColor(review.result);

    isnew = true;
    timerenabled = review.timeoutenabled;
    if (timerenabled)
        time = 20.0;
}

function loadReview() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', "http://localhost/streamrc/reviews/data", true);
    xhr.responseType = 'json';
    xhr.onload = function () {
        var status = xhr.status;
        if (status === 200) {
            createReview(xhr.response);
        }
    };
    xhr.send();
}

setInterval(refresh, 1000);
setInterval(loadReview, 1000);
