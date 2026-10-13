//const apiUrl = "https://localhost:5001/api/B2BSite/";
const apiUrl = "https://listenpay.online/api/B2BSite/";
async function loadVideo() {
    try {
        let response = await fetch(`${apiUrl}GetVideo`);
        if (!response.ok) throw new Error("API error");

        let video = await response.json();

        window.videoId = video.videoId;

        //document.getElementById("videoModalLabel").innerText = video.title;

        loadComments(video.comments);

        if (typeof YT !== "undefined" && YT.Player) {
            new YT.Player("player", {
                height: "315",
                width: "560",
                videoId: video.videoId,
                events: {
                    onReady: onPlayerReady,
                    onStateChange: onPlayerStateChange
                }
            });
        }

        await registerUser();
        getVideoProgress();

    } catch (error) {
        console.error("Failed to load video:", error);
    }
}

function loadComments(comments) {
    let list = document.getElementById("commentsList");
    if (!list) return;

    list.innerHTML = "";

    if (!comments || comments.length === 0) {
        list.innerHTML = "<p>No comments yet.</p>";
        return;
    }

    comments.forEach(c => {
        let div = document.createElement("div");
        div.className = "p-2 border rounded bg-secondary mb-2";
        div.innerHTML = `<strong>${c.user?.email ?? "Anonymous"}</strong><br>${c.commentText}`;
        list.appendChild(div);
    });
}

if (document.getElementById('videoModal') != null) {
    document.addEventListener("DOMContentLoaded", () => {
        var myModal = new bootstrap.Modal(document.getElementById('videoModal'));
        myModal.show();
        loadVideo();
    });
    document.getElementById('videoModal').addEventListener('hidden.bs.modal', function () {
        document.querySelectorAll('modal-backdrop').forEach(el => el.remove());
        document.body.classList.remove('modal-open');
        document.body.style = "";
    });
}
async function registerUser() {
    let userGuid = JSON.parse(localStorage.getItem("user"));
    if (userGuid && userGuid.guid)
        return;

    const userPayload = {
        guid: userGuid?.guid || "",
        username: userGuid?.userName || "",
        email: userGuid?.email || ""
    };

    const userResponse = await fetch(`${apiUrl}AddUser`, {
        method: "POST",
        headers: { "Content-Type": "application/json" },
        body: JSON.stringify(userPayload)
    });

    const userData = await userResponse.json();

    if (!userGuid) {
        localStorage.setItem("user", JSON.stringify(userData));
    }
}

async function getVideoProgress() {

    let userGuid = JSON.parse(localStorage.getItem("user"));
    if (userGuid && userGuid.guid) {
        let response = await fetch(`${apiUrl}GetVideoProgress?userId=${userGuid.guid}&videoId=${window.videoId}`);
        if (!response.ok) throw new Error("API error");

        window.videoProgress = await response.json();
        document.getElementById("vprogress").textContent = " " + Math.floor(window.videoProgress) + "%";

    }
}
async function addComment() {
    document.getElementById("addCommentBtn").disabled = true
    document.getElementById("addCommentBtn").textContent = "Adding...";
    const commentInput = document.getElementById("commentInput");
    const commentText = commentInput.value.trim();
    let userGuid = JSON.parse(localStorage.getItem("user"));
    if (!userGuid) {
        await registerUser();
    }
    userGuid = JSON.parse(localStorage.getItem("user"));

    if (!commentText) {
        alert("Please enter a comment.");
        return;
    }



    const payload = {
        commentText: commentText,
        videoId: window.videoId,
        userId: userGuid.guid
    };

    try {
        const response = await fetch(`${apiUrl}AddComment`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify(payload)
        });

        if (!response.ok) {
            throw new Error("Failed to add comment.");
        }

        const result = await response.json();
        loadVideo();
        commentInput.value = ""; // clear input
        document.getElementById("addCommentBtn").disabled = false;
        document.getElementById("addCommentBtn").textContent = "Post Comment to Earn ListenPay";

    } catch (error) {
        document.getElementById("addCommentBtn").textContent = "Post Comment to Earn ListenPay";
        document.getElementById("addCommentBtn").disabled = false;
        console.error("Error adding comment:", error);
    }
}

//document.getElementById("addCommentBtn").addEventListener("click", addComment);

$(document).ready(function () {
    $('#contact-form').submit(function (event) {
        event.preventDefault();       
        var formArray = $('#contact-form').serializeArray();
        $.post('/WebAPI/SendEmail', formArray, function (result) {
            alert('Message sent successfully!');
            $('#contact-form').find("input,textarea").val('');
        }).fail(function () {
            alert('There is a error in sending Message.');
        });       
    });    
});