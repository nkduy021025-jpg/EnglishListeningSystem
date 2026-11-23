(() => {
    const form = document.getElementById("lesson-rating-form");
    const rating = form.elements.soSao;
    const picker = document.getElementById("rating-stars");
    const stars = [...picker.querySelectorAll("button")];
    const error = document.getElementById("rating-selection-error");

    // Khi rời chuột, hiển thị lại số sao đã chọn.
    function paintStars(score) {
        stars.forEach(star => {
            const filled = Number(star.dataset.rating) <= score;
            const icon = star.querySelector("i");
            icon.classList.toggle("bi-star-fill", filled);
            icon.classList.toggle("bi-star", !filled);
        });
    }

    stars.forEach(star => {
        star.addEventListener("mouseenter", () => paintStars(Number(star.dataset.rating)));
        star.addEventListener("click", () => {
            rating.value = star.dataset.rating;
            stars.forEach(button => button.setAttribute("aria-pressed", String(button === star)));
            paintStars(Number(rating.value));
            error.hidden = true;
        });
    });

    picker.addEventListener("mouseleave", () => paintStars(Number(rating.value)));

    form.addEventListener("submit", event => {
        if (Number(rating.value) === 0) {
            event.preventDefault();
            error.hidden = false;
            stars[0].focus();
        }
    });
})();
