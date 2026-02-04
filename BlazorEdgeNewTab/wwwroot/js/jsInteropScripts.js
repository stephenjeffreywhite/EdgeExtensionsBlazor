window.setBackgroundImage1 = (url) => document.getElementsByTagName("body")[0].style =
    `background-image: url(${url})`;

function enableToasts() {
    var toastElList = [].slice.call(document.querySelectorAll('.toast'));
    var toastList = toastElList.map(function (toastEl) {
        return new window.bootstrap.Toast(toastEl, option);
    });
}

function showSaveChangesToast() {
    var toastTrigger = document.getElementById('saveChangesBtn');
    var toastLiveExample = document.getElementById('saveChangesToast');
    if (toastTrigger) {
        toastTrigger.addEventListener('click', function () {
            var toast = new bootstrap.Toast(toastLiveExample);

            toast.show();
        });
    }
}

window.SetFocusToElement = (element) => {
    element.focus();
}

document.addEventListener("dragover", (e) => {
    // Limit scope so we're not preventing default everywhere:
    if (e.target.classList.contains('QuickLinkContent')) {
        e.preventDefault();      // <-- the key
    }
}, true);