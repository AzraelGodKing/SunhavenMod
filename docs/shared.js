/* ===================================================================
   Sun Haven Mod Hub - Shared JavaScript
   =================================================================== */

// Dark mode: apply stored theme immediately to prevent flash
(function() {
    var stored = localStorage.getItem('sh-mod-theme');
    if (stored) {
        document.documentElement.setAttribute('data-theme', stored);
    }
})();

document.addEventListener('DOMContentLoaded', function() {

    // -----------------------------------------------------------------
    // 1. BACK-TO-TOP BUTTON
    // -----------------------------------------------------------------
    var topBtn = document.createElement('button');
    topBtn.className = 'back-to-top';
    topBtn.setAttribute('aria-label', 'Back to top');
    topBtn.innerHTML = '\u2191';
    topBtn.title = 'Back to top';
    document.body.appendChild(topBtn);

    window.addEventListener('scroll', function() {
        if (window.scrollY > 300) {
            topBtn.classList.add('visible');
        } else {
            topBtn.classList.remove('visible');
        }
    });

    topBtn.addEventListener('click', function() {
        window.scrollTo({ top: 0, behavior: 'smooth' });
    });

    // -----------------------------------------------------------------
    // 2. DARK MODE TOGGLE
    // -----------------------------------------------------------------
    var toggle = document.createElement('button');
    toggle.className = 'theme-toggle';
    toggle.setAttribute('aria-label', 'Toggle dark mode');
    var currentTheme = document.documentElement.getAttribute('data-theme');
    toggle.innerHTML = currentTheme === 'dark' ? '\u2600\uFE0F' : '\uD83C\uDF19';
    document.body.appendChild(toggle);

    toggle.addEventListener('click', function() {
        var current = document.documentElement.getAttribute('data-theme');
        var next = current === 'dark' ? 'light' : 'dark';
        document.documentElement.setAttribute('data-theme', next);
        localStorage.setItem('sh-mod-theme', next);
        toggle.innerHTML = next === 'dark' ? '\u2600\uFE0F' : '\uD83C\uDF19';
    });

    // -----------------------------------------------------------------
    // 3. COPY-TO-CLIPBOARD FOR CONFIG PATHS
    // -----------------------------------------------------------------
    var configCodes = document.querySelectorAll('.config-note code');
    configCodes.forEach(function(codeEl) {
        var wrapper = document.createElement('span');
        wrapper.className = 'copy-wrapper';

        var copyBtn = document.createElement('button');
        copyBtn.className = 'copy-btn';
        copyBtn.innerHTML = '\uD83D\uDCCB';
        copyBtn.title = 'Copy to clipboard';

        codeEl.parentNode.insertBefore(wrapper, codeEl);
        wrapper.appendChild(codeEl);
        wrapper.appendChild(copyBtn);

        copyBtn.addEventListener('click', function() {
            var text = codeEl.textContent;
            if (navigator.clipboard && navigator.clipboard.writeText) {
                navigator.clipboard.writeText(text).then(onCopied);
            } else {
                // Fallback for file:// or older browsers
                var ta = document.createElement('textarea');
                ta.value = text;
                ta.style.position = 'fixed';
                ta.style.opacity = '0';
                document.body.appendChild(ta);
                ta.select();
                document.execCommand('copy');
                document.body.removeChild(ta);
                onCopied();
            }

            function onCopied() {
                copyBtn.innerHTML = '\u2705';
                copyBtn.classList.add('copied');
                setTimeout(function() {
                    copyBtn.innerHTML = '\uD83D\uDCCB';
                    copyBtn.classList.remove('copied');
                }, 2000);
            }
        });
    });

    // -----------------------------------------------------------------
    // 4. SEARCH / FILTER ON INDEX PAGE
    // -----------------------------------------------------------------
    var searchInput = document.getElementById('modSearch');
    if (searchInput) {
        var cards = document.querySelectorAll('.mod-card');
        var tagButtons = document.querySelectorAll('.filter-tag');
        var activeTag = 'all';

        function filterCards() {
            var query = searchInput.value.toLowerCase().trim();
            var visibleCount = 0;
            cards.forEach(function(card) {
                var name = (card.getAttribute('data-name') || '').toLowerCase();
                var desc = card.querySelector('.mod-description');
                var descText = desc ? desc.textContent.toLowerCase() : '';
                var features = card.querySelectorAll('.mod-feature');
                var featureText = Array.from(features).map(function(f) {
                    return f.textContent.toLowerCase();
                }).join(' ');
                var tags = (card.getAttribute('data-tags') || '').split(',');

                var matchesSearch = !query || name.indexOf(query) !== -1 ||
                    descText.indexOf(query) !== -1 || featureText.indexOf(query) !== -1;
                var matchesTag = activeTag === 'all' || tags.indexOf(activeTag) !== -1;

                var show = matchesSearch && matchesTag;
                card.style.display = show ? '' : 'none';
                if (show) visibleCount++;
            });
        }

        searchInput.addEventListener('input', filterCards);

        tagButtons.forEach(function(btn) {
            btn.addEventListener('click', function() {
                tagButtons.forEach(function(b) { b.classList.remove('active'); });
                btn.classList.add('active');
                activeTag = btn.getAttribute('data-tag');
                filterCards();
            });
        });
    }

    // -----------------------------------------------------------------
    // 5. SCROLL-SPY FOR RACIALBONUS NAV PILLS
    // -----------------------------------------------------------------
    var pills = document.querySelectorAll('.nav-pill');
    if (pills.length) {
        var sections = [];
        pills.forEach(function(pill) {
            var href = pill.getAttribute('href');
            if (href && href.charAt(0) === '#') {
                var section = document.getElementById(href.substring(1));
                if (section) sections.push({ pill: pill, section: section });
            }
        });

        if (sections.length) {
            function updateActivePill() {
                var scrollPos = window.scrollY + 150;
                var active = sections[0];
                sections.forEach(function(s) {
                    if (s.section.offsetTop <= scrollPos) active = s;
                });
                pills.forEach(function(p) { p.classList.remove('active'); });
                if (active) active.pill.classList.add('active');
            }
            window.addEventListener('scroll', updateActivePill);
            updateActivePill();
        }
    }

});
