function createMessageElement(message, parent) {
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

            text.textContent = chunk.content;
            text.className = "element";
            parent.appendChild(text);
        } else {
            var image = document.createElement("img");
            image.setAttribute("src", "http://localhost/streamrc/image?id=" + chunk.content);
            image.className = "element";
            parent.appendChild(image);
        }
    }
}