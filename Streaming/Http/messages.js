function createMessageElement(message, parent, nopre) {
    for (var i = 0; i < message.length; ++i) {
        var chunk = message[i];
        if (chunk.type === 0) {
            var text = document.createElement("span");
            if (chunk.color)
                text.style.color = chunk.color;
            else text.style.color = "#FFFFFF";

            if (chunk.fontweight !== 0) {
                if (chunk.fontWeight === 1)
                    text.style.fontWeight = "lighter";
                else if (chunk.fontWeight === 2)
                    text.style.fontWeight = "normal";
                else text.style.fontWeight = "bold";
            }

            if (nopre)
                text.textContent = chunk.content;
            else {
                var pre = document.createElement("pre");
                pre.textContent = chunk.content;
                text.appendChild(pre);
            }
            text.className = "element";
            parent.appendChild(text);
        }
        else if(chunk.type===1) {
            var emote = document.createElement("img");
            if(!isNaN(chunk.content))
                emote.setAttribute("src", "http://localhost/streamrc/image?id=" + chunk.content);
            else emote.setAttribute("src", chunk.content);
            emote.className = "element";
            parent.appendChild(emote);
        }
        else if (chunk.type === 2) {

            var image = document.createElement("img");
            if (!isNaN(chunk.content))
                image.setAttribute("src", "http://localhost/streamrc/image?id=" + chunk.content);
            else image.
                setAttribute("src", chunk.content);
            image.className = "imageattachement";
            parent.appendChild(image);
        }
    }
}

function clearMessage(message) {
    while (message.firstChild)
        message.removeChild(message.firstChild);
}