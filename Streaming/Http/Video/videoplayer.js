var playlist = [];

var tag = document.createElement('script');

tag.src = "https://www.youtube.com/iframe_api";
var firstScriptTag = document.getElementsByTagName('script')[0];
firstScriptTag.parentNode.insertBefore(tag, firstScriptTag);

var player;
function onYouTubeIframeAPIReady() {
    player = new YT.Player('player', {
        height: '360',
        width: '640',
        playerVars: {
            autoplay:0,
            controls: 0,
            enablejsapi: 1,
            fs: 0,
            iv_load_policy: 3,
            showinfo:0
        },
        events: {
            'onReady': onPlayerReady,
            'onStateChange': onPlayerStateChange
        }
    });
    document.getElementById('player').style.opacity = 0;
    setInterval(loadVideos, 1000);
}

function onPlayerReady(event) {
    event.target.playVideo();
}

function onPlayerStateChange(event) {
    var playerstate = player.getPlayerState();
    if (playerstate === YT.PlayerState.ENDED || playerstate === YT.PlayerState.CUED) {
        var videoid = player.getVideoData().video_id;
        console.log('stopped ' + videoid);
        videoStopped();
    }
    else if (event.data === YT.PlayerState.PLAYING) {
        document.getElementById('player').style.opacity = 255;
    }
}

function videoStopped() {
    if (playlist.length > 0)
        playNextVideo();
    else {
        document.getElementById('player').style.opacity = 0;
    }
}

function loadVideos() {
    var xhr = new XMLHttpRequest();
    xhr.open('GET', "http://localhost/streamrc/video/videos", true);
    xhr.responseType = 'json';
    xhr.onload = function () {
        var status = xhr.status;
        if (status === 200) {
            addVideos(xhr.response);
        }
    };
    xhr.send();

}

function addVideos(loadedvideos) {
    for (var i = 0; i < loadedvideos.videos.length; ++i)
        playlist.push(loadedvideos.videos[i]);

    var playerstate = player.getPlayerState();
    if (playlist.length > 0 &&  (playerstate <= 0||playerstate===5))
        playNextVideo();
}

function playNextVideo() {
    var video = playlist[0];
    playlist.splice(0, 1);

    document.getElementById('player').style.opacity = 255;

    var parameters = {
        videoId: video.id
    };

    if (video.startseconds > 0)
        parameters.startSeconds = video.startseconds;
    if (video.endseconds > 0)
        parameters.endSeconds = video.endseconds;

    player.loadVideoById(parameters);
}
