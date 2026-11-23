(() => {
    const panel = document.getElementById("dictation");
    const sentences = JSON.parse(panel.dataset.sentences);
    const audio = document.getElementById("lesson-audio");
    const rate = document.getElementById("playback-rate");
    const previous = document.getElementById("previous-sentence");
    const next = document.getElementById("next-sentence");
    const position = document.getElementById("sentence-position");
    const form = document.getElementById("dictation-form");
    const answer = document.getElementById("dictation-answer");
    const feedback = document.getElementById("dictation-feedback");
    const skip = document.getElementById("skip-sentence");
    const ratingPosition = document.getElementById("rating-sentence-position");
    let index = Number(panel.dataset.sentencePosition) - 1;

    function currentRange() {
        const sentence = sentences[index];
        if (!sentence || sentence.Start < 0 || sentence.End <= sentence.Start || sentence.Start >= audio.duration) {
            return null;
        }
        return sentence;
    }

    if (audio) {
        audio.addEventListener("loadedmetadata", () => {
            audio.currentTime = currentRange()?.Start ?? 0;
        });
        audio.addEventListener("play", () => {
            const range = currentRange();
            if (range && (audio.currentTime < range.Start || audio.currentTime >= range.End)) {
                audio.currentTime = range.Start;
            }
        });
        audio.addEventListener("timeupdate", () => {
            const range = currentRange();
            if (range && !audio.paused && audio.currentTime >= range.End) {
                audio.pause();
                audio.currentTime = range.Start;
            }
        });
        rate.addEventListener("change", () => {
            audio.playbackRate = Number(rate.value);
        });
        document.getElementById("transcript-tab").addEventListener("show.bs.tab", () => audio.pause());
    }

    if (!form) {
        return;
    }

    function showSentence(newIndex) {
        index = newIndex;
        // Giữ câu đang luyện khi gửi đánh giá.
        ratingPosition.value = index + 1;
        position.textContent = `Câu ${index + 1} / ${sentences.length}`;
        previous.disabled = index === 0;
        next.disabled = skip.disabled = index === sentences.length - 1;
        answer.value = "";
        feedback.textContent = "";
        if (audio) {
            audio.pause();
            if (audio.readyState > 0) {
                audio.currentTime = currentRange()?.Start ?? 0;
            }
        }
        answer.focus();
    }

    function normalize(text) {
        return text.normalize("NFKC").toLowerCase().replace(/['’]/g, "")
            .replace(/[^\p{L}\p{N}]+/gu, " ").trim();
    }

    form.addEventListener("submit", event => {
        event.preventDefault();
        const typed = normalize(answer.value);
        feedback.textContent = typed && typed === normalize(sentences[index].Text)
            ? "Chính xác!"
            : "Chưa khớp. Hãy nghe lại và thử lần nữa.";
    });
    previous.addEventListener("click", () => showSentence(index - 1));
    next.addEventListener("click", () => showSentence(index + 1));
    skip.addEventListener("click", () => showSentence(index + 1));
})();
