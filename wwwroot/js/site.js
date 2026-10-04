// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Fish image preview + client side upload validation.
(function () {
    'use strict';

    var ALLOWED_EXTENSIONS = ['.jpg', '.jpeg', '.png', '.webp'];
    var MAX_FILE_SIZE = 4 * 1024 * 1024; // 4 MB

    function isAllowed(file) {
        var name = (file.name || '').toLowerCase();
        for (var i = 0; i < ALLOWED_EXTENSIONS.length; i++) {
            if (name.lastIndexOf(ALLOWED_EXTENSIONS[i], name.length - ALLOWED_EXTENSIONS[i].length) > -1) {
                return true;
            }
        }
        return false;
    }

    function formatSize(bytes) {
        return bytes < 1024 * 1024
            ? Math.round(bytes / 1024) + ' KB'
            : (bytes / (1024 * 1024)).toFixed(2) + ' MB';
    }

    function init() {
        var input = document.getElementById('ImageFile');
        var frame = document.getElementById('imagePreview');
        var info = document.getElementById('imageInfo');
        var zone = document.getElementById('uploadZone');
        var errorBox = document.getElementById('imageClientError');

        if (!input || !frame) {
            return;
        }

        function showError(message) {
            if (!errorBox) {
                return;
            }
            errorBox.textContent = message;
            errorBox.classList.toggle('d-none', !message);
        }

        function clearError() {
            showError('');
            input.classList.remove('is-invalid');
        }

        function render(file) {
            if (!file) {
                frame.innerHTML =
                    '<div class="fish-preview-empty">' +
                    '<svg width="46" height="46" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.4">' +
                    '<path d="M2.5 12c2.6-3.6 5.6-5.4 9-5.4 3.2 0 5.6 1.6 7.1 4.2-1.5 3.6-4.4 5.4-7.7 5.4-3.3 0-6.1-1.4-8.4-4.2Z"/>' +
                    '<path d="M17 9.6h.01"/><path d="M2.5 12 6 7.8M2.5 12 6 16.2"/>' +
                    '<path d="M20.5 18.5c-1.6.9-3.2 1.3-4.8 1.3"/>' +
                    '</svg>' +
                    '<div>No image selected</div>' +
                    '<div class="fw-normal">Upload a fish photo to see it here</div>' +
                    '</div>';
                if (info) {
                    info.textContent = '';
                }
                return;
            }

            var reader = new FileReader();
            reader.onload = function (e) {
                frame.innerHTML = '<img alt="Selected fish image preview" src="' + e.target.result + '" />';
            };
            reader.readAsDataURL(file);

            if (info) {
                info.textContent = file.name + ' • ' + formatSize(file.size);
            }
        }

        input.addEventListener('change', function () {
            var file = input.files && input.files.length ? input.files[0] : null;

            if (!file) {
                clearError();
                render(null);
                return;
            }

            if (!isAllowed(file)) {
                showError('Image must be a .jpg, .jpeg, .png or .webp file.');
                input.classList.add('is-invalid');
                render(null);
                return;
            }

            if (file.size > MAX_FILE_SIZE) {
                showError('Image is too large. Maximum size is 4 MB.');
                input.classList.add('is-invalid');
                render(null);
                return;
            }

            clearError();
            render(file);
        });

        if (zone) {
            zone.addEventListener('click', function () {
                input.click();
            });

            ['dragenter', 'dragover'].forEach(function (evt) {
                zone.addEventListener(evt, function (e) {
                    e.preventDefault();
                    zone.classList.add('is-dragover');
                });
            });

            ['dragleave', 'drop'].forEach(function (evt) {
                zone.addEventListener(evt, function (e) {
                    e.preventDefault();
                    zone.classList.remove('is-dragover');
                });
            });

            zone.addEventListener('drop', function (e) {
                if (e.dataTransfer && e.dataTransfer.files.length) {
                    input.files = e.dataTransfer.files;
                    input.dispatchEvent(new Event('change'));
                }
            });
        }

        render(null);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', init);
    } else {
        init();
    }
})();
