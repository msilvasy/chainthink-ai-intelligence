let player;

function onYouTubeIframeAPIReady() {
  if (!window.videoId) return;

  player = new YT.Player("player", {
    height: "315",
    width: "560",
    videoId: window.videoId,
    events: {
      onReady: onPlayerReady,
      onStateChange: onPlayerStateChange
    }
  });
}

function onPlayerReady(event) {
  console.log("Player is ready", event.target);
}

async function onPlayerStateChange(event) {
    let userGuid =JSON.parse(localStorage.getItem("user"));
     if(!userGuid){
      await registerUser();
    }
     userGuid =JSON.parse(localStorage.getItem("user")); 
     
     let youTubeVideoId = window.videoId;
  
    if (!userGuid || !youTubeVideoId) return;
  
    const payload = {
      userId: userGuid.guid,
      youTubeVideoId: youTubeVideoId,
      watchTimeInSecond: Math.floor(event.target.getCurrentTime()),
      videoDuration: Math.floor(event.target.getDuration())
    };
  
    if (
      event.data === YT.PlayerState.PLAYING ||
      event.data === YT.PlayerState.PAUSED ||
      event.data === YT.PlayerState.ENDED
    ) {
      console.log("Sending watch event:", payload);
  
      try {
        const response =  await fetch(`${apiUrl}AddUserVideoWatchTime`, {
          method: "POST",
          headers: { "Content-Type": "application/json" },
          body: JSON.stringify(payload)
        });

       await getVideoProgress();

      } catch (err) {
        console.error("Failed to send watch time:", err);
      }
    }
  }
  

function stopVideo() {
  if (player) {
    player.pauseVideo();
  }
}

function closePlayer() {
  document.getElementById("player").innerHTML = "";
  console.log("closeYTPlayer", true);
}



///

// let player;
// let watchInterval;
// let watchedSeconds = new Set();

// function onYouTubeIframeAPIReady() {
//   if (!window.videoId) return;

//   player = new YT.Player("player", {
//     height: "315",
//     width: "560",
//     videoId: window.videoId,
//     events: {
//       onReady: onPlayerReady,
//       onStateChange: onPlayerStateChange
//     }
//   });
// }

// function onPlayerReady(event) {
//   console.log("Player is ready", event.target);
// }

// function onPlayerStateChange(event) {
//   if (event.data === YT.PlayerState.PLAYING) {
//     // Start tracking every second
//     if (!watchInterval) {
//       watchInterval = setInterval(trackWatchProgress, 1000);
//     }
//   } else {
//     // Pause or stop tracking when not playing
//     clearInterval(watchInterval);
//     watchInterval = null;

//     if (event.data === YT.PlayerState.ENDED) {
//       sendFinalWatchData();
//     }
//   }
// }

// function trackWatchProgress() {
//   if (!player) return;
//   const currentSecond = Math.floor(player.getCurrentTime());
//   watchedSeconds.add(currentSecond); // mark this second as watched
// }

// async function sendFinalWatchData() {
//   let userGuid = JSON.parse(localStorage.getItem("user"));
//   if (!userGuid) {
//     await registerUser();
//     userGuid = JSON.parse(localStorage.getItem("user"));
//   }

//   if (!userGuid || !window.videoId) return;

//   const duration = Math.floor(player.getDuration());
//   const watchedCount = watchedSeconds.size;
//   const watchedPercent = Math.round((watchedCount / duration) * 100);

//   const payload = {
//     userId: userGuid.guid,
//     youTubeVideoId: window.videoId,
//     videoDuration: duration,
//     watchedSeconds: watchedCount,
//     watchedPercent: watchedPercent
//   };

//   console.log("Sending final watch data:", payload);

//   try {
//     await fetch(`${apiUrl}AddUserVideoWatchTime`, {
//       method: "POST",
//       headers: { "Content-Type": "application/json" },
//       body: JSON.stringify(payload)
//     });

//     await getVideoProgress();
//   } catch (err) {
//     console.error("Failed to send final watch data:", err);
//   }
// }

// function stopVideo() {
//   if (player) {
//     player.pauseVideo();
//   }
// }

// function closePlayer() {
//   document.getElementById("player").innerHTML = "";
//   console.log("closeYTPlayer", true);
// }
