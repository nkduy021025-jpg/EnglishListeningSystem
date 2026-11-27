(function () {
    var form = document.getElementById("dictation-form");
    if (!form) {
        return;
    }

    var panel = document.getElementById("dictation");
    var sentences = JSON.parse(panel.dataset.sentences);
    var completed = new Set(JSON.parse(panel.dataset.completed));
    var index = Number(panel.dataset.sentencePosition) - 1;
    var previous = document.getElementById("previous-sentence");
    var next = document.getElementById("next-sentence");
    var position = document.getElementById("sentence-position");
    var completionBadge = document.getElementById("sentence-completed");
    var progress = document.getElementById("lesson-progress");
    var answer = document.getElementById("transcriptInput");
    var check = document.getElementById("check-transcript");
    var result = document.getElementById("transcript-result");
    var feedback = document.getElementById("dictation-feedback");
    var words = document.getElementById("transcript-words");
    var reveal = document.getElementById("show-transcript");
    var transcript = document.getElementById("current-transcript");
    var transcriptRows = document.querySelectorAll(".practice-transcript li");
    var ratingPosition = document.getElementById("rating-sentence-position");
    var audioPlayer = document.getElementById("audioPlayer");
    var play = document.getElementById("play-sentence");
    var replay = document.getElementById("replay-sentence");
    var rate = document.getElementById("playback-rate");
    var audioMessage = document.getElementById("audio-message");
    var frame = 0;
    var ctrlAlone = false;
    var pending = false;

    function updateNavigation() {
        position.textContent = "Sentence " + (index + 1) + " / " + sentences.length;
        completionBadge.hidden = !completed.has(sentences[index].Id);
        previous.disabled = pending || index === 0;
        next.disabled = pending || index === sentences.length - 1;
    }

    function hideTranscript() {
        reveal.textContent = "Show Transcript";
        reveal.setAttribute("aria-expanded", "false");
        transcript.hidden = true;
    }

    function showSentence(newIndex) {
        if (audioPlayer) {
            audioPlayer.pause();
            audioMessage.hidden = true;
        }
        index = newIndex;
        form.elements.maCau.value = sentences[index].Id;
        // Giữ vị trí câu khi gửi phần đánh giá bài học.
        ratingPosition.value = index + 1;
        answer.value = "";
        result.hidden = true;
        hideTranscript();
        updateNavigation();
        answer.focus();
    }

    function toggleTranscript() {
        if (!transcript.hidden) {
            hideTranscript();
        } else {
            transcript.textContent = transcriptRows[index].textContent;
            transcript.hidden = false;
            reveal.textContent = "Hide Transcript";
            reveal.setAttribute("aria-expanded", "true");
        }
    }

    function showAudioError(message) {
        audioMessage.textContent = message;
        audioMessage.hidden = false;
    }

    function stopAtEnd() {
        // Dừng ở mốc cuối phụ đề, kể cả khi người học đổi tốc độ.
        var endTime = sentences[index].End;
        if (!audioPlayer.paused && audioPlayer.currentTime >= endTime) {
            audioPlayer.pause();
            audioPlayer.currentTime = endTime;
        }
    }

    function watchPlayback() {
        stopAtEnd();
        if (!audioPlayer.paused) {
            frame = requestAnimationFrame(watchPlayback);
        }
    }

    function showPlayState() {
        var playing = !audioPlayer.paused;
        play.querySelector("span").textContent = playing ? "Pause" : "Play";
        play.querySelector("i").classList.toggle("bi-pause-fill", playing);
        play.querySelector("i").classList.toggle("bi-play-fill", !playing);
        cancelAnimationFrame(frame);
        if (playing) {
            frame = requestAnimationFrame(watchPlayback);
        }
    }

    async function playFromStart() {
        var sentence = sentences[index];
        audioMessage.hidden = true;
        if (!Number.isFinite(sentence.Start) || !Number.isFinite(sentence.End) || sentence.Start < 0
            || sentence.Start >= sentence.End || sentence.Start >= audioPlayer.duration) {
            showAudioError("The audio segment for this sentence is unavailable.");
            return;
        }

        try {
            audioPlayer.currentTime = sentence.Start;
            await audioPlayer.play();
        } catch (error) {
            if (error.name !== "AbortError") {
                showAudioError("The audio could not be played. Please try again.");
            }
        }
    }

    function togglePlayback() {
        if (audioPlayer.paused) {
            playFromStart();
        } else {
            audioPlayer.pause();
        }
    }

    function handleShortcutStart(event) {
        if (event.key === "Control") {
            if (!event.repeat && !event.altKey && !event.shiftKey && !event.metaKey) {
                ctrlAlone = true;
            }
        } else {
            ctrlAlone = false;
        }
    }

    function handleShortcutEnd(event) {
        if (event.key === "Control" && ctrlAlone && panel.classList.contains("active")) {
            ctrlAlone = false;
            playFromStart();
        }
    }

    function paintResult(message, isCorrect, comparison) {
        feedback.textContent = message;
        feedback.classList.toggle("text-warning", isCorrect);
        words.replaceChildren();

        for (var i = 0; i < comparison.length; i++) {
            var word = comparison[i];
            var item = document.createElement("li");
            item.classList.add("transcript-word");
            if (word.status === "correct") {
                item.classList.add("transcript-word-correct");
                item.textContent = "✓ " + word.expected;
            } else {
                item.classList.add("transcript-word-error");
                if (word.status === "missing") {
                    item.textContent = "Missing: " + word.expected;
                } else if (word.status === "extra") {
                    item.textContent = "Extra: " + word.actual;
                } else {
                    item.textContent = word.actual + " → " + word.expected;
                }
            }
            words.appendChild(item);
        }
        words.hidden = comparison.length === 0;
        result.hidden = false;
    }

    async function checkTranscript(event) {
        event.preventDefault();
        if (pending) {
            return;
        }
        if (!answer.value.trim()) {
            paintResult("Enter what you hear before checking your answer.", false, []);
            answer.focus();
            return;
        }

        var body = new URLSearchParams(new FormData(form));
        pending = true;
        check.disabled = true;
        updateNavigation();

        // Gửi token chống giả mạo cùng transcript và cập nhật ngay trên trang.
        try {
            var response = await fetch(form.action, { method: "POST", body: body });
            if (response.redirected || response.status === 401 || response.status === 403) {
                paintResult("Your session has ended. Log in again to check your answer.", false, []);
                return;
            }
            var data = {};
            if (response.headers.get("content-type")?.includes("application/json")) {
                data = await response.json();
            }
            if (!response.ok) {
                paintResult(data.message || "Your answer could not be checked. Please reload the lesson and try again.", false, []);
                return;
            }

            if (data.sentenceCompleted) {
                completed.add(sentences[index].Id);
            }
            var percent = data.progress.toLocaleString("en", { maximumFractionDigits: 2 });
            progress.textContent = data.completedCount + " / " + sentences.length + " sentences completed (" + percent + "%)";
            var message = "Some words need another listen.";
            if (data.correct) {
                message = data.pointsAwarded > 0 ? "Correct! +1 point" : "Correct!";
            }
            paintResult(message, data.correct, data.words);
        } catch (error) {
            paintResult("Your answer could not be checked. Please try again.", false, []);
        } finally {
            pending = false;
            check.disabled = false;
            updateNavigation();
        }
    }

    if (audioPlayer) {
        play.addEventListener("click", togglePlayback);
        replay.addEventListener("click", playFromStart);
        rate.addEventListener("change", function () {
            audioPlayer.playbackRate = Number(rate.value);
        });
        audioPlayer.addEventListener("play", showPlayState);
        audioPlayer.addEventListener("pause", showPlayState);
        audioPlayer.addEventListener("ended", showPlayState);
        audioPlayer.addEventListener("timeupdate", stopAtEnd);
        audioPlayer.addEventListener("error", function () {
            showAudioError("The audio could not be loaded. Please reload the lesson and try again.");
        });
        document.getElementById("dictation-tab").addEventListener("hide.bs.tab", function () {
            audioPlayer.pause();
        });
        document.addEventListener("keydown", handleShortcutStart);
        document.addEventListener("keyup", handleShortcutEnd);
        window.addEventListener("blur", function () {
            ctrlAlone = false;
        });
    }

    previous.addEventListener("click", function () { showSentence(index - 1); });
    next.addEventListener("click", function () { showSentence(index + 1); });
    reveal.addEventListener("click", toggleTranscript);
    form.addEventListener("submit", checkTranscript);
})();
